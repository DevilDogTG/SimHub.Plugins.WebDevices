using System;

namespace DevDogs.WebDevices.Core.Routing;

/// <summary>
/// Whether DevDogs Navigation Dash can be shown, read from its SimHub properties. Value equality, so an unchanged
/// status never rebuilds device targets.
/// </summary>
/// <param name="Installed">Whether the plugin's properties exist in SimHub.</param>
/// <param name="Ready">Whether the page can show the map.</param>
/// <param name="Port">The port its page is served on.</param>
/// <param name="Reason">Why it is not ready, as the plugin words it; <see langword="null"/> when it says nothing.</param>
public sealed record NavigationDashStatus(bool Installed, bool Ready, int Port, string? Reason)
{
    /// <summary>Gets the status when the plugin is not in SimHub.</summary>
    public static NavigationDashStatus NotInstalled { get; } = new(false, false, NavigationDash.DefaultPort, null);

    /// <summary>
    /// Builds the status from the values of the plugin's SimHub properties.
    /// </summary>
    /// <param name="url">The value of <see cref="NavigationDash.UrlProperty"/>, or <see langword="null"/> when missing.</param>
    /// <param name="ready">The value of <see cref="NavigationDash.ReadyProperty"/>, or <see langword="null"/> when missing.</param>
    /// <param name="status">The value of <see cref="NavigationDash.StatusProperty"/>, or <see langword="null"/> when missing.</param>
    /// <returns>The status; installed when either the address or the ready flag exists.</returns>
    public static NavigationDashStatus From(object? url, object? ready, object? status)
    {
        if (url is null && ready is null)
        {
            return NotInstalled;
        }

        var reason = status?.ToString()?.Trim();
        return new NavigationDashStatus(
            Installed: true,
            Ready: IsTrue(ready),
            Port: PortOf(url as string),
            Reason: string.IsNullOrEmpty(reason) ? null : reason);
    }

    private static bool IsTrue(object? value) => value switch
    {
        bool b => b,
        string s => bool.TryParse(s.Trim(), out var b) && b,
        _ => false,
    };

    private static int PortOf(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) && uri.Port is > 0 and <= 65535
            ? uri.Port
            : NavigationDash.DefaultPort;
}
