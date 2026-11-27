namespace DevDogs.WebDevices.Core.Editing;

/// <summary>
/// A dashboard installed in SimHub, as <see cref="DashboardCatalog"/> reads it.
/// </summary>
/// <param name="Code">The <c>DashTemplates</c> folder name, which SimHub's web server uses in dashboard URLs.</param>
/// <param name="Title">The title SimHub shows: the metadata title, else the code.</param>
/// <param name="Author">The author from the metadata, if any.</param>
/// <param name="Category">The category set in Dash Studio, else <see cref="DashboardCatalog.Uncategorized"/>.</param>
/// <param name="PreviewPath">The full path of the preview picture, if there is one.</param>
/// <param name="Listed">
/// Whether SimHub lists it for web dashboards: <see langword="false"/> for overlays and dashboards made for one
/// kind of device.
/// </param>
public sealed record DashboardInfo(string Code, string Title, string? Author, string Category, string? PreviewPath, bool Listed);
