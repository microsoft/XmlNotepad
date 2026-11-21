using System.Xml;
using XmlNotepad;

namespace XmlNotepad.Services;

public sealed class XmlIncludeService
{
    public XmlDocument Expand(XmlDocument document, string? filePath, Uri? remoteSource)
    {
        XmlResolver resolver = Settings.Instance.Resolver ?? new XmlUrlResolver();
        if (remoteSource is not null)
        {
            resolver = new RemoteXmlResolver(resolver);
        }
        var location = RemoteDocument.GetBaseUri(filePath, remoteSource);
        using var reader = XmlIncludeReader.CreateIncludeReader(document, XmlHelpers.CreateXmlSettings(resolver), location.AbsoluteUri, estimateSize: false);
        var expanded = new SourceXmlDocument
        {
            PreserveWhitespace = document.PreserveWhitespace,
            XmlResolver = new XmlResourceResolver(resolver)
        };
        expanded.Load(reader);
        return expanded;
    }
}
