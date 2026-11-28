using System.Xml;
using System.Xml.Schema;

namespace XmlNotepad.Services;

internal sealed record SchemaNodeDocumentation(string Text, IReadOnlyList<SchemaDocumentationLink> Links)
{
    internal static IReadOnlyDictionary<XmlNode, SchemaNodeDocumentation>? Collect(
        XmlDocument source, XmlDocument validated, string? language, Dictionary<XmlElement, XmlAttribute>? identifiers)
    {
        var results = language is null ? null : new Dictionary<XmlNode, SchemaNodeDocumentation>();
        var cache = new Dictionary<XmlSchemaAnnotated, SchemaNodeDocumentation?>();
        var pending = new Stack<(XmlNode Source, XmlNode Validated)>();
        pending.Push((source, validated));
        while (pending.TryPop(out var pair))
        {
            if (pair.Source is XmlElement sourceElement && pair.Validated is XmlElement validatedElement)
            {
                Add(sourceElement, validatedElement.SchemaInfo);
                foreach (XmlAttribute attribute in sourceElement.Attributes)
                {
                    if (validatedElement.GetAttributeNode(attribute.LocalName, attribute.NamespaceURI) is { } typed)
                    {
                        var info = typed.SchemaInfo;
                        Add(attribute, info);
                        if (identifiers is not null && (info.SchemaType?.TypeCode == XmlTypeCode.Id ||
                            info.MemberType?.TypeCode == XmlTypeCode.Id))
                        {
                            identifiers.TryAdd(sourceElement, attribute);
                        }
                    }
                }
            }
            if (pair.Source.NextSibling is { } next && pair.Validated.NextSibling is { } typedNext)
            {
                pending.Push((next, typedNext));
            }
            if (pair.Source.FirstChild is { } child && pair.Validated.FirstChild is { } typedChild)
            {
                pending.Push((child, typedChild));
            }
        }
        return results;

        void Add(XmlNode node, IXmlSchemaInfo? info)
        {
            if (info is null || results is null || language is null)
            {
                return;
            }
            foreach (var candidate in Candidates(info, validated.Schemas))
            {
                if (!cache.TryGetValue(candidate, out var documentation))
                {
                    documentation = ReadAnnotation(candidate, language);
                    cache[candidate] = documentation;
                }
                if (documentation is not null)
                {
                    results.Add(node, documentation);
                    return;
                }
            }
        }
    }

    private static IEnumerable<XmlSchemaAnnotated> Candidates(IXmlSchemaInfo info, XmlSchemaSet schemas)
    {
        if (info.SchemaElement is { } element)
        {
            yield return element;
            if (!element.RefName.IsEmpty && schemas.GlobalElements[element.RefName] is XmlSchemaElement referenced)
            {
                yield return referenced;
            }
        }
        if (info.SchemaAttribute is { } attribute)
        {
            yield return attribute;
            if (!attribute.RefName.IsEmpty && schemas.GlobalAttributes[attribute.RefName] is XmlSchemaAttribute referenced)
            {
                yield return referenced;
            }
        }
        if (info.MemberType is { } memberType)
        {
            yield return memberType;
        }
        if (info.SchemaType is { } schemaType)
        {
            yield return schemaType;
        }
    }

    private static SchemaNodeDocumentation? ReadAnnotation(XmlSchemaAnnotated annotated, string language)
    {
        var entries = SchemaCache.GetDocumentation(annotated, language).ToArray();
        if (entries.Length == 0 && language.Length != 0)
        {
            entries = SchemaCache.GetDocumentation(annotated, "").Where(entry => string.IsNullOrEmpty(entry.Language)).ToArray();
        }
        var blocks = new List<string>();
        var links = new Dictionary<Uri, SchemaDocumentationLink>();
        var source = annotated.SourceUri;
        for (var parent = annotated.Parent; string.IsNullOrEmpty(source) && parent is not null; parent = parent.Parent)
        {
            source = parent.SourceUri;
        }
        foreach (var entry in entries)
        {
            AddLink(entry.Source, "Documentation source");
            foreach (var node in entry.Markup ?? [])
            {
                var text = node?.InnerText.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    blocks.Add(node!.LocalName switch
                    {
                        "tooltip" => $"Summary{Environment.NewLine}{text}",
                        "remarks" => $"Remarks{Environment.NewLine}{text}",
                        _ => text
                    });
                }
                if (node?.SelectNodes("descendant-or-self::*[@href]") is { } elements)
                {
                    foreach (XmlElement element in elements)
                    {
                        if (element.LocalName == "a")
                        {
                            AddLink(element.GetAttribute("href"), element.InnerText.Trim());
                        }
                    }
                }
            }
        }
        return blocks.Count == 0 && links.Count == 0
            ? null : new(string.Join(Environment.NewLine + Environment.NewLine, blocks), links.Values.ToArray());

        void AddLink(string? href, string label)
        {
            if (string.IsNullOrWhiteSpace(href))
            {
                return;
            }
            if (!Uri.TryCreate(href, UriKind.Absolute, out var uri) &&
                (!Uri.TryCreate(source, UriKind.Absolute, out var baseUri) || !Uri.TryCreate(baseUri, href, out uri)))
            {
                return;
            }
            if (RemoteDocument.IsWebUri(uri))
            {
                links.TryAdd(uri, new(label.Length > 0 ? label : uri.AbsoluteUri, uri));
            }
        }
    }
}

internal sealed record SchemaDocumentationLink(string Label, Uri Location);
