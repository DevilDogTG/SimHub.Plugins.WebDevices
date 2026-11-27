using System.Collections.Generic;
using System.Linq;
using DevDogs.WebDevices.Core.Editing;
using DevDogs.WebDevices.Core.Routing;
using FluentAssertions;
using Xunit;

namespace DevDogs.WebDevices.Core.Tests.Editing;

public class DashboardChoicesTests
{
    private static readonly IReadOnlyList<DashboardInfo> Installed =
    [
        new("AIM MXS", "AIM MXS", "Wotever", "AIM", @"C:\dash\AIM MXS.djson.png", Listed: true),
        new("AIM GS-DASH", "AIM GS-DASH", "Wotever", "AIM", null, Listed: true),
        new("ATS-DASH", "ATS-DASH-ROUTER-ADV", "oliverCJ", "Truck Simulator", null, Listed: true),
        new("System Monitor", "System Monitor", "Wotever", DashboardCatalog.Uncategorized, null, Listed: true),
        new("Example", "Example", null, DashboardCatalog.Uncategorized, null, Listed: true),
        new("ControlCenter", "ControlCenter", "Wotever", "Generic", null, Listed: true),
        new("SimHub - Hud 1", "SimHub - Hud 1", null, DashboardCatalog.Uncategorized, null, Listed: false),
    ];

    private static DashboardChoices Create(bool navigationDash = true, IReadOnlyList<DashboardInfo>? dashboards = null) =>
        new(dashboards ?? Installed, navigationDash);

    private static IEnumerable<(string Category, string Title)> Shown(IEnumerable<DashboardChoice> choices) =>
        choices.Select(c => (c.Category, c.Title));

    [Fact]
    public void Lists_devdogs_plugins_first_then_categories_a_to_z_and_uncategorized_last_by_title_within_each()
    {
        Shown(Create().Listed).Should().Equal(
            ("DevDogs Plugins", "DevDogs Navigation Dash"),
            ("AIM", "AIM GS-DASH"),
            ("AIM", "AIM MXS"),
            ("Generic", "ControlCenter"),
            ("Truck Simulator", "ATS-DASH-ROUTER-ADV"),
            ("Uncategorized", "Example"),
            ("Uncategorized", "System Monitor"));
    }

    [Fact]
    public void Leaves_out_navigation_dash_when_it_is_not_installed_and_hidden_dashboards_always()
    {
        var listed = Create(navigationDash: false).Listed;

        listed.Should().NotContain(c => c.IsNavigationDash || c.Code == "SimHub - Hud 1");
        listed.Should().NotContain(c => c.Category == DashboardChoices.DevDogsPlugins);
    }

    [Fact]
    public void Merges_categories_that_differ_only_in_case_spelled_as_first_met()
    {
        var choices = Create(dashboards:
        [
            new("a", "a", null, "Truck Simulator", null, Listed: true),
            new("b", "b", null, "truck simulator", null, Listed: true),
        ]);

        choices.Listed.Where(c => !c.IsNavigationDash).Select(c => c.Category).Should().Equal("Truck Simulator", "Truck Simulator");
    }

    [Fact]
    public void Detail_holds_the_author_and_the_code_when_it_differs_from_the_title()
    {
        var choices = Create();

        choices.Describe("ATS-DASH")!.Detail.Should().Be("oliverCJ · ATS-DASH");
        choices.Describe("AIM MXS")!.Detail.Should().Be("Wotever");
        choices.Describe("Example")!.Detail.Should().BeEmpty();
        choices.Describe(NavigationDash.Code)!.Detail.Should().Contain("WebGL 2");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_search_lists_everything(string? search)
    {
        var choices = Create();

        choices.Search(search).Should().Equal(choices.Listed);
    }

    [Theory]
    [InlineData("aim", "AIM GS-DASH", "AIM MXS")]
    [InlineData("router", "ATS-DASH-ROUTER-ADV")]
    [InlineData("ats-dash", "ATS-DASH-ROUTER-ADV")]
    [InlineData("truck", "DevDogs Navigation Dash", "ATS-DASH-ROUTER-ADV")]
    [InlineData("wotever aim", "AIM GS-DASH", "AIM MXS")]
    [InlineData("  WOTEVER   mxs ", "AIM MXS")]
    [InlineData("uncategorized", "Example", "System Monitor")]
    [InlineData("hud")]
    [InlineData("nothing like this")]
    public void Search_needs_every_word_in_title_code_category_or_detail(string search, params string[] titles)
    {
        Create().Search(search).Select(c => c.Title).Should().Equal(titles);
    }

    [Fact]
    public void Describes_listed_hidden_and_unknown_values_and_nothing_for_blank()
    {
        var choices = Create();

        choices.Describe("atS-dash").Should().BeSameAs(choices.Listed.Single(c => c.Code == "ATS-DASH"));
        choices.Describe("SimHub - Hud 1")!.Availability.Should().Be(DashboardAvailability.Hidden);
        choices.Describe("SimHub - Hud 1")!.Note.Should().Be("hidden in SimHub's list");
        var deleted = choices.Describe(" Deleted dash ")!;
        deleted.Should().Be(new DashboardChoice("Deleted dash", "Deleted dash", "Uncategorized", "", null, DashboardAvailability.NotInstalled));
        deleted.Note.Should().Be("not installed");
        choices.Describe(null).Should().BeNull();
        choices.Describe(" ").Should().BeNull();
    }

    [Fact]
    public void Describes_navigation_dash_by_name_and_as_not_installed_when_missing()
    {
        Create().Describe(NavigationDash.Code)!.Should().Match<DashboardChoice>(c =>
            c.Title == "DevDogs Navigation Dash" && c.Availability == DashboardAvailability.Listed && c.Note == null);
        Create(navigationDash: false).Describe(NavigationDash.Code)!.Availability.Should().Be(DashboardAvailability.NotInstalled);
    }

    [Fact]
    public void Installed_means_known_to_simhub_listed_or_hidden()
    {
        var choices = Create(navigationDash: false);

        choices.IsInstalled("system monitor").Should().BeTrue();
        choices.IsInstalled("SimHub - Hud 1").Should().BeTrue();
        choices.IsInstalled("Deleted dash").Should().BeFalse();
        choices.IsInstalled(NavigationDash.Code).Should().BeFalse();
        Create().IsInstalled(NavigationDash.Code).Should().BeTrue();
    }

    [Fact]
    public void Title_of_a_value_is_its_shown_name()
    {
        var choices = Create();

        choices.TitleOf("ATS-DASH").Should().Be("ATS-DASH-ROUTER-ADV");
        choices.TitleOf(NavigationDash.Code).Should().Be("DevDogs Navigation Dash");
        choices.TitleOf("Deleted dash").Should().Be("Deleted dash");
        choices.TitleOf(null).Should().BeEmpty();
    }
}
