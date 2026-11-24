using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using XmlNotepad.Services;

namespace XmlNotepad.ViewModels;

public sealed class XmlNodeItem : ObservableObject
{
    private readonly XmlNode? _node;
    private readonly XmlNodeItem? _parent;
    private readonly XmlNodeLabels? _labels;
    private bool _childrenLoaded;
    private bool _isExpanded;
    private List<XmlNodeItem>? _children;
    private readonly int _depth;
    internal event Action? TreeChanged;

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
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value))
            {
                OnPropertyChanged(nameof(ExpansionGlyph));
                OnPropertyChanged(nameof(ExpansionLabel));
                NotifyTreeChanged();
            }
        }
    }

    public IReadOnlyList<XmlNodeItem> Children => _children is null ? Array.Empty<XmlNodeItem>() : _children;
    public bool CanExpand => _node is not null && HasChildren(_node);
    public string ExpansionGlyph => IsExpanded ? "\uE70D" : "\uE76C";
    public string ExpansionLabel => IsExpanded ? $"Collapse {DisplayName}" : $"Expand {DisplayName}";
    public int Level => _depth + 1;

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
        if (!HasChildren(_node))
        {
            return;
        }
        foreach (var child in EnumerateChildren())
        {
            InsertChild(Children.Count, new XmlNodeItem(child, this));
        }
    }

    public void RefreshChildren()
    {
        OnPropertyChanged(nameof(IsContainer));
        if (_childrenLoaded && _node is not null)
        {
            SynchronizeChildren();
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
                RemoveChildAt(index);
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
                RemoveChildAt(_children!.IndexOf(item));
                InsertChild(index, item);
            }
            else
            {
                InsertChild(index, new XmlNodeItem(node, this));
            }
        }
        if (nodes.Length == 0)
        {
            IsExpanded = false;
        }
        NotifyTreeChanged();
    }

    private void InsertChild(int index, XmlNodeItem item)
    {
        (_children ??= []).Insert(index, item);
    }

    private void RemoveChildAt(int index)
    {
        _children!.RemoveAt(index);
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
        OnPropertyChanged(nameof(ExpansionLabel));
        OnPropertyChanged(nameof(CanExpand));
        OnPropertyChanged(nameof(ValuePreview));
        OnPropertyChanged(nameof(IsContainer));
        NotifyIdentifyingTextChanged();
        _parent?.NotifyIdentifyingTextChanged();
    }

    internal void NotifyIdentifyingTextChanged() => OnPropertyChanged(nameof(IdentifyingText));

    private void NotifyTreeChanged()
    {
        var root = this;
        while (root.ParentItem is { IsDocument: false } parent)
        {
            root = parent;
        }
        root.TreeChanged?.Invoke();
    }

    private bool IsDocument => _node is XmlDocument;

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
