using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class CsvImportDialog : ContentDialog
{
    private static readonly (string Label, char Value)[] Delimiters =
    [
        ("Comma (,)", ','), ("Tab", '\t'), ("Space", ' '), ("Semicolon (;)", ';'),
        ("Colon (:)", ':'), ("Vertical bar (|)", '|'), ("Slash (/)", '/'), ("Custom", '\0')
    ];

    private readonly string _text;

    public CsvImportDialog(string text)
    {
        _text = text;
        InitializeComponent();
        DelimiterBox.ItemsSource = Delimiters.Select(delimiter => delimiter.Label).ToArray();
        DelimiterBox.SelectedIndex = 0;
    }

    public char Delimiter { get; private set; }

    public bool FirstRowIsHeader => HeaderCheckBox.IsChecked == true;

    private void Delimiter_Changed(object sender, SelectionChangedEventArgs e) => UpdatePreview();

    private void CustomDelimiter_Changed(object sender, TextChangedEventArgs e) => UpdatePreview();

    private void UpdatePreview()
    {
        if (DelimiterBox.SelectedIndex < 0 || CustomDelimiterBox is null || PreviewList is null || PreviewStatus is null)
        {
            return;
        }

        char delimiter = Delimiters[DelimiterBox.SelectedIndex].Value;
        CustomDelimiterBox.Visibility = delimiter == '\0' ? Visibility.Visible : Visibility.Collapsed;
        Delimiter = delimiter == '\0' ? CustomDelimiterBox.Text.FirstOrDefault() : delimiter;
        var fields = Delimiter == '\0' ? [] : CsvImport.ReadFirstRow(_text, Delimiter);
        PreviewList.ItemsSource = fields;
        PreviewStatus.Text = Delimiter == '\0' ? "Enter a delimiter."
            : fields.Length == 0 ? "No rows found in this file." : string.Empty;
        IsPrimaryButtonEnabled = fields.Length > 0;
    }
}
