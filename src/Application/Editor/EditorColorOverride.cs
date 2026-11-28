using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace XmlNotepad;

public static class EditorColorOverride
{
    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.RegisterAttached(
        "Foreground", typeof(Brush), typeof(EditorColorOverride), new PropertyMetadata(null, Foreground_Changed));

    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.RegisterAttached(
        "Background", typeof(Brush), typeof(EditorColorOverride), new PropertyMetadata(null, Background_Changed));

    public static Brush? GetForeground(DependencyObject target) => (Brush?)target.GetValue(ForegroundProperty);
    public static void SetForeground(DependencyObject target, Brush? value) => target.SetValue(ForegroundProperty, value);

    public static Brush? GetBackground(DependencyObject target) => (Brush?)target.GetValue(BackgroundProperty);
    public static void SetBackground(DependencyObject target, Brush? value) => target.SetValue(BackgroundProperty, value);

    private static void Foreground_Changed(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        var property = target is TextBlock ? TextBlock.ForegroundProperty : TextElement.ForegroundProperty;
        Apply(target, property, args.NewValue);
    }

    private static void Background_Changed(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is TextBox textBox)
        {
            SetResource(textBox.Resources, "TextControlBackgroundPointerOver", args.NewValue);
            SetResource(textBox.Resources, "TextControlBackgroundFocused", args.NewValue);
        }
        var property = target is Panel ? Panel.BackgroundProperty : Control.BackgroundProperty;
        Apply(target, property, args.NewValue);
    }

    private static void SetResource(ResourceDictionary resources, string key, object? value)
    {
        if (value is null)
        {
            resources.Remove(key);
        }
        else
        {
            resources[key] = value;
        }
    }

    private static void Apply(DependencyObject target, DependencyProperty property, object? value)
    {
        if (value is null)
        {
            target.ClearValue(property);
        }
        else
        {
            target.SetValue(property, value);
        }
    }
}
