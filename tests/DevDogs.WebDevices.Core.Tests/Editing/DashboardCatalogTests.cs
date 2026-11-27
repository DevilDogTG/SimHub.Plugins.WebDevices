using System;
using System.IO;
using System.Linq;
using DevDogs.WebDevices.Core.Editing;
using FluentAssertions;
using Xunit;

namespace DevDogs.WebDevices.Core.Tests.Editing;

public sealed class DashboardCatalogTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "webdevices-dash-" + Guid.NewGuid().ToString("N"));

    public DashboardCatalogTests()
    {
        Directory.CreateDirectory(_root);
    }

    private string Settings => Path.Combine(_root, "DashStudioSettings_2.json");

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string Dashboard(string folder, string? metadata = null, string? file = null, bool preview = false, bool deviceScope = false)
    {
        var dir = Directory.CreateDirectory(Path.Combine(_root, "DashTemplates", folder)).FullName;
        var djson = Path.Combine(dir, (file ?? folder) + ".djson");
        File.WriteAllText(djson, "{}");
        if (metadata is not null)
        {
            File.WriteAllText(djson + ".metadata", metadata);
        }

        if (preview)
        {
            File.WriteAllBytes(djson + ".png", [137, 80, 78, 71]);
        }

        if (deviceScope)
        {
            File.WriteAllText(djson + ".device-scope.json", "{}");
        }

        return djson;
    }

    private DashboardInfo Only() =>
        DashboardCatalog.Scan(Path.Combine(_root, "DashTemplates"), Settings).Should().ContainSingle().Subject;

    [Fact]
    public void Lists_folders_that_hold_a_dashboard_of_the_same_name_sorted_by_code_ignoring_case()
    {
        Dashboard("MYTEC C127 - System Info");
        Dashboard("clock");
        Dashboard("Ressources", file: "something-else");
        Directory.CreateDirectory(Path.Combine(_root, "DashTemplates", "_Library"));
        File.WriteAllText(Path.Combine(_root, "DashTemplates", "loose.djson"), "{}");

        DashboardCatalog.Scan(Path.Combine(_root, "DashTemplates")).Select(d => d.Code)
            .Should().Equal("clock", "MYTEC C127 - System Info");
    }

    [Fact]
    public void A_missing_folder_gives_an_empty_list()
    {
        DashboardCatalog.Scan(Path.Combine(_root, "nope")).Should().BeEmpty();
    }

    [Fact]
    public void Reads_title_and_author_from_the_metadata()
    {
        Dashboard("ATS-DASH", """{ "Title": " ATS-DASH-ROUTER-ADV ", "Author": "oliverCJ", "IsOverlay": false }""");

        Only().Should().Be(new DashboardInfo("ATS-DASH", "ATS-DASH-ROUTER-ADV", "oliverCJ", "Uncategorized", null, Listed: true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("""{ "Title": null, "Author": "  " }""")]
    [InlineData("""{ "Title": 12 }""")]
    [InlineData("not json {")]
    public void Without_a_usable_metadata_title_the_code_is_the_title(string? metadata)
    {
        Dashboard("AudiConcept", metadata);

        var dashboard = Only();
        dashboard.Title.Should().Be("AudiConcept");
        dashboard.Author.Should().BeNull();
        dashboard.Listed.Should().BeTrue();
    }

    [Fact]
    public void Takes_the_category_from_dash_studio_settings_matching_the_code_ignoring_case()
    {
        Dashboard("SimHub - FordGT", """{ "Category": "ETS2" }""");
        File.WriteAllText(Settings, """
            {
              "SimHubWebPort": 8888,
              "DashParameters": {
                "Simhub - FordGT": { "OverrideControls": false, "Category": "Generic" },
                "Other": { "Category": "AIM" }
              }
            }
            """);

        Only().Category.Should().Be("Generic");
    }

    [Theory]
    [InlineData("""{ "DashParameters": { "Example": { "LastUse": "2026-11-25" } } }""")]
    [InlineData("""{ "DashParameters": { "Example": { "Category": " " } } }""")]
    [InlineData("""{ "DashParameters": [] }""")]
    [InlineData("""{ }""")]
    [InlineData("broken")]
    [InlineData(null)]
    public void Without_a_dash_studio_category_it_is_uncategorized_even_when_the_metadata_names_one(string? settings)
    {
        Dashboard("Example", """{ "Category": "ETS2" }""");
        if (settings is not null)
        {
            File.WriteAllText(Settings, settings);
        }

        Only().Category.Should().Be(DashboardCatalog.Uncategorized);
    }

    [Fact]
    public void Gives_the_preview_path_when_the_picture_exists()
    {
        var djson = Dashboard("AIM GS-DASH", preview: true);
        Dashboard("MOZA Wheelbase Control");

        var dashboards = DashboardCatalog.Scan(Path.Combine(_root, "DashTemplates"));

        dashboards[0].PreviewPath.Should().Be(djson + ".png");
        dashboards[1].PreviewPath.Should().BeNull();
    }

    [Fact]
    public void Overlays_and_device_only_dashboards_are_not_listed()
    {
        Dashboard("SimHub - Hud 1", """{ "IsOverlay": true }""");
        Dashboard("VendorHeusinkveldDisplayDash", """{ "IsOverlay": false }""", deviceScope: true);
        Dashboard("System Monitor", """{ "IsOverlay": false }""");

        DashboardCatalog.Scan(Path.Combine(_root, "DashTemplates")).Select(d => (d.Code, d.Listed)).Should().Equal(
            ("SimHub - Hud 1", false),
            ("System Monitor", true),
            ("VendorHeusinkveldDisplayDash", false));
    }
}
