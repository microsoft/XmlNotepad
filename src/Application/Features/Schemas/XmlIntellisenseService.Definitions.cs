using System.Xml;
using System.Xml.Schema;

namespace XmlNotepad.Services;

public sealed partial class XmlIntellisenseService
{
    internal XmlDefinitionTarget? GetDefinition(XmlDocument document, XmlNode node, string? path, Uri? remoteSource)
    {
        if (node is XmlText or XmlCDataSection && node.ParentNode is XmlElement parent)
        {
            node = parent;
        }

        var baseUri = RemoteDocument.GetBaseUri(path, remoteSource);
        var element = node is XmlAttribute attribute ? attribute.OwnerElement : node as XmlElement;
        if (element is null)
        {
            return null;
        }
        if (element is { LocalName: "include", NamespaceURI: XmlIncludeReader.XIncludeNamespaceUri } &&
            element.GetAttributeNode("href") is { Value.Length: > 0 } href)
        {
            return ResolveDefinitionLocation(element, baseUri, href.Value, remoteSource, isInclude: true);
        }
        if (node is XmlAttribute { NamespaceURI: XmlSchema.InstanceNamespace } location)
        {
            if (location.LocalName == "noNamespaceSchemaLocation")
            {
                return ResolveDefinitionLocation(element, baseUri, location.Value, remoteSource);
            }
            if (location.LocalName == "schemaLocation")
            {
                var pairs = location.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (pairs.Length < 2 || pairs.Length % 2 != 0)
                {
                    return null;
                }
                var selectedLocation = pairs[1];
                for (var index = 0; index < pairs.Length; index += 2)
                {
                    if (pairs[index] == element.NamespaceURI)
                    {
                        selectedLocation = pairs[index + 1];
                        break;
                    }
                }
                return ResolveDefinitionLocation(element, baseUri, selectedLocation, remoteSource);
            }
        }

        if (node is not (XmlElement or XmlAttribute))
        {
            return null;
        }
        EnsureDocument(document, remoteSource);
        using var checker = new Checker(null);
        var resolver = (SchemaResolver)_model.SchemaResolver;
        var previousHandler = resolver.Handler;
        try
        {
            checker.Validate(_model);
            var info = checker.GetTypeInfo(node);
            if (FindIdDefinition(node, checker, _model.TypeInfoMap) is { } target)
            {
                return new XmlDefinitionTarget(Node: target);
            }

            XmlSchemaObject? declaration = (XmlSchemaObject?)info?.SchemaElement ?? (XmlSchemaObject?)info?.SchemaAttribute
                ?? (XmlSchemaObject?)info?.MemberType ?? info?.SchemaType;
            for (var source = declaration; source is not null; source = source.Parent)
            {
                if (!string.IsNullOrWhiteSpace(source.SourceUri))
                {
                    return ResolveDefinitionLocation(element, baseUri, source.SourceUri, remoteSource) is { } result
                        ? result with { Line = declaration!.LineNumber, Column = declaration.LinePosition }
                        : null;
                }
            }
            return null;
        }
        finally
        {
            resolver.Handler = previousHandler;
        }
    }

    private static XmlNode? FindIdDefinition(XmlNode node, Checker checker, Dictionary<XmlNode, XmlSchemaInfo> types)
    {
        var references = new HashSet<string>(StringComparer.Ordinal);
        AddReference(node);
        if (node is XmlElement element)
        {
            foreach (XmlAttribute attribute in element.Attributes)
            {
                AddReference(attribute);
            }
        }
        if (references.Count == 0)
        {
            return null;
        }
        foreach (var (candidate, info) in types)
        {
            if (info.SchemaType?.Datatype?.TokenizedType == XmlTokenizedType.ID &&
                references.Contains((candidate.Value ?? candidate.InnerText).Trim()))
            {
                return candidate is XmlAttribute attribute ? attribute.OwnerElement : candidate;
            }
        }
        return null;

        void AddReference(XmlNode reference)
        {
            var info = checker.GetTypeInfo(reference);
            if (info?.SchemaType?.Datatype?.TokenizedType is XmlTokenizedType.IDREF or XmlTokenizedType.IDREFS)
            {
                references.UnionWith((reference.Value ?? reference.InnerText)
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            }
        }
    }

    private static XmlDefinitionTarget? ResolveDefinitionLocation(XmlElement element, Uri baseUri, string value,
        Uri? remoteSource, bool isInclude = false)
    {
        if (Uri.TryCreate(element.BaseURI, UriKind.Absolute, out var documentBase))
        {
            baseUri = documentBase;
        }
        if (!Uri.TryCreate(baseUri, value.Trim(), out var location) ||
            !(location.IsFile && remoteSource is null || RemoteDocument.IsWebUri(location)))
        {
            return null;
        }
        return new XmlDefinitionTarget(Location: location, IsInclude: isInclude);
    }
}

internal sealed record XmlDefinitionTarget(XmlNode? Node = null, Uri? Location = null,
    int Line = 0, int Column = 0, bool IsInclude = false);
