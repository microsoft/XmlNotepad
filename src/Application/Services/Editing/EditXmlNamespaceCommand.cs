using System.Xml;

namespace XmlNotepad.Services;

internal sealed class EditXmlNamespaceCommand : Command
{
    private readonly EditXmlValueCommand? _edit;
    private readonly SourceXmlDocument? _source;
    private readonly Dictionary<XmlNode, XmlNode> _replacements = new();
    private readonly Dictionary<XmlNode, XmlNode> _originals = new();

    public override string Name => "Edit namespace";
    public override bool IsNoop => (_edit?.IsNoop ?? true) && _replacements.Count == 0;

    public EditXmlNamespaceCommand(XmlAttribute declaration, string value, XmlElement? insertionOwner = null,
        XmlAttribute? replacedAttribute = null)
    {
        if (insertionOwner is null)
        {
            _edit = new EditXmlValueCommand(declaration, declaration.Value, value);
        }
        var owner = insertionOwner ?? declaration.OwnerElement
            ?? throw new InvalidOperationException("The namespace declaration is detached.");
        var document = owner.OwnerDocument;
        _source = document as SourceXmlDocument;
        var prefix = declaration.Prefix == "xmlns" ? declaration.LocalName : string.Empty;
        ValidateBinding(prefix, value);

        var pending = new Stack<XmlElement>();
        pending.Push(owner);
        while (pending.TryPop(out var element))
        {
            if (element != owner && element.HasAttribute(declaration.LocalName, XmlStandardUris.XmlnsUri))
            {
                continue;
            }

            if (element.Prefix == prefix && element.NamespaceURI != value)
            {
                AddReplacement(element, document.CreateElement(prefix, element.LocalName, value));
            }

            if (prefix.Length > 0)
            {
                foreach (XmlAttribute attribute in element.Attributes)
                {
                    if (attribute == replacedAttribute || attribute.Prefix != prefix || attribute.NamespaceURI == value)
                    {
                        continue;
                    }

                    if (element.GetAttributeNode(attribute.LocalName, value) is { } conflict && conflict != replacedAttribute)
                    {
                        throw new XmlException($"Changing this namespace would duplicate the attribute '{attribute.Name}'.");
                    }
                    AddReplacement(attribute, document.CreateAttribute(prefix, attribute.LocalName, value));
                }
            }

            for (var child = element.LastChild; child is not null; child = child.PreviousSibling)
            {
                if (child is XmlElement childElement)
                {
                    pending.Push(childElement);
                }
            }
        }
    }

    private static void ValidateBinding(string prefix, string value)
    {
        if (prefix == "xml" ? value != XmlStandardUris.XmlUri :
            prefix == "xmlns" || value is XmlStandardUris.XmlnsUri or XmlStandardUris.XmlUri ||
            (prefix.Length > 0 && value.Length == 0))
        {
            throw new XmlException("This namespace URI is invalid for the declaration.");
        }
        XmlConvert.VerifyXmlChars(value);
    }

    private void AddReplacement(XmlNode original, XmlNode replacement)
    {
        _replacements.Add(original, replacement);
        _originals.Add(replacement, original);
    }

    public XmlNode GetCurrentNode(XmlNode node, bool undo) =>
        (undo ? _originals : _replacements).GetValueOrDefault(node, node);

    public override void Do()
    {
        _edit?.Do();
        foreach (var (original, replacement) in _replacements)
        {
            XmlNodeReplacement.Replace(original, replacement);
        }
        _source?.RemapPositions(_replacements);
    }

    public override void Undo()
    {
        foreach (var (replacement, original) in _originals.Reverse())
        {
            XmlNodeReplacement.Replace(replacement, original);
        }
        _edit?.Undo();
        _source?.RemapPositions(_originals);
    }

    public override void Redo() => Do();
}
