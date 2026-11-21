using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private static string SamplesDirectory => Path.Combine(ApplicationFiles.ContentDirectory, "Samples");

    private async void OpenSample_Click(object sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(SamplesDirectory))
        {
            ShowError("The bundled XML Notepad samples could not be found.");
            return;
        }

        if (!await ConfirmSaveChangesAsync())
        {
            return;
        }

        var dialog = new OpenSampleDialog(SamplesDirectory);
        if (await ShowDialogAsync(dialog) == ContentDialogResult.Primary &&
            dialog.SelectedFileName is { } fileName)
        {
            await TryOpenAsync(Path.Combine(SamplesDirectory, fileName));
        }
    }

    private static bool IsBundledSample(string path) =>
        string.Equals(Path.GetDirectoryName(path), SamplesDirectory, StringComparison.OrdinalIgnoreCase);

    private async void Help_Click(object sender, RoutedEventArgs e) => await OpenDocumentationAsync();

    private async void Help_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        e.Handled = true;
        await OpenDocumentationAsync();
    }

    private async Task OpenDocumentationAsync()
    {
        if (!await Launcher.LaunchUriAsync(App.DocumentationUri))
        {
            ShowError("The XML Notepad documentation could not be opened in your browser.");
        }
    }

    private async void About_Click(object sender, RoutedEventArgs e) => await ShowDialogAsync(new AboutDialog());

    private async void FileAssociations_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var executable = App.Settings.ExecutablePath;
            var settingsPage = ShellFileAssociations.FindSettingsPage(executable);
            if (settingsPage is null)
            {
                var dialog = new ContentDialog
                {
                    Title = "File associations",
                    Content = new TextBlock
                    {
                        Text = "Add this portable copy to Windows’ Open with list for XML, XSD, XSL, and XSLT files.",
                        TextWrapping = TextWrapping.Wrap
                    },
                    PrimaryButtonText = "Register and open Settings",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close
                };
                if (await ShowDialogAsync(dialog) != ContentDialogResult.Primary)
                {
                    return;
                }
                settingsPage = ShellFileAssociations.RegisterPortable(executable);
            }
            if (!await Launcher.LaunchUriAsync(settingsPage) && !await Launcher.LaunchUriAsync(ShellFileAssociations.DefaultApps))
            {
                ShowError("Windows Default Apps settings could not be opened.");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException or
            System.Runtime.InteropServices.COMException)
        {
            ShowError($"File associations could not be configured: {exception.Message}");
        }
    }
}
