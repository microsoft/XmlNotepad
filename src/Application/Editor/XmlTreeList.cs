using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using XmlNotepad.ViewModels;

namespace XmlNotepad;

public sealed partial class XmlTreeList : ListView
{
    internal event Action<XmlNodeItem, bool>? ExpansionRequested;

    protected override DependencyObject GetContainerForItemOverride() => new XmlTreeListItem(this);

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        if (element is XmlTreeListItem container && item is XmlNodeItem row)
        {
            container.SetBinding(AutomationProperties.NameProperty,
                new Binding { Source = row, Path = new PropertyPath(nameof(XmlNodeItem.DisplayName)) });
            AutomationProperties.SetLevel(container, row.Level);
            AutomationProperties.SetItemType(container, row.KindLabel);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new TreeListAutomationPeer(this);

    private sealed partial class TreeListAutomationPeer(XmlTreeList owner) : ListViewAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tree;
    }

    private sealed partial class XmlTreeListItem(XmlTreeList list) : ListViewItem
    {
        internal XmlNodeItem? Row => list.ItemFromContainer(this) as XmlNodeItem;

        internal void RequestExpansion(bool expanded)
        {
            if (Row is { CanExpand: true } item)
            {
                list.ExpansionRequested?.Invoke(item, expanded);
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new TreeItemAutomationPeer(this);
    }

    private sealed partial class TreeItemAutomationPeer(XmlTreeListItem owner)
        : ListViewItemAutomationPeer(owner), IExpandCollapseProvider
    {
        public ExpandCollapseState ExpandCollapseState => owner.Row switch
        {
            { CanExpand: true, IsExpanded: true } => ExpandCollapseState.Expanded,
            { CanExpand: true } => ExpandCollapseState.Collapsed,
            _ => ExpandCollapseState.LeafNode
        };

        public void Expand() => owner.RequestExpansion(true);

        public void Collapse() => owner.RequestExpansion(false);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TreeItem;

        protected override object GetPatternCore(PatternInterface patternInterface) =>
            patternInterface == PatternInterface.ExpandCollapse && owner.Row?.CanExpand == true
                ? this : base.GetPatternCore(patternInterface);
    }
}
