using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
    {
        dialog.XamlRoot = XamlRoot;
        dialog.RequestedTheme = ActualTheme;
        ActualThemeChanged += UpdateDialogTheme;
        try
        {
            return await dialog.ShowAsync();
        }
        finally
        {
            ActualThemeChanged -= UpdateDialogTheme;
        }

        void UpdateDialogTheme(FrameworkElement sender, object args) => dialog.RequestedTheme = sender.ActualTheme;
    }
}
