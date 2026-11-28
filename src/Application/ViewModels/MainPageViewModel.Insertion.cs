using System.Xml;
using XmlNotepad.Services;

namespace XmlNotepad.ViewModels;

public sealed partial class MainPageViewModel
{
    public bool CanInsertSiblingOfSameType => SiblingInsertType.HasValue;

    internal XmlNodeType? SiblingInsertType => SelectedNode is { IsPlaceholder: false } item ? item.Node switch
    {
        XmlElement element when element == _session.Document.DocumentElement => null,
        XmlDeclaration => XmlNodeType.ProcessingInstruction,
        XmlWhitespace or XmlSignificantWhitespace when item.Node.ParentNode is not XmlDocument => XmlNodeType.Text,
        XmlAttribute or XmlElement or XmlText or XmlComment or XmlCDataSection or XmlProcessingInstruction => item.Node.NodeType,
        _ => null
    } : null;

    internal XmlDefinitionTarget? GetDefinition(XmlNode node)
    {
        _intellisense ??= new XmlIntellisenseService(App.SettingsService.SchemaCache, App.SettingsService);
        return _intellisense.GetDefinition(_session.Document, node, _session.FilePath, _session.RemoteSource);
    }

    internal IReadOnlyList<XmlSuggestion> GetExpectedValues(XmlNode node)
    {
        _intellisense ??= new XmlIntellisenseService(App.SettingsService.SchemaCache, App.SettingsService);
        return _intellisense.GetExpectedValues(_session.Document, node, _session.RemoteSource);
    }

    public IReadOnlyList<XmlSuggestion> GetExpectedNames(XmlNodeType type, InsertPosition position)
    {
        if (type is not (XmlNodeType.Element or XmlNodeType.Attribute))
        {
            return [];
        }

        XmlNode parent;
        XmlNode? nextSibling;
        try
        {
            (parent, nextSibling) = GetInsertionPoint(type, position);
        }
        catch (InvalidOperationException)
        {
            return [];
        }

        _intellisense ??= new XmlIntellisenseService(App.SettingsService.SchemaCache, App.SettingsService);
        return _intellisense.GetExpectedNames(_session.Document, parent, nextSibling, type, _session.RemoteSource);
    }

    public void InsertElement(string name)
    {
        InsertNode(XmlNodeType.Element, InsertPosition.Child, name, string.Empty);
    }

    public void InsertAttribute(string name)
    {
        InsertNode(XmlNodeType.Attribute, InsertPosition.Child, name, string.Empty);
    }

    public void InsertText(string value)
    {
        InsertNode(XmlNodeType.Text, InsertPosition.Child, string.Empty, value);
    }

    public void InsertComment(string value)
    {
        InsertNode(XmlNodeType.Comment, InsertPosition.Child, string.Empty, value);
    }

    public void InsertNode(XmlNodeType type, InsertPosition position, string name, string value, string? schemaNamespace = null)
    {
        var (parent, nextSibling) = GetInsertionPoint(type, position);
        var document = _session.Document;
        XmlNodeName? qualifiedName = type is XmlNodeType.Element or XmlNodeType.Attribute
            ? XmlNodeName.Resolve(parent, type, name, schemaNamespace)
            : null;
        if (type == XmlNodeType.ProcessingInstruction)
        {
            XmlConvert.VerifyName(name);
        }

        XmlNode node = type switch
        {
            XmlNodeType.Element => document.CreateElement(qualifiedName!.Prefix, qualifiedName.LocalName, qualifiedName.NamespaceUri),
            XmlNodeType.Attribute => document.CreateAttribute(qualifiedName!.Prefix, qualifiedName.LocalName, qualifiedName.NamespaceUri),
            XmlNodeType.Text => _session.Document.CreateTextNode(value),
            XmlNodeType.Comment => _session.Document.CreateComment(value),
            XmlNodeType.CDATA => _session.Document.CreateCDataSection(value),
            XmlNodeType.ProcessingInstruction => _session.Document.CreateProcessingInstruction(name, value),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "This node type cannot be inserted from the Insert menu.")
        };

        var insert = XmlStructureCommand.Insert($"Insert {type}", parent, node, nextSibling);
        EditXmlNamespaceCommand? binding = null;
        if (node is XmlAttribute attribute)
        {
            var owner = parent as XmlElement ?? throw new InvalidOperationException("Attributes can only be inserted on an element.");
            if (owner.GetAttributeNode(attribute.LocalName, attribute.NamespaceURI) is not null)
            {
                throw new InvalidOperationException($"The element already has an attribute named '{attribute.Name}'.");
            }
            attribute.Value = value;
            var group = new CompoundCommand("Insert attribute");
            group.Add(insert);
            if (qualifiedName!.Declaration is { } prefix)
            {
                group.Add(XmlStructureCommand.Insert("Declare prefix", owner, prefix));
            }
            if (attribute.NamespaceURI == XmlStandardUris.XmlnsUri)
            {
                binding = new EditXmlNamespaceCommand(attribute, value, owner);
                group.Add(binding);
            }
            _undo.Push(group);
        }
        else
        {
            if (node is XmlElement element && qualifiedName?.Declaration is { } declaration)
            {
                element.Attributes.Append(declaration);
            }
            _undo.Push(insert);
        }

        if (binding is not null)
        {
            RefreshAfterHistoryChange(node, binding);
            parent = binding.GetCurrentNode(parent, undo: false);
        }
        RefreshTreeItem(parent);
        if (parent is not XmlDocument && FindItem(parent) is { } parentItem)
        {
            parentItem.LoadChildren();
            parentItem.RefreshChildren();
            parentItem.IsExpanded = true;
        }
        SelectedNode = FindItem(node) ?? FindItem(parent) ?? SelectedNode;
    }

    public void CopySelectedNode() => CommitSelectedValue();

    private (XmlNode Parent, XmlNode? NextSibling) GetInsertionPoint(XmlNodeType type, InsertPosition position)
    {
        var selected = SelectedNode is { IsPlaceholder: false } item ? item.Node : _session.Document;
        var insertingAttribute = type == XmlNodeType.Attribute;
        if (position == InsertPosition.Child)
        {
            if (selected is XmlDocument or XmlElement)
            {
                return (selected, null);
            }

            if (selected is XmlAttribute attribute)
            {
                var owner = attribute.OwnerElement ?? throw new InvalidOperationException("The selected attribute has no owner element.");
                return (owner, insertingAttribute ? GetNextAttribute(owner, attribute) : null);
            }

            var parent = selected.ParentNode ?? throw new InvalidOperationException("The selected node cannot have inserted siblings.");
            if (insertingAttribute)
            {
                if (parent is XmlElement owner)
                {
                    return (owner, null);
                }
                throw new InvalidOperationException("Attributes can only be inserted on an element.");
            }
            return (parent, selected.NextSibling);
        }

        if (selected is XmlAttribute selectedAttribute)
        {
            var owner = selectedAttribute.OwnerElement ?? throw new InvalidOperationException("The selected attribute has no owner element.");
            if (insertingAttribute)
            {
                return (owner, position == InsertPosition.Before
                    ? selectedAttribute
                    : GetNextAttribute(owner, selectedAttribute));
            }
            return (owner, owner.FirstChild);
        }

        var selectedParent = selected.ParentNode ?? throw new InvalidOperationException("The selected node has no parent.");
        if (insertingAttribute)
        {
            if (selectedParent is XmlElement owner)
            {
                return (owner, null);
            }
            throw new InvalidOperationException("Attributes can only be inserted on an element.");
        }
        return (selectedParent, position == InsertPosition.Before ? selected : selected.NextSibling);
    }
}
