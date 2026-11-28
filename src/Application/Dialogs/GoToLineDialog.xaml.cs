using Microsoft.UI.Xaml.Controls;

namespace XmlNotepad;

public sealed partial class GoToLineDialog : ContentDialog
{
    private readonly int _lastLine;

    public GoToLineDialog(int lastLine, int line, int column)
    {
        _lastLine = lastLine;
        InitializeComponent();
        LineBox.Header = $"Line (1–{lastLine})";
        LineBox.Text = line.ToString();
        ColumnBox.Text = column.ToString();
        LineBox.SelectAll();
    }

    public int Line { get; private set; }
    public int Column { get; private set; }

    private void Position_Changed(object sender, TextChangedEventArgs args)
    {
        if (LineBox is null || ColumnBox is null)
        {
            return;
        }

        bool validLine = int.TryParse(LineBox.Text, out int line) && line > 0 && line <= _lastLine;
        bool validColumn = int.TryParse(ColumnBox.Text, out int column) && column > 0;
        Line = line;
        Column = validColumn ? column : 1;
        IsPrimaryButtonEnabled = validLine && (validColumn || string.IsNullOrWhiteSpace(ColumnBox.Text));
    }
}
