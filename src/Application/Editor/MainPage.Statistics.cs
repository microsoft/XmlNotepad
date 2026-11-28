using Microsoft.UI.Xaml;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private async void XmlStatistics_Click(object sender, RoutedEventArgs e)
    {
        EndInlineValueEdit(commit: true);
        var stats = await ViewModel.CollectStatisticsAsync();
        if (IsLoaded)
        {
            await ShowDialogAsync(new XmlStatisticsDialog(stats, ViewModel.FileName, XamlRoot));
        }
    }
}
