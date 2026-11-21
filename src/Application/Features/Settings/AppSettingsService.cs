using System.Xml;

namespace XmlNotepad.Services;

public sealed class AppSettingsService : IServiceProvider
{
    private readonly SettingsStorage _storage = new(AppContext.BaseDirectory,
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
    private readonly WebProxyService _webProxyService;

    public AppSettingsService() : this(SettingsLocation.Auto)
    {
    }

    internal AppSettingsService(SettingsLocation settingsMode)
    {
        Settings = new Settings
        {
            StartupPath = ApplicationFiles.ContentDirectory,
            ExecutablePath = Environment.ProcessPath ?? AppContext.BaseDirectory,
            Resolver = new XmlUrlResolver()
        };
        Settings.SetDefaults();
        _webProxyService = new WebProxyService(this);
        SchemaCache = new SchemaCache(this);
        Settings["SchemaCache"] = SchemaCache;
        _storage.Load(Settings, settingsMode);
    }

    public Settings Settings { get; }

    public SchemaCache SchemaCache { get; }

    public bool CanStoreSettingsBesideApp => _storage.CanUsePortableLocation();

    public bool CanMoveSettings => Settings.GetLocation() is SettingsLocation.Local or SettingsLocation.Portable or SettingsLocation.Roaming;

    public void MoveSettings(SettingsLocation location)
    {
        if (CanMoveSettings)
        {
            _storage.Move(Settings, location);
        }
    }

    public object? GetService(Type serviceType) => serviceType == typeof(WebProxyService)
        ? _webProxyService
        : serviceType == typeof(Settings) ? Settings : null;

    public IReadOnlyList<Uri> RecentFiles => Settings["RecentFiles"] as Uri[] ?? [];

    public IReadOnlyList<Uri> RecentStylesheets => Settings["RecentXsltFiles"] as Uri[] ?? [];

    public bool IsSettingsFile(string path) =>
        string.Equals(path, Settings.FileName, StringComparison.OrdinalIgnoreCase);

    public void AddRecentFile(string path)
    {
        var location = RemoteDocument.TryGetUri(path, out var remote) ? remote : new Uri(Path.GetFullPath(path));
        if (location.IsFile && IsSettingsFile(location.LocalPath))
        {
            return;
        }
        AddRecentLocation("RecentFiles", location, RecentFiles);
    }

    internal void AddRecentStylesheet(Uri location) => AddRecentLocation("RecentXsltFiles", location, RecentStylesheets);

    private void AddRecentLocation(string settingName, Uri location, IReadOnlyList<Uri> recent)
    {
        Settings[settingName] = new[] { location }
            .Concat(recent.Where(uri => uri.IsAbsoluteUri && !SameLocation(uri, location)))
            .Take(10)
            .ToArray();
        Save();
    }

    private static bool SameLocation(Uri first, Uri second) => first.IsFile && second.IsFile
        ? string.Equals(first.LocalPath, second.LocalPath, StringComparison.OrdinalIgnoreCase)
        : string.Equals(first.AbsoluteUri, second.AbsoluteUri, StringComparison.Ordinal);

    public void Save()
    {
        SettingsStorage.Write(Settings, Settings.FileName);
    }
}
