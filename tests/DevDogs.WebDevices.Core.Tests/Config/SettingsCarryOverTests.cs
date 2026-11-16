using System;
using System.IO;
using DevDogs.WebDevices.Core.Config;
using FluentAssertions;
using Xunit;

namespace DevDogs.WebDevices.Core.Tests.Config;

public sealed class SettingsCarryOverTests : IDisposable
{
    private readonly string _folder = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), $"SettingsCarryOver_{Guid.NewGuid():N}")).FullName;

    private string Previous => Path.Combine(_folder, "WebDevicesPlugin.WebDevicesSettings.json");

    private string Current => Path.Combine(_folder, "DDWebDevicesPlugin.WebDevicesSettings.json");

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void Copies_the_previous_file_when_only_it_exists_and_keeps_it()
    {
        File.WriteAllText(Previous, """{ "Devices": [ { "Name": "ipad-left" } ] }""");

        SettingsCarryOver.CopyIfMissing(Previous, Current).Should().BeTrue();

        File.ReadAllText(Current).Should().Be("""{ "Devices": [ { "Name": "ipad-left" } ] }""");
        File.Exists(Previous).Should().BeTrue();
    }

    [Fact]
    public void Does_nothing_when_neither_file_exists()
    {
        SettingsCarryOver.CopyIfMissing(Previous, Current).Should().BeFalse();

        File.Exists(Current).Should().BeFalse();
    }

    [Fact]
    public void Leaves_the_current_file_alone_when_only_it_exists()
    {
        File.WriteAllText(Current, "current");

        SettingsCarryOver.CopyIfMissing(Previous, Current).Should().BeFalse();

        File.ReadAllText(Current).Should().Be("current");
        File.Exists(Previous).Should().BeFalse();
    }

    [Fact]
    public void The_current_file_wins_when_both_exist()
    {
        File.WriteAllText(Previous, "previous");
        File.WriteAllText(Current, "current");

        SettingsCarryOver.CopyIfMissing(Previous, Current).Should().BeFalse();

        File.ReadAllText(Current).Should().Be("current");
        File.ReadAllText(Previous).Should().Be("previous");
    }

    [Fact]
    public void A_failed_copy_throws_rather_than_reporting_success()
    {
        File.WriteAllText(Previous, "previous");
        var unreachable = Path.Combine(_folder, "missing-folder", "DDWebDevicesPlugin.WebDevicesSettings.json");

        var copy = () => SettingsCarryOver.CopyIfMissing(Previous, unreachable);

        copy.Should().Throw<IOException>();
    }
}
