namespace XmlNotepad.Services;

internal static class ComparisonStyles
{
    private const string DefaultResource = "XmlNotepad.ComparisonDefault.css";

    internal static string UserPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft", "Xml Notepad", "XmlDiffStyles.css");

    internal static string EnsureUserFile()
    {
        var path = UserPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path))
        {
            var defaults = ReadDefault();
            FileStream stream;
            try
            {
                stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            }
            catch (IOException) when (File.Exists(path))
            {
                return path;
            }
            using var writer = new StreamWriter(stream);
            writer.Write(defaults);
        }
        return path;
    }

    internal static void WriteToReport(TextWriter writer, string path)
    {
        var defaults = ReadDefault();
        writer.WriteLine(defaults);
        try
        {
            var custom = File.ReadAllText(path);
            if (!string.Equals(custom.Trim(), defaults.Trim(), StringComparison.Ordinal))
            {
                writer.WriteLine(custom.Replace("<", @"\3c ", StringComparison.Ordinal));
            }
        }
        catch (FileNotFoundException)
        {
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    private static string ReadDefault()
    {
        using var stream = typeof(ComparisonStyles).Assembly.GetManifestResourceStream(DefaultResource)
            ?? throw new InvalidOperationException("The default XML comparison stylesheet is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
