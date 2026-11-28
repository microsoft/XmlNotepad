using System.Xml;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using DrawingColor = System.Drawing.Color;
using WindowsColor = Windows.UI.Color;

namespace XmlNotepad.ViewModels;

internal enum EditorColorRole
{
    Element, Attribute, Text, Comment, Instruction, CData, IdentifyingText, Document, Pane, Editor
}

public sealed partial class EditorPalette : ObservableObject
{
    internal sealed record Entry(string Label, Func<ThemeColors, DrawingColor> Read, Action<ThemeColors, DrawingColor> Write);

    internal static readonly IReadOnlyDictionary<EditorColorRole, Entry> Entries = new Dictionary<EditorColorRole, Entry>
    {
        [EditorColorRole.Element] = new("Element", colors => colors.Element, (colors, value) => colors.Element = value),
        [EditorColorRole.Attribute] = new("Attribute", colors => colors.Attribute, (colors, value) => colors.Attribute = value),
        [EditorColorRole.Text] = new("Text", colors => colors.Text, (colors, value) => colors.Text = value),
        [EditorColorRole.Comment] = new("Comment", colors => colors.Comment, (colors, value) => colors.Comment = value),
        [EditorColorRole.Instruction] = new("Processing instruction", colors => colors.PI, (colors, value) => colors.PI = value),
        [EditorColorRole.CData] = new("CDATA", colors => colors.CDATA, (colors, value) => colors.CDATA = value),
        [EditorColorRole.IdentifyingText] = new("Identifying text", colors => colors.SchemaAwareTextColor, (colors, value) => colors.SchemaAwareTextColor = value),
        [EditorColorRole.Document] = new("Document background", colors => colors.Background, (colors, value) => colors.Background = value),
        [EditorColorRole.Pane] = new("Container value background", colors => colors.ContainerBackground, (colors, value) => colors.ContainerBackground = value),
        [EditorColorRole.Editor] = new("Value field background", colors => colors.EditorBackground, (colors, value) => colors.EditorBackground = value)
    };

    [ObservableProperty]
    public partial EditorPaletteBrushes Brushes { get; private set; }

    public EditorPalette()
    {
        Brushes = new EditorPaletteBrushes(new Dictionary<EditorColorRole, Brush>());
    }

    internal static string SettingName(ColorTheme theme) => theme == ColorTheme.Dark ? "DarkColors" : "LightColors";

    internal static ThemeColors ReadColors(Settings settings, ColorTheme theme) =>
        settings[SettingName(theme)] as ThemeColors ?? ThemeColors.GetDefaultColors(theme);

    internal static WindowsColor ToWindowsColor(DrawingColor value) => WindowsColor.FromArgb(value.A, value.R, value.G, value.B);

    internal void Load(Settings settings, ColorTheme theme, bool highContrast)
    {
        var brushes = new Dictionary<EditorColorRole, Brush>();
        if (!highContrast)
        {
            var defaults = ThemeColors.GetDefaultColors(theme);
            var colors = ReadColors(settings, theme);
            foreach (var (role, entry) in Entries)
            {
                var color = entry.Read(colors);
                if (color.A > 0 && color.ToArgb() != entry.Read(defaults).ToArgb())
                {
                    brushes.Add(role, new SolidColorBrush(ToWindowsColor(color)));
                }
            }
        }
        Brushes = new EditorPaletteBrushes(brushes);
    }
}

public sealed class EditorPaletteBrushes
{
    private readonly IReadOnlyDictionary<EditorColorRole, Brush> _brushes;

    internal EditorPaletteBrushes(IReadOnlyDictionary<EditorColorRole, Brush> brushes) => _brushes = brushes;

    public Brush? IdentifyingText => _brushes.GetValueOrDefault(EditorColorRole.IdentifyingText);
    public Brush? DocumentBackground => _brushes.GetValueOrDefault(EditorColorRole.Document);
    public Brush? EditorBackground => _brushes.GetValueOrDefault(EditorColorRole.Editor);

    public Brush? GetContainerBackground(bool isContainer) =>
        isContainer ? _brushes.GetValueOrDefault(EditorColorRole.Pane) : null;

    public Brush? GetNode(XmlNodeType type) => _brushes.GetValueOrDefault(type switch
    {
        XmlNodeType.Element => EditorColorRole.Element,
        XmlNodeType.Attribute => EditorColorRole.Attribute,
        XmlNodeType.Comment => EditorColorRole.Comment,
        XmlNodeType.ProcessingInstruction or XmlNodeType.XmlDeclaration => EditorColorRole.Instruction,
        XmlNodeType.CDATA => EditorColorRole.CData,
        _ => EditorColorRole.Text
    });
}
