using Microsoft.UI.Xaml.Controls;

namespace XmlNotepad;

public sealed partial class OpenSampleDialog : ContentDialog
{
    public OpenSampleDialog(string directory)
    {
        InitializeComponent();
        var samples = Directory.EnumerateFiles(directory)
            .Select(Path.GetFileName)
            .Where(name => Path.GetExtension(name) is ".xml" or ".xsd" or ".xsl")
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        SamplesList.ItemsSource = samples;
        IsPrimaryButtonEnabled = samples.Length > 0;
        SamplesList.SelectedIndex = samples.Length > 0 ? 0 : -1;
    }

    public string? SelectedFileName => SamplesList.SelectedItem as string;

    private void SamplesList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        IsPrimaryButtonEnabled = SelectedFileName is not null;
}
