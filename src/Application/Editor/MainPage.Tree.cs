using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XmlNotepad.ViewModels;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private readonly XmlTreeRows _treeRows = new();
    private readonly List<XmlNodeItem> _observedTreeRoots = [];
    private bool _treeRowsPending;
    private bool _updatingTreeRows;

    private void InitializeTree()
    {
        XmlTree.ExpansionRequested += SetTreeRowExpansion;
        XmlTree.ItemsSource = _treeRows;
        ViewModel.RootNodes.CollectionChanged += (_, _) => ObserveTreeRoots();
        ObserveTreeRoots();
        UpdateTreeRows();
    }

    private void ObserveTreeRoots()
    {
        foreach (var root in _observedTreeRoots)
        {
            root.TreeChanged -= QueueTreeRowsUpdate;
        }
        _observedTreeRoots.Clear();
        foreach (var root in ViewModel.RootNodes)
        {
            root.TreeChanged += QueueTreeRowsUpdate;
            _observedTreeRoots.Add(root);
        }
        QueueTreeRowsUpdate();
    }

    private void QueueTreeRowsUpdate()
    {
        if (!_treeRowsPending)
        {
            _treeRowsPending = true;
            DispatcherQueue.TryEnqueue(UpdateTreeRows);
        }
    }

    private void UpdateTreeRows()
    {
        if (!_treeRowsPending || _updatingTreeRows)
        {
            return;
        }
        _treeRowsPending = false;
        _updatingTreeRows = true;
        try
        {
            _treeRows.Update(ViewModel.RootNodes);
            XmlTree.SelectedItem = ViewModel.SelectedNode;
        }
        finally
        {
            _updatingTreeRows = false;
        }
    }

    private void TreeRowExpansion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: XmlNodeItem item })
        {
            SetTreeRowExpansion(item, !item.IsExpanded);
        }
    }

    private void SetTreeRowExpansion(XmlNodeItem item, bool expand)
    {
        if (expand && !item.CanExpand)
        {
            return;
        }
        if (!expand)
        {
            SelectCollapsingAncestor(item);
        }
        else
        {
            item.LoadChildren();
        }
        item.IsExpanded = expand;
        UpdateTreeRows();
    }

    private void FocusTreeItem(XmlNodeItem item)
    {
        ViewModel.SelectedNode = item;
        if (XmlTree.ContainerFromItem(item) is ListViewItem row)
        {
            row.Focus(FocusState.Programmatic);
            return;
        }

        XmlTree.ScrollIntoView(item);
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            if (ReferenceEquals(ViewModel.SelectedNode, item))
            {
                FocusSelectedTreeRow();
            }
        });
    }

    private void SelectCollapsingAncestor(XmlNodeItem branch)
    {
        for (var parent = ViewModel.SelectedNode?.ParentItem; parent is not null; parent = parent.ParentItem)
        {
            if (ReferenceEquals(parent, branch))
            {
                EndInlineValueEdit(commit: true);
                ViewModel.CommitSelectedValue();
                ViewModel.SelectedNode = branch;
                return;
            }
        }
    }
}
