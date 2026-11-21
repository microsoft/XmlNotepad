using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace XmlNotepad.ViewModels;

public sealed partial class XmlTreeNodeContent : DependencyObject
{
    private readonly WeakReference<XmlNodeItem> _item;

    internal XmlTreeNodeContent(XmlNodeItem item) => _item = new(item);

    public XmlNodeItem? Item => _item.TryGetTarget(out var item) ? item : null;

    internal static XmlNodeItem? GetItem(object? node) =>
        (node as TreeViewNode)?.Content is XmlTreeNodeContent content ? content.Item : null;

    internal static void UpdateNodes(IList<TreeViewNode> target, IEnumerable<XmlNodeItem> source, NotifyCollectionChangedEventArgs change)
    {
        switch (change.Action)
        {
            case NotifyCollectionChangedAction.Add when change.NewItems is [XmlNodeItem added]:
                target.Insert(change.NewStartingIndex, added.TreeNode);
                break;
            case NotifyCollectionChangedAction.Remove when change.OldItems?.Count == 1:
                target.RemoveAt(change.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Move when change.NewItems?.Count == 1:
                var moved = target[change.OldStartingIndex];
                target.RemoveAt(change.OldStartingIndex);
                target.Insert(change.NewStartingIndex, moved);
                break;
            default:
                target.Clear();
                foreach (var item in source)
                {
                    target.Add(item.TreeNode);
                }
                break;
        }
    }
}
