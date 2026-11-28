using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using System.Xml;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private void OpenComparisonStyles_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OpenTextEditor(ComparisonStyles.EnsureUserFile());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowError($"The comparison stylesheet could not be opened: {exception.Message}");
        }
    }

    private async void CompareXml_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CurrentFilePath is not { } baseline)
        {
            return;
        }

        var picker = new FileOpenPicker(XamlRoot.ContentIslandEnvironment.AppWindowId);
        foreach (var extension in new[] { ".xml", ".xsd", ".xsl", ".xslt", ".config" })
        {
            picker.FileTypeFilter.Add(extension);
        }
        if (await picker.PickSingleFileAsync() is not { } file)
        {
            return;
        }

        if (string.Equals(baseline, file.Path, StringComparison.OrdinalIgnoreCase))
        {
            ShowError("Choose a different XML file to compare.");
            return;
        }

        ViewModel.CommitSelectedValue();
        if (ViewModel.IsDirty)
        {
            var prompt = new ContentDialog
            {
                Title = "Compare XML files",
                Content = "The open document has unsaved changes. Choose which version to compare.",
                PrimaryButtonText = "Save and compare",
                SecondaryButtonText = "Compare saved version",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };
            var choice = await ShowDialogAsync(prompt);
            if (choice == ContentDialogResult.None)
            {
                return;
            }

            if (choice == ContentDialogResult.Primary && !await TrySaveAsync(baseline))
            {
                return;
            }
        }

        var options = new XmlComparisonDialog();
        if (await ShowDialogAsync(options) != ContentDialogResult.Primary)
        {
            return;
        }

        options.SaveOptions();
        try
        {
            var comparison = new XmlComparisonService(App.Settings);
            var result = await ViewModel.CompareAsync(comparison, baseline, file.Path);
            CloseError();
            if (result is not null)
            {
                await ShowDialogAsync(new ComparisonResultsDialog(result, XamlRoot));
            }
            else
            {
                ErrorInfoBar.Severity = InfoBarSeverity.Success;
                ErrorInfoBar.Title = "No differences found";
                ErrorInfoBar.Message = "The files match under the selected comparison options.";
                ErrorInfoBar.IsOpen = true;
            }
        }
        catch (Exception exception) when (exception is XmlException or IOException or UnauthorizedAccessException
            or InvalidOperationException or ArgumentException or System.Net.WebException or System.Net.Http.HttpRequestException)
        {
            ShowError($"The files could not be compared: {exception.Message}");
        }
    }
}
