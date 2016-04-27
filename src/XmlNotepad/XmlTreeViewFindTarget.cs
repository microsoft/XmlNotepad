using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Microsoft.Xml;
using System.Diagnostics;

namespace XmlNotepad {
    
    internal class XmlTreeViewFindTarget : IFindTarget {
        XmlTreeView view;
        string expression;
        FindFlags flags;
        SearchFilter filter;
        XmlNamespaceManager nsmgr;
        XmlDocument doc;
        ArrayList list;
        XmlTreeNode current;
        Regex regex;
        StringComparison comp;
        XmlNodeMatch match;
        IEditableView ev;
        int start;

        public XmlTreeViewFindTarget(XmlTreeView view) {
            this.view = view;
        }

        bool Backwards { get { return (flags & FindFlags.Backwards) != 0; } }
        bool IsXPath { get { return (flags & FindFlags.XPath) != 0; } }
        bool IsRegex { get { return (flags & FindFlags.Regex) != 0; } }
        bool MatchCase { get { return ((this.flags & FindFlags.MatchCase) != 0); } }
        bool WholeWord { get { return (this.flags & FindFlags.WholeWord) != 0; } }

        void FindNodes() {
            XmlDocument doc = this.view.Model.Document;
            if (this.doc != doc) {
                this.doc = doc;
                this.nsmgr = new XmlNamespaceManager(doc.NameTable);
            }
            this.view.Model.ModelChanged += new EventHandler<ModelChangedEventArgs>(OnModelChanged);
            
            string expr = "//node()";
            if (filter == SearchFilter.Comments) {
                expr = "//comment()";
            }
            this.regex = null;
            if (IsXPath) {
                expr = this.expression;                
            } else if (IsRegex) {
                this.regex = new Regex(this.expression);                
            }

            this.comp = MatchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

            list = new ArrayList();

            foreach (XmlNode node in this.doc.SelectNodes(expr, this.nsmgr)) {
                MatchNode(node);
                XmlElement e = node as XmlElement;
                if (!IsXPath && e != null && e.HasAttributes) {
                    foreach (XmlAttribute a in e.Attributes) {
                        MatchNode(a);
                    }
                }
            }
        }

        private void MatchNode(XmlNode node) {
            if (!IsXPath) {
                if (filter == SearchFilter.Comments && node.NodeType == XmlNodeType.Comment) {
                    MatchStrings(list, node, node.Value, false);
                } else {
                    bool namedNode = (node.NodeType == XmlNodeType.Element ||
                        node.NodeType == XmlNodeType.Attribute ||
                        node.NodeType == XmlNodeType.EntityReference ||
                        node.NodeType == XmlNodeType.ProcessingInstruction ||
                        node.NodeType == XmlNodeType.XmlDeclaration);
                    if ((filter == SearchFilter.Names && namedNode) || filter == SearchFilter.Everything) {
                        MatchStrings(list, node, node.Name, true);
                    }
                    if (filter == SearchFilter.Text || filter == SearchFilter.Everything) {
                        MatchStrings(list, node, node.Value, false);
                    }
                }
            } else {
                bool name = (node is XmlElement || node is XmlAttribute);
                int len = name ? node.Name.Length : node.Value.Length;
                list.Add(new XmlNodeMatch(node, 0, len, name));
            }
        }

        void OnModelChanged(object sender, ModelChangedEventArgs e) {
            // Then the list of matching nodes we found might now be invalid!
            this.list = null;
        }

        object[] a = new object[1];
        char[] ws = new char[] { ' ', '\t', '\n', '\r', '.', ',', ';', '!', '\'', '"', '+', '=', '-', '<', '>', '(', ')' };

        void MatchStrings(ArrayList list, XmlNode node, string value, bool name) {
            if (string.IsNullOrEmpty(value)) 
                return;

            // Normalize the newlines the same way the text editor does so that we
            // don't get off-by-one errors after newlines.
            if (value.IndexOf('\n') >= 0 && value.IndexOf("\r\n") < 0) {
                value = value.Replace("\n", "\r\n");
            }

            a[0] = value;
            object[] strings = a;

            if (value.IndexOfAny(ws) >= 0 && WholeWord) {
                strings = value.Split(ws, StringSplitOptions.RemoveEmptyEntries);
            }
            int len = this.expression.Length;
            int index = 0;

            foreach (string word in strings) {
                index = value.IndexOf(word, index);

                if (this.regex != null) {
                    foreach (Match m in this.regex.Matches(word)){
                        list.Add(new XmlNodeMatch(node, m.Index + index, m.Length, name));
                    }
                } else if (this.WholeWord) {
                    if (string.Compare(this.expression, word, comp) == 0) {
                        list.Add(new XmlNodeMatch(node, index, len, name));
                    }
                } else {
                    int j = word.IndexOf(this.expression, 0, comp);
                    while (j >= 0) {
                        list.Add(new XmlNodeMatch(node, j+index, len, name));
                        j = word.IndexOf(this.expression, j+len, comp);
                    } 
                }
            }
        }

        void CheckCurrentState(string expression, FindFlags flags, SearchFilter filter) {
            if (this.expression != expression || this.flags != flags || this.filter != filter) {
                this.expression = expression;
                this.flags = flags;
                this.filter = filter;
                this.list = null;
                this.match = null;
            }
        }

        #region IFindTarget Members

        public FindResult FindNext(string expression, FindFlags flags, SearchFilter filter) {

            CheckCurrentState(expression, flags, filter);

            if (ev != null && ev.IsEditing) {
                start = ev.SelectionStart; // remember where we were in the editor.
                ev.EndEdit(false);
            }
            this.ev = null;
            this.match = null;

            if (string.IsNullOrEmpty(expression))
                return FindResult.None;

            if (this.list == null) {
                FindNodes();
            }
            
            // In case user changed the selection since the last find.
            int pos = FindSelectedNode();
            int wrap = -1;
            bool first = true;
            bool hasSomething = this.list.Count > 0;
            while (this.list != null && hasSomething && 
                (first || pos != wrap)) {
                first = false;
                if (wrap == -1) wrap = pos;
                if (this.Backwards) {
                    pos--;
                    if (pos < 0) pos = list.Count - 1;
                } else {
                    pos++;
                    if (pos >= list.Count) pos = 0;
                }

                XmlNodeMatch m = list[pos] as XmlNodeMatch;
                XmlNode node = m.Node;
                this.match = m;

                if (node != null) {
                    this.current = this.view.FindNode(node);
                    if (this.current == this.view.SelectedNode) {
                        continue;
                    }
                    if (this.current != null) {
                        this.view.SelectedNode = this.current;
                        if (m.IsName) {
                            ev = this.view.TreeView;
                        } else {
                            ev = this.view.NodeTextView;
                        }
                        if (ev.BeginEdit(null)) {
                            ev.SelectText(m.Index, m.Length);
                        }
                    }
                }
                return FindResult.Found;
            }
            return hasSomething ? FindResult.NoMore : FindResult.None;
        }

        int FindSelectedNode() {
            // Now find where the selectedNode is in the matching list so we can start there.
            int pos = -1;
            XmlNode selectedNode = null;
            XmlTreeNode selected = this.view.SelectedNode;

            if (selected != null && list != null) {
                selectedNode = selected.Node;
                if (selectedNode != null) {
                    // I'm not using XPathNavigator.ComparePosition because it is returning XmlNodeOrder.Unknown
                    // sometimes which is not very useful!
                    foreach (XmlNodeMatch m in list) {
                        XmlNode node = m.Node;
                        if (node == selectedNode) {
                            if (m.Index >= start)
                                return ++pos; // selected node is one of the matching nodes.
                        } else if (IsNodeAfter(selectedNode, node)) {
                            break;
                        }
                        pos++;
                    }
                } 
            }
            if (Backwards) pos++;
            return pos;
        }

        // returns true if the match node comes after the selected node in document order.
        bool IsNodeAfter(XmlNode selected, XmlNode match) {
            if (FindCommonParent(ref selected, ref match)) {
                XmlNode next = selected.NextSibling;
                while (next != null) {
                    if (next == match) return true;
                    next = next.NextSibling;
                }
            }
            return false;
        }

        bool FindCommonParent(ref XmlNode a, ref XmlNode b) {
            List<XmlNode> aparents = GetParentChain(a);
            List<XmlNode> bparents = GetParentChain(b);
            // now find the lowest common node.
            for (int i = aparents.Count - 1; i >= 0; i--) {
                XmlNode p1 = aparents[i];
                for (int j = bparents.Count - 1; j >= 0; j--) {
                    XmlNode p2 = bparents[j];
                    if (p1 == p2) {
                        // Ok, found the common parent, so now return the
                        // siblings under this parent so we can calculate 
                        // relative document order of those siblings.
                        if (i + 1 < aparents.Count)
                            a = aparents[i + 1];
                        if (j + 1 < bparents.Count)
                            b = bparents[j + 1];
                        return true;
                    }
                }
            }
            return false;
        }

        List<XmlNode> GetParentChain(XmlNode p) {
            List<XmlNode> parents = new List<XmlNode>();
            p = p.SelectSingleNode("..");
            while (p != null) {
                parents.Insert(0, p);
                p = p.ParentNode;
            }
            return parents;
        }

        public Rectangle MatchRect {
            get {
                if (current != null && match != null) {
                    Rectangle bounds;
                    if (match.IsName) {
                        bounds = view.TreeView.EditorBounds;
                    } else {
                        bounds = view.NodeTextView.EditorBounds;
                    }
                    return bounds;
                }
                return Rectangle.Empty;
            }
        }

        public bool ReplaceCurrent( string replaceWith) {
            if (this.ev != null && this.ev.IsEditing && this.match != null) {
                this.list.Remove(this.match);
                this.ev.ReplaceText(this.match.Index, this.match.Length, replaceWith);
                return true;
            }
            return false;
        }

        public string Location {
            get {
                return this.GetLocation();
            }
        }

        public XmlNamespaceManager Namespaces {
            get {
                if (nsmgr == null) {
                    this.GetLocation();
                }
                return nsmgr;
            }
            set {
                this.nsmgr = value;
            }
        }
        
        #endregion


        private string GetLocation() {
            string path = null;
            this.doc = this.view.Model.Document;
            this.nsmgr = new XmlNamespaceManager(doc.NameTable);
            XmlTreeNode node = this.view.SelectedNode as XmlTreeNode;
            if (node != null) {
                XmlNode xnode = node.Node;
                if (xnode != null) {
                    XmlNode nsctx = xnode;
                    if (nsctx.NodeType != XmlNodeType.Element) {
                        if (nsctx.NodeType == XmlNodeType.Attribute) {
                            nsctx = ((XmlAttribute)nsctx).OwnerElement;
                        } else {
                            nsctx = nsctx.ParentNode;
                        }
                    }
                    if (nsctx == null || nsctx == this.doc)
                        nsctx = this.doc.DocumentElement;

                    foreach (XmlAttribute a in nsctx.SelectNodes("namespace::*")) {
                        string prefix = (a.Prefix == "xmlns") ? a.LocalName : "";
                        if (!string.IsNullOrEmpty(prefix)) {
                            string ns = a.Value;
                            if (nsmgr.LookupPrefix(ns) == null) {
                                nsmgr.AddNamespace(prefix, ns);
                            }
                        }
                    }
                    foreach (XmlElement child in nsctx.SelectNodes("//*[namespace-uri(.) != '']")) {
                        string uri = child.NamespaceURI;
                        if (nsmgr.LookupPrefix(uri) == null) {
                            string prefix = child.Prefix;
                            if (!string.IsNullOrEmpty(prefix)) {
                                nsmgr.AddNamespace(prefix, uri);
                            }
                        }
                    }

                    XPathGenerator gen = new XPathGenerator();
                    path = gen.GetXPath(xnode, nsmgr);
                }
            }
            return path;
        }

        class XmlNodeMatch {
            int index;
            int length;
            XmlNode node;
            bool name;

            public XmlNodeMatch(XmlNode node, int index, int length, bool isName) {
                this.index = index;
                this.node = node;
                this.length = length;
                this.name = isName;
            }

            public XmlNode Node { get { return this.node; } }
            public int Index { get { return this.index; } }
            public int Length { get { return this.length; } }
            public bool IsName { get { return this.name; } }
        }

    }


}
