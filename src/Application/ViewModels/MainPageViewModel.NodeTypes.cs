using System.Xml;
using XmlNotepad.Services;

namespace XmlNotepad.ViewModels;

public sealed partial class MainPageViewModel
{
    public bool CanConvertNode(XmlNodeItem? item, string type) => item?.CanConvertTo(type) == true;

    internal void ChangeSelectedNodeType(XmlNodeType target)
    {
        CommitSelectedValue();
        if (SelectedNode is { IsPlaceholder: false } item && XmlNodeTypeChangeCommand.CanConvert(item.Node, target))
        {
            var command = new XmlNodeTypeChangeCommand(item.Node, target);
            _undo.Push(command);
            RefreshAfterHistoryChange(item.Node, command);
        }
    }
}
