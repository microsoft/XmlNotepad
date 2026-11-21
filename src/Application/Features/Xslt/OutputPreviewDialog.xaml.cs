using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class OutputPreviewDialog : ContentDialog
{
    private readonly PreviewOutput _result;
    private bool _closed;

    public OutputPreviewDialog(PreviewOutput result, XamlRoot root)
    {
        _result = result;
        InitializeComponent();
        XamlRoot = root;
        UpdateSize();
        OutputFileName.Text = result.SuggestedFileName;
        if (result.FileExtension != ".html")
        {
            TextPreview.Text = result.Content;
            TextPreview.Visibility = Visibility.Visible;
        }
        else
        {
            HtmlPreview.Visibility = Visibility.Visible;
        }
        Opened += Dialog_Opened;
        Closed += Dialog_Closed;
    }

    private async void Dialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        XamlRoot.Changed += Root_Changed;
        if (_result.FileExtension != ".html")
        {
            return;
        }

        try
        {
            await Preview.EnsureCoreWebView2Async();
            if (!_closed)
            {
                Preview.CoreWebView2.Settings.IsScriptEnabled = false;
                Preview.NavigateToString(_result.Content);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or COMException)
        {
            ShowError($"The output could not be displayed: {exception.Message}");
        }
    }

    private void Dialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        _closed = true;
        XamlRoot.Changed -= Root_Changed;
        if (HtmlPreview.Visibility == Visibility.Visible)
        {
            Preview.Close();
        }
    }

    private void Root_Changed(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateSize();

    private void UpdateSize()
    {
        LayoutRoot.Width = Math.Clamp(XamlRoot.Size.Width - 96, 0, 1060);
        LayoutRoot.Height = Math.Clamp(XamlRoot.Size.Height - 180, 0, 660);
    }

    private async void SaveOutput_Click(object sender, RoutedEventArgs e)
    {
        SaveOutputButton.IsEnabled = false;
        try
        {
            var picker = new FileSavePicker(XamlRoot.ContentIslandEnvironment.AppWindowId)
            {
                SuggestedFileName = _result.SuggestedFileName,
                DefaultFileExtension = _result.FileExtension
            };
            picker.FileTypeChoices.Add("Transformation output", _result.FileExtension == ".html"
                ? new List<string> { ".html", ".htm" }
                : new List<string> { _result.FileExtension });
            if (await picker.PickSaveFileAsync() is { } file)
            {
                await File.WriteAllBytesAsync(file.Path, _result.OutputBytes);
                PreviewError.IsOpen = false;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or COMException)
        {
            ShowError($"The output could not be saved: {exception.Message}");
        }
        finally
        {
            SaveOutputButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        if (!_closed)
        {
            PreviewError.Title = "Preview error";
            PreviewError.Message = message;
            PreviewError.IsOpen = true;
        }
    }
}
