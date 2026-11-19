namespace DevDogs.WebDevices.Core.Routing;

/// <summary>
/// What one device should show right now.
/// </summary>
/// <param name="Dashboard">
/// The dashboard code (or <see cref="NavigationDash.Code"/>), or <see langword="null"/> when nothing is configured
/// for the state.
/// </param>
/// <param name="Path">
/// The page path on the server at <paramref name="Port"/>: a SimHub dashboard (see <see cref="DashUrl"/>) or
/// <see cref="NavigationDash.Path"/>. <see langword="null"/> when there is nothing to frame. A new path or port means
/// the device frame must reload.
/// </param>
/// <param name="Port">The port the page is served on; <see langword="null"/> when there is no path.</param>
/// <param name="Notice">Shown instead of a page when the chosen target cannot be shown, such as Navigation Dash not ready.</param>
/// <param name="NoticeDetail">A second, smaller line under <paramref name="Notice"/>.</param>
public sealed record DeviceTarget(
    string? Dashboard,
    string? Path,
    int? Port = null,
    string? Notice = null,
    string? NoticeDetail = null);
