using System;
using System.Xml;
using System.Xml.Schema;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Diagnostics;

namespace XmlNotepad
{
    public class XmlIntellisenseProvider : IIntellisenseProvider, IDisposable
    {
        private readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
        private readonly XmlCache _model;
        private IXmlTreeNode _node;
        private XmlNode _xn;
        private Checker _checker;

        const string vsIntellisense = "http://schemas.microsoft.com/Visual-Studio-Intellisense";

        public XmlIntellisenseProvider(XmlCache model)
        {
            this._model = model;
        }

        public virtual Uri BaseUri
        {
            get { return this._model?.Location; }
        }

        public virtual bool IsNameEditable { get { return true; } }

        public virtual bool IsValueEditable { get { return true; } }

        public void SetContextNode(IXmlTreeNode node)
        {
            this.ContextNode = node;
            OnContextChanged();
        }

        public IXmlTreeNode ContextNode
        {
            get { return _node; }
            set { _node = value; }
        }

        public virtual void OnContextChanged()
        {
            this._checker = null;

            // Get intellisense for elements and attributes
            if (this._node.NodeType == XmlNodeType.Element ||
                this._node.NodeType == XmlNodeType.Attribute ||
                this._node.NodeType == XmlNodeType.Text ||
                this._node.NodeType == XmlNodeType.CDATA)
            {
                IXmlTreeNode elementNode = GetClosestElement(this._node);
                if (elementNode != null && elementNode.NodeType == XmlNodeType.Element)
                {
                    this._xn = elementNode.Node;
                    if (_xn is XmlElement xe)
                    {
                        this._checker = new Checker(xe,
                            elementNode == this._node.ParentNode ? IntellisensePosition.FirstChild :
                            (this._node.Node == null ? IntellisensePosition.AfterNode : IntellisensePosition.OnNode)
                            );
                        this._checker.ValidateContext(_model);
                    }
                }
            }
        }

        static IXmlTreeNode GetClosestElement(IXmlTreeNode treeNode)
        {
            IXmlTreeNode element = treeNode.ParentNode;
            if (treeNode.ParentNode != null)
            {
                foreach (IXmlTreeNode child in treeNode.ParentNode.Nodes)
                {
                    if (child.Node != null && child.NodeType == XmlNodeType.Element)
                    {
                        element = child;
                    }
                    if (child == treeNode)
                        break;
                }
            }
            return element;
        }

        public virtual XmlSchemaType GetSchemaType()
        {
            XmlSchemaInfo info = GetSchemaInfo();
            return info?.SchemaType;
        }

        XmlSchemaInfo GetSchemaInfo()
        {
            IXmlTreeNode tn = _node;
            if (tn.NodeType == XmlNodeType.Text ||
                tn.NodeType == XmlNodeType.CDATA)
            {
                tn = tn.ParentNode;
            }
            if (tn == null) return null;
            XmlNode xn = tn.Node;
            if (xn != null && _model != null)
            {
                XmlSchemaInfo info = _model.GetTypeInfo(xn);
                return info;
            }
            return null;
        }

        public virtual string GetDefaultValue()
        {
            XmlSchemaInfo info = GetSchemaInfo();
            if (info != null)
            {
                if (info.SchemaAttribute != null)
                {
                    return info.SchemaAttribute.DefaultValue;
                }
                else if (info.SchemaElement != null)
                {
                    return info.SchemaElement.DefaultValue;
                }
            }
            return null;
        }

        public virtual IIntellisenseList GetExpectedValues()
        {
            XmlSchemaType type = GetSchemaType();
            if (type != null)
            {
                return _model.SchemaCache.GetExpectedValues(type);
            }
            if (_node != null && _node.Node != null)
            {
                XmlNode xn = _node.Node;
                if (xn.NodeType == XmlNodeType.Attribute && xn.NamespaceURI == "http://www.w3.org/2000/xmlns/")
                {
                    XmlNode parent = _node.ParentNode.Node;
                    return GetNamespaceList(parent);
                }
            }
            return null;
        }

        public XmlIntellisenseList GetNamespaceList(XmlNode node)
        {
            var list = new XmlIntellisenseList() { IsOpen = true };

            foreach (XmlNode a in node.SelectNodes("namespace::*"))
            {
                string tns = a.Value;
                list.Add(tns, null, null);
            }
            foreach (CacheEntry ce in this._model.SchemaCache.GetSchemas())
            {
                if (ce.Schema == null) continue;
                string tns = ce.Schema.TargetNamespace;
                list.Add(tns, null, null);
            }
            list.Add(XmlStandardUris.XsiUri, null, null);
            list.Add(XmlStandardUris.XsdUri, null, null);
            list.Sort();

            return list;
        }

        public virtual IIntellisenseList GetExpectedNames()
        {
            var checker = this._checker;
            if (checker == null)
            {
                // fall back on the model checker so we can get top level intellisense in an empty doc.
                checker = this._model.Checker;
            }

            if (checker != null)
            {
                XmlIntellisenseList list = new XmlIntellisenseList();
                if (_node.NodeType == XmlNodeType.Attribute)
                {
                    XmlSchemaAttribute[] expected = checker.GetExpectedAttributes();
                    if (expected != null)
                    {
                        foreach (XmlSchemaAttribute a in expected)
                        {
                            list.Add(GetQualifiedName(a), a.QualifiedName.Namespace, SchemaCache.GetAnnotation(a, SchemaCache.AnnotationNode.Tooltip, null));
                        }
                    }
                }
                else
                {
                    XmlSchemaParticle[] particles = checker.GetExpectedParticles();
                    if (particles != null)
                    {
                        foreach (XmlSchemaParticle p in particles)
                        {
                            if (p is XmlSchemaElement se)
                            {
                                list.Add(GetQualifiedName(se), se.QualifiedName.Namespace, SchemaCache.GetAnnotation(p, SchemaCache.AnnotationNode.Tooltip, null));
                            }
                            else
                            {
                                // todo: expand XmlSchemaAny particles.
                                list.IsOpen = true;
                            }
                        }
                    }
                }
                list.Sort();
                return list;
            }
            return null;
        }

        static string GetUnhandledAttribute(XmlAttribute[] attributes, string localName, string nsuri)
        {
            if (attributes != null)
            {
                foreach (XmlAttribute a in attributes)
                {
                    if (a.LocalName == localName && a.NamespaceURI == nsuri)
                    {
                        return a.Value;
                    }
                }
            }
            return null;
        }

        public string GetIntellisenseAttribute(string name)
        {
            string value = null;
            XmlSchemaInfo info = GetSchemaInfo();
            if (info != null)
            {
                if (info.SchemaElement != null)
                {
                    value = GetUnhandledAttribute(info.SchemaElement.UnhandledAttributes, name, vsIntellisense);
                }
                if (info.SchemaAttribute != null)
                {
                    value = GetUnhandledAttribute(info.SchemaAttribute.UnhandledAttributes, name, vsIntellisense);
                }
                if (value == null && info.SchemaType != null)
                {
                    value = GetUnhandledAttribute(info.SchemaType.UnhandledAttributes, name, vsIntellisense);
                }
            }
            return value;
        }

        public virtual IXmlBuilder Builder
        {
            get
            {
                string typeName = GetIntellisenseAttribute("builder");
                if (!string.IsNullOrEmpty(typeName))
                {
                    IXmlBuilder builder = ConstructType(typeName) as IXmlBuilder;
                    if (builder != null) builder.Owner = this;
                    return builder;
                }

                // Some default builders.
                XmlSchemaType type = GetSchemaType();
                if (type != null)
                {
                    switch (type.TypeCode)
                    {
                        case XmlTypeCode.AnyUri:
                            IXmlBuilder builder = ConstructType("XmlNotepad.UriBuilder") as IXmlBuilder;
                            if (builder != null) builder.Owner = this;
                            return builder;
                    }
                }
                return null;
            }
        }

        public virtual IXmlEditor Editor
        {
            get
            {
                string typeName = GetIntellisenseAttribute("editor");
                if (!string.IsNullOrEmpty(typeName))
                {
                    return ConstructType(typeName) as IXmlEditor;
                }

                // Some default editors.
                XmlSchemaType type = GetSchemaType();
                if (type != null)
                {
                    switch (type.TypeCode)
                    {
                        case XmlTypeCode.Date:
                        case XmlTypeCode.DateTime:
                        case XmlTypeCode.Time:
                            IXmlEditor editor = ConstructType("XmlNotepad.DateTimeEditor") as IXmlEditor;
                            if (editor != null) editor.Owner = this;
                            return editor;
                    }
                }
                return null;
            }
        }

        public void RegisterBuilder(string name, Type t)
        {
            _typeCache[name] = t;
        }

        public void RegisterEditor(string name, Type t)
        {
            _typeCache[name] = t;
        }

        object ConstructType(string typeName)
        {
            // Cache the objects so they can preserve user state.
            if (!_typeCache.TryGetValue(typeName, out Type t))
            {
                // see if it is built in.
                t = Type.GetType(typeName);
            }

            if (t == null)
            {
                // perhaps there's an associated assembly we need to load.
                string assembly = GetIntellisenseAttribute("assembly");
                if (!string.IsNullOrEmpty(assembly))
                {
                    try
                    {
                        string[] parts = assembly.Split(',');
                        string newdir = Path.GetDirectoryName(this.GetType().Assembly.Location);
                        Uri uri = new Uri(newdir + "\\");
                        Uri resolved = new Uri(uri, parts[0] + ".dll");
                        Assembly a;
                        if (resolved.IsFile && File.Exists(resolved.LocalPath))
                        {
                            a = Assembly.LoadFrom(resolved.LocalPath);
                        }
                        else
                        {
                            a = Assembly.Load(assembly);
                        }
                        if (a != null)
                        {
                            t = a.GetType(typeName);
                        }
                    }
                    catch (Exception ex)
                    {
                        t = null;
                        Debug.WriteLine(string.Format("Error loading assembly '{0}': {1}", assembly, ex.Message));
                    }
                    // System.Windows.Forms.MessageBox.Show(string.Format(SR.AssemblyLoadError, assembly, e.Message),
                    // SR.AssemblyLoadCaption, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                }
            }
            if (t != null)
            {
                ConstructorInfo ci = t.GetConstructor(new Type[0]);
                if (ci != null)
                {
                    object result = ci.Invoke(new Object[0]);
                    if (result != null)
                    {
                        _typeCache[typeName] = t;
                        return result;
                    }
                }
            }
            return null;
        }

        public string GetQualifiedName(XmlSchemaAttribute a)
        {
            string name = a.Name;
            string nsuri;
            if (a.QualifiedName != null)
            {
                name = a.QualifiedName.Name;
                nsuri = a.QualifiedName.Namespace;
            }
            else if (a.RefName != null)
            {
                name = a.QualifiedName.Name;
                nsuri = a.QualifiedName.Namespace;
            }
            else
            {
                nsuri = GetSchema(a).TargetNamespace;
            }
            if (!string.IsNullOrEmpty(nsuri) && this._xn != null)
            {
                string prefix = this._xn.GetPrefixOfNamespace(nsuri);
                if (!string.IsNullOrEmpty(prefix))
                {
                    return prefix + ":" + name;
                }
            }
            return name;
        }

        public string GetQualifiedName(XmlSchemaElement e)
        {
            string name = e.Name;
            string nsuri;
            if (e.QualifiedName != null)
            {
                name = e.QualifiedName.Name;
                nsuri = e.QualifiedName.Namespace;
            }
            else if (e.RefName != null)
            {
                name = e.QualifiedName.Name;
                nsuri = e.QualifiedName.Namespace;
            }
            else
            {
                nsuri = GetSchema(e).TargetNamespace;
            }
            if (!string.IsNullOrEmpty(nsuri) && this._xn != null)
            {
                string prefix = this._xn.GetPrefixOfNamespace(nsuri);
                if (!string.IsNullOrEmpty(prefix))
                {
                    return prefix + ":" + name;
                }
            }
            return name;
        }

        XmlSchema GetSchema(XmlSchemaObject o)
        {
            if (o == null) return null;
            if (o is XmlSchema s) return s;
            return GetSchema(o.Parent);
        }

        ~XmlIntellisenseProvider()
        {
            Dispose(false);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            this._typeCache.Clear();
        }
    }

    public class XmlIntellisenseList : IIntellisenseList
    {
        private class Entry
        {
            public string name;
            public string ns;
            public string tooltip;
            public Entry(string name, string ns, string tip)
            {
                this.name = name;
                this.ns = ns;
                this.tooltip = string.IsNullOrEmpty(tip) ? null : tip;
            }
        }

        class EntryComparer : IComparer<Entry>
        {
            public int Compare(Entry x, Entry y)
            {
                return string.Compare(x.name, y.name);
            }
        }

        private readonly Dictionary<string, Entry> _unique = new Dictionary<string, Entry>();
        private readonly List<Entry> _items = new List<Entry>();
        private bool _isOpen;

        public XmlIntellisenseList()
        {
        }

        public bool IsOpen
        {
            get { return this._isOpen; }
            set { this._isOpen = value; }
        }

        public void Add(string s, string ns, string tip)
        {
            if (!string.IsNullOrEmpty(s) && !_unique.ContainsKey(s))
            {
                Entry e = new Entry(s, ns, tip);
                _unique[s] = e;
                _items.Add(e);
            }
        }

        public void Sort()
        {
            _items.Sort(new EntryComparer());
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public string GetName(int i)
        {
            return _items[i].name;
        }

        public string GetNamespace(int i)
        {
            return _items[i].ns;
        }

        public string GetTooltip(int i)
        {
            return _items[i].tooltip;
        }

    }


}
