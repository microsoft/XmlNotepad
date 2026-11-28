using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class SchemaDocumentationPane : UserControl
{
    public SchemaDocumentationPane() => InitializeComponent();

    internal bool IsTextFocused => DocumentationText.FocusState != FocusState.Unfocused;

    internal void Show(string name, string message, IReadOnlyList<SchemaDocumentationLink>? links = null)
    {
        if (NodeName.Text != name || DocumentationText.Text != message)
        {
            DocumentationScroll.ChangeView(null, 0, null, disableAnimation: true);
        }
        NodeName.Text = name;
        NodeName.Visibility = name.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        DocumentationText.Text = message;
        DocumentationText.Visibility = message.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        while (DocumentationContent.Children.Count > 2)
        {
            DocumentationContent.Children.RemoveAt(DocumentationContent.Children.Count - 1);
        }
        foreach (var link in links ?? [])
        {
            DocumentationContent.Children.Add(new HyperlinkButton
            {
                Content = link.Label,
                NavigateUri = link.Location,
                Padding = new Thickness(0)
            });
        }
    }

}
