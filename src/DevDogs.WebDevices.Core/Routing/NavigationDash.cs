using System;

namespace DevDogs.WebDevices.Core.Routing;

/// <summary>
/// DevDogs Navigation Dash as a device target (docs/adr/ADR-0009): a map page served by the
/// <c>DDNavigationDashPlugin</c> SimHub plugin from its own port, chosen in the dashboard fields by a reserved code.
/// </summary>
public static class NavigationDash
{
    /// <summary>
    /// The value stored in a dashboard field for this target. <c>:</c> cannot appear in a Windows folder name, so no
    /// SimHub dashboard code (a <c>DashTemplates</c> folder name) can be the same. Never change it.
    /// </summary>
    public const string Code = "devdogs:navigation-dash";

    /// <summary>The name shown in the dashboard pickers.</summary>
    public const string DisplayName = "DevDogs Navigation Dash";

    /// <summary>Navigation Dash's default port, used when its address cannot be read.</summary>
    public const int DefaultPort = 8893;

    /// <summary>The path of the map page on Navigation Dash's server.</summary>
    public const string Path = "/";

    /// <summary>The SimHub property holding the page address, <c>http://&lt;LAN IP&gt;:&lt;port&gt;/</c>.</summary>
    public const string UrlProperty = "DDNavigationDashPlugin.Dash.Url";

    /// <summary>The SimHub property that is true once the page can show the map.</summary>
    public const string ReadyProperty = "DDNavigationDashPlugin.Status.Ready";

    /// <summary>The SimHub property saying why the page is not ready.</summary>
    public const string StatusProperty = "DDNavigationDashPlugin.Status";

    /// <summary>
    /// Returns whether a dashboard value chooses Navigation Dash.
    /// </summary>
    /// <param name="dashboard">A stored dashboard value.</param>
    /// <returns><see langword="true"/> for <see cref="Code"/>, ignoring case and surrounding spaces.</returns>
    public static bool IsCode(string? dashboard) =>
        string.Equals(dashboard?.Trim(), Code, StringComparison.OrdinalIgnoreCase);
}
