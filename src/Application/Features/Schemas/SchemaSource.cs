using System.Xml;
using System.Xml.Schema;

namespace XmlNotepad.Services;

internal static class SchemaSource
{
    internal static Uri ParseLocation(string value)
    {
        if (Uri.TryCreate(value.Trim().Trim('"'), UriKind.Absolute, out var location) &&
            (location.IsFile || RemoteDocument.IsWebUri(location)))
        {
            XmlResourceResolver.ValidateUri(location);
            return location;
        }
        throw new XmlException("Enter an absolute file path or an HTTP or HTTPS URL.");
    }

    internal static Uri ParseClipboardLine(string value)
    {
        var line = value.Trim();
        var separator = line.IndexOfAny(' ', '\t');
        if (separator > 0 && Uri.TryCreate(line[..separator], UriKind.Absolute, out var prefix) && !prefix.IsFile &&
            Uri.TryCreate(line[(separator + 1)..].Trim().Trim('"'), UriKind.Absolute, out var suffix) &&
            (suffix.IsFile || RemoteDocument.IsWebUri(suffix)))
        {
            return ParseLocation(suffix.AbsoluteUri);
        }
        return ParseLocation(line);
    }

    internal static XmlSchema Read(Uri location, XmlResolver resolver)
    {
        XmlResourceResolver.ValidateUri(location);
        using var reader = XmlReader.Create(location.AbsoluteUri, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = resolver
        });
        return XmlSchema.Read(reader, (_, args) => throw args.Exception)
            ?? throw new XmlSchemaException("The document does not contain an XML schema.");
    }

    internal static XmlResolver CreateResolver(Uri location, XmlResolver resolver)
    {
        var restricted = new XmlResourceResolver(resolver);
        return location.IsFile ? restricted : new RemoteXmlResolver(restricted);
    }
}
