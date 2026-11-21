using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace XmlNotepad;

public sealed class PaneSplitter : ContentControl
{
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(PaneSplitter),
        new PropertyMetadata(Orientation.Vertical, OnOrientationChanged));

    public PaneSplitter()
    {
        UpdateCursor();
    }

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    private static void OnOrientationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((PaneSplitter)sender).UpdateCursor();

    private void UpdateCursor() => ProtectedCursor = InputSystemCursor.Create(Orientation == Orientation.Horizontal
        ? InputSystemCursorShape.SizeNorthSouth
        : InputSystemCursorShape.SizeWestEast);
}
