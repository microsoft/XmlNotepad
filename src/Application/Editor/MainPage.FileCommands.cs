using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private async void OpenUrl_Click(object sender, RoutedEventArgs e)
    {
        var address = new TextBox
        {
            Header = "Document URL",
            PlaceholderText = "https://"
        };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(address, "DocumentUrl");
        var dialog = new ContentDialog
        {
            Title = "Open from URL",
            Content = address,
            PrimaryButtonText = "Open",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            CornerRadius = new CornerRadius(8),
            IsPrimaryButtonEnabled = false
        };
        address.TextChanged += (_, _) =>
            dialog.IsPrimaryButtonEnabled = RemoteDocument.TryGetUri(address.Text.Trim(), out _);
        if (await ShowDialogAsync(dialog) == ContentDialogResult.Primary &&
            RemoteDocument.TryGetUri(address.Text.Trim(), out var location) && await ConfirmSaveChangesAsync())
        {
            await TryOpenAsync(location.AbsoluteUri);
        }
    }

    private async void Options_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new EditorOptionsDialog(ActualTheme == ElementTheme.Dark ? ColorTheme.Dark : ColorTheme.Light);
        if (await ShowDialogAsync(dialog) == ContentDialogResult.Primary)
        {
            App.ApplyThemeToWindows();
            InvalidateSchemaDocumentation();
            ConfigureDiagnosticsPane();
            UpdateCheckSchedule();
        }
    }

    private async void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmSaveChangesAsync())
        {
            return;
        }

        try
        {
            var path = App.Settings.FileName;
            if (!File.Exists(path))
            {
                App.SettingsService.Save();
            }
            await TryOpenAsync(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowError($"The settings file could not be opened: {exception.Message}");
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _window?.RequestClose();
    }

    private void NewWindow_Click(object sender, RoutedEventArgs e) =>
        App.OpenDocumentWindow(ViewModel.CurrentDocumentLocation);

    private bool _checkingDisk;
    private (string Path, DocumentFileVersion? Baseline, DocumentFileVersion? Observed)? _lastFileNotice;

    private void StartFileMonitoring()
    {
        var timer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(2);
        timer.Tick += CheckFileOnDisk;
        Unloaded += (_, _) => timer.Stop();
        timer.Start();
    }

    private async void CheckFileOnDisk(DispatcherQueueTimer sender, object args)
    {
        if (_checkingDisk || ViewModel.IsBusy)
        {
            return;
        }

        if (ViewModel.CurrentFilePath is not { } path)
        {
            ClearFileNotice();
            return;
        }

        _checkingDisk = true;
        var baseline = ViewModel.CurrentFileVersion;
        var document = ViewModel.Document;
        try
        {
            var observed = await Task.Run(() => DocumentFileVersion.Read(path));
            if (!IsLoaded || ViewModel.IsBusy || document != ViewModel.Document ||
                ViewModel.CurrentFilePath != path || ViewModel.CurrentFileVersion != baseline)
            {
                return;
            }

            if (observed == baseline)
            {
                ClearFileNotice();
                return;
            }

            var notice = (path, baseline, observed);
            if (_lastFileNotice == notice)
            {
                return;
            }

            _lastFileNotice = notice;
            FileChangeInfoBar.Title = observed is null ? "File unavailable" : "File changed on disk";
            FileChangeInfoBar.Message = Path.GetFileName(path);
            FileChangeInfoBar.IsOpen = true;

            if (observed is not null && !App.SettingsService.Settings.GetBoolean("PromptOnReload") &&
                VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Count == 0)
            {
                ViewModel.CommitSelectedValue();
                if (!ViewModel.IsDirty)
                {
                    await ReloadDocumentAsync(path);
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A file can disappear or be replaced between reading its metadata fields.
        }
        finally
        {
            _checkingDisk = false;
        }
    }

    private async void ReloadFromDisk_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentFilePath is not { } path)
        {
            return;
        }

        ViewModel.CommitSelectedValue();
        if (ViewModel.IsDirty)
        {
            var dialog = new ContentDialog
            {
                Title = "Reload from disk?",
                Content = $"Reloading {ViewModel.FileName} will discard unsaved changes.",
                PrimaryButtonText = "Reload",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                CornerRadius = new CornerRadius(8)
            };
            if (await ShowDialogAsync(dialog) != ContentDialogResult.Primary)
            {
                return;
            }
        }

        await ReloadDocumentAsync(path);
    }

    private async Task ReloadDocumentAsync(string path)
    {
        var selected = ViewModel.SelectedNode is { IsPlaceholder: false } item ? item.Node : null;
        var namespaces = selected is null ? null : XmlHelpers.GetNamespaceScope(selected);
        var selectionPath = selected is null ? null : XmlHelpers.GetXPathLocation(selected, namespaces);

        if (await TryOpenAsync(path))
        {
            ClearFileNotice();
            if (!string.IsNullOrEmpty(selectionPath) && namespaces is not null &&
                ViewModel.Document.SelectSingleNode(selectionPath, namespaces) is { } match)
            {
                ViewModel.SelectNode(match);
                if (ViewModel.SelectedNode is { } selection)
                {
                    RevealTreeItem(selection);
                }
            }
        }
    }

    private void ClearFileNotice()
    {
        _lastFileNotice = null;
        FileChangeInfoBar.IsOpen = false;
    }

    private async void OpenInTextEditor_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmSaveChangesAsync() || ViewModel.CurrentFilePath is not { } path)
        {
            return;
        }
        OpenTextEditor(path);
    }

    private void ShowOpenError(string path, string summary, Exception exception)
    {
        ShowError($"{summary}: {exception.Message}");
        if (File.Exists(path))
        {
            OpenFailedFileButton.Tag = Path.GetFullPath(path);
            OpenFailedFileButton.Visibility = Visibility.Visible;
        }
    }

    private void OpenFailedFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string path })
        {
            OpenTextEditor(path);
        }
    }

    private async Task<bool> ConfirmSaveDestinationAsync(string path, DocumentSaveTarget destination, bool changedOnDisk)
    {
        var name = Path.GetFileName(path);
        var (title, message, action) = (changedOnDisk, destination.Version is not null, destination.IsReadOnly) switch
        {
            (true, false, _) => ("File no longer on disk", $"Recreate {name} with your edits?", "Recreate file"),
            (true, true, true) => ("Read-only file changed on disk", $"Make {name} writable and replace the newer version with your edits?", "Make writable and replace"),
            (true, true, false) => ("File changed on disk", $"Replace the newer version of {name} with your edits?", "Replace file"),
            _ => ("This file is read-only", $"Make {name} writable and save your changes?", "Make writable and save")
        };
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = action,
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            CornerRadius = new CornerRadius(8)
        };
        return await ShowDialogAsync(dialog) == ContentDialogResult.Primary;
    }

    private void OpenTextEditor(string path)
    {
        var editor = App.SettingsService.Settings.GetString("TextEditor");
        if (!File.Exists(editor))
        {
            editor = Path.Combine(Environment.SystemDirectory, "notepad.exe");
        }

        try
        {
            var startInfo = new ProcessStartInfo(editor) { UseShellExecute = false };
            startInfo.ArgumentList.Add(path);
            using var process = Process.Start(startInfo);
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or UnauthorizedAccessException)
        {
            ShowError($"The text editor could not be opened: {exception.Message}");
        }
    }
}
