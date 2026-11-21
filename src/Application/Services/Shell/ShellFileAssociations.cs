using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace XmlNotepad.Services;

internal static class ShellFileAssociations
{
    private const string InstalledName = "XML Notepad";
    private const string PortableName = "XML Notepad (Portable)";
    private const string InstalledProgId = "XmlNotepad.Document";
    private const string PortableProgId = "XmlNotepad.Portable.Document";
    private const string PortableCapabilities = @"Software\LovettSoftware\XmlNotepadPortable\Capabilities";
    private const string RegisteredApplications = @"Software\RegisteredApplications";
    private static readonly string[] Extensions = [".xml", ".xsd", ".xsl", ".xslt"];

    internal static readonly Uri DefaultApps = new("ms-settings:defaultapps");

    internal static Uri? FindSettingsPage(string executable)
    {
        var appId = GetPackagedAppId();
        if (appId is not null)
        {
            return SettingsPage("registeredAUMID", appId);
        }
        if (IsRegistered(Registry.LocalMachine, InstalledName, InstalledProgId, executable))
        {
            return SettingsPage("registeredAppMachine", InstalledName);
        }
        return IsRegistered(Registry.CurrentUser, PortableName, PortableProgId, executable)
            ? SettingsPage("registeredAppUser", PortableName) : null;
    }

    internal static Uri RegisterPortable(string executable)
    {
        WritePortableRegistration(Registry.CurrentUser, executable);
        const int associationChanged = 0x08000000;
        SHChangeNotify(associationChanged, 0, IntPtr.Zero, IntPtr.Zero);
        return SettingsPage("registeredAppUser", PortableName);
    }

    internal static void WritePortableRegistration(RegistryKey root, string executable)
    {
        using var progId = root.CreateSubKey($@"Software\Classes\{PortableProgId}");
        progId.SetValue(null, "XML document");
        using var command = progId.CreateSubKey(@"shell\open\command");
        command.SetValue(null, OpenCommand(executable));
        using var icon = progId.CreateSubKey("DefaultIcon");
        icon.SetValue(null, $"\"{executable}\",0");

        using var capabilities = root.CreateSubKey(PortableCapabilities);
        capabilities.SetValue("ApplicationName", PortableName);
        capabilities.SetValue("ApplicationDescription", "Edit XML documents, schemas, and stylesheets.");
        using var associations = capabilities.CreateSubKey("FileAssociations");
        foreach (var extension in Extensions)
        {
            associations.SetValue(extension, PortableProgId);
            using var openWith = root.CreateSubKey($@"Software\Classes\{extension}\OpenWithProgids");
            openWith.SetValue(PortableProgId, string.Empty);
        }
        using var registered = root.CreateSubKey(RegisteredApplications);
        registered.SetValue(PortableName, PortableCapabilities);
    }

    private static bool IsRegistered(RegistryKey root, string name, string progId, string executable)
    {
        using var registered = root.OpenSubKey(RegisteredApplications);
        using var command = root.OpenSubKey($@"Software\Classes\{progId}\shell\open\command");
        return registered?.GetValue(name) is string &&
            string.Equals(command?.GetValue(null) as string, OpenCommand(executable), StringComparison.OrdinalIgnoreCase);
    }

    private static string OpenCommand(string executable) => $"\"{executable}\" \"%1\"";

    private static Uri SettingsPage(string parameter, string value) =>
        new($"{DefaultApps}?{parameter}={Uri.EscapeDataString(value)}");

    private static string? GetPackagedAppId()
    {
        const int insufficientBuffer = 122;
        uint length = 0;
        if (GetCurrentApplicationUserModelId(ref length, null) != insufficientBuffer)
        {
            return null;
        }
        var appId = new StringBuilder((int)length);
        return GetCurrentApplicationUserModelId(ref length, appId) == 0 ? appId.ToString() : null;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int GetCurrentApplicationUserModelId(ref uint length, StringBuilder? appId);

    [DllImport("shell32.dll", ExactSpelling = true)]
    private static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);
}
