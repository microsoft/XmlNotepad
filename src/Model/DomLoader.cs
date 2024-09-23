using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace XmlNotepad
{
    /// <summary>
    /// This class keeps track of DOM node line locations so you can do error reporting.
    /// </summary>
    internal class DomLoader
    {
        private Dictionary<XmlNode, LineInfo> _lineTable;
        private List<LineInfo> _lineInfos = new List<LineInfo>();
        private LineInfo _firstRealLine;
        private XmlDocument _doc;
        private XmlReader _reader;
        private IServiceProvider _site;
        private SchemaCache _sc;
        private long _maxLineIndex;
        private HashSet<string> _allNamespaces = new HashSet<string>();

        public DomLoader(IServiceProvider site, SchemaCache cache)
        {
            this._site = site;
            this._sc = cache;
            this._maxLineIndex = Settings.Instance.GetLong("MaximumLineIndex");
        }

        public HashSet<string> AllNamespaces => _allNamespaces;

        void AddToTable(XmlNode node)
        {
            var nsuri = node.NamespaceURI;
            if (!string.IsNullOrEmpty(nsuri))
            {
                _allNamespaces.Add(nsuri);
            }
            // stop these tables from eating up too much memory on very large XML documents.
            if (this._lineInfos.Count < _maxLineIndex)
            {
                var info = new LineInfo(_reader);
                info.Node = node;
                this._lineInfos.Add(info); // remember the order
                if (_firstRealLine == null && info.LineNumber != 0)
                {
                    _firstRealLine = info;
                }
            }
        }

        void GetOrCreateNodeLineTable()
        {
            if (this._lineTable == null)
            {
                this._lineTable = new Dictionary<XmlNode, LineInfo>();
                foreach (var info in _lineInfos)
                {
                    this._lineTable[info.Node] = info;
                }
            }
        }

        public LineInfo GetLineInfo(XmlNode node)
        {
            if (node == null)
            {
                return null;
            }
            GetOrCreateNodeLineTable();
            if (node != null && this._lineTable.TryGetValue(node, out LineInfo info))
            {
                return info;
            }
            return null;
        }

        /// <summary>
        /// Return the node closest to the given line/col or null if the line
        /// is out of range.  Returns the node that spans this position, or the node cloest
        /// to it.
        /// </summary>
        public XmlNode FindNodeAt(int line, int column)
        {
            if (line < 0)
            {
                line = 0;
            }
            if (column < 0)
            {
                column = 0;
            }
            if (this._firstRealLine == null)
            {
                return null;
            }
            // binary search since the list is sorted.
            int start = this._lineInfos.IndexOf(this._firstRealLine);
            int end = this._lineInfos.Count - 1;
            while (true)
            {
                int mid = (end + start) / 2;
                var startinfo = this._lineInfos[start];
                var endinfo = this._lineInfos[end];
                var midinfo = this._lineInfos[mid];
                if (end == start || end == start + 1)
                {
                    // We have drilled right down to the last 2 nodes.
                    if (line == startinfo.LineNumber && line == endinfo.LineNumber)
                    {
                        // then decision might be based on column.
                        if (column > startinfo.LinePosition && column == endinfo.LinePosition)
                        {
                            return endinfo.Node;
                        }
                    }
                    else if (line > startinfo.LineNumber && line <= endinfo.LineNumber)
                    {
                        return endinfo.Node;
                    }
                    return startinfo.Node;
                }

                if (line == startinfo.LineNumber && line == endinfo.LineNumber)
                {
                    // then use line position instead!
                    if (column == startinfo.LinePosition && column == endinfo.LinePosition)
                    {
                        // hmmm, that's weird, is the line info all zeros?
                        // nothing to be gained by going any further then.
                        return startinfo.Node;

                    }

                    if (column <= startinfo.LinePosition)
                    {
                        return startinfo.Node;
                    }

                    Debug.Assert(column > startinfo.LinePosition);
                    if (column <= midinfo.LinePosition)
                    {
                        // drill into top half
                        end = mid;
                    }
                    else
                    {
                        // drill into bottom half.
                        start = mid;
                    }
                }
                else
                {
                    if (line <= startinfo.LineNumber)
                    {
                        return startinfo.Node;
                    }

                    Debug.Assert(line > startinfo.LineNumber);
                    if (line <= midinfo.LineNumber)
                    {
                        // drill into top half
                        end = mid;
                    }
                    else
                    {
                        // drill into bottom half.
                        start = mid;
                    }
                }
            }
        }

        public int GetLastLine()
        {
            if (this._lineInfos.Count == 0)
            {
                return 0;
            }
            return this._lineInfos[this._lineInfos.Count - 1].LineNumber;
        }

        public XmlDocument Load(XmlReader r)
        {
            this._lineTable = null;
            this._lineInfos = new List<LineInfo>();
            this._firstRealLine = null;
            this._doc = new XmlDocument();
            this._doc.PreserveWhitespace = Settings.Instance.GetBoolean("PreserveWhitespace");
            this._doc.XmlResolver = Settings.Instance.Resolver;
            this._doc.Schemas.XmlResolver = Settings.Instance.Resolver;
            SetLoading(this._doc, true);
            try
            {
                this._reader = r;
                AddToTable(this._doc);
                LoadDocument();
            }
            finally
            {
                SetLoading(this._doc, false);
            }
            return _doc;
        }

        void SetLoading(XmlDocument doc, bool flag)
        {
            FieldInfo fi = this._doc.GetType().GetField("isLoading", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi != null)
            {
                fi.SetValue(doc, flag);
            }
        }

        private void LoadDocument()
        {
            bool preserveWhitespace = this._doc.PreserveWhitespace;
            XmlReader r = this._reader;
            XmlNode parent = this._doc;
            XmlElement element;
            while (r.Read())
            {
                XmlNode node = null;
                switch (r.NodeType)
                {
                    case XmlNodeType.Element:
                        bool fEmptyElement = r.IsEmptyElement;
                        element = _doc.CreateElement(r.Prefix, r.LocalName, r.NamespaceURI);

                        AddToTable(element);
                        element.IsEmpty = fEmptyElement;
                        ReadAttributes(r, element);

                        if (!fEmptyElement)
                        {
                            parent.AppendChild(element);
                            parent = element;
                            continue;
                        }
                        node = element;
                        break;

                    case XmlNodeType.EndElement:
                        if (parent.ParentNode == null)
                        {
                            // syntax error in document.
                            IXmlLineInfo li = (IXmlLineInfo)r;
                            throw new XmlException(string.Format(Strings.UnexpectedToken,
                                "</" + r.LocalName + ">", li.LineNumber, li.LinePosition), null, li.LineNumber, li.LinePosition);
                        }
                        parent = parent.ParentNode;
                        continue;

                    case XmlNodeType.EntityReference:
                        if (r.CanResolveEntity)
                        {
                            r.ResolveEntity();
                        }
                        continue;

                    case XmlNodeType.EndEntity:
                        continue;

                    case XmlNodeType.Attribute:
                        node = LoadAttributeNode();
                        break;

                    case XmlNodeType.Text:
                        node = _doc.CreateTextNode(r.Value);
                        AddToTable(node);
                        break;

                    case XmlNodeType.SignificantWhitespace:
                        node = _doc.CreateSignificantWhitespace(r.Value);
                        AddToTable(node);
                        break;

                    case XmlNodeType.Whitespace:
                        if (preserveWhitespace)
                        {
                            node = _doc.CreateWhitespace(r.Value);
                            AddToTable(node);
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    case XmlNodeType.CDATA:
                        node = _doc.CreateCDataSection(r.Value);
                        AddToTable(node);
                        break;

                    case XmlNodeType.XmlDeclaration:
                        node = LoadDeclarationNode();
                        break;

                    case XmlNodeType.ProcessingInstruction:
                        node = _doc.CreateProcessingInstruction(r.Name, r.Value);
                        AddToTable(node);
                        if (string.IsNullOrEmpty(this.xsltFileName) && r.Name == "xml-stylesheet")
                        {
                            this.xsltFileName = ParseXsltArgs(((XmlProcessingInstruction)node).Data);
                        }
                        else if (string.IsNullOrEmpty(this.xsltDefaultOutput) && r.Name == "xsl-output")
                        {
                            this.xsltDefaultOutput = ParseXsltOutputArgs(((XmlProcessingInstruction)node).Data);
                        }
                        break;

                    case XmlNodeType.Comment:
                        node = _doc.CreateComment(r.Value);
                        AddToTable(node);
                        break;

                    case XmlNodeType.DocumentType:
                        {
                            string pubid = r.GetAttribute("PUBLIC");
                            string sysid = r.GetAttribute("SYSTEM");
                            node = _doc.CreateDocumentType(r.Name, pubid, sysid, r.Value);
                            break;
                        }

                    default:
                        UnexpectedNodeType(r.NodeType);
                        break;
                }

                Debug.Assert(node != null);
                Debug.Assert(parent != null);
                if (parent != null)
                {
                    parent.AppendChild(node);
                }
            }
        }

        public static string ParseXsltArgs(string data)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<xsl " + data + "/>");
                XmlElement root = doc.DocumentElement;
                if (root.GetAttribute("type") == "text/xsl")
                {
                    return root.GetAttribute("href");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error parsing XSLT args: " + ex.Message);
            }
            return null;
        }

        public static string ParseXsltOutputArgs(string data)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<xsl " + data + "/>");
                XmlElement root = doc.DocumentElement;
                return root.GetAttribute("default");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error parsing <?xsl-output args: " + ex.Message);
            }
            return null;
        }


        private void ReadAttributes(XmlReader r, XmlElement element)
        {
            if (r.MoveToFirstAttribute())
            {
                XmlAttributeCollection attributes = element.Attributes;
                do
                {
                    XmlAttribute attr = LoadAttributeNode();
                    attributes.Append(attr); // special case for load
                }
                while (r.MoveToNextAttribute());
                r.MoveToElement();
            }
        }
        string xsltFileName = null;
        string xsltDefaultOutput = null;

        public string XsltFileName
        {
            get { return this.xsltFileName; }
        }

        public string XsltDefaultOutput
        {
            get { return this.xsltDefaultOutput; }
        }

        private XmlAttribute LoadAttributeNode()
        {
            Debug.Assert(_reader.NodeType == XmlNodeType.Attribute);

            XmlReader r = _reader;
            XmlAttribute attr = _doc.CreateAttribute(r.Prefix, r.LocalName, r.NamespaceURI);
            AddToTable(attr);
            XmlNode parent = attr;

            while (r.ReadAttributeValue())
            {
                XmlNode node = null;
                switch (r.NodeType)
                {
                    case XmlNodeType.Text:
                        node = _doc.CreateTextNode(r.Value);
                        AddToTable(node);
                        break;
                    case XmlNodeType.EntityReference:
                        if (r.CanResolveEntity)
                        {
                            r.ResolveEntity();
                        }
                        continue;
                    case XmlNodeType.EndEntity:
                        continue;
                    default:
                        UnexpectedNodeType(r.NodeType);
                        break;
                }
                Debug.Assert(node != null);
                parent.AppendChild(node);
            }
            if (attr.NamespaceURI == XmlStandardUris.XsiUri)
            {
                HandleXsiAttribute(attr);
            }
            else if (attr.NamespaceURI == XmlStandardUris.XmlnsUri)
            {
                var nsuri = attr.InnerText;
                if (!string.IsNullOrEmpty(nsuri))
                {
                    _allNamespaces.Add(nsuri);
                }
            }
            return attr;
        }

        void HandleXsiAttribute(XmlAttribute a)
        {
            switch (a.LocalName)
            {
                case "schemaLocation":
                    LoadSchemaLocations(a.Value);
                    break;
                case "noNamespaceSchemaLocation":
                    LoadSchema(null, a.Value);
                    break;
            }
        }

        void LoadSchemaLocations(string pairs)
        {
            string[] words = pairs.Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0, n = words.Length; i < n; i++)
            {
                if (i + 1 < n)
                {
                    string ns = words[i];
                    i++;
                    string url = words[i];
                    LoadSchema(ns, url);
                }
            }
        }

        void LoadSchema(string targetNamespace, string fname)
        {
            try
            {
                Uri resolved = new Uri(new Uri(_reader.BaseURI), fname);
                var schema = this._sc.LoadSchema(targetNamespace, resolved);
                this._doc.Schemas.Add(schema);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading schema: " + ex.Message);
            }
        }

        private XmlDeclaration LoadDeclarationNode()
        {
            Debug.Assert(_reader.NodeType == XmlNodeType.XmlDeclaration);

            //parse data
            XmlDeclaration decl = _doc.CreateXmlDeclaration("1.0", null, null);
            AddToTable(decl);

            // Try first to use the reader to get the xml decl "attributes". Since not all readers are required to support this, it is possible to have
            // implementations that do nothing
            while (_reader.MoveToNextAttribute())
            {
                switch (_reader.Name)
                {
                    case "version":
                        break;
                    case "encoding":
                        decl.Encoding = _reader.Value;
                        break;
                    case "standalone":
                        decl.Standalone = _reader.Value;
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
            }
            return decl;
        }

        void UnexpectedNodeType(XmlNodeType type)
        {
            IXmlLineInfo li = (IXmlLineInfo)_reader;
            throw new XmlException(string.Format(Strings.UnexpectedNodeType, type.ToString()), null,
                li.LineNumber, li.LinePosition);
        }

    }

    public class LineInfo : IXmlLineInfo
    {
        int line, col;
        string baseUri;
        IXmlSchemaInfo info;

        internal LineInfo(int line, int col)
        {
            this.line = line;
            this.col = col;
        }

        internal LineInfo(XmlReader reader)
        {
            IXmlLineInfo li = reader as IXmlLineInfo;
            if (li != null)
            {
                this.line = li.LineNumber;
                this.col = li.LinePosition;
                this.baseUri = reader.BaseURI;
                this.info = reader.SchemaInfo;
            }
        }
        public bool HasLineInfo()
        {
            return true;
        }

        public int LineNumber
        {
            get { return this.line; }
        }

        public int LinePosition
        {
            get { return this.col; }
        }

        public string BaseUri
        {
            get { return this.baseUri; }
        }

        public IXmlSchemaInfo SchemaInfo
        {
            get { return this.info; }
        }

        public XmlNode Node { get; set; }
    }
}
