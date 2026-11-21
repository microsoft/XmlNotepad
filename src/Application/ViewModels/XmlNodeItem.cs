using System.Collections.ObjectModel;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XmlNotepad.Services;

namespace XmlNotepad.ViewModels;

public sealed class XmlNodeItem : ObservableObject
{
    private readonly XmlNode? _node;
    private readonly XmlNodeItem? _parent;
    private readonly XmlNodeLabels? _labels;
    private bool _childrenLoaded;
    private bool _isExpanded;
    private TreeViewNode? _treeNode;
    private readonly int _depth;

    public XmlNode Node => _node ?? throw new InvalidOperationException("Placeholder rows do not represent XML nodes.");

    public bool IsPlaceholder => _node is null;

    public XmlNodeItem? ParentItem => _parent;
    public bool CanConvertTo(string type) => _node is not null &&
        Enum.TryParse<XmlNodeType>(type, out var target) && XmlNodeTypeChangeCommand.CanConvert(_node, target);

    public bool CanDuplicate => _node is not null &&
        _node is not XmlDocument and not XmlDeclaration and not XmlDocumentType &&
        (_node is XmlAttribute || _node.ParentNode is not null &&
            !(_node == _node.OwnerDocument?.DocumentElement || _node.ParentNode is XmlDocument &&
                _node.NodeType is XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace));

    public bool ChildrenLoaded => _childrenLoaded;

    public bool IsExpanded
    {
        get => _treeNode?.IsExpanded ?? _isExpanded;
        set
        {
            _isExpanded = value;
            if (_treeNode is { } treeNode)
            {
                treeNode.IsExpanded = value;
            }
        }
    }

    public ObservableCollection<XmlNodeItem> Children { get; } = [];

    internal TreeViewNode TreeNode => _treeNode ?? CreateTreeNode();

    private TreeViewNode CreateTreeNode()
    {
        var treeNode = new TreeViewNode { Content = new XmlTreeNodeContent(this) };
        _treeNode = treeNode;
        foreach (var child in Children)
        {
            treeNode.Children.Add(child.TreeNode);
        }
        Children.CollectionChanged += (_, change) => XmlTreeNodeContent.UpdateNodes(treeNode.Children, Children, change);
        UpdateUnrealizedChildren();
        treeNode.IsExpanded = _isExpanded;
        return treeNode;
    }

    private void UpdateUnrealizedChildren()
    {
        if (_treeNode is { } treeNode)
        {
            treeNode.HasUnrealizedChildren = !_childrenLoaded && _node is not null && HasChildren(_node);
        }
    }

    public string DisplayName => _node is null ? string.Empty : _node.NodeType switch
    {
        XmlNodeType.DocumentType => $"DOCTYPE {_node.Name}",
        XmlNodeType.XmlDeclaration => "xml",
        XmlNodeType.Comment => "#comment",
        XmlNodeType.ProcessingInstruction => $"<?{_node.Name}?>",
        XmlNodeType.Text => "#text",
        XmlNodeType.CDATA => "#cdata-section",
        _ => _node.Name
    };

    public string IdentifyingText => _labels?.GetLabel(_node) ?? string.Empty;

    public XmlNodeType NodeType => _node?.NodeType ?? XmlNodeType.None;
    public bool IsContainer => _node is XmlElement element && !IsValueEditable(element);
    public EditorPalette Palette { get; }

    public EditorTypography Typography => App.Typography;

    public Thickness GetIndentation(int indentPerLevel) => new(_depth * indentPerLevel, 0, 0, 0);

    public string IconGlyph => _node?.NodeType switch
    {
        XmlNodeType.Element => "\uE943",
        XmlNodeType.Attribute => "\uE8EC",
        XmlNodeType.Comment => "\uE90A",
        XmlNodeType.CDATA => "\uE8D2",
        XmlNodeType.Text or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace => "\uE8E4",
        XmlNodeType.XmlDeclaration => "\uE7C3",
        XmlNodeType.ProcessingInstruction => "\uE713",
        XmlNodeType.DocumentType => "\uE8F1",
        XmlNodeType.EntityReference => "\uE71B",
        _ => string.Empty
    };

    public string KindLabel => _node?.NodeType switch
    {
        XmlNodeType.CDATA => "CDATA section",
        XmlNodeType.ProcessingInstruction => "Processing instruction",
        XmlNodeType.XmlDeclaration => "XML declaration",
        XmlNodeType.DocumentType => "Document type",
        XmlNodeType.EntityReference => "Entity reference",
        XmlNodeType.SignificantWhitespace => "Significant whitespace",
        { } kind => kind.ToString(),
        _ => string.Empty
    };

    public string ValuePreview
    {
        get
        {
            const int previewLimit = 320;
            var value = _node is null ? string.Empty : GetNodeValue(_node);
            if (value.Length > previewLimit)
            {
                value = value[..(char.IsHighSurrogate(value[previewLimit - 1]) ? previewLimit - 1 : previewLimit)] + "…";
            }
            return value.Replace("\r\n", " ↵ ", StringComparison.Ordinal).Replace('\r', '\n')
                .Replace("\n", " ↵ ", StringComparison.Ordinal).Replace('\t', ' ');
        }
    }

    public XmlNodeItem(XmlNode? node, XmlNodeItem? parent = null) : this(node, parent, parent?._labels)
    {
    }

    internal XmlNodeItem(XmlNode? node, XmlNodeItem? parent, XmlNodeLabels? labels, EditorPalette? palette = null)
    {
        _node = node;
        _parent = parent;
        _depth = parent is null || parent._node is XmlDocument ? 0 : parent._depth + 1;
        _labels = labels;
        Palette = palette ?? parent?.Palette ?? new EditorPalette();
    }

    public void LoadChildren()
    {
        if (_node is null)
        {
            return;
        }

        if (_childrenLoaded)
        {
            return;
        }

        _childrenLoaded = true;
        foreach (var child in EnumerateChildren())
        {
            Children.Add(new XmlNodeItem(child, this));
        }
        UpdateUnrealizedChildren();
    }

    public void RefreshChildren()
    {
        OnPropertyChanged(nameof(IsContainer));
        if (_childrenLoaded && _node is not null)
        {
            SynchronizeChildren();
        }
        else
        {
            UpdateUnrealizedChildren();
        }
        NotifyChanged();
    }

    private void SynchronizeChildren()
    {
        var nodes = EnumerateChildren().ToArray();
        if (nodes.Length == Children.Count && nodes.SequenceEqual(Children.Select(item => item.Node)))
        {
            return;
        }

        var retained = new Dictionary<XmlNode, XmlNodeItem>();
        var currentNodes = nodes.ToHashSet();
        for (var index = Children.Count - 1; index >= 0; index--)
        {
            var item = Children[index];
            if (currentNodes.Contains(item.Node))
            {
                retained.Add(item.Node, item);
            }
            else
            {
                Children.RemoveAt(index);
            }
        }

        for (var index = 0; index < nodes.Length; index++)
        {
            var node = nodes[index];
            if (index < Children.Count && ReferenceEquals(Children[index].Node, node))
            {
                continue;
            }
            if (retained.TryGetValue(node, out var item))
            {
                Children.Move(Children.IndexOf(item), index);
            }
            else
            {
                Children.Insert(index, new XmlNodeItem(node, this));
            }
        }
    }

    private IEnumerable<XmlNode> EnumerateChildren()
    {
        if (_node?.Attributes is { } attributes)
        {
            foreach (XmlAttribute attribute in attributes)
            {
                yield return attribute;
            }
        }

        if (_node is not (null or XmlAttribute))
        {
            for (var child = _node.FirstChild; child is not null; child = child.NextSibling)
            {
                yield return child;
            }
        }
    }

    public void NotifyChanged()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(ValuePreview));
        OnPropertyChanged(nameof(IsContainer));
        NotifyIdentifyingTextChanged();
        _parent?.NotifyIdentifyingTextChanged();
    }

    internal void NotifyIdentifyingTextChanged() => OnPropertyChanged(nameof(IdentifyingText));

    internal static bool IsValueEditable(XmlNode node) => node.NodeType switch
    {
        XmlNodeType.Attribute or XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.Comment or XmlNodeType.ProcessingInstruction => true,
        XmlNodeType.Element => node.ChildNodes.Cast<XmlNode>().All(child => child.NodeType is XmlNodeType.Text or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace),
        _ => false
    };

    internal static string GetNodeValue(XmlNode node) => node is XmlElement
        ? IsValueEditable(node) ? node.InnerText : string.Empty
        : node.Value ?? string.Empty;

    private static bool HasChildren(XmlNode node) => node is not XmlAttribute && (node.Attributes?.Count > 0 || node.HasChildNodes);
}
