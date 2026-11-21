using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Storage;
using XmlNotepad.Services;
using XmlNotepad.ViewModels;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private XmlNode? _draggedNode;
    private bool _openingDroppedFile;

    private void Node_DragStarting(TreeView sender, TreeViewDragItemsStartingEventArgs e)
    {
        if (XmlTreeNodeContent.GetItem(e.Items.FirstOrDefault()) is not { IsPlaceholder: false } item)
        {
            e.Cancel = true;
            return;
        }

        ViewModel.CommitSelectedValue();
        _draggedNode = item.Node;
        // The tree's native Move drop edits its visual nodes before the XML command can run.
        e.Data.RequestedOperation = DataPackageOperation.Copy;
        e.Data.SetText(item.Node.OuterXml);
    }

    private void Node_DropCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs e) => _draggedNode = null;

    private void Node_DragOver(object sender, DragEventArgs e)
    {
        if (_draggedNode is not { } source)
        {
            return;
        }

        e.Handled = true;
        e.AcceptedOperation = DataPackageOperation.None;
        if (TryGetNodeDrop(sender, e, source, out var target, out var position, out var copy))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.IsGlyphVisible = false;
            var relation = position == InsertPosition.Child ? "into" : position == InsertPosition.Before ? "before" : "after";
            e.DragUIOverride.Caption = $"{(copy ? "Copy" : "Move")} {relation} {target.Name}";
        }
    }

    private void Node_Drop(object sender, DragEventArgs e)
    {
        if (_draggedNode is not { } source)
        {
            return;
        }

        _draggedNode = null;
        e.Handled = true;
        e.AcceptedOperation = DataPackageOperation.None;
        if (!TryGetNodeDrop(sender, e, source, out var target, out var position, out var copy))
        {
            return;
        }

        try
        {
            if (ViewModel.DropNode(source, target, position, copy))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                if (ViewModel.SelectedNode is { } item)
                {
                    RevealTreeItem(item);
                }
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or XmlException)
        {
            ShowError($"The node could not be dropped: {exception.Message}");
        }
    }

    private bool TryGetNodeDrop(object sender, DragEventArgs e, XmlNode source,
        out XmlNode target, out InsertPosition position, out bool copy)
    {
        target = null!;
        position = InsertPosition.Child;
        copy = (e.Modifiers & DragDropModifiers.Control) != 0;
        if (sender is not TreeViewItem row || XmlTreeNodeContent.GetItem(XmlTree.NodeFromContainer(row)) is not { IsPlaceholder: false } item)
        {
            return false;
        }

        var y = e.GetPosition(row).Y;
        position = y < row.ActualHeight / 3 ? InsertPosition.Before
            : y > row.ActualHeight * 2 / 3 ? InsertPosition.After : InsertPosition.Child;
        target = item.Node;
        return ViewModel.CanDropNode(source, target, position, copy);
    }

    private void File_DragOver(object sender, DragEventArgs e)
    {
        if (_draggedNode is null && !_openingDroppedFile && CanOpenDrop(e.DataView) &&
            (e.AllowedOperations & DataPackageOperation.Copy) != 0)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Open file in XML Notepad";
            e.Handled = true;
        }
    }

    private async void File_Drop(object sender, DragEventArgs e)
    {
        if (_draggedNode is not null || _openingDroppedFile || !CanOpenDrop(e.DataView))
        {
            return;
        }

        e.Handled = true;
        _openingDroppedFile = true;
        try
        {
            string? path;
            var deferral = e.GetDeferral();
            try
            {
                path = null;
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    path = items.OfType<StorageFile>().Select(file => file.Path)
                        .FirstOrDefault(filePath => !string.IsNullOrWhiteSpace(filePath));
                }
                else if (await e.DataView.GetWebLinkAsync() is { } uri && Services.RemoteDocument.IsWebUri(uri))
                {
                    path = uri.AbsoluteUri;
                }
                e.AcceptedOperation = path is null ? DataPackageOperation.None : DataPackageOperation.Copy;
            }
            finally
            {
                deferral.Complete();
            }

            if (path is not null && await ConfirmSaveChangesAsync())
            {
                await TryOpenAsync(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or COMException)
        {
            ShowError($"The dropped file could not be opened: {exception.Message}");
        }
        finally
        {
            _openingDroppedFile = false;
        }
    }

    private static bool CanOpenDrop(DataPackageView data) =>
        data.Contains(StandardDataFormats.StorageItems) || data.Contains(StandardDataFormats.WebLink);
}
