namespace XmlNotepad.Services;

internal readonly record struct DocumentFileVersion(DateTime LastWriteTimeUtc, long Length)
{
    public static DocumentFileVersion? Read(string path) => Read(new FileInfo(DocumentSaveTarget.ResolvePath(path)));

    internal static DocumentFileVersion? Read(FileInfo file) =>
        file.Exists ? new DocumentFileVersion(file.LastWriteTimeUtc, file.Length) : null;
}

internal readonly record struct DocumentSaveTarget(string Path, DocumentFileVersion? Version, bool IsReadOnly)
{
    internal static DocumentSaveTarget Inspect(string path)
    {
        var file = new FileInfo(ResolvePath(path));
        var version = DocumentFileVersion.Read(file);
        return new DocumentSaveTarget(file.FullName, version, version is not null && file.IsReadOnly);
    }

    internal static string ResolvePath(string path)
    {
        var target = new FileInfo(path);
        try
        {
            return target.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? target.FullName;
        }
        catch (FileNotFoundException)
        {
            return target.FullName;
        }
    }
}
