using System.Xml;

namespace XmlNotepad.Services;

internal sealed class SourceXmlDocument : XmlDocument
{
    private readonly List<(XmlNode Node, int Line, int Column)> _positions = [];
    private readonly long _maximumNodes = Settings.Instance.GetLong("MaximumLineIndex");
    private IXmlLineInfo? _lineInfo;
    private bool _positionsOutOfOrder;

    public int LastIndexedLine => _positions.Count == 0 ? 0 : _positions[^1].Line;

    internal Dictionary<XmlNode, (int Line, int Column)> GetPositions(IEnumerable<XmlNode> nodes)
    {
        var pending = nodes.ToHashSet();
        var found = new Dictionary<XmlNode, (int Line, int Column)>();
        foreach (var (node, line, column) in _positions)
        {
            if (pending.Count == 0)
            {
                break;
            }
            if (pending.Remove(node))
            {
                found.Add(node, (line, column));
            }
        }
        return found;
    }

    internal void RemapPositions(IReadOnlyDictionary<XmlNode, XmlNode> replacements)
    {
        if (replacements.Count == 0)
        {
            return;
        }

        for (var index = 0; index < _positions.Count; index++)
        {
            var (node, line, column) = _positions[index];
            if (replacements.TryGetValue(node, out var replacement))
            {
                _positions[index] = (replacement, line, column);
            }
        }
    }

    public override void Load(XmlReader reader)
    {
        _positions.Clear();
        _positionsOutOfOrder = false;
        _lineInfo = reader as IXmlLineInfo;
        try
        {
            base.Load(reader);
            if (_positionsOutOfOrder)
            {
                _positions.Sort(static (left, right) => (left.Line, left.Column).CompareTo((right.Line, right.Column)));
            }
        }
        finally
        {
            _lineInfo = null;
        }
    }

    internal (int Line, int Column)? FindPosition(XmlNode node)
    {
        foreach (var position in _positions)
        {
            if (position.Node == node)
            {
                return (position.Line, position.Column);
            }
        }

        return null;
    }

    public XmlNode? FindNode(int line, int column)
    {
        var low = 0;
        var high = _positions.Count;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            var position = _positions[middle];
            if ((position.Line, position.Column).CompareTo((line, column)) < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        if (low > 0 && _positions[low - 1].Line == line &&
            (low == _positions.Count || (_positions[low].Line, _positions[low].Column) != (line, column)))
        {
            low--;
        }

        for (var index = low; index < _positions.Count; index++)
        {
            if (XmlDocumentTraversal.IsAttached(this, _positions[index].Node))
            {
                return _positions[index].Node;
            }
        }

        for (var index = low - 1; index >= 0; index--)
        {
            if (XmlDocumentTraversal.IsAttached(this, _positions[index].Node))
            {
                return _positions[index].Node;
            }
        }

        return null;
    }

    private T Track<T>(T node) where T : XmlNode
    {
        if (_lineInfo is { LineNumber: > 0 } info && _positions.Count < _maximumNodes)
        {
            var location = (info.LineNumber, info.LinePosition);
            if (!_positionsOutOfOrder && _positions.Count > 0)
            {
                var previous = _positions[^1];
                _positionsOutOfOrder = location.CompareTo((previous.Line, previous.Column)) < 0;
            }

            _positions.Add((node, location.LineNumber, location.LinePosition));
        }

        return node;
    }

    public override XmlElement CreateElement(string? prefix, string localName, string? namespaceURI) =>
        Track(base.CreateElement(prefix, localName, namespaceURI));

    public override XmlAttribute CreateAttribute(string? prefix, string localName, string? namespaceURI) =>
        Track(base.CreateAttribute(prefix, localName, namespaceURI));

    public override XmlText CreateTextNode(string? text) => Track(base.CreateTextNode(text));
    public override XmlCDataSection CreateCDataSection(string? data) => Track(base.CreateCDataSection(data));
    public override XmlComment CreateComment(string? data) => Track(base.CreateComment(data));
    public override XmlSignificantWhitespace CreateSignificantWhitespace(string? text) => Track(base.CreateSignificantWhitespace(text));
    public override XmlWhitespace CreateWhitespace(string? text) => Track(base.CreateWhitespace(text));
    public override XmlEntityReference CreateEntityReference(string name) => Track(base.CreateEntityReference(name));

    public override XmlProcessingInstruction CreateProcessingInstruction(string target, string? data) =>
        Track(base.CreateProcessingInstruction(target, data));

    public override XmlDeclaration CreateXmlDeclaration(string version, string? encoding, string? standalone) =>
        Track(base.CreateXmlDeclaration(version, encoding, standalone));

    public override XmlDocumentType CreateDocumentType(string name, string? publicId, string? systemId, string? internalSubset) =>
        Track(base.CreateDocumentType(name, publicId, systemId, internalSubset));
}
