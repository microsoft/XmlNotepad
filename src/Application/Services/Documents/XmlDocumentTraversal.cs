using System.Xml;

namespace XmlNotepad.Services;

internal static class XmlDocumentTraversal
{
    public static bool IsAttached(XmlDocument document, XmlNode? node)
    {
        while (node is not null && node != document)
        {
            node = node is XmlAttribute attribute ? attribute.OwnerElement : node.ParentNode;
        }
        return node == document;
    }

    public static IEnumerable<XmlNode> Descendants(XmlDocument document)
    {
        for (var node = document.FirstChild; node is not null;)
        {
            yield return node;
            if (node.FirstChild is { } child)
            {
                node = child;
                continue;
            }

            while (node is not null && node.NextSibling is null)
            {
                node = node.ParentNode;
            }
            node = node?.NextSibling;
        }
    }
}
