using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using XmlNotepad;

namespace XmlNotepad.Services;

public sealed record XmlSearchMatch(XmlNode Node, int Index, int Length, bool IsName);

public sealed class XmlSearchService
{
    private IReadOnlyList<XmlSearchMatch> _matches = [];
    private XmlDocument? _document;
    private string? _expression;
    private FindFlags _flags;
    private SearchFilter _filter;
    private int _position = -1;
    private XmlNode? _lastMatchNode;

    public int MatchCount => _matches.Count;
    public int MatchNumber => _position + 1;

    public XmlSearchMatch? FindNext(XmlDocument document, string expression, FindFlags flags, SearchFilter filter,
        XmlNode? selectedNode, bool includeCurrent = false, XmlSearchMatch? resumeAfterReplace = null)
    {
        var backwards = (flags & FindFlags.Backwards) != 0;
        GetMatches(document, expression, flags, filter, selectedNode);

        if (_matches.Count == 0)
        {
            return null;
        }

        if (resumeAfterReplace is not null)
        {
            _position = FindResumePosition(resumeAfterReplace, backwards);
        }
        else
        {
            if ((_position < 0 || selectedNode != _lastMatchNode) && selectedNode is not null)
            {
                var current = -1;
                for (var i = 0; i < _matches.Count; i++)
                {
                    var index = backwards ? _matches.Count - i - 1 : i;
                    if (_matches[index].Node == selectedNode)
                    {
                        current = index;
                        break;
                    }
                }
                _position = includeCurrent && current >= 0 ? current + (backwards ? 1 : -1) : current;
            }

            _position = backwards
                ? (_position <= 0 ? _matches.Count - 1 : _position - 1)
                : (_position + 1) % _matches.Count;
        }

        var match = _matches[_position];
        _lastMatchNode = match.Node;
        return match;
    }

    public IReadOnlyList<XmlSearchMatch> GetMatches(XmlDocument document, string expression, FindFlags flags,
        SearchFilter filter, XmlNode? context)
    {
        flags &= ~FindFlags.Backwards;
        if (_document != document || _expression != expression || _flags != flags || _filter != filter)
        {
            var matches = FindMatches(document, expression, flags, filter, context);
            _document = document;
            _expression = expression;
            _flags = flags;
            _filter = filter;
            _matches = matches;
            _position = -1;
            _lastMatchNode = null;
        }
        return _matches;
    }

    private int FindResumePosition(XmlSearchMatch replaced, bool backwards)
    {
        var anchor = replaced.Node.CreateNavigator();
        var direction = backwards ? XmlNodeOrder.After : XmlNodeOrder.Before;
        for (var i = 0; i < _matches.Count; i++)
        {
            var index = backwards ? _matches.Count - i - 1 : i;
            var match = _matches[index];
            if (match.Node == replaced.Node)
            {
                var isNext = match.IsName == replaced.IsName
                    ? (backwards ? match.Index < replaced.Index : match.Index >= replaced.Index)
                    : backwards == match.IsName;
                if (isNext)
                {
                    return index;
                }
            }
            else if (match.Node.CreateNavigator() is { } other && anchor?.ComparePosition(other) == direction)
            {
                return index;
            }
        }
        return backwards ? _matches.Count - 1 : 0;
    }

    public IReadOnlyList<XmlSearchMatch> FindMatches(XmlDocument document, string expression, FindFlags flags, SearchFilter filter, XmlNode? context)
    {
        if (string.IsNullOrEmpty(expression))
        {
            return [];
        }

        var isXPath = flags.HasFlag(FindFlags.XPath);
        var isRegex = flags.HasFlag(FindFlags.Regex);
        var startsWith = !isXPath && flags.HasFlag(FindFlags.StartsWith);
        var wholeWord = !isXPath && flags.HasFlag(FindFlags.WholeWord);

        var comparison = flags.HasFlag(FindFlags.MatchCase)
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
        Regex? regex = null;
        if (isRegex || wholeWord || startsWith)
        {
            var pattern = isRegex ? expression : Regex.Escape(expression);
            if (startsWith)
            {
                pattern = $@"\A(?:{pattern})";
            }
            if (wholeWord)
            {
                pattern = $"(?<![\\p{{L}}\\p{{M}}\\p{{N}}_])(?:{pattern})(?![\\p{{L}}\\p{{M}}\\p{{N}}_])";
            }
            var options = RegexOptions.CultureInvariant |
                (flags.HasFlag(FindFlags.MatchCase) ? RegexOptions.None : RegexOptions.IgnoreCase);
            regex = new Regex(pattern, options, TimeSpan.FromMilliseconds(150));
        }

        var matches = new List<XmlSearchMatch>();
        if (isXPath)
        {
            XmlNode namespaceContext = context ?? (XmlNode?)document.DocumentElement ?? document;
            var namespaces = XmlHelpers.GetNamespaceScope(namespaceContext);
            if (document.SelectNodes(expression, namespaces) is { } nodes)
            {
                foreach (XmlNode node in nodes)
                {
                    AddXPathMatch(matches, node);
                }
            }
            return matches;
        }

        bool searchNames = filter is SearchFilter.Everything or SearchFilter.Names;
        bool searchValues = filter is SearchFilter.Everything or SearchFilter.Text;
        foreach (var node in XmlDocumentTraversal.Descendants(document))
        {
            if (node is XmlDeclaration or XmlDocumentType or XmlEntityReference)
            {
                continue;
            }

            if (filter == SearchFilter.Comments)
            {
                if (node.NodeType == XmlNodeType.Comment)
                {
                    AddStringMatches(matches, node, node.Value, isName: false, expression, comparison, regex);
                }
                continue;
            }

            if (searchNames && node is XmlElement or XmlProcessingInstruction)
            {
                AddStringMatches(matches, node, node.Name, isName: true, expression, comparison, regex);
            }
            if (searchValues)
            {
                AddStringMatches(matches, node, node.Value, isName: false, expression, comparison, regex);
            }

            if (node is XmlElement element && element.HasAttributes)
            {
                foreach (XmlAttribute attribute in element.Attributes)
                {
                    if (searchNames)
                    {
                        AddStringMatches(matches, attribute, attribute.Name, isName: true, expression, comparison, regex);
                    }
                    if (searchValues)
                    {
                        AddStringMatches(matches, attribute, attribute.Value, isName: false, expression, comparison, regex);
                    }
                }
            }
        }

        return matches;
    }

    public void Reset()
    {
        _matches = [];
        _document = null;
        _expression = null;
        _position = -1;
        _lastMatchNode = null;
    }

    private static void AddXPathMatch(List<XmlSearchMatch> matches, XmlNode node)
    {
        if (node is XmlElement or XmlAttribute)
        {
            matches.Add(new XmlSearchMatch(node, 0, node.Name.Length, IsName: true));
        }
        else if (node.Value is { } value)
        {
            matches.Add(new XmlSearchMatch(node, 0, value.Length, IsName: false));
        }
    }

    private static void AddStringMatches(
        List<XmlSearchMatch> matches,
        XmlNode node,
        string? value,
        bool isName,
        string expression,
        StringComparison comparison,
        Regex? regex)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (regex is not null)
        {
            foreach (Match match in regex.Matches(value))
            {
                matches.Add(new XmlSearchMatch(node, match.Index, match.Length, isName));
            }
            return;
        }

        var index = value.IndexOf(expression, comparison);
        while (index >= 0)
        {
            matches.Add(new XmlSearchMatch(node, index, expression.Length, isName));
            index = value.IndexOf(expression, index + Math.Max(expression.Length, 1), comparison);
        }
    }
}
