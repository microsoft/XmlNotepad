using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Globalization;
using System.Xml.Serialization;


namespace XmlNotepad
{
    /// <summary>
    /// This class represents a cached schema which may or may not be loaded yet.
    /// This allows delay loading of schemas.
    /// </summary>
    public class CacheEntry {
        string targetNamespace;
        Uri location;
        XmlSchema schema;
        bool disabled;
        string fileName;
        DateTime lastModified;
        CacheEntry next; // entry with same targetNamespace;

        public string TargetNamespace {
            get { return targetNamespace; }
            set { targetNamespace = value; }
        }

        public Uri Location {
            get { return location; }
            set { location = value; 
                schema = null;
                if (location.IsFile) {
                    fileName = location.LocalPath;
                }
            }
        }

        public bool HasUpToDateSchema {
            get {
                if (schema == null) return false;
                if (fileName != null) {
                    DateTime lastWriteTime = File.GetLastWriteTime(fileName);
                    if (lastWriteTime > this.lastModified) {
                        return false;
                    }
                }
                return true;
            }
        }

        public XmlSchema Schema {
            get { return schema; }
            set {
                if (schema != value) {
                    schema = value;
                    if (fileName != null) {
                        this.lastModified = File.GetLastWriteTime(fileName);
                    }
                }
            }
        }

        public CacheEntry Next {
            get { return next; }
            set { next = value; }
        }

        public bool Disabled {
            get { return disabled; }
            set { disabled = value; }
        }

        public CacheEntry FindByUri(Uri uri) {
            CacheEntry e = this;
            while (e != null) {
                if (e.location == uri) {
                    return e;
                }
                e = e.next;
            }
            return null;
        }

        // Remove the given cache entry and return the new head of the linked list.
        public CacheEntry RemoveUri(Uri uri) {
            CacheEntry e = this;
            CacheEntry previous = null;
            while (e != null) {
                if (e.location == uri) {
                    if (previous == null) {
                        return e.next; // return new head
                    }
                    previous.next = e.next; //unlink it
                    return this; // head is unchanged.
                }
                previous = e;
                e = e.next;
            }
            return this;
        }

        public void Add(CacheEntry newEntry) {
            CacheEntry e = this;
            while (e != null) {
                if (e == newEntry) {
                    return;
                }
                if (e.location == newEntry.location) {
                    e.schema = newEntry.schema;
                    e.lastModified = newEntry.lastModified;
                    e.disabled = newEntry.disabled;
                    return;
                }
                if (e.next == null) {
                    e.next = newEntry;
                    break;
                }
                e = e.next;
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
        private IServiceProvider site;
        // targetNamespace -> CacheEntry
        Dictionary<string, CacheEntry> namespaceMap = new Dictionary<string, CacheEntry>();
        // sourceUri -> CacheEntry
        Dictionary<Uri, CacheEntry> uriMap = new Dictionary<Uri, CacheEntry>();
        PersistentFileNames pfn = new PersistentFileNames();

        public SchemaCache(IServiceProvider site) {
            this.site = site;
        }

        void FireOnChanged()
        {
            if (null!=this.Changed  )
            {
                this.Changed(this, EventArgs.Empty);
            }
        }

        public void Clear() {
            namespaceMap.Clear();
            uriMap.Clear();
        }

        public IList<CacheEntry> GetSchemas() {
            List<CacheEntry> list = new List<CacheEntry>();
            foreach (CacheEntry ce in namespaceMap.Values) {
                CacheEntry e = ce;
                while (e != null) {
                    list.Add(e);
                    e = e.Next;
                }
            }
            return list;
        }

        public CacheEntry Add(string nsuri, Uri uri, bool disabled) {
            if (nsuri == null) nsuri = "";

            CacheEntry existing = null;
            CacheEntry e = null;

            if (namespaceMap.ContainsKey(nsuri)) {
                existing = namespaceMap[nsuri];
                e = existing.FindByUri(uri);                
            }
            if (e == null) {
                e = new CacheEntry();
                e.Location = uri;
                e.TargetNamespace = nsuri;

                if (existing != null) {
                    existing.Add(e);
                } else {
                    namespaceMap[nsuri] = e;
                }
            }
            e.Disabled = disabled;
            if (uriMap.ContainsKey(uri)) {
                CacheEntry oe = (CacheEntry)uriMap[uri];
                if (oe != e) {
                    // target namespace must have changed!
                    nsuri = oe.TargetNamespace;
                    if (nsuri == null) nsuri = "";
                    if (namespaceMap.ContainsKey(nsuri)) {
                        namespaceMap.Remove(nsuri);
                    }
                }
            }
            uriMap[uri] = e;
            this.FireOnChanged();

            return e;
        }

        public CacheEntry Add(XmlSchema s) {
            CacheEntry e = Add(s.TargetNamespace, new Uri(s.SourceUri), false);
            if (e.Schema != null) {
                e.Schema = s;
            }
            return e;
        }

        public void Remove(CacheEntry ce) {
            Remove(ce.Location);
        }

        public void Remove(Uri uri) {
            if (uriMap.ContainsKey(uri)) {
                CacheEntry e = uriMap[uri];
                uriMap.Remove(uri);
                string key = e.TargetNamespace;
                if (namespaceMap.ContainsKey(key)) {
                    CacheEntry head = namespaceMap[key];
                    CacheEntry newHead = head.RemoveUri(uri);
                    if (newHead == null) {
                        namespaceMap.Remove(key);
                    } else if (newHead != head) {
                        namespaceMap[key] = newHead;
                    }
                    this.FireOnChanged();
                }
            }
        }

        public void Remove(string filename) {
            Uri uri = new Uri(filename);
            Remove(uri);
        }

        public void Remove(XmlSchema s) {
            Remove(s.SourceUri);
        }        

        public CacheEntry FindSchemasByNamespace(string targetNamespace) {
            if (namespaceMap.ContainsKey(targetNamespace)) {
                return namespaceMap[targetNamespace];                
            }
            return null;
        }

        public CacheEntry FindSchemaByUri(string sourceUri) {
            if (string.IsNullOrEmpty(sourceUri)) return null;
            return FindSchemaByUri(new Uri(sourceUri));
        }

        public CacheEntry FindSchemaByUri(Uri uri) {
            if (uriMap.ContainsKey(uri)) {
                return uriMap[uri];
            }
            return null;
        }

        public XmlResolver Resolver {
            get {
                return new SchemaResolver(this, site);
            }
        }

        public XmlSchemaType GetTypeInfo(XmlQualifiedName qname)
        {
            return this.FindSchemaType(qname);
        }

        public XmlSchemaType FindSchemaType(XmlQualifiedName qname) {
            string tns = qname.Namespace == null ? "" : qname.Namespace;
            CacheEntry e = this.FindSchemasByNamespace(tns);
            if (e == null) return null;
            while (e != null) {
                XmlSchema s = e.Schema;
                if (s != null) {
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

        public IIntellisenseList GetExpectedValues(XmlSchemaType si) {
            if (si == null) return null;
            XmlIntellisenseList list = new XmlIntellisenseList();
            GetExpectedValues(si, list);
            return list;
        }

        public void GetExpectedValues(XmlSchemaType si, XmlIntellisenseList list) {
            if (si == null) return;
            if (si is XmlSchemaSimpleType) {
                XmlSchemaSimpleType st = (XmlSchemaSimpleType)si;
                GetExpectedValues(st, list);
            } else if (si is XmlSchemaComplexType) {
                XmlSchemaComplexType ct = (XmlSchemaComplexType)si;
                if (ct.ContentModel is XmlSchemaComplexContent) {
                    XmlSchemaComplexContent cc = (XmlSchemaComplexContent)ct.ContentModel;
                    if (cc.Content is XmlSchemaComplexContentExtension) {
                        XmlSchemaComplexContentExtension ce = (XmlSchemaComplexContentExtension)cc.Content;
                        GetExpectedValues(GetTypeInfo(ce.BaseTypeName), list);
                    } else if (cc.Content is XmlSchemaComplexContentRestriction) {
                        XmlSchemaComplexContentRestriction cr = (XmlSchemaComplexContentRestriction)cc.Content;
                        GetExpectedValues(GetTypeInfo(cr.BaseTypeName), list);
                    }
                } else if (ct.ContentModel is XmlSchemaSimpleContent) {
                    XmlSchemaSimpleContent sc = (XmlSchemaSimpleContent)ct.ContentModel;
                    if (sc.Content is XmlSchemaSimpleContentExtension) {
                        XmlSchemaSimpleContentExtension ce = (XmlSchemaSimpleContentExtension)sc.Content;
                        GetExpectedValues(GetTypeInfo(ce.BaseTypeName), list);
                    } else if (sc.Content is XmlSchemaSimpleContentRestriction) {
                        XmlSchemaSimpleContentRestriction cr = (XmlSchemaSimpleContentRestriction)sc.Content;
                        GetExpectedValues(GetTypeInfo(cr.BaseTypeName), list);
                    }
                }
            }
            return;
        }

        void GetExpectedValues(XmlSchemaSimpleType st, XmlIntellisenseList list) {
            if (st == null) return;
            if (st.Datatype != null) {
                switch (st.Datatype.TypeCode) {
                    case XmlTypeCode.Language:
                        foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures)) {
                            list.Add(ci.Name, ci.DisplayName);
                        }
                        list.Sort();
                        break;
                    case XmlTypeCode.Boolean:
                        list.Add("0", null);
                        list.Add("1", null);
                        list.Add("true", null);
                        list.Add("false", null);
                        break;
                }
            }

            if (st.Content is XmlSchemaSimpleTypeList) {
                XmlSchemaSimpleTypeList ce = (XmlSchemaSimpleTypeList)st.Content;
                GetExpectedValues(ce.ItemType, list);
            } else if (st.Content is XmlSchemaSimpleTypeUnion) {
                XmlSchemaSimpleTypeUnion cr = (XmlSchemaSimpleTypeUnion)st.Content;
                if (cr.BaseMemberTypes != null) {
                    foreach (XmlSchemaSimpleType bt in cr.BaseMemberTypes) {
                        GetExpectedValues(bt, list);
                    }
                }
            } else if (st.Content is XmlSchemaSimpleTypeRestriction) {
                XmlSchemaSimpleTypeRestriction cr = (XmlSchemaSimpleTypeRestriction)st.Content;
                GetExpectedValues(FindSchemaType(cr.BaseTypeName), list);
                foreach (XmlSchemaFacet f in cr.Facets) {
                    if (f is XmlSchemaEnumerationFacet) {
                        XmlSchemaEnumerationFacet ef = (XmlSchemaEnumerationFacet)f;
                        list.Add(ef.Value, GetAnnotation(ef, SchemaCache.AnnotationNode.Tooltip, null));
                    }
                }                
            }
            return;
        }

        public enum AnnotationNode { Default, Suggestion, Tooltip  }

        public static XmlSchemaDocumentation GetDocumentation(XmlSchemaAnnotated a, string language)
        {
            XmlSchemaAnnotation ann = a.Annotation;
            if (ann == null) return null;
            foreach (XmlSchemaObject o in ann.Items) {
                // search for xs:documentation nodes
                XmlSchemaDocumentation doc = o as XmlSchemaDocumentation;
                if (doc != null)
                {
                    if (string.IsNullOrEmpty(language) || doc.Language == language)
                    {
                        return doc;
                    }
                }
            }
            return null;
        }

        public static string GetAnnotation(XmlSchemaAnnotated a, AnnotationNode node, string language) {
            XmlSchemaAnnotation ann = a.Annotation;
            if (ann == null) return null;
            string filter = node.ToString().ToLowerInvariant();
            if (filter == "default") filter = "";
            string result = GetMarkup(ann, filter, language);
            if (!string.IsNullOrEmpty(result)) return result;
            return GetMarkup(ann, null, language);
        }

        static string GetMarkup(XmlSchemaAnnotation ann, string filter, string language) {
            StringBuilder sb = new StringBuilder();
            foreach (XmlSchemaObject o in ann.Items) {
                // for xs:documentation nodes
                if (o is XmlSchemaDocumentation) {
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
                                    sb.Append(n.InnerText);
                                }
                            }
                        }
                    }
                }
            }
            return sb.ToString();
        }

        #region IXmlSerializable Members

        public XmlSchema GetSchema() {
            return null;
        }

        public void ReadXml(XmlReader r) {
            this.Clear();
            if (r.IsEmptyElement) return;
            while (r.Read() && r.NodeType != XmlNodeType.EndElement) {
                if (r.NodeType == XmlNodeType.Element) {
                    string nsuri = r.GetAttribute("nsuri");
                    bool disabled = false;
                    string s = r.GetAttribute("disabled");
                    if (!string.IsNullOrEmpty(s)) {
                        bool.TryParse(s, out disabled);
                    }
                    string filename = r.ReadString();
                    this.Add(nsuri, pfn.GetAbsoluteFilename(filename), disabled);
                }
            }
        }

        public void WriteXml(XmlWriter w) {
            try {
                foreach (CacheEntry e in this.GetSchemas()) {
                    string path = pfn.GetPersistentFileName(e.Location);
                    if (path != null) {
                        w.WriteStartElement("Schema");
                        string uri = e.TargetNamespace;
                        if (uri == null) uri = "";
                        w.WriteAttributeString("nsuri", uri);
                        if (e.Disabled) {
                            w.WriteAttributeString("disabled", "true");
                        }
                        w.WriteString(path);
                        w.WriteEndElement();
                    }
                }
            } catch (Exception x) {
                Console.WriteLine(x.Message);
            }
        }

        #endregion
    }

    public class SchemaResolver : XmlProxyResolver {
        SchemaCache cache;
        ValidationEventHandler handler;

        public SchemaResolver(SchemaCache cache, IServiceProvider site) : base(site) {
            this.cache = cache;
        }

        public ValidationEventHandler Handler {
            get { return handler; }
            set { handler = value; }
        }

        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn) {
            CacheEntry ce = cache.FindSchemaByUri(absoluteUri);
            if (ce != null && ce.HasUpToDateSchema) return ce.Schema;

            XmlSchema s = null;

            if (ofObjectToReturn == typeof(XmlSchema))
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ValidationEventHandler += handler;
                settings.XmlResolver = this;
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
