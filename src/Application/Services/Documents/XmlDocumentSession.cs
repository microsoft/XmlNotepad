using System.Text;
using System.Xml;
using XmlNotepad;

namespace XmlNotepad.Services;

public sealed class XmlDocumentSession
{
    internal const int MaximumIndentSize = 32;
    private XmlDocument _document = new();

    public event EventHandler<XmlNodeChangedEventArgs>? DocumentChanged;

    public event EventHandler? DocumentReplaced;

    public event EventHandler? StateChanged;

    public XmlDocument Document => _document;

    public string? FilePath { get; private set; }
    internal DocumentFileVersion? FileVersion { get; private set; }
    internal Uri? RemoteSource { get; private set; }

    private string? _importFileName;
    private Encoding? _sourceEncoding;

    public string FileName => FilePath is null
        ? _importFileName ?? (RemoteSource is { } source ? RemoteDocument.GetFileName(source) : "Untitled")
        : Path.GetFileName(FilePath);

    public bool IsDirty { get; private set; }

    public void CreateNew()
    {
        FilePath = null;
        RemoteSource = null;
        FileVersion = null;
        _importFileName = null;
        _sourceEncoding = null;
        SetDocument(CreateDocument());
        IsDirty = false;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task OpenAsync(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var (document, version, encoding) = await Task.Run(() =>
        {
            var version = DocumentFileVersion.Read(fullPath);
            var resolver = new XmlResourceResolver(Settings.Instance.Resolver ?? new XmlUrlResolver());
            using var stream = File.OpenRead(fullPath);
            var (document, encoding) = LoadDocument(stream, new Uri(fullPath), resolver);
            return (document, version, encoding);
        });
        FilePath = fullPath;
        FileVersion = version;
        RemoteSource = null;
        _importFileName = null;
        _sourceEncoding = encoding;
        ReplaceDocument(document, false);
    }

    internal async Task OpenAsync(RemoteDocument remote)
    {
        using var stream = await remote.OpenStreamAsync();
        var resolver = new RemoteXmlResolver(Settings.Instance.Resolver ?? new XmlUrlResolver());
        var (document, encoding) = await Task.Run(() => LoadDocument(stream, remote.Location, resolver));
        FilePath = null;
        FileVersion = null;
        RemoteSource = remote.Location;
        _importFileName = null;
        _sourceEncoding = encoding;
        ReplaceDocument(document, false);
    }

    public void ReplaceDocument(XmlDocument document, bool isDirty)
    {
        SetDocument(document);
        IsDirty = isDirty;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetImportedDocument(XmlDocument document, string sourcePath, Uri? remoteSource = null)
    {
        FilePath = null;
        FileVersion = null;
        RemoteSource = remoteSource;
        _sourceEncoding = null;
        var name = remoteSource is null ? Path.GetFileName(sourcePath) : RemoteDocument.GetFileName(remoteSource);
        _importFileName = Path.GetFileNameWithoutExtension(name) + ".xml";
        ReplaceDocument(document, true);
    }

    internal async Task SaveAsync(string path, DocumentSaveTarget approvedTarget)
    {
        var document = _document;
        var fullPath = Path.GetFullPath(path);
        FileVersion = await Task.Run(() =>
        {
            XmlDocumentWriter.Save(document, fullPath, Settings.Instance, _sourceEncoding, approvedTarget);
            return DocumentFileVersion.Read(fullPath);
        });
        FilePath = fullPath;
        RemoteSource = null;
        IsDirty = false;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetDocument(XmlDocument document)
    {
        _document.NodeChanged -= OnDocumentChanged;
        _document.NodeInserted -= OnDocumentChanged;
        _document.NodeRemoved -= OnDocumentChanged;
        _document = document;
        _document.NodeChanged += OnDocumentChanged;
        _document.NodeInserted += OnDocumentChanged;
        _document.NodeRemoved += OnDocumentChanged;
        DocumentReplaced?.Invoke(this, EventArgs.Empty);
    }

    private void OnDocumentChanged(object? sender, XmlNodeChangedEventArgs e)
    {
        if (!XmlDocumentTraversal.IsAttached(_document, e.NewParent) &&
            !XmlDocumentTraversal.IsAttached(_document, e.OldParent))
        {
            return;
        }

        IsDirty = true;
        DocumentChanged?.Invoke(this, e);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static XmlDocument CreateDocument()
    {
        var document = new XmlDocument
        {
            PreserveWhitespace = Settings.Instance.GetBoolean("PreserveWhitespace")
        };
        document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));
        document.AppendChild(document.CreateElement("root"));
        return document;
    }

    private static (XmlDocument Document, Encoding? Encoding) LoadDocument(Stream stream, Uri location, XmlResolver resolver)
    {
        var encoding = XmlDocumentEncoding.ReadPrefix(stream);
        var settings = XmlHelpers.CreateXmlSettings(resolver);
        settings.CheckCharacters = false;
        using var reader = XmlReader.Create(stream, settings, location.AbsoluteUri);
        var document = new SourceXmlDocument
        {
            PreserveWhitespace = Settings.Instance.GetBoolean("PreserveWhitespace"),
            XmlResolver = resolver
        };

        document.Load(reader);
        return (document, encoding);
    }
}
