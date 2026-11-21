using System.Xml;
using System.Xml.Schema;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private DispatcherQueueTimer? _diagnosticsTimer;
    private bool _diagnosticsDeferredForEditor;
    private XmlValidationResult? _documentationResult;

    private bool DiagnosticsVisible => DiagnosticsArea.Visibility == Visibility.Visible;
    private bool NeedsSchemaValidation => DiagnosticsVisible || ViewModel.ShowIdentifyingText;
    private bool IsSchemaDocumentationVisible => DiagnosticsVisible && DiagnosticsPane.IsDocumentationSelected;

    private void InitializeSchemaDocumentation()
    {
        var timer = DispatcherQueue.CreateTimer();
        timer.IsRepeating = false;
        timer.Interval = TimeSpan.FromMilliseconds(300);
        timer.Tick += DiagnosticsTimer_Tick;
        _diagnosticsTimer = timer;
        DiagnosticsPane.IsDocumentationSelected = App.Settings.GetBoolean("DynamicHelpVisible");
        DiagnosticsPane.SelectedTabChanged += DiagnosticsTabChanged;
        DiagnosticsPane.IssueInvoked += NavigateToValidationIssue;
        Unloaded += (_, _) => timer.Stop();
        ConfigureDiagnosticsPane();
    }

    private void ConfigureDiagnosticsPane()
    {
        ViewModel.ReadNodeLabelOptions();
        DiagnosticsArea.Visibility = App.Settings.GetBoolean("ShowDiagnosticsPane", true)
            ? Visibility.Visible : Visibility.Collapsed;
        UpdateDiagnosticsHeight();
        RefreshSchemaDocumentationTab();
        ScheduleDiagnostics();
    }

    private void DiagnosticsTabChanged(object? sender, EventArgs e)
    {
        App.Settings["DynamicHelpVisible"] = DiagnosticsPane.IsDocumentationSelected;
        App.SettingsService.Save();
        RefreshSchemaDocumentationTab();
    }

    private void RefreshSchemaDocumentationTab()
    {
        if (IsSchemaDocumentationVisible)
        {
            UpdateSchemaDocumentation();
            if (_documentationResult is null)
            {
                ScheduleDiagnostics();
            }
        }
    }

    private void InvalidateSchemaDocumentation()
    {
        _documentationResult = null;
        ScheduleDiagnostics();
    }

    private void ScheduleDiagnostics()
    {
        _diagnosticsTimer?.Stop();
        if (NeedsSchemaValidation)
        {
            _diagnosticsTimer?.Start();
        }
    }

    private async void DiagnosticsTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        if (!NeedsSchemaValidation)
        {
            return;
        }
        if (ViewModel.IsBusy)
        {
            sender.Start();
            return;
        }
        if (IsValueEditorFocused())
        {
            _diagnosticsDeferredForEditor = true;
            return;
        }
        var focusedControl = FocusManager.GetFocusedElement(XamlRoot) as Control;
        try
        {
            var result = await ViewModel.ValidateAsync(_validationService,
                IsSchemaDocumentationVisible ? App.Settings.GetString("Language") : null);
            if (IsLoaded && DiagnosticsVisible)
            {
                if (result.Issues.Count > 0 || !DiagnosticsPane.ShowsValidationNotice)
                {
                    DiagnosticsPane.ShowIssues(result.Issues.Count > 0 ? result.Issues : null);
                }
                SetSchemaDocumentationResult(result);
            }
        }
        catch (Exception exception) when (exception is XmlException or XmlSchemaException or IOException or
            UnauthorizedAccessException or System.Net.WebException or System.Net.Http.HttpRequestException or InvalidOperationException or ArgumentException)
        {
            if (IsLoaded && DiagnosticsVisible)
            {
                XmlValidationIssue[] issues = [new(XmlSeverityType.Error, exception.Message)];
                DiagnosticsPane.ShowIssues(issues);
                SetSchemaDocumentationResult(new XmlValidationResult(true, issues));
            }
        }
        finally
        {
            if (IsLoaded && focusedControl is { IsLoaded: true, IsEnabled: true } && focusedControl.XamlRoot == XamlRoot &&
                !ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), focusedControl))
            {
                focusedControl.Focus(FocusState.Programmatic);
            }
        }
    }

    private void SetSchemaDocumentationResult(XmlValidationResult result)
    {
        if (IsLoaded && IsSchemaDocumentationVisible)
        {
            _documentationResult = result;
            UpdateSchemaDocumentation();
        }
    }

    private void UpdateSchemaDocumentation()
    {
        if (!IsSchemaDocumentationVisible)
        {
            return;
        }
        var node = ViewModel.SelectedNode is { IsPlaceholder: false } item ? item.Node : null;
        if (node is not null && node is not (XmlElement or XmlAttribute))
        {
            node = node.ParentNode;
        }
        var name = node?.Name ?? string.Empty;
        var pane = DiagnosticsPane.Documentation;
        if (_documentationResult is null)
        {
            pane.Show(name, "Loading schema documentation…");
        }
        else if (node is null)
        {
            pane.Show("", "Select an element or attribute to see its documentation.");
        }
        else if (_documentationResult.Documentation?.GetValueOrDefault(node) is { } documentation)
        {
            pane.Show(name, documentation.Text, documentation.Links);
        }
        else if (!_documentationResult.HasSchema)
        {
            pane.Show(name, "No schema is associated with this document.");
        }
        else
        {
            pane.Show(name, _documentationResult.Documentation is null && _documentationResult.Issues.Count > 0
                ? _documentationResult.Issues[0].Message : "This node has no schema documentation.");
        }
    }
}
