using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace XmlNotepad.ViewModels;

public sealed partial class EditorTypography : ObservableObject
{
    internal const string DefaultFamily = "Courier New";
    internal const double DefaultSize = 10;
    internal const double MinimumSize = 6;
    internal const double MaximumSize = 72;
    internal const int DefaultTreeIndent = 12;
    internal const int MaximumTreeIndent = 80;

    [ObservableProperty]
    public partial double Size { get; private set; }

    [ObservableProperty]
    public partial FontFamily Family { get; private set; }

    [ObservableProperty]
    public partial FontWeight Weight { get; private set; }

    [ObservableProperty]
    public partial FontStyle Style { get; private set; }

    [ObservableProperty]
    public partial double RowHeight { get; private set; }

    [ObservableProperty]
    public partial int TreeIndent { get; private set; }

    internal EditorTypography()
    {
        Family = new FontFamily(DefaultFamily);
    }

    internal static double GetPointSize(Settings settings)
    {
        var size = settings.GetDouble("FontSize", DefaultSize);
        return double.IsFinite(size) ? Math.Clamp(size, MinimumSize, MaximumSize) : DefaultSize;
    }

    internal void Load(Settings settings)
    {
        var family = settings.GetString("FontFamily").Trim();
        Family = new FontFamily(family.Length == 0 ? DefaultFamily : family);
        Size = GetPointSize(settings) * 96 / 72;
        Weight = settings.GetString("FontWeight") == "Bold" ? FontWeights.Bold : FontWeights.Normal;
        Style = settings.GetString("FontStyle") == "Italic" ? FontStyle.Italic : FontStyle.Normal;
        RowHeight = Math.Max(32, Math.Ceiling(Size * 1.35) + 10);
        TreeIndent = GetTreeIndent(settings);
    }

    internal static int GetTreeIndent(Settings settings) =>
        Math.Clamp(settings.GetInteger("TreeIndent", DefaultTreeIndent), 0, MaximumTreeIndent);
}
