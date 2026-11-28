using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XmlNotepad.ViewModels;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private XmlNodeItem? _valueDetailsItem;
    private string _displayedValue = string.Empty;
    private bool _updatingValueDetails;

    private static bool HasLongValueLine(string value) => XmlValueLineEndings.HasLineLongerThan(
        value, App.Settings.GetInteger("MaximumLineLength", 10000));

    private static int GetValueLengthLimit(string value)
    {
        var limit = App.Settings.GetInteger("MaximumValueLength", short.MaxValue);
        return limit > 0 ? Math.Max(value.Length, limit) : 0;
    }

    private void ShowValueDetails()
    {
        if (!ReferenceEquals(_valueDetailsItem, ViewModel.SelectedNode))
        {
            _valueDetailsItem = ViewModel.SelectedNode;
            SelectedNodeTitle.Text = _valueDetailsItem?.DisplayName ?? string.Empty;
            var value = ViewModel.SelectedValue;
            WrapValueLines.IsChecked = !HasLongValueLine(value) || App.Settings.GetBoolean("AutoFormatLongLines");
            LoadValueDetails();
        }
        ValueDetails.Visibility = Visibility.Visible;
    }

    private void LoadValueDetails()
    {
        var value = ViewModel.SelectedValue;
        _updatingValueDetails = true;
        try
        {
            ValueEditor.MaxLength = GetValueLengthLimit(value);
            ValueEditor.Text = value;
            _displayedValue = ValueEditor.Text;
        }
        finally
        {
            _updatingValueDetails = false;
        }
    }

    private void CloseValueDetails()
    {
        _valueDetailsItem = null;
        ValueDetails.Visibility = Visibility.Collapsed;
        ValueEditor.Text = string.Empty;
        _displayedValue = string.Empty;
    }

    private void WrapValueLines_Changed(object sender, RoutedEventArgs e)
    {
        if (ValueEditor is not null && sender is CheckBox wrap)
        {
            ValueEditor.TextWrapping = wrap.IsChecked == true ? TextWrapping.Wrap : TextWrapping.NoWrap;
        }
    }

    private void ValueEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingValueDetails || _valueDetailsItem is null ||
            !ReferenceEquals(_valueDetailsItem, ViewModel.SelectedNode))
        {
            return;
        }

        var text = ValueEditor.Text;
        var value = XmlValueLineEndings.ApplyEditorChange(ViewModel.SelectedValue, _displayedValue, text);
        _displayedValue = text;
        _updatingValueDetails = true;
        try
        {
            ViewModel.SelectedValue = value;
        }
        finally
        {
            _updatingValueDetails = false;
        }
    }

    private void ValueEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_valueDetailsItem is not null && ReferenceEquals(_valueDetailsItem, ViewModel.SelectedNode))
        {
            ViewModel.CommitSelectedValue();
        }
        ResumeDeferredDiagnostics();
    }
}
