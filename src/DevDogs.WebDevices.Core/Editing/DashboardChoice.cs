using DevDogs.WebDevices.Core.Routing;

namespace DevDogs.WebDevices.Core.Editing;

/// <summary>Whether a dashboard picker entry can be chosen from the list.</summary>
public enum DashboardAvailability
{
    /// <summary>Installed and in the list.</summary>
    Listed,

    /// <summary>Installed but left out of the list, as SimHub leaves it out of its own: an overlay or a device-only dashboard.</summary>
    Hidden,

    /// <summary>Not installed in SimHub.</summary>
    NotInstalled,
}

/// <summary>
/// One entry of the dashboard pickers.
/// </summary>
/// <param name="Code">The value stored in the settings.</param>
/// <param name="Title">The name shown.</param>
/// <param name="Category">The group it is listed under.</param>
/// <param name="Detail">The second line: author, and the code when it differs from the title.</param>
/// <param name="PreviewPath">The full path of the preview picture, if there is one.</param>
/// <param name="Availability">Whether it is installed and listed.</param>
public sealed record DashboardChoice(
    string Code,
    string Title,
    string Category,
    string Detail,
    string? PreviewPath,
    DashboardAvailability Availability)
{
    /// <summary>Gets a value indicating whether this entry is DevDogs Navigation Dash rather than a SimHub dashboard.</summary>
    public bool IsNavigationDash => NavigationDash.IsCode(Code);

    /// <summary>Gets the short note shown next to a stored value that is not in the list, or <see langword="null"/>.</summary>
    public string? Note => Availability switch
    {
        DashboardAvailability.Hidden => "hidden in SimHub's list",
        DashboardAvailability.NotInstalled => "not installed",
        _ => null,
    };
}
