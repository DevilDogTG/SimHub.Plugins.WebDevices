using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DevDogs.WebDevices.Core.Editing;

namespace DevDogs.WebDevices.UI;

/// <summary>
/// A dashboard field like SimHub's own picker: preview and title when closed; a searchable list grouped by
/// category when open. It edits <see cref="SelectedCode"/>, the stored dashboard code; what is listed and how a
/// code is shown come from <see cref="Choices"/>. The DataContext is left to the parent, so bindings on
/// <see cref="SelectedCode"/> reach the device editor.
/// </summary>
public partial class DashboardPicker : UserControl
{
    /// <summary>Identifies the <see cref="SelectedCode"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedCodeProperty = DependencyProperty.Register(
        nameof(SelectedCode),
        typeof(string),
        typeof(DashboardPicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((DashboardPicker)d).Describe()));

    /// <summary>Identifies the <see cref="Choices"/> dependency property.</summary>
    public static readonly DependencyProperty ChoicesProperty = DependencyProperty.Register(
        nameof(Choices),
        typeof(DashboardChoices),
        typeof(DashboardPicker),
        new PropertyMetadata(null, (d, _) => ((DashboardPicker)d).Describe()));

    /// <summary>Identifies the <see cref="EmptyText"/> dependency property.</summary>
    public static readonly DependencyProperty EmptyTextProperty = DependencyProperty.Register(
        nameof(EmptyText),
        typeof(string),
        typeof(DashboardPicker),
        new PropertyMetadata("None"));

    private static readonly DependencyPropertyKey CurrentPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Current),
        typeof(DashboardChoice),
        typeof(DashboardPicker),
        new PropertyMetadata(null));

    private static readonly DependencyPropertyKey NoteTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(NoteText),
        typeof(string),
        typeof(DashboardPicker),
        new PropertyMetadata(""));

    /// <summary>Identifies the <see cref="Current"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentProperty = CurrentPropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="NoteText"/> dependency property.</summary>
    public static readonly DependencyProperty NoteTextProperty = NoteTextPropertyKey.DependencyProperty;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardPicker"/> class.
    /// </summary>
    public DashboardPicker()
    {
        InitializeComponent();
    }

    /// <summary>Gets or sets the stored dashboard code; <see langword="null"/> when the field is empty.</summary>
    public string? SelectedCode
    {
        get => (string?)GetValue(SelectedCodeProperty);
        set => SetValue(SelectedCodeProperty, value);
    }

    /// <summary>Gets or sets what the list offers and how a code is shown.</summary>
    public DashboardChoices? Choices
    {
        get => (DashboardChoices?)GetValue(ChoicesProperty);
        set => SetValue(ChoicesProperty, value);
    }

    /// <summary>Gets or sets the dimmed text of an empty field: what the device shows instead.</summary>
    public string EmptyText
    {
        get => (string)GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    /// <summary>Gets the entry shown in the closed field, or <see langword="null"/> when it is empty.</summary>
    public DashboardChoice? Current => (DashboardChoice?)GetValue(CurrentProperty);

    /// <summary>Gets the note after the title, such as " · not installed", or an empty text.</summary>
    public string NoteText => (string)GetValue(NoteTextProperty);

    private void Describe()
    {
        var current = Choices?.Describe(SelectedCode);
        SetValue(CurrentPropertyKey, current);
        SetValue(NoteTextPropertyKey, current?.Note is { } note ? "  · " + note : "");
        ClearButton.Visibility = current is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Popup_Opened(object sender, EventArgs e)
    {
        // A click on the field while the list is open only closes it: without this the click reopens it.
        Toggle.IsHitTestVisible = false;
        Panel.Width = Math.Max(ActualWidth, 400);
        SearchBox.Text = "";
        ShowMatches();
        if (Current is { } current && List.Items.Contains(current))
        {
            List.SelectedItem = current;
            Reveal(current);
        }

        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => Keyboard.Focus(SearchBox)));
    }

    private void Popup_Closed(object sender, EventArgs e)
    {
        Toggle.IsHitTestVisible = true;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!Popup.IsOpen)
        {
            return;
        }

        ShowMatches();
        if (List.Items.Count > 0)
        {
            List.SelectedIndex = 0;
        }
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                Move(1);
                break;
            case Key.Up:
                Move(-1);
                break;
            case Key.PageDown:
                Move(5);
                break;
            case Key.PageUp:
                Move(-5);
                break;
            case Key.Enter:
                if (List.SelectedItem is DashboardChoice choice)
                {
                    Choose(choice);
                }

                break;
            case Key.Escape:
                Popup.IsOpen = false;
                Toggle.Focus();
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void List_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source
            && ItemsControl.ContainerFromElement(List, source) is ListBoxItem { DataContext: DashboardChoice choice })
        {
            Choose(choice);
        }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        SelectedCode = null;
    }

    private void ShowMatches()
    {
        var view = new ListCollectionView((Choices?.Search(SearchBox.Text) ?? []).ToList());
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(DashboardChoice.Category)));
        List.ItemsSource = view;
        Scroller()?.ScrollToTop();
        NoMatch.Visibility = view.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Move(int step)
    {
        if (List.Items.Count == 0)
        {
            return;
        }

        var index = List.SelectedIndex < 0 && step < 0 ? List.Items.Count : List.SelectedIndex;
        List.SelectedIndex = Math.Max(0, Math.Min(List.Items.Count - 1, index + step));
        if (List.SelectedIndex == 0)
        {
            Scroller()?.ScrollToTop();
        }
        else
        {
            List.ScrollIntoView(List.SelectedItem);
        }
    }

    /// <summary>
    /// Scrolls an entry into view from the top of the list, so an entry near the top keeps its group header in
    /// view (scrolling to it directly would put the entry itself at the top, above which the header is hidden).
    /// </summary>
    private void Reveal(DashboardChoice choice)
    {
        Scroller()?.ScrollToTop();
        List.UpdateLayout();
        List.ScrollIntoView(choice);
    }

    private ScrollViewer? Scroller() => Find<ScrollViewer>(List);

    private static T? Find<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            var match = child as T ?? Find<T>(child);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private void Choose(DashboardChoice choice)
    {
        SelectedCode = choice.Code;
        Popup.IsOpen = false;
        Toggle.Focus();
    }
}
