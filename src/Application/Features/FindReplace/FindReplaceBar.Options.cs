using System.Xml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class FindReplaceBar
{
    private readonly MenuFlyout _optionsFlyout = new();
    private ToggleMenuFlyoutItem _matchCaseOption = null!;
    private ToggleMenuFlyoutItem _wholeWordOption = null!;
    private ToggleMenuFlyoutItem _regexOption = null!;
    private ToggleMenuFlyoutItem _xpathOption = null!;
    private ToggleMenuFlyoutItem _replaceBackwardsOption = null!;
    private MenuFlyoutSubItem _filterSubItem = null!;
    private MenuFlyoutSubItem _recentFindSubItem = null!;
    private MenuFlyoutSubItem _recentReplaceSubItem = null!;
    private MenuFlyoutSubItem _namespaceSubItem = null!;
    private readonly SearchHistoryService _history = new(App.SettingsService);
    private XmlNode? _namespaceContext;
    private int _selectedFilter;
    private static readonly string[] FilterNames = ["Everything", "Names", "Text", "Comments"];

    public void SetNamespaceContext(XmlNode? node) => _namespaceContext = node;

    private void SearchOption_Click(object? sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(sender, _regexOption) && _regexOption.IsChecked) _xpathOption.IsChecked = false;
        if (ReferenceEquals(sender, _xpathOption) && _xpathOption.IsChecked) _regexOption.IsChecked = false;
        SaveOptions();
        if (ReferenceEquals(sender, _xpathOption) && _xpathOption.IsChecked &&
            string.IsNullOrEmpty(FindTextBox.Text) && _namespaceContext is not null)
        {
            FindTextBox.Text = XmlHelpers.GetXPathLocation(_namespaceContext, XmlHelpers.GetNamespaceScope(_namespaceContext));
            FocusFindText();
        }
        SearchQueryChanged();
    }

    private void FilterOption_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: int filter })
        {
            _selectedFilter = filter;
            _filterSubItem.Text = $"Search in: {FilterNames[_selectedFilter]}";
            SaveOptions();
            SearchQueryChanged();
        }
    }

    private void SaveOptions()
    {
        var settings = App.SettingsService.Settings;
        settings["SearchMatchCase"] = _matchCaseOption.IsChecked;
        settings["SearchWholeWord"] = _wholeWordOption.IsChecked;
        settings["SearchXPath"] = _xpathOption.IsChecked;
        settings["SearchRegex"] = _regexOption.IsChecked;
        settings["SearchBackwards"] = _replaceBackwardsOption.IsChecked;
        settings["SearchFilter"] = _selectedFilter;
        App.SettingsService.Save();
        UpdateOptionPresentation();
    }

    private void UpdateOptionPresentation()
    {
        OptionsButton.IsEnabled = _incrementalFilter is null;
        if (_incrementalFilter is { } incrementalFilter)
        {
            SearchModeText.Text = incrementalFilter == SearchFilter.Names ? "Name starts with" : "Value starts with";
            SearchModeText.Visibility = Visibility.Visible;
            UpdateSearchDetails();
            return;
        }
        var modes = new List<string>();
        if (_xpathOption.IsChecked)
        {
            modes.Add("XPath");
        }
        else
        {
            if (_regexOption.IsChecked) modes.Add("Regex");
            if (_matchCaseOption.IsChecked) modes.Add("Match case");
            if (_wholeWordOption.IsChecked) modes.Add("Whole word");
            if (_selectedFilter != 0) modes.Add(FilterNames[_selectedFilter]);
        }
        if (ReplaceRow.Visibility == Visibility.Visible && _replaceBackwardsOption.IsChecked)
        {
            modes.Add("Replace backwards");
        }
        SearchModeText.Text = string.Join(" · ", modes);
        SearchModeText.Visibility = modes.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        _matchCaseOption.IsEnabled = !_xpathOption.IsChecked;
        _wholeWordOption.IsEnabled = !_xpathOption.IsChecked;
        _filterSubItem.IsEnabled = !_xpathOption.IsChecked;
        _namespaceSubItem.IsEnabled = _xpathOption.IsChecked;
        _replaceBackwardsOption.Visibility = ReplaceRow.Visibility;
        UpdateSearchDetails();
    }

    private FindReplaceRequestEventArgs CreateRequest(bool backwards = false, bool preview = false)
    {
        if (_incrementalFilter is { } incrementalFilter)
        {
            var incrementalFlags = FindFlags.StartsWith | (backwards ? FindFlags.Backwards : FindFlags.Normal);
            return new(FindTextBox.Text, string.Empty, incrementalFlags, incrementalFilter, preview);
        }
        var flags = _xpathOption.IsChecked ? FindFlags.XPath : FindFlags.Normal;
        if (!_xpathOption.IsChecked)
        {
            if (_regexOption.IsChecked) flags |= FindFlags.Regex;
            if (_matchCaseOption.IsChecked) flags |= FindFlags.MatchCase;
            if (_wholeWordOption.IsChecked) flags |= FindFlags.WholeWord;
        }
        if (backwards) flags |= FindFlags.Backwards;
        return new(FindTextBox.Text, ReplaceTextBox.Text, flags, (SearchFilter)_selectedFilter, preview);
    }

    private ToggleMenuFlyoutItem CreateOption(string text, bool isChecked)
    {
        var item = new ToggleMenuFlyoutItem { Text = text, IsChecked = isChecked };
        item.Click += SearchOption_Click;
        return item;
    }

    private void BuildOptionsFlyout()
    {
        _optionsFlyout.Items.Add(_matchCaseOption);
        _optionsFlyout.Items.Add(_wholeWordOption);
        _optionsFlyout.Items.Add(new MenuFlyoutSeparator());
        _optionsFlyout.Items.Add(_regexOption);
        _optionsFlyout.Items.Add(_xpathOption);
        _optionsFlyout.Items.Add(_replaceBackwardsOption);

        _namespaceSubItem = new MenuFlyoutSubItem { Text = "XPath namespaces" };
        _optionsFlyout.Items.Add(_namespaceSubItem);

        _filterSubItem = new MenuFlyoutSubItem { Text = $"Search in: {FilterNames[Math.Clamp(_selectedFilter, 0, FilterNames.Length - 1)]}" };
        for (var i = 0; i < FilterNames.Length; i++)
        {
            var option = new MenuFlyoutItem { Text = FilterNames[i], Tag = i };
            option.Click += FilterOption_Click;
            _filterSubItem.Items.Add(option);
        }
        _optionsFlyout.Items.Add(new MenuFlyoutSeparator());
        _optionsFlyout.Items.Add(_filterSubItem);

        _recentFindSubItem = new MenuFlyoutSubItem { Text = "Recent searches" };
        _recentReplaceSubItem = new MenuFlyoutSubItem { Text = "Recent replacements" };
        _optionsFlyout.Items.Add(new MenuFlyoutSeparator());
        _optionsFlyout.Items.Add(_recentFindSubItem);
        _optionsFlyout.Items.Add(_recentReplaceSubItem);
        RefreshHistoryFlyouts();
        RefreshNamespaceFlyout();
    }

    private void RefreshNamespaceFlyout()
    {
        if (_namespaceSubItem is null)
        {
            return;
        }

        _namespaceSubItem.Items.Clear();
        var namespaces = _namespaceContext is null ? null : XmlHelpers.GetNamespaceScope(_namespaceContext);
        var prefixes = namespaces?.Cast<string>()
            .Where(prefix => !string.IsNullOrEmpty(prefix) && prefix != "xmlns")
            .Select(prefix => (Prefix: prefix, Uri: namespaces!.LookupNamespace(prefix)))
            .Where(mapping => !string.IsNullOrEmpty(mapping.Uri))
            .ToArray() ?? [];

        if (!_xpathOption.IsChecked)
        {
            _namespaceSubItem.Items.Add(new MenuFlyoutItem { Text = "Enable XPath to view namespaces", IsEnabled = false });
        }
        else if (prefixes.Length == 0)
        {
            _namespaceSubItem.Items.Add(new MenuFlyoutItem { Text = "No prefixes in scope", IsEnabled = false });
        }
        else
        {
            foreach (var mapping in prefixes)
            {
                _namespaceSubItem.Items.Add(new MenuFlyoutItem
                {
                    Text = $"{mapping.Prefix}  {mapping.Uri}",
                    IsEnabled = false
                });
            }
        }
    }

    private void RefreshHistoryFlyouts()
    {
        if (_recentFindSubItem is null || _recentReplaceSubItem is null)
        {
            return;
        }

        PopulateHistory(_recentFindSubItem, _history.FindItems, FindHistoryItem_Click);
        PopulateHistory(_recentReplaceSubItem, _history.ReplaceItems, ReplaceHistoryItem_Click);
    }

    private static void PopulateHistory(MenuFlyoutSubItem menu, IReadOnlyList<string> items, RoutedEventHandler click)
    {
        menu.Items.Clear();
        if (items.Count == 0)
        {
            menu.Items.Add(new MenuFlyoutItem { Text = "No recent items", IsEnabled = false });
            return;
        }

        foreach (var value in items.Reverse())
        {
            var item = new MenuFlyoutItem { Text = value, Tag = value };
            item.Click += click;
            menu.Items.Add(item);
        }
    }

    private void FindHistoryItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: string value })
        {
            FindTextBox.Text = value;
            FocusFindText();
        }
    }

    private void ReplaceHistoryItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: string value })
        {
            SetExpanded(true);
            ReplaceTextBox.Text = value;
            ReplaceTextBox.Focus(FocusState.Programmatic);
        }
    }
}
