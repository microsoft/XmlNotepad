using System.Collections.ObjectModel;
using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XmlNotepad;
using XmlNotepad.Services;
using static XmlNotepad.ViewModels.XmlNodeItem;

namespace XmlNotepad.ViewModels;

public sealed partial class MainPageViewModel : ObservableObject
{
    private readonly XmlDocumentSession _session = new();
    private readonly UndoManager _undo = new(1000);
    private readonly XmlNodeLabels _nodeLabels = new();
    private XmlIntellisenseService? _intellisense;
    private XmlNodeItem? _selectedNode;
    private string _selectedValue = string.Empty;
    private bool _isBusy;

    public EditorPalette Palette { get; } = new();

    public ObservableCollection<XmlNodeItem> RootNodes { get; } = [];

    public string FileName => _session.FileName;

    public string? CurrentFilePath => _session.FilePath;
    internal bool HasRemoteSource => _session.RemoteSource is not null;
    internal string? CurrentDocumentLocation => _session.FilePath ?? _session.RemoteSource?.AbsoluteUri;
    internal DocumentFileVersion? CurrentFileVersion => _session.FileVersion;
    public bool HasFilePath => CurrentFilePath is not null;

    public XmlDocument Document => _session.Document;

    public bool IsDirty => _session.IsDirty;

    public string Title => $"{FileName}{(IsDirty ? " *" : "")} - XML Notepad";

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
    }

    public bool IsNotBusy => !IsBusy;

    public XmlNodeItem? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
            {
                SelectedValue = value is { IsPlaceholder: false } ? GetNodeValue(value.Node) : string.Empty;
                OnPropertyChanged(nameof(CanEditValue));
                OnPropertyChanged(nameof(IsValueReadOnly));
                OnPropertyChanged(nameof(CanRenameSelectedNode));
                OnPropertyChanged(nameof(CanDuplicateSelectedNode));
                OnPropertyChanged(nameof(CanInsertSiblingOfSameType));
                OnPropertyChanged(nameof(CanCopySelectedNode));
                OnPropertyChanged(nameof(CanDeleteSelectedNode));
                OnPropertyChanged(nameof(CanCutSelectedNode));
                NotifyNudgeState();
            }
        }
    }

    public bool CanEditValue => SelectedNode is { IsPlaceholder: false } item && IsValueEditable(item.Node);

    public bool CanRenameSelectedNode => SelectedNode is { IsPlaceholder: false } item &&
        item.Node is XmlElement or XmlAttribute or XmlProcessingInstruction;

    public bool CanDuplicateSelectedNode => SelectedNode?.CanDuplicate == true;

    public bool CanCopySelectedNode => SelectedNode is { IsPlaceholder: false } item && item.Node is not XmlDocument;

    public bool CanDeleteSelectedNode => SelectedNode is { IsPlaceholder: false } item &&
        item.Node is not XmlDocument and not XmlDeclaration && item.Node != _session.Document.DocumentElement;

    public bool CanCutSelectedNode => CanCopySelectedNode && CanDeleteSelectedNode;

    public bool CanNudgeUp => GetNudgeCommand(NudgeDirection.Up) is not null;

    public bool CanNudgeDown => GetNudgeCommand(NudgeDirection.Down) is not null;

    public bool CanNudgeLeft => GetNudgeCommand(NudgeDirection.Left) is not null;

    public bool CanNudgeRight => GetNudgeCommand(NudgeDirection.Right) is not null;

    public bool IsValueReadOnly => !CanEditValue;

    public string SelectedValue
    {
        get => _selectedValue;
        set => SetProperty(ref _selectedValue, value);
    }

    public bool CanUndo => _undo.CanUndo;

    public bool CanRedo => _undo.CanRedo;

    internal event Action<string>? ValueEditFailed;

    public MainPageViewModel()
    {
        _nodeLabels.ReadOptions(Settings.Instance);
        _session.DocumentChanged += Session_DocumentChanged;
        _session.DocumentReplaced += Session_DocumentReplaced;
        _session.StateChanged += Session_StateChanged;
        _undo.StateChanged += Undo_StateChanged;
        _session.CreateNew();
    }

    public void Dispose()
    {
        _intellisense = null;
    }

    public void CreateNew()
    {
        _undo.Clear();
        _session.CreateNew();
    }

    public async Task OpenAsync(string path)
    {
        IsBusy = true;
        try
        {
            await _session.OpenAsync(path);
            _undo.Clear();
        }
        finally
        {
            IsBusy = false;
        }
    }

    internal async Task SaveAsync(string path, DocumentSaveTarget approvedTarget)
    {
        CommitSelectedValue();
        IsBusy = true;
        try
        {
            await _session.SaveAsync(path, approvedTarget);
        }
        finally
        {
            IsBusy = false;
        }
    }

    internal async Task<RemoteDocument> DownloadAsync(Uri location)
    {
        IsBusy = true;
        try
        {
            return await RemoteDocument.DownloadAsync(location);
        }
        finally
        {
            IsBusy = false;
        }
    }

    internal async Task OpenAsync(RemoteDocument remote)
    {
        IsBusy = true;
        try
        {
            await _session.OpenAsync(remote);
            _undo.Clear();
        }
        finally
        {
            IsBusy = false;
        }
    }

    internal async Task ImportAsync(string path, Func<XmlDocument> convert, Uri? remoteSource = null)
    {
        IsBusy = true;
        try
        {
            var document = await Task.Run(convert);
            _undo.Clear();
            _session.SetImportedDocument(document, path, remoteSource);
        }
        finally
        {
            IsBusy = false;
        }
    }

    internal void LoadGeneratedDocument(XmlDocument document)
    {
        _undo.Clear();
        _session.SetImportedDocument(document, $"{document.DocumentElement?.LocalName ?? "Generated"}.xml");
    }

    public async Task<XmlValidationResult> ValidateAsync(XmlValidationService validationService, string? documentationLanguage = null)
    {
        CommitSelectedValue();
        IsBusy = true;
        try
        {
            var document = _session.Document;
            var filePath = _session.FilePath;
            var remoteSource = _session.RemoteSource;
            var collectIdentifiers = _nodeLabels.Enabled;
            var result = await Task.Run(() => validationService.Validate(document, filePath, remoteSource, documentationLanguage,
                collectIdentifiers));
            ApplyValidationIdentifiers(result);
            return result;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyValidationIdentifiers(XmlValidationResult result)
    {
        if (_nodeLabels.SetIdentifiers(result.Identifiers))
        {
            RefreshNodeLabels();
        }
    }

    public async Task ExpandIncludesAsync(XmlIncludeService includeService)
    {
        IsBusy = true;
        try
        {
            var document = _session.Document;
            var filePath = _session.FilePath;
            var remoteSource = _session.RemoteSource;
            var expanded = await Task.Run(() => includeService.Expand(document, filePath, remoteSource));
            _undo.Clear();
            _session.ReplaceDocument(expanded, true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<PreviewOutput> TransformAsync(XmlXsltService xsltService, string? stylesheetPath,
        bool explicitlySelected, ColorTheme theme, Uri? trustedStylesheet = null, CancellationToken cancellation = default)
    {
        IsBusy = true;
        try
        {
            var document = _session.Document;
            var documentPath = _session.FilePath;
            var remoteSource = _session.RemoteSource;
            return await Task.Run(() => xsltService.Transform(document, documentPath, remoteSource, stylesheetPath,
                explicitlySelected, theme, trustedStylesheet, cancellation), cancellation);
        }
        finally
        {
            IsBusy = false;
        }
    }

    internal async Task<PreviewOutput?> CompareAsync(XmlComparisonService comparison, string baseline, string changed)
    {
        IsBusy = true;
        try
        {
            return await Task.Run(() => comparison.Compare(baseline, changed));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Expand(XmlNodeItem item) => item.LoadChildren();

    internal bool ShowIdentifyingText => _nodeLabels.Enabled;

    internal void ReadNodeLabelOptions()
    {
        _nodeLabels.ReadOptions(Settings.Instance);
        RefreshNodeLabels();
    }

    private void RefreshNodeLabels()
    {
        var pending = new Stack<XmlNodeItem>(RootNodes);
        while (pending.TryPop(out var item))
        {
            if (!item.IsPlaceholder)
            {
                item.NotifyIdentifyingTextChanged();
                foreach (var child in item.Children)
                {
                    pending.Push(child);
                }
            }
        }
    }

    public void CommitSelectedValue()
    {
        if (!CanEditValue || SelectedNode is not { } item)
        {
            return;
        }

        var oldValue = GetNodeValue(item.Node);
        if (!string.Equals(oldValue, SelectedValue, StringComparison.Ordinal))
        {
            try
            {
                ApplyValueEdit(item.Node, oldValue, SelectedValue);
                if (item.Node is XmlElement)
                {
                    item.RefreshChildren();
                }
                else
                {
                    item.NotifyChanged();
                    item.ParentItem?.NotifyChanged();
                }
            }
            catch (XmlException exception)
            {
                RefreshSelectedValue();
                ValueEditFailed?.Invoke(exception.Message);
            }
        }
    }

    internal void CancelSelectedValueEdit() => RefreshSelectedValue();

    private void ApplyValueEdit(XmlNode node, string oldValue, string value)
    {
        if (node is XmlAttribute { NamespaceURI: XmlStandardUris.XmlnsUri } declaration)
        {
            var selectedNode = SelectedNode is { IsPlaceholder: false } selected ? selected.Node : null;
            var edit = new EditXmlNamespaceCommand(declaration, value);
            _undo.Push(edit);
            RefreshAfterHistoryChange(selectedNode, edit);
        }
        else
        {
            _undo.Push(new EditXmlValueCommand(node, oldValue, value));
        }
    }

    public void RenameSelectedNode(string newName)
    {
        if (!CanRenameSelectedNode || SelectedNode is not { } item)
        {
            return;
        }

        CommitSelectedValue();
        RenameNode(item.Node, newName.Trim());
    }

    public void DuplicateSelectedNode()
    {
        if (!CanDuplicateSelectedNode || SelectedNode is not { IsPlaceholder: false } selected)
        {
            return;
        }

        var source = selected.Node;
        XmlNode duplicate;
        XmlNode parent;
        XmlNode? nextSibling;
        if (source is XmlAttribute attribute)
        {
            var owner = attribute.OwnerElement ?? throw new InvalidOperationException("The selected attribute has no owner element.");
            var duplicateName = GetUniqueAttributeName(owner, attribute);
            var duplicateAttribute = _session.Document.CreateAttribute(attribute.Prefix, duplicateName, attribute.NamespaceURI);
            duplicateAttribute.Value = attribute.Value;
            duplicate = duplicateAttribute;
            parent = owner;
            nextSibling = GetNextAttribute(owner, attribute);
        }
        else
        {
            duplicate = source.CloneNode(deep: true);
            parent = source.ParentNode ?? throw new InvalidOperationException("The selected node cannot be duplicated.");
            nextSibling = source.NextSibling;
        }

        _undo.Push(XmlStructureCommand.Insert($"Duplicate {source.NodeType}", parent, duplicate, nextSibling));
        RefreshTreeItem(parent);
        SelectedNode = FindItem(duplicate) ?? FindItem(parent) ?? SelectedNode;
    }

    public void SelectNode(XmlNode node)
    {
        if (node.ParentNode is XmlAttribute attributeValue)
        {
            node = attributeValue;
        }
        if (FindItem(node, expandPath: true) is { } item)
        {
            SelectedNode = item;
        }
    }

    private XmlNodeItem? FindItem(XmlNode node, bool expandPath = false)
    {
        var path = new Stack<XmlNode>();
        XmlNode? current = node;
        while (current is not null && current != _session.Document)
        {
            path.Push(current);
            current = current is XmlAttribute attribute ? attribute.OwnerElement : current.ParentNode;
        }
        if (current is null)
        {
            return null;
        }

        XmlNodeItem? item = null;
        while (path.TryPop(out var pathNode))
        {
            if (expandPath && item is not null)
            {
                item.LoadChildren();
                item.IsExpanded = true;
            }
            var siblings = item?.Children ?? RootNodes;
            item = siblings.FirstOrDefault(candidate => !candidate.IsPlaceholder && candidate.Node == pathNode);
            if (item is null)
            {
                return null;
            }
        }

        return item;
    }

    public void ReplaceSearchMatch(XmlSearchMatch match, string replacement)
    {
        if (match.IsName)
        {
            var newName = ReplaceSpan(match.Node.Name, match, replacement);
            RenameNode(match.Node, newName);
        }
        else
        {
            ReplaceNodeValue(match.Node, ReplaceSpan(GetNodeValue(match.Node), match, replacement));
        }
    }

    public int ReplaceAllSearchMatches(IReadOnlyList<XmlSearchMatch> matches, string replacement)
    {
        if (matches.Count == 0)
        {
            return 0;
        }

        var replacements = matches
            .GroupBy(match => match.Node)
            .OrderBy(group => group.Key is XmlAttribute { NamespaceURI: XmlStandardUris.XmlnsUri })
            .Select(group => (Node: group.Key, Matches: group.ToArray()))
            .ToArray();
        _undo.OpenCompoundAction("Replace All");
        try
        {
            foreach (var item in replacements)
            {
                var valueMatches = item.Matches.Where(match => !match.IsName).ToArray();
                if (valueMatches.Length > 0)
                {
                    var value = GetNodeValue(item.Node);
                    ReplaceNodeValue(item.Node, ReplaceSpans(value, valueMatches, replacement));
                }

                var nameMatches = item.Matches.Where(match => match.IsName).ToArray();
                if (nameMatches.Length > 0)
                {
                    RenameNode(item.Node, ReplaceSpans(item.Node.Name, nameMatches, replacement));
                }
            }
        }
        finally
        {
            _undo.CloseCompoundAction();
        }

        RefreshVisibleTree();
        RefreshSelectedValue();
        NotifyNudgeState();
        return matches.Count;
    }

    private void RenameNode(XmlNode node, string newName)
    {
        var command = new XmlNodeRenameCommand(node, newName);
        if (command.IsNoop)
        {
            return;
        }

        _undo.Push(command);
        RefreshAfterHistoryChange(node, command);
    }

    private void ReplaceNodeValue(XmlNode node, string value)
    {
        var oldValue = GetNodeValue(node);
        if (string.Equals(oldValue, value, StringComparison.Ordinal))
        {
            return;
        }

        ApplyValueEdit(node, oldValue, value);
        var parent = node is XmlAttribute attribute ? attribute.OwnerElement : node.ParentNode;
        if (parent is not null)
        {
            RefreshTreeItem(parent);
        }
        if (SelectedNode is { IsPlaceholder: false } selected && selected.Node == node)
        {
            RefreshSelectedValue();
        }
    }

    private static string ReplaceSpan(string value, XmlSearchMatch match, string replacement) =>
        value[..match.Index] + replacement + value[(match.Index + match.Length)..];

    private static string ReplaceSpans(string value, IReadOnlyList<XmlSearchMatch> matches, string replacement)
    {
        foreach (var match in matches.OrderByDescending(match => match.Index))
        {
            value = ReplaceSpan(value, match, replacement);
        }
        return value;
    }

    public void DeleteSelectedNode()
    {
        if (SelectedNode is not { IsPlaceholder: false } selected)
        {
            return;
        }

        var node = selected.Node;
        if (node is XmlDocument or XmlDeclaration || node == _session.Document.DocumentElement)
        {
            throw new InvalidOperationException("The XML declaration and document root cannot be deleted.");
        }

        var parent = node is XmlAttribute attribute ? attribute.OwnerElement : node.ParentNode;
        if (parent is null)
        {
            throw new InvalidOperationException("The selected node cannot be deleted.");
        }

        _undo.Push(XmlStructureCommand.Remove("Delete node", node));
        RefreshTreeItem(parent);
        SelectedNode = FindItem(parent) ?? RootNodes.FirstOrDefault(item => !item.IsPlaceholder);
    }

    public void PasteXml(string content)
    {
        var selected = SelectedNode is { IsPlaceholder: false } item ? item.Node : null;
        var parserContext = selected ?? (XmlNode)_session.Document;
        var parsed = XmlClipboardService.Parse(_session.Document, parserContext, content);
        if (parsed.Count == 0)
        {
            throw new InvalidOperationException("The clipboard does not contain XML nodes.");
        }

        var areAttributes = parsed.All(node => node is XmlAttribute);
        if (parsed.Any(node => node is XmlAttribute) && !areAttributes)
        {
            throw new InvalidOperationException("XML attributes cannot be pasted together with other node types.");
        }

        XmlNode parent;
        XmlNode? before;
        if (areAttributes)
        {
            parent = selected switch
            {
                XmlAttribute attribute => attribute.OwnerElement!,
                XmlElement element => element,
                _ => GetSelectedElementContainer()
            };
            if (parent is not XmlElement)
            {
                throw new InvalidOperationException("Attributes can only be pasted onto an element.");
            }
            before = selected is XmlAttribute selectedAttribute ? GetNextAttribute(selectedAttribute.OwnerElement!, selectedAttribute) : null;
        }
        else
        {
            parent = selected is XmlElement ? selected : GetSelectedElementContainer();
            before = selected is not XmlElement && selected?.ParentNode == parent ? selected.NextSibling : null;
        }

        var nodes = parsed.Select(node => _session.Document.ImportNode(node, true)).ToArray();
        if (!areAttributes && nodes.Any(node => node is XmlDeclaration or XmlDocumentType or XmlDocumentFragment))
        {
            throw new InvalidOperationException("XML declarations and document types cannot be pasted inside an element.");
        }
        if (areAttributes)
        {
            var pastedNames = new HashSet<string>(StringComparer.Ordinal);
            var owner = (XmlElement)parent;
            foreach (XmlAttribute attribute in nodes)
            {
                var key = $"{attribute.NamespaceURI}\0{attribute.LocalName}";
                if (owner.Attributes.Cast<XmlAttribute>().Any(existing => existing.NamespaceURI == attribute.NamespaceURI && existing.LocalName == attribute.LocalName) ||
                    !pastedNames.Add(key))
                {
                    throw new InvalidOperationException($"The target already has an attribute named '{attribute.Name}'.");
                }
            }
        }
        _undo.Push(new XmlPasteNodesCommand(parent, nodes, before));
        RefreshTreeItem(parent);
        SelectedNode = FindItem(nodes[0]) ?? FindItem(parent) ?? SelectedNode;
    }

    public void NudgeSelectedNode(NudgeDirection direction)
    {
        if (SelectedNode is not { IsPlaceholder: false } item || GetNudgeCommand(direction) is not { } command)
        {
            return;
        }

        MoveNode(item.Node, command);
    }

    private void MoveNode(XmlNode node, XmlNodeMoveCommand command)
    {
        var sourceParent = node is XmlAttribute attribute ? attribute.OwnerElement : node.ParentNode;
        _undo.Push(command);
        if (sourceParent is not null)
        {
            RefreshTreeItem(sourceParent);
        }
        if (command.TargetParent != sourceParent)
        {
            RefreshTreeItem(command.TargetParent);
        }
        SelectNode(node);
    }

    public void Undo()
    {
        var selectedNode = SelectedNode is { IsPlaceholder: false } selected ? selected.Node : null;
        var edit = _undo.Undo();
        RefreshAfterHistoryChange(selectedNode, edit, undo: true);
        RefreshSelectedValue();
        NotifyNudgeState();
    }

    public void Redo()
    {
        var selectedNode = SelectedNode is { IsPlaceholder: false } selected ? selected.Node : null;
        var edit = _undo.Redo();
        RefreshAfterHistoryChange(selectedNode, edit);
        RefreshSelectedValue();
        NotifyNudgeState();
    }

    private void UpdateTree()
    {
        RootNodes.Clear();
        var documentItem = new XmlNodeItem(_session.Document, null, _nodeLabels, Palette);
        documentItem.LoadChildren();
        foreach (var item in documentItem.Children)
        {
            RootNodes.Add(item);
        }

        SelectedNode = RootNodes.FirstOrDefault(node => !node.IsPlaceholder && node.Node.NodeType == XmlNodeType.Element)
            ?? RootNodes.FirstOrDefault(node => !node.IsPlaceholder);
    }

    private void Session_DocumentChanged(object? sender, XmlNodeChangedEventArgs e)
    {
        if (e.Node is { } changedNode && SelectedNode is { } selectedNode && selectedNode.Node == changedNode)
        {
            SelectedValue = GetNodeValue(changedNode);
            selectedNode.NotifyChanged();
        }
    }

    private void Session_DocumentReplaced(object? sender, EventArgs e)
    {
        _nodeLabels.SetIdentifiers(null);
        UpdateTree();
        OnPropertyChanged(nameof(CanGoToLine));
    }

    public int LastIndexedLine => (_session.Document as SourceXmlDocument)?.LastIndexedLine ?? 0;

    public bool CanGoToLine => LastIndexedLine > 0;

    public (int Line, int Column) SelectedSourcePosition =>
        SelectedIndexedSourcePosition ?? (1, 1);

    internal (int Line, int Column)? SelectedIndexedSourcePosition =>
        SelectedNode is { IsPlaceholder: false } item && _session.Document is SourceXmlDocument source
            ? source.FindPosition(item.Node) : null;

    public bool GoToLine(int line, int column)
    {
        if (_session.Document is not SourceXmlDocument source || source.FindNode(line, column) is not { } node)
        {
            return false;
        }

        SelectNode(node);
        return true;
    }

    private void Session_StateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(HasFilePath));
    }

    private void Undo_StateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    private void RefreshSelectedValue()
    {
        if (SelectedNode is { } item)
        {
            SelectedValue = GetNodeValue(item.Node);
            item.NotifyChanged();
        }
    }

    private XmlElement GetSelectedElementContainer()
    {
        XmlNode? node = SelectedNode is { IsPlaceholder: false } selected ? selected.Node : _session.Document.DocumentElement;
        if (node is XmlAttribute attribute)
        {
            node = attribute.OwnerElement;
        }

        while (node is not null && node is not XmlElement)
        {
            node = node.ParentNode;
        }

        return (XmlElement?)node ?? throw new InvalidOperationException("Add a document element before inserting XML nodes.");
    }

    private void RefreshTreeItem(XmlNode parent)
    {
        if (parent is XmlDocument)
        {
            UpdateTree();
            return;
        }
        var item = FindItem(parent);
        item?.RefreshChildren();
    }

    private XmlNodeMoveCommand? GetNudgeCommand(NudgeDirection direction)
    {
        if (SelectedNode is not { IsPlaceholder: false } item)
        {
            return null;
        }

        var node = item.Node;
        if (node is XmlAttribute attribute)
        {
            var owner = attribute.OwnerElement;
            if (owner is null)
            {
                return null;
            }
            var attributes = owner.Attributes.Cast<XmlAttribute>().ToArray();
            var index = Array.IndexOf(attributes, attribute);
            return direction switch
            {
                NudgeDirection.Up when index > 0 => new XmlNodeMoveCommand(attribute, owner, attributes[index - 1]),
                NudgeDirection.Down when index >= 0 && index < attributes.Length - 1 => new XmlNodeMoveCommand(attribute, owner, GetNextAttribute(owner, attributes[index + 1])),
                NudgeDirection.Left when owner != _session.Document.DocumentElement && owner.ParentNode is XmlElement parent &&
                    !parent.Attributes.Cast<XmlAttribute>().Any(existing => existing.LocalName == attribute.LocalName && existing.NamespaceURI == attribute.NamespaceURI)
                    => new XmlNodeMoveCommand(attribute, parent, null),
                _ => null
            };
        }

        var oldParent = node.ParentNode;
        if (oldParent is null || oldParent is XmlDocument)
        {
            return null;
        }

        return direction switch
        {
            NudgeDirection.Up when node.PreviousSibling is { } previous
                => new XmlNodeMoveCommand(node, oldParent, previous),
            NudgeDirection.Down when node.NextSibling is { } next
                => new XmlNodeMoveCommand(node, oldParent, next.NextSibling),
            NudgeDirection.Left when oldParent is XmlElement parent && parent.ParentNode is XmlElement grandParent
                => new XmlNodeMoveCommand(node, grandParent, ReferenceEquals(node, parent.FirstChild) ? parent : parent.NextSibling),
            NudgeDirection.Right when node.PreviousSibling is XmlElement previousElement
                => new XmlNodeMoveCommand(node, previousElement, null),
            _ => null
        };
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

    private static string GetUniqueAttributeName(XmlElement owner, XmlAttribute attribute)
    {
        bool IsAvailable(string localName) => !owner.Attributes.Cast<XmlAttribute>()
            .Any(existing => existing.NamespaceURI == attribute.NamespaceURI && existing.LocalName == localName);

        var localName = attribute.LocalName;
        if (localName.Length == 1 && char.IsAsciiLetter(localName[0]))
        {
            for (var candidate = (char)(localName[0] + 1); char.IsAsciiLetter(candidate); candidate++)
            {
                if (IsAvailable(candidate.ToString()))
                {
                    return candidate.ToString();
                }
            }
        }

        var digitStart = localName.Length;
        while (digitStart > 0 && char.IsAsciiDigit(localName[digitStart - 1]))
        {
            digitStart--;
        }

        var stem = digitStart == localName.Length ? localName : localName[..digitStart];
        var number = digitStart == localName.Length
            ? 0
            : int.TryParse(localName[digitStart..], out var parsedNumber) ? parsedNumber : 0;

        string candidateName;
        do
        {
            candidateName = $"{stem}{++number}";
        }
        while (!IsAvailable(candidateName));

        return candidateName;
    }

    private void NotifyNudgeState()
    {
        OnPropertyChanged(nameof(CanNudgeUp));
        OnPropertyChanged(nameof(CanNudgeDown));
        OnPropertyChanged(nameof(CanNudgeLeft));
        OnPropertyChanged(nameof(CanNudgeRight));
    }

    private void RefreshVisibleTree()
    {
        foreach (var item in RootNodes)
        {
            RefreshVisibleTree(item);
        }
        if (SelectedNode is { IsPlaceholder: false } selected && selected.Node != _session.Document &&
            selected.Node.ParentNode is null && selected.Node is not XmlAttribute { OwnerElement: not null })
        {
            SelectedNode = RootNodes.FirstOrDefault(item => !item.IsPlaceholder);
        }
    }

    private void RefreshAfterHistoryChange(XmlNode? selectedNode, Command? edit = null, bool undo = false)
    {
        if (edit is EditXmlValueCommand valueEdit)
        {
            if (FindItem(valueEdit.Node) is { } editedItem)
            {
                if (valueEdit.Node is XmlElement)
                {
                    editedItem.RefreshChildren();
                    if (selectedNode is not null && FindItem(selectedNode) is null)
                    {
                        SelectedNode = editedItem;
                    }
                }
                else
                {
                    editedItem.NotifyChanged();
                    editedItem.ParentItem?.ParentItem?.NotifyIdentifyingTextChanged();
                }
            }
            return;
        }

        var expandedNodes = new HashSet<XmlNode>();
        foreach (var root in RootNodes)
        {
            CollectExpandedNodes(root, expandedNodes);
        }
        if (edit is EditXmlNamespaceCommand or XmlNodeRenameCommand or XmlNodeTypeChangeCommand)
        {
            expandedNodes = expandedNodes.Select(CurrentNode).ToHashSet();
            if (selectedNode is not null)
            {
                selectedNode = CurrentNode(selectedNode);
            }
        }

        UpdateTree();
        foreach (var root in RootNodes)
        {
            RestoreExpandedNodes(root, expandedNodes);
        }

        if (selectedNode is null)
        {
            return;
        }

        var item = FindItem(selectedNode);
        if (item is not null)
        {
            SelectedNode = item;
        }

        XmlNode CurrentNode(XmlNode node) => edit switch
        {
            EditXmlNamespaceCommand binding => binding.GetCurrentNode(node, undo),
            XmlNodeRenameCommand rename => rename.GetCurrentNode(node, undo),
            XmlNodeTypeChangeCommand change when ReferenceEquals(node, undo ? change.Replacement : change.Original) =>
                undo ? change.Original : change.Replacement,
            _ => node
        };
    }

    private static void CollectExpandedNodes(XmlNodeItem item, HashSet<XmlNode> expandedNodes)
    {
        if (item.IsPlaceholder)
        {
            return;
        }

        if (item.IsExpanded)
        {
            expandedNodes.Add(item.Node);
            foreach (var child in item.Children)
            {
                CollectExpandedNodes(child, expandedNodes);
            }
        }
    }

    private static void RestoreExpandedNodes(XmlNodeItem item, HashSet<XmlNode> expandedNodes)
    {
        if (!item.IsPlaceholder && expandedNodes.Contains(item.Node))
        {
            item.LoadChildren();
            item.IsExpanded = true;
            foreach (var child in item.Children)
            {
                RestoreExpandedNodes(child, expandedNodes);
            }
        }
    }

    private static void RefreshVisibleTree(XmlNodeItem item)
    {
        if (!item.ChildrenLoaded)
        {
            return;
        }
        item.RefreshChildren();
        foreach (var child in item.Children)
        {
            RefreshVisibleTree(child);
        }
    }

}

public enum NudgeDirection
{
    Up,
    Down,
    Left,
    Right
}
