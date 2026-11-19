using System;
using System.Collections.Generic;
using DevDogs.WebDevices.Core.Config;
using DevDogs.WebDevices.Core.State;

namespace DevDogs.WebDevices.Core.Routing;

/// <summary>
/// Keeps every device's <see cref="DeviceTarget"/> in step with the game state and the settings.
/// Thread-safe: fed from SimHub's telemetry thread, read from HTTP threads, reconfigured from the UI.
/// </summary>
public sealed class DeviceRouter
{
    private readonly object _gate = new();
    private readonly SwitchDebouncer _debouncer;
    private RouterSettings _settings;
    private Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private NavigationDashStatus _navigation = NavigationDashStatus.NotInstalled;
    private long _cacheBuster;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceRouter"/> class, starting in system idle.
    /// </summary>
    /// <param name="settings">The settings; copied, so later edits to the object have no effect.</param>
    /// <param name="cacheBusterSeed">
    /// The value dashboard paths count up from. Seed with the current time so paths stay unique across restarts.
    /// </param>
    public DeviceRouter(RouterSettings settings, long cacheBusterSeed)
    {
        _settings = Copy(settings);
        _cacheBuster = cacheBusterSeed;
        _debouncer = new SwitchDebouncer(RouterState.SystemIdle, TimeSpan.Zero, TimeSpan.Zero);
        ApplyDelays();
        Rebuild();
    }

    /// <summary>
    /// Raised, outside any lock, with the names of the devices whose target changed (including removed devices).
    /// Handlers run on the caller's thread and must return quickly.
    /// </summary>
    public event Action<IReadOnlyCollection<string>>? TargetsChanged;

    /// <summary>Gets the debounced state devices are showing.</summary>
    public RouterState State
    {
        get
        {
            lock (_gate)
            {
                return _debouncer.Current;
            }
        }
    }

    /// <summary>Gets the port of SimHub's web server, which devices load dashboards from.</summary>
    public int SimHubWebPort
    {
        get
        {
            lock (_gate)
            {
                return _settings.SimHubWebPort;
            }
        }
    }

    /// <summary>Gets the DevDogs Navigation Dash status targets are built with.</summary>
    public NavigationDashStatus Navigation
    {
        get
        {
            lock (_gate)
            {
                return _navigation;
            }
        }
    }

    /// <summary>Gets a copy of the settings in use.</summary>
    public RouterSettings Settings
    {
        get
        {
            lock (_gate)
            {
                return Copy(_settings);
            }
        }
    }

    /// <summary>
    /// Records one telemetry frame.
    /// </summary>
    /// <param name="signals">The frame's game flags.</param>
    /// <param name="now">A monotonic timestamp.</param>
    /// <returns><see langword="true"/> when the debounced state changed on this frame.</returns>
    public bool Observe(GameSignals signals, TimeSpan now)
    {
        IReadOnlyCollection<string> changed;
        lock (_gate)
        {
            if (!_debouncer.Update(GameStateDetector.Detect(signals), now))
            {
                return false;
            }

            changed = Rebuild();
        }

        Raise(changed);
        return true;
    }

    /// <summary>
    /// Replaces the settings and recomputes every target. The port is not applied here; the HTTP server owns it.
    /// </summary>
    /// <param name="settings">The new settings; copied.</param>
    public void ApplySettings(RouterSettings settings)
    {
        IReadOnlyCollection<string> changed;
        lock (_gate)
        {
            _settings = Copy(settings);
            ApplyDelays();
            changed = Rebuild();
        }

        Raise(changed);
    }

    /// <summary>
    /// Records whether DevDogs Navigation Dash can be shown and recomputes the targets that use it.
    /// </summary>
    /// <param name="status">The status read from Navigation Dash's SimHub properties.</param>
    /// <returns><see langword="true"/> when the status differs from the one in use.</returns>
    public bool UpdateNavigation(NavigationDashStatus status)
    {
        IReadOnlyCollection<string> changed;
        lock (_gate)
        {
            if (_navigation == status)
            {
                return false;
            }

            _navigation = status;
            changed = Rebuild();
        }

        Raise(changed);
        return true;
    }

    /// <summary>
    /// Returns what <paramref name="deviceName"/> should show.
    /// </summary>
    /// <param name="deviceName">The device name from the URL, matched ignoring case.</param>
    /// <returns>The target, or <see langword="null"/> when no such device is configured.</returns>
    public DeviceTarget? GetTarget(string deviceName)
    {
        lock (_gate)
        {
            return _entries.TryGetValue(deviceName, out var entry) ? entry.Target : null;
        }
    }

    private static RouterSettings Copy(RouterSettings settings) =>
        RouterSettingsJson.Deserialize(RouterSettingsJson.Serialize(settings));

    private void ApplyDelays()
    {
        _debouncer.SystemIdleDelay = TimeSpan.FromMilliseconds(Math.Max(0, _settings.SystemIdleDelayMs));
        _debouncer.SwitchDelay = TimeSpan.FromMilliseconds(Math.Max(0, _settings.SwitchDelayMs));
    }

    private List<string> Rebuild()
    {
        var state = _debouncer.Current;
        var entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        var changed = new List<string>();

        foreach (var device in _settings.Devices)
        {
            if (entries.ContainsKey(device.Name))
            {
                continue; // duplicate names are rejected by validation; the first one wins if one slips through
            }

            var key = KeyFor(DashboardResolver.Resolve(device, state), device.HideControls);
            if (_entries.TryGetValue(device.Name, out var previous) && previous.Key == key)
            {
                // Unchanged, so the frame keeps its page: the same dashboard in two states does not reload.
                entries[device.Name] = previous;
                continue;
            }

            entries[device.Name] = new Entry(key, TargetFor(key));
            changed.Add(device.Name);
        }

        foreach (var name in _entries.Keys)
        {
            if (!entries.ContainsKey(name))
            {
                changed.Add(name);
            }
        }

        _entries = entries;
        return changed;
    }

    /// <summary>
    /// Returns everything a device's frame depends on; a new target is built only when this changes.
    /// </summary>
    private Key KeyFor(string? dashboard, bool hideControls)
    {
        if (dashboard is null)
        {
            return new Key(null, false, null, null, null);
        }

        if (!NavigationDash.IsCode(dashboard))
        {
            return new Key(dashboard, hideControls, _settings.SimHubWebPort, null, null);
        }

        // SimHub's dashboard controls do not apply to the map page, so the setting is left out of its key.
        if (_navigation.Ready)
        {
            return new Key(NavigationDash.Code, false, _navigation.Port, null, null);
        }

        return _navigation.Installed
            ? new Key(NavigationDash.Code, false, null, NavigationDash.DisplayName + " is not ready.", _navigation.Reason ?? "Waiting for it to load the map.")
            : new Key(NavigationDash.Code, false, null, NavigationDash.DisplayName + " is not installed in SimHub.", "Install and enable it, or pick another dashboard for this state.");
    }

    private DeviceTarget TargetFor(Key key)
    {
        if (key.Dashboard is null)
        {
            return new DeviceTarget(null, null);
        }

        if (key.Notice is not null)
        {
            return new DeviceTarget(key.Dashboard, null, null, key.Notice, key.NoticeDetail);
        }

        var path = NavigationDash.IsCode(key.Dashboard)
            ? NavigationDash.Path
            : DashUrl.Build(key.Dashboard, key.HideControls, ++_cacheBuster);
        return new DeviceTarget(key.Dashboard, path, key.Port);
    }

    private void Raise(IReadOnlyCollection<string> changed)
    {
        if (changed.Count > 0)
        {
            TargetsChanged?.Invoke(changed);
        }
    }

    private sealed record Key(string? Dashboard, bool HideControls, int? Port, string? Notice, string? NoticeDetail);

    private sealed record Entry(Key Key, DeviceTarget Target);
}
