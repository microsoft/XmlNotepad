using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace XmlNotepad;

public sealed partial class EditorOptionsDialog
{
    private void LoadValueOptions(Settings source)
    {
        LongLineThresholdBox.Text = Math.Max(0, source.GetInteger("MaximumLineLength")).ToString(CultureInfo.InvariantCulture);
        ValueLengthLimitBox.Text = Math.Max(0, source.GetInteger("MaximumValueLength")).ToString(CultureInfo.InvariantCulture);
        WrapLongLinesBox.IsChecked = source.GetBoolean("AutoFormatLongLines");
    }

    private bool TryReadValueOptions(out int longLineThreshold, out int valueLengthLimit)
    {
        var validThreshold = TryParseLength(LongLineThresholdBox, out longLineThreshold);
        var validLimit = TryParseLength(ValueLengthLimitBox, out valueLengthLimit);
        ValueOptionsError.IsOpen = !validThreshold || !validLimit;
        if (ValueOptionsError.IsOpen)
        {
            ValueOptionsError.Message = "Enter a nonnegative whole number. Use 0 for no limit.";
            (validThreshold ? ValueLengthLimitBox : LongLineThresholdBox).Focus(FocusState.Programmatic);
        }
        return !ValueOptionsError.IsOpen;
    }

    private static bool TryParseLength(TextBox box, out int value) =>
        int.TryParse(box.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value);

    private void ApplyValueOptions(int longLineThreshold, int valueLengthLimit)
    {
        _settings["MaximumLineLength"] = longLineThreshold;
        _settings["MaximumValueLength"] = valueLengthLimit;
        _settings["AutoFormatLongLines"] = WrapLongLinesBox.IsChecked == true;
    }
}
