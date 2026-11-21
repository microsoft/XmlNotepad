using System.Xml;
using System.Xml.Schema;

namespace XmlNotepad.Services;

internal static class XmlDocumentCharacterIssues
{
    public static void AddTo(XmlDocument document, List<XmlValidationIssue> issues)
    {
        foreach (var node in XmlDocumentTraversal.Descendants(document))
        {
            if (node.Value is { Length: > 0 } value)
            {
                AddValueIssues(value, node is XmlText ? node.ParentNode ?? node : node, issues);
            }
            if (node is XmlElement { HasAttributes: true } element)
            {
                foreach (XmlAttribute attribute in element.Attributes)
                {
                    AddValueIssues(attribute.Value, attribute, issues);
                }
            }
        }
    }

    private static void AddValueIssues(string value, XmlNode node, List<XmlValidationIssue> issues)
    {
        foreach (var message in XmlCharacterDiagnostics.GetErrors(value))
        {
            issues.Add(new XmlValidationIssue(XmlSeverityType.Error, message, node));
        }
    }
}
