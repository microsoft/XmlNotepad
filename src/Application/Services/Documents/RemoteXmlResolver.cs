using System.Net;
using System.Xml;

namespace XmlNotepad.Services;

internal sealed class RemoteXmlResolver(XmlResolver inner) : XmlResolver
{
    public override ICredentials? Credentials
    {
        set => inner.Credentials = value!;
    }

    public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
    {
        var uri = inner.ResolveUri(baseUri, relativeUri);
        EnsureWebUri(uri);
        return uri;
    }

    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        EnsureWebUri(absoluteUri);
        return inner.GetEntity(absoluteUri, role, ofObjectToReturn);
    }

    public override Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        EnsureWebUri(absoluteUri);
        return inner.GetEntityAsync(absoluteUri, role, ofObjectToReturn);
    }

    internal static void EnsureWebUri(Uri uri)
    {
        if (!RemoteDocument.IsWebUri(uri))
        {
            throw new XmlException("References in web documents must use HTTP or HTTPS.");
        }
    }
}
