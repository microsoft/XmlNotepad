using System.Xml;
using System.Xml.Xsl;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private CancellationTokenSource? _xsltCancellation;

    private async void ViewXsltResult_Click(object sender, RoutedEventArgs e) =>
        await RunXsltAsync(_xsltService.GetLinkedStylesheetPath(ViewModel.Document));

    private async void TransformWithXslt_Click(object sender, RoutedEventArgs e)
    {
        var baseUri = Uri.TryCreate(ViewModel.CurrentDocumentLocation, UriKind.Absolute, out var current)
            ? current : RemoteDocument.GetBaseUri(null, null);
        var dialog = new StylesheetLocationDialog(baseUri, App.SettingsService.RecentStylesheets);
        if (await ShowDialogAsync(dialog) == ContentDialogResult.Primary && dialog.Location is { } location)
        {
            await RunXsltAsync(location.AbsoluteUri, explicitlySelected: true);
        }
    }

    private async Task RunXsltAsync(string? stylesheetPath, bool explicitlySelected = false)
    {
        ViewModel.CommitSelectedValue();
        try
        {
            var theme = ActualTheme == ElementTheme.Dark ? ColorTheme.Dark : ColorTheme.Light;
            var result = await PreviewXsltAsync(stylesheetPath, explicitlySelected, theme);
            if (result is not null)
            {
                CloseError();
                await ShowDialogAsync(new OutputPreviewDialog(result, XamlRoot));
                if (explicitlySelected && Uri.TryCreate(stylesheetPath, UriKind.Absolute, out var location))
                {
                    App.SettingsService.AddRecentStylesheet(location);
                }
            }
        }
        catch (Exception exception) when (exception is XmlException or XsltException or IOException or UnauthorizedAccessException
            or System.Net.WebException or System.Net.Http.HttpRequestException or System.Security.SecurityException
            or InvalidOperationException or ArgumentException)
        {
            ShowError($"XSLT transformation failed: {exception.Message}");
        }
    }

    private async Task<PreviewOutput?> PreviewXsltAsync(string? stylesheetPath, bool explicitlySelected, ColorTheme theme)
    {
        try
        {
            return await TransformWithProgressAsync(stylesheetPath, explicitlySelected, theme);
        }
        catch (ScriptedStylesheetException exception) when (exception.TrustedEntryPoint is { } entryPoint)
        {
            var paths = entryPoint.Equals(exception.Location)
                ? entryPoint.LocalPath
                : $"{entryPoint.LocalPath}\n\nScript: {exception.Location.LocalPath}";
            var dialog = new ContentDialog
            {
                Title = "Run stylesheet script?",
                Content = new TextBlock
                {
                    Text = $"The stylesheet below and its dependencies can run code with your Windows account's permissions. " +
                           $"Only continue if you trust these files.\n\n{paths}",
                    TextWrapping = TextWrapping.Wrap
                },
                PrimaryButtonText = "Run script",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                CornerRadius = new CornerRadius(8)
            };
            if (await ShowDialogAsync(dialog) == ContentDialogResult.Primary)
            {
                return await TransformWithProgressAsync(stylesheetPath, explicitlySelected, theme, entryPoint);
            }
            return null;
        }
    }

    private async Task<PreviewOutput?> TransformWithProgressAsync(string? stylesheetPath, bool explicitlySelected,
        ColorTheme theme, Uri? trustedStylesheet = null)
    {
        using var cancellation = new CancellationTokenSource();
        _xsltCancellation = cancellation;
        ContentDialog? progress = null;
        Task<ContentDialogResult>? showing = null;
        try
        {
            var operation = ViewModel.TransformAsync(_xsltService, stylesheetPath, explicitlySelected, theme,
                trustedStylesheet, cancellation.Token);
            await Task.WhenAny(operation, Task.Delay(350));
            if (!operation.IsCompleted)
            {
                var label = new TextBlock { Text = "Transforming XML…" };
                progress = new ContentDialog
                {
                    Title = "XSLT transformation",
                    Content = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 12,
                        Children = { new ProgressRing { IsActive = true, Width = 24, Height = 24 }, label }
                    },
                    CloseButtonText = "Cancel",
                    CornerRadius = new CornerRadius(8)
                };
                progress.Closing += (_, args) =>
                {
                    if (!operation.IsCompleted)
                    {
                        args.Cancel = true;
                        label.Text = "Cancelling…";
                        cancellation.Cancel();
                    }
                };
                showing = ShowDialogAsync(progress);
            }

            var result = await operation;
            return cancellation.IsCancellationRequested ? null : result;
        }
        catch (Exception) when (cancellation.IsCancellationRequested)
        {
            return null;
        }
        finally
        {
            try
            {
                if (showing is not null)
                {
                    progress!.Hide();
                    await showing;
                }
            }
            finally
            {
                _xsltCancellation = null;
            }
        }
    }
}
