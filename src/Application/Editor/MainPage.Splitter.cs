using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private const double MinimumPaneWidth = 160;
    private const double ColumnSplitterWidth = 12;
    private const double TreeRowTrailingInset = 4;
    private const double DefaultDiagnosticsHeight = 156;
    private const double MinimumDiagnosticsHeight = 96;
    private const double MinimumEditorHeight = 160;
    private const double KeyboardResizeStep = 16;
    private double _preferredTreeWidth = App.SettingsService.Settings.GetInteger("TreeViewSize");
    private double _preferredDiagnosticsHeight = App.SettingsService.Settings.GetInteger("TaskListSize");

    private void EditorPanes_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateTreeWidth();

    private void PaneSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
        _preferredTreeWidth = ClampTreeWidth(TreeColumn.ActualWidth + e.HorizontalChange);
        UpdateTreeWidth();
    }

    private void PaneSplitter_DragCompleted(object sender, DragCompletedEventArgs e) => SaveTreeWidth();

    private void PaneSplitter_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Left:
                _preferredTreeWidth = ClampTreeWidth(TreeColumn.ActualWidth - KeyboardResizeStep);
                break;
            case VirtualKey.Right:
                _preferredTreeWidth = ClampTreeWidth(TreeColumn.ActualWidth + KeyboardResizeStep);
                break;
            case VirtualKey.Home:
                _preferredTreeWidth = 0;
                break;
            default:
                return;
        }

        e.Handled = true;
        UpdateTreeWidth();
        SaveTreeWidth();
    }

    private double ClampTreeWidth(double width)
    {
        var available = Math.Max(0, EditorPanes.ActualWidth - ColumnSplitterWidth);
        var minimum = Math.Min(MinimumPaneWidth, available / 2);
        return Math.Clamp(width, minimum, available - minimum);
    }

    private void UpdateTreeWidth()
    {
        var available = Math.Max(0, EditorPanes.ActualWidth - ColumnSplitterWidth);
        var treeWidth = ClampTreeWidth(_preferredTreeWidth > 0 ? _preferredTreeWidth : available * 0.4);
        TreeColumn.Width = new GridLength(treeWidth);
        ((Microsoft.UI.Xaml.Controls.ColumnDefinition)Resources["XmlValueColumnWidth"]).Width =
            new GridLength(Math.Max(0, available - treeWidth - TreeRowTrailingInset));
    }

    private void SaveTreeWidth() => App.SettingsService.Settings["TreeViewSize"] = (int)Math.Round(_preferredTreeWidth);

    private void Workspace_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateDiagnosticsHeight();

    private void DiagnosticsSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
        _preferredDiagnosticsHeight = ClampDiagnosticsHeight(DiagnosticsPane.ActualHeight - e.VerticalChange);
        UpdateDiagnosticsHeight();
    }

    private void DiagnosticsSplitter_DragCompleted(object sender, DragCompletedEventArgs e) => SaveDiagnosticsHeight();

    private void DiagnosticsSplitter_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Up:
                _preferredDiagnosticsHeight = ClampDiagnosticsHeight(DiagnosticsPane.ActualHeight + KeyboardResizeStep);
                break;
            case VirtualKey.Down:
                _preferredDiagnosticsHeight = ClampDiagnosticsHeight(DiagnosticsPane.ActualHeight - KeyboardResizeStep);
                break;
            case VirtualKey.Home:
                _preferredDiagnosticsHeight = 0;
                break;
            default:
                return;
        }

        e.Handled = true;
        UpdateDiagnosticsHeight();
        SaveDiagnosticsHeight();
    }

    private double ClampDiagnosticsHeight(double height)
    {
        var available = Math.Max(0, Workspace.ActualHeight - DiagnosticsSplitter.Height);
        var maximum = Math.Max(available - MinimumEditorHeight, available / 2);
        return Math.Clamp(height, Math.Min(MinimumDiagnosticsHeight, maximum), maximum);
    }

    private void UpdateDiagnosticsHeight()
    {
        if (DiagnosticsVisible && Workspace.ActualHeight > 0)
        {
            var preferred = _preferredDiagnosticsHeight > 0 ? _preferredDiagnosticsHeight : DefaultDiagnosticsHeight;
            DiagnosticsPane.Height = ClampDiagnosticsHeight(preferred);
        }
    }

    private void SaveDiagnosticsHeight() =>
        App.SettingsService.Settings["TaskListSize"] = (int)Math.Round(_preferredDiagnosticsHeight);
}
