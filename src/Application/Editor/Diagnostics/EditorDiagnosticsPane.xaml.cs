using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class EditorDiagnosticsPane : UserControl
{
    internal event EventHandler? SelectedTabChanged;
    internal event Action<XmlValidationIssue>? IssueInvoked;

    public EditorDiagnosticsPane() => InitializeComponent();

    internal SchemaDocumentationPane Documentation => DocumentationPane;
    internal bool HasIssues => Issues.ItemsSource is not null;
    internal IReadOnlyList<XmlValidationIssue> CurrentIssues =>
        Issues.ItemsSource as IReadOnlyList<XmlValidationIssue> ?? [];
    internal bool ShowsValidationNotice =>
        Issues.ItemsSource is IReadOnlyList<XmlValidationIssue> { Count: 1 } issues && issues[0].Severity is null;

    internal bool IsDocumentationSelected
    {
        get => Tabs.SelectedIndex == 1;
        set => Tabs.SelectedIndex = value ? 1 : 0;
    }

    internal void ShowIssues(IReadOnlyList<XmlValidationIssue>? issues) => Issues.ItemsSource = issues;

    internal void SelectIssue(XmlValidationIssue issue)
    {
        Issues.SelectedItem = issue;
        Issues.ScrollIntoView(issue);
    }

    internal bool FocusSelectedTab() => Tabs.SelectedItem is TabViewItem tab && tab.Focus(FocusState.Programmatic);

    public static Visibility SeverityVisibility(string actual, string expected) =>
        actual == expected ? Visibility.Visible : Visibility.Collapsed;

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        SelectedTabChanged?.Invoke(this, EventArgs.Empty);

    private void Issues_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is XmlValidationIssue issue)
        {
            IssueInvoked?.Invoke(issue);
        }
    }
}
