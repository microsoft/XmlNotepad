// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml;

namespace Microsoft.Xml;

public partial class XmlStats
{
    /// <summary>
    /// Get the summary report as a string.
    /// </summary>
    public string GetReport()
    {
        using (StringWriter sw = new StringWriter())
        {
            this.WriteReport("Summary", sw);
            return sw.ToString();
        }
    }

    private string FormatMilliseconds()
    {
        float time = this._watch.ElapsedMilliseconds;
        if (time > 1000)
        {
            return String.Format("{0,1:F} secs", time / 1000f);
        }
        else
        {
            return String.Format("{0,1:F} msecs", time);
        }
    }

    private void WriteReport(string[] files, TextWriter output)
    {
        this._watch.Stop();

        foreach (var f in files) {
            output.Write("*** " + f);                     // filename or "Summary"
            output.Write(this._newLine);
        }
        output.Write(this._newLine);
        output.Write("Processed in {0}", FormatMilliseconds());
        output.Write(this._newLine);
        output.Write(this._newLine);

        ReportStats(output);
    }


    /// <summary>
    /// Get the summary report written to the given output.
    /// </summary>
    public void WriteReport(string path, TextWriter output)
    {
        this._watch.Stop();
        output.Write(String.Format("*** {0}   ({1})", path, FormatMilliseconds()));
        output.Write(this._newLine);
        output.Write(this._newLine);

        ReportStats(output);
        Reset();
    }

    private void ReportStats(TextWriter output)
    { 
        // strip out unselected nodes.
        foreach (var key in new List<string>(this._elements.Keys))
        {
            var es = this._elements[key];
            if (es.Selected <= 0)
            {
                this._elements.Remove(key);
            }
        }
        // count how many unique attributes
        long attrsCount = 0;
        foreach (NodeStats ns in this._elements.Values)
        {
            if (ns.Attrs != null)
            {
                attrsCount += ns.Attrs.Count;
            }
        }

        // overall stats
        output.Write("elements");
        output.Write(this._newLine);
        output.Write("{0,-20} {1,9:D}", "  unique", this._elements.Count);
        output.Write(this._newLine);
        output.Write("{0,-20} {1,9:D}", "  empty", this._emptyCount);
        output.Write(this._newLine);
        output.Write("{0,-20} {1,9:D}", "  total", this._elemCount);
        output.Write(this._newLine);
        output.Write("{0,-20} {1,9:D}", "  chars", this._elemChars);
        output.Write(this._newLine);

        output.Write("attributes");
        output.Write(this._newLine);
        output.Write("{0,-20} {1,9:D}", "  unique", attrsCount);
        output.Write(this._newLine);
        output.Write("{0,-20} {1,9:D}", "  total", this._attrCount);
        output.Write(this._newLine);
        output.Write("{0,-20} {1,9:D}", "  chars", this._attrChars);
        output.Write(this._newLine);

        output.Write("comments");
        output.Write(this._newLine);
        output.Write("{0,-20} {1,9:D}", "  total", this._commentCount);
        output.Write(this._newLine);
        output.Write("{0,-20} {1,9:D}", "  chars", this._commentChars);
        output.Write(this._newLine);

        output.Write("PIs");
        output.Write(this._newLine);
        output.Write("{0,-20} {1,9:D}", "  total", this._piCount);
        output.Write(this._newLine);
        output.Write("{0,-20} {1,9:D}", "  chars", this._piChars);
        output.Write(this._newLine);

        if (Whitespace != WhitespaceHandling.None)
        {
            output.Write("whitespace");
            output.Write(this._newLine);
            output.Write("{0,-20} {1,9:D}", "  chars", this._whiteChars);
            output.Write(this._newLine);
            output.Write("{0,-20} {1,9:D}", "  significant", this._whiteSChars);
            output.Write(this._newLine);
        }

        // elem/attr stats
        output.Write(this._newLine);
        output.Write(this._newLine);
        output.Write("elem/attr                count     chars");
        output.Write(this._newLine);
        output.Write("----------------------------------------");
        output.Write(this._newLine);

        foreach (NodeStats es in _elements.Values.OrderBy(node => node.Name, StringComparer.Ordinal))
        {
            output.Write("{0,-20} {1,9:D} {2,9:D}", es.Name, es.Count, es.Chars);
            output.Write(this._newLine);
            if (es.Attrs != null)
            {
                foreach (NodeStats ns in es.Attrs.Values.OrderBy(node => node.Name, StringComparer.CurrentCulture))
                {
                    output.Write("  @{0,-17} {1,9:D} {2,9:D}", ns.Name, ns.Count, ns.Chars);
                    output.Write(this._newLine);
                }
            }
        }
        output.Write(this._newLine);
    }
}

internal sealed class NodeStats(string name)
{
    public string Name { get; } = name;
    public long Count { get; set; } = 1;
    public long Chars { get; set; }
    public Dictionary<string, NodeStats>? Attrs { get; set; }
    public int Selected { get; set; }
}
