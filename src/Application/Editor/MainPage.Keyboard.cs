using System.Xml;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Core;
using XmlNotepad.Services;
using XmlNotepad.ViewModels;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private bool IsEditingText => IsValueEditorFocused() || FindReplaceBar.IsEditingText ||
        FocusManager.GetFocusedElement(XamlRoot) is TextBox || DiagnosticsPane.Documentation.IsTextFocused;

    private bool FocusSelectedTreeRow() =>
        ViewModel.SelectedNode is { IsPlaceholder: false } item &&
        XmlTree.ContainerFromItem(item) is ListViewItem row && row.Focus(FocusState.Programmatic) ||
        XmlTree.Focus(FocusState.Programmatic);

    private void Undo_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        if (!IsEditingText && ViewModel.CanUndo)
        {
            ViewModel.Undo();
            e.Handled = true;
        }
    }

    private void Redo_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        if (!IsEditingText && ViewModel.CanRedo)
        {
            ViewModel.Redo();
            e.Handled = true;
        }
    }

    private void FocusNextPane_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e) =>
        e.Handled = FocusAdjacentPane(backwards: false);

    private void FocusPreviousPane_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e) =>
        e.Handled = FocusAdjacentPane(backwards: true);

    private bool FocusAdjacentPane(bool backwards)
    {
        var panes = new List<UIElement> { XmlTree };
        if (FindReplaceBar.Visibility == Visibility.Visible)
        {
            panes.Add(FindReplaceBar);
        }
        if (DiagnosticsVisible)
        {
            panes.Add(DiagnosticsPane);
        }

        var current = -1;
        for (var focused = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
            focused is not null && current < 0;
            focused = VisualTreeHelper.GetParent(focused))
        {
            if (focused == ValueDetails)
            {
                current = 0;
            }
            else if (focused is UIElement element)
            {
                current = panes.IndexOf(element);
            }
        }

        var next = current < 0 ? 0 : (current + (backwards ? panes.Count - 1 : 1)) % panes.Count;
        return panes[next] switch
        {
            EditorDiagnosticsPane diagnostics => diagnostics.FocusSelectedTab(),
            FindReplaceBar find => find.FocusInput(),
            _ => FocusSelectedTreeRow()
        };
    }

    private async void XmlTree_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.OriginalSource is not (ListViewItem or ListView) || _inlineValueEditor is not null ||
            ViewModel.SelectedNode is not { IsPlaceholder: false } item)
        {
            return;
        }

        var control = IsKeyDown(VirtualKey.Control);
        var shift = IsKeyDown(VirtualKey.Shift);
        var alt = IsKeyDown(VirtualKey.Menu);
        if (control && shift && !alt && GetNudgeDirection(e.Key) is { } direction)
        {
            e.Handled = true;
            RunNudge(direction);
            if (ViewModel.SelectedNode is { } moved)
            {
                RevealTreeItem(moved);
            }
            return;
        }
        if (control && !shift && !alt && e.Key == VirtualKey.D)
        {
            e.Handled = true;
            DuplicateNode_Click(sender, new RoutedEventArgs());
            return;
        }
        if (e.Key == VirtualKey.Insert && !shift && !(control && alt))
        {
            e.Handled = true;
            if (!control && !alt)
            {
                await InsertSiblingAsync();
            }
            else
            {
                await RequestInsertAsync(control ? XmlNodeType.Element : XmlNodeType.Attribute, InsertPosition.Child);
            }
            return;
        }
        if (shift && !control && !alt && e.Key == VirtualKey.Enter)
        {
            e.Handled = BeginNodeValueEdit(item, null);
            return;
        }
        if (control || shift || alt)
        {
            return;
        }

        switch (e.Key)
        {
            case VirtualKey.Enter:
                var cell = XmlTree.ContainerFromItem(item) is ListViewItem row
                    ? FindDescendant<Grid>(row, grid => grid.Name == "ValueCell") : null;
                e.Handled = BeginNodeValueEdit(item, cell);
                break;
            case VirtualKey.Delete:
                e.Handled = true;
                if (ViewModel.CanDeleteSelectedNode)
                {
                    DeleteNode_Click(sender, new RoutedEventArgs());
                }
                break;
            case VirtualKey.Add:
                e.Handled = true;
                SetTreeRowExpansion(item, true);
                break;
            case VirtualKey.Subtract:
                e.Handled = true;
                SetTreeRowExpansion(item, false);
                break;
            case VirtualKey.Multiply:
                e.Handled = true;
                SetBranchExpansion(new[] { item }, true);
                break;
            case VirtualKey.Right:
                e.Handled = true;
                if (!item.IsExpanded)
                {
                    SetTreeRowExpansion(item, true);
                }
                else if (item.Children.Count > 0)
                {
                    FocusTreeItem(item.Children[0]);
                }
                break;
            case VirtualKey.Left:
                e.Handled = true;
                if (item.IsExpanded)
                {
                    SetTreeRowExpansion(item, false);
                }
                else if (item.Level > 1 && item.ParentItem is { } parent)
                {
                    FocusTreeItem(parent);
                }
                break;
        }
    }

    private static bool IsKeyDown(VirtualKey key) =>
        (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;

    private static NudgeDirection? GetNudgeDirection(VirtualKey key) => key switch
    {
        VirtualKey.Left => NudgeDirection.Left,
        VirtualKey.Right => NudgeDirection.Right,
        VirtualKey.Up => NudgeDirection.Up,
        VirtualKey.Down => NudgeDirection.Down,
        _ => null
    };
}
