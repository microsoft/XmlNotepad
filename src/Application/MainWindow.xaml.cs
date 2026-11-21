using Microsoft.UI.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using System.Xml;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class MainWindow : Window
{
    private bool _closePromptRunning;
    private bool _allowClose;
    private readonly MainWindowPlacement _placement;
    private readonly ThemeSettings _themeSettings;

    internal bool HighContrast => _themeSettings.HighContrast;

    public MainWindow(string? startupFile, int line = 0, int column = 0, XmlDocument? initialDocument = null)
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        var icon = Path.Combine(ApplicationFiles.ContentDirectory, "Assets", "XmlNotepad.ico");
        if (File.Exists(icon))
        {
            AppWindow.SetIcon(icon);
        }
        _placement = new MainWindowPlacement(AppWindow, App.SettingsService.Settings);
        _themeSettings = ThemeSettings.CreateForWindowId(AppWindow.Id);
        _themeSettings.Changed += ThemeSettings_Changed;
        AppWindow.Closing += AppWindow_Closing;
        RootFrame.Navigate(typeof(MainPage), (this, startupFile, line, column, initialDocument));
        Closed += (_, _) =>
        {
            _themeSettings.Changed -= ThemeSettings_Changed;
            if (RootFrame.Content is MainPage page)
            {
                page.ViewModel.Dispose();
            }
            App.SettingsService.Save();
        };
    }

    private void ThemeSettings_Changed(ThemeSettings sender, object args) =>
        DispatcherQueue.TryEnqueue(RefreshEditorPalette);

    internal void Show()
    {
        Activate();
        _placement.RestorePresentation();
    }

    internal void RefreshEditorPalette()
    {
        if (RootFrame.Content is MainPage page)
        {
            page.RefreshEditorPalette();
        }
    }

    public void SetTitle(string title)
    {
        Title = title;
        AppTitleBar.Title = title;
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_allowClose)
        {
            return;
        }

        args.Cancel = true;
        RequestClose();
    }

    internal void RequestClose()
    {
        if (!_closePromptRunning)
        {
            _ = ConfirmAndCloseAsync();
        }
    }

    private async Task ConfirmAndCloseAsync()
    {
        _closePromptRunning = true;
        try
        {
            var canClose = RootFrame.Content is MainPage page && await page.ConfirmCloseAsync();
            if (canClose)
            {
                _placement.Save();
                _allowClose = true;
                Close();
            }
        }
        finally
        {
            _closePromptRunning = false;
        }
    }
}
