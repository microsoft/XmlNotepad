namespace XmlNotepad.Services;

internal sealed record LaunchOptions(string? Document, SettingsLocation SettingsMode, bool ShowPointerPosition)
{
    internal static LaunchOptions Parse(IEnumerable<string> arguments)
    {
        string? document = null;
        var settingsMode = SettingsLocation.Auto;
        var showPointerPosition = false;
        var readOptions = true;
        foreach (var argument in arguments)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                continue;
            }
            if (readOptions && argument == "--")
            {
                readOptions = false;
                continue;
            }
            if (readOptions && (argument[0] == '-' || argument[0] == '/'))
            {
                switch (argument.TrimStart('-', '/').ToLowerInvariant())
                {
                    case "test":
                        settingsMode = SettingsLocation.Test;
                        break;
                    case "template":
                        settingsMode = SettingsLocation.PortableTemplate;
                        break;
                    case "debugmouse":
                        showPointerPosition = true;
                        break;
                }
            }
            else if (document is null)
            {
                document = argument.StartsWith("file:", StringComparison.OrdinalIgnoreCase) &&
                    Uri.TryCreate(argument, UriKind.Absolute, out var uri) && uri.IsFile
                    ? uri.LocalPath : argument;
            }
        }
        return new LaunchOptions(document, settingsMode, showPointerPosition);
    }
}
