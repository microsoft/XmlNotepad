using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;
using System.Xml;
using System.Xml.Schema;
using Windows.System;
using Windows.UI.Core;
using XmlNotepad.Services;
using XmlNotepad.ViewModels;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private TextBox? _inlineValueEditor;
    private Control? _inlineValueControl;
    private Grid? _inlineValueCell;
    private XmlNodeItem? _inlineValueItem;

    private async void InsertSibling_Click(object sender, RoutedEventArgs e) => await InsertSiblingAsync();

    private async Task InsertSiblingAsync()
    {
        if (ViewModel.SiblingInsertType is { } type)
        {
            EndInlineValueEdit(commit: true);
            ViewModel.CommitSelectedValue();
            await RequestInsertAsync(type, InsertPosition.After);
        }
    }

    private void ChangeNodeType_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: string type } || !Enum.TryParse<XmlNodeType>(type, out var target))
        {
            return;
        }
        EndInlineValueEdit(commit: true);
        try
        {
            ViewModel.ChangeSelectedNodeType(target);
            if (ViewModel.SelectedNode is { } item)
            {
                RevealTreeItem(item);
            }
            XmlTree.Focus(FocusState.Programmatic);
        }
        catch (Exception exception) when (exception is XmlException or InvalidOperationException or ArgumentException)
        {
            ShowError($"Could not change the node type: {exception.Message}");
        }
    }

    private void NodeValue_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is Grid { DataContext: XmlNodeItem { IsPlaceholder: false } item } cell &&
            !ReferenceEquals(cell, _inlineValueCell))
        {
            e.Handled = BeginNodeValueEdit(item, cell);
        }
    }

    private void EditMultilineValue_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedNode is { } item)
        {
            BeginNodeValueEdit(item, null);
        }
    }

    private bool BeginNodeValueEdit(XmlNodeItem item, Grid? cell)
    {
        if (item.IsPlaceholder || !XmlNodeItem.IsValueEditable(item.Node))
        {
            return false;
        }

        EndInlineValueEdit(commit: true);
        ViewModel.CommitSelectedValue();
        ViewModel.SelectedNode = item;
        var value = ViewModel.SelectedValue;
        if (cell is null || value.Length > 320 || value.Contains('\r') || value.Contains('\n') || HasLongValueLine(value))
        {
            ShowValueDetails();
            ValueEditor.Focus(FocusState.Programmatic);
            ValueEditor.SelectAll();
            return true;
        }

        CloseValueDetails();
        var suggestions = GetValueSuggestions(item);
        var editorHeight = Math.Max(32, Typography.RowHeight);
        var suggestBox = suggestions.Count > 0 ? CreateValueSuggestBox(value, suggestions, cell.ActualWidth) : null;
        TextBox? textBox = null;
        if (suggestBox is not null)
        {
            suggestBox.Height = editorHeight;
            cell.Children.Add(suggestBox);
            suggestBox.ApplyTemplate();
            textBox = FindDescendant<TextBox>(suggestBox);
            if (textBox is null)
            {
                cell.Children.Remove(suggestBox);
                suggestBox = null;
            }
        }
        if (textBox is null)
        {
            textBox = new TextBox
            {
                Text = value,
                Height = editorHeight,
                VerticalAlignment = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            cell.Children.Add(textBox);
        }
        textBox.MaxLength = GetValueLengthLimit(value);
        textBox.FontFamily = Typography.Family;
        textBox.FontSize = Typography.Size;
        textBox.FontWeight = Typography.Weight;
        textBox.FontStyle = Typography.Style;
        EditorColorOverride.SetBackground(textBox, item.Palette.Brushes.EditorBackground);
        textBox.Padding = new Thickness(7, 0, 7, 0);
        AutomationProperties.SetAutomationId(textBox, "InlineNodeValueEditor");
        AutomationProperties.SetName(textBox, $"Value of {item.DisplayName}");
        _inlineValueControl = (Control?)suggestBox ?? textBox;
        _inlineValueEditor = textBox;
        _inlineValueCell = cell;
        _inlineValueItem = item;
        textBox.TextChanged += InlineValueEditor_TextChanged;
        textBox.PreviewKeyDown += InlineValueEditor_KeyDown;
        textBox.LostFocus += InlineValueEditor_LostFocus;
        textBox.Unloaded += InlineValueEditor_LostFocus;
        cell.Children[0].Visibility = Visibility.Collapsed;
        textBox.Focus(FocusState.Programmatic);
        textBox.SelectAll();
        if (suggestBox is not null)
        {
            suggestBox.IsSuggestionListOpen = true;
        }
        return true;
    }

    private IReadOnlyList<XmlSuggestion> GetValueSuggestions(XmlNodeItem item)
    {
        try
        {
            return ViewModel.GetExpectedValues(item.Node);
        }
        catch (Exception exception) when (exception is XmlException or XmlSchemaException or IOException or
            UnauthorizedAccessException or System.Net.WebException or System.Net.Http.HttpRequestException or ArgumentException)
        {
            return [];
        }
    }

    private AutoSuggestBox CreateValueSuggestBox(string value, IReadOnlyList<XmlSuggestion> suggestions, double cellWidth)
    {
        var box = new AutoSuggestBox
        {
            Text = value,
            Width = Math.Min(360, cellWidth),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            ItemsSource = suggestions,
            TextMemberPath = nameof(XmlSuggestion.Name),
            ItemTemplate = (DataTemplate)Resources["XmlValueSuggestionTemplate"],
            ItemContainerStyle = (Style)Resources["XmlValueSuggestionItemStyle"],
            MaxSuggestionListHeight = 240
        };
        box.TextChanged += (_, args) =>
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var matches = suggestions.Where(suggestion =>
                    suggestion.Name.StartsWith(box.Text, StringComparison.OrdinalIgnoreCase)).ToArray();
                box.ItemsSource = matches;
                box.IsSuggestionListOpen = matches.Length != 0;
            }
        };
        box.QuerySubmitted += (_, args) =>
        {
            EndInlineValueEdit(commit: true, (args.ChosenSuggestion as XmlSuggestion)?.Name ?? args.QueryText, restoreTreeFocus: true);
        };
        return box;
    }

    private void ValueEditor_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            ViewModel.CancelSelectedValueEdit();
        }
        else if (e.Key != VirtualKey.Enter ||
            !InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
        {
            return;
        }

        e.Handled = true;
        ViewModel.CommitSelectedValue();
        CloseValueDetails();
        FocusSelectedTreeRow();
    }

    private void InlineValueEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox && ReferenceEquals(textBox, _inlineValueEditor) &&
            ReferenceEquals(ViewModel.SelectedNode, _inlineValueItem))
        {
            ViewModel.SelectedValue = textBox.Text;
        }
    }

    private void InlineValueEditor_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_inlineValueControl is AutoSuggestBox { IsSuggestionListOpen: true } box)
        {
            if (e.Key == VirtualKey.Escape)
            {
                e.Handled = true;
                box.IsSuggestionListOpen = false;
            }
            return;
        }
        if (e.Key is VirtualKey.Enter or VirtualKey.Escape)
        {
            e.Handled = true;
            EndInlineValueEdit(commit: e.Key == VirtualKey.Enter, restoreTreeFocus: true);
        }
    }

    private void InlineValueEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(sender, _inlineValueEditor))
        {
            EndInlineValueEdit(commit: true);
        }
    }

    private void EndInlineValueEdit(bool commit, string? submittedValue = null, bool restoreTreeFocus = false)
    {
        if (_inlineValueEditor is not { } editor || _inlineValueControl is not { } control ||
            _inlineValueCell is not { } cell)
        {
            return;
        }

        var item = _inlineValueItem;
        _inlineValueEditor = null;
        _inlineValueControl = null;
        _inlineValueCell = null;
        _inlineValueItem = null;
        editor.TextChanged -= InlineValueEditor_TextChanged;
        editor.PreviewKeyDown -= InlineValueEditor_KeyDown;
        editor.LostFocus -= InlineValueEditor_LostFocus;
        editor.Unloaded -= InlineValueEditor_LostFocus;
        if (restoreTreeFocus)
        {
            FocusSelectedTreeRow();
        }
        cell.Children.Remove(control);
        cell.Children[0].Visibility = Visibility.Visible;

        if (ReferenceEquals(ViewModel.SelectedNode, item))
        {
            if (commit)
            {
                ViewModel.SelectedValue = submittedValue ?? editor.Text;
                ViewModel.CommitSelectedValue();
            }
            else
            {
                ViewModel.CancelSelectedValueEdit();
            }
        }
        ResumeDeferredDiagnostics();
    }
}
