using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private readonly XmlSearchService _searchService = new();
    private XmlSearchMatch? _currentSearchMatch;
    private (string Expression, FindFlags Flags, SearchFilter Filter)? _currentSearchQuery;

    private void FindReplace_Click(object sender, RoutedEventArgs e) => ShowFindReplaceBar(showReplace: true);

    private void IncrementalSearch_Click(object sender, RoutedEventArgs e) => ShowIncrementalSearch();

    private void IncrementalSearch_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        ShowIncrementalSearch();
    }

    private void ShowIncrementalSearch()
    {
        var filter = IsValueEditorFocused() ? SearchFilter.Text : SearchFilter.Names;
        FindReplaceBar.SetNamespaceContext(ViewModel.SelectedNode?.Node);
        FindReplaceBar.ShowIncremental(filter);
    }

    private void FindNext_Click(object sender, RoutedEventArgs e) => StepSearch(backwards: false);

    private void Find_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        ShowFindReplaceBar(showReplace: false);
    }

    private void Replace_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        ShowFindReplaceBar(showReplace: true);
    }

    private void FindNext_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        StepSearch(backwards: false);
    }

    private void FindPrevious_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        StepSearch(backwards: true);
    }

    private void StepSearch(bool backwards)
    {
        if (FindReplaceBar.Visibility == Visibility.Collapsed || FindReplaceBar.SearchText.Length == 0)
        {
            ShowFindReplaceBar(showReplace: false);
        }

        if (backwards)
        {
            FindReplaceBar.FindPrevious();
        }
        else
        {
            FindReplaceBar.FindNext();
        }
    }

    private void FindReplaceBar_FindRequested(object? sender, FindReplaceRequestEventArgs e) =>
        RunSearch(() => ShowMatch(FindMatch(e, includeCurrent: e.IsPreview), e));

    private void FindReplaceBar_ReplaceRequested(object? sender, FindReplaceRequestEventArgs e) =>
        RunSearch(() =>
        {
            ViewModel.CommitSelectedValue();
            var match = _currentSearchMatch;
            if (match?.Node != ViewModel.SelectedNode?.Node || _currentSearchQuery != GetSearchQuery(e))
            {
                match = null;
            }
            match ??= FindMatch(e, includeCurrent: true);
            if (match is null)
            {
                ShowMatch(null, e);
                return;
            }

            ViewModel.SelectNode(match.Node);
            ViewModel.ReplaceSearchMatch(match, e.Replacement);
            ResetSearchState();
            var selected = ViewModel.SelectedNode?.Node ?? match.Node;
            var nextOffset = (e.Flags & FindFlags.Backwards) != 0 ? match.Index :
                match.Index + (match.Length == 0 && e.Replacement.Length == 0 ? 1 : e.Replacement.Length);
            var resume = match with { Node = selected, Index = nextOffset };
            ShowMatch(_searchService.FindNext(ViewModel.Document, e.Expression, e.Flags, e.Filter, selected,
                resumeAfterReplace: resume), e);
        });

    private void FindReplaceBar_ReplaceAllRequested(object? sender, FindReplaceRequestEventArgs e) =>
        RunSearch(() =>
        {
            ViewModel.CommitSelectedValue();
            var matches = _searchService.GetMatches(ViewModel.Document, e.Expression, e.Flags, e.Filter,
                ViewModel.SelectedNode?.Node);
            var count = ViewModel.ReplaceAllSearchMatches(matches, e.Replacement);
            ResetSearchState();
            FindReplaceBar.ShowStatus(count == 0 ? "No matching nodes were found." : $"Replaced {count} occurrence(s).");
        });

    private XmlSearchMatch? FindMatch(FindReplaceRequestEventArgs request, bool includeCurrent)
    {
        ViewModel.CommitSelectedValue();
        return _searchService.FindNext(ViewModel.Document, request.Expression, request.Flags, request.Filter,
            ViewModel.SelectedNode?.Node, includeCurrent);
    }

    private static (string Expression, FindFlags Flags, SearchFilter Filter) GetSearchQuery(FindReplaceRequestEventArgs request) =>
        (request.Expression, request.Flags & ~FindFlags.Backwards, request.Filter);

    private void ShowMatch(XmlSearchMatch? match, FindReplaceRequestEventArgs request)
    {
        _currentSearchMatch = match;
        _currentSearchQuery = GetSearchQuery(request);
        if (match is null)
        {
            CloseValueDetails();
            FindReplaceBar.ShowStatus("No matching nodes were found.");
            return;
        }

        ViewModel.SelectNode(match.Node);
        if (ViewModel.SelectedNode is { } item)
        {
            RevealTreeItem(item);
        }
        if (match.IsName)
        {
            CloseValueDetails();
        }
        else
        {
            ShowValueDetails();
            var value = ViewModel.SelectedValue;
            var start = XmlValueLineEndings.GetEditorOffset(value, match.Index);
            var end = XmlValueLineEndings.GetEditorOffset(value, match.Index + match.Length);
            ValueEditor.Select(start, end - start);
        }
        FindReplaceBar.ShowStatus($"{_searchService.MatchNumber} of {_searchService.MatchCount}");
    }

    private void RunSearch(Action search)
    {
        try
        {
            search();
        }
        catch (RegexMatchTimeoutException)
        {
            ResetSearchState();
            FindReplaceBar.ShowStatus("The regular expression took too long to match.");
        }
        catch (Exception exception) when (exception is ArgumentException or XmlException or XPathException or InvalidOperationException)
        {
            ResetSearchState();
            FindReplaceBar.ShowStatus(exception.Message);
        }
    }

    private void FindReplaceBar_CloseRequested(object? sender, EventArgs e)
    {
        FindReplaceBar.HideFind();
        ResetSearchState();
        CloseValueDetails();
        XmlTree.Focus(FocusState.Programmatic);
    }

    private void ResetSearchState()
    {
        _currentSearchMatch = null;
        _currentSearchQuery = null;
        _searchService.Reset();
    }

    private void ShowFindReplaceBar(bool showReplace)
    {
        FindReplaceBar.SetNamespaceContext(ViewModel.SelectedNode?.Node);
        FindReplaceBar.ShowFind(showReplace);
    }

    private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e) =>
        FindReplaceBar.Width = Math.Min(800, Math.Max(0, e.NewSize.Width - 24));
}
