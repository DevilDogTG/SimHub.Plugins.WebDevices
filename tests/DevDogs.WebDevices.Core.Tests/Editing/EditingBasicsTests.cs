using System.Collections.Generic;
using DevDogs.WebDevices.Core.Editing;
using FluentAssertions;
using Xunit;

namespace DevDogs.WebDevices.Core.Tests.Editing;

public class EditingBasicsTests
{
    private sealed class Sample : ObservableObject
    {
        private string _name = "";

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
    }

    [Fact]
    public void SetProperty_raises_property_changed_only_for_a_new_value()
    {
        var sample = new Sample();
        var raised = new List<string?>();
        sample.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sample.Name = "ipad";
        sample.Name = "ipad";

        raised.Should().Equal("Name");
    }

    [Fact]
    public void RelayCommand_runs_its_action_when_allowed()
    {
        var runs = 0;
        var allowed = false;
        var command = new RelayCommand(() => runs++, () => allowed);

        command.CanExecute(null).Should().BeFalse();
        allowed = true;
        command.CanExecute(null).Should().BeTrue();
        command.Execute(null);

        runs.Should().Be(1);
    }

    [Fact]
    public void RelayCommand_announces_can_execute_changes()
    {
        var command = new RelayCommand(() => { });
        var raised = 0;
        command.CanExecuteChanged += (_, _) => raised++;

        command.RaiseCanExecuteChanged();

        raised.Should().Be(1);
    }

    [Fact]
    public void RelayCommand_of_T_passes_its_parameter()
    {
        string? received = null;
        var command = new RelayCommand<string>(value => received = value);

        command.Execute("ETS2");

        received.Should().Be("ETS2");
    }
}
