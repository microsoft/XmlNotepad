using System.Xml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using Newtonsoft.Json;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private async Task<bool> SaveAsAsync()
    {
        var picker = new FileSavePicker(XamlRoot.ContentIslandEnvironment.AppWindowId)
        {
            SuggestedFileName = ViewModel.FileName == "Untitled" ? "NewDocument.xml" : ViewModel.FileName,
            DefaultFileExtension = ".xml"
        };
        picker.FileTypeChoices.Add("XML document", new List<string> { ".xml" });

        var file = await picker.PickSaveFileAsync();
        return file is not null && await TrySaveAsync(file.Path);
    }

    private async Task<bool> TryOpenAsync(string path)
    {
        try
        {
            using var remote = RemoteDocument.TryGetUri(path, out var url) ? await ViewModel.DownloadAsync(url) : null;
            var extension = remote?.Extension ?? Path.GetExtension(path).ToLowerInvariant();
            switch (extension)
            {
                case ".csv":
                    var csv = remote is null ? await File.ReadAllTextAsync(path) : await remote.ReadTextAsync();
                    if (!await TryImportCsvAsync(path, csv, remote?.Location))
                    {
                        return false;
                    }
                    break;
                case ".json":
                    var json = remote is null ? await File.ReadAllTextAsync(path) : await remote.ReadTextAsync();
                    await ViewModel.ImportAsync(path, () => JsonConvert.DeserializeXmlNode(json, "root")
                        ?? throw new JsonSerializationException("The file does not contain a JSON document."), remote?.Location);
                    break;
                case ".htm":
                case ".html":
                    if (remote is null)
                    {
                        await ViewModel.ImportAsync(path, () => HtmlImport.Load(path));
                    }
                    else
                    {
                        var html = await remote.ReadTextAsync();
                        await ViewModel.ImportAsync(path, () =>
                        {
                            using var input = new StringReader(html);
                            return HtmlImport.Load(input);
                        }, remote.Location);
                    }
                    break;
                default:
                    if (remote is null)
                    {
                        await ViewModel.OpenAsync(path);
                    }
                    else
                    {
                        await ViewModel.OpenAsync(remote);
                    }
                    break;
            }
            CloseError();
            RememberDocument(path);
            return true;
        }
        catch (XmlException exception)
        {
            ShowOpenError(path, "The XML document could not be loaded", exception);
        }
        catch (JsonException exception)
        {
            ShowOpenError(path, "The JSON document could not be imported", exception);
        }
        catch (HttpRequestException exception)
        {
            ShowError($"The document could not be downloaded: {exception.Message}");
        }
        catch (OperationCanceledException)
        {
            ShowError("The download timed out.");
        }
        catch (IOException exception)
        {
            ShowError($"The file could not be opened: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            ShowError($"Access to the file was denied: {exception.Message}");
        }
        catch (ArgumentException exception)
        {
            ShowError($"The document path is invalid: {exception.Message}");
        }

        return false;
    }

    private async Task<bool> TryImportCsvAsync(string path, string text, Uri? remoteSource)
    {
        var dialog = new CsvImportDialog(text);
        if (await ShowDialogAsync(dialog) != ContentDialogResult.Primary)
        {
            return false;
        }

        var delimiter = dialog.Delimiter;
        var firstRowIsHeader = dialog.FirstRowIsHeader;
        var location = remoteSource ?? new Uri(Path.GetFullPath(path));
        await ViewModel.ImportAsync(path, () => CsvImport.Convert(location, text, delimiter, firstRowIsHeader), remoteSource);
        return true;
    }

    private async Task<bool> TrySaveAsync(string path)
    {
        try
        {
            var destination = DocumentSaveTarget.Inspect(path);
            var changedOnDisk = string.Equals(Path.GetFullPath(path), ViewModel.CurrentFilePath, StringComparison.OrdinalIgnoreCase)
                && ViewModel.CurrentFileVersion != destination.Version;
            if ((changedOnDisk || destination.IsReadOnly) && !await ConfirmSaveDestinationAsync(path, destination, changedOnDisk))
            {
                return false;
            }
            await ViewModel.SaveAsync(path, destination);
            if (App.SettingsService.IsSettingsFile(Path.GetFullPath(path)))
            {
                App.Settings.Reload();
                App.ApplyThemeToWindows();
                ConfigureDiagnosticsPane();
                ApplyStatusBarPreference();
                UpdateCheckSchedule();
            }
            CloseError();
            RememberDocument(path);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException)
        {
            ShowError($"The document could not be saved: {exception.Message}");
            return false;
        }
    }

    private void RememberDocument(string path)
    {
        try
        {
            App.SettingsService.AddRecentFile(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowError($"Could not update the recent files list: {exception.Message}");
        }
        RefreshRecentFilesMenu();
    }

    private async Task<bool> ConfirmSaveChangesAsync()
    {
        ViewModel.CommitSelectedValue();
        if (!ViewModel.IsDirty)
        {
            return true;
        }

        var dialog = new ContentDialog
        {
            Title = "Save changes?",
            Content = $"Save changes to {ViewModel.FileName} before continuing?",
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Discard",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await ShowDialogAsync(dialog);
        if (result == ContentDialogResult.Primary)
        {
            return await Save_ClickAsync();
        }

        return result == ContentDialogResult.Secondary;
    }

    internal async Task<bool> ConfirmCloseAsync()
    {
        if (_xsltCancellation is { } transformation)
        {
            transformation.Cancel();
            return false;
        }
        var canClose = await ConfirmSaveChangesAsync();
        if (canClose)
        {
            FindReplaceBar.HideFind();
        }
        return canClose;
    }

    private Task<bool> Save_ClickAsync()
    {
        return ViewModel.CurrentFilePath is string path && !IsBundledSample(path)
            ? TrySaveAsync(path)
            : SaveAsAsync();
    }
}
