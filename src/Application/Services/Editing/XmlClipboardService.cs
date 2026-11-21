using System.Text.RegularExpressions;
using System.Xml;
using Windows.ApplicationModel.DataTransfer;
using XmlNotepad;

namespace XmlNotepad.Services;

public static class XmlClipboardService
{
    private static readonly Regex AttributePattern = new(@"^\s*[^\s=]+\s*=\s*(?:""[^""]*""|'[^']*')\s*$", RegexOptions.Compiled);

    public static void Copy(XmlNode node)
    {
        var data = new DataPackage();
        data.SetText(node.OuterXml);
        Clipboard.SetContent(data);
    }

    public static async Task<string?> GetTextAsync()
    {
        var content = Clipboard.GetContent();
        return content.Contains(StandardDataFormats.Text) ? await content.GetTextAsync() : null;
    }

    public static IReadOnlyList<XmlNode> Parse(XmlDocument owner, XmlNode context, string content)
    {
        var namespaces = XmlHelpers.GetNamespaceScope(context);
        var parserContext = new XmlParserContext(owner.NameTable, namespaces, null, XmlSpace.None);
        if (AttributePattern.IsMatch(content))
        {
            using var reader = new XmlTextReader($"<clipboard {content} />", XmlNodeType.Element, parserContext)
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null
            };
            var wrapper = owner.ReadNode(reader) as XmlElement;
            return wrapper?.Attributes.Cast<XmlAttribute>().Select(attribute => (XmlNode)attribute.CloneNode(true)).ToArray() ?? [];
        }

        using var fragmentReader = new XmlTextReader(content, XmlNodeType.Element, parserContext)
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null,
            WhitespaceHandling = Settings.Instance.GetBoolean("PreserveWhitespace")
                ? WhitespaceHandling.All
                : WhitespaceHandling.Significant
        };
        var nodes = new List<XmlNode>();
        while (true)
        {
            var node = owner.ReadNode(fragmentReader);
            if (node is null)
            {
                break;
            }
            nodes.Add(node);
        }
        return nodes;
    }
}
