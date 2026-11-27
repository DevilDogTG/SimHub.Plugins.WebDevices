using DevDogs.WebDevices.Core.Routing;
using FluentAssertions;
using Xunit;

namespace DevDogs.WebDevices.Core.Tests.Routing;

public class NavigationDashTests
{
    [Fact]
    public void The_code_cannot_be_a_windows_folder_name()
    {
        NavigationDash.Code.Should().Contain(":");
    }

    [Theory]
    [InlineData("devdogs:navigation-dash", true)]
    [InlineData(" DevDogs:Navigation-Dash ", true)]
    [InlineData("DevDogs Navigation Dash", false)]
    [InlineData("Truck", false)]
    [InlineData(null, false)]
    public void Recognises_the_code_ignoring_case_and_spaces(string? value, bool expected)
    {
        NavigationDash.IsCode(value).Should().Be(expected);
    }

    [Fact]
    public void Missing_properties_mean_not_installed()
    {
        NavigationDashStatus.From(null, null, null).Should().Be(NavigationDashStatus.NotInstalled);
        NavigationDashStatus.NotInstalled.Should().Be(new NavigationDashStatus(false, false, 8893, null));
    }

    [Fact]
    public void Reads_ready_port_and_reason()
    {
        NavigationDashStatus.From("http://192.168.10.102:8893/", true, "Ready")
            .Should().Be(new NavigationDashStatus(true, true, 8893, "Ready"));
        NavigationDashStatus.From("http://192.168.10.102:9100/", false, " Waiting for map data ")
            .Should().Be(new NavigationDashStatus(true, false, 9100, "Waiting for map data"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData(42)]
    public void An_unreadable_address_falls_back_to_the_default_port(object? url)
    {
        NavigationDashStatus.From(url, false, null).Port.Should().Be(8893);
    }

    [Fact]
    public void Either_property_marks_it_installed()
    {
        NavigationDashStatus.From(null, false, null).Installed.Should().BeTrue();
        NavigationDashStatus.From("http://pc:8893/", null, null).Installed.Should().BeTrue();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData("True", true)]
    [InlineData("true ", true)]
    [InlineData(false, false)]
    [InlineData("yes", false)]
    [InlineData(1, false)]
    [InlineData(null, false)]
    public void Ready_only_from_a_true_flag(object? ready, bool expected)
    {
        NavigationDashStatus.From("http://pc:8893/", ready, null).Ready.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_reason_is_null(string? status)
    {
        NavigationDashStatus.From("http://pc:8893/", false, status).Reason.Should().BeNull();
    }
}
