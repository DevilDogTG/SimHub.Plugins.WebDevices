using System;
using System.Collections.Generic;
using DevDogs.WebDevices.Core.Config;
using DevDogs.WebDevices.Core.Routing;
using DevDogs.WebDevices.Core.State;
using FluentAssertions;
using Xunit;

namespace DevDogs.WebDevices.Core.Tests.Routing;

public class DeviceRouterNavigationTests
{
    private static readonly GameSignals Ets2Menu = new("ETS2", true, false, false);
    private static readonly GameSignals Ets2Driving = new("ETS2", true, true, false);
    private static readonly GameSignals Ets2Paused = new("ETS2", true, true, false, Paused: true);
    private static readonly NavigationDashStatus Ready = new(true, true, 8893, "Ready");
    private static readonly NavigationDashStatus Loading = new(true, false, 8893, "Waiting for map data");

    private static RouterSettings Settings(string? gameIdle = NavigationDash.Code) => new()
    {
        SystemIdleDelayMs = 0,
        SwitchDelayMs = 0,
        SimHubWebPort = 8888,
        Devices =
        {
            new DeviceConfig
            {
                Name = "tablet",
                HideControls = true,
                SystemIdleDashboard = "Clock",
                Games = { new GameMapping { GameName = "ETS2", GameIdle = gameIdle, Driving = NavigationDash.Code } },
            },
        },
    };

    private static (DeviceRouter Router, List<IReadOnlyCollection<string>> Events) Create(
        NavigationDashStatus? navigation = null,
        RouterSettings? settings = null)
    {
        var router = new DeviceRouter(settings ?? Settings(), cacheBusterSeed: 1000);
        if (navigation is not null)
        {
            router.UpdateNavigation(navigation);
        }

        var events = new List<IReadOnlyCollection<string>>();
        router.TargetsChanged += events.Add;
        return (router, events);
    }

    [Fact]
    public void Simhub_dashboards_carry_simhubs_web_port()
    {
        var target = Create(Ready).Router.GetTarget("tablet")!;

        target.Dashboard.Should().Be("Clock");
        target.Port.Should().Be(8888);
        target.Notice.Should().BeNull();
    }

    [Fact]
    public void Starts_with_navigation_dash_not_installed()
    {
        Create().Router.Navigation.Should().Be(NavigationDashStatus.NotInstalled);
    }

    [Fact]
    public void A_ready_navigation_dash_is_framed_from_its_own_port()
    {
        var (router, _) = Create(Ready with { Port = 9100 });

        router.Observe(Ets2Driving, TimeSpan.Zero);

        router.GetTarget("tablet").Should().Be(new DeviceTarget(NavigationDash.Code, "/", 9100));
    }

    [Fact]
    public void Not_ready_shows_a_notice_with_its_reason_and_nothing_to_frame()
    {
        var (router, _) = Create(Loading);

        router.Observe(Ets2Driving, TimeSpan.Zero);

        router.GetTarget("tablet").Should().Be(new DeviceTarget(
            NavigationDash.Code, null, null, "DevDogs Navigation Dash is not ready.", "Waiting for map data"));
    }

    [Fact]
    public void Not_ready_without_a_reason_still_explains()
    {
        var (router, _) = Create(Loading with { Reason = null });

        router.Observe(Ets2Driving, TimeSpan.Zero);

        router.GetTarget("tablet")!.NoticeDetail.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Not_installed_says_so()
    {
        var (router, _) = Create();

        router.Observe(Ets2Driving, TimeSpan.Zero);

        var target = router.GetTarget("tablet")!;
        target.Path.Should().BeNull();
        target.Notice.Should().Be("DevDogs Navigation Dash is not installed in SimHub.");
    }

    [Fact]
    public void Pausing_and_menus_keep_the_map_when_it_is_chosen_for_both()
    {
        var (router, events) = Create(Ready);
        router.Observe(Ets2Driving, TimeSpan.Zero);
        var driving = router.GetTarget("tablet");
        events.Clear();

        router.Observe(Ets2Paused, TimeSpan.FromSeconds(1)).Should().BeTrue("paused counts as game idle");
        router.GetTarget("tablet").Should().BeSameAs(driving);
        router.Observe(Ets2Menu, TimeSpan.FromSeconds(2));
        router.GetTarget("tablet").Should().BeSameAs(driving);
        router.Observe(Ets2Driving, TimeSpan.FromSeconds(3)).Should().BeTrue();

        router.GetTarget("tablet").Should().BeSameAs(driving);
        events.Should().BeEmpty("the device frame must not reload");
    }

    [Fact]
    public void Turning_ready_replaces_the_notice_with_the_map()
    {
        var (router, events) = Create(Loading);
        router.Observe(Ets2Driving, TimeSpan.Zero);
        events.Clear();

        router.UpdateNavigation(Ready).Should().BeTrue();

        router.GetTarget("tablet")!.Path.Should().Be("/");
        events.Should().ContainSingle().Which.Should().Equal("tablet");
    }

    [Fact]
    public void An_unchanged_status_changes_nothing()
    {
        var (router, events) = Create(Ready);

        router.UpdateNavigation(Ready with { }).Should().BeFalse();

        events.Should().BeEmpty();
    }

    [Fact]
    public void A_new_reason_while_ready_keeps_the_map()
    {
        var (router, events) = Create(Ready);
        router.Observe(Ets2Driving, TimeSpan.Zero);
        var before = router.GetTarget("tablet");
        events.Clear();

        router.UpdateNavigation(Ready with { Reason = "Following route" }).Should().BeTrue();

        router.GetTarget("tablet").Should().BeSameAs(before);
        events.Should().BeEmpty();
    }

    [Fact]
    public void A_new_port_reloads_the_map()
    {
        var (router, _) = Create(Ready);
        router.Observe(Ets2Driving, TimeSpan.Zero);

        router.UpdateNavigation(Ready with { Port = 9100 });

        router.GetTarget("tablet")!.Port.Should().Be(9100);
    }

    [Fact]
    public void Navigation_dash_changes_leave_devices_that_do_not_use_it_alone()
    {
        var (router, events) = Create(Loading);

        router.UpdateNavigation(Ready).Should().BeTrue();

        events.Should().BeEmpty("the tablet shows Clock with no game running");
    }

    [Fact]
    public void Hiding_simhubs_controls_does_not_reload_the_map()
    {
        var (router, events) = Create(Ready);
        router.Observe(Ets2Driving, TimeSpan.Zero);
        var before = router.GetTarget("tablet");
        events.Clear();

        var settings = Settings();
        settings.Devices[0].HideControls = false;
        router.ApplySettings(settings);

        router.GetTarget("tablet").Should().BeSameAs(before);
        events.Should().BeEmpty();
    }

    [Fact]
    public void The_code_is_matched_ignoring_case()
    {
        var (router, _) = Create(Ready, Settings(gameIdle: "DevDogs:Navigation-Dash"));
        router.Observe(Ets2Driving, TimeSpan.Zero);
        var driving = router.GetTarget("tablet");

        router.Observe(Ets2Menu, TimeSpan.FromSeconds(1));

        router.GetTarget("tablet").Should().BeSameAs(driving);
    }

    [Fact]
    public void Switching_from_the_map_to_a_simhub_dashboard_and_back()
    {
        var (router, _) = Create(Ready, Settings(gameIdle: "Truck menu"));
        router.Observe(Ets2Driving, TimeSpan.Zero);

        router.Observe(Ets2Menu, TimeSpan.FromSeconds(1));
        router.GetTarget("tablet")!.Should().Match<DeviceTarget>(t => t.Dashboard == "Truck menu" && t.Port == 8888);

        router.Observe(Ets2Driving, TimeSpan.FromSeconds(2));
        router.GetTarget("tablet").Should().Be(new DeviceTarget(NavigationDash.Code, "/", 8893));
    }

    [Fact]
    public void A_new_simhub_web_port_rebuilds_simhub_dashboards()
    {
        var (router, events) = Create();
        var before = router.GetTarget("tablet")!;

        var settings = Settings();
        settings.SimHubWebPort = 8900;
        router.ApplySettings(settings);

        var after = router.GetTarget("tablet")!;
        after.Port.Should().Be(8900);
        after.Path.Should().NotBe(before.Path);
        events.Should().ContainSingle();
    }
}
