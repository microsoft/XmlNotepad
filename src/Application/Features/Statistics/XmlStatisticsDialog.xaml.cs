using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class XmlStatisticsDialog : ContentDialog
{
    private readonly XmlStatistics _stats;

    internal XmlStatisticsDialog(XmlStatistics stats, string fileName, XamlRoot root)
    {
        _stats = stats;
        InitializeComponent();
        DocumentLabel.Text = fileName;
        SummaryRows.ItemsSource = stats.Summary;
        StatisticsList.ItemsSource = stats.Names;
        ResizeReport(root);
        root.Changed += Root_Changed;
        Closed += (_, _) => root.Changed -= Root_Changed;
    }

    private void Root_Changed(XamlRoot sender, XamlRootChangedEventArgs args) => ResizeReport(sender);

    private void ResizeReport(XamlRoot root)
    {
        ReportLayout.Width = Math.Clamp(root.Size.Width - 96, 0, 696);
        ReportLayout.Height = Math.Clamp(root.Size.Height - 180, 0, 544);
    }

    private void CopyReport_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        var text = new StringBuilder().AppendLine(DocumentLabel.Text).AppendLine("Item	Count	Characters");
        foreach (var row in _stats.Summary.Concat(_stats.Names))
        {
            text.Append(row.Name).Append('\t').Append(row.Count).Append('\t').AppendLine(row.Characters.ToString());
        }
        var data = new DataPackage();
        data.SetText(text.ToString());
        Clipboard.SetContent(data);
        PrimaryButtonText = "Copied";
    }
}
