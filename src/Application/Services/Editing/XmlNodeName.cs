using System.Xml;

namespace XmlNotepad.Services;

internal sealed record XmlNodeName(string Prefix, string LocalName, string NamespaceUri, XmlAttribute? Declaration)
{
    public static XmlNodeName Resolve(XmlNode context, XmlNodeType type, string name, string? schemaNamespace = null)
    {
        var separator = name.IndexOf(':');
        var prefix = separator < 0 ? string.Empty : XmlConvert.VerifyNCName(name[..separator]);
        var localName = XmlConvert.VerifyNCName(name[(separator + 1)..]);
        var isAttribute = type == XmlNodeType.Attribute;
        if (prefix == "xmlns" || isAttribute && prefix.Length == 0 && localName == "xmlns")
        {
            if (!isAttribute)
            {
                throw new XmlException("The xmlns prefix is reserved for namespace declarations.");
            }
            return new(prefix, localName, XmlStandardUris.XmlnsUri, null);
        }

        var scope = XmlHelpers.GetNamespaceScope(context);
        var currentNamespace = prefix.Length == 0 && isAttribute ? "" : scope.LookupNamespace(prefix);
        var namespaceUri = schemaNamespace ?? currentNamespace ?? "";
        if (namespaceUri.Length == 0 && prefix.Length > 0)
        {
            var number = 1;
            do
            {
                namespaceUri = $"uri:{number++}";
            } while (scope.LookupPrefix(namespaceUri) is not null);
        }

        if (isAttribute && namespaceUri.Length > 0 && currentNamespace != namespaceUri)
        {
            prefix = scope.GetNamespacesInScope(XmlNamespaceScope.All)
                .FirstOrDefault(binding => binding.Key.Length > 0 && binding.Value == namespaceUri).Key
                ?? GetAvailablePrefix(scope, prefix);
            currentNamespace = scope.LookupNamespace(prefix);
        }

        if (namespaceUri == (currentNamespace ?? "") || prefix == "xml")
        {
            return new(prefix, localName, namespaceUri, null);
        }

        var document = context as XmlDocument ?? context.OwnerDocument!;
        var declaration = prefix.Length == 0
            ? document.CreateAttribute("", "xmlns", XmlStandardUris.XmlnsUri)
            : document.CreateAttribute("xmlns", prefix, XmlStandardUris.XmlnsUri);
        declaration.Value = namespaceUri;
        return new(prefix, localName, namespaceUri, declaration);
    }

    private static string GetAvailablePrefix(XmlNamespaceManager scope, string preferred)
    {
        if (preferred.Length > 0 && scope.LookupNamespace(preferred) is null)
        {
            return preferred;
        }

        var number = 1;
        string prefix;
        do
        {
            prefix = $"ns{number++}";
        } while (scope.LookupNamespace(prefix) is not null);
        return prefix;
    }
}
