using System.Xml;
using XmlNotepad.Services;

namespace XmlNotepad.ViewModels;

public sealed partial class MainPageViewModel
{
    internal bool CanDropNode(XmlNode source, XmlNode target, InsertPosition position, bool copy) =>
        GetDropDestination(source, target, position, copy) is not null;

    internal bool DropNode(XmlNode source, XmlNode target, InsertPosition position, bool copy)
    {
        if (GetDropDestination(source, target, position, copy) is not { } destination)
        {
            return false;
        }

        var (parent, before) = destination;
        if (!copy && before == source)
        {
            return false;
        }

        CommitSelectedValue();
        if (copy)
        {
            var clone = source.CloneNode(deep: true);
            _undo.Push(new XmlPasteNodesCommand(parent, new[] { clone }, before));
            RefreshTreeItem(parent);
            SelectNode(clone);
        }
        else
        {
            var command = new XmlNodeMoveCommand(source, parent, before);
            if (command.IsNoop)
            {
                return false;
            }
            MoveNode(source, command);
        }

        return true;
    }

    private (XmlNode Parent, XmlNode? Before)? GetDropDestination(
        XmlNode source, XmlNode target, InsertPosition position, bool copy)
    {
        if (source == target || source.OwnerDocument != Document || target.OwnerDocument != Document ||
            source is XmlDocumentType or XmlDeclaration || !copy && source == Document.DocumentElement)
        {
            return null;
        }

        var sourceParent = GetDropParent(source);
        var parent = position == InsertPosition.Child ? target as XmlElement : GetDropParent(target);
        if (sourceParent is not (XmlElement or XmlDocument) || parent is not (XmlElement or XmlDocument) ||
            parent.IsReadOnly || (!copy && sourceParent.IsReadOnly))
        {
            return null;
        }

        for (XmlNode? ancestor = parent; ancestor is not null; ancestor = ancestor.ParentNode)
        {
            if (ancestor == source)
            {
                return null;
            }
        }

        if (source is XmlAttribute attribute)
        {
            if (parent is not XmlElement element || position != InsertPosition.Child && target is not XmlAttribute ||
                element.GetAttributeNode(attribute.LocalName, attribute.NamespaceURI) is { } existing &&
                (copy || existing != source))
            {
                return null;
            }
        }
        else if (target is XmlAttribute || parent is XmlDocument &&
            source.NodeType is not (XmlNodeType.Comment or XmlNodeType.ProcessingInstruction or XmlNodeType.Whitespace))
        {
            return null;
        }

        XmlNode? before = position switch
        {
            InsertPosition.Child => null,
            InsertPosition.Before => target,
            _ => target is XmlAttribute nextAttribute ? GetNextAttribute((XmlElement)parent, nextAttribute) : target.NextSibling
        };

        return before is XmlDeclaration ? null : (parent, before);
    }

    private static XmlNode? GetDropParent(XmlNode node) => node is XmlAttribute attribute
        ? attribute.OwnerElement
        : node.ParentNode;
}
