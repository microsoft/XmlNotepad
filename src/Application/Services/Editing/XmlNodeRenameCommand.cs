using System.Xml;

namespace XmlNotepad.Services;

public sealed class XmlNodeRenameCommand : Command
{
    private readonly XmlAttribute? _declaration;
    private readonly EditXmlNamespaceCommand? _binding;

    public override string Name => "Rename node";
    public override bool IsNoop => Original.Name == Replacement.Name && Original.NamespaceURI == Replacement.NamespaceURI;
    public XmlNode Original { get; }
    public XmlNode Replacement { get; }

    public XmlNodeRenameCommand(XmlNode original, string newName)
    {
        Original = original;
        var document = original.OwnerDocument!;
        switch (original)
        {
            case XmlElement element:
                var elementName = XmlNodeName.Resolve(element, XmlNodeType.Element, newName);
                Replacement = document.CreateElement(elementName.Prefix, elementName.LocalName, elementName.NamespaceUri);
                _declaration = elementName.Declaration;
                break;
            case XmlAttribute { OwnerElement: { } owner } attribute:
                var attributeName = XmlNodeName.Resolve(owner, XmlNodeType.Attribute, newName);
                if (owner.GetAttributeNode(attributeName.LocalName, attributeName.NamespaceUri) is { } conflict && conflict != attribute)
                {
                    throw new InvalidOperationException($"The element already has an attribute named '{newName}'.");
                }
                var renamedAttribute = document.CreateAttribute(attributeName.Prefix, attributeName.LocalName, attributeName.NamespaceUri);
                Replacement = renamedAttribute;
                if (attributeName.NamespaceUri == XmlStandardUris.XmlnsUri && !IsNoop)
                {
                    _binding = new EditXmlNamespaceCommand(renamedAttribute, attribute.Value, owner, attribute);
                }
                _declaration = attributeName.Declaration;
                break;
            case XmlProcessingInstruction instruction:
                XmlConvert.VerifyName(newName);
                Replacement = document.CreateProcessingInstruction(newName, instruction.Data);
                break;
            default:
                throw new InvalidOperationException("Only elements, attributes, and processing instructions can be renamed.");
        }
    }

    public override void Do()
    {
        Replace(Original, Replacement);
        if (_declaration is not null)
        {
            var owner = Replacement as XmlElement ?? ((XmlAttribute)Replacement).OwnerElement!;
            owner.Attributes.Append(_declaration);
        }
        _binding?.Do();
    }

    public override void Undo()
    {
        _binding?.Undo();
        _declaration?.OwnerElement?.Attributes.Remove(_declaration);
        Replace(Replacement, Original);
    }

    public override void Redo() => Do();

    internal XmlNode GetCurrentNode(XmlNode node, bool undo)
    {
        if (node == (undo ? Replacement : Original))
        {
            return undo ? Original : Replacement;
        }
        return _binding?.GetCurrentNode(node, undo) ?? node;
    }

    private static void Replace(XmlNode from, XmlNode to)
    {
        XmlNodeReplacement.Replace(from, to);
        if (to.OwnerDocument is SourceXmlDocument source)
        {
            source.RemapPositions(new Dictionary<XmlNode, XmlNode> { [from] = to });
        }
    }
}
