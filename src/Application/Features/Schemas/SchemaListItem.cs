using CommunityToolkit.Mvvm.ComponentModel;

namespace XmlNotepad.ViewModels;

public sealed partial class SchemaListItem : ObservableObject
{
    public SchemaListItem(Uri location, string targetNamespace, bool isEnabled)
    {
        Location = location;
        TargetNamespace = targetNamespace;
        IsEnabled = isEnabled;
    }

    public Uri Location { get; }

    public string FileName => Location.IsFile ? Path.GetFileName(Location.LocalPath) : Location.AbsoluteUri;

    public string TargetNamespace { get; }

    public string DisplayNamespace => string.IsNullOrWhiteSpace(TargetNamespace) ? "(no target namespace)" : TargetNamespace;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }
}
