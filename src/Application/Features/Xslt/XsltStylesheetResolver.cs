using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace XmlNotepad.Services;

internal sealed class XsltStylesheetResolver(XmlResolver resolver) : XmlResolver
{
    private static readonly XNamespace TransformNamespace = "http://www.w3.org/1999/XSL/Transform";
    private static readonly XName ScriptElement = XName.Get("script", "urn:schemas-microsoft-com:xslt");

    internal ScriptedStylesheetException? ScriptError { get; private set; }

    public override ICredentials? Credentials
    {
        set => resolver.Credentials = value!;
    }

    public override Uri ResolveUri(Uri? baseUri, string? relativeUri) => resolver.ResolveUri(baseUri, relativeUri);

    public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn) => Open(absoluteUri);

    internal XmlReader Open(Uri location)
    {
        XmlResourceResolver.ValidateUri(location);
        using var input = XmlHelpers.ReadXml(location.AbsoluteUri, resolver);
        var sheet = XDocument.Load(input, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        if (sheet.Root is { } root &&
            (root.Name == TransformNamespace + "stylesheet" || root.Name == TransformNamespace + "transform") &&
            root.Element(ScriptElement) is not null)
        {
            ScriptError = new ScriptedStylesheetException(location);
            throw ScriptError;
        }
        return sheet.CreateReader();
    }
}

internal sealed class ScriptedStylesheetException(Uri location, Uri? trustedEntryPoint = null, string? reason = null) : XsltException(
    $"'{(location.IsFile ? location.LocalPath : location.AbsoluteUri)}' contains an embedded msxsl:script block. " +
    (reason ?? "The .NET 11 XSLT engine cannot run embedded scripts."))
{
    internal Uri Location { get; } = location;
    internal Uri? TrustedEntryPoint { get; } = trustedEntryPoint;
}
