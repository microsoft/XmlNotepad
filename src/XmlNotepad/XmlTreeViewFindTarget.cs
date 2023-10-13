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

namespace XmlNotepad
{

    public class XmlTreeViewFindTarget : IFindTarget
    {
        private XmlTreeView _view;
        private string _expression;
        private FindFlags _flags;
        private SearchFilter _filter;
        private XmlNamespaceManager _nsmgr;
        private XmlDocument _doc;
        private List<XmlNodeMatch> _list;
        private XmlTreeNode _current;
        private Regex _regex;
        private StringComparison _comp;
        private XmlNodeMatch _match;
        private IEditableView _ev;
        private bool _doingReplace;
        private int _position; // current position in the list.
        private int _start; // this is the start position in the list around which we can wrap the find.
        private bool _resetPosition; // model has changed 'list' needs updating, but we don't want to forget our position either.

        public XmlTreeViewFindTarget(XmlTreeView view)
        {
            this._view = view;
        }

        bool Backwards { get { return (_flags & FindFlags.Backwards) != 0; } }
        bool IsXPath { get { return (_flags & FindFlags.XPath) != 0; } }
        bool IsRegex { get { return (_flags & FindFlags.Regex) != 0; } }
        bool MatchCase { get { return ((this._flags & FindFlags.MatchCase) != 0); } }
        bool WholeWord { get { return (this._flags & FindFlags.WholeWord) != 0; } }

        void FindNodes()
        {
            XmlDocument doc = this._view.Model.Document;
            if (this._doc == null)
            {
                this._doc = doc;
            }
            else if (this._doc != doc)
            {
                this._doc = doc;
                this._nsmgr = new XmlNamespaceManager(doc.NameTable);
            }
            this._view.Model.ModelChanged -= OnModelChanged;
            this._view.Model.ModelChanged += OnModelChanged;

            string expr = "//node()";
            if (_filter == SearchFilter.Comments)
            {
                expr = "//comment()";
            }
            this._regex = null;
            if (IsXPath)
            {
                expr = this._expression;
            }
            else if (IsRegex)
            {
                this._regex = new Regex(this._expression);
            }

            this._comp = MatchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;

            this._list = new List<XmlNodeMatch>();

            foreach (XmlNode node in this._doc.SelectNodes(expr, this._nsmgr))
            {
                MatchNode(node, _list);
                XmlElement e = node as XmlElement;
                if (!IsXPath && e != null && e.HasAttributes)
                {
                    foreach (XmlAttribute a in e.Attributes)
                    {
                        MatchNode(a, _list);
                    }
                }
            }
        }

        private void MatchNode(XmlNode node, List<XmlNodeMatch> matches)
        {
            if (!IsXPath)
            {
                if (_filter == SearchFilter.Comments && node.NodeType == XmlNodeType.Comment)
                {
                    MatchStrings(matches, node, node.Value, false);
                }
                else
                {
                    bool namedNode = (node.NodeType == XmlNodeType.Element ||
                        node.NodeType == XmlNodeType.Attribute ||
                        node.NodeType == XmlNodeType.EntityReference ||
                        node.NodeType == XmlNodeType.ProcessingInstruction ||
                        node.NodeType == XmlNodeType.XmlDeclaration);
                    if ((_filter == SearchFilter.Names && namedNode) || _filter == SearchFilter.Everything)
                    {
                        MatchStrings(matches, node, node.Name, true);
                    }
                    if (_filter == SearchFilter.Text || _filter == SearchFilter.Everything)
                    {
                        MatchStrings(matches, node, node.Value, false);
                    }
                }
            }
            else
            {
                bool name = (node is XmlElement || node is XmlAttribute);
                int len = name ? node.Name.Length : node.Value.Length;
                matches.Add(new XmlNodeMatch(node, 0, len, name));
            }
        }

        void OnModelChanged(object sender, ModelChangedEventArgs e)
        {
            if (!this._doingReplace)
            {
                // Then the list of matching nodes we found might now be invalid!
                ResetPosition();
            }
        }

        private void ResetPosition()
        {
            this._resetPosition = true;
        }

        struct Token
        {
            public int Index;
            public string Value;
        }

        IEnumerable<Token> Tokenize(string value)
        {
            int start = 0;
            bool inWord = false;
            for (int i = 0, n = value.Length; i < n; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                    case '.':
                    case ',':
                    case ';':
                    case '!':
                    case '\'':
                    case '"':
                    case '+':
                    case '=':
                    case '-':
                    case '<':
                    case '>':
                    case '(':
                    case ')':
                        if (inWord)
                        {
                            inWord = false;
                            yield return new Token() { Value = value.Substring(start, i - start), Index = start };
                        }
                        break;
                    default:
                        if (!inWord)
                        {
                            inWord = true;
                            start = i;
                        }
                        break;
                }
            }
            if (inWord)
            {
                yield return new Token() { Value = value.Substring(start, value.Length - start), Index = start };
            }
        }

        void MatchStrings(List<XmlNodeMatch> list, XmlNode node, string value, bool name)
        {
            if (string.IsNullOrEmpty(value))
                return;

            if (this.WholeWord)
            {
                foreach (var token in Tokenize(value))
                {
                    var index = token.Index;
                    var word = token.Value;

                    if (string.Compare(this._expression, word, _comp) == 0)
                    {
                        list.Add(new XmlNodeMatch(node, index, word.Length, name));
                    }
                }
            }
            else
            {
                if (this._regex != null)
                {
                    foreach (Match m in this._regex.Matches(value))
                    {
                        list.Add(new XmlNodeMatch(node, m.Index, m.Length, name));
                    }
                }
                else
                {
                    int len = this._expression.Length;
                    int j = value.IndexOf(this._expression, 0, _comp);
                    while (j >= 0)
                    {
                        list.Add(new XmlNodeMatch(node, j, len, name));
                        j = value.IndexOf(this._expression, j + len, _comp);
                    }
                }
            }
        }

        void CheckCurrentState(string expression, FindFlags flags, SearchFilter filter)
        {
            if (this._expression != expression || this._flags != flags || this._filter != filter)
            {
                this._expression = expression;
                this._flags = flags;
                this._filter = filter;
                this._list = null;
                this._match = null;
            }
        }

        #region IFindTarget Members

        public FindResult FindNext(string expression, FindFlags flags, SearchFilter filter)
        {
            CheckCurrentState(expression, flags, filter);

            this._match = null;

            if (string.IsNullOrEmpty(expression))
                return FindResult.None;

            if (this._list == null)
            {
                FindNodes();
                this._position = -1;
                this._start = -1; // we have not yet moved to one of the found nodes.
            }
            else if (this._resetPosition)
            {
                this._resetPosition = false;
                FindNodes();
                if (this._start >= _list.Count)
                {
                    this._start = _list.Count - 1;
                }
            
                if (this._position >= _list.Count)
                {
                    this._position = _list.Count - 1;
                }
            }

            int s = this._start;
            bool first = (this._start == -1);

            var rc = FindSelectedNode();
            int pos = rc.Item1;
            bool exact = rc.Item2;

            if (pos != this._position)
            {
                // user has moved the selection somewhere else, so start the find ring over again.
                first = true;
            }

            bool hasSomething = this._list != null && this._list.Count > 0;
            while (hasSomething)
            {
                if (this.Backwards)
                {
                    pos--;
                    if (pos < 0) pos = _list.Count - 1;
                }
                else
                {
                    pos++;
                    if (pos >= _list.Count) pos = 0;
                }

                if (first)
                {
                    this._start = s = pos;
                    first = false; 
                }
                else if (pos == this._start)
                {
                    // we have wrapped around!
                    break;
                }

                this._position = pos;

                XmlNodeMatch m = _list[pos] as XmlNodeMatch;
                if (m.Replaced)
                {
                    continue;
                }
                XmlNode node = m.Node;
                this._match = m;

                if (node != null)
                {
                    this._current = this._view.FindNode(node);
                    if (this._current != null)
                    {
                        this._view.SelectedNode = this._current;
                        if (m.IsName)
                        {
                            _ev = this._view.TreeView;
                        }
                        else
                        {
                            _ev = this._view.NodeTextView;
                        }
                        if (_ev.BeginEdit(null))
                        {
                            _ev.SelectText(m.Index, m.Length);
                        }
                    }
                }
                return FindResult.Found;
            }

            this._start = -1; // get ready for another cycle around.
            return hasSomething ? FindResult.NoMore : FindResult.None;
        }

        /// <summary>
        /// Now find item in the list that is the selectedNode or immediately before it so that
        /// we can start the first FindNext operation with the item that is closest to the selected node.
        /// </summary>
        (int, bool) FindSelectedNode()
        {
            XmlNode selectedNode = null;
            bool treeFocused = this._view.TreeView.ContainsFocus;
            bool textViewFocused = this._view.NodeTextView.ContainsFocus;
            bool textEditing = this._view.NodeTextView.IsEditing;
            int start = 0;
            if (textEditing)
            {
                textViewFocused = true;
                // can select multiple words in one big paragraph, so we need to know where we are.
                start = this._view.NodeTextView.SelectionStart;
            }
            else if (!textViewFocused)
            {
                // then pretend the tree view has focus for 'findnext' purposes.
                treeFocused = true;
            }
            XmlTreeNode selected = this._view.SelectedNode;

            if (selected != null && _list != null)
            {
                selectedNode = selected.Node;
                if (selectedNode != null)
                {
                    // I'm not using XPathNavigator.ComparePosition because it is returning XmlNodeOrder.Unknown
                    // sometimes which is not very useful!
                    if (Backwards)
                    {
                        for (int pos = _list.Count - 1; pos >= 0; pos--)
                        {
                            XmlNodeMatch m = _list[pos];
                            XmlNode node = m.Node;
                            if (node == selectedNode)
                            {
                                if (m.IsName && treeFocused)
                                {
                                    return (pos, true); // selected node is the node name.
                                }
                                else if (!m.IsName && m.Index <= start)
                                {
                                    return (pos, true); // selected node is one of the matching nodes.
                                }
                            }
                            else if (!IsNodeAfter(selectedNode, node))
                            {
                                return (pos + 1, false);
                            }
                        }
                    }
                    else
                    {
                        for (int pos = 0; pos < _list.Count; pos++)
                        {
                            XmlNodeMatch m = _list[pos];
                            XmlNode node = m.Node;
                            if (node == selectedNode)
                            {
                                if (m.IsName && treeFocused)
                                {
                                    return (pos, true); // selected node is the node name.
                                }
                                else if (!m.IsName && m.Index >= start)
                                {
                                    return (pos, true); // selected node is one of the matching nodes.
                                }
                            }
                            else if (IsNodeAfter(selectedNode, node))
                            {
                                return (pos - 1, false);
                            }
                        }
                    }
                }
            }

            // then simply start at the beginning.
            return (this.Backwards ? _list.Count : -1, false);
        }

        private void FixOffsets(XmlNodeMatch match, string replaceWith)
        {
            int delta = replaceWith.Length - match.Length;
            List<XmlNodeMatch> matches = new List<XmlNodeMatch>();
            MatchNode(match.Node, matches);
            int pos = 0;
            foreach(var m in _list)
            {
                if (!m.Replaced && m.Node == match.Node && pos < matches.Count)
                {
                    m.Index = matches[pos++].Index;
                }
            }
        }

        // returns true if the match node comes after the selected node in document order.
        bool IsNodeAfter(XmlNode selected, XmlNode match)
        {
            List<XmlNode> aparents = GetParentChain(selected);
            List<XmlNode> bparents = GetParentChain(match);
            // now find the lowest common node.
            int i = 0;
            for (; i < aparents.Count && i < bparents.Count; i++)
            {
                XmlNode p1 = aparents[i];
                XmlNode p2 = bparents[i];
                if (p1 != p2)
                {
                    // Ok, found the common parent, so now return the
                    // siblings under this parent so we can calculate 
                    // relative document order of those siblings.
                    break;
                }
            }

            XmlNode sibling1 = null;
            XmlNode sibling2 = null;
            if (i < aparents.Count)
            {
                sibling1 = aparents[i];
            }
            else
            {
                sibling1 = selected;
            }

            if (i < bparents.Count)
            {
                sibling2 = bparents[i];
            }
            else
            {
                sibling2 = match;
            }

            if (sibling1 == sibling2)
            {
                // then selected is a direct child of match, therefore match does NOT
                // come after its parent, by definition
                return false;
            }

            for (XmlNode s1 = sibling1; s1 != null; s1 = s1.NextSibling)
            {
                if (s1 == sibling2)
                {
                    return true;
                }
            }

            return false;
        }

        List<XmlNode> GetParentChain(XmlNode p)
        {
            List<XmlNode> parents = new List<XmlNode>();
            p = p.SelectSingleNode("..");
            while (p != null)
            {
                parents.Insert(0, p);
                p = p.ParentNode;
            }
            return parents;
        }

        public Rectangle MatchRect
        {
            get
            {
                if (_current != null && _match != null)
                {
                    Rectangle bounds;
                    if (_match.IsName)
                    {
                        bounds = _view.TreeView.EditorBounds;
                    }
                    else
                    {
                        bounds = _view.NodeTextView.EditorBounds;
                    }
                    return bounds;
                }
                return Rectangle.Empty;
            }
        }

        public bool ReplaceCurrent(string replaceWith)
        {
            if (this._ev != null && this._ev.IsEditing && this._match != null)
            {
                bool success = true;
                try
                {
                    this._doingReplace = true;
                    success = this._ev.ReplaceText(this._match.Index, this._match.Length, replaceWith);
                    this._match.Replaced = true; // cannot match this node any more when we cycle around.
                    this.FixOffsets(this._match, replaceWith);
                } 
                finally
                {
                    this._doingReplace = false;
                }
                return success;
            }
            return false;
        }

        public string Location
        {
            get
            {
                return this.GetLocation();
            }
        }

        public XmlNamespaceManager Namespaces
        {
            get
            {
                if (_nsmgr == null)
                {
                    this.GetLocation();
                }
                return _nsmgr;
            }
            set
            {
                this._nsmgr = value;
            }
        }

        #endregion


        private string GetLocation()
        {
            string path = null;
            XmlTreeNode node = this._view.SelectedNode as XmlTreeNode;
            if (node != null && node.Node != null)
            {
                var xnode = node.Node;
                this._nsmgr = XmlHelpers.GetNamespaceScope(xnode);
                path = XmlHelpers.GetXPathLocation(xnode, _nsmgr);
            }
            return path;
        }

        class XmlNodeMatch
        {
            int index;
            int length;
            XmlNode node;
            bool name;
            bool replaced;

            public XmlNodeMatch(XmlNode node, int index, int length, bool isName)
            {
                this.index = index;
                this.node = node;
                this.length = length;
                this.name = isName;
            }

            public XmlNode Node => this.node;
            public int Index { get => this.index; set => this.index = value; }
            public int Length => this.length;
            public bool IsName => this.name;
            public bool Replaced { get => replaced; set => replaced = value; }
        }

    }


}
