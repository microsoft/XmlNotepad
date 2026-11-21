using System.Reflection;
using Microsoft.UI.Xaml.Controls;

namespace XmlNotepad;

public sealed partial class AboutDialog : ContentDialog
{
    private static readonly Assembly AppAssembly = typeof(App).Assembly;

    public AboutDialog() => InitializeComponent();

    public string VersionLabel => $"Version {AppAssembly.GetName().Version}";

    public string CopyrightLabel => AppAssembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";

    public Uri DocumentationUri => App.DocumentationUri;
}
