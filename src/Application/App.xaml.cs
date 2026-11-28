using System.Text;
using System.Xml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.Windows.AppLifecycle;
using Windows.Storage;
using XmlNotepad;
using XmlNotepad.Services;
using XmlNotepad.ViewModels;
using IFileActivatedEventArgs = Windows.ApplicationModel.Activation.IFileActivatedEventArgs;

namespace XmlNotepad;

public partial class App : Application
{
    private static readonly HashSet<MainWindow> Windows = [];
    private readonly LaunchOptions _launch = LaunchOptions.Parse(Environment.GetCommandLineArgs().Skip(1));

    public static Settings Settings { get; private set; } = null!;

    public static AppSettingsService SettingsService { get; private set; } = null!;

    internal static EditorTypography Typography { get; } = new();
    internal static Uri DocumentationUri { get; } = new("https://microsoft.github.io/XmlNotepad/");

    internal static bool ShowPointerPosition => ((App)Current)._launch.ShowPointerPosition;


    public App()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        SettingsService = new AppSettingsService(_launch.SettingsMode);
        Settings = SettingsService.Settings;
        InitializeComponent();
        Typography.Load(Settings);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (AppInstance.GetCurrent().GetActivatedEventArgs() is
            { Kind: ExtendedActivationKind.File, Data: IFileActivatedEventArgs activation })
        {
            var paths = activation.Files.OfType<StorageFile>().Select(file => file.Path)
                .Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (paths.Length > 0)
            {
                foreach (var path in paths)
                {
                    OpenDocumentWindow(path);
                }
                return;
            }
        }
        OpenDocumentWindow(_launch.Document);
    }

    internal static void OpenDocumentWindow(string? path, int line = 0, int column = 0, XmlDocument? initialDocument = null)
    {
        var window = new MainWindow(path, line, column, initialDocument);
        Windows.Add(window);
        window.Closed += (_, _) => Windows.Remove(window);
        ApplyTheme(window);
        window.Show();
    }

    internal static void ApplyThemeToWindows()
    {
        Typography.Load(Settings);
        foreach (var window in Windows)
        {
            ApplyTheme(window);
            window.RefreshEditorPalette();
        }
    }

    private static void ApplyTheme(Window window)
    {
        if (window.Content is FrameworkElement root)
        {
            root.RequestedTheme = Settings.GetBoolean("FollowSystemTheme")
                ? ElementTheme.Default
                : Settings["Theme"] is ColorTheme.Dark ? ElementTheme.Dark : ElementTheme.Light;

            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                window.AppWindow.TitleBar.PreferredTheme = root.RequestedTheme switch
                {
                    ElementTheme.Dark => TitleBarTheme.Dark,
                    ElementTheme.Light => TitleBarTheme.Light,
                    _ => TitleBarTheme.UseDefaultAppMode
                };
            }
        }
    }
}
