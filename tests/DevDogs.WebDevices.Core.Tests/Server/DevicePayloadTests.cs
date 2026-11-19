using DevDogs.WebDevices.Core.Routing;
using DevDogs.WebDevices.Core.Server;
using DevDogs.WebDevices.Core.State;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DevDogs.WebDevices.Core.Tests.Server;

public class DevicePayloadTests
{
    [Fact]
    public void Carries_everything_the_device_page_needs_on_one_line()
    {
        var json = DevicePayload.ToJson(
            "ipad",
            new RouterState(GameState.Driving, "ETS2"),
            new DeviceTarget("Truck", "/Dash?v=1#Truck"),
            simHubWebPort: 8888);

        json.Should().NotContain("\n");
        JObject.Parse(json).Should().BeEquivalentTo(JObject.Parse("""
            {
              "device": "ipad",
              "state": "Driving",
              "game": "ETS2",
              "dashboard": "Truck",
              "path": "/Dash?v=1#Truck",
              "port": 8888,
              "simHubPort": 8888,
              "notice": null,
              "noticeDetail": null
            }
            """));
    }

    [Fact]
    public void Missing_values_are_explicit_nulls()
    {
        var json = DevicePayload.ToJson("ipad", RouterState.SystemIdle, new DeviceTarget(null, null), 8888);

        var payload = JObject.Parse(json);
        payload["state"]!.Value<string>().Should().Be("SystemIdle");
        payload["game"]!.Type.Should().Be(JTokenType.Null);
        payload["dashboard"]!.Type.Should().Be(JTokenType.Null);
        payload["path"]!.Type.Should().Be(JTokenType.Null);
        payload["port"]!.Type.Should().Be(JTokenType.Null);
        payload["notice"]!.Type.Should().Be(JTokenType.Null);
    }

    [Fact]
    public void The_frame_port_is_the_targets_own()
    {
        var json = DevicePayload.ToJson(
            "tablet",
            new RouterState(GameState.Driving, "ETS2"),
            new DeviceTarget(NavigationDash.Code, "/", 8893),
            simHubWebPort: 8888);

        var payload = JObject.Parse(json);
        payload["path"]!.Value<string>().Should().Be("/");
        payload["port"]!.Value<int>().Should().Be(8893);
        payload["simHubPort"]!.Value<int>().Should().Be(8888, "the home-screen icon still comes from SimHub");
    }

    [Fact]
    public void Carries_a_notice_instead_of_a_page()
    {
        var json = DevicePayload.ToJson(
            "tablet",
            new RouterState(GameState.Driving, "ETS2"),
            new DeviceTarget(NavigationDash.Code, null, null, "DevDogs Navigation Dash is not ready.", "Waiting for map data"),
            simHubWebPort: 8888);

        var payload = JObject.Parse(json);
        payload["path"]!.Type.Should().Be(JTokenType.Null);
        payload["port"]!.Type.Should().Be(JTokenType.Null);
        payload["notice"]!.Value<string>().Should().Be("DevDogs Navigation Dash is not ready.");
        payload["noticeDetail"]!.Value<string>().Should().Be("Waiting for map data");
    }
}
