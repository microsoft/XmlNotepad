using System.Text;
using System.Xml;

namespace XmlNotepad.Services;

internal static class XmlDocumentWriter
{
    public static void Save(XmlDocument document, string path, Settings settings, Encoding? sourceEncoding = null,
        DocumentSaveTarget? approvedTarget = null)
    {
        var encoding = XmlDocumentEncoding.ForSave(document, sourceEncoding);
        if (settings.GetBoolean("NoByteOrderMark") && encoding.CodePage == Encoding.UTF8.CodePage)
        {
            encoding = new UTF8Encoding(false);
        }

        var writerSettings = new XmlWriterSettings
        {
            Encoding = encoding,
            Indent = settings.GetBoolean("AutoFormatOnSave") && !document.PreserveWhitespace,
            IndentChars = new string(settings["IndentChar"] is IndentChar.Tab ? '\t' : ' ',
                Math.Clamp(settings.GetInteger("IndentLevel", 2), 0, XmlDocumentSession.MaximumIndentSize)),
            NewLineOnAttributes = settings.GetBoolean("AttributesOnNewLine"),
            NewLineChars = Settings.UnescapeNewLines(settings.GetString("NewLineChars")),
            NewLineHandling = NewLineHandling.Entitize,
            OmitXmlDeclaration = document.FirstChild is not XmlDeclaration
        };

        path = DocumentSaveTarget.ResolvePath(path);
        if (approvedTarget is { } approved && !string.Equals(path, approved.Path, StringComparison.OrdinalIgnoreCase))
        {
            throw DestinationChanged();
        }
        var temporaryPath = Path.Combine(Path.GetDirectoryName(path)!, Path.GetRandomFileName());
        var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        try
        {
            using (stream)
            {
                WriteDocument(document, stream, writerSettings);
                stream.Flush(flushToDisk: true);
            }

            var destination = new FileInfo(path);
            if (approvedTarget is { } expected && DocumentFileVersion.Read(destination) != expected.Version)
            {
                throw DestinationChanged();
            }
            if (destination.Exists)
            {
                ReplaceDestination(temporaryPath, destination, approvedTarget?.IsReadOnly == true);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }
        catch
        {
            TryDelete(temporaryPath);
            throw;
        }
    }

    private static IOException DestinationChanged() =>
        new("The destination changed during the save. Review the file and try again.");

    private static void ReplaceDestination(string source, FileInfo destination, bool makeWritable)
    {
        var wasReadOnly = destination.IsReadOnly;
        if (wasReadOnly)
        {
            if (!makeWritable)
            {
                throw new UnauthorizedAccessException("The destination is read-only.");
            }
            destination.IsReadOnly = false;
        }

        try
        {
            File.Replace(source, destination.FullName, destinationBackupFileName: null);
        }
        catch
        {
            if (wasReadOnly)
            {
                try
                {
                    destination.Refresh();
                    destination.IsReadOnly = true;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    System.Diagnostics.Debug.WriteLine(exception);
                }
            }
            throw;
        }
    }

    private static void WriteDocument(XmlDocument document, Stream stream, XmlWriterSettings settings)
    {
        try
        {
            using var writer = XmlWriter.Create(stream, settings);
            document.Save(writer);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new XmlException(exception.Message, exception);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Debug.WriteLine(exception);
        }
    }
}
