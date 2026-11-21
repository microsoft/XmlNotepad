using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;
using System.Xml;
using System.Xml.Schema;
using XmlNotepad;
using XmlNotepad.Services;
using XmlNotepad.ViewModels;

namespace XmlNotepad;

public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; } = new();
    private EditorTypography Typography => App.Typography;
    private readonly XmlValidationService _validationService = new(App.SettingsService.SchemaCache);
    private readonly XmlIncludeService _includeService = new();
    private readonly XmlXsltService _xsltService = new(App.SettingsService);
    private string? _startupFile;
    private (int Line, int Column) _startupPosition;
    private MainWindow? _window;

    public MainPage()
    {
        InitializeComponent();
        InitializeTree();
        RefreshEditorPalette();
        ActualThemeChanged += (_, _) => RefreshEditorPalette();
        FindReplaceBar.FindRequested += FindReplaceBar_FindRequested;
        FindReplaceBar.ReplaceRequested += FindReplaceBar_ReplaceRequested;
        FindReplaceBar.ReplaceAllRequested += FindReplaceBar_ReplaceAllRequested;
        FindReplaceBar.CloseRequested += FindReplaceBar_CloseRequested;
        FindReplaceBar.QueryChanged += (_, _) => ResetSearchState();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.ValueEditFailed += ShowError;
        UpdateWindowTitle();
        Loaded += MainPage_Loaded;
    }

    internal void RefreshEditorPalette() => ViewModel.Palette.Load(
        App.Settings, ActualTheme == ElementTheme.Dark ? ColorTheme.Dark : ColorTheme.Light, _window?.HighContrast == true);

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is ValueTuple<MainWindow, string?, int, int, XmlDocument?> request)
        {
            (_window, _startupFile, _startupPosition.Line, _startupPosition.Column, var initialDocument) = request;
            RefreshEditorPalette();
            if (initialDocument is not null)
            {
                ViewModel.LoadGeneratedDocument(initialDocument);
            }
        }
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainPage_Loaded;
        StartFileMonitoring();
        StartUpdateChecks();
        InitializeSchemaDocumentation();
        InitializeStatusBar();
        UpdateWindowTitle();
        SelectCurrentNode();
        RefreshRecentFilesMenu();
        if (_startupFile is { } path)
        {
            _startupFile = null;
            if (await TryOpenAsync(path) && _startupPosition.Line > 0 &&
                ViewModel.GoToLine(_startupPosition.Line, Math.Max(1, _startupPosition.Column)) &&
                ViewModel.SelectedNode is { } item)
            {
                RevealTreeItem(item);
            }
        }
    }

    private async void New_Click(object sender, RoutedEventArgs e)
    {
        if (await ConfirmSaveChangesAsync())
        {
            ViewModel.CreateNew();
            CloseError();
        }
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmSaveChangesAsync())
        {
            return;
        }

        var picker = new FileOpenPicker(XamlRoot.ContentIslandEnvironment.AppWindowId);
        picker.FileTypeFilter.Add(".xml");
        picker.FileTypeFilter.Add(".xsl");
        picker.FileTypeFilter.Add(".xslt");
        picker.FileTypeFilter.Add(".xsd");
        picker.FileTypeFilter.Add(".csv");
        picker.FileTypeFilter.Add(".json");
        picker.FileTypeFilter.Add(".htm");
        picker.FileTypeFilter.Add(".html");

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            await TryOpenAsync(file.Path);
        }
    }

    private async void RecentFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: string path } && await ConfirmSaveChangesAsync())
        {
            await TryOpenAsync(path);
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e) => await Save_ClickAsync();

    private async void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CommitSelectedValue();
        await SaveAsAsync();
    }

    private async void InsertElement_Click(object sender, RoutedEventArgs e)
    {
        await RequestInsertAsync(XmlNodeType.Element, InsertPosition.Child);
    }

    private async void InsertAttribute_Click(object sender, RoutedEventArgs e)
    {
        await RequestInsertAsync(XmlNodeType.Attribute, InsertPosition.Child);
    }

    private async void InsertText_Click(object sender, RoutedEventArgs e)
    {
        await RequestInsertAsync(XmlNodeType.Text, InsertPosition.Child);
    }

    private async void InsertComment_Click(object sender, RoutedEventArgs e)
    {
        await RequestInsertAsync(XmlNodeType.Comment, InsertPosition.Child);
    }

    private async void InsertNodeAtPosition_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: string tag })
        {
            return;
        }

        var parts = tag.Split('|');
        if (parts.Length != 2 || !Enum.TryParse(parts[0], out XmlNodeType type) ||
            !Enum.TryParse(parts[1], out InsertPosition position))
        {
            return;
        }

        await RequestInsertAsync(type, position);
    }

    private async void RenameNode_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedNode is not { IsPlaceholder: false } selected || !ViewModel.CanRenameSelectedNode)
        {
            return;
        }

        var nameBox = new TextBox { Text = selected.Node.Name, PlaceholderText = "XML name" };
        var dialog = new ContentDialog
        {
            Title = "Rename node",
            Content = nameBox,
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };
        if (await ShowDialogAsync(dialog) != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            ViewModel.RenameSelectedNode(nameBox.Text);
            CloseError();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or XmlException)
        {
            ShowError($"The node could not be renamed: {exception.Message}");
        }
    }

    private void XmlTreeItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is TreeViewItem row && XmlTreeNodeContent.GetItem(XmlTree.NodeFromContainer(row)) is { IsPlaceholder: false } item)
        {
            ViewModel.SelectedNode = item;
            XmlTree.SelectedItem = item.TreeNode;
        }
    }

    private void DuplicateNode_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.DuplicateSelectedNode();
            CloseError();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or XmlException)
        {
            ShowError($"The node could not be duplicated: {exception.Message}");
        }
    }

    private void DeleteNode_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.DeleteSelectedNode();
        }
        catch (InvalidOperationException exception)
        {
            ShowError(exception.Message);
        }
    }

    private void Undo_Click(object sender, RoutedEventArgs e) => ViewModel.Undo();

    private void Redo_Click(object sender, RoutedEventArgs e) => ViewModel.Redo();

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedNode is not { IsPlaceholder: false } selected || !ViewModel.CanCopySelectedNode)
        {
            return;
        }

        try
        {
            ViewModel.CopySelectedNode();
            XmlClipboardService.Copy(selected.Node);
            CloseError();
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.Runtime.InteropServices.ExternalException)
        {
            ShowError($"The node could not be copied: {exception.Message}");
        }
    }

    private void CopyXPath_Click(object sender, RoutedEventArgs e)
    {
        var node = ViewModel.SelectedNode?.Node;
        if (node is null)
        {
            return;
        }

        var xpath = XmlHelpers.GetXPathLocation(node, XmlHelpers.GetNamespaceScope(node));
        if (!string.IsNullOrEmpty(xpath))
        {
            var package = new DataPackage();
            package.SetText(xpath);
            Clipboard.SetContent(package);
        }
    }

    private void Cut_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedNode is not { IsPlaceholder: false } selected || !ViewModel.CanCutSelectedNode)
        {
            return;
        }

        try
        {
            ViewModel.CopySelectedNode();
            XmlClipboardService.Copy(selected.Node);
            ViewModel.DeleteSelectedNode();
            CloseError();
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.Runtime.InteropServices.ExternalException)
        {
            ShowError($"The node could not be cut: {exception.Message}");
        }
    }

    private async void Paste_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var text = await XmlClipboardService.GetTextAsync();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }
            ViewModel.PasteXml(text);
            CloseError();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or XmlException or System.Runtime.InteropServices.ExternalException)
        {
            ShowError($"The clipboard XML could not be pasted: {exception.Message}");
        }
    }

    private void NudgeUp_Click(object sender, RoutedEventArgs e) => RunNudge(NudgeDirection.Up);

    private void NudgeDown_Click(object sender, RoutedEventArgs e) => RunNudge(NudgeDirection.Down);

    private void NudgeLeft_Click(object sender, RoutedEventArgs e) => RunNudge(NudgeDirection.Left);

    private void NudgeRight_Click(object sender, RoutedEventArgs e) => RunNudge(NudgeDirection.Right);

    private void RunNudge(NudgeDirection direction)
    {
        try
        {
            ViewModel.NudgeSelectedNode(direction);
            CloseError();
        }
        catch (Exception exception) when (exception is InvalidOperationException or XmlException)
        {
            ShowError($"The node could not be moved: {exception.Message}");
        }
    }

    private async Task RequestInsertAsync(XmlNodeType type, InsertPosition position)
    {
        var displayName = type switch
        {
            XmlNodeType.ProcessingInstruction => "processing instruction",
            XmlNodeType.CDATA => "CDATA section",
            _ => type.ToString().ToLowerInvariant()
        };
        var nameLabel = type switch
        {
            XmlNodeType.Element => "Element name",
            XmlNodeType.Attribute => "Attribute name",
            _ => "Target"
        };
        var needsName = type is XmlNodeType.Element or XmlNodeType.Attribute or XmlNodeType.ProcessingInstruction;
        var expectedNames = needsName ? ViewModel.GetExpectedNames(type, position) : [];
        var nameBox = new AutoSuggestBox
        {
            Text = type switch
            {
                XmlNodeType.Element => "element",
                XmlNodeType.Attribute => "attribute",
                XmlNodeType.ProcessingInstruction => "target",
                _ => string.Empty
            },
            PlaceholderText = nameLabel,
            TextMemberPath = nameof(XmlSuggestion.Name),
            ItemTemplate = (DataTemplate)Resources["XmlSuggestionTemplate"],
            ItemsSource = expectedNames
        };
        XmlSuggestion? selectedSuggestion = null;
        nameBox.SuggestionChosen += (_, args) => selectedSuggestion = args.SelectedItem as XmlSuggestion;
        nameBox.TextChanged += (_, args) =>
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                selectedSuggestion = null;
                var query = nameBox.Text.Trim();
                nameBox.ItemsSource = expectedNames
                    .Where(suggestion => suggestion.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }
        };
        var valueBox = new TextBox
        {
            PlaceholderText = type switch
            {
                XmlNodeType.Text => "Text value",
                XmlNodeType.Comment => "Comment text",
                XmlNodeType.CDATA => "CDATA content",
                XmlNodeType.ProcessingInstruction => "Data",
                _ => "Value"
            },
            AcceptsReturn = type is XmlNodeType.Text or XmlNodeType.Comment or XmlNodeType.CDATA
        };
        AutomationProperties.SetAutomationId(nameBox, "InsertName");
        AutomationProperties.SetAutomationId(valueBox, "InsertValue");
        var content = new StackPanel { Spacing = 8 };
        if (needsName)
        {
            content.Children.Add(new TextBlock { Text = nameLabel });
            content.Children.Add(nameBox);
        }
        if (!needsName || type is XmlNodeType.Attribute or XmlNodeType.ProcessingInstruction)
        {
            content.Children.Add(new TextBlock { Text = type == XmlNodeType.ProcessingInstruction ? "Data" : "Value" });
            content.Children.Add(valueBox);
        }

        var dialog = new ContentDialog
        {
            Title = $"Insert {displayName} {position.ToString().ToLowerInvariant()}",
            Content = content,
            PrimaryButtonText = "Insert",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        if (await ShowDialogAsync(dialog) != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            var name = nameBox.Text.Trim();
            ViewModel.InsertNode(type, position, name, valueBox.Text,
                selectedSuggestion?.Name == name ? selectedSuggestion.Namespace : null);
            CloseError();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or XmlException)
        {
            ShowError($"The node could not be inserted: {exception.Message}");
        }
    }

    private void ResumeDeferredDiagnostics()
    {
        if (_diagnosticsDeferredForEditor)
        {
            _diagnosticsDeferredForEditor = false;
            ScheduleDiagnostics();
        }
    }

    private void New_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        New_Click(sender, new RoutedEventArgs());
    }

    private void Open_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Open_Click(sender, new RoutedEventArgs());
    }

    private void Save_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Save_Click(sender, new RoutedEventArgs());
    }

    private void Rename_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        RenameNode_Click(sender, new RoutedEventArgs());
    }

    private void Cut_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (IsValueEditorFocused() || FindReplaceBar.IsEditingText) return;
        args.Handled = true;
        Cut_Click(sender, new RoutedEventArgs());
    }

    private void Copy_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (IsValueEditorFocused() || FindReplaceBar.IsEditingText || DiagnosticsPane.Documentation.IsTextFocused) return;
        args.Handled = true;
        Copy_Click(sender, new RoutedEventArgs());
    }

    private void Paste_AcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (IsValueEditorFocused() || FindReplaceBar.IsEditingText) return;
        args.Handled = true;
        Paste_Click(sender, new RoutedEventArgs());
    }

    private async void ValidateXml_Click(object sender, RoutedEventArgs e)
    {
        _diagnosticsTimer?.Stop();
        _diagnosticsDeferredForEditor = false;
        ClearValidationIssues();
        try
        {
            var result = await ViewModel.ValidateAsync(_validationService,
                IsSchemaDocumentationVisible ? App.Settings.GetString("Language") : null);
            SetSchemaDocumentationResult(result);
            if (DiagnosticsVisible)
            {
                DiagnosticsPane.ShowIssues(result.Issues.Count > 0 ? result.Issues : (XmlValidationIssue[])
                [
                    new XmlValidationIssue(null, result.HasSchema
                        ? "The XML document is valid."
                        : "No schema found. Add an XML schema reference before validating this document.")
                ]);
                DiagnosticsPane.IsDocumentationSelected = false;
                ErrorInfoBar.IsOpen = false;
                return;
            }
            if (!result.HasSchema && result.Issues.Count == 0)
            {
                ErrorInfoBar.Severity = InfoBarSeverity.Warning;
                ErrorInfoBar.Title = "No schema found";
                ErrorInfoBar.Message = "Add an XML schema reference before validating this document.";
            }
            else if (result.ErrorCount == 0 && result.WarningCount == 0)
            {
                ErrorInfoBar.Severity = InfoBarSeverity.Success;
                ErrorInfoBar.Title = "Validation successful";
                ErrorInfoBar.Message = "The XML document is valid.";
            }
            else
            {
                ErrorInfoBar.Severity = result.ErrorCount > 0 ? InfoBarSeverity.Error : InfoBarSeverity.Warning;
                ErrorInfoBar.Title = $"Validation found {result.ErrorCount} {(result.ErrorCount == 1 ? "error" : "errors")} and " +
                    $"{result.WarningCount} {(result.WarningCount == 1 ? "warning" : "warnings")}";
                ErrorInfoBar.Message = result.Issues.Any(issue => issue.Node is not null)
                    ? "Select a result to go to its location in the XML tree." : string.Empty;
                ValidationIssues.ItemsSource = result.Issues;
                ValidationIssues.Visibility = Visibility.Visible;
            }

            ErrorInfoBar.IsOpen = true;
        }
        catch (Exception exception) when (exception is XmlException or XmlSchemaException or IOException or UnauthorizedAccessException or System.Net.WebException or System.Net.Http.HttpRequestException or InvalidOperationException or ArgumentException)
        {
            ShowError($"XML validation failed: {exception.Message}");
        }
    }

    private async void ExpandXIncludes_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.ExpandIncludesAsync(_includeService);
            CloseError();
        }
        catch (Exception exception) when (exception is ApplicationException or XmlException or IOException or UnauthorizedAccessException or System.Net.WebException or System.Net.Http.HttpRequestException or InvalidOperationException or ArgumentException)
        {
            ShowError($"XInclude expansion failed: {exception.Message}");
        }
    }

    private async void ManageSchemas_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SchemaManagerDialog(XamlRoot);
        await ShowDialogAsync(dialog);
        if (dialog.Applied)
        {
            InvalidateSchemaDocumentation();
            ValidateXml_Click(sender, e);
        }
    }

    private bool IsValueEditorFocused() => ValueEditor.FocusState != FocusState.Unfocused ||
        _inlineValueEditor is { FocusState: not FocusState.Unfocused };

    private void XmlTree_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        if (XmlTreeNodeContent.GetItem(args.Node) is { } item)
        {
            ViewModel.Expand(item);
        }
    }

    private void XmlTree_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (XmlTreeNodeContent.GetItem(args.AddedItems.FirstOrDefault()) is { IsPlaceholder: false } item)
        {
            if (!ReferenceEquals(ViewModel.SelectedNode, item))
            {
                EndInlineValueEdit(commit: true);
                ViewModel.CommitSelectedValue();
            }
            ViewModel.SelectedNode = item;
            SelectedNodeTitle.Text = item.DisplayName;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainPageViewModel.Title))
        {
            ScheduleStatusUpdate();
            ResetSearchState();
            InvalidateSchemaDocumentation();
            UpdateWindowTitle();
            if (_lastFileNotice is { } notice &&
                (notice.Path != ViewModel.CurrentFilePath || notice.Baseline != ViewModel.CurrentFileVersion))
            {
                ClearFileNotice();
            }
            if (DiagnosticsPane.HasIssues || ValidationIssues.ItemsSource is not null)
            {
                CloseError();
            }
        }

        if (e.PropertyName is nameof(MainPageViewModel.IsValueReadOnly))
        {
            ValueEditor.IsReadOnly = ViewModel.IsValueReadOnly;
        }

        if (e.PropertyName is nameof(MainPageViewModel.SelectedNode))
        {
            ScheduleStatusUpdate();
            CloseValueDetails();
            SelectCurrentNode();
            FindReplaceBar.SetNamespaceContext(ViewModel.SelectedNode?.Node);
            UpdateSchemaDocumentation();
        }

        if (e.PropertyName is nameof(MainPageViewModel.SelectedValue) && !_updatingValueDetails &&
            _valueDetailsItem is not null && ReferenceEquals(_valueDetailsItem, ViewModel.SelectedNode))
        {
            LoadValueDetails();
        }
    }

    private void ShowError(string message)
    {
        ClearValidationIssues();
        ErrorInfoBar.Severity = InfoBarSeverity.Error;
        ErrorInfoBar.Title = "Error";
        ErrorInfoBar.Message = message;
        ErrorInfoBar.IsOpen = true;
    }

    private void CloseError()
    {
        ClearValidationIssues();
        ErrorInfoBar.IsOpen = false;
    }

    private void ClearValidationIssues()
    {
        OpenFailedFileButton.Tag = null;
        OpenFailedFileButton.Visibility = Visibility.Collapsed;
        _lastNavigatedIssue = null;
        DiagnosticsPane.ShowIssues(null);
        ValidationIssues.ItemsSource = null;
        ValidationIssues.Visibility = Visibility.Collapsed;
    }

    private void UpdateWindowTitle()
    {
        _window?.SetTitle(ViewModel.Title);
    }

    private void RefreshRecentFilesMenu()
    {
        RecentFilesMenu.Items.Clear();
        foreach (var uri in App.SettingsService.RecentFiles)
        {
            var path = uri.IsFile ? uri.LocalPath : uri.ToString();
            var item = new MenuFlyoutItem
            {
                Text = uri.IsFile ? Path.GetFileName(path) : uri.AbsoluteUri,
                Tag = path
            };
            item.Click += RecentFile_Click;
            RecentFilesMenu.Items.Add(item);
        }

        if (RecentFilesMenu.Items.Count == 0)
        {
            RecentFilesMenu.Items.Add(new MenuFlyoutItem { Text = "No recent files", IsEnabled = false });
        }
    }

    private void SelectCurrentNode()
    {
        if (ViewModel.SelectedNode is { } item)
        {
            XmlTree.SelectedItem = item.TreeNode;
            SelectedNodeTitle.Text = item.DisplayName;
        }
    }

}
