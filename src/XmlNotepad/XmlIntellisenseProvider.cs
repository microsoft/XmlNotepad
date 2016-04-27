using System;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace XmlNotepad {
    public class XmlIntellisenseProvider : IIntellisenseProvider, IDisposable {
        Hashtable typeCache = new Hashtable();
        XmlCache model;
        XmlTreeNode node;
        XmlNode xn;
        Checker checker;
        ISite site;

        const string vsIntellisense = "http://schemas.microsoft.com/Visual-Studio-Intellisense";

        public XmlIntellisenseProvider(XmlCache model, ISite site) {
            this.model = model;
            this.site = site;
        }

        public virtual Uri BaseUri {
            get { return this.model != null ? this.model.Location : null;  }
        }

        public virtual bool IsNameEditable { get { return true; } }

        public virtual bool IsValueEditable { get { return true; } }

        public void SetContextNode(TreeNode node) {
            this.ContextNode = node;
            OnContextChanged();
        }

        public TreeNode ContextNode {
            get { return node; }
            set { node = value as XmlTreeNode; }
        }

        public virtual void OnContextChanged() {
            this.checker = null;
           
            // Get intellisense for elements and attributes
            if (this.node.NodeType == XmlNodeType.Element ||
                this.node.NodeType == XmlNodeType.Attribute ||
                this.node.NodeType == XmlNodeType.Text ||
                this.node.NodeType == XmlNodeType.CDATA) {
                XmlTreeNode elementNode = GetClosestElement(this.node);
                if (elementNode != null && elementNode.NodeType == XmlNodeType.Element) {
                    this.xn = elementNode.Node;
                    if (xn is XmlElement) {
                        this.checker = new Checker((XmlElement)xn,
                            elementNode == this.node.Parent ? IntellisensePosition.FirstChild :
                            (this.node.Node == null ? IntellisensePosition.AfterNode : IntellisensePosition.OnNode)
                            );
                        this.checker.ValidateContext(model);
                    }
                }
            }
        }

        static XmlTreeNode GetClosestElement(XmlTreeNode treeNode) {
            XmlTreeNode element = treeNode.Parent as XmlTreeNode;
            if (treeNode.Parent != null) {
                foreach (XmlTreeNode child in treeNode.Parent.Nodes) {
                    if (child.Node != null && child.NodeType == XmlNodeType.Element) {
                        element = child;
                    }
                    if (child == treeNode)
                        break;
                }
            }
            return element;
        }

        public virtual XmlSchemaType GetSchemaType() {
            XmlSchemaInfo info = GetSchemaInfo();
            return info != null ? info.SchemaType : null;
        }

        XmlSchemaInfo GetSchemaInfo() {
            XmlTreeNode tn = node;
            if (tn.NodeType == XmlNodeType.Text ||
                tn.NodeType == XmlNodeType.CDATA) {
                tn = (XmlTreeNode)tn.Parent;
            }
            if (tn == null) return null;
            XmlNode xn = tn.Node;
            if (xn != null && model != null) {
                XmlSchemaInfo info = model.GetTypeInfo(xn);
                return info;
            }
            return null;
        }

        public virtual string GetDefaultValue() {
            XmlSchemaInfo info = GetSchemaInfo();
            if (info != null) {
                if (info.SchemaAttribute != null) {
                    return info.SchemaAttribute.DefaultValue;
                } else if (info.SchemaElement != null) {
                    return info.SchemaElement.DefaultValue;
                }   
            }
            return null;
        }

        public virtual IIntellisenseList GetExpectedValues() {
            XmlSchemaType type = GetSchemaType();
            if (type != null) {
                return model.SchemaCache.GetExpectedValues(type);
            }
            if (node != null && node.Node != null) {
                XmlNode xn = node.Node;
                if (xn.NodeType == XmlNodeType.Attribute && xn.NamespaceURI == "http://www.w3.org/2000/xmlns/") {
                    XmlNode parent = ((XmlTreeNode)node.Parent).Node;
                    return GetNamespaceList(parent);
                }
            }            
            return null;
        }

        public XmlIntellisenseList GetNamespaceList(XmlNode node) {
            XmlIntellisenseList list = new XmlIntellisenseList();
            list.IsOpen = true;

            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach (XmlNode a in node.SelectNodes("namespace::*")) {
                string tns = a.Value;
                list.Add(tns, null);
            }
            foreach (CacheEntry ce in this.model.SchemaCache.GetSchemas()) {
                if (ce.Schema == null) continue;
                string tns = ce.Schema.TargetNamespace;
                list.Add(tns, null);
            }
            list.Add("http://www.w3.org/2001/XMLSchema-instance", null);
            list.Add("http://www.w3.org/2001/XMLSchema", null);
            list.Sort();

            return list;
        }

        public virtual IIntellisenseList GetExpectedNames() {
            if (checker != null) {
                XmlIntellisenseList list = new XmlIntellisenseList();
                if (node.NodeType == XmlNodeType.Attribute) {
                    XmlSchemaAttribute[] expected = checker.GetExpectedAttributes();
                    if (expected != null) {
                        foreach (XmlSchemaAttribute a in expected) {
                            list.Add(GetQualifiedName(a), SchemaCache.GetAnnotation(a, SchemaCache.AnnotationNode.Tooltip, null));
                        }
                    }
                } else {
                    XmlSchemaParticle[] particles = checker.GetExpectedParticles();
                    if (particles != null) {
                        foreach (XmlSchemaParticle p in particles) {
                            if (p is XmlSchemaElement) {
                                list.Add(GetQualifiedName((XmlSchemaElement)p), SchemaCache.GetAnnotation(p, SchemaCache.AnnotationNode.Tooltip, null));
                            } else {
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

        static string GetUnhandledAttribute(XmlAttribute[] attributes, string localName, string nsuri) {
            if (attributes != null) {
                foreach (XmlAttribute a in attributes) {
                    if (a.LocalName == localName && a.NamespaceURI == nsuri) {
                        return a.Value;
                    }
                }
            }
            return null;
        }

        public string GetIntellisenseAttribute(string name) {
            string value = null;
            XmlSchemaInfo info = GetSchemaInfo();
            if (info != null) {
                if (info.SchemaElement != null) {
                    value = GetUnhandledAttribute(info.SchemaElement.UnhandledAttributes, name, vsIntellisense);
                }
                if (info.SchemaAttribute != null) {
                    value = GetUnhandledAttribute(info.SchemaAttribute.UnhandledAttributes, name, vsIntellisense);
                }
                if (value == null && info.SchemaType != null) {
                    value = GetUnhandledAttribute(info.SchemaType.UnhandledAttributes, name, vsIntellisense);
                }
            }
            return value;
        }

        public virtual IXmlBuilder Builder {
            get {
                string typeName = GetIntellisenseAttribute("builder");
                if (!string.IsNullOrEmpty(typeName)) {
                    IXmlBuilder builder = ConstructType(typeName) as IXmlBuilder;
                    if (builder != null) builder.Owner = this;
                    return builder;
                }

                // Some default builders.
                XmlSchemaType type = GetSchemaType();
                if (type != null) {
                    switch (type.TypeCode) {
                        case XmlTypeCode.AnyUri:
                            IXmlBuilder builder = ConstructType("XmlNotepad.UriBuilder") as IXmlBuilder;
                            if (builder != null) builder.Owner = this;
                            return builder;
                    }
                }
                return null;
            }
        }

        public virtual IXmlEditor Editor {
            get {
                string typeName = GetIntellisenseAttribute("editor");
                if (!string.IsNullOrEmpty(typeName)) {
                    return ConstructType(typeName) as IXmlEditor;
                }

                // Some default editors.
                XmlSchemaType type = GetSchemaType();
                if (type != null) {
                    switch (type.TypeCode) {
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

        object ConstructType(string typeName) {
            // Cache the objects so they can preserve user state.
            if (typeCache.ContainsKey(typeName))
                return typeCache[typeName];

            Type t = Type.GetType(typeName);
            if (t == null) {
                // perhaps there's an associated assembly we need to load.
                string assembly = GetIntellisenseAttribute("assembly");
                if (!string.IsNullOrEmpty(assembly)) {
                    try {
                        string[] parts = assembly.Split(',');
                        string newdir = Path.GetDirectoryName(this.GetType().Assembly.Location);
                        Uri uri = new Uri(newdir+"\\");
                        Uri resolved =new Uri(uri, parts[0]+".dll");
                        Assembly a;
                        if (resolved.IsFile && File.Exists(resolved.LocalPath)) {
                            a = Assembly.LoadFrom(resolved.LocalPath);
                        } else {
                            a = Assembly.Load(assembly);
                        }
                        if (a != null) {
                            t = a.GetType(typeName);
                        }
                    } catch (Exception e) {
                        System.Windows.Forms.MessageBox.Show(string.Format(SR.AssemblyLoadError, assembly, e.Message),
                        SR.AssemblyLoadCaption, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    }
                }
            }
            if (t != null) {
                ConstructorInfo ci = t.GetConstructor(new Type[0]);
                if (ci != null) {
                    object result = ci.Invoke(new Object[0]);
                    if (result != null) {
                        typeCache[typeName] = result;
                        return result;
                    }
                }
            }
            return null;
        }

        public string GetQualifiedName(XmlSchemaAttribute a) {
            string name = a.Name;
            string nsuri = null;
            if (a.QualifiedName != null) {
                name = a.QualifiedName.Name;
                nsuri = a.QualifiedName.Namespace;
            } else if (a.RefName != null) {
                name = a.QualifiedName.Name;
                nsuri = a.QualifiedName.Namespace;
            } else {
                nsuri = GetSchema(a).TargetNamespace;
            }
            if (!string.IsNullOrEmpty(nsuri) && this.xn != null) {
                string prefix = this.xn.GetPrefixOfNamespace(nsuri);
                if (!string.IsNullOrEmpty(prefix)) {
                    return prefix + ":" + name;
                }
            }
            return name;
        }

        public string GetQualifiedName(XmlSchemaElement e) {
            string name = e.Name;
            string nsuri = null;
            if (e.QualifiedName != null) {
                name = e.QualifiedName.Name;
                nsuri = e.QualifiedName.Namespace;
            } else if (e.RefName != null) {
                name = e.QualifiedName.Name;
                nsuri = e.QualifiedName.Namespace;
            } else {
                nsuri = GetSchema(e).TargetNamespace;
            }
            if (!string.IsNullOrEmpty(nsuri) && this.xn != null) {
                string prefix = this.xn.GetPrefixOfNamespace(nsuri);
                if (!string.IsNullOrEmpty(prefix)) {
                    return prefix + ":" + name;
                }
            }
            return name;
        }

        XmlSchema GetSchema(XmlSchemaObject o) {
            if (o == null) return null;
            if (o is XmlSchema) return (XmlSchema)o;
            return GetSchema(o.Parent);
        }

        ~XmlIntellisenseProvider() {
            Dispose(false);
        }

        #region IDisposable Members

        public void Dispose() {
            Dispose(true);
        }

        #endregion

        protected virtual void Dispose(bool disposing) {
            if (this.typeCache != null) {
                foreach (object value in this.typeCache.Values) {
                    IDisposable d = value as IDisposable;
                    if (d != null) d.Dispose();
                }
                this.typeCache = null;
            }
        }
    }

    public class XmlIntellisenseList : IIntellisenseList {
        class Entry {
            public string name;
            public string tooltip;
            public Entry(string name, string tip) {
                this.name = name;
                this.tooltip = string.IsNullOrEmpty(tip) ? null : tip;
            }
        }
        class EntryComparer : IComparer<Entry> {
            public int Compare(Entry x, Entry y) {
                return string.Compare(x.name, y.name);
            }
        }
        Dictionary<string, Entry> unique = new Dictionary<string, Entry>();
        List<Entry> items = new List<Entry>();
        bool isOpen;

        public XmlIntellisenseList() {
        }

        public bool IsOpen {
            get { return this.isOpen; }
            set { this.isOpen = value; }
        }

        public void Add(string s, string tip) {
            if (!string.IsNullOrEmpty(s) && !unique.ContainsKey(s)) {
                Entry e = new Entry(s, tip);
                unique[s] = e;
                items.Add(e);
            }
        }

        public void Sort() {
            items.Sort(new EntryComparer());
        }

        public int Count {
            get { return items.Count; }
        }

        public string GetValue(int i) {
            return items[i].name;
        }

        public string GetTooltip(int i) {
            return items[i].tooltip;
        }

    }


}
