// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Microsoft.Xml
{
    /// <summary>
    /// XmlStats class provides a command line tool that reports various statistics about the structure of 
    /// a given XML file, like total number of element and attributes and so on.
    /// </summary>
    public class XmlStats
    {
        private Dictionary<string, NodeStats> _elements = new Dictionary<string, NodeStats>();
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
        private WhitespaceHandling _whiteSpace = WhitespaceHandling.All;
        private Stopwatch _watch = new Stopwatch();

        private static void PrintUsage()
        {
            Console.WriteLine("usage: XmlStats [options] <filenames>");
            Console.WriteLine("    reports statistics about elements, attributes and text found");
            Console.WriteLine("    in the specified XML files (URLs or local file names).");
            Console.WriteLine("    filenames: wildcards in local filenames allowed");
            Console.WriteLine("               (for reporting on all files in a directory)");
            Console.WriteLine("options:");
            Console.WriteLine("  -f filename Reports stats on files names found in the given file (one per line).");
            Console.WriteLine("  -v          Generates individual reports for all specified files (default is summary only).");
            Console.WriteLine("  -nologo     Removes logo from the report");
            Console.WriteLine("  -w[a|s|n]   XML whitespace handling: -wa=All (default), -ws=Significant, -wn=None");
        }

        [STAThread]
        private static int Main(string[] args)
        {
            bool summary = true;
            bool logo = true;
            XmlStats xs = new XmlStats();
            List<string> files = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.Length > 1 && arg[0] == '-')
                {
                    string larg = arg.Trim('-').ToLower(CultureInfo.CurrentCulture);
                    switch (larg)
                    {
                        case "?":
                        case "h":
                        case "help":
                            PrintUsage();
                            return 1;
                        case "f":
                            if (i + 1 == args.Length)
                            {
                                Console.WriteLine("missing file name after '-f' argument");
                                PrintUsage();
                                return 1;
                            }
                            else
                            {
                                files = ReadFileNames(args[++i]);
                            }
                            break;
                        case "v":
                            summary = false;
                            break;
                        case "wn":
                            xs._whiteSpace = WhitespaceHandling.None;
                            break;
                        case "ws":
                            xs._whiteSpace = WhitespaceHandling.Significant;
                            break;
                        case "wa":
                            xs._whiteSpace = WhitespaceHandling.All;
                            break;
                        case "nologo":
                            logo = false;
                            break;
                        default: // invalid opt
                            Console.WriteLine("invalid option '" + arg + "'");
                            PrintUsage();
                            return 1;
                    }
                }
                else if (arg.IndexOf("://", StringComparison.InvariantCulture) > 0)
                {
                    // url
                    files.Add(arg);
                }
                else if (arg.IndexOf("*", StringComparison.InvariantCulture) >= 0 || arg.IndexOf("?", StringComparison.InvariantCulture) >= 0)
                {
                    // resolve wildcards
                    string path = Path.Combine(Directory.GetCurrentDirectory(), arg);
                    string dir = Path.GetDirectoryName(path);
                    string name = Path.GetFileName(path);
                    string[] names = Directory.GetFiles(dir, name);

                    foreach (string file in names)
                    {
                        files.Add(file);
                    }
                }
                else
                {
                    files.Add(arg);
                }
            }

            if (files.Count == 0)
            {
                PrintUsage();
                return 1;
            }

            if (logo)
            {
                Console.WriteLine("*** XmlStats " + typeof(XmlStats).Assembly.GetName().Version.ToString() + " by Chris Lovett and Andreas Lang");
                Console.WriteLine();
            }

            xs.ProcessFiles(files.ToArray(), summary, Console.Out, "\n");

            if (files.Count > 1)
            {
                Console.WriteLine("*** XmlStats ended.");
            }
            return 0;
        }

        private static List<string> ReadFileNames(string fileName)
        {
            List<string> files = new List<string>();
            using (var reader = new StreamReader(fileName, true))
            {                
                while (!reader.EndOfStream)
                {
                    files.Add(reader.ReadLine());
                }
            }
            return files;
        }

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
                        this._watch.Reset();
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
            using (XmlTextReader r = new XmlTextReader(input))
            {
                this.Process(r);
            }
        }

        /// <summary>
        /// Add stats from given xml file to the current XmlStats.
        /// </summary>
        public void Process(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("*** file not found: " + path);
                return;
            }

            try
            {
                using (XmlTextReader r = new XmlTextReader(path))
                {
                    this.Process(r);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("*** xml error in file: " + path);
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Add stats from the given xml reader to the current XmlStats.
        /// </summary>
        public void Process(XmlTextReader r)
        {
            if (r == null)
            {
                return;
            }

            r.WhitespaceHandling = this._whiteSpace;

            Stack elementStack = new Stack();
            NodeStats currentElement = null;

            while (r.Read())
            {
                switch (r.NodeType)
                {
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Text:
                        {
                            long len = r.Value.Length;
                            currentElement.Chars += len;
                            this._elemChars += len;
                            break;
                        }
                    case XmlNodeType.Element:
                        this._elemCount++;

                        if (r.IsEmptyElement)
                        {
                            this._emptyCount++;
                        }

                        NodeStats es = CountNode(this._elements, r.Name);
                        elementStack.Push(es);
                        currentElement = es;

                        while (r.MoveToNextAttribute())
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
                        break;
                    case XmlNodeType.EndElement:
                        currentElement = (NodeStats)elementStack.Pop();
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
                        this._piCount++;
                        this._piChars += r.Value.Length;
                        break;
                    case XmlNodeType.Comment:
                        this._commentCount++;
                        this._commentChars += r.Value.Length;
                        break;
                    case XmlNodeType.SignificantWhitespace:
                        this._whiteSChars += r.Value.Length;
                        break;
                    case XmlNodeType.Whitespace:
                        this._whiteChars += r.Value.Length;
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

        }

        private void ReportStats(TextWriter output)
        { 
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

            if (this._whiteSpace != WhitespaceHandling.None)
            {
                output.Write("whitespace");
                output.Write(this._newLine);
                output.Write("{0,-20} {1,9:D}", "  chars", this._whiteChars);
                output.Write(this._newLine);
                if (this._whiteSpace == WhitespaceHandling.Significant ||
                    this._whiteSpace == WhitespaceHandling.All)
                {
                    output.Write("{0,-20} {1,9:D}", "  significant", this._whiteSChars);
                    output.Write(this._newLine);
                }
            }

            // elem/attr stats
            output.Write(this._newLine);
            output.Write(this._newLine);
            output.Write("elem/attr                count     chars");
            output.Write(this._newLine);
            output.Write("----------------------------------------");
            output.Write(this._newLine);

            // sort the list.
            SortedList slist = new SortedList(this._elements, new NodeStatsComparer());

            foreach (NodeStats es in slist.Values)
            {
                output.Write("{0,-20} {1,9:D} {2,9:D}", es.Name, es.Count, es.Chars);
                output.Write(this._newLine);
                if (es.Attrs != null)
                {
                    var list = new List<NodeStats>(es.Attrs.Values);
                    list.Sort(new Comparison<NodeStats>((a, b) =>
                    {
                        return a.Name.CompareTo(b.Name);
                    }));

                    foreach (NodeStats ns in list)
                    {
                        output.Write("  @{0,-17} {1,9:D} {2,9:D}", ns.Name, ns.Count, ns.Chars);
                        output.Write(this._newLine);
                    }
                }
            }
            output.Write(this._newLine);
        }

        /// <summary>
        /// Reset all the current stats to zero.
        /// </summary>
        public void Reset()
        {
            this._elements = new Dictionary<string, NodeStats>();
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
            NodeStats es = null;
            if (!ht.TryGetValue(name, out es))
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

    internal class NodeStats
    {
        public string Name;
        public long Count;
        public long Chars;
        public Dictionary<string, NodeStats> Attrs;

        public NodeStats(string name)
        {
            this.Name = name;
            this.Count = 1;
            this.Chars = 0;
        }
    }

    internal class NodeStatsComparer : IComparer
    {
        // Used for sorting keys of NodeStats hashtable, the keys are string objects.
        public int Compare(object x, object y)
        {
            string a = x as string;
            string b = y as string;
            if (a == null)
            {
                return (b == null) ? 0 : -1;
            }
            else if (b == null)
            {
                return (a == null) ? 0 : 1;
            }
            else
            {
                return string.Compare(a, b, StringComparison.Ordinal);
            }
        }
    }
}
/* EoF XmlStats.cs ---------------------------------------------------*/
