using System.Globalization;
using System.Xml;

namespace XmlNotepad.Services;

public sealed record XmlStatisticRow(string Name, long Count, long Characters)
{
    public string CountLabel => Count.ToString("N0", CultureInfo.CurrentCulture);
    public string CharacterLabel => Characters.ToString("N0", CultureInfo.CurrentCulture);
}

internal sealed record XmlStatistics(IReadOnlyList<XmlStatisticRow> Summary, IReadOnlyList<XmlStatisticRow> Names)
{
    internal static XmlStatistics Collect(XmlDocument document)
    {
        var elements = new Dictionary<string, (long Count, long Characters)>(StringComparer.Ordinal);
        var attributes = new Dictionary<(string Element, string Attribute), (long Count, long Characters)>();
        long elementCount = 0, attributeCount = 0, emptyElements = 0, textChars = 0, attributeChars = 0;
        long comments = 0, commentChars = 0, instructions = 0, instructionChars = 0;
        long whitespace = 0, whitespaceChars = 0, significantWhitespace = 0, significantWhitespaceChars = 0;
        for (var node = document.FirstChild; node is not null;)
        {
            switch (node)
            {
                case XmlElement element:
                    elementCount++;
                    if (element.IsEmpty)
                    {
                        emptyElements++;
                    }
                    var chars = 0L;
                    foreach (XmlNode child in element.ChildNodes)
                    {
                        if (child is XmlText or XmlCDataSection)
                        {
                            chars += child.Value?.Length ?? 0;
                        }
                    }
                    textChars += chars;
                    AddCount(elements, element.Name, chars);
                    foreach (XmlAttribute attribute in element.Attributes)
                    {
                        attributeCount++;
                        attributeChars += attribute.Value.Length;
                        AddCount(attributes, (element.Name, attribute.Name), attribute.Value.Length);
                    }
                    break;
                case XmlComment comment:
                    comments++;
                    commentChars += comment.Length;
                    break;
                case XmlProcessingInstruction instruction:
                    instructions++;
                    instructionChars += instruction.Data.Length;
                    break;
                case XmlWhitespace space:
                    whitespace++;
                    whitespaceChars += space.Length;
                    break;
                case XmlSignificantWhitespace space:
                    significantWhitespace++;
                    significantWhitespaceChars += space.Length;
                    break;
            }

            if (node.FirstChild is { } firstChild)
            {
                node = firstChild;
                continue;
            }
            while (node is not null && node.NextSibling is null)
            {
                node = node.ParentNode;
            }
            node = node?.NextSibling;
        }

        var names = new List<XmlStatisticRow>();
        var attributesByElement = attributes.OrderBy(pair => pair.Key.Attribute, StringComparer.Ordinal)
            .ToLookup(pair => pair.Key.Element, StringComparer.Ordinal);
        foreach (var (name, counts) in elements.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            names.Add(new XmlStatisticRow(name, counts.Count, counts.Characters));
            foreach (var attribute in attributesByElement[name])
            {
                names.Add(new XmlStatisticRow($"{name}/@{attribute.Key.Attribute}",
                    attribute.Value.Count, attribute.Value.Characters));
            }
        }

        XmlStatisticRow[] summary =
        [
            new("Elements", elementCount, textChars),
            new("Distinct element names", elements.Count, 0),
            new("Self-closing elements", emptyElements, 0),
            new("Attributes", attributeCount, attributeChars),
            new("Distinct element and attribute pairs", attributes.Count, 0),
            new("Comments", comments, commentChars),
            new("Processing instructions", instructions, instructionChars),
            new("Whitespace", whitespace, whitespaceChars),
            new("Significant whitespace", significantWhitespace, significantWhitespaceChars)
        ];
        return new XmlStatistics(summary, names);
    }

    private static void AddCount<TKey>(Dictionary<TKey, (long Count, long Characters)> counts, TKey key, long chars)
        where TKey : notnull
    {
        var current = counts.GetValueOrDefault(key);
        counts[key] = (current.Count + 1, current.Characters + chars);
    }
}
