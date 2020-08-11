// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Microsoft.Xml
{
    public class XmlStats
    {
        Hashtable elements = new Hashtable();
        long elemCount;
        long emptyCount;
        long attrCount;
        long commentCount;
        long piCount;
        long elemChars;
        long attrChars;
        long commentChars;
        long whiteChars;
        long whiteSChars;
        long piChars;
        string newLine = "\n";
        WhitespaceHandling whiteSpace = WhitespaceHandling.All;
        Stopwatch watch = new Stopwatch();

        public static void PrintUsage()
        {
            Console.WriteLine("*** usage: XmlStats [options] <filenames>");
            Console.WriteLine("    reports statistics about elements, attributes and text found");
            Console.WriteLine("    in the specified XML files (URLs or local file names).");
            Console.WriteLine("    filenames: wildcards in local filenames allowed");
            Console.WriteLine("               (for reporting on all files in a directory)");
            Console.WriteLine("*** options:");
            Console.WriteLine("-v         Generates individual reports for all specified files (default is summary only).");
            Console.WriteLine("-nologo    Removes logo from the report");
            Console.WriteLine("-w[a|s|n]  XML whitespace handling: -wa=All (default), -ws=Significant, -wn=None");
        }

        [STAThread]
        public static void Main(string[] args)
        {
            bool summary = true;
            bool logo = true;
            XmlStats xs = new XmlStats();
            ArrayList files = new ArrayList();

            foreach (string arg in args)
            {
                if (arg.Length > 1 && (arg[0] == '-' || arg[0] == '/'))
                {
                    string larg = arg.Substring(1).ToLower(CultureInfo.CurrentCulture);
                    switch (larg)
                    {
                        case "?":
                        case "h":
                            PrintUsage();
                            return;
                        case "v":
                            summary = false;
                            break;
                        case "wn":
                            xs.whiteSpace = WhitespaceHandling.None;
                            break;
                        case "ws":
                            xs.whiteSpace = WhitespaceHandling.Significant;
                            break;
                        case "wa":
                            xs.whiteSpace = WhitespaceHandling.All;
                            break;
                        case "nologo":
                            logo = false;
                            break;
                        default: // invalid opt
                            Console.WriteLine("+++ invalid option ignored '" + arg + "'");
                            break;
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

            if (logo)
            {
                Console.WriteLine("*** XmlStats V1.09 (March 2003)");
                Console.WriteLine("    (by Chris Lovett and andreas lang,");
                Console.WriteLine("     http://www.lovettsoftware.com/tools/xmlstats/readme.htm)");
                Console.WriteLine();
            }

            xs.ProcessFiles((string[])files.ToArray(typeof(string)), summary, Console.Out, "\n");

            Console.WriteLine("*** XmlStats ended.");
        }

        public void ProcessFiles(string[] files, bool summary, TextWriter output, string newLineChar)
        {
            this.newLine = newLineChar;

            this.Reset();
            int count = 0;

            foreach (string file in files)
            {
                if (!summary)
                {
                    this.Reset();
                }

                try
                {
                    this.Process(file);
                    count++;
                    if (!summary)
                    {
                        this.Report(file, output);
                    }
                }
                catch (Exception e)
                {
                    output.Write("+++ error in file '" + file + "':");
                    output.Write(this.newLine);
                    output.Write(e.Message);
                    output.Write(this.newLine);
                }
            }

            if (summary && count > 0)
            {
                this.Report("XmlStats", output);
            }
        }

        public void Process(TextReader input)
        {
            this.Reset();
            using (XmlTextReader r = new XmlTextReader(input))
            {
                this.Process(r);
            }
        }

        public void Process(string path)
        {
            try
            {
                using (XmlTextReader r = new XmlTextReader(path))
                {
                    this.Process(r);
                }
            }
            catch (Exception)
            {
                // ok to ignore bad xml here
            }
        }

        public void Process(XmlTextReader r)
        {
            if (r == null)
            {
                return;
            }

            r.WhitespaceHandling = this.whiteSpace;

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
                            this.elemChars += len;
                            break;
                        }
                    case XmlNodeType.Element:
                        this.elemCount++;

                        if (r.IsEmptyElement)
                        {
                            this.emptyCount++;
                        }

                        NodeStats es = CountNode(this.elements, r.Name);
                        elementStack.Push(es);
                        currentElement = es;

                        if (es.Attrs == null)
                        {
                            es.Attrs = new Hashtable();
                        }

                        Hashtable attrs = es.Attrs;

                        while (r.MoveToNextAttribute())
                        {
                            this.attrCount++;

                            // create a name that makes attributes unique to their parent elements
                            NodeStats ns = CountNode(attrs, r.Name);
                            string s = r.Value;
                            if (s != null)
                            {
                                long len = r.Value.Length;
                                ns.Chars += len;
                                this.attrChars += len;
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
                        this.piCount++;
                        this.piChars += r.Value.Length;
                        break;
                    case XmlNodeType.Comment:
                        this.commentCount++;
                        this.commentChars += r.Value.Length;
                        break;
                    case XmlNodeType.SignificantWhitespace:
                        this.whiteSChars += r.Value.Length;
                        break;
                    case XmlNodeType.Whitespace:
                        this.whiteChars += r.Value.Length;
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

        public string GetReport()
        {
            using (StringWriter sw = new StringWriter())
            {
                this.Report("Summary", sw);
                return sw.ToString();
            }
        }

        public void Report(string path, TextWriter output)
        {
            output.Write("*** " + path);                     // filename or "Summary"

            this.watch.Stop();
            float time = this.watch.ElapsedMilliseconds;
            if (time > 1000)
            {
                output.Write("   ({0,1:F} secs)", time / 1000f);
            }
            else
            {
                output.Write("   ({0,1:F} msecs)", time);
            }

            output.Write(this.newLine);
            output.Write(this.newLine);

            // count how many unique attributes
            long attrsCount = 0;
            foreach (NodeStats ns in this.elements.Values)
            {
                attrsCount += ns.Attrs.Count;
            }

            // overall stats
            output.Write("elements");
            output.Write(this.newLine);
            output.Write("{0,-20} {1,9:D}", "  unique", this.elements.Count);
            output.Write(this.newLine);
            output.Write("{0,-20} {1,9:D}", "  empty", this.emptyCount);
            output.Write(this.newLine);
            output.Write("{0,-20} {1,9:D}", "  total", this.elemCount);
            output.Write(this.newLine);
            output.Write("{0,-20} {1,9:D}", "  chars", this.elemChars);
            output.Write(this.newLine);

            output.Write("attributes");
            output.Write(this.newLine);
            output.Write("{0,-20} {1,9:D}", "  unique", attrsCount);
            output.Write(this.newLine);
            output.Write("{0,-20} {1,9:D}", "  total", this.attrCount);
            output.Write(this.newLine);
            output.Write("{0,-20} {1,9:D}", "  chars", this.attrChars);
            output.Write(this.newLine);

            output.Write("comments");
            output.Write(this.newLine);
            output.Write("{0,-20} {1,9:D}", "  total", this.commentCount);
            output.Write(this.newLine);
            output.Write("{0,-20} {1,9:D}", "  chars", this.commentChars);
            output.Write(this.newLine);

            output.Write("PIs");
            output.Write(this.newLine);
            output.Write("{0,-20} {1,9:D}", "  total", this.piCount);
            output.Write(this.newLine);
            output.Write("{0,-20} {1,9:D}", "  chars", this.piChars);
            output.Write(this.newLine);

            if (this.whiteSpace != WhitespaceHandling.None)
            {
                output.Write("whitespace");
                output.Write(this.newLine);
                output.Write("{0,-20} {1,9:D}", "  chars", this.whiteChars);
                output.Write(this.newLine);
                if (this.whiteSpace == WhitespaceHandling.Significant ||
                    this.whiteSpace == WhitespaceHandling.All)
                {
                    output.Write("{0,-20} {1,9:D}", "  significant", this.whiteSChars);
                    output.Write(this.newLine);
                }
            }

            // elem/attr stats
            output.Write(this.newLine);
            output.Write(this.newLine);
            output.Write("elem/attr                count     chars");
            output.Write(this.newLine);
            output.Write("----------------------------------------");
            output.Write(this.newLine);

            // sort the list.
            SortedList slist = new SortedList(this.elements, new NodeStatsComparer());

            foreach (NodeStats es in slist.Values)
            {
                output.Write("{0,-20} {1,9:D} {2,9:D}", es.Name, es.Count, es.Chars);
                output.Write(this.newLine);
                foreach (NodeStats ns in es.Attrs.Values)
                {
                    output.Write("  @{0,-17} {1,9:D} {2,9:D}", ns.Name, ns.Count, ns.Chars);
                    output.Write(this.newLine);
                }
            }
            output.Write(this.newLine);
        }

        internal void Reset()
        {
            this.elements = new Hashtable();
            this.elemCount = 0;
            this.emptyCount = 0;
            this.attrCount = 0;
            this.commentCount = 0;
            this.piCount = 0;
            this.elemChars = 0;
            this.attrChars = 0;
            this.commentChars = 0;
            this.piChars = 0;
            this.whiteChars = 0;
            this.whiteSChars = 0;

            this.watch.Reset();
            this.watch.Start();
        }

        internal static NodeStats CountNode(Hashtable ht, string name)
        {
            NodeStats es = (NodeStats)ht[name];
            if (es == null)
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
        public Hashtable Attrs;

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
