using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private DispatcherQueueTimer? _statusTimer;

    private void InitializeStatusBar()
    {
        var timer = DispatcherQueue.CreateTimer();
        timer.IsRepeating = false;
        timer.Interval = TimeSpan.FromMilliseconds(70);
        timer.Tick += (_, _) => UpdateStatusBar();
        _statusTimer = timer;
        Unloaded += (_, _) => timer.Stop();
        if (App.ShowPointerPosition)
        {
            StatusBarToggle.IsEnabled = false;
            LayoutRoot.PointerMoved += (_, args) =>
            {
                var point = args.GetCurrentPoint(LayoutRoot).Position;
                StatusPosition.Text = $"Pointer X {point.X:0}, Y {point.Y:0}";
            };
        }
        ApplyStatusBarPreference();
    }

    private void ApplyStatusBarPreference()
    {
        StatusBarToggle.IsChecked = App.ShowPointerPosition || App.Settings.GetBoolean("ShowStatusBar", true);
        DocumentStatusBar.Visibility = StatusBarToggle.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        ScheduleStatusUpdate();
    }

    private void StatusBarToggle_Click(object sender, RoutedEventArgs e)
    {
        App.Settings["ShowStatusBar"] = StatusBarToggle.IsChecked;
        ApplyStatusBarPreference();
        App.SettingsService.Save();
    }

    private void ScheduleStatusUpdate()
    {
        _statusTimer?.Stop();
        if (DocumentStatusBar?.Visibility == Visibility.Visible)
        {
            _statusTimer?.Start();
        }
    }

    private void UpdateStatusBar()
    {
        if (ViewModel.SelectedNode is not { IsPlaceholder: false } item)
        {
            StatusXPath.Text = string.Empty;
            if (!App.ShowPointerPosition)
            {
                StatusPosition.Text = string.Empty;
            }
            ToolTipService.SetToolTip(StatusXPath, null);
            return;
        }

        var xpath = XmlHelpers.GetXPathLocation(item.Node, XmlHelpers.GetNamespaceScope(item.Node));
        StatusXPath.Text = string.IsNullOrEmpty(xpath) ? string.Empty : $"XPath {xpath}";
        ToolTipService.SetToolTip(StatusXPath, StatusXPath.Text);
        if (!App.ShowPointerPosition)
        {
            StatusPosition.Text = ViewModel.SelectedIndexedSourcePosition is { } position
                ? $"Line {position.Line}, column {position.Column}" : string.Empty;
        }
    }
}
