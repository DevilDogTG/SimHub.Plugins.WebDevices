using System;
using System.Collections.Generic;
using System.Linq;
using DevDogs.WebDevices.Core.Routing;

namespace DevDogs.WebDevices.Core.Editing;

/// <summary>
/// What the dashboard pickers offer: SimHub's listed dashboards grouped by their Dash Studio category, with
/// DevDogs Navigation Dash in its own group first when it is installed. Also describes any stored value, listed
/// or not, for the closed picker.
/// </summary>
public sealed class DashboardChoices
{
    /// <summary>The group DevDogs' own targets are listed under, first.</summary>
    public const string DevDogsPlugins = "DevDogs Plugins";

    private const string NavigationDashDetail = "Live truck map · needs WebGL 2 on the device";

    private static readonly StringComparer Text = StringComparer.OrdinalIgnoreCase;

    private readonly Dictionary<string, DashboardChoice> _installed = new(Text);
    private readonly DashboardChoice _navigationDash;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardChoices"/> class.
    /// </summary>
    /// <param name="dashboards">The dashboards installed in SimHub.</param>
    /// <param name="navigationDashInstalled">Whether DevDogs Navigation Dash is in SimHub; only then is it listed.</param>
    public DashboardChoices(IReadOnlyList<DashboardInfo> dashboards, bool navigationDashInstalled)
    {
        // A category typed twice with different case is one group, spelled as first met.
        var categories = new Dictionary<string, string>(Text);
        foreach (var dashboard in dashboards)
        {
            if (_installed.ContainsKey(dashboard.Code))
            {
                continue;
            }

            if (!categories.TryGetValue(dashboard.Category, out var category))
            {
                categories[dashboard.Category] = category = dashboard.Category;
            }

            _installed[dashboard.Code] = new DashboardChoice(
                dashboard.Code,
                dashboard.Title,
                category,
                Detail(dashboard),
                dashboard.PreviewPath,
                dashboard.Listed ? DashboardAvailability.Listed : DashboardAvailability.Hidden);
        }

        _navigationDash = new DashboardChoice(
            NavigationDash.Code,
            NavigationDash.DisplayName,
            DevDogsPlugins,
            NavigationDashDetail,
            PreviewPath: null,
            navigationDashInstalled ? DashboardAvailability.Listed : DashboardAvailability.NotInstalled);

        Listed = _installed.Values.Append(_navigationDash)
            .Where(c => c.Availability == DashboardAvailability.Listed)
            .OrderBy(GroupRank)
            .ThenBy(c => c.Category, Text)
            .ThenBy(c => c.Title, Text)
            .ThenBy(c => c.Code, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Gets the entries the list offers, in display order: group by group, by title within a group.</summary>
    public IReadOnlyList<DashboardChoice> Listed { get; }

    /// <summary>
    /// Returns the listed entries matching a search, in display order.
    /// </summary>
    /// <param name="search">
    /// Words separated by spaces; each must appear, ignoring case, in the title, code, category or detail (which
    /// holds the author). Blank matches everything.
    /// </param>
    /// <returns>The matching entries.</returns>
    public IReadOnlyList<DashboardChoice> Search(string? search)
    {
        var words = (search ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return words.Length == 0 ? Listed : Listed.Where(c => words.All(word => Matches(c, word))).ToList();
    }

    /// <summary>
    /// Describes a stored dashboard value for the closed picker.
    /// </summary>
    /// <param name="stored">The stored value.</param>
    /// <returns>
    /// The entry, marked hidden or not installed when it is not in the list; an entry titled by the value itself
    /// when nothing is known about it; <see langword="null"/> for a blank value.
    /// </returns>
    public DashboardChoice? Describe(string? stored)
    {
        var code = stored?.Trim();
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }

        if (NavigationDash.IsCode(code))
        {
            return _navigationDash;
        }

        return _installed.TryGetValue(code!, out var choice)
            ? choice
            : new DashboardChoice(code!, code!, DashboardCatalog.Uncategorized, "", null, DashboardAvailability.NotInstalled);
    }

    /// <summary>
    /// Returns whether a stored dashboard value is installed in SimHub, listed or not.
    /// </summary>
    /// <param name="stored">The stored value.</param>
    /// <returns><see langword="true"/> when the dashboard (or Navigation Dash) is installed.</returns>
    public bool IsInstalled(string? stored) => Describe(stored) is { Availability: not DashboardAvailability.NotInstalled };

    /// <summary>
    /// Returns the name shown for a stored dashboard value.
    /// </summary>
    /// <param name="stored">The stored value.</param>
    /// <returns>Its title, the value itself when unknown, or an empty text for a blank value.</returns>
    public string TitleOf(string? stored) => Describe(stored)?.Title ?? "";

    private static string Detail(DashboardInfo dashboard)
    {
        var parts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(dashboard.Author))
        {
            parts.Add(dashboard.Author!);
        }

        if (!Text.Equals(dashboard.Title, dashboard.Code))
        {
            parts.Add(dashboard.Code);
        }

        return string.Join(" · ", parts);
    }

    private static int GroupRank(DashboardChoice choice) =>
        Text.Equals(choice.Category, DevDogsPlugins) ? 0
        : Text.Equals(choice.Category, DashboardCatalog.Uncategorized) ? 2
        : 1;

    private static bool Matches(DashboardChoice choice, string word) =>
        Contains(choice.Title, word) || Contains(choice.Code, word) || Contains(choice.Category, word) || Contains(choice.Detail, word);

    private static bool Contains(string text, string word) => text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;
}
