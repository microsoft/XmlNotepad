using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Windows.Storage.Pickers;
using System.Runtime.InteropServices;
using System.Text;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class ComparisonResultsDialog : ContentDialog
{
    private const string ChangeRows = """
        Array.from(document.querySelectorAll('tbody tr')).filter(
            row => row.querySelector('a[name], .add, .remove, .change, .movefrom, .moveto'))
        """;
    private readonly PreviewOutput _report;
    private string? _previewFile;
    private bool _closed;
    private bool _moving;
    private int _changeCount;
    private int _currentChange = -1;
    private bool _reportReady;

    public ComparisonResultsDialog(PreviewOutput report, XamlRoot root)
    {
        _report = report;
        InitializeComponent();
        XamlRoot = root;
        UpdateSize();
        root.Changed += Root_Changed;
        Opened += Dialog_Opened;
        ActualThemeChanged += Dialog_ThemeChanged;
        Closed += (_, _) =>
        {
            _closed = true;
            root.Changed -= Root_Changed;
            Report.Close();
            if (_previewFile is { } path)
            {
                DeletePreviewFile(path);
            }
        };
    }

    private void Root_Changed(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateSize();

    private void UpdateSize()
    {
        LayoutRoot.Width = Math.Clamp(XamlRoot.Size.Width - 96, 0, 1200);
        LayoutRoot.Height = Math.Clamp(XamlRoot.Size.Height - 170, 0, 700);
    }

    private async void Dialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        try
        {
            await Report.EnsureCoreWebView2Async();
            if (_closed)
            {
                return;
            }

            var core = Report.CoreWebView2;
            core.Settings.IsScriptEnabled = false;
            var content = _report.Content.Replace(
                "color-scheme: light dark;", $"color-scheme: {ReportColorScheme};", StringComparison.Ordinal);
            if (_report.OutputBytes.Length < 2 * 1024 * 1024)
            {
                Report.NavigateToString(content);
            }
            else
            {
                var path = Path.Combine(Path.GetTempPath(), $"XmlNotepad-comparison-{Guid.NewGuid():N}.html");
                _previewFile = path;
                await File.WriteAllTextAsync(path, content, new UTF8Encoding(true));
                if (_closed)
                {
                    DeletePreviewFile(path);
                }
                else
                {
                    Report.Source = new Uri(path);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or COMException)
        {
            ShowError($"The report could not be displayed: {exception.Message}");
        }
    }

    private async void Report_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (!args.IsSuccess || _closed)
        {
            return;
        }

        try
        {
            _reportReady = true;
            await ApplyReportThemeAsync();
            var count = await Report.ExecuteScriptAsync($"({ChangeRows}).length");
            int.TryParse(count, out _changeCount);
            UpdateNavigation();
        }
        catch (Exception exception) when (exception is InvalidOperationException or COMException)
        {
            ShowError($"Changes could not be indexed: {exception.Message}");
        }
    }

    private string ReportColorScheme => ActualTheme == ElementTheme.Dark ? "dark" : "light";

    private async void Dialog_ThemeChanged(FrameworkElement sender, object args)
    {
        if (_reportReady && !_closed)
        {
            await ApplyReportThemeAsync();
        }
    }

    private async Task ApplyReportThemeAsync()
    {
        try
        {
            await Report.ExecuteScriptAsync($"document.documentElement.style.colorScheme = '{ReportColorScheme}'");
        }
        catch (Exception exception) when (exception is InvalidOperationException or COMException)
        {
            ShowError($"The report theme could not be updated: {exception.Message}");
        }
    }

    private async void Previous_Click(object sender, RoutedEventArgs e) => await MoveToChangeAsync(_currentChange - 1);

    private async void Next_Click(object sender, RoutedEventArgs e) => await MoveToChangeAsync(_currentChange + 1);

    private async Task MoveToChangeAsync(int index)
    {
        if (_moving || _closed || index < 0 || index >= _changeCount)
        {
            return;
        }

        _moving = true;
        try
        {
            var moved = await Report.ExecuteScriptAsync($$"""
                (() => {
                    const row = ({{ChangeRows}})[{{index}}];
                    if (!row) return false;
                    document.querySelector('.current-change')?.classList.remove('current-change');
                    row.classList.add('current-change');
                    row.scrollIntoView({ block: 'center' });
                    return true;
                })()
                """);
            if (moved == "true")
            {
                _currentChange = index;
                UpdateNavigation();
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or COMException)
        {
            ShowError($"The change could not be displayed: {exception.Message}");
        }
        finally
        {
            _moving = false;
        }
    }

    private void UpdateNavigation()
    {
        if (_closed)
        {
            return;
        }

        PreviousButton.IsEnabled = _currentChange > 0;
        NextButton.IsEnabled = _currentChange + 1 < _changeCount;
        ChangePosition.Text = _currentChange < 0 ? string.Empty : $"Change {_currentChange + 1} of {_changeCount}";
    }

    private async void SaveReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileSavePicker(XamlRoot.ContentIslandEnvironment.AppWindowId)
            {
                SuggestedFileName = _report.SuggestedFileName,
                DefaultFileExtension = ".html"
            };
            picker.FileTypeChoices.Add("HTML report", new List<string> { ".html" });
            if (await picker.PickSaveFileAsync() is { } file)
            {
                await File.WriteAllBytesAsync(file.Path, _report.OutputBytes);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or COMException)
        {
            ShowError($"The report could not be saved: {exception.Message}");
        }
    }

    private void ShowError(string message)
    {
        if (!_closed)
        {
            ReportError.Message = message;
            ReportError.IsOpen = true;
        }
    }

    private static void DeletePreviewFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Debug.WriteLine(exception.Message);
        }
    }
}
