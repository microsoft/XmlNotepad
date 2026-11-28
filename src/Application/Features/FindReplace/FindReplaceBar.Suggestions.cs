using System.Xml;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class FindReplaceBar
{
    private const int MaximumSuggestions = 8;
    private const int SuggestionScanLimit = 4096;

    private void UpdateSuggestions()
    {
        var comparison = _matchCaseOption.IsChecked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var comparer = _matchCaseOption.IsChecked ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var query = SearchText;
        IEnumerable<string> candidates = _history.FindItems.Reverse();
        if (query.Length > 0 && (_incrementalFilter == SearchFilter.Names ||
            _incrementalFilter is null && !_regexOption.IsChecked && !_xpathOption.IsChecked && _selectedFilter is 0 or 1) &&
            _namespaceContext?.OwnerDocument is { } document)
        {
            candidates = candidates.Concat(GetDocumentNames(document));
        }

        var suggestions = query.Length == 0 ? [] : candidates
            .Where(value => value.Length > query.Length && value.StartsWith(query, comparison))
            .Distinct(comparer)
            .Take(MaximumSuggestions)
            .ToArray();
        FindTextBox.ItemsSource = suggestions;
        FindTextBox.IsSuggestionListOpen = suggestions.Length > 0;
    }

    private static IEnumerable<string> GetDocumentNames(XmlDocument document)
    {
        var visited = 0;
        foreach (var node in XmlDocumentTraversal.Descendants(document))
        {
            if (visited++ >= SuggestionScanLimit)
            {
                yield break;
            }
            if (node is XmlElement element)
            {
                yield return element.Name;
                foreach (XmlAttribute attribute in element.Attributes)
                {
                    if (++visited >= SuggestionScanLimit)
                    {
                        yield break;
                    }
                    yield return attribute.Name;
                }
            }
            else if (node is XmlProcessingInstruction)
            {
                yield return node.Name;
            }
        }
    }
}
