using System.Xml;
using System.Xml.Schema;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private async void GoToDefinition_Click(object sender, RoutedEventArgs e)
    {
        EndInlineValueEdit(commit: true);
        ViewModel.CommitSelectedValue();
        if (ViewModel.SelectedNode is not { IsPlaceholder: false } selected)
        {
            return;
        }

        try
        {
            var target = ViewModel.GetDefinition(selected.Node);
            if (target?.Node is { } node)
            {
                ViewModel.SelectNode(node);
                if (ViewModel.SelectedNode is { } item)
                {
                    RevealTreeItem(item);
                    XmlTree.Focus(FocusState.Programmatic);
                }
            }
            else if (target?.Location is { } location)
            {
                if (location.IsFile && !File.Exists(location.LocalPath))
                {
                    if (!target.IsInclude)
                    {
                        throw new FileNotFoundException($"The definition file was not found: {location.LocalPath}");
                    }
                    if (!await CreateMissingIncludeAsync(location.LocalPath))
                    {
                        return;
                    }
                }
                App.OpenDocumentWindow(location.IsFile ? location.LocalPath : location.AbsoluteUri, target.Line, target.Column);
            }
            else if (ValidationIssues.ItemsSource is null)
            {
                ClearValidationIssues();
                ErrorInfoBar.Severity = InfoBarSeverity.Informational;
                ErrorInfoBar.Title = "No definition found";
                ErrorInfoBar.Message = string.Empty;
                ErrorInfoBar.IsOpen = true;
            }
        }
        catch (Exception exception) when (exception is XmlException or XmlSchemaException or IOException or
            UnauthorizedAccessException or System.Net.WebException or HttpRequestException or ArgumentException)
        {
            ShowError($"Could not open the definition: {exception.Message}");
        }
    }

    private async Task<bool> CreateMissingIncludeAsync(string path)
    {
        var dialog = new ContentDialog
        {
            Title = "Create missing include?",
            Content = path,
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };
        if (await ShowDialogAsync(dialog) != ContentDialogResult.Primary)
        {
            return false;
        }

        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await stream.WriteAsync("<root />"u8.ToArray());
        return true;
    }
}
