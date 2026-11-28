using System.Globalization;
using System.Xml;
using System.Xml.Schema;
using XmlNotepad;

namespace XmlNotepad.Services;

public sealed class XmlValidationService
{
    private readonly SchemaCache _schemaCache;

    public XmlValidationService(SchemaCache schemaCache)
    {
        _schemaCache = schemaCache;
    }

    public XmlValidationResult Validate(XmlDocument document, string? filePath, Uri? remoteSource,
        string? documentationLanguage = null, bool collectIdentifiers = false)
    {
        var issues = new List<XmlValidationIssue>();
        IReadOnlyDictionary<XmlNode, SchemaNodeDocumentation>? documentation = null;
        var identifiers = collectIdentifiers ? new Dictionary<XmlElement, XmlAttribute>() : null;
        XmlResolver resolver = remoteSource is null ? _schemaCache.Resolver : new RemoteXmlResolver(_schemaCache.Resolver);
        var schemas = new XmlSchemaSet { XmlResolver = resolver };

        foreach (XmlSchema schema in document.Schemas.Schemas())
        {
            schemas.Add(schema);
        }

        schemas.ValidationEventHandler += (_, args) =>
        {
            if (args.Severity == XmlSeverityType.Error)
            {
                throw args.Exception;
            }
            issues.Add(new XmlValidationIssue(args.Severity, args.Message));
        };

        try
        {
            var root = document.DocumentElement;
            if (root is null)
            {
                return new XmlValidationResult(false, []);
            }
            AddReferencedSchemas(document, filePath, remoteSource, schemas);
            var selectedNamespaces = schemas.Schemas().Cast<XmlSchema>()
                .Select(schema => schema.TargetNamespace ?? string.Empty)
                .ToHashSet(StringComparer.Ordinal);
            var schemaEntries = _schemaCache.GetSchemas();
            if (schemaEntries.Count > 0)
            {
                var documentNamespaces = GetDocumentNamespaces(document);
                foreach (var entry in schemaEntries)
                {
                    var ns = entry.TargetNamespace ?? string.Empty;
                    if (!entry.Disabled && documentNamespaces.Contains(ns) && !selectedNamespaces.Contains(ns) &&
                        (remoteSource is null || RemoteDocument.IsWebUri(entry.Location)) &&
                        resolver.GetEntity(entry.Location, null, typeof(XmlSchema)) is XmlSchema registeredSchema)
                    {
                        if (ns != root.NamespaceURI || CanSupplyRoot(registeredSchema, root.LocalName))
                        {
                            schemas.Add(registeredSchema);
                        }
                    }
                }
            }
            if (schemas.Count == 0)
            {
                return CompleteResult(false, issues, document, filePath, remoteSource);
            }

            schemas.Compile();
            if (schemas.GlobalElements[new XmlQualifiedName(root.LocalName, root.NamespaceURI)] is null)
            {
                return CompleteResult(false, issues, document, filePath, remoteSource);
            }

            // DOM validation can insert schema defaults. Keep those changes off the editable tree.
            var validationDocument = (XmlDocument)document.CloneNode(true);
            validationDocument.Schemas = schemas;
            try
            {
                validationDocument.Validate((_, args) => issues.Add(new XmlValidationIssue(args.Severity, args.Message,
                    (args.Exception as XmlSchemaValidationException)?.SourceObject as XmlNode)));
            }
            finally
            {
                RestoreSourceNodes(issues, document, validationDocument);
            }
            if (documentationLanguage is not null || identifiers is not null)
            {
                documentation = SchemaNodeDocumentation.Collect(document, validationDocument, documentationLanguage, identifiers);
            }
        }
        catch (XmlException exception)
        {
            issues.Add(new XmlValidationIssue(XmlSeverityType.Error, exception.Message));
        }
        catch (XmlSchemaException exception)
        {
            issues.Add(new XmlValidationIssue(XmlSeverityType.Error, exception.Message));
        }

        return CompleteResult(true, issues, document, filePath, remoteSource) with
        {
            Documentation = documentation,
            Identifiers = identifiers
        };
    }

    private static XmlValidationResult CompleteResult(bool hasSchema, List<XmlValidationIssue> issues,
        XmlDocument document, string? filePath, Uri? remoteSource)
    {
        XmlDocumentCharacterIssues.AddTo(document, issues);
        var label = remoteSource is not null ? RemoteDocument.GetFileName(remoteSource) : Path.GetFileName(filePath) ?? "Untitled";
        var positions = (document as SourceXmlDocument)?.GetPositions(issues.Select(issue => issue.Node).OfType<XmlNode>());
        for (var index = 0; index < issues.Count; index++)
        {
            var issue = issues[index];
            if (issue.Node is { } node && positions is not null && positions.TryGetValue(node, out var position))
            {
                issue = issue with
                {
                    Line = position.Line.ToString(CultureInfo.InvariantCulture),
                    Column = position.Column.ToString(CultureInfo.InvariantCulture)
                };
            }
            issues[index] = issue with { FileName = label };
        }
        return new XmlValidationResult(hasSchema, issues);
    }

    private static bool CanSupplyRoot(XmlSchema schema, string name)
    {
        var declarations = schema.Items.OfType<XmlSchemaElement>().ToArray();
        return declarations.Length == 0 || schema.Includes.Count > 0 || declarations.Any(element => element.Name == name);
    }

    private static HashSet<string> GetDocumentNamespaces(XmlDocument document)
    {
        var namespaces = new HashSet<string>(StringComparer.Ordinal);
        for (var node = document.DocumentElement as XmlNode; node is not null;)
        {
            if (node is XmlElement element)
            {
                namespaces.Add(element.NamespaceURI);
                foreach (XmlAttribute attribute in element.Attributes)
                {
                    if (attribute.NamespaceURI.Length > 0 && attribute.NamespaceURI != XmlStandardUris.XmlnsUri)
                    {
                        namespaces.Add(attribute.NamespaceURI);
                    }
                }
            }

            if (node.FirstChild is { } child)
            {
                node = child;
                continue;
            }
            while (node is not null && node.NextSibling is null)
            {
                node = node.ParentNode;
            }
            node = node?.NextSibling;
        }
        return namespaces;
    }

    private static void RestoreSourceNodes(List<XmlValidationIssue> issues, XmlDocument source, XmlDocument validated)
    {
        if (issues.Count == 0)
        {
            return;
        }

        var pending = issues.Select(issue => issue.Node is XmlAttribute attribute ? attribute.OwnerElement : issue.Node)
            .OfType<XmlNode>().ToHashSet();
        var mapped = new Dictionary<XmlNode, XmlNode>();
        var stack = new Stack<(XmlNode Source, XmlNode Validated)>();
        stack.Push((source, validated));
        while (pending.Count > 0 && stack.TryPop(out var pair))
        {
            if (pending.Remove(pair.Validated))
            {
                mapped.Add(pair.Validated, pair.Source);
            }

            if (pair.Source.NextSibling is { } nextSource && pair.Validated.NextSibling is { } nextValidated)
            {
                stack.Push((nextSource, nextValidated));
            }
            if (pair.Source.FirstChild is { } firstSource && pair.Validated.FirstChild is { } firstValidated)
            {
                stack.Push((firstSource, firstValidated));
            }
        }

        for (int index = 0; index < issues.Count; index++)
        {
            var issue = issues[index];
            XmlNode? target = null;
            if (issue.Node is XmlAttribute attribute && attribute.OwnerElement is { } owner &&
                mapped.GetValueOrDefault(owner) is XmlElement sourceElement)
            {
                target = sourceElement.GetAttributeNode(attribute.LocalName, attribute.NamespaceURI) ?? (XmlNode)sourceElement;
            }
            else if (issue.Node is { } node)
            {
                target = mapped.GetValueOrDefault(node);
            }

            issues[index] = issue with { Node = target };
        }
    }

    private static void AddReferencedSchemas(XmlDocument document, string? filePath, Uri? remoteSource, XmlSchemaSet schemas)
    {
        var root = document.DocumentElement;
        if (root is null)
        {
            return;
        }

        var schemaLocation = root.GetAttribute("schemaLocation", "http://www.w3.org/2001/XMLSchema-instance");
        var locations = schemaLocation.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (locations.Length % 2 != 0)
        {
            throw new XmlException("The xsi:schemaLocation attribute must contain namespace and location pairs.");
        }

        for (var i = 0; i < locations.Length; i += 2)
        {
            schemas.Add(locations[i], ResolveSchemaLocation(filePath, remoteSource, document.BaseURI, locations[i + 1]));
        }

        var noNamespaceLocation = root.GetAttribute("noNamespaceSchemaLocation", "http://www.w3.org/2001/XMLSchema-instance");
        if (!string.IsNullOrWhiteSpace(noNamespaceLocation))
        {
            schemas.Add(null, ResolveSchemaLocation(filePath, remoteSource, document.BaseURI, noNamespaceLocation));
        }
    }

    private static string ResolveSchemaLocation(string? filePath, Uri? remoteSource, string baseUri, string location)
    {
        if (remoteSource is not null)
        {
            var resolved = new Uri(remoteSource, location);
            RemoteXmlResolver.EnsureWebUri(resolved);
            return resolved.AbsoluteUri;
        }

        if (Uri.TryCreate(location, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.AbsoluteUri;
        }

        var basePath = !string.IsNullOrWhiteSpace(filePath)
            ? Path.GetFullPath(filePath)
            : Uri.TryCreate(baseUri, UriKind.Absolute, out var documentUri) && documentUri.IsFile
                ? documentUri.LocalPath
                : Environment.CurrentDirectory;
        var baseDirectory = Directory.Exists(basePath) ? basePath : Path.GetDirectoryName(basePath) ?? Environment.CurrentDirectory;
        return new Uri(Path.GetFullPath(Path.Combine(baseDirectory, location))).AbsoluteUri;
    }
}

public sealed record XmlValidationIssue(XmlSeverityType? Severity, string Message, XmlNode? Node = null)
{
    public string SeverityLabel => Severity?.ToString() ?? "Info";
    public string Summary => $"{SeverityLabel}: {Message}";
    public string FileName { get; init; } = "";
    public string Line { get; init; } = "";
    public string Column { get; init; } = "";
}

public sealed record XmlValidationResult(bool HasSchema, IReadOnlyList<XmlValidationIssue> Issues)
{
    internal IReadOnlyDictionary<XmlNode, SchemaNodeDocumentation>? Documentation { get; init; }
    internal IReadOnlyDictionary<XmlElement, XmlAttribute>? Identifiers { get; init; }
    public int ErrorCount => Issues.Count(issue => issue.Severity == XmlSeverityType.Error);

    public int WarningCount => Issues.Count(issue => issue.Severity == XmlSeverityType.Warning);
}
