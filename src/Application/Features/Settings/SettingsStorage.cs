namespace XmlNotepad.Services;

internal sealed class SettingsStorage(string applicationDirectory, string localApplicationData, string roamingApplicationData)
{
    private const string FileName = "XmlNotepad.settings";
    private const string TemplateName = "XmlNotepad.template.settings";
    private readonly string _portable = Path.Combine(applicationDirectory, FileName);
    private readonly string _local = Path.Combine(localApplicationData, "Microsoft", "Xml Notepad", FileName);
    private readonly string _roaming = Path.Combine(roamingApplicationData, "Microsoft", "Xml Notepad", FileName);
    private readonly string _previousWinUi = Path.Combine(localApplicationData, "Microsoft", "XMLNotepad", "settings.xml");
    private readonly string _locationFile = Path.Combine(localApplicationData, "Microsoft", "Xml Notepad", "settings-location.txt");

    internal void Load(Settings settings, SettingsLocation mode = SettingsLocation.Auto)
    {
        if (mode is SettingsLocation.Test or SettingsLocation.PortableTemplate)
        {
            var sessionFile = Path.Combine(Path.GetTempPath(), "Microsoft", "Xml Notepad",
                mode == SettingsLocation.Test ? "XmlNotepad.test.settings" : "XmlNotepad.template-preview.settings");
            var isolatedSource = mode == SettingsLocation.Test ? sessionFile : FindTemplate();
            if (isolatedSource is not null && File.Exists(isolatedSource))
            {
                settings.Load(isolatedSource);
            }
            settings.FileName = sessionFile;
            settings.SetLocation(mode);
            return;
        }

        if (File.Exists(_portable) && CanUsePortableLocation())
        {
            settings.Load(_portable);
            settings.SetLocation(SettingsLocation.Portable);
            return;
        }

        var preferred = ReadLocation();
        var local = File.Exists(_previousWinUi) && (preferred != SettingsLocation.Local || !File.Exists(_local))
            ? _previousWinUi : _local;
        if (preferred == SettingsLocation.Roaming && File.Exists(_roaming))
        {
            settings.Load(_roaming);
            settings.SetLocation(SettingsLocation.Roaming);
            return;
        }
        if (File.Exists(local))
        {
            settings.Load(local);
            settings.SetLocation(SettingsLocation.Local);
            return;
        }

        var source = File.Exists(_roaming) ? _roaming : File.Exists(_portable) ? _portable : FindTemplate();
        if (source is not null)
        {
            settings.Load(source);
        }
        settings.FileName = _roaming;
        settings.SetLocation(SettingsLocation.Roaming);
    }

    private string? FindTemplate()
    {
        var template = Path.Combine(applicationDirectory, TemplateName);
        if (File.Exists(template))
        {
            return template;
        }
        var resourceTemplate = Path.Combine(applicationDirectory, "Resources", TemplateName);
        return File.Exists(resourceTemplate) ? resourceTemplate : null;
    }

    internal bool CanUsePortableLocation()
    {
        try
        {
            if (File.Exists(_portable) && File.GetAttributes(_portable).HasFlag(FileAttributes.ReadOnly))
            {
                return false;
            }
            var probe = Path.Combine(applicationDirectory, $".{Guid.NewGuid():N}.tmp");
            using var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    internal void Move(Settings settings, SettingsLocation location)
    {
        if (settings.GetLocation() == location)
        {
            return;
        }

        var target = location switch
        {
            SettingsLocation.Portable => _portable,
            SettingsLocation.Local => _local,
            SettingsLocation.Roaming => _roaming,
            _ => throw new ArgumentOutOfRangeException(nameof(location))
        };
        if (File.Exists(target))
        {
            throw new IOException($"A settings file already exists at {target}.");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var source = settings.FileName;
        var movedExisting = File.Exists(source);
        if (movedExisting)
        {
            File.Move(source, target);
        }
        else
        {
            Write(settings, target, overwrite: false);
        }

        try
        {
            if (location is SettingsLocation.Local or SettingsLocation.Roaming)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_locationFile)!);
                WriteFile(_locationFile, path => File.WriteAllText(path, location.ToString()), overwrite: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            if (movedExisting)
            {
                File.Move(target, source);
            }
            else
            {
                File.Delete(target);
            }
            throw;
        }
        settings.FileName = target;
        settings.SetLocation(location);
    }

    internal static void Write(Settings settings, string path, bool overwrite = true) =>
        WriteFile(path, settings.Save, overwrite);

    private static void WriteFile(string path, Action<string> write, bool overwrite)
    {
        var temporary = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            write(temporary);
            File.Move(temporary, path, overwrite);
        }
        finally
        {
            File.Delete(temporary);
        }
    }

    private SettingsLocation ReadLocation()
    {
        try
        {
            return File.Exists(_locationFile) && Enum.TryParse<SettingsLocation>(File.ReadAllText(_locationFile), out var location)
                ? location : SettingsLocation.Auto;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return SettingsLocation.Auto;
        }
    }
}
