using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XmlNotepad.ViewModels;
using DrawingColor = System.Drawing.Color;

namespace XmlNotepad;

public sealed partial class EditorOptionsDialog
{
    private readonly EditorColorRole[] _colorRoles = Enum.GetValues<EditorColorRole>();
    private readonly Dictionary<(ColorTheme Theme, EditorColorRole Role), DrawingColor> _colorChanges = [];
    private bool _loadingColor;

    private void LoadAppearanceOptions(Settings source, ColorTheme currentTheme)
    {
        EditorFontFamilyBox.Text = source.GetString("FontFamily", EditorTypography.DefaultFamily);
        EditorFontSizeBox.Minimum = EditorTypography.MinimumSize;
        EditorFontSizeBox.Maximum = EditorTypography.MaximumSize;
        EditorFontSizeBox.Value = EditorTypography.GetPointSize(source);
        EditorFontBoldBox.IsChecked = source.GetString("FontWeight") == "Bold";
        EditorFontItalicBox.IsChecked = source.GetString("FontStyle") == "Italic";
        EditorLineHeightBox.Minimum = EditorTypography.MinimumLineHeight;
        EditorLineHeightBox.Maximum = EditorTypography.MaximumLineHeight;
        EditorLineHeightBox.Value = EditorTypography.GetLineHeight(source);
        TreeIndentBox.Minimum = 0;
        TreeIndentBox.Maximum = EditorTypography.MaximumTreeIndent;
        TreeIndentBox.Value = EditorTypography.GetTreeIndent(source);
        ColorRoleBox.ItemsSource = _colorRoles.Select(role => EditorPalette.Entries[role].Label).ToArray();
        PaletteThemeBox.SelectedIndex = (int)currentTheme;
        ColorRoleBox.SelectedIndex = 0;
        DisplaySelectedColor();
    }

    private void ApplyAppearanceOptions()
    {
        var family = EditorFontFamilyBox.Text.Trim();
        _settings["FontFamily"] = family.Length == 0 ? EditorTypography.DefaultFamily : family;
        if (double.IsFinite(EditorFontSizeBox.Value))
        {
            _settings["FontSize"] = EditorFontSizeBox.Value;
        }
        _settings["FontWeight"] = EditorFontBoldBox.IsChecked == true ? "Bold" : "Normal";
        _settings["FontStyle"] = EditorFontItalicBox.IsChecked == true ? "Italic" : "Normal";
        if (double.IsFinite(EditorLineHeightBox.Value))
        {
            _settings["EditorLineHeight"] = (int)Math.Round(EditorLineHeightBox.Value);
        }
        if (double.IsFinite(TreeIndentBox.Value))
        {
            _settings["TreeIndent"] = (int)Math.Round(TreeIndentBox.Value);
        }
        if (_restoreDefaults)
        {
            _settings["LightColors"] = ThemeColors.GetDefaultColors(ColorTheme.Light);
            _settings["DarkColors"] = ThemeColors.GetDefaultColors(ColorTheme.Dark);
        }
        foreach (var group in _colorChanges.GroupBy(change => change.Key.Theme))
        {
            var current = EditorPalette.ReadColors(_settings, group.Key);
            var updated = new ThemeColors { Markup = current.Markup };
            foreach (var entry in EditorPalette.Entries.Values)
            {
                entry.Write(updated, entry.Read(current));
            }
            foreach (var change in group)
            {
                EditorPalette.Entries[change.Key.Role].Write(updated, change.Value);
            }
            _settings[EditorPalette.SettingName(group.Key)] = updated;
        }
    }

    private (ColorTheme Theme, EditorColorRole Role) SelectedColor =>
        ((ColorTheme)PaletteThemeBox.SelectedIndex, _colorRoles[ColorRoleBox.SelectedIndex]);

    private void PaletteSelection_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (PaletteThemeBox?.SelectedIndex >= 0 && ColorRoleBox?.SelectedIndex >= 0)
        {
            DisplaySelectedColor();
        }
    }

    private void DisplaySelectedColor()
    {
        var selected = SelectedColor;
        var color = _colorChanges.TryGetValue(selected, out var edited)
            ? edited
            : EditorPalette.Entries[selected.Role].Read(_restoreDefaults
                ? ThemeColors.GetDefaultColors(selected.Theme)
                : EditorPalette.ReadColors(_settings, selected.Theme));
        _loadingColor = true;
        XmlColorPicker.Color = EditorPalette.ToWindowsColor(color);
        _loadingColor = false;
    }

    private void XmlColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs e)
    {
        if (!_loadingColor && PaletteThemeBox?.SelectedIndex >= 0 && ColorRoleBox?.SelectedIndex >= 0)
        {
            var color = e.NewColor;
            _colorChanges[SelectedColor] = DrawingColor.FromArgb(color.A, color.R, color.G, color.B);
        }
    }

    private void RestoreColor_Click(object sender, RoutedEventArgs e)
    {
        var selected = SelectedColor;
        _colorChanges[selected] = EditorPalette.Entries[selected.Role].Read(ThemeColors.GetDefaultColors(selected.Theme));
        DisplaySelectedColor();
    }
}
