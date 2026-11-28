using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using XmlNotepad.ViewModels;

namespace XmlNotepad;

public sealed partial class SchemaManagerDialog
{
    private readonly UndoManager _history = new(1000);
    private bool _refreshingRows;

    private readonly record struct SchemaRow(SchemaListItem Item, bool Enabled);

    private SchemaListItem? SingleSelectedSchema => SchemaList.SelectedItems.Count == 1
        ? SchemaList.SelectedItems[0] as SchemaListItem : null;

    private SchemaListItem[] CaptureSelection() => SchemaList.SelectedItems.OfType<SchemaListItem>().ToArray();

    private SchemaRow[] CaptureRows() => _items.Select(item => new SchemaRow(item, item.IsEnabled)).ToArray();

    private void EditSchemas(string name, IEnumerable<SchemaRow> rows, SchemaListItem[] selected)
    {
        var before = CaptureRows();
        var after = rows.ToArray();
        if (before.SequenceEqual(after))
        {
            if (!CaptureSelection().SequenceEqual(selected))
            {
                RestoreSelection(selected);
            }
            return;
        }

        var previousSelection = CaptureSelection();
        _history.Push(new SchemaEditCommand(name,
            () => RestoreRows(after, selected), () => RestoreRows(before, previousSelection)));
        UpdateSchemaCommands();
    }

    private void RemoveSchemas(HashSet<SchemaListItem> selection, string name)
    {
        if (selection.Count > 0)
        {
            EditSchemas(name, CaptureRows().Where(row => !selection.Contains(row.Item)), []);
        }
    }

    private void RestoreRows(SchemaRow[] rows, SchemaListItem[] selected)
    {
        var previousSelection = CaptureSelection();
        _refreshingRows = true;
        try
        {
            _items.Clear();
            foreach (var row in rows)
            {
                row.Item.IsEnabled = row.Enabled;
                _items.Add(row.Item);
            }
            RefreshSchemaList(selected);
        }
        finally
        {
            _refreshingRows = false;
        }
        if (!previousSelection.SequenceEqual(selected))
        {
            UpdateSchemaSelection();
        }
    }

    private void SchemaSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SchemaSortBox is null || SchemaList is null)
        {
            return;
        }

        _refreshingRows = true;
        try
        {
            RefreshSchemaList(CaptureSelection());
        }
        finally
        {
            _refreshingRows = false;
        }
    }

    private void RefreshSchemaList(SchemaListItem[] selected)
    {
        var rows = SchemaSortBox.SelectedIndex switch
        {
            1 => _items.OrderBy(item => item.FileName, StringComparer.CurrentCultureIgnoreCase).ToArray(),
            2 => _items.OrderByDescending(item => item.FileName, StringComparer.CurrentCultureIgnoreCase).ToArray(),
            3 => _items.OrderBy(item => item.TargetNamespace, StringComparer.Ordinal).ToArray(),
            4 => _items.OrderByDescending(item => item.TargetNamespace, StringComparer.Ordinal).ToArray(),
            _ => _items.ToArray()
        };
        SchemaList.ItemsSource = rows;
        RestoreSelection(selected);
    }

    private void RestoreSelection(SchemaListItem[] selected)
    {
        SchemaList.SelectedItems.Clear();
        foreach (var item in selected)
        {
            SchemaList.SelectedItems.Add(item);
        }
        if (selected.Length > 0)
        {
            SchemaList.ScrollIntoView(selected[0]);
        }
    }

    private void UpdateSchemaCommands()
    {
        var selectedCount = SchemaList.SelectedItems.Count;
        CopySchemaButton.IsEnabled = CutSchemaButton.IsEnabled = RemoveSchemaButton.IsEnabled = !_working && selectedCount > 0;
        GenerateSchemaButton.IsEnabled = !_working && selectedCount == 1;
        SetSchemaLocationButton.IsEnabled = SchemaLocationBox.IsEnabled = !_working && selectedCount <= 1;
        SelectAllSchemasButton.IsEnabled = !_working && _items.Count > 0;
        ClearSchemaListButton.IsEnabled = SelectAllSchemasButton.IsEnabled;
        UndoSchemaButton.IsEnabled = !_working && _history.CanUndo;
        RedoSchemaButton.IsEnabled = !_working && _history.CanRedo;
    }

    private void UndoSchema_Click(object sender, RoutedEventArgs e) => UndoSchema();
    private void RedoSchema_Click(object sender, RoutedEventArgs e) => RedoSchema();

    private void UndoSchema()
    {
        if (!_working)
        {
            _history.Undo();
            UpdateSchemaCommands();
        }
    }

    private void RedoSchema()
    {
        if (!_working)
        {
            _history.Redo();
            UpdateSchemaCommands();
        }
    }

    private void UndoSchema_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        UndoSchema();
        e.Handled = true;
    }

    private void RedoSchema_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        RedoSchema();
        e.Handled = true;
    }

    private async void CutSchema_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        e.Handled = true;
        await CopySchemaAsync(cut: true);
    }

    private async void CopySchema_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        e.Handled = true;
        await CopySchemaAsync(cut: false);
    }

    private async void PasteSchema_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        e.Handled = true;
        await PasteSchemasAsync();
    }

    private void RemoveSchema_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        e.Handled = true;
        RemoveSelectedSchemas();
    }

    private void SelectAllSchemas_Click(object sender, RoutedEventArgs e) => SelectAllSchemas();

    private void ClearSchemaList_Click(object sender, RoutedEventArgs e)
    {
        if (!_working && _items.Count > 0)
        {
            EditSchemas("Clear schema list", [], []);
        }
    }

    private void SelectAllSchemas_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
    {
        e.Handled = true;
        SelectAllSchemas();
    }

    private void SelectAllSchemas()
    {
        if (!_working)
        {
            SchemaList.SelectAll();
        }
    }

    private sealed class SchemaEditCommand(string name, Action apply, Action undo) : Command
    {
        public override string Name => name;
        public override bool IsNoop => false;
        public override void Do() => apply();
        public override void Undo() => undo();
        public override void Redo() => apply();
    }
}
