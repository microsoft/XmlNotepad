// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Xml;

namespace Microsoft.Xml;

public partial class XmlStats
{
    private static readonly HttpClient Http = new();
    private readonly Dictionary<string, NodeStats> _elements = new(StringComparer.Ordinal);
    private long _elemCount;
    private long _emptyCount;
    private long _attrCount;
    private long _commentCount;
    private long _piCount;
    private long _elemChars;
    private long _attrChars;
    private long _commentChars;
    private long _whiteChars;
    private long _whiteSChars;
    private long _piChars;
    private string _newLine = "\n";
    private readonly Stopwatch _watch = new();
    private readonly HashSet<string> _filters = new(StringComparer.Ordinal);

    internal WhitespaceHandling Whitespace { get; set; } = WhitespaceHandling.All;

    internal void AddFilters(string names) => _filters.UnionWith(names.Split(',', StringSplitOptions.TrimEntries));

    /// <summary>
    /// Process the given files adding to the current XmlStats.
    /// </summary>
    /// <param name="files">The list of files to process.</param>
    /// <param name="summary">Whether to print the report.</param>
    /// <param name="output">The output to write the report to.</param>
    /// <param name="newLineChar">What kind of newline character to use in the reporting.</param>
    public void ProcessFiles(string[] files, bool summary, TextWriter output, string newLineChar)
    {
        this._newLine = newLineChar;

        this._watch.Start();
        int count = 0;

        foreach (string file in files)
        {
            try
            {
                this.Process(file);
                count++;
                if (!summary)
                {
                    this.WriteReport(file, output);
                }
            }
            catch (Exception e)
            {
                output.Write("+++ error in file '" + file + "':");
                output.Write(this._newLine);
                output.Write(e.Message);
                output.Write(this._newLine);
            }
        }

        if (summary && count > 0)
        {
            this.WriteReport(files, output);
        }
    }

    /// <summary>
    /// Add stats from the given xml content to the current XmlStats.
    /// </summary>
    public void Process(TextReader input)
    {
        using var reader = new XmlTextReader(input);
        ProcessInput(reader);
    }

    /// <summary>
    /// Add stats from given xml file to the current XmlStats.
    /// </summary>
    public void Process(string path)
    {
        Uri.TryCreate(path, UriKind.Absolute, out var location);
        bool isWeb = location?.Scheme is "https" or "http";
        string localPath = location is { IsFile: true } ? location.LocalPath : path;
        if (!isWeb && !File.Exists(localPath))
        {
            Console.WriteLine("*** file not found: " + path);
            return;
        }

        try
        {
            if (isWeb)
            {
                using var stream = Http.GetStreamAsync(location!).GetAwaiter().GetResult();
                using var remoteReader = new XmlTextReader(path, stream);
                ProcessInput(remoteReader);
                return;
            }

            using var reader = new XmlTextReader(localPath);
            ProcessInput(reader);
        }
        catch (Exception ex)
        {
            Console.WriteLine("*** xml error in file: " + path);
            Console.WriteLine(ex.Message);
        }
    }

    private void ProcessInput(XmlTextReader reader)
    {
        reader.WhitespaceHandling = Whitespace;
        reader.DtdProcessing = DtdProcessing.Ignore;
        reader.XmlResolver = null;
        Process(reader);
    }

    /// <summary>
    /// Add stats from the given xml reader to the current XmlStats.
    /// </summary>
    public void Process(XmlReader r)
    {
        if (r == null)
        {
            return;
        }

        var elementStack = new Stack<NodeStats?>();
        NodeStats? currentElement = null;
        bool hasFilters = this._filters.Count > 0;
        int selected = hasFilters ? 0 : 1;

        while (r.Read())
        {
            switch (r.NodeType)
            {
                case XmlNodeType.CDATA:
                case XmlNodeType.Text:
                    {
                        if (selected > 0 && currentElement is not null)
                        {
                            long len = r.Value.Length;
                            currentElement.Chars += len;
                            this._elemChars += len;
                        }
                        break;
                    }
                case XmlNodeType.Element:
                    int dec = 0;
                    if (hasFilters && this._filters.Contains(r.LocalName))
                    {
                        selected++;
                        if (r.IsEmptyElement) dec = 1;
                    }
                    if (selected > 0)
                    {
                        this._elemCount++;
                    }

                    if (r.IsEmptyElement && selected > 0)
                    {
                        this._emptyCount++;
                    }
                    NodeStats? es = null;
                    if (selected > 0)
                    {
                        es = CountNode(this._elements, r.Name);
                        es.Selected = selected;
                    }
                    elementStack.Push(es);
                    currentElement = es;

                    while (r.MoveToNextAttribute())
                    {
                        if (es is not null)
                        {
                            if (es.Attrs == null)
                            {
                                es.Attrs = new Dictionary<string, NodeStats>();
                            }

                            var attrs = es.Attrs;

                            this._attrCount++;

                            // create a name that makes attributes unique to their parent elements
                            NodeStats ns = CountNode(attrs, r.Name);
                            string s = r.Value;
                            if (s != null)
                            {
                                long len = r.Value.Length;
                                ns.Chars += len;
                                this._attrChars += len;
                            }
                        }
                    }
                    selected -= dec;
                    break;
                case XmlNodeType.EndElement:
                    if (hasFilters && this._filters.Contains(r.LocalName))
                    {
                        selected--;
                    }
                    currentElement = elementStack.Pop();
                    break;
                case XmlNodeType.Entity:
                    break;
                case XmlNodeType.EndEntity:
                    break;
                case XmlNodeType.EntityReference:
                    // if you want entity references expanded then use the XmlValidatingReader.
                    // or perhaps we should report a list of them!
                    break;
                case XmlNodeType.ProcessingInstruction:
                    if (selected > 0)
                    {
                        this._piCount++;
                        this._piChars += r.Value.Length;
                    }
                    break;
                case XmlNodeType.Comment:
                    if (selected > 0)
                    {
                        this._commentCount++;
                        this._commentChars += r.Value.Length;
                    }
                    break;
                case XmlNodeType.SignificantWhitespace:
                    if (selected > 0)
                    {
                        this._whiteSChars += r.Value.Length;
                    }
                    break;
                case XmlNodeType.Whitespace:
                    if (selected > 0)
                    {
                        this._whiteChars += r.Value.Length;
                    }
                    break;
                case XmlNodeType.None:
                    break;
                case XmlNodeType.Notation:
                    break;
                case XmlNodeType.XmlDeclaration:
                    break;
                case XmlNodeType.Document:
                    break;
                case XmlNodeType.DocumentFragment:
                    break;
                case XmlNodeType.DocumentType:
                    break;
            }
        }
        r.Close();
    }

    /// <summary>
    /// Reset all the current stats to zero.
    /// </summary>
    public void Reset()
    {
        _elements.Clear();
        this._elemCount = 0;
        this._emptyCount = 0;
        this._attrCount = 0;
        this._commentCount = 0;
        this._piCount = 0;
        this._elemChars = 0;
        this._attrChars = 0;
        this._commentChars = 0;
        this._piChars = 0;
        this._whiteChars = 0;
        this._whiteSChars = 0;

        this._watch.Reset();
        this._watch.Start();
    }

    internal static NodeStats CountNode(Dictionary<string, NodeStats> ht, string name)
    {
        if (!ht.TryGetValue(name, out var es))
        {
            ht[name] = es = new NodeStats(name);
        }
        else
        {
            es.Count++;
        }
        return es;
    }
}
