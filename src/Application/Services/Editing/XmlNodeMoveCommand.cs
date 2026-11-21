using System.Xml;
using XmlNotepad;

namespace XmlNotepad.Services;

public sealed class XmlNodeMoveCommand : Command
{
    private readonly XmlNode _node;
    private readonly XmlNode _oldParent;
    private readonly XmlNode? _oldNext;
    private readonly XmlAttribute? _oldNextAttribute;
    private readonly XmlNode _newParent;
    private readonly XmlNode? _newBefore;

    public override string Name => "Move node";
    public override bool IsNoop => ReferenceEquals(_oldParent, _newParent) &&
        ReferenceEquals(_node is XmlAttribute ? _oldNextAttribute : _oldNext, _newBefore);
    public XmlNode TargetParent => _newParent;

    public XmlNodeMoveCommand(XmlNode node, XmlNode newParent, XmlNode? newBefore)
    {
        _node = node;
        _newParent = newParent;
        _newBefore = newBefore;
        if (node is XmlAttribute attribute)
        {
            _oldParent = attribute.OwnerElement ?? throw new InvalidOperationException("The attribute has no owner element.");
            _oldNextAttribute = GetNextAttribute((XmlElement)_oldParent, attribute);
            if (newParent is not XmlElement)
            {
                throw new InvalidOperationException("Attributes can only be moved between elements.");
            }
        }
        else
        {
            _oldParent = node.ParentNode ?? throw new InvalidOperationException("The selected node is not attached to the document.");
            _oldNext = node.NextSibling;
        }
    }

    public override void Do() => MoveTo(_newParent, _newBefore);
    public override void Redo() => MoveTo(_newParent, _newBefore);
    public override void Undo() => MoveTo(_oldParent, _node is XmlAttribute ? _oldNextAttribute : _oldNext);

    private void MoveTo(XmlNode parent, XmlNode? before)
    {
        if (_node is XmlAttribute attribute)
        {
            var owner = (XmlElement)parent;
            if (attribute.OwnerElement is { } oldOwner)
            {
                oldOwner.Attributes.Remove(attribute);
            }
            owner.Attributes.InsertBefore(attribute, before as XmlAttribute);
        }
        else
        {
            _node.ParentNode?.RemoveChild(_node);
            parent.InsertBefore(_node, before?.ParentNode == parent ? before : null);
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
