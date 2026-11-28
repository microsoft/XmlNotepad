using System.Globalization;
using Microsoft.UI.Xaml;

namespace XmlNotepad;

public sealed partial class EditorOptionsDialog
{
    private void LoadValidationOptions(Settings source)
    {
        ProcessDtdBox.IsChecked = !source.GetBoolean("IgnoreDTD", true);
        MaximumLineIndexBox.Text = Math.Max(0, source.GetLong("MaximumLineIndex")).ToString(CultureInfo.InvariantCulture);
        SchemaLanguageBox.Text = source.GetString("Language");
    }

    private void DtdProcessing_Changed(object sender, RoutedEventArgs e)
    {
        if (DtdWarning is not null)
        {
            DtdWarning.IsOpen = ProcessDtdBox.IsChecked == true;
        }
    }

    private bool TryReadValidationOptions(out long maximumLineIndex)
    {
        var valid = long.TryParse(MaximumLineIndexBox.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out maximumLineIndex);
        ValidationOptionsError.IsOpen = !valid;
        if (!valid)
        {
            ValidationOptionsError.Message = "Enter a nonnegative whole number. Use 0 to turn off line indexing.";
            MaximumLineIndexBox.Focus(FocusState.Programmatic);
        }
        return valid;
    }

    private void ApplyValidationOptions(long maximumLineIndex)
    {
        _settings["IgnoreDTD"] = ProcessDtdBox.IsChecked != true;
        _settings["MaximumLineIndex"] = maximumLineIndex;
        _settings["Language"] = SchemaLanguageBox.Text.Trim();
    }
}
