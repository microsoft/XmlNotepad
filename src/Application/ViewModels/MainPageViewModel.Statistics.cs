using XmlNotepad.Services;

namespace XmlNotepad.ViewModels;

public sealed partial class MainPageViewModel
{
    internal async Task<XmlStatistics> CollectStatisticsAsync()
    {
        CommitSelectedValue();
        var document = Document;
        IsBusy = true;
        try
        {
            return await Task.Run(() => XmlStatistics.Collect(document));
        }
        finally
        {
            IsBusy = false;
        }
    }
}
