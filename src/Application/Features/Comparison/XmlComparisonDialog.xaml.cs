using Microsoft.UI.Xaml.Controls;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class XmlComparisonDialog : ContentDialog
{
    private readonly List<(string Key, CheckBox Control)> _choices = [];

    public XmlComparisonDialog()
    {
        InitializeComponent();
        foreach (var (key, label, _) in XmlComparisonService.Options)
        {
            var control = new CheckBox { Content = label, IsChecked = App.Settings.GetBoolean(key) };
            IgnoreOptions.Children.Add(control);
            _choices.Add((key, control));
        }
        HideIdentical.IsChecked = App.Settings.GetBoolean("XmlDiffHideIdentical");
    }

    internal void SaveOptions()
    {
        foreach (var (key, control) in _choices)
        {
            App.Settings[key] = control.IsChecked == true;
        }
        App.Settings["XmlDiffHideIdentical"] = HideIdentical.IsChecked == true;
        App.SettingsService.Save();
    }
}
