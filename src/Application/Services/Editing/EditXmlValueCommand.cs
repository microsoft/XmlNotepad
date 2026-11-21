using System.Xml;
using XmlNotepad;

namespace XmlNotepad.Services;

internal sealed class EditXmlValueCommand(XmlNode node, string oldValue, string newValue) : Command
{
    internal XmlNode Node => node;

    public override string Name => "Edit value";

    public override bool IsNoop => string.Equals(oldValue, newValue, StringComparison.Ordinal);

    public override void Do() => SetValue(node, newValue);

    public override void Undo() => SetValue(node, oldValue);

    public override void Redo() => SetValue(node, newValue);

    private static void SetValue(XmlNode node, string value)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            node.InnerText = value;
        }
        else
        {
            node.Value = value;
        }
    }
}
