using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevDogs.WebDevices.Core.Editing;

/// <summary>
/// Lists the dashboards installed in SimHub. Each lives in <c>DashTemplates\&lt;code&gt;\&lt;code&gt;.djson</c>,
/// and the folder name is the code SimHub's web server uses in dashboard URLs. Next to the djson SimHub keeps
/// <c>.djson.metadata</c> (title, author, overlay flag), <c>.djson.png</c> (preview) and, for a dashboard made
/// for one kind of device, <c>.djson.device-scope.json</c>.
/// </summary>
public static class DashboardCatalog
{
    /// <summary>The category of dashboards with none set in Dash Studio, as SimHub names it.</summary>
    public const string Uncategorized = "Uncategorized";

    /// <summary>
    /// Returns the dashboards found under <paramref name="dashTemplatesFolder"/>, sorted by code ignoring case.
    /// Never throws on a missing or unreadable metadata or settings file: the dashboard is then titled by its code,
    /// uncategorized and listed.
    /// </summary>
    /// <param name="dashTemplatesFolder">SimHub's <c>DashTemplates</c> folder.</param>
    /// <param name="dashStudioSettingsFile">
    /// SimHub's <c>PluginsData\Common\DashStudioSettings_2.json</c>, which holds the categories set in Dash Studio
    /// (<c>DashParameters.&lt;code&gt;.Category</c>); none are read when omitted.
    /// </param>
    /// <returns>The dashboards; empty when the folder does not exist.</returns>
    public static IReadOnlyList<DashboardInfo> Scan(string dashTemplatesFolder, string? dashStudioSettingsFile = null)
    {
        if (!Directory.Exists(dashTemplatesFolder))
        {
            return [];
        }

        var categories = ReadCategories(dashStudioSettingsFile);
        return Directory.EnumerateDirectories(dashTemplatesFolder)
            .Select(Path.GetFileName)
            .Where(code => File.Exists(Path.Combine(dashTemplatesFolder, code, code + ".djson")))
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .Select(code => Read(Path.Combine(dashTemplatesFolder, code, code + ".djson"), code, categories))
            .ToList();
    }

    private static DashboardInfo Read(string djson, string code, IReadOnlyDictionary<string, string> categories)
    {
        var metadata = ReadJson(djson + ".metadata");
        var preview = djson + ".png";
        var overlay = metadata?["IsOverlay"] is { Type: JTokenType.Boolean } flag && (bool)flag;
        return new DashboardInfo(
            code,
            Text(metadata?["Title"]) ?? code,
            Text(metadata?["Author"]),
            categories.TryGetValue(code, out var category) ? category : Uncategorized,
            File.Exists(preview) ? preview : null,
            Listed: !overlay && !File.Exists(djson + ".device-scope.json"));
    }

    /// <summary>
    /// Reads the Dash Studio categories by dashboard code, ignoring case: SimHub keys them by code but does not
    /// keep the folder's case (<c>Simhub - FordGT</c> for the folder <c>SimHub - FordGT</c>). The metadata's own
    /// category is not used; SimHub ignores it too.
    /// </summary>
    private static Dictionary<string, string> ReadCategories(string? dashStudioSettingsFile)
    {
        var categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (dashStudioSettingsFile is null || ReadJson(dashStudioSettingsFile)?["DashParameters"] is not JObject parameters)
        {
            return categories;
        }

        foreach (var dashboard in parameters.Properties())
        {
            if (dashboard.Value is JObject values && Text(values["Category"]) is { } category)
            {
                categories[dashboard.Name] = category;
            }
        }

        return categories;
    }

    private static JObject? ReadJson(string path)
    {
        try
        {
            return File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private static string? Text(JToken? token) =>
        token is { Type: JTokenType.String } && !string.IsNullOrWhiteSpace((string?)token) ? ((string)token!).Trim() : null;
}
