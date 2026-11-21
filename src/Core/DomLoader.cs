using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;

namespace XmlNotepad
{
    /// <summary>
    /// This class keeps track of DOM node line locations so you can do error reporting.
    /// </summary>
    internal class DomLoader
    {
        private readonly Dictionary<XmlNode, LineInfo> _lineTable = new Dictionary<XmlNode, LineInfo>();
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
            if (_lineTable.Count < _maxLineIndex)
            {
                _lineTable[node] = new LineInfo(_reader);
            }
        }

        public LineInfo GetLineInfo(XmlNode node) =>
            node != null && _lineTable.TryGetValue(node, out var info) ? info : null;

        public XmlDocument Load(XmlReader r)
        {
            _lineTable.Clear();
            _allNamespaces.Clear();
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

        internal LineInfo(XmlReader reader)
        {
            IXmlLineInfo li = reader as IXmlLineInfo;
            if (li != null)
            {
                this.line = li.LineNumber;
                this.col = li.LinePosition;
                this.baseUri = reader.BaseURI;
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

    }
}
