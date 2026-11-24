using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using XmlNotepad.Services;
using XmlNotepad.ViewModels;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private void ExpandBranch_Click(object sender, RoutedEventArgs e) => SetSelectedBranchExpansion(true);

    private void CollapseBranch_Click(object sender, RoutedEventArgs e) => SetSelectedBranchExpansion(false);

    private void ExpandAll_Click(object sender, RoutedEventArgs e) => SetBranchExpansion(ViewModel.RootNodes, true);

    private void CollapseAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var root in ViewModel.RootNodes)
        {
            SelectCollapsingAncestor(root);
        }
        SetBranchExpansion(ViewModel.RootNodes, false);
    }

    private void SetSelectedBranchExpansion(bool expand)
    {
        if (ViewModel.SelectedNode is { IsPlaceholder: false } item)
        {
            SetBranchExpansion(new[] { item }, expand);
            XmlTree.Focus(FocusState.Programmatic);
        }
    }

    private void SetBranchExpansion(IReadOnlyList<XmlNodeItem> roots, bool expand)
    {
        var pending = new Stack<XmlNodeItem>();
        for (var index = roots.Count - 1; index >= 0; index--)
        {
            pending.Push(roots[index]);
        }
        while (pending.TryPop(out var item))
        {
            if (item.IsPlaceholder)
            {
                continue;
            }
            if (expand)
            {
                item.LoadChildren();
            }
            var children = item.Children;
            item.IsExpanded = expand && children.Count > 0;
            for (var index = children.Count - 1; index >= 0; index--)
            {
                pending.Push(children[index]);
            }
        }
        UpdateTreeRows();
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
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            if (!XmlTree.IsLoaded || !ReferenceEquals(ViewModel.SelectedNode, item))
            {
                return;
            }

            UpdateTreeRows();
            if (_treeRows.Contains(item))
            {
                XmlTree.SelectedItem = item;
                XmlTree.ScrollIntoView(item);
            }
        });
    }

    internal static T? FindDescendant<T>(DependencyObject element, Predicate<T>? filter = null) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            var child = VisualTreeHelper.GetChild(element, index);
            if (child is T match && (filter is null || filter(match)))
            {
                return match;
            }
            if (FindDescendant(child, filter) is { } descendant)
            {
                return descendant;
            }
        }

        return null;
    }
}
