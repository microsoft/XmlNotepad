using System.Xml;
using XmlNotepad;

namespace XmlNotepad.Services;

/// <summary>Undoable insertion or removal of one node in the XML DOM.</summary>
public sealed class XmlStructureCommand : Command
{
    private readonly XmlNode _parent;
    private readonly XmlNode _node;
    private readonly XmlNode? _nextSibling;
    private readonly bool _insert;

    public override string Name { get; }
    public override bool IsNoop => false;

    private XmlStructureCommand(string name, XmlNode parent, XmlNode node, XmlNode? nextSibling, bool insert)
    {
        Name = name;
        _parent = parent;
        _node = node;
        _nextSibling = nextSibling;
        _insert = insert;
    }

    public static XmlStructureCommand Insert(string name, XmlNode parent, XmlNode node, XmlNode? nextSibling = null) =>
        new(name, parent, node, nextSibling, insert: true);

    public static XmlStructureCommand Remove(string name, XmlNode node)
    {
        if (node is XmlAttribute attribute)
        {
            var owner = attribute.OwnerElement ?? throw new InvalidOperationException("The attribute has no owner element.");
            return new(name, owner, node, GetNextAttribute(owner, attribute), insert: false);
        }

        var parent = node.ParentNode ?? throw new InvalidOperationException("The selected node cannot be removed.");
        return new(name, parent, node, node.NextSibling, insert: false);
    }

    public override void Do() => Apply(_insert);
    public override void Undo() => Apply(!_insert);
    public override void Redo() => Apply(_insert);

    private void Apply(bool insert)
    {
        if (_node is XmlAttribute attribute)
        {
            var owner = (XmlElement)_parent;
            if (insert)
            {
                if (attribute.OwnerElement != owner)
                {
                    owner.Attributes.InsertBefore(attribute, _nextSibling as XmlAttribute);
                }
            }
            else if (attribute.OwnerElement == owner)
            {
                owner.Attributes.Remove(attribute);
            }
            return;
        }

        if (insert)
        {
            if (_node.ParentNode != _parent)
            {
                if (_nextSibling?.ParentNode == _parent)
                {
                    _parent.InsertBefore(_node, _nextSibling);
                }
                else
                {
                    _parent.AppendChild(_node);
                }
            }
        }
        else if (_node.ParentNode == _parent)
        {
            _parent.RemoveChild(_node);
        }
    }

    private static XmlAttribute? GetNextAttribute(XmlElement owner, XmlAttribute attribute)
    {
        var found = false;
        foreach (XmlAttribute current in owner.Attributes)
        {
            if (found)
            {
                return current;
            }
            if (current == attribute)
            {
                found = true;
            }
        }
        return null;
    }
}
