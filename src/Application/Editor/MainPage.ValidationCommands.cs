using System.Xml;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class MainPage
{
    private XmlValidationIssue? _lastNavigatedIssue;

    private IEnumerable<XmlValidationIssue> CurrentValidationIssues =>
        DiagnosticsVisible ? DiagnosticsPane.CurrentIssues :
            ValidationIssues.ItemsSource as IReadOnlyList<XmlValidationIssue> ?? [];

    private void NextError_Click(object sender, RoutedEventArgs e)
    {
        var issues = CurrentValidationIssues.Where(issue => issue.Severity is not null).ToArray();
        if (issues.Length == 0)
        {
            return;
        }

        var current = Array.FindIndex(issues, issue => ReferenceEquals(issue, _lastNavigatedIssue));
        var next = issues[(current + 1) % issues.Length];
        if (DiagnosticsVisible)
        {
            DiagnosticsPane.IsDocumentationSelected = false;
            DiagnosticsPane.SelectIssue(next);
        }
        NavigateToValidationIssue(next);
    }

    private async void ExportErrors_Click(object sender, RoutedEventArgs e)
    {
        var issues = CurrentValidationIssues.Where(issue => issue.Severity is not null).ToArray();
        if (issues.Length == 0)
        {
            return;
        }

        var report = CreateErrorReport(issues, ViewModel.CurrentFilePath);
        var picker = new FileSavePicker(XamlRoot.ContentIslandEnvironment.AppWindowId)
        {
            SuggestedFileName = "Errors.xml",
            DefaultFileExtension = ".xml"
        };
        picker.FileTypeChoices.Add("XML error report", new List<string> { ".xml" });

        try
        {
            if (await picker.PickSaveFileAsync() is { } file)
            {
                await Task.Run(() => XmlDocumentWriter.Save(report, file.Path, App.Settings));
            }
        }
        catch (Exception exception) when (exception is XmlException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            ShowError($"Could not export validation errors: {exception.Message}");
        }
    }

    private static XmlDocument CreateErrorReport(IEnumerable<XmlValidationIssue> issues, string? sourcePath)
    {
        var document = new XmlDocument();
        document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));
        var root = document.CreateElement("ErrorList");
        document.AppendChild(root);

        foreach (var issue in issues)
        {
            var error = document.CreateElement("Error");
            root.AppendChild(error);
            AddField("Severity", issue.SeverityLabel);
            AddField("Description", issue.Message);
            AddField("Line", issue.Line);
            AddField("Column", issue.Column);
            AddField("FileName", sourcePath ?? issue.FileName);

            void AddField(string name, string value)
            {
                var field = document.CreateElement(name);
                field.InnerText = value;
                error.AppendChild(field);
            }
        }

        return document;
    }
}
