using System.Xml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using XmlNotepad.Services;

namespace XmlNotepad;

public sealed partial class StylesheetLocationDialog : ContentDialog
{
    private readonly Uri _documentBase;

    public Uri? Location { get; private set; }

    public StylesheetLocationDialog(Uri documentBase, IReadOnlyList<Uri> recentStylesheets)
    {
        _documentBase = documentBase;
        InitializeComponent();
        foreach (var stylesheet in recentStylesheets.Where(uri => uri.IsAbsoluteUri))
        {
            var displayLocation = GetDisplayLocation(stylesheet);
            var fileName = Path.GetFileName(stylesheet.IsFile ? stylesheet.LocalPath : stylesheet.AbsolutePath);
            var item = new ComboBoxItem
            {
                Content = string.IsNullOrEmpty(fileName) ? displayLocation : fileName,
                Tag = stylesheet
            };
            ToolTipService.SetToolTip(item, displayLocation);
            AutomationProperties.SetName(item, displayLocation);
            RecentStylesheetsBox.Items.Add(item);
        }
        RecentStylesheetsBox.Visibility = RecentStylesheetsBox.Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        IsPrimaryButtonEnabled = false;
    }

    private static string GetDisplayLocation(Uri location) => location.IsFile ? location.LocalPath : location.AbsoluteUri;

    private void Location_Changed(object sender, TextChangedEventArgs e)
    {
        IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(LocationBox.Text);
        if (RecentStylesheetsBox?.SelectedItem is ComboBoxItem { Tag: Uri location } &&
            !string.Equals(LocationBox.Text, GetDisplayLocation(location), StringComparison.Ordinal))
        {
            RecentStylesheetsBox.SelectedIndex = -1;
        }
        if (LocationError is not null)
        {
            LocationError.IsOpen = false;
        }
    }

    private void RecentStylesheets_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecentStylesheetsBox.SelectedItem is ComboBoxItem { Tag: Uri location })
        {
            LocationBox.Text = GetDisplayLocation(location);
        }
    }

    private void Transform_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var text = LocationBox.Text.Trim().Trim('"');
        if (text.Length > 0 && Uri.TryCreate(_documentBase, text, out var location) &&
            (location.IsFile || RemoteDocument.IsWebUri(location)))
        {
            try
            {
                XmlResourceResolver.ValidateUri(location);
                Location = location;
                return;
            }
            catch (XmlException exception)
            {
                LocationError.Message = exception.Message;
            }
        }
        else
        {
            LocationError.Message = "Enter a stylesheet path or an HTTP or HTTPS URL.";
        }
        args.Cancel = true;
        LocationError.IsOpen = true;
        LocationBox.Focus(FocusState.Programmatic);
    }

    private async void Browse_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker(XamlRoot.ContentIslandEnvironment.AppWindowId);
        picker.FileTypeFilter.Add(".xsl");
        picker.FileTypeFilter.Add(".xslt");
        if (await picker.PickSingleFileAsync() is { } file)
        {
            LocationBox.Text = file.Path;
        }
    }
}
