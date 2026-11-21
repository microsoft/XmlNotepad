using System.Xml;

namespace XmlNotepad.ViewModels;

internal sealed class XmlNodeLabels
{
    private HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<XmlElement, XmlAttribute>? _identifiers;

    internal bool Enabled { get; private set; }

    internal void ReadOptions(Settings settings)
    {
        Enabled = settings.GetBoolean("SchemaAwareText");
        _names = new HashSet<string>(
            settings.GetString("SchemaAwareNames").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);
    }

    internal bool SetIdentifiers(IReadOnlyDictionary<XmlElement, XmlAttribute>? identifiers)
    {
        var previous = _identifiers;
        _identifiers = identifiers;
        return (previous?.Count ?? 0) != (identifiers?.Count ?? 0) ||
            previous is not null && identifiers is not null &&
            previous.Any(pair => !identifiers.TryGetValue(pair.Key, out var attribute) || attribute != pair.Value);
    }

    internal string GetLabel(XmlNode? node)
    {
        if (!Enabled || node is not XmlElement element)
        {
            return string.Empty;
        }

        if (_identifiers?.TryGetValue(element, out var identifier) == true &&
            identifier.OwnerElement == element && GetSuffix(identifier.Value) is { } typedLabel)
        {
            return typedLabel;
        }
        if (GetSuffix(element.GetAttribute("id", XmlStandardUris.XmlUri)) is { } xmlId)
        {
            return xmlId;
        }
        foreach (XmlAttribute attribute in element.Attributes)
        {
            if (_names.Contains(attribute.Name) && GetSuffix(attribute.Value) is { } attributeLabel)
            {
                return attributeLabel;
            }
        }
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child is XmlElement && _names.Contains(child.Name) && XmlNodeItem.IsValueEditable(child) &&
                GetSuffix(child.InnerText) is { } childLabel)
            {
                return childLabel;
            }
        }
        return string.Empty;
    }

    private static string? GetSuffix(string value)
    {
        const int limit = 96;
        var trimmed = value.AsSpan().Trim();
        if (trimmed.IsEmpty)
        {
            return null;
        }
        var length = trimmed.Length > limit && char.IsHighSurrogate(trimmed[limit - 1]) ? limit - 1 : limit;
        var text = trimmed[..Math.Min(length, trimmed.Length)].ToString()
            .Replace("\r\n", " ", StringComparison.Ordinal).Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
        return ": " + text + (trimmed.Length > limit ? "…" : string.Empty);
    }
}
