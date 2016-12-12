/***********************************************************************
* FileName:    XmlStats.cs
*
* Description: reports statistics about XML files
*
* Authors:     Chris Lovett and Andreas Lang
*
* Archive:     http://www.lovettsoftware.com/tools/xmlstats/readme.htm
*
* History:     See readme.
*
* todo / wish list:
*
* - option -e and entity ref list
* - report size/timestamp of files
* - calculate percentages of various "total chars" related to file size
* - report min/max/avg length of text of every elem
*
***********************************************************************/

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using System.Collections;

namespace Microsoft.Xml
{
    public class XmlStats : PerfTimer
    {
        Hashtable elements = new Hashtable();
        long elemCount = 0;
        long emptyCount = 0;
        long attrCount = 0;
        long cmntCount = 0;
        long piCount = 0;
        long elemChars = 0;
        long attrChars = 0;
        long cmntChars = 0;
        long whiteChars = 0;
        long whiteSChars = 0;
        long piChars = 0;
        WhitespaceHandling whiteSpace = WhitespaceHandling.All;

        static void PrintUsage()
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
        static void Main(string[] args)
        {
            bool summary = true;
            bool logo = true;
            XmlStats xs = new XmlStats();
            ArrayList files = new ArrayList();

            foreach (string arg in args)
            {
                if (arg.Length > 1 && (arg[0] == '-' || arg[0] == '/'))
                {
                    string larg = arg.Substring(1).ToLower();
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
                else if (arg.IndexOf("://") > 0)  // url
                {
                    files.Add(arg);
                }
                else if (arg.IndexOf("*") >= 0 || arg.IndexOf("?") >= 0)     // wildcard
                {                                                             // resolve
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

        string newLine = "\n";

        public void ProcessFiles(string[] files, bool summary, TextWriter output, string newLineChar)
        {
            this.newLine = newLineChar;

            Reset();
            int count = 0;

            foreach (string file in files)
            {
                if (!summary)
                    Reset();

                try
                {
                    Process(file);
                    count++;
                    if (!summary)
                        Report(file, output);
                }
                catch (Exception e)
                {
                    output.Write("+++ error in file '" + file + "':");
                    output.Write(newLine);
                    output.Write(e.Message);
                    output.Write(newLine);
                }
            }

            if (summary && count > 0)
            {
                Report("XmlStats", output);
            }
        }

        public void Process(TextReader input)
        {
            Reset();
            XmlTextReader r = new XmlTextReader(input);
            Process(r);
        }

        public void Process(string path)
        {
            XmlTextReader r = new XmlTextReader(path);
            Process(r);
        }

        public void Process(XmlTextReader r)
        {
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
                            elemChars += len;
                            break;
                        }
                    case XmlNodeType.Element:
                        elemCount++;

                        if (r.IsEmptyElement)
                            emptyCount++;

                        NodeStats es = CountNode(elements, r.Name);
                        elementStack.Push(es);
                        currentElement = es;

                        if (es.attrs == null)
                            es.attrs = new Hashtable();
                        Hashtable attrs = es.attrs;

                        while (r.MoveToNextAttribute())
                        {
                            attrCount++;
                            // create a name that makes attributes unique to their parent elements
                            NodeStats ns = CountNode(attrs, r.Name);
                            string s = r.Value;
                            if (s != null)
                            {
                                long len = r.Value.Length;
                                ns.Chars += len;
                                attrChars += len;
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
                        piCount++;
                        piChars += r.Value.Length;
                        break;
                    case XmlNodeType.Comment:
                        cmntCount++;
                        cmntChars += r.Value.Length;
                        break;
                    case XmlNodeType.SignificantWhitespace:
                        whiteSChars += r.Value.Length;
                        break;
                    case XmlNodeType.Whitespace:
                        whiteChars += r.Value.Length;
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
            StringWriter sw = new StringWriter();
            Report("Summary", sw);
            return sw.ToString();
        }

        public void Report(string path, TextWriter output)
        {
            output.Write("*** " + path);                     // filename or "Summary"

            this.Stop();
            float time = this.Milliseconds;
            if (time > 1000)
            {
                output.Write("   ({0,1:F} secs)", (time / 1000f));
            }
            else
            {
                output.Write("   ({0,1:F} msecs)", time);
            }

            output.Write(newLine);
            output.Write(newLine);

            // count how many unique attributes
            long attrsCount = 0;
            foreach (NodeStats ns in elements.Values)
            {
                attrsCount += ns.attrs.Count;
            }

            // overall stats
            output.Write("elements");
            output.Write(newLine);
            output.Write("{0,-20} {1,9:D}", "  unique", elements.Count);
            output.Write(newLine);
            output.Write("{0,-20} {1,9:D}", "  empty", emptyCount);
            output.Write(newLine);
            output.Write("{0,-20} {1,9:D}", "  total", elemCount);
            output.Write(newLine);
            output.Write("{0,-20} {1,9:D}", "  chars", elemChars);
            output.Write(newLine);

            output.Write("attributes");
            output.Write(newLine);
            output.Write("{0,-20} {1,9:D}", "  unique", attrsCount);
            output.Write(newLine);
            output.Write("{0,-20} {1,9:D}", "  total", attrCount);
            output.Write(newLine);
            output.Write("{0,-20} {1,9:D}", "  chars", attrChars);
            output.Write(newLine);

            output.Write("comments");
            output.Write(newLine);
            output.Write("{0,-20} {1,9:D}", "  total", cmntCount);
            output.Write(newLine);
            output.Write("{0,-20} {1,9:D}", "  chars", cmntChars);
            output.Write(newLine);

            output.Write("PIs");
            output.Write(newLine);
            output.Write("{0,-20} {1,9:D}", "  total", piCount);
            output.Write(newLine);
            output.Write("{0,-20} {1,9:D}", "  chars", piChars);
            output.Write(newLine);

            if (this.whiteSpace != WhitespaceHandling.None)
            {
                output.Write("whitespace");
                output.Write(newLine);
                output.Write("{0,-20} {1,9:D}", "  chars", whiteChars);
                output.Write(newLine);
                if (this.whiteSpace == WhitespaceHandling.Significant ||
                    this.whiteSpace == WhitespaceHandling.All)
                {
                    output.Write("{0,-20} {1,9:D}", "  significant", whiteSChars);
                    output.Write(newLine);
                }
            }

            // elem/attr stats
            output.Write(newLine);
            output.Write(newLine);
            output.Write("elem/attr                count     chars");
            output.Write(newLine);
            output.Write("----------------------------------------");
            output.Write(newLine);

            // sort the list.
            SortedList slist = new SortedList(elements, new NodeStatsComparer());

            foreach (NodeStats es in slist.Values)
            {
                output.Write("{0,-20} {1,9:D} {2,9:D}", es.Name, es.Count, es.Chars);
                output.Write(newLine);
                foreach (NodeStats ns in es.attrs.Values)
                {
                    output.Write("  @{0,-17} {1,9:D} {2,9:D}", ns.Name, ns.Count, ns.Chars);
                    output.Write(newLine);
                }
            }
            output.Write(newLine);
        }

        internal void Reset()
        {
            this.elements = new Hashtable();
            this.elemCount = 0;
            this.emptyCount = 0;
            this.attrCount = 0;
            this.cmntCount = 0;
            this.piCount = 0;
            this.elemChars = 0;
            this.attrChars = 0;
            this.cmntChars = 0;
            this.piChars = 0;
            this.whiteChars = 0;
            this.whiteSChars = 0;

            this.Start();
        }

        internal NodeStats CountNode(Hashtable ht, string name)
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
        public NodeStats(string name)
        {
            this.Name = name;
            this.Count = 1;
            this.Chars = 0;
        }
        public string Name;
        public long Count;
        public long Chars;
        public Hashtable attrs;
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
                return a.CompareTo(b);
            }
        }
    }

    public class PerfTimer
    {
        [DllImport("kernel32.dll", EntryPoint = "QueryPerformanceCounter", CharSet = CharSet.Unicode)]
        extern static bool QueryPerformanceCounter(out long perfcount);

        [DllImport("kernel32.dll", EntryPoint = "QueryPerformanceFrequency", CharSet = CharSet.Unicode)]
        extern static bool QueryPerformanceFrequency(out long frequency);

        long startTime;
        long stopTime;

        public void Start()
        {
            QueryPerformanceCounter(out this.startTime);
        }

        public void Stop()
        {
            QueryPerformanceCounter(out this.stopTime);
        }

        public float Milliseconds
        {
            get
            {
                long frequency;
                QueryPerformanceFrequency(out frequency);
                float diff = (stopTime - startTime);
                return diff * 1000f / (float)frequency;
            }
        }
    }
}
/* EoF XmlStats.cs ---------------------------------------------------*/
