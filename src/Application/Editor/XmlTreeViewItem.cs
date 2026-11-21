using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace XmlNotepad;

public sealed class XmlTreeViewItem : TreeViewItem
{
    public static readonly DependencyProperty IndentationProperty = DependencyProperty.Register(
        nameof(Indentation), typeof(Thickness), typeof(XmlTreeViewItem), new PropertyMetadata(new Thickness(0)));

    public Thickness Indentation
    {
        get => (Thickness)GetValue(IndentationProperty);
        set => SetValue(IndentationProperty, value);
    }
}
