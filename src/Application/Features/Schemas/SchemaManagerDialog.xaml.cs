using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using System.Xml;
using System.Xml.Schema;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using XmlNotepad.Services;
using XmlNotepad.ViewModels;

namespace XmlNotepad;

public sealed partial class SchemaManagerDialog : ContentDialog
{
    private readonly List<SchemaListItem> _items = [];

    public bool Applied { get; private set; }
    private bool _closed;
    private bool _working;

    public SchemaManagerDialog(XamlRoot root)
    {
        InitializeComponent();
        XamlRoot = root;
        UpdateSize();
        XamlRoot.Changed += XamlRoot_Changed;
        Closed += (_, _) =>
        {
            _closed = true;
            XamlRoot.Changed -= XamlRoot_Changed;
        };
        foreach (var entry in App.SettingsService.SchemaCache.GetSchemas())
        {
            _items.Add(new SchemaListItem(entry.Location, entry.TargetNamespace, !entry.Disabled));
        }
        SchemaSortBox.SelectedIndex = 0;
        UpdateSchemaCommands();
    }

    private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateSize();

    private void UpdateSize()
    {
        LayoutRoot.Width = Math.Clamp(XamlRoot.Size.Width - 96, 0, 800);
        LayoutRoot.Height = Math.Clamp(XamlRoot.Size.Height - 190, 0, 460);
    }

    private async void AddSchema_Click(object sender, RoutedEventArgs e) => await RunOperationAsync(async () =>
    {
        var picker = new FileOpenPicker(XamlRoot.ContentIslandEnvironment.AppWindowId);
        picker.FileTypeFilter.Add(".xsd");
        var files = await picker.PickMultipleFilesAsync();
        if (files.Count > 0)
        {
            await AddLocationsAsync(files.Select(file => file.Path), SchemaSource.ParseLocation);
        }
    });

    private async void SetSchemaLocation_Click(object sender, RoutedEventArgs e) => await RunOperationAsync(async () =>
    {
        if (SingleSelectedSchema is not { } selected)
        {
            await AddLocationsAsync(new[] { SchemaLocationBox.Text }, SchemaSource.ParseLocation);
            return;
        }
        var location = SchemaSource.ParseLocation(SchemaLocationBox.Text);
        if (_items.Any(item => !ReferenceEquals(item, selected) && item.Location == location))
        {
            throw new XmlException("That schema location is already in the list.");
        }
        var replacement = await Task.Run(() => LoadItem(location, selected.IsEnabled));
        if (!_closed)
        {
            EditSchemas("Replace schema location",
                CaptureRows().Select(row => ReferenceEquals(row.Item, selected) ? new SchemaRow(replacement, replacement.IsEnabled) : row),
                [replacement]);
        }
    });

    private async void CopySchema_Click(object sender, RoutedEventArgs e) => await CopySchemaAsync(cut: false);

    private async void CutSchema_Click(object sender, RoutedEventArgs e) => await CopySchemaAsync(cut: true);

    private Task CopySchemaAsync(bool cut) => RunOperationAsync(() =>
    {
        var selection = CaptureSelection().ToHashSet();
        if (selection.Count > 0)
        {
            var data = new DataPackage();
            data.SetText(string.Join(Environment.NewLine, _items.Where(selection.Contains).Select(item =>
                string.IsNullOrEmpty(item.TargetNamespace) ? item.Location.AbsoluteUri : $"{item.TargetNamespace}\t{item.Location.AbsoluteUri}")));
            Clipboard.SetContent(data);
            if (cut)
            {
                RemoveSchemas(selection, "Cut schemas");
            }
        }
        return Task.CompletedTask;
    });

    private async void PasteSchema_Click(object sender, RoutedEventArgs e) => await PasteSchemasAsync();

    private Task PasteSchemasAsync() => RunOperationAsync(async () =>
    {
        var text = await XmlClipboardService.GetTextAsync();
        if (!string.IsNullOrWhiteSpace(text))
        {
            await AddLocationsAsync(text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(line => !string.IsNullOrWhiteSpace(line)), SchemaSource.ParseClipboardLine);
        }
    });

    private async Task AddLocationsAsync(IEnumerable<string> locations, Func<string, Uri> parseLocation)
    {
        var (additions, errors) = await Task.Run(() => ReadBatch(locations, parseLocation));
        if (_closed)
        {
            return;
        }
        if (additions.Count > 0)
        {
            var rows = CaptureRows().ToList();
            var selected = new List<SchemaListItem>();
            foreach (var addition in additions)
            {
                var index = rows.FindIndex(row => row.Item.Location == addition.Location);
                if (index >= 0)
                {
                    var existing = rows[index].Item;
                    var item = existing.TargetNamespace == addition.TargetNamespace ? existing : addition;
                    rows[index] = new SchemaRow(item, true);
                    selected.Add(item);
                }
                else
                {
                    rows.Add(new SchemaRow(addition, true));
                    selected.Add(addition);
                }
            }
            EditSchemas("Add schemas", rows, selected.ToArray());
        }
        if (errors.Count > 0)
        {
            var message = string.Join(Environment.NewLine, errors.Take(3));
            ErrorInfoBar.Message = errors.Count <= 3 ? message : $"{message}{Environment.NewLine}{errors.Count - 3} more locations failed.";
            ErrorInfoBar.IsOpen = true;
        }
    }

    private static (List<SchemaListItem> Items, List<string> Errors) ReadBatch(
        IEnumerable<string> locations, Func<string, Uri> parseLocation)
    {
        var seen = new HashSet<Uri>();
        var items = new List<SchemaListItem>();
        var errors = new List<string>();
        foreach (var input in locations)
        {
            try
            {
                var location = parseLocation(input);
                if (seen.Add(location))
                {
                    items.Add(LoadItem(location, true));
                }
            }
            catch (Exception exception) when (IsSchemaOperationError(exception))
            {
                var label = input.Length <= 160 ? input : input[..160] + "…";
                errors.Add($"{label}: {exception.Message}");
            }
        }
        return (items, errors);
    }

    private static SchemaListItem LoadItem(Uri location, bool enabled)
    {
        var resolver = SchemaSource.CreateResolver(location, App.Settings.Resolver);
        var schema = SchemaSource.Read(location, resolver);
        return new SchemaListItem(location, schema.TargetNamespace ?? string.Empty, enabled);
    }

    private async Task RunOperationAsync(Func<Task> operation)
    {
        if (_working || _closed)
        {
            return;
        }
        _working = true;
        IsPrimaryButtonEnabled = false;
        SchemaList.IsEnabled = false;
        UpdateSchemaCommands();
        try
        {
            ErrorInfoBar.IsOpen = false;
            await operation();
        }
        catch (Exception exception) when (IsSchemaOperationError(exception))
        {
            if (!_closed)
            {
                ErrorInfoBar.Message = exception.Message;
                ErrorInfoBar.IsOpen = true;
            }
        }
        finally
        {
            _working = false;
            if (!_closed)
            {
                IsPrimaryButtonEnabled = true;
                SchemaList.IsEnabled = true;
                UpdateSchemaCommands();
            }
        }
    }

    private static bool IsSchemaOperationError(Exception exception) => exception is XmlException or XmlSchemaException or IOException
        or UnauthorizedAccessException or System.Net.WebException or System.Net.Http.HttpRequestException
        or ArgumentException or InvalidOperationException or COMException;

    private void SchemaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_refreshingRows)
        {
            UpdateSchemaSelection();
        }
    }

    private void SchemaList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs e)
    {
        if (!e.InRecycleQueue && e.Item is SchemaListItem item)
        {
            AutomationProperties.SetName(e.ItemContainer, item.FileName);
        }
    }

    private void UpdateSchemaSelection()
    {
        var item = SingleSelectedSchema;
        SchemaLocationBox.Text = item is null ? string.Empty : item.Location.IsFile ? item.Location.LocalPath : item.Location.AbsoluteUri;
        SchemaLocationBox.PlaceholderText = SchemaList.SelectedItems.Count > 1
            ? "Select one schema to change its location" : "Schema path or URL";
        SetSchemaLocationButton.Content = item is null ? "Add location" : "Replace location";
        GenerationOptions.Visibility = Visibility.Collapsed;
        UpdateSchemaCommands();
    }

    private void RemoveSchema_Click(object sender, RoutedEventArgs e) => RemoveSelectedSchemas();

    private void RemoveSelectedSchemas()
    {
        if (!_working)
        {
            RemoveSchemas(CaptureSelection().ToHashSet(), "Remove schemas");
        }
    }

    private void SchemaEnabled_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_working && !_refreshingRows && sender is ToggleSwitch toggle &&
            toggle.DataContext is SchemaListItem item && _items.Contains(item) && item.IsEnabled != toggle.IsOn)
        {
            EditSchemas(toggle.IsOn ? "Enable schema" : "Disable schema",
                CaptureRows().Select(row => ReferenceEquals(row.Item, item) ? row with { Enabled = toggle.IsOn } : row),
                CaptureSelection());
        }
    }

    private void Apply_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var cache = App.SettingsService.SchemaCache;
        var previous = cache.GetSchemas().Select(entry => (entry.TargetNamespace, entry.Location, entry.Disabled)).ToArray();
        try
        {
            ReplaceEntries(_items.Select(item => (item.TargetNamespace, item.Location, !item.IsEnabled)));
            App.SettingsService.Save();
            Applied = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ReplaceEntries(previous);
            ErrorInfoBar.Message = exception.Message;
            ErrorInfoBar.IsOpen = true;
            args.Cancel = true;
        }

        void ReplaceEntries(IEnumerable<(string Namespace, Uri Location, bool Disabled)> entries)
        {
            cache.Clear();
            foreach (var (targetNamespace, location, disabled) in entries)
            {
                cache.Add(targetNamespace, location, disabled);
            }
        }
    }
}
