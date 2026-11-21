using System.Xml;
using System.Xml.Schema;

namespace XmlNotepad.Services;

public sealed partial class XmlIntellisenseService
{
    private readonly SchemaCache _schemaCache;
    private readonly IServiceProvider _services;
    private XmlCache _model;
    private XmlDocument? _document;

    public XmlIntellisenseService(SchemaCache schemaCache, IServiceProvider services)
    {
        _schemaCache = schemaCache;
        _services = services;
        _model = new XmlCache(services, schemaCache);
    }

    public IReadOnlyList<XmlSuggestion> GetExpectedNames(XmlDocument document, XmlNode parent, XmlNode? nextSibling, XmlNodeType type, Uri? remoteSource)
    {
        EnsureDocument(document, remoteSource);
        if (parent is XmlDocument && type == XmlNodeType.Element)
        {
            return _schemaCache.GetPossibleTopLevelElements()
                .Select(element => new XmlSuggestion(
                    element.QualifiedName.Name,
                    element.QualifiedName.Namespace,
                    SchemaCache.GetAnnotation(element, SchemaCache.AnnotationNode.Tooltip, null)))
                .ToArray();
        }

        if (parent is not XmlElement element)
        {
            return [];
        }
        var scope = XmlHelpers.GetNamespaceScope(element);
        if (type == XmlNodeType.Attribute)
        {
            var checker = new Checker(element, IntellisensePosition.FirstChild);
            checker.ValidateContext(_model);
            return (checker.GetExpectedAttributes() ?? [])
                .Select(attribute => new XmlSuggestion(
                    GetQualifiedName(attribute.QualifiedName, scope),
                    attribute.QualifiedName.Namespace,
                    SchemaCache.GetAnnotation(attribute, SchemaCache.AnnotationNode.Tooltip, null)))
                .ToArray();
        }

        var preceding = nextSibling is null ? element.LastChild : nextSibling.PreviousSibling;
        while (preceding is not null && preceding is not XmlElement)
        {
            preceding = preceding.PreviousSibling;
        }
        var childChecker = preceding is XmlElement precedingElement
            ? new Checker(precedingElement, IntellisensePosition.AfterNode)
            : new Checker(element, IntellisensePosition.FirstChild);
        childChecker.ValidateContext(_model);
        return (childChecker.GetExpectedParticles() ?? [])
            .OfType<XmlSchemaElement>()
            .Select(child => new XmlSuggestion(GetQualifiedName(child.QualifiedName, scope), child.QualifiedName.Namespace,
                SchemaCache.GetAnnotation(child, SchemaCache.AnnotationNode.Tooltip, null)))
            .OrderBy(suggestion => suggestion.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void EnsureDocument(XmlDocument document, Uri? remoteSource)
    {
        if (ReferenceEquals(_document, document))
        {
            return;
        }

        var cache = remoteSource is null ? _schemaCache
            : new SchemaCache(_services, new RemoteXmlResolver(new XmlUrlResolver()));
        _model = new XmlCache(_services, cache);
        _model.Load(document);
        _document = document;
    }

    internal IReadOnlyList<XmlSuggestion> GetExpectedValues(XmlDocument document, XmlNode node, Uri? remoteSource)
    {
        if (node is XmlText or XmlCDataSection && node.ParentNode is XmlElement owner)
        {
            node = owner;
        }

        var element = node is XmlAttribute attribute ? attribute.OwnerElement : node as XmlElement;
        if (element is null)
        {
            return [];
        }

        EnsureDocument(document, remoteSource);
        if (document.Schemas.Count == 0 && _model.SchemaCache.GetSchemas().All(entry => entry.Disabled) &&
            document.DocumentElement is { } root &&
            !root.HasAttribute("schemaLocation", XmlSchema.InstanceNamespace) &&
            !root.HasAttribute("noNamespaceSchemaLocation", XmlSchema.InstanceNamespace))
        {
            return [];
        }

        using var checker = new Checker(element, IntellisensePosition.OnNode);
        var resolver = (SchemaResolver)_model.SchemaResolver;
        var previousHandler = resolver.Handler;
        try
        {
            checker.ValidateContext(_model);
            if (checker.GetTypeInfo(node) is not { } info)
            {
                return [];
            }
            var fixedValue = info.SchemaAttribute?.FixedValue ?? info.SchemaElement?.FixedValue;
            if (fixedValue is not null)
            {
                return new[] { new XmlSuggestion(fixedValue, null, "Fixed by the schema") };
            }

            var suggestions = new List<XmlSuggestion>();
            var defaultValue = info.SchemaAttribute?.DefaultValue ?? info.SchemaElement?.DefaultValue;
            if (defaultValue is not null)
            {
                suggestions.Add(new XmlSuggestion(defaultValue, null, "Schema default"));
            }
            var values = _model.SchemaCache.GetExpectedValues(info.SchemaType);
            if (values is not null)
            {
                for (var index = 0; index < values.Count; index++)
                {
                    suggestions.Add(new XmlSuggestion(values.GetName(index), null, values.GetTooltip(index)));
                }
            }
            return suggestions.DistinctBy(suggestion => suggestion.Name, StringComparer.Ordinal).ToArray();
        }
        finally
        {
            resolver.Handler = previousHandler;
        }
    }

    private static string GetQualifiedName(XmlQualifiedName name, XmlNamespaceManager scope) =>
        scope.LookupPrefix(name.Namespace) is { Length: > 0 } prefix ? $"{prefix}:{name.Name}" : name.Name;

}

public sealed record XmlSuggestion(string Name, string? Namespace, string? Tooltip)
{
    public override string ToString() => Name;
}
