using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class EditorOptionsDialog : ContentDialog
{
    private static readonly string[] LineEndings = ["\r\n", "\n", "\r"];
    private readonly Settings _settings;
    private readonly ColorTheme _currentTheme;
    private bool _restoreDefaults;

    public EditorOptionsDialog(ColorTheme currentTheme)
    {
        _settings = App.SettingsService.Settings;
        _currentTheme = currentTheme;
        InitializeComponent();
        if (App.SettingsService.CanMoveSettings)
        {
            SettingsLocationBox.SelectedIndex = (int)_settings.GetLocation();
            PortableSettingsItem.IsEnabled = App.SettingsService.CanStoreSettingsBesideApp;
        }
        else
        {
            SettingsLocationBox.Visibility = Visibility.Collapsed;
        }
        UpdateOptions.Visibility = _settings.GetBoolean("DisableUpdateUI") ? Visibility.Collapsed : Visibility.Visible;
        LoadOptions(_settings);
    }

    private void LoadOptions(Settings source)
    {
        LoadValidationOptions(source);
        LoadValueOptions(source);
        ThemeBox.SelectedIndex = source.GetBoolean("FollowSystemTheme") ? 0 : source["Theme"] is ColorTheme.Dark ? 2 : 1;
        LoadAppearanceOptions(source, _currentTheme);
        DiagnosticsPaneBox.IsChecked = source.GetBoolean("ShowDiagnosticsPane", true);
        IdentifyingTextBox.IsChecked = source.GetBoolean("SchemaAwareText");
        IdentifyingNamesBox.Text = source.GetString("SchemaAwareNames");
        PreserveWhitespaceBox.IsChecked = source.GetBoolean("PreserveWhitespace");
        PromptBeforeReloadBox.IsChecked = source.GetBoolean("PromptOnReload");
        AutoFormatBox.IsChecked = source.GetBoolean("AutoFormatOnSave");
        IndentCharacterBox.SelectedIndex = source["IndentChar"] is IndentChar.Tab ? 1 : 0;
        IndentSizeBox.Maximum = XmlDocumentSession.MaximumIndentSize;
        IndentSizeBox.Value = Math.Clamp(source.GetInteger("IndentLevel", 2), 0, XmlDocumentSession.MaximumIndentSize);
        var lineEnding = Settings.UnescapeNewLines(source.GetString("NewLineChars"));
        LineEndingsBox.SelectedIndex = Math.Max(0, Array.IndexOf(LineEndings, lineEnding));
        NewLineOnAttributesBox.IsChecked = source.GetBoolean("AttributesOnNewLine");
        OmitUtf8BomBox.IsChecked = source.GetBoolean("NoByteOrderMark");
        TextEditorBox.Text = source.GetString("TextEditor");
        DefaultXsltBox.IsChecked = !source.GetBoolean("DisableDefaultXslt");
        XsltScriptsBox.IsChecked = source.GetBoolean("EnableXsltScripts", true);
        AutomaticUpdatesBox.IsChecked = source.GetBoolean("UpdateEnabled");
        var feed = source.GetString("UpdateLocation");
        UpdateLocationBox.Text = string.IsNullOrWhiteSpace(feed) ? Settings.DefaultUpdateLocation : feed;
        var interval = source.GetTimeSpan("UpdateFrequency", TimeSpan.FromDays(1));
        UpdateIntervalBox.Text = (interval < ReleaseUpdateService.MinimumInterval ? TimeSpan.FromDays(1) : interval)
            .ToString("c", CultureInfo.InvariantCulture);
    }

    private void Options_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        _restoreDefaults = true;
        _colorChanges.Clear();
        var defaults = Settings.CreateDefaultSnapshot();
        LoadOptions(defaults);
        ValidationOptionsError.IsOpen = false;
        ValueOptionsError.IsOpen = false;
        UpdateOptionsError.IsOpen = false;
        ResetNotice.IsOpen = true;
        OptionsScroll.ChangeView(null, 0, null, disableAnimation: true);
    }

    private void Apply(long maximumLineIndex, int longLineThreshold, int valueLengthLimit)
    {
        _settings["FollowSystemTheme"] = ThemeBox.SelectedIndex == 0;
        _settings["ShowDiagnosticsPane"] = DiagnosticsPaneBox.IsChecked == true;
        _settings["SchemaAwareText"] = IdentifyingTextBox.IsChecked == true;
        _settings["SchemaAwareNames"] = IdentifyingNamesBox.Text.Trim();
        ApplyAppearanceOptions();
        if (ThemeBox.SelectedIndex > 0 || _restoreDefaults)
        {
            _settings["Theme"] = ThemeBox.SelectedIndex == 2 ? ColorTheme.Dark : ColorTheme.Light;
        }
        _settings["PreserveWhitespace"] = PreserveWhitespaceBox.IsChecked == true;
        _settings["PromptOnReload"] = PromptBeforeReloadBox.IsChecked == true;
        ApplyValidationOptions(maximumLineIndex);
        ApplyValueOptions(longLineThreshold, valueLengthLimit);
        _settings["AutoFormatOnSave"] = AutoFormatBox.IsChecked == true;
        _settings["IndentChar"] = IndentCharacterBox.SelectedIndex == 1 ? IndentChar.Tab : IndentChar.Space;
        if (double.IsFinite(IndentSizeBox.Value))
        {
            _settings["IndentLevel"] = (int)Math.Round(IndentSizeBox.Value);
        }
        _settings["NewLineChars"] = Settings.EscapeNewLines(LineEndings[LineEndingsBox.SelectedIndex]);
        _settings["AttributesOnNewLine"] = NewLineOnAttributesBox.IsChecked == true;
        _settings["NoByteOrderMark"] = OmitUtf8BomBox.IsChecked == true;
        _settings["TextEditor"] = TextEditorBox.Text.Trim();
        _settings["DisableDefaultXslt"] = DefaultXsltBox.IsChecked != true;
        _settings["EnableXsltScripts"] = XsltScriptsBox.IsChecked == true;
        if (UpdateOptions.Visibility == Visibility.Visible)
        {
            var location = UpdateLocationBox.Text.Trim();
            if (!string.Equals(_settings.GetString("UpdateLocation"), location, StringComparison.Ordinal))
            {
                _settings["LastUpdateCheck"] = DateTime.MinValue;
            }
            _settings["UpdateLocation"] = location;
            _settings["UpdateEnabled"] = AutomaticUpdatesBox.IsChecked == true;
            _settings["UpdateFrequency"] = TimeSpan.ParseExact(UpdateIntervalBox.Text.Trim(), "c", CultureInfo.InvariantCulture);
        }
        App.SettingsService.Save();
    }

    private void Options_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (!TryReadValidationOptions(out var maximumLineIndex) ||
            !TryReadValueOptions(out var longLineThreshold, out var valueLengthLimit))
        {
            args.Cancel = true;
            return;
        }

        string? error = null;
        if (UpdateOptions.Visibility == Visibility.Visible)
        {
            if (!ReleaseUpdateService.TryGetLocation(UpdateLocationBox.Text.Trim(), out _))
            {
                error = "Enter an HTTP or HTTPS address, or an absolute path to an XML file.";
                UpdateLocationBox.Focus(FocusState.Programmatic);
            }
            else if (!TimeSpan.TryParseExact(UpdateIntervalBox.Text.Trim(), "c", CultureInfo.InvariantCulture, out var interval) ||
                interval < ReleaseUpdateService.MinimumInterval)
            {
                error = "Enter at least five seconds in days.hours:minutes:seconds format, for example 1.00:00:00.";
                UpdateIntervalBox.Focus(FocusState.Programmatic);
            }
        }
        UpdateOptionsError.Message = error ?? string.Empty;
        UpdateOptionsError.IsOpen = error is not null;
        if (error is not null)
        {
            args.Cancel = true;
            return;
        }

        try
        {
            App.SettingsService.MoveSettings((SettingsLocation)SettingsLocationBox.SelectedIndex);
            Apply(maximumLineIndex, longLineThreshold, valueLengthLimit);
            StorageError.IsOpen = false;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StorageError.Message = $"Could not save settings: {exception.Message}";
            StorageError.IsOpen = true;
            SettingsLocationBox.Focus(FocusState.Programmatic);
            args.Cancel = true;
        }
    }

    private async void BrowseEditor_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker(XamlRoot.ContentIslandEnvironment.AppWindowId);
        picker.FileTypeFilter.Add(".exe");
        if (await picker.PickSingleFileAsync() is { } application)
        {
            TextEditorBox.Text = application.Path;
        }
    }
}
