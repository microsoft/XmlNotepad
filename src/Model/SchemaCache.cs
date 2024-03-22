using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Globalization;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Xml.Linq;

namespace XmlNotepad
{
    /// <summary>
    /// This class represents a cached schema which may or may not be loaded yet.
    /// This allows delay loading of schemas.
    /// </summary>
    public class CacheEntry
    {
        private string _targetNamespace;
        private Uri _location;
        private XmlSchema _schema;
        private bool _disabled;
        private string _fileName;
        private DateTime _lastModified;
        private CacheEntry _next; // entry with same targetNamespace;

        public string TargetNamespace
        {
            get { return _targetNamespace; }
            set { _targetNamespace = value; }
        }

        public Uri Location
        {
            get { return _location; }
            set
            {
                _location = value;
                _schema = null;
                if (_location.IsFile)
                {
                    _fileName = _location.LocalPath;
                }
            }
        }

        public bool HasUpToDateSchema
        {
            get
            {
                if (_schema == null) return false;
                if (_fileName != null)
                {
                    DateTime lastWriteTime = File.GetLastWriteTime(_fileName);
                    if (lastWriteTime > this._lastModified)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public XmlSchema Schema
        {
            get { return _schema; }
            set
            {
                if (_schema != value)
                {
                    _schema = value;
                    if (_fileName != null)
                    {
                        this._lastModified = File.GetLastWriteTime(_fileName);
                    }
                }
            }
        }

        public CacheEntry Next
        {
            get { return _next; }
            set { _next = value; }
        }

        public bool Disabled
        {
            get { return _disabled; }
            set { _disabled = value; }
        }

        public CacheEntry FindByUri(Uri uri)
        {
            CacheEntry e = this;
            while (e != null)
            {
                if (e._location == uri)
                {
                    return e;
                }
                e = e._next;
            }
            return null;
        }

        // Remove the given cache entry and return the new head of the linked list.
        public CacheEntry RemoveUri(Uri uri)
        {
            CacheEntry e = this;
            CacheEntry previous = null;
            while (e != null)
            {
                if (e._location == uri)
                {
                    if (previous == null)
                    {
                        return e._next; // return new head
                    }
                    previous._next = e._next; //unlink it
                    return this; // head is unchanged.
                }
                previous = e;
                e = e._next;
            }
            return this;
        }

        public void Add(CacheEntry newEntry)
        {
            CacheEntry e = this;
            while (e != null)
            {
                if (e == newEntry)
                {
                    return;
                }
                if (e._location == newEntry._location)
                {
                    e._schema = newEntry._schema;
                    e._lastModified = newEntry._lastModified;
                    e._disabled = newEntry._disabled;
                    return;
                }
                if (e._next == null)
                {
                    e._next = newEntry;
                    break;
                }
                e = e._next;
            }
        }

    }

    /// <summary>
    /// This class encapsulates an XmlSchema manager that loads schemas and associates them with
    /// the XML documents being edited. It also tracks changes to the schemas on disk and reloads
    /// them when necessary.
    /// </summary>
    public class SchemaCache : IXmlSerializable
    {
        //MCorning 10.19.06 Added event so New Menu can populate submenu with nsuri values
        public event EventHandler Changed;

        // targetNamespace -> CacheEntry
        Dictionary<string, CacheEntry> namespaceMap = new Dictionary<string, CacheEntry>();
        // sourceUri -> CacheEntry
        Dictionary<Uri, CacheEntry> uriMap = new Dictionary<Uri, CacheEntry>();
        PersistentFileNames pfn;
        IServiceProvider site;
        private XmlResolver resolver;
        private List<XmlSchemaElement> globalElementCache;

        public SchemaCache(IServiceProvider site)
        {
            this.site = site;
            this.pfn = new PersistentFileNames(Settings.Instance.StartupPath);
            this.resolver = new SchemaResolver(this.site, this);
        }

        void FireOnChanged()
        {
            globalElementCache = null;
            if (null != this.Changed)
            {
                this.Changed(this, EventArgs.Empty);
            }
        }

        public void Clear()
        {
            namespaceMap.Clear();
            uriMap.Clear();
        }

        public IList<CacheEntry> GetSchemas()
        {
            List<CacheEntry> list = new List<CacheEntry>();
            foreach (CacheEntry ce in namespaceMap.Values)
            {
                CacheEntry e = ce;
                while (e != null)
                {
                    list.Add(e);
                    e = e.Next;
                }
            }
            return list;
        }

        public CacheEntry Add(string nsuri, Uri uri, bool disabled)
        {
            if (nsuri == null) nsuri = "";

            CacheEntry existing = null;
            CacheEntry e = null;

            if (namespaceMap.ContainsKey(nsuri))
            {
                existing = namespaceMap[nsuri];
                e = existing.FindByUri(uri);
            }
            if (e == null)
            {
                e = new CacheEntry();
                e.Location = uri;
                e.TargetNamespace = nsuri;

                if (existing != null)
                {
                    existing.Add(e);
                }
                else
                {
                    namespaceMap[nsuri] = e;
                }
            }
            e.Disabled = disabled;
            if (uriMap.ContainsKey(uri))
            {
                CacheEntry oe = (CacheEntry)uriMap[uri];
                if (oe != e)
                {
                    // target namespace must have changed!
                    nsuri = oe.TargetNamespace;
                    if (nsuri == null) nsuri = "";
                    if (namespaceMap.ContainsKey(nsuri))
                    {
                        namespaceMap.Remove(nsuri);
                    }
                }
            }
            uriMap[uri] = e;
            this.FireOnChanged();

            return e;
        }

        public CacheEntry Add(XmlSchema s)
        {
            if (s.SourceUri == null)
            {
                // then this is a built in schema like the one for http://www.w3.org/XML/1998/namespace
                return null;
            }
            CacheEntry e = Add(s.TargetNamespace, new Uri(s.SourceUri), false);
            if (e.Schema == null)
            {
                e.Schema = s;
            }

            // There is a bug in the .NET core version of XmlSchema where some imports have
            // the .Schema property nulled out (like for http://www.w3.org/2000/09/xmldsig#)
            // So we fix that here and force loading of those schemas.
            AddImports(s);
            return e;
        }

        private void AddImports(XmlSchema parent)
        {
            foreach (var o in parent.Includes)
            {
                if (o is XmlSchemaInclude i)
                {
                    XmlSchema s = i.Schema;
                    if (s == null)
                    {
                        s = LoadSchema(new Uri(new Uri(i.SourceUri), i.SchemaLocation));
                    }
                    if (s != null && !ContainsSchema(s))
                    {
                        Add(s);
                    }
                }
                else if (o is XmlSchemaImport j)
                {
                    XmlSchema s = j.Schema;
                    if (s == null)
                    {
                        s = LoadSchema(new Uri(new Uri(j.SourceUri), j.SchemaLocation));
                    }
                    if (s != null && !ContainsSchema(s))
                    {
                        Add(s);
                    }
                }
            }

        }

        private XmlSchema LoadSchema(Uri uri)
        {
            try
            {
                return (XmlSchema)this.Resolver.GetEntity(uri, "", typeof(XmlSchema));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading schema: " + ex.Message);
            }
            return null;
        }

        public void Remove(CacheEntry ce)
        {
            Remove(ce.Location);
        }

        public void Remove(Uri uri)
        {
            if (uriMap.ContainsKey(uri))
            {
                CacheEntry e = uriMap[uri];
                uriMap.Remove(uri);
                string key = e.TargetNamespace;
                if (namespaceMap.ContainsKey(key))
                {
                    CacheEntry head = namespaceMap[key];
                    CacheEntry newHead = head.RemoveUri(uri);
                    if (newHead == null)
                    {
                        namespaceMap.Remove(key);
                    }
                    else if (newHead != head)
                    {
                        namespaceMap[key] = newHead;
                    }
                    this.FireOnChanged();
                }
            }
        }

        public void Remove(string filename)
        {
            Uri uri = new Uri(filename);
            Remove(uri);
        }

        public void Remove(XmlSchema s)
        {
            Remove(s.SourceUri);
        }

        public CacheEntry FindSchemasByNamespace(string targetNamespace)
        {
            if (namespaceMap.ContainsKey(targetNamespace))
            {
                return namespaceMap[targetNamespace];
            }
            return null;
        }

        public bool ContainsSchema(XmlSchema s)
        {
            if (!string.IsNullOrEmpty(s.SourceUri)) {
                var ce = FindSchemaByUri(s.SourceUri);
                return ce != null && ce.Schema == s;
            }
            else
            {
                var ce = FindSchemasByNamespace(s.TargetNamespace);
                return ce != null && ce.Schema == s;
            }
        }

        public CacheEntry FindSchemaByUri(string sourceUri)
        {
            if (string.IsNullOrEmpty(sourceUri)) return null;
            return FindSchemaByUri(new Uri(sourceUri));
        }

        public CacheEntry FindSchemaByUri(Uri uri)
        {
            if (uriMap.ContainsKey(uri))
            {
                return uriMap[uri];
            }
            return null;
        }

        internal XmlSchema LoadSchema(string targetNamespace, Uri resolved)
        {
            return this.resolver.GetEntity(resolved, "", typeof(XmlSchema)) as XmlSchema;
        }

        public XmlResolver Resolver => this.resolver;

        public XmlSchemaType GetTypeInfo(XmlQualifiedName qname)
        {
            return this.FindSchemaType(qname);
        }

        public XmlSchemaType FindSchemaType(XmlQualifiedName qname)
        {
            string tns = qname.Namespace == null ? "" : qname.Namespace;
            CacheEntry e = this.FindSchemasByNamespace(tns);
            if (e == null) return null;
            while (e != null)
            {
                XmlSchema s = e.Schema;
                if (s != null)
                {
                    XmlSchemaObject so = s.SchemaTypes[qname];
                    if (so is XmlSchemaType)
                        return (XmlSchemaType)so;
                }
                e = e.Next;
            }
            return null;
        }

        public XmlSchemaElement GetElementType(XmlQualifiedName qname)
        {
            return this.FindSchemaElement(qname);
        }

        public XmlSchemaElement FindSchemaElement(XmlQualifiedName qname)
        {
            string tns = qname.Namespace == null ? "" : qname.Namespace;
            CacheEntry e = this.FindSchemasByNamespace(tns);
            if (e == null) return null;
            while (e != null)
            {
                XmlSchema s = e.Schema;
                if (s != null)
                {
                    XmlSchemaObject so = s.Elements[qname];
                    if (so is XmlSchemaElement)
                        return (XmlSchemaElement)so;
                }
                e = e.Next;
            }
            return null;
        }

        public XmlSchemaAttribute GetAttributeType(XmlQualifiedName qname)
        {
            return this.FindSchemaAttribute(qname);
        }

        public XmlSchemaAttribute FindSchemaAttribute(XmlQualifiedName qname)
        {
            string tns = qname.Namespace == null ? "" : qname.Namespace;
            CacheEntry e = this.FindSchemasByNamespace(tns);
            if (e == null) return null;
            while (e != null)
            {
                XmlSchema s = e.Schema;
                if (s != null)
                {
                    XmlSchemaObject so = s.Attributes[qname];
                    if (so is XmlSchemaAttribute)
                        return (XmlSchemaAttribute)so;
                }
                e = e.Next;
            }
            return null;
        }

        public IIntellisenseList GetExpectedValues(XmlSchemaType si)
        {
            if (si == null) return null;
            XmlIntellisenseList list = new XmlIntellisenseList();
            GetExpectedValues(si, list);
            return list;
        }

        public void GetExpectedValues(XmlSchemaType si, XmlIntellisenseList list)
        {
            if (si == null) return;
            if (si is XmlSchemaSimpleType)
            {
                XmlSchemaSimpleType st = (XmlSchemaSimpleType)si;
                GetExpectedValues(st, list);
            }
            else if (si is XmlSchemaComplexType)
            {
                XmlSchemaComplexType ct = (XmlSchemaComplexType)si;
                if (ct.ContentModel is XmlSchemaComplexContent)
                {
                    XmlSchemaComplexContent cc = (XmlSchemaComplexContent)ct.ContentModel;
                    if (cc.Content is XmlSchemaComplexContentExtension)
                    {
                        XmlSchemaComplexContentExtension ce = (XmlSchemaComplexContentExtension)cc.Content;
                        GetExpectedValues(GetTypeInfo(ce.BaseTypeName), list);
                    }
                    else if (cc.Content is XmlSchemaComplexContentRestriction)
                    {
                        XmlSchemaComplexContentRestriction cr = (XmlSchemaComplexContentRestriction)cc.Content;
                        GetExpectedValues(GetTypeInfo(cr.BaseTypeName), list);
                    }
                }
                else if (ct.ContentModel is XmlSchemaSimpleContent)
                {
                    XmlSchemaSimpleContent sc = (XmlSchemaSimpleContent)ct.ContentModel;
                    if (sc.Content is XmlSchemaSimpleContentExtension)
                    {
                        XmlSchemaSimpleContentExtension ce = (XmlSchemaSimpleContentExtension)sc.Content;
                        GetExpectedValues(GetTypeInfo(ce.BaseTypeName), list);
                    }
                    else if (sc.Content is XmlSchemaSimpleContentRestriction)
                    {
                        XmlSchemaSimpleContentRestriction cr = (XmlSchemaSimpleContentRestriction)sc.Content;
                        GetExpectedValues(GetTypeInfo(cr.BaseTypeName), list);
                    }
                }
            }
            return;
        }

        void GetExpectedValues(XmlSchemaSimpleType st, XmlIntellisenseList list)
        {
            if (st == null) return;
            if (st.Datatype != null)
            {
                switch (st.Datatype.TypeCode)
                {
                    case XmlTypeCode.Language:
                        foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
                        {
                            list.Add(ci.Name, null, ci.DisplayName);
                        }
                        list.Sort();
                        break;
                    case XmlTypeCode.Boolean:
                        list.Add("0", null, null);
                        list.Add("1", null, null);
                        list.Add("true", null, null);
                        list.Add("false", null, null);
                        break;
                }
            }

            if (st.Content is XmlSchemaSimpleTypeList)
            {
                XmlSchemaSimpleTypeList ce = (XmlSchemaSimpleTypeList)st.Content;
                GetExpectedValues(ce.ItemType, list);
            }
            else if (st.Content is XmlSchemaSimpleTypeUnion)
            {
                XmlSchemaSimpleTypeUnion cr = (XmlSchemaSimpleTypeUnion)st.Content;
                if (cr.BaseMemberTypes != null)
                {
                    foreach (XmlSchemaSimpleType bt in cr.BaseMemberTypes)
                    {
                        GetExpectedValues(bt, list);
                    }
                }
            }
            else if (st.Content is XmlSchemaSimpleTypeRestriction)
            {
                XmlSchemaSimpleTypeRestriction cr = (XmlSchemaSimpleTypeRestriction)st.Content;
                GetExpectedValues(FindSchemaType(cr.BaseTypeName), list);
                foreach (XmlSchemaFacet f in cr.Facets)
                {
                    if (f is XmlSchemaEnumerationFacet)
                    {
                        XmlSchemaEnumerationFacet ef = (XmlSchemaEnumerationFacet)f;
                        list.Add(ef.Value, null, GetAnnotation(ef, SchemaCache.AnnotationNode.Tooltip, null));
                    }
                }
            }
            return;
        }

        public enum AnnotationNode { Default, Suggestion, Tooltip }

        public static IEnumerable<XmlSchemaDocumentation> GetDocumentation(XmlSchemaAnnotated a, string language)
        {
            XmlSchemaAnnotation ann = a.Annotation;
            if (ann != null)
            {
                foreach (XmlSchemaObject o in ann.Items)
                {
                    // search for xs:documentation nodes
                    XmlSchemaDocumentation doc = o as XmlSchemaDocumentation;
                    if (doc != null)
                    {
                        if (string.IsNullOrEmpty(language) || doc.Language == language)
                        {
                            yield return doc;
                        }
                    }
                }
            }
        }

        public static string GetAnnotation(XmlSchemaAnnotated a, AnnotationNode node, string language)
        {
            XmlSchemaAnnotation ann = a.Annotation;
            if (ann == null) return null;
            string filter = node.ToString().ToLowerInvariant();
            if (filter == "default") filter = "";
            string result = GetMarkup(ann, filter, language);
            if (!string.IsNullOrEmpty(result)) return result;
            return GetMarkup(ann, null, language);
        }

        static string GetMarkup(XmlSchemaAnnotation ann, string filter, string language)
        {
            StringBuilder sb = new StringBuilder();
            foreach (XmlSchemaObject o in ann.Items)
            {
                // for xs:documentation nodes
                if (o is XmlSchemaDocumentation)
                {
                    XmlSchemaDocumentation d = (XmlSchemaDocumentation)o;
                    if (string.IsNullOrEmpty(language) || d.Language == language)
                    {
                        XmlNode[] ma = d.Markup;
                        if (ma != null)
                        {
                            // if we only have the xs:documentation node (no markup)...
                            foreach (XmlNode n in ma)
                            {
                                if (!string.IsNullOrEmpty(filter))
                                {
                                    if (string.Compare(filter, n.LocalName, StringComparison.InvariantCultureIgnoreCase) == 0)
                                    {
                                        sb.Append(n.InnerText);
                                    }
                                }
                                else
                                {
                                    string text = n.InnerText;
                                    if (sb.Length > 0 && !EndsWithNewLine(sb) && !StartsWithNewLine(text))
                                    {
                                        sb.AppendLine();
                                    }
                                    sb.Append(text);
                                }
                            }
                        }
                    }
                }
            }
            return sb.ToString();
        }

        static bool EndsWithNewLine(StringBuilder sb)
        {
            int len = sb.Length;
            if (len > 0 && sb[len - 1] == '\n') return true;
            return false;
        }

        static bool StartsWithNewLine(String sb)
        {
            int len = sb.Length;
            if (len > 0 && sb[0] == '\n') return true;
            if (len > 1 && sb[0] == '\r'  && sb[1] == '\n') return true;
            return false;
        }

        #region IXmlSerializable Members

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader r)
        {
            this.Clear();
            if (r.IsEmptyElement) return;
            while (r.Read() && r.NodeType != XmlNodeType.EndElement)
            {
                if (r.NodeType == XmlNodeType.Element)
                {
                    string nsuri = r.GetAttribute("nsuri");
                    bool disabled = false;
                    string s = r.GetAttribute("disabled");
                    if (!string.IsNullOrEmpty(s))
                    {
                        bool.TryParse(s, out disabled);
                    }
                    string filename = r.ReadString();
                    this.Add(nsuri, pfn.GetAbsoluteFileName(filename), disabled);
                }
            }
        }

        public void WriteXml(XmlWriter w)
        {
            try
            {
                foreach (CacheEntry e in this.GetSchemas())
                {
                    string path = pfn.GetPersistentFileName(e.Location);
                    if (path != null)
                    {
                        w.WriteStartElement("Schema");
                        string uri = e.TargetNamespace;
                        if (uri == null) uri = "";
                        w.WriteAttributeString("nsuri", uri);
                        if (e.Disabled)
                        {
                            w.WriteAttributeString("disabled", "true");
                        }
                        w.WriteString(path);
                        w.WriteEndElement();
                    }
                }
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
        }

        public IEnumerable<XmlSchemaAttribute> GetIdAttributes()
        {
            foreach (CacheEntry ce in this.GetSchemas())
            {
                if (ce.Schema == null) continue;
                foreach (var so in ce.Schema.Items)
                {
                    if (so is XmlSchemaAttribute sa)
                    {
                        if (sa.SchemaTypeName.Name == "ID")
                        {
                            yield return sa;
                        }
                    }
                    else if (so is XmlSchemaElement se)
                    {
                        if (se.SchemaType is XmlSchemaComplexType ct)
                        {
                            if (ct.Attributes != null)
                            {
                                foreach (var a in ct.Attributes)
                                {
                                    if (a is XmlSchemaAttribute sa2)
                                    {
                                        if (sa2.SchemaTypeName.Name == "ID")
                                        {
                                            yield return sa2;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal IEnumerable<XmlSchemaElement> GetPossibleTopLevelElements()
        {
            if (globalElementCache == null)
            {
                globalElementCache = new List<XmlSchemaElement>();
                foreach (var entry in this.GetSchemas())
                {
                    if (entry.Schema == null)
                    {
                        if (entry.Location != null)
                        {
                            entry.Schema = this.LoadSchema(entry.Location);
                        }
                    }
                    if (entry.Schema != null)
                    {
                        XmlSchemaSet set = new XmlSchemaSet();
                        set.Add(entry.Schema);

                        try
                        {
                            set.Compile();
                            foreach (var o in set.GlobalElements.Values)
                            {
                                if (o is XmlSchemaElement e)
                                {
                                    globalElementCache.Add(e);
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            return globalElementCache;
        }

        #endregion
    }

    public class SchemaResolver : XmlProxyResolver
    {
        SchemaCache cache;
        ValidationEventHandler handler;

        public SchemaResolver(IServiceProvider site, SchemaCache cache) : base(site)
        {
            this.cache = cache;
        }

        public ValidationEventHandler Handler
        {
            get { return handler; }
            set { handler = value; }
        }

        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            CacheEntry ce = cache.FindSchemaByUri(absoluteUri);
            if (ce != null && ce.HasUpToDateSchema) return ce.Schema;

            XmlSchema s = null;

            if (ofObjectToReturn == typeof(XmlSchema))
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ValidationEventHandler += handler;
                settings.XmlResolver = this;
                settings.DtdProcessing = DtdProcessing.Ignore;
                XmlReader r = XmlReader.Create(absoluteUri.AbsoluteUri, settings);
                if (r != null)
                {
                    s = XmlSchema.Read(r, handler);
                    if (s != null)
                    {
                        s.SourceUri = absoluteUri.AbsoluteUri;
                        if (ce != null)
                        {
                            ce.Schema = s;
                        }
                        else
                        {
                            cache.Add(s);
                        }
                        return s;
                    }
                }
            }
            else
            {
                return base.GetEntity(absoluteUri, role, typeof(Stream)) as Stream;
            }

            return null;
        }
    }
}
