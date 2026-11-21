using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using XmlNotepad.Services;
using XmlNotepad.ViewModels;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private void InitializeTree()
    {
        foreach (var root in ViewModel.RootNodes)
        {
            XmlTree.RootNodes.Add(root.TreeNode);
        }
        ViewModel.RootNodes.CollectionChanged += (_, change) =>
            XmlTreeNodeContent.UpdateNodes(XmlTree.RootNodes, ViewModel.RootNodes, change);
    }

    private void ExpandBranch_Click(object sender, RoutedEventArgs e) => SetSelectedBranchExpansion(true);

    private void CollapseBranch_Click(object sender, RoutedEventArgs e) => SetSelectedBranchExpansion(false);

    private void ExpandAll_Click(object sender, RoutedEventArgs e) => SetBranchExpansion(ViewModel.RootNodes, true);

    private void CollapseAll_Click(object sender, RoutedEventArgs e) => SetBranchExpansion(ViewModel.RootNodes, false);

    private void SetSelectedBranchExpansion(bool expand)
    {
        if (ViewModel.SelectedNode is { IsPlaceholder: false } item)
        {
            SetBranchExpansion(new[] { item }, expand);
            XmlTree.Focus(FocusState.Programmatic);
        }
    }

    private static void SetBranchExpansion(IEnumerable<XmlNodeItem> roots, bool expand)
    {
        var pending = new Stack<XmlNodeItem>(roots.Reverse());
        var branches = new Stack<XmlNodeItem>();
        while (pending.TryPop(out var item))
        {
            if (item.IsPlaceholder)
            {
                continue;
            }
            if (expand)
            {
                item.LoadChildren();
                if (item.Children.Count > 0)
                {
                    branches.Push(item);
                }
            }
            else
            {
                item.IsExpanded = false;
            }
            foreach (var child in item.Children.Reverse())
            {
                pending.Push(child);
            }
        }
        while (branches.TryPop(out var branch))
        {
            branch.IsExpanded = true;
        }
    }

    private void ValidationIssue_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is XmlValidationIssue issue)
        {
            NavigateToValidationIssue(issue);
        }
    }

    private void NavigateToValidationIssue(XmlValidationIssue issue)
    {
        _lastNavigatedIssue = issue;
        if (issue.Node is { } node && node.OwnerDocument == ViewModel.Document)
        {
            ViewModel.SelectNode(node);
            if (ViewModel.SelectedNode is { } item && ReferenceEquals(item.Node, node))
            {
                RevealTreeItem(item);
            }
        }
    }

    private async void GoToLine_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.CanGoToLine)
        {
            return;
        }

        var (line, column) = ViewModel.SelectedSourcePosition;
        var dialog = new GoToLineDialog(ViewModel.LastIndexedLine, line, column);
        if (await ShowDialogAsync(dialog) == ContentDialogResult.Primary &&
            ViewModel.GoToLine(dialog.Line, dialog.Column) && ViewModel.SelectedNode is { } item)
        {
            RevealTreeItem(item);
        }
    }

    private void RevealTreeItem(XmlNodeItem item)
    {
        if (FindDescendant<ListViewBase>(XmlTree) is not { } list)
        {
            return;
        }

        var node = item.TreeNode;
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            if (!XmlTree.IsLoaded || !ReferenceEquals(ViewModel.SelectedNode, item))
            {
                return;
            }

            var index = list.Items.IndexOf(node);
            if (index >= 0)
            {
                list.SelectedIndex = index;
                list.ScrollIntoView(node);
            }
        });
    }

    internal static T? FindDescendant<T>(DependencyObject element) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            var child = VisualTreeHelper.GetChild(element, index);
            if ((child as T ?? FindDescendant<T>(child)) is { } match)
            {
                return match;
            }
        }

        return null;
    }
}
