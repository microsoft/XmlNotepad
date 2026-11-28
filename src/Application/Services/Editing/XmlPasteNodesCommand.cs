using System.Xml;
using XmlNotepad;

namespace XmlNotepad.Services;

public sealed class XmlPasteNodesCommand : Command
{
    private readonly XmlNode _parent;
    private readonly IReadOnlyList<XmlNode> _nodes;
    private readonly XmlNode? _before;

    public override string Name => "Paste nodes";
    public override bool IsNoop => _nodes.Count == 0;

    public XmlPasteNodesCommand(XmlNode parent, IReadOnlyList<XmlNode> nodes, XmlNode? before)
    {
        _parent = parent;
        _nodes = nodes;
        _before = before;
    }

    public override void Do() => Insert();
    public override void Redo() => Insert();

    public override void Undo()
    {
        for (var index = _nodes.Count - 1; index >= 0; index--)
        {
            Remove(_nodes[index]);
        }
    }

    private void Insert()
    {
        foreach (var node in _nodes)
        {
            if (node is XmlAttribute attribute)
            {
                var owner = _parent as XmlElement ?? throw new InvalidOperationException("An attribute can only be pasted onto an element.");
                if (owner.Attributes.Cast<XmlAttribute>().Any(existing => existing.LocalName == attribute.LocalName && existing.NamespaceURI == attribute.NamespaceURI))
                {
                    throw new InvalidOperationException($"The target already has an attribute named '{attribute.Name}'.");
                }
                owner.Attributes.InsertBefore(attribute, _before as XmlAttribute);
            }
            else
            {
                _parent.InsertBefore(node, _before?.ParentNode == _parent ? _before : null);
            }
        }
    }

    private void Remove(XmlNode node)
    {
        if (node is XmlAttribute attribute)
        {
            ((XmlElement)_parent).Attributes.Remove(attribute);
        }
        else if (node.ParentNode == _parent)
        {
            _parent.RemoveChild(node);
        }
    }
}
