using System.Net;
using System.Text;
using System.Xml;
using Microsoft.XmlDiffPatch;

namespace XmlNotepad.Services;

internal sealed class XmlComparisonService(Settings settings, string? customStylesPath = null)
{
    internal static readonly (string Key, string Label, XmlDiffOptions Flag)[] Options =
    [
        ("XmlDiffIgnoreChildOrder", "Child element order", XmlDiffOptions.IgnoreChildOrder),
        ("XmlDiffIgnoreComments", "Comments", XmlDiffOptions.IgnoreComments),
        ("XmlDiffIgnorePI", "Processing instructions", XmlDiffOptions.IgnorePI),
        ("XmlDiffIgnoreWhitespace", "Whitespace", XmlDiffOptions.IgnoreWhitespace),
        ("XmlDiffIgnoreNamespaces", "Namespaces", XmlDiffOptions.IgnoreNamespaces),
        ("XmlDiffIgnorePrefixes", "Namespace prefixes", XmlDiffOptions.IgnorePrefixes),
        ("XmlDiffIgnoreXmlDecl", "XML declaration", XmlDiffOptions.IgnoreXmlDecl),
        ("XmlDiffIgnoreDtd", "Document type declaration", XmlDiffOptions.IgnoreDtd)
    ];

    public PreviewOutput? Compare(string baselinePath, string changedPath)
    {
        var options = XmlDiffOptions.None;
        foreach (var (key, _, flag) in Options)
        {
            if (settings.GetBoolean(key))
            {
                options |= flag;
            }
        }

        var readerSettings = XmlHelpers.CreateXmlSettings(settings.Resolver);
        var baselineUri = new Uri(baselinePath).AbsoluteUri;
        using var baseline = File.OpenRead(baselinePath);
        using var changed = File.OpenRead(changedPath);
        using var diffGram = new MemoryStream();
        using (var originalReader = XmlReader.Create(baseline, readerSettings, baselineUri))
        using (var changedReader = XmlReader.Create(changed, readerSettings, new Uri(changedPath).AbsoluteUri))
        using (var writer = XmlWriter.Create(diffGram))
        {
            if (new XmlDiff(options).Compare(originalReader, changedReader, writer))
            {
                return null;
            }
        }

        baseline.Position = 0;
        diffGram.Position = 0;
        using var viewInput = new XmlTextReader(baselineUri, baseline)
        {
            DtdProcessing = readerSettings.DtdProcessing,
            XmlResolver = new XmlResourceResolver(settings.Resolver)
        };
        using var changes = XmlReader.Create(diffGram, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
        var view = new XmlDiffView();
        view.Load(viewInput, changes);

        using var html = new StringWriter();
        html.Write("""
            <!doctype html><html><head><meta charset="utf-8">
            <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'">
            <title>XML comparison</title>
            <style>
            """);
        ComparisonStyles.WriteToReport(html, customStylesPath ?? ComparisonStyles.UserPath);
        html.Write("""
            </style></head><body>
            <div id="header">
            <p class="legend"><span class="add">Added</span><span class="remove">Removed</span>
            <span class="change">Changed</span><span class="moveto">Moved</span></p></div>
            <div id="main">
            """);
        html.Write("<table><colgroup><col class=\"gutter\"><col><col></colgroup><thead><tr><th>#</th><th><small>Original</small>");
        html.Write(WebUtility.HtmlEncode(Path.GetFileName(baselinePath)));
        html.Write("</th><th><small>Compared with</small>");
        html.Write(WebUtility.HtmlEncode(Path.GetFileName(changedPath)));
        html.Write("</th></tr></thead><tbody>");
        view.GetHtml(html, settings.GetBoolean("XmlDiffHideIdentical"));
        html.Write("</tbody></table></div></body></html>");
        var content = html.ToString();
        return new PreviewOutput(content, Encoding.UTF8.GetBytes(content), ".html", "XmlComparison.html");
    }
}
