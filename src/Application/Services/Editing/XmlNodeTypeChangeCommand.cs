using System.Xml;

namespace XmlNotepad.Services;

internal sealed class XmlNodeTypeChangeCommand : Command
{
    private readonly XmlStructureCommand _remove;
    private readonly XmlStructureCommand _insert;

    internal XmlNode Original { get; }
    internal XmlNode Replacement { get; }
    public override string Name => "Change node type";
    public override bool IsNoop => false;

    internal XmlNodeTypeChangeCommand(XmlNode original, XmlNodeType target)
    {
        if (!CanConvert(original, target))
        {
            throw new InvalidOperationException("That node type cannot be used at this location.");
        }
        Original = original;
        var parent = original is XmlAttribute attribute ? attribute.OwnerElement! : original.ParentNode!;
        Replacement = ConvertNode(original, target, parent);
        if (Replacement is XmlAttribute replacement && ((XmlElement)parent).HasAttribute(replacement.LocalName, replacement.NamespaceURI))
        {
            throw new InvalidOperationException($"The element already has an attribute named '{replacement.Name}'.");
        }
        _remove = XmlStructureCommand.Remove(Name, original);
        var next = target == XmlNodeType.Attribute ? null :
            original is XmlAttribute ? parent.FirstChild : original.NextSibling;
        _insert = XmlStructureCommand.Insert(Name, parent, Replacement, next);
    }

    internal static bool CanConvert(XmlNode source, XmlNodeType target)
    {
        if (source.NodeType == target || source is XmlDocument or XmlDocumentType or XmlDeclaration ||
            source is XmlAttribute { NamespaceURI: XmlStandardUris.XmlnsUri })
        {
            return false;
        }
        var parent = source is XmlAttribute attribute ? attribute.OwnerElement : source.ParentNode;
        return parent switch
        {
            XmlElement => target is XmlNodeType.Element or XmlNodeType.Attribute or XmlNodeType.Text or
                XmlNodeType.CDATA or XmlNodeType.Comment or XmlNodeType.ProcessingInstruction,
            XmlDocument document => target is XmlNodeType.Comment or XmlNodeType.ProcessingInstruction ||
                target == XmlNodeType.Element && document.DocumentElement is null,
            _ => false
        };
    }

    public override void Do()
    {
        _remove.Do();
        try
        {
            _insert.Do();
        }
        catch
        {
            _remove.Undo();
            throw;
        }
        MapPosition(Original, Replacement);
    }

    public override void Undo()
    {
        _insert.Undo();
        _remove.Undo();
        MapPosition(Replacement, Original);
    }

    public override void Redo() => Do();

    private static XmlNode ConvertNode(XmlNode source, XmlNodeType target, XmlNode parent)
    {
        var document = source.OwnerDocument!;
        if (IsNamed(target) && !IsNamed(source.NodeType) && TryReadNamedNode(source, parent) is { } parsed)
        {
            if (parsed.NodeType == target)
            {
                return parsed;
            }
            source = parsed;
        }

        var named = IsNamed(source.NodeType);
        var value = source is XmlElement element && element.ChildNodes.Cast<XmlNode>().Any(child =>
            child.NodeType is not (XmlNodeType.Text or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace))
            ? element.InnerXml : source.InnerText;
        var markup = named ? source.OuterXml : value;
        switch (target)
        {
            case XmlNodeType.Element:
                var replacement = document.CreateElement(source.Prefix, named ? source.LocalName : "element", source.NamespaceURI);
                replacement.InnerText = value;
                return replacement;
            case XmlNodeType.Attribute:
                var attribute = document.CreateAttribute(source.Prefix, named ? source.LocalName : "attribute", source.NamespaceURI);
                attribute.Value = value;
                return attribute;
            case XmlNodeType.ProcessingInstruction:
                if (value.Contains("?>", StringComparison.Ordinal))
                {
                    throw new XmlException("The value contains the processing instruction terminator '?>'.");
                }
                return document.CreateProcessingInstruction(named ? source.LocalName : "pi", value);
            case XmlNodeType.CDATA:
                if (markup.Contains("]]>", StringComparison.Ordinal))
                {
                    markup = markup.Replace("<![CDATA[", "/[", StringComparison.Ordinal).Replace("]]>", "]/", StringComparison.Ordinal);
                }
                return document.CreateCDataSection(markup);
            case XmlNodeType.Comment:
                markup = markup.Replace("<!--", "/*", StringComparison.Ordinal).Replace("-->", "*/", StringComparison.Ordinal);
                if (markup.Contains("--", StringComparison.Ordinal) || markup.EndsWith('-'))
                {
                    throw new XmlException("The value contains a sequence that cannot be represented in an XML comment.");
                }
                return document.CreateComment(markup);
            default:
                return document.CreateTextNode(markup);
        }
    }

    private static XmlNode? TryReadNamedNode(XmlNode source, XmlNode context)
    {
        var content = source.InnerText;
        if (source is XmlComment)
        {
            content = content.Replace("/*", "<!--", StringComparison.Ordinal).Replace("*/", "-->", StringComparison.Ordinal);
        }
        else if (source is XmlCDataSection)
        {
            content = content.Replace("/[", "<![CDATA[", StringComparison.Ordinal).Replace("]/", "]]>", StringComparison.Ordinal);
        }
        try
        {
            return XmlClipboardService.Parse(source.OwnerDocument!, context, content) is [var named] && IsNamed(named.NodeType)
                ? named : null;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private static bool IsNamed(XmlNodeType type) => type is XmlNodeType.Element or XmlNodeType.Attribute or XmlNodeType.ProcessingInstruction;

    private static void MapPosition(XmlNode from, XmlNode to)
    {
        if (from.OwnerDocument is SourceXmlDocument source)
        {
            source.RemapPositions(new Dictionary<XmlNode, XmlNode> { [from] = to });
        }
    }
}
