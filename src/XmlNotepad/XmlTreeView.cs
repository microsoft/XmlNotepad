using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using SR = XmlNotepad.StringResources;


namespace XmlNotepad
{

    public class XmlTreeView : System.Windows.Forms.UserControl
    {
        private XmlCache _model;
        private Settings _settings;
        private bool _disposed;
        private const int HScrollIncrement = 5;
        private int _updating;
        private bool _saving;

        public event EventHandler<NodeChangeEventArgs> NodeChanged;
        public event EventHandler<NodeChangeEventArgs> NodeInserted;
        public event EventHandler<NodeSelectedEventArgs> SelectionChanged;
        public event EventHandler ClipboardChanged;

        private XmlTreeNode _dragged;
        private XmlTreeViewDropFeedback _feedback;
        private IntelliTip _tip;
        private NodeTextView _nodeTextView;
        private TreeView _myTreeView;
        private HashSet<string> _schemaAwareNames;
        private bool _showSchemaAwareText;

        public XmlTreeView()
        {
            this.SetStyle(ControlStyles.ContainerControl, true);
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            _myTreeView.AfterLabelEdit += new EventHandler<NodeLabelEditEventArgs>(myTreeView_AfterLabelEdit);
            _myTreeView.AfterCollapse += new EventHandler<TreeViewEventArgs>(myTreeView_AfterCollapse);
            _myTreeView.AfterExpand += new EventHandler<TreeViewEventArgs>(myTreeView_AfterExpand);
            _myTreeView.AfterSelect += new EventHandler<TreeViewEventArgs>(myTreeView_AfterSelect);
            _myTreeView.MouseWheel += new MouseEventHandler(HandleMouseWheel);
            _myTreeView.KeyDown += new KeyEventHandler(myTreeView_KeyDown);

            this._myTreeView.DragDrop += new DragEventHandler(treeViewFeedback_DragDrop);
            this._myTreeView.DragEnter += new DragEventHandler(treeViewFeedback_DragEnter);
            this._myTreeView.DragLeave += new EventHandler(treeViewFeedback_DragLeave);
            this._myTreeView.DragOver += new DragEventHandler(treeViewFeedback_DragOver);
            this._myTreeView.AllowDrop = true;
            this._myTreeView.GiveFeedback += new GiveFeedbackEventHandler(myTreeView_GiveFeedback);
            this._myTreeView.ItemDrag += new ItemDragEventHandler(myTreeView_ItemDrag);
            this._myTreeView.AfterBatchUpdate += new EventHandler(myTreeView_AfterBatchUpdate);

            this._nodeTextView.KeyDown += new KeyEventHandler(nodeTextView_KeyDown);
            this._nodeTextView.MouseWheel += new MouseEventHandler(HandleMouseWheel);
            this._nodeTextView.AfterSelect += new EventHandler<TreeViewEventArgs>(nodeTextView_AfterSelect);
            this._nodeTextView.AccessibleRole = System.Windows.Forms.AccessibleRole.List;

            this.Disposed += new EventHandler(OnDisposed);

            _tip = new IntelliTip(this);
            _tip.AddWatch(this._nodeTextView);
            _tip.AddWatch(this._myTreeView);
            _tip.ShowToolTip += new IntelliTipEventHandler(OnShowToolTip);
        }

        void OnDisposed(object sender, EventArgs e)
        {
            this._disposed = true;
        }

        public void Close()
        {
            this._tip.Close();
            this._myTreeView.Close();
            this._nodeTextView.Close();
        }

        [Browsable(false)]
        public XmlTreeNode SelectedNode
        {
            get
            {
                return this._myTreeView.SelectedNode as XmlTreeNode;
            }
            set
            {
                this._myTreeView.SelectedNode = value;
            }
        }

        void OnShowToolTip(object sender, IntelliTipEventArgs args)
        {
            Point pt = this._myTreeView.ApplyScrollOffset(args.Location);
            XmlTreeNode tn = this.TreeView.FindNodeAt(20, pt.Y) as XmlTreeNode;
            if (tn != null)
            {
                args.ToolTip = tn.GetToolTip();
            }
        }

        public void ExpandAll()
        {
            this.SuspendLayout();
            this._myTreeView.ExpandAll();
            this.ResumeLayout();
        }

        public void CollapseAll()
        {
            this.SuspendLayout();
            this._myTreeView.CollapseAll();
            this.ResumeLayout();
        }

        public void SetSite(ISite site)
        {
            base.Site = site;
            this._nodeTextView.SetSite(site);
            this._myTreeView.SetSite(site);

            // register our customer builders
            this.IntellisenseProvider.RegisterBuilder("XmlNotepad.ColorBuilder", typeof(ColorBuilder));
            this.IntellisenseProvider.RegisterBuilder("XmlNotepad.UriBuilder", typeof(UriBuilder));
            this.IntellisenseProvider.RegisterEditor("XmlNotepad.DateTimeEditor", typeof(DateTimeEditor));

            this._model = (XmlCache)this.Site.GetService(typeof(XmlCache));            
            if (this._model != null)
            {
                this._model.FileChanged += new EventHandler(OnFileChanged);
                this._model.ModelChanged += new EventHandler<ModelChangedEventArgs>(OnModelChanged);
            }
            this._settings = (Settings)this.Site.GetService(typeof(Settings));
            if (this._settings != null)
            {
                this._settings.Changed += new SettingsEventHandler(OnSettingsChanged);
                int indent = this.Settings.GetInteger("TreeIndent");
                this._myTreeView.TreeIndent = indent;
                this._schemaAwareNames = new HashSet<string>(this._settings.GetString("SchemaAwareNames").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.InvariantCultureIgnoreCase);
                this._showSchemaAwareText = this._settings.GetBoolean("SchemaAwareText");
            }
            if (this._model != null) BindTree();
        }

        public HashSet<string> SchemaAwareNames => this._schemaAwareNames;

        public bool ShowSchemaAwareText => this._showSchemaAwareText;

        [Browsable(false)]
        public XmlCache Model
        {
            get
            {
                if (this.Site == null)
                {
                    throw new ApplicationException("ISite has not been provided, so model cannot be found");
                }
                return this._model;
            }
        }

        [Browsable(false)]
        public Settings Settings
        {
            get
            {
                if (this.Site == null)
                {
                    throw new ApplicationException("ISite has not been provided, so settings cannot be found");
                }
                return this._settings;
            }
        }

        public NodeTextView NodeTextView
        {
            get { return _nodeTextView; }
            set { _nodeTextView = value; }
        }

        public void CancelEdit()
        {
            TreeNode n = _myTreeView.SelectedNode;
            if (n != null && n.IsEditing)
            {
                n.EndEdit(true);
            }
            this._nodeTextView.EndEdit(true);
        }


        public virtual XmlTreeNode CreateTreeNode()
        {
            return new XmlTreeNode(this);
        }

        public virtual XmlTreeNode CreateTreeNode(XmlNode node)
        {
            return new XmlTreeNode(this, node);
        }

        public virtual XmlTreeNode CreateTreeNode(XmlTreeNode parent, XmlNode node)
        {
            return new XmlTreeNode(this, parent, node);
        }

        /// <summary>
        /// Find the given node in the tree by expanding the minimum amount of stuff to get there.
        /// </summary>
        /// <param name="node">The XmlNode to find</param>
        /// <returns>The XmlTreeNode representing this XmlNode or null if the XmlNode is disconnected from the current document.</returns>
        public XmlTreeNode FindNode(XmlNode node)
        {
            if (node is XmlDocument)
            {
                // there is no XmlTreeNode for the document.
                return null;
            }

            XmlTreeNode parent;

            if (node is XmlAttribute a)
            {
                parent = FindNode(a.OwnerElement);
                return FindChild(parent.Children, node);
            }

            if (node == null || node.ParentNode == null)
            {
                // then we have a node that is disconnected
                return null;
            }

            if (node.OwnerDocument != this._model.Document)
            {
                return null;
            }

            parent = FindNode(node.ParentNode);
            if (parent == null)
            {
                // then the node is a root node.
                return FindChild(this._myTreeView.Nodes, node);
            }
            else
            {
                return FindChild(parent.Children, node);
            }
        }

        XmlTreeNode FindChild(TreeNodeCollection nodes, XmlNode node)
        {
            foreach (XmlTreeNode xn in nodes)
            {
                if (xn.Node == node) return xn;
            }

            return null;
        }

        XmlTreeNode FindIdAttribute(HashSet<string> attributeNames, string value)
        {
            if (attributeNames.Count == 0)
            {
                return null;
            }
            return FindIdAttribute(this._myTreeView.Nodes, attributeNames, value);
        }

        XmlTreeNode FindIdAttribute(TreeNodeCollection nodes, HashSet<string> attributeNames, string value)
        {
            foreach (XmlTreeNode xn in nodes)
            {
                if (xn.NodeType == XmlNodeType.Attribute && attributeNames.Contains(xn.Node.Name) && xn.Node.Value == value)
                {
                    return xn;
                }
                if (xn.Children != null)
                {
                    var found = FindIdAttribute(xn.Children, attributeNames, value);
                    if (found != null) return found;
                }
            }

            return null;
        }

        public bool Commit()
        {
            this._nodeTextView.EndEdit(false);
            TreeNode n = _myTreeView.SelectedNode;
            if (n != null && n.IsEditing)
            {
                return n.EndEdit(false);
            }
            return true;
        }

        [Browsable(false)]
        public UndoManager UndoManager
        {
            get { return (UndoManager)this.Site.GetService(typeof(UndoManager)); }
        }

        [System.ComponentModel.Browsable(false)]
        public IIntellisenseProvider IntellisenseProvider
        {
            get { return (IIntellisenseProvider)this.Site.GetService(typeof(IIntellisenseProvider)); }
        }

        [Browsable(false)]
        public TreeView TreeView
        {
            get { return this._myTreeView; }
        }

        private void myTreeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            try
            {
                XmlTreeNode xn = (XmlTreeNode)e.Node;
                XmlNode n = xn.Node;
                if (e.CancelEdit) return; // it's being cancelled.

                if (e.Label == null || StringHelper.IsNullOrEmpty(e.Label.Trim()))
                {

                    string arg = null;
                    if (xn.NodeImage == NodeImage.Attribute)
                        arg = "attributes";
                    else if (xn.NodeImage == NodeImage.Element || xn.NodeImage == NodeImage.OpenElement || xn.NodeImage == NodeImage.Leaf)
                        arg = "elements";

                    if (arg != null && n == null && MessageBox.Show(this,
                        SR.XmlNameEmptyPrompt, SR.XmlNameErrorCaption,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.No)
                    {
                        e.Node.BeginEdit();
                        e.CancelEdit = true;
                    }
                    return;
                }
                Command cmd = null;
                if (n == null)
                {
                    TreeNode parent = e.Node.Parent;
                    XmlNode context = (parent == null) ? this._model.Document : ((XmlTreeNode)parent).Node;
                    cmd = this.UndoManager.Peek();
                    try
                    {
                        InsertNode inode = cmd as InsertNode;
                        if (inode != null)
                        {
                            if (inode.RequiresName)
                            {
                                inode.XmlNode = inode.CreateNode(context, e.Label, e.Namespace);
                                // Cause selection event to be triggered so that menu state
                                // is recalculated.
                                XmlElement scope = inode.XmlNode as XmlElement;
                                if (scope == null)
                                {
                                    scope = context as XmlElement;
                                }
                                XmlDocument doc = context is XmlDocument ? (XmlDocument)context : context.OwnerDocument;
                                this._myTreeView.SelectedNode = null;
                                this.OnNodeInserted(inode.NewNode);
                                var prefix = inode.XmlNode.Prefix;
                                if (!string.IsNullOrEmpty(prefix))
                                {
                                    if (prefix != "xmlns" && !XmlHelpers.IsPrefixInScope(context, prefix))
                                    {
                                        XmlAttribute attr = doc.CreateAttribute("xmlns", inode.XmlNode.Prefix, XmlStandardUris.XmlnsUri);
                                        attr.Value = inode.XmlNode.NamespaceURI;
                                        scope.Attributes.Append(attr);
                                        xn.Children.Add(new XmlTreeNode(this, (XmlTreeNode)e.Node, attr));
                                        xn.Expand();
                                    }
                                }   
                                else if (!string.IsNullOrEmpty(e.Namespace))
                                {
                                    if (!XmlHelpers.IsDefaultNamespaceInScope(context, e.Namespace))
                                    {
                                        XmlAttribute attr = doc.CreateAttribute("", "xmlns", XmlStandardUris.XmlnsUri);
                                        attr.Value = e.Namespace;
                                        scope.Attributes.Append(attr);
                                        xn.Children.Add(new XmlTreeNode(this, (XmlTreeNode)e.Node, attr));
                                        xn.Expand();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message,
                            SR.XmlNameErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this._myTreeView.SelectedNode = e.Node;
                        e.CancelEdit = true;
                        xn.Label = e.Label.Trim();
                        e.Node.BeginEdit();
                        return;
                    }
                    e.Node.Label = e.Label;
                    this._myTreeView.SelectedNode = e.Node;
                    this._nodeTextView.Invalidate(e.Node);
                    this._nodeTextView.FocusBeginEdit(null);
                    return; // one undoable unit.
                }
                switch (n != null ? n.NodeType : XmlNodeType.None)
                {
                    case XmlNodeType.Comment:
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        e.CancelEdit = true;
                        // actually it would be cool to change the node type at this point.
                        break;
                    case XmlNodeType.Attribute:
                        cmd = new EditAttributeName(n as XmlAttribute, e);
                        break;
                    case XmlNodeType.Element:
                        cmd = new EditElementName(n as XmlElement, e);
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        cmd = new EditProcessingInstructionName(n as XmlProcessingInstruction, e);
                        break;
                }
                if (cmd != null)
                {
                    this.UndoManager.Push(cmd);
                }
            }
            catch (Exception ex)
            {
                e.CancelEdit = true;
                MessageBox.Show(this, ex.Message, SR.EditErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void myTreeView_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            if (!this._myTreeView.InBatchUpdate)
            {
                PerformLayout();
                Invalidate();
            }
        }

        private void myTreeView_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (!this._myTreeView.InBatchUpdate)
            {
                PerformLayout();
                Invalidate();
            }
        }

        private void myTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode n = e.Node;
            if (this.TreeView.InBatchUpdate)
            {
                this._nodeTextView.InternalSelect(n);
            }
            else
            {
                this._nodeTextView.SelectedNode = n;
                if (n != null)
                {
                    ScrollIntoView(n);
                }
                if (SelectionChanged != null) SelectionChanged(this, new NodeSelectedEventArgs(n as XmlTreeNode));
            }
        }

        void myTreeView_AfterBatchUpdate(object sender, EventArgs e)
        {
            if (this.SelectedNode != null)
            {
                ScrollIntoView(this.SelectedNode);
            }
        }

        void nodeTextView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (this._myTreeView != null)
            {
                this._myTreeView.SelectedNode = e.Node;
            }
        }

        public Point ScrollPosition
        {
            get { return _myTreeView.ScrollPosition; }
            set
            {
                if (_myTreeView.ScrollPosition.Y != value.Y)
                {
                    // sync node text view to the same position.
                    _nodeTextView.ScrollPosition = new Point(0, value.Y);
                    _nodeTextView.Invalidate();
                }
                else
                {
                    // horizontal only
                }
                _myTreeView.ScrollPosition = value;
                _myTreeView.Invalidate();
            }
        }

        public virtual void ScrollIntoView(TreeNode n)
        {
            // Scroll the newly selected node into view vertically.
            Rectangle r = n.LabelBounds;
            int delta = _myTreeView.TreeIndent + imageList1.ImageSize.Width + TreeNode.GetGap(_myTreeView.TreeIndent);
            r = new Rectangle(r.Left - delta, r.Top, r.Width + delta, r.Height);
            int y = r.Top + _myTreeView.ScrollPosition.Y;
            if (y > _myTreeView.Height - _myTreeView.ItemHeight)
            {
                y = y - _myTreeView.Height + _myTreeView.ItemHeight;
            }
            else if (y > 0)
            {
                y = 0;
            }
            if (y != 0)
            {
                int newy = _myTreeView.ScrollPosition.Y - y;
                _myTreeView.ScrollPosition = new Point(_myTreeView.ScrollPosition.X, newy);
                _nodeTextView.ScrollPosition = new Point(0, newy);
                this._nodeTextView.Invalidate();
                this.vScrollBar1.Value = Math.Max(0, Math.Min(this.vScrollBar1.Maximum, this.vScrollBar1.Value + (y / _myTreeView.ItemHeight)));
            }

            // Tweak horizontal to make the newly selected label visible.
            int x = this._myTreeView.ScrollPosition.X;
            if (r.Left + this._myTreeView.ScrollPosition.X < 0)
            {
                // Label is off the left hand side.
                x = -r.Left;
            }
            else if (r.Right + this._myTreeView.ScrollPosition.X > this.resizer.Left)
            {
                // Label is off the right hand side
                x = this.resizer.Left - r.Right - 10;
                if (r.Left + x < 0)
                {
                    // Label is too long to fit, now it hangs off the left side, so
                    // let's just leave it where it was
                    x = this._myTreeView.ScrollPosition.X;
                }
            }
            if (x != this._myTreeView.ScrollPosition.X)
            {
                int pos = Math.Max(0, Math.Min(this.hScrollBar1.Maximum * HScrollIncrement, -x));
                _myTreeView.ScrollPosition = new Point(-pos, _myTreeView.ScrollPosition.Y);
                this.hScrollBar1.Value = pos / HScrollIncrement;
            }
        }

        internal void OnNodeChanged(XmlTreeNode node)
        {
            if (NodeChanged != null) NodeChanged(this, new NodeChangeEventArgs(node));
        }

        public virtual void OnNodeInserted(XmlTreeNode node)
        {
            if (NodeInserted != null) NodeInserted(this, new NodeChangeEventArgs(node));
            // Populate default value.
            if (node.Node != null && !node.Node.HasChildNodes &&
                (node.NodeType == XmlNodeType.Attribute || node.NodeType == XmlNodeType.Element))
            {
                SetDefaultValue(node);
            }
        }

        protected virtual void SetDefaultValue(XmlTreeNode node)
        {
            IIntellisenseProvider provider = this.IntellisenseProvider;
            if (provider != null)
            {
                provider.SetContextNode(node);
                string defaultValue = provider.GetDefaultValue();
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    EditNodeValue cmd = new EditNodeValue(this, node, defaultValue);
                    this.UndoManager.Push(cmd);
                }
            }
        }

        private void OnModelChanged(object sender, ModelChangedEventArgs e)
        {
            if (_disposed) return;
            if (this._updating > 0) return;
            ModelChangeType t = e.ModelChangeType;
            switch (t)
            {
                case ModelChangeType.NodeInserted:
                case ModelChangeType.NodeChanged:
                    CheckChange(e);
                    break;
                case ModelChangeType.Cleared:
                case ModelChangeType.Reloaded:
                    CancelEdit();
                    BindTree();
                    break;
                case ModelChangeType.NamespaceChanged:
                    RecalculateNamespaces(e.Node);
                    break;
            }
            _nodeTextView.Invalidate();
        }

        private void CheckChange(ModelChangedEventArgs e)
        {
            // Normally our user initiated editing commands already create the right XmlTreeNodes
            // but when document is saved it may add nodes (like xml declaration) and so 
            // we check for this here and add corresponding nodes in the tree view when necessary.
            XmlNode node = e.Node;
            if (!IsEditing && _saving && node != null && null == FindNode(node))
            {
                if (e.ModelChangeType == ModelChangeType.NodeInserted)
                {
                    // figure out where this new node lives in the tree...
                    XmlTreeNode context = null;
                    InsertPosition position = InsertPosition.Child;
                    if (node.PreviousSibling != null)
                    {
                        context = FindNode(node.PreviousSibling);
                        position = InsertPosition.After;
                    }
                    else if (node.NextSibling != null)
                    {
                        context = FindNode(node.NextSibling);
                        position = InsertPosition.Before;
                    }
                    else
                    {
                        context = FindNode(node.ParentNode);
                        position = InsertPosition.Child;
                    }
                    if (context != null)
                    {
                        InsertNode inode = new InsertNode(context, position, node, false, false);
                        this.UndoManager.Push(inode);
                        this._nodeTextView.Invalidate();
                    }
                }
            }
        }

        public bool IsEditing
        {
            get { return this._myTreeView.IsEditing || this._nodeTextView.IsEditing; }
        }

        private void OnFileChanged(object sender, EventArgs e)
        {
            BindTree();
        }

        void BindTree()
        {
            this.vScrollBar1.Maximum = 0;
            this.vScrollBar1.Value = 0;
            this.hScrollBar1.Maximum = 0;
            this.hScrollBar1.Value = 0;
            this._nodeTextView.Top = 0;
            this._nodeTextView.ScrollPosition = new Point(0, 0);
            this._myTreeView.ScrollPosition = new Point(0, 0);

            // try and preserve the selection and expanded state to the selection.
            XmlTreeNode selection = (XmlTreeNode)this.SelectedNode;
            XmlNamespaceManager nsmgr = null;
            string xpath = null;
            if (selection != null && selection.Node != null)
            {
                var xnode = selection.Node;
                nsmgr = XmlHelpers.GetNamespaceScope(xnode);
                xpath = XmlHelpers.GetXPathLocation(xnode, nsmgr);
            }

            this.SuspendLayout();
            this._myTreeView.BeginUpdate();
            try
            {
                XmlTreeNodeCollection nodes = new XmlTreeNodeCollection(this, this._model.Document);
                this._myTreeView.Nodes = this._nodeTextView.Nodes = nodes;

                foreach (XmlTreeNode tn in this._myTreeView.Nodes)
                {
                    tn.Expand();
                }
                this._nodeTextView.Reset();
            }
            finally
            {
                this._myTreeView.EndUpdate();
            }

            bool foundSelection = false;
            if (this._model.Document != null && !string.IsNullOrEmpty(xpath))
            {
                var matchingNode = this._model.Document.SelectSingleNode(xpath, nsmgr);
                if (matchingNode != null)
                {
                    var treeNode = this.FindNode(matchingNode);
                    TreeView.EnsureVisible(treeNode);
                    this.SelectedNode = treeNode;
                    foundSelection = true;
                }
            }

            this.ResumeLayout();
            this._myTreeView.Invalidate();
            this._myTreeView.Focus();
            if (!foundSelection && this._myTreeView.Nodes.Count > 0)
            {
                this.SelectedNode = (XmlTreeNode)this._myTreeView.Nodes[0];
            }
        }


        int CountVisibleNodes(TreeNodeCollection tc)
        {
            if (tc == null) return 0;
            int count = 0;
            foreach (TreeNode tn in tc)
            {
                count++;
                if (tn.IsExpanded)
                {
                    count += CountVisibleNodes(tn.Children);
                }
            }
            return count;
        }

        internal void SyncScrollbars()
        {

            if (this.hScrollBar1.Visible)
            {
                int x = this.resizer.Left;
                int w = this._myTreeView.VirtualWidth + 10;
                this._myTreeView.Height = this.Height - this.hScrollBar1.Height;
                int hScrollMax = 10 + ((w - x) / HScrollIncrement);
                this.hScrollBar1.Minimum = 0;
                this.hScrollBar1.Maximum = hScrollMax;
                this.hScrollBar1.Value = Math.Min(this.hScrollBar1.Value, hScrollMax);
            }
            else
            {
                this.hScrollBar1.Visible = false;
                this.hScrollBar1.Value = 0;
            }
            int itemHeight = this._myTreeView.ItemHeight;
            int visibleNodes = this._myTreeView.VirtualHeight / itemHeight;
            int vScrollMax = Math.Max(0, visibleNodes - 1);
            this.vScrollBar1.Maximum = vScrollMax;
            this.vScrollBar1.SmallChange = 1;
            this.vScrollBar1.LargeChange = this._myTreeView.VisibleRows;
            this.vScrollBar1.Minimum = 0;
            if (this._myTreeView.VirtualHeight < this.Height)
            {
                this.vScrollBar1.Value = 0;
            }
            else
            {
                this.vScrollBar1.Value = Math.Min(this.vScrollBar1.Value, vScrollMax);
            }

            int y = -this.vScrollBar1.Value * this._myTreeView.ItemHeight;
            this._myTreeView.ScrollPosition = new Point(-this.hScrollBar1.Value * HScrollIncrement, y);
            this._nodeTextView.ScrollPosition = new Point(0, y);

        }

        public int ResizerPosition
        {
            get { return this.resizer.Left; }
            set { this.resizer.Left = value; this.PerformLayout(); }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {

            int x = this.resizer.Left;
            this._myTreeView.Width = x;

            int count = CountVisibleNodes(this._myTreeView.Nodes);
            int h = Math.Max(this.Height, this._myTreeView.ItemHeight * count);
            this.vScrollBar1.Left = this.Right - this.vScrollBar1.Width;
            this.vScrollBar1.Height = this.Height;
            this.hScrollBar1.Top = this.Height - this.hScrollBar1.Height;

            this.hScrollBar1.Width = x;
            this._myTreeView.Size = new Size(x, this.Height);
            this._nodeTextView.Size = new Size(this.vScrollBar1.Left - this.resizer.Right, this.Height);
            this._nodeTextView.Left = this.resizer.Right;

            int w = this._myTreeView.VirtualWidth + 10;
            this._myTreeView.Width = Math.Max(w, x);
            if (w > x)
            {
                this._myTreeView.Height = this.Height - this.hScrollBar1.Height;
                this.hScrollBar1.Visible = true;
            }
            else
            {
                this.hScrollBar1.Visible = false;
            }

            SyncScrollbars();

            this.resizer.Height = this.Height;
            Invalidate();
            this._nodeTextView.Invalidate();
        }

        public void OnLoaded()
        {
            this._nodeTextView.OnLoaded();
        }

        private void OnClipboardChanged()
        {
            if (ClipboardChanged != null) ClipboardChanged(this, EventArgs.Empty);
        }

        public void Cut()
        {
            this.Commit();
            XmlTreeNode selection = (XmlTreeNode)this._myTreeView.SelectedNode;
            if (selection != null)
            {
                this.UndoManager.Push(new CutCommand(this, selection));
                OnClipboardChanged();
            }
        }

        public void Copy()
        {
            this.Commit();
            XmlTreeNode selection = (XmlTreeNode)this._myTreeView.SelectedNode;
            if (selection != null)
            {
                TreeData.SetData(selection);
                OnClipboardChanged();
            }
        }

        public void CopyXPath()
        {
            var xpath = this.GetSelectedXPath();
            if (!string.IsNullOrEmpty(xpath))
            {
                Clipboard.SetText(xpath);
                OnClipboardChanged();
            }
        }

        public string GetSelectedXPath()
        {
            XmlTreeNode selection = (XmlTreeNode)this._myTreeView.SelectedNode;
            if (selection != null && selection.Node != null)
            {
                var xnode = selection.Node;
                var nsmgr = XmlHelpers.GetNamespaceScope(xnode);
                string path = XmlHelpers.GetXPathLocation(xnode, nsmgr);
                return path;
            }
            return string.Empty;
        }

        public void Paste(InsertPosition position)
        {
            this.Commit();
            try
            {
                this.UndoManager.Push(new PasteCommand(this._model.Document, this, position, TreeData.GetData()));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, SR.PasteErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void PasteXml(InsertPosition position, string xml)
        {
            this.Commit();
            try
            {
                this.UndoManager.Push(new PasteCommand(this._model.Document, this, position, new TreeData(xml)));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, SR.PasteErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public virtual bool CanInsertNode(InsertPosition position, XmlNodeType type)
        {
            XmlTreeNode n = (XmlTreeNode)this._myTreeView.SelectedNode;
            if (n != null && n.Node == null)
            {
                // We are still editing this tree node and haven't created XmlNode
                // for it yet - so bail!
                return false;
            }
            InsertNode inode = new InsertNode(this);
            return inode.Initialize(n, position, type);
        }

        public void ChangeTo(XmlNodeType nt)
        {
            try
            {
                if (this.Commit())
                {
                    XmlTreeNode n = (XmlTreeNode)this._myTreeView.SelectedNode;
                    if (n == null) return;
                    ChangeNode cmd = new ChangeNode(this, n, nt);
                    this.UndoManager.Push(cmd);
                    this._nodeTextView.Invalidate();
                    this._myTreeView.SelectedNode = cmd.NewNode;
                    this._myTreeView.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, SR.ChangeErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void InsertNode(InsertPosition position, XmlNodeType type)
        {
            try
            {
                XmlTreeNode n = (XmlTreeNode)this._myTreeView.SelectedNode;
                InsertNode inode = new InsertNode(this);
                inode.Initialize(n, position, type);
                this.UndoManager.Push(inode);
                this._nodeTextView.Invalidate();
                this._myTreeView.SelectedNode = inode.NewNode;
                if (inode.RequiresName)
                {
                    this._myTreeView.Focus();
                    inode.NewNode.BeginEdit();
                }
                else
                {
                    this._nodeTextView.FocusBeginEdit(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, SR.InsertErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool Delete()
        {
            if (this._myTreeView.SelectedNode != null)
            {
                XmlTreeNode t = (XmlTreeNode)this._myTreeView.SelectedNode;
                this.UndoManager.Push(new DeleteNode(this._model.Document, t));
                this._nodeTextView.Invalidate();
                return true;
            }
            return false;
        }

        public bool Insert()
        {
            // Insert empty node of same type as current node right after current node.
            if (this._myTreeView.SelectedNode != null)
            {
                XmlTreeNode n = (XmlTreeNode)this._myTreeView.SelectedNode;
                var newType = n.Node.NodeType;
                if (newType == XmlNodeType.XmlDeclaration)
                {
                    // can't have 2 XML declarations...
                    newType = XmlNodeType.ProcessingInstruction;
                }
                InsertNode(InsertPosition.After, newType);
                return true;
            }
            return false;
        }

        public bool Duplicate()
        {
            if (this._myTreeView.SelectedNode != null)
            {
                XmlTreeNode t = (XmlTreeNode)this._myTreeView.SelectedNode;
                this.UndoManager.Push(new MoveNode(this, t, t, InsertPosition.After, true));
                this._nodeTextView.Invalidate();
                return true;
            }
            return false;

        }

        private void HandleMouseWheel(object sender, MouseEventArgs e)
        {
            int y = SystemInformation.MouseWheelScrollLines * (e.Delta / 120);
            int v = Math.Max(0, Math.Min(this.vScrollBar1.Value - y, this.vScrollBar1.Maximum + 1 - this.vScrollBar1.LargeChange));
            this.vScrollBar1.Value = v;
            vScrollBar1_Scroll(this, new ScrollEventArgs(ScrollEventType.ThumbTrack, v));
        }

        private void hScrollBar1_Scroll(object sender, System.Windows.Forms.ScrollEventArgs e)
        {
            if (this.TreeView.IsEditing)
            {
                this.TreeView.EndEdit(true);
            }
            if (this._nodeTextView.IsEditing)
            {
                this._nodeTextView.EndEdit(true);
            }
            this._myTreeView.ScrollPosition = new Point(-e.NewValue * HScrollIncrement, this._myTreeView.ScrollPosition.Y);
        }

        private void vScrollBar1_Scroll(object sender, System.Windows.Forms.ScrollEventArgs e)
        {
            if (this.TreeView.IsEditing)
            {
                this.TreeView.EndEdit(true);
            }
            if (this._nodeTextView.IsEditing)
            {
                this._nodeTextView.EndEdit(true);
            }
            int y = -e.NewValue * this._myTreeView.ItemHeight;
            this._myTreeView.ScrollPosition = new Point(this._myTreeView.ScrollPosition.X, y);
            this._nodeTextView.ScrollPosition = new Point(0, y);
            this._nodeTextView.Invalidate();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            CurrentEvent.Event = new KeyEventArgs(keyData);
            Keys modifiers = (keyData & Keys.Modifiers);
            Keys key = keyData & ~modifiers;
            switch (key)
            {
                case Keys.Tab:
                    if (modifiers == Keys.Shift)
                    {
                        bool editing = this._nodeTextView.IsEditing;
                        if (this._nodeTextView.Focused || editing)
                        {
                            if (this._nodeTextView.EndEdit(false))
                            {
                                this._myTreeView.Focus();
                            }
                        }
                        else
                        {
                            if (this._myTreeView.SelectedNode != null)
                            {
                                TreeNode previous = this._myTreeView.SelectedNode.PrevVisibleNode;
                                if (previous != null) this._myTreeView.SelectedNode = previous;
                            }
                            this._nodeTextView.Focus();
                        }
                    }
                    else
                    {
                        bool editing = this._myTreeView.IsEditing;
                        if (this._myTreeView.Focused || editing)
                        {
                            if (this._myTreeView.EndEdit(false))
                            {
                                this._nodeTextView.Focus();
                                if (editing)
                                {
                                    this._nodeTextView.FocusBeginEdit(null);
                                }
                            }
                        }
                        else
                        {
                            if (this._myTreeView.SelectedNode != null)
                            {
                                TreeNode next = this._myTreeView.SelectedNode.NextVisibleNode;
                                if (next != null) this._myTreeView.SelectedNode = next;
                            }
                            this._myTreeView.Focus();
                        }
                    }
                    return true;
            }
            return false;
        }

        public void StartIncrementalSearch()
        {
            if (this._nodeTextView.ContainsFocus)
            {
                this._nodeTextView.StartIncrementalSearch();
            }
            else
            {
                this.TreeView.Focus();
                this.TreeView.StartIncrementalSearch();
            }
        }

        private void myTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            CurrentEvent.Event = e;
            if (!this.IsEditing)
            {

                bool ctrlMods = e.Modifiers == Keys.Control || e.Modifiers == (Keys.Control | Keys.Shift);
                bool nudgeMods = e.Modifiers == (Keys.Control | Keys.Shift);
                XmlTreeNode xn = this.SelectedNode;
                TreeNode n = this._myTreeView.SelectedNode;
                switch (e.KeyCode)
                {
                    case Keys.Escape:
                        this.Commit();
                        if (!e.Handled)
                        {
                            this._myTreeView.SelectedNode = null;
                            if (this.SelectionChanged != null)
                                SelectionChanged(this, new NodeSelectedEventArgs(null));
                        }
                        break;
                    case Keys.X:
                        if (ctrlMods)
                        {
                            this.Cut();
                            e.Handled = true;
                        }
                        break;
                    case Keys.C:
                        if (ctrlMods)
                        {
                            this.Copy();
                            e.Handled = true;
                        }
                        break;
                    case Keys.V:
                        if (ctrlMods)
                        {
                            this.Paste(InsertPosition.Child);
                            e.Handled = true;
                        }
                        break;
                    case Keys.Right:
                        if (nudgeMods && CanNudgeNode(xn, NudgeDirection.Right))
                        {
                            this.NudgeNode(xn, NudgeDirection.Right);
                            e.Handled = true;
                        }
                        else if (!this.IsEditing && n != null && n.Children.Count == 0)
                        {
                            this._nodeTextView.Focus();
                            e.Handled = true;
                        }
                        break;
                    case Keys.Delete:
                        this.Delete();
                        break;
                    case Keys.F2:
                    case Keys.Enter:
                        BeginEditNodeName();
                        e.Handled = true;
                        break;
                    case Keys.Up:
                        if (nudgeMods && CanNudgeNode(xn, NudgeDirection.Up))
                        {
                            this.NudgeNode(xn, NudgeDirection.Up);
                            e.Handled = true;
                        }
                        break;
                    case Keys.Down:
                        if (nudgeMods && CanNudgeNode(xn, NudgeDirection.Down))
                        {
                            this.NudgeNode(xn, NudgeDirection.Down);
                            e.Handled = true;
                        }
                        break;
                    case Keys.Left:
                        if (nudgeMods && CanNudgeNode(xn, NudgeDirection.Left))
                        {
                            this.NudgeNode(xn, NudgeDirection.Left);
                            e.Handled = true;
                        }
                        break;
                    default:
                        if (!e.Handled)
                        {
                            nodeTextView_KeyDown(sender, e);
                        }
                        break;
                }
            }
            else if (!e.Handled)
            {
                base.OnKeyDown(e);
            }

        }

        public void BeginEditNodeName()
        {
            TreeNode n = this._myTreeView.SelectedNode;
            if (!this.IsEditing && n != null && n.IsLabelEditable)
            {
                n.BeginEdit();
            }
        }

        private void nodeTextView_KeyDown(object sender, KeyEventArgs e)
        {
            _tip.Hide();
            CurrentEvent.Event = e;
            if (!this.IsEditing)
            {
                bool ctrlMods = e.Modifiers == Keys.Control || e.Modifiers == (Keys.Control | Keys.Shift);
                Keys key = (e.KeyData & ~Keys.Modifiers);
                switch (key)
                {
                    case Keys.Left:
                        if (_nodeTextView.Focused)
                        {
                            this._myTreeView.Focus();
                            e.Handled = true;
                        }
                        break;
                    case Keys.Delete:
                        this.Delete();
                        e.Handled = true;
                        return;
                    case Keys.X:
                        if (ctrlMods)
                        {
                            this.Cut();
                            e.Handled = true;
                        }
                        break;
                    case Keys.C:
                        if (ctrlMods)
                        {
                            this.Copy();
                            e.Handled = true;
                        }
                        break;
                    case Keys.V:
                        if (ctrlMods)
                        {
                            this.Paste(InsertPosition.Child);
                            e.Handled = true;
                        }
                        break;
                    default:
                        if (!e.Handled)
                        {
                            this._myTreeView.HandleKeyDown(e);
                        }
                        break;
                }
            }
            if (!e.Handled)
            {
                base.OnKeyDown(e);
            }
        }

        private void OnSettingsChanged(object sender, string name)
        {
            bool update = false;
            // change the node colors.
            if (name == "LightColors" || name == "DarkColors" || name == "Theme" || name == "Colors")
            {
                var theme = (ColorTheme)this.Settings["Theme"];
                var colorSetName = theme == ColorTheme.Light ? "LightColors" : "DarkColors";
                var colors = (ThemeColors)this.Settings[colorSetName];
                Color backColor = colors.Background;
                this.BackColor = backColor;
                this._myTreeView.BackColor = backColor;
                this._nodeTextView.BackColor = backColor;

                Color foreColor = colors.Text;
                this._myTreeView.ForeColor = foreColor;
                this._nodeTextView.ForeColor = foreColor;
                update = true;
            }

            if (name == "Font")
            {
                // this.Font = (Font)this._settings["Font"];
                update = true;
            }
            if (name == "TreeIndent")
            {
                int indent = this.Settings.GetInteger("TreeIndent");
                this._myTreeView.TreeIndent = indent;
                update = true;
            }
            if (name == "SchemaAwareNames")
            {
                this._schemaAwareNames = new HashSet<string>(this._settings.GetString("SchemaAwareNames").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.InvariantCultureIgnoreCase);
                update = true;
            }
            if (name == "SchemaAwareText")
            {
                this._showSchemaAwareText = this._settings.GetBoolean("SchemaAwareText");
                update = true;
            }

            if (update)
            {
                this._myTreeView.BeginUpdate();
                InvalidateNodes(this._myTreeView.Nodes); // force nodes to pick up new colors.
                this._myTreeView.EndUpdate();
            }
        }

        void InvalidateNodes(TreeNodeCollection nodes)
        {
            if (nodes == null) return;
            foreach (XmlTreeNode xn in nodes)
            {
                if (xn.IsVisible)
                {
                    xn.Invalidate();
                    if (xn.IsExpanded)
                    {
                        InvalidateNodes(xn.Children);
                    }
                }
            }
        }

        enum DragKeyState
        {
            LeftButton = 1,
            RightButton = 2,
            Shift = 4,
            Control = 8,
            MiddleButton = 16,
            Alt = 32
        }

        static TreeData CheckDragEvent(DragEventArgs e)
        {
            TreeData data = null;
            string name = DataFormats.GetFormat(typeof(TreeData).FullName).Name;
            try
            {
                if (e.Data.GetDataPresent(name, false))
                {
                    data = (TreeData)e.Data.GetData(name);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception:" + ex.ToString());
            }
            if (data == null && e.Data.GetDataPresent(DataFormats.Text))
            {
                string xml = (string)e.Data.GetData(DataFormats.Text);
                data = new TreeData(xml);
            }
            if (data != null)
            {
                DragKeyState ks = (DragKeyState)e.KeyState;
                // Copy when the control key is down.
                e.Effect = ((ks & DragKeyState.Control) != DragKeyState.Control) ?
                    DragDropEffects.Move : DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
            return data;
        }

        private void treeViewFeedback_DragDrop(object sender, DragEventArgs e)
        {
            TreeData data = CheckDragEvent(e);
            FinishDragDrop(data, e.Effect);
        }

        private void treeViewFeedback_DragEnter(object sender, DragEventArgs e)
        {
            TreeData data = CheckDragEvent(e);
            if (data != null && this._feedback == null)
            {
                this._feedback = new XmlTreeViewDropFeedback();
                if (this._dragged == null)
                {
                    // dragging from another app, so we have to import the node at this point.
                    XmlTreeNode target = (XmlTreeNode)this._myTreeView.FindNodeAt(e.X, e.Y);
                    this._dragged = data.GetTreeNode(this.Model.Document, target, this);
                }
                this._feedback.Item = this._dragged;
                this._feedback.TreeView = this._myTreeView;
            }
            if (this._feedback != null)
            {
                this._feedback.Position = new Point(e.X, e.Y);
            }
        }

        private void treeViewFeedback_DragLeave(object sender, EventArgs e)
        {
            RemoveFeedback();
        }

        private void treeViewFeedback_DragOver(object sender, DragEventArgs e)
        {
            // find the node under the X,Y position and draw feedback as to where the new node will
            // be dropped. 
            if (this._feedback != null)
            {
                CheckDragEvent(e);
                this._feedback.Position = new Point(e.X, e.Y);
            }
        }

        private void myTreeView_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            e.UseDefaultCursors = true;
        }

        private void myTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this._dragged = (XmlTreeNode)e.Item;
                this._myTreeView.SelectedNode = this._dragged;
                TreeData data = new TreeData(this._dragged);
                DragDropEffects effect = this.DoDragDrop(data, DragDropEffects.All);
                if (this._dragged != null && effect != DragDropEffects.None)
                {
                    FinishDragDrop(data, effect);
                }
                RemoveFeedback();

            }
        }
        void RemoveFeedback()
        {
            if (this._feedback != null)
            {
                this._feedback.Finish(this._dragged != null);
                this._feedback.Dispose();
                this._feedback = null;
            }
        }

        protected void FinishDragDrop(TreeData data, DragDropEffects effect)
        {
            if (data != null && effect != DragDropEffects.None && this._dragged != null)
            {
                bool copy = (effect == DragDropEffects.Copy);
                if (this._feedback != null)
                {
                    // Then we are also the drop site
                    MoveNode cmd = null;
                    if (this._feedback.Before != null)
                    {
                        cmd = MoveNode(this._dragged, (XmlTreeNode)this._feedback.Before, InsertPosition.Before, copy);
                    }
                    else if (this._feedback.After != null)
                    {
                        cmd = MoveNode(this._dragged, (XmlTreeNode)this._feedback.After, InsertPosition.After, copy);
                    }
                    // Now we can expand it because it is now in the tree
                    if (cmd != null && cmd.Source.Children.Count > 1)
                    {
                        cmd.Source.Expand();
                    }
                }
                else if (!copy)
                {
                    // Then this was a move to another process, so now we have to remove it
                    // from this process.
                    Debug.Assert(this._myTreeView.SelectedNode == this._dragged);
                    this.Delete();
                }
            }
            this._dragged = null;
            RemoveFeedback();
        }

        private MoveNode MoveNode(XmlTreeNode source, XmlTreeNode dest, InsertPosition where, bool copy)
        {
            try
            {
                MoveNode cmd = new MoveNode(this, source, dest, where, copy);
                this.UndoManager.Push(cmd);
                return cmd;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, SR.MoveErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return null;
        }

        public bool CanNudgeNode(XmlTreeNode node, NudgeDirection dir)
        {
            if (node == null) return false;
            NudgeNode n = new NudgeNode(this, node, dir);
            return n.IsEnabled;
        }

        public void NudgeNode(XmlTreeNode node, NudgeDirection dir)
        {
            try
            {
                NudgeNode cmd = new NudgeNode(this, node, dir);
                this.UndoManager.Push(cmd);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, SR.NudgeErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        bool reentrantLock = false;

        public void RecalculateNamespaces(XmlNode node)
        {
            if (node is XmlText) node = node.ParentNode;
            if (node.NodeType != XmlNodeType.Element && node.NodeType != XmlNodeType.Attribute)
            {
                return;
            }
            if (reentrantLock) return;

            Command exec = this.UndoManager.Executing;
            if (exec != null && exec.Name == "Edit Namespace")
                return; // don't "redo" this during Commnad.Redo()! 

            this.SuspendLayout();
            this._myTreeView.BeginUpdate();
            this._model.BeginUpdate();

            Cursor.Current = Cursors.WaitCursor;

            // Do not re-enter this when we are processing the recalcNamespaces compound command!
            reentrantLock = true;
            try
            {
                // This xmlns attribute has changed, so we need to recalculate the NamespaceURI
                // property on the scoped element and it's children so that validation works
                // as expected.  
                XmlElement scope;
                if (node is XmlAttribute)
                {
                    scope = ((XmlAttribute)node).OwnerElement;
                }
                else
                {
                    scope = (XmlElement)node;
                }
                if (scope == null) return;
                XmlTreeNode tnode = FindNode(scope);
                if (tnode == null) return;
                if (tnode.Node == null) return;

                XmlNamespaceManager nsmgr = XmlHelpers.GetNamespaceScope(scope);
                CompoundCommand cmd = new CompoundCommand("Edit Namespace");
                tnode.RecalculateNamespaces(nsmgr, cmd);
                if (cmd.Count > 0)
                {
                    this.UndoManager.Merge(cmd);
                }
            }
            finally
            {
                reentrantLock = false;
                this._myTreeView.EndUpdate();
                this.ResumeLayout();
                this._myTreeView.Invalidate();
                this._model.EndUpdate();
                Cursor.Current = Cursors.Arrow;
            }
        }

        //=======================================================================================
        // DO NOT EDIT BELOW THIS LINE
        //=======================================================================================

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private System.Windows.Forms.ImageList imageList1;
        private System.ComponentModel.IContainer components;
        private PaneResizer resizer;
        private System.Windows.Forms.VScrollBar vScrollBar1;
        private System.Windows.Forms.HScrollBar hScrollBar1;

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(XmlTreeView));
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.vScrollBar1 = new System.Windows.Forms.VScrollBar();
            this.hScrollBar1 = new System.Windows.Forms.HScrollBar();
            this.resizer = new XmlNotepad.PaneResizer();
            this._myTreeView = new XmlNotepad.TreeView();
            this._nodeTextView = new XmlNotepad.NodeTextView();
            this.SuspendLayout();
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "Element.png");
            this.imageList1.Images.SetKeyName(1, "RedBall.png");
            this.imageList1.Images.SetKeyName(2, "BlueBall.png");
            this.imageList1.Images.SetKeyName(3, "Text.png");
            this.imageList1.Images.SetKeyName(4, "GreenBall.png");
            this.imageList1.Images.SetKeyName(5, "PurpleBall.png");
            this.imageList1.Images.SetKeyName(6, "Open.png");
            this.imageList1.Images.SetKeyName(7, "cdata.png");
            // 
            // vScrollBar1
            // 
            this.vScrollBar1.AccessibleName = "VScrollBar";
            this.vScrollBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.vScrollBar1.Location = new System.Drawing.Point(477, 0);
            this.vScrollBar1.Name = "vScrollBar1";
            this.vScrollBar1.Size = new System.Drawing.Size(20, 224);
            this.vScrollBar1.TabIndex = 0;
            this.vScrollBar1.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vScrollBar1_Scroll);
            // 
            // hScrollBar1
            // 
            this.hScrollBar1.AccessibleName = "HScrollBar";
            this.hScrollBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.hScrollBar1.Location = new System.Drawing.Point(0, 204);
            this.hScrollBar1.Name = "hScrollBar1";
            this.hScrollBar1.Size = new System.Drawing.Size(179, 20);
            this.hScrollBar1.TabIndex = 2;
            this.hScrollBar1.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hScrollBar1_Scroll);
            // 
            // resizer
            // 
            this.resizer.AccessibleName = "XmlTreeResizer";
            this.resizer.Border3DStyle = System.Windows.Forms.Border3DStyle.Flat;
            this.resizer.Location = new System.Drawing.Point(200, 0);
            this.resizer.Name = "resizer";
            this.resizer.Pane1 = this._myTreeView;
            this.resizer.Pane2 = this._nodeTextView;
            this.resizer.PaneWidth = 5;
            this.resizer.Size = new System.Drawing.Size(5, 408);
            this.resizer.TabIndex = 3;
            this.resizer.Vertical = true;
            // 
            // myTreeView
            // 
            this._myTreeView.AccessibleName = "TreeView";
            this._myTreeView.AccessibleRole = System.Windows.Forms.AccessibleRole.List;
            this._myTreeView.ImageList = this.imageList1;
            this._myTreeView.LabelEdit = true;
            this._myTreeView.LineColor = System.Drawing.SystemColors.ControlDark;
            this._myTreeView.Location = new System.Drawing.Point(0, 0);
            this._myTreeView.MouseDownEditDelay = 400;
            this._myTreeView.Name = "myTreeView";
            this._myTreeView.Nodes = null;
            this._myTreeView.ScrollPosition = new System.Drawing.Point(0, 0);
            this._myTreeView.SelectedNode = null;
            this._myTreeView.Size = new System.Drawing.Size(216, 224);
            this._myTreeView.TabIndex = 1;
            this._myTreeView.TreeIndent = 12;
            this._myTreeView.VirtualHeight = 0;
            this._myTreeView.VirtualWidth = 0;
            // 
            // nodeTextView
            // 
            this._nodeTextView.AccessibleDescription = "Right hand side of the XmlTreeView for editing node values";
            this._nodeTextView.AccessibleName = "NodeTextView";
            this._nodeTextView.AccessibleRole = System.Windows.Forms.AccessibleRole.List;
            this._nodeTextView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._nodeTextView.BackColor = System.Drawing.Color.White;
            this._nodeTextView.Location = new System.Drawing.Point(301, 0);
            this._nodeTextView.Name = "nodeTextView";
            this._nodeTextView.Nodes = null;
            this._nodeTextView.ScrollPosition = new System.Drawing.Point(0, 0);
            this._nodeTextView.SelectedNode = null;
            this._nodeTextView.Size = new System.Drawing.Size(179, 224);
            this._nodeTextView.TabIndex = 4;
            // 
            // XmlTreeView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this.vScrollBar1);
            this.Controls.Add(this.hScrollBar1);
            this.Controls.Add(this.resizer);
            this.Controls.Add(this._nodeTextView);
            this.Controls.Add(this._myTreeView);
            this.Name = "XmlTreeView";
            this.Size = new System.Drawing.Size(496, 224);
            this.ResumeLayout(false);

        }

        public void BeginUpdate()
        {
            this._updating++;
            this.Model.BeginUpdate();
        }

        public void EndUpdate()
        {
            this._updating--;
            this.Model.EndUpdate();
        }

        public void BeginSave()
        {
            this._saving = true;
        }

        public void EndSave()
        {
            this._saving = false;
        }

        internal XmlTreeNode FindElementById(string id)
        {
            HashSet<string> names = new HashSet<string>();
            if (this.Model != null && this.Model.SchemaCache != null)
            {
                foreach (XmlSchemaAttribute a in this.Model.SchemaCache.GetIdAttributes())
                {
                    names.Add(a.Name);
                }
            }

            return this.FindIdAttribute(names, id);
        }
        #endregion
    }

    public enum NodeImage
    {
        None,
        Element,
        Attribute,
        Leaf,
        Text,
        Comment,
        PI,
        OpenElement,
        CData,
    }

    public class XmlTreeNode : TreeNode, IXmlTreeNode
    {
        private Settings _settings;
        private NodeImage _img;
        private Color _foreColor;
        internal List<XmlTreeNode> _children;
        private XmlTreeView _view;
        private XmlNode _node;
        private XmlSchemaType _type;
        private XmlNodeType _nodeType;
        private string _editLabel;
        private string _schemaAwareText;
        private Color _schemaAwareColor;

        public XmlTreeNode(XmlTreeView view)
        {
            this._view = view;
        }

        public XmlTreeNode(XmlTreeView view, XmlNode node)
        {
            this._view = view;
            this._node = node;
            Init();
        }

        public XmlTreeNode(XmlTreeView view, XmlTreeNode parent, XmlNode node)
            : base(parent)
        {
            this._view = view;
            this._node = node;
            Init();
        }

        public IXmlTreeNode ParentNode => this.Parent as IXmlTreeNode;

        [Browsable(false)]
        public XmlNodeType NodeType
        {
            get { return (this._node != null) ? this._node.NodeType : this._nodeType; }
            set { this._nodeType = value; }
        }

        [Browsable(false)]
        public XmlTreeView XmlTreeView
        {
            get { return this._view; }
            set
            {
                this._view = value;
                this.TreeView = value == null ? null : value.TreeView;
                PropagateView(value, _children);
                Init();
            }
        }

        public IEnumerable<IXmlTreeNode> Nodes
        {
            get
            {
                foreach (var item in this.Children)
                {
                    yield return item as IXmlTreeNode;
                }
            }
        }

        public override void Remove()
        {
            base.Remove();
            XmlTreeNode xp = this.Parent as XmlTreeNode;
            if (xp != null)
            {
                xp.OnChildRemoved();
            }
        }

        void OnChildRemoved()
        {
            if (this._img == NodeImage.Element && this.Children.Count == 0)
            {
                MakeLeaf();
            }
        }

        void MakeLeaf()
        {
            this.Collapse();
            this._img = NodeImage.Leaf;
            this.Invalidate();
        }

        [Browsable(false)]
        public XmlSchemaType SchemaType
        {
            get { return this._type; }
            set { this._type = value; }
        }

        void PropagateView(XmlTreeView view, List<XmlTreeNode> children)
        {
            if (children != null)
            {
                foreach (XmlTreeNode child in children)
                {
                    child.XmlTreeView = view;
                    PropagateView(view, child._children);
                }
            }
        }

        void Init()
        {
            if (this._view != null)
            {
                this._settings = _view.Settings;
                this._img = CalculateNodeImage(this.Node);
                this.InitForeColor(this._img);
                this._schemaAwareText = null;
            }
        }

        internal string GetSchemaAwareText()
        {
            string id = GetIdAttributeValue();
            if (!string.IsNullOrEmpty(id))
            {
                return id;
            }
            TreeNodeCollection nodes = GetChildren(this);
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (!string.IsNullOrEmpty(node.Label) && this._view.SchemaAwareNames.Contains(node.Label))
                    {
                        return node.Text;
                    }
                }
            }
            // must not return null, as the difference between null and string.Empty is
            // used to determine if this has already been computed for efficiency reasons.
            return string.Empty; 
        }

        public override void Invalidate()
        {
            base.Invalidate();
            Init();
            this.XmlTreeView.SyncScrollbars();
        }

        public Settings Settings { get { return this._settings; } }

        public XmlNode Node
        {
            get { return this._node; }
            set
            {
                this._node = value;
                int count = this.Children.Count;
                Init();
                this.Invalidate();
                if (this.TreeView != null)
                {
                    this.TreeView.InvalidateLayout(); // LabelBounds needs recalculating.                
                    this.TreeView.InvalidateNode(this);
                }
            }
        }
        public override string Label
        {
            get
            {
                if (this.Node == null) return _editLabel;

                var name = this.Node.Name;
                if (name == "#cdata-section")
                {
                    // make it less verbose.
                    name = "#cdata";
                }
                return name;
            }
            set
            {
                _editLabel = value;
                this.Invalidate();
            }
        }

        public override string Label2 {
            get {
                // very important this is lazily constructed only for visible nodes because it is expensive!
                if (this._schemaAwareText == null && this._view?.ShowSchemaAwareText == true)
                {
                    this._schemaAwareText = this.GetSchemaAwareText();
                    if (!string.IsNullOrEmpty(this._schemaAwareText))
                    {
                        this._schemaAwareText = ": " + this._schemaAwareText;
                    }
                }
                return this._schemaAwareText;
            }
            set => this._schemaAwareText = value;
        }

        public override Color Label2Color => this._schemaAwareColor;

        public override bool IsLabelEditable
        {
            get
            {
                return (this._node == null || this._node is XmlProcessingInstruction ||
                    ((this._node is XmlAttribute || this._node is XmlElement)));
            }
        }

        public override string Text
        {
            get
            {
                XmlNode n = this.Node;
                string text = null;
                if (n is XmlElement)
                {
                    NodeImage i = this.NodeImage;
                    if (NodeImage.Element != i && NodeImage.OpenElement != i)
                    {
                        text = n.InnerText;
                    }
                }
                else if (n is XmlProcessingInstruction)
                {
                    text = ((XmlProcessingInstruction)n).Data;
                }
                else if (n != null)
                {
                    text = n.Value;
                }
                return text;
            }
            set
            {
                EditNodeValue ev = new EditNodeValue(this.XmlTreeView, this, value);
                UndoManager undo = this.XmlTreeView.UndoManager;
                undo.Push(ev);
            }
        }

        public override Color ForeColor
        {
            get
            {
                return this._foreColor;
            }
        }

        public override TreeNodeCollection Children
        {
            get
            {
                return new XmlTreeNodeCollection(this);
            }
        }

        public NodeImage NodeImage
        {
            get
            {
                if (this.IsExpanded)
                {
                    return NodeImage.OpenElement;
                }
                return this._img;
            }
        }

        public override int ImageIndex
        {
            get
            {
                return (int)this.NodeImage - 1;
            }
        }

        public void InitForeColor(NodeImage img)
        {
            var theme = (ColorTheme)this._settings["Theme"];
            var colorSetName = theme == ColorTheme.Light ? "LightColors" : "DarkColors";
            ThemeColors colors = (ThemeColors)this._settings[colorSetName];
            Color color = colors.Text;
            switch (img)
            {
                case NodeImage.Element:
                case NodeImage.OpenElement:
                case NodeImage.Leaf:
                    color = colors.Element;
                    break;
                case NodeImage.Attribute:
                    color = colors.Attribute;
                    break;
                case NodeImage.PI:
                    color = colors.PI;
                    break;
                case NodeImage.CData:
                    color = colors.CDATA;
                    break;
                case NodeImage.Comment:
                    color = colors.Comment;
                    break;
            }
            this._foreColor = color;
            this._schemaAwareColor = colors.SchemaAwareTextColor;
        }

        NodeImage CalculateNodeImage(XmlNode n)
        {
            XmlNodeType nt = (n == null) ? this._nodeType : n.NodeType;
            switch (nt)
            {
                case XmlNodeType.Attribute:
                    return NodeImage.Attribute;
                case XmlNodeType.Comment:
                    return NodeImage.Comment;
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.XmlDeclaration:
                    return NodeImage.PI;
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    return NodeImage.Text;
                case XmlNodeType.CDATA:
                    return NodeImage.CData;
                case XmlNodeType.Element:
                    XmlElement e = (XmlElement)n;
                    if (e != null && IsContainer(e))
                    {
                        return NodeImage.Element;
                    }
                    return NodeImage.Leaf;
                default:
                    return NodeImage.PI;
            }
        }

        static bool IsContainer(XmlElement e)
        {
            if (e.HasChildNodes)
            {
                int count = 0;
                foreach (XmlNode c in e.ChildNodes)
                {
                    if (c is XmlComment || c is XmlProcessingInstruction || c is XmlElement)
                        return true;
                    if (c is XmlText || c is XmlCDataSection)
                    {
                        count++;
                        if (count > 1) return true;
                    }
                }
            }
            return HasSpecifiedAttributes(e);
        }

        static bool HasSpecifiedAttributes(XmlElement e)
        {
            if (e.HasAttributes)
            {
                foreach (XmlAttribute a in e.Attributes)
                {
                    if (a.Specified) return true;
                }
            }
            return false;
        }

        public void RecalculateNamespaces(XmlNamespaceManager nsmgr, CompoundCommand cmd)
        {
            Debug.Assert(this.NodeType == XmlNodeType.Element || this.NodeType == XmlNodeType.Attribute);
            XmlNode e = this.Node;
            if (e == null) return; // user is still editing this tree node!
            bool hasXmlNs = false;
            if (e.Attributes != null)
            {
                XmlAttributeCollection acol = e.Attributes;
                for (int i = 0, n = acol.Count; i < n; i++)
                {
                    XmlAttribute a = acol[i];
                    string value = a.Value;
                    if (a.NamespaceURI == XmlStandardUris.XmlnsUri)
                    {
                        if (!hasXmlNs)
                        {
                            nsmgr.PushScope();
                            hasXmlNs = true;
                        }
                        XmlNameTable nt = nsmgr.NameTable;
                        string prefix = nt.Add(a.LocalName);
                        if (prefix == "xmlns") prefix = "";
                        if (!nsmgr.HasNamespace(prefix))
                        {
                            try
                            {
                                nsmgr.AddNamespace(prefix, nt.Add(value));
                            }
                            catch (Exception ex)
                            {
                                // illegal namespace declaration, perhaps user has not finished editing it yet.
                                Trace.WriteLine(ex.Message);
                            }
                        }
                    }
                }
            }

            XmlName name = XmlHelpers.ParseName(nsmgr, e.Name, this.NodeType);
            if (name.NamespaceUri != e.NamespaceURI &&
                (string.IsNullOrEmpty(name.Prefix) || !string.IsNullOrEmpty(name.NamespaceUri)))
            {
                // Node has bound to a different namespace!
                // Note that XmlNode doesn't let you change the NamespaceURI property
                // so we have to recreate the XmlNode objects, and so we have to create
                // a command for this since it is editing the tree!
                EditNodeName rename = new EditNodeName(this, name, false);
                cmd.Add(rename);
            }

            foreach (XmlTreeNode child in this.Children)
            {
                switch (child.NodeType)
                {
                    case XmlNodeType.Attribute:
                    case XmlNodeType.Element:
                        child.RecalculateNamespaces(nsmgr, cmd);
                        break;
                    default:
                        // no change required on text nodes.
                        break;
                }
            }

            if (hasXmlNs)
            {
                nsmgr.PopScope();
            }
        }

        public override bool CanExpandAll
        {
            get
            {
                return (this.NodeImage != NodeImage.Leaf); // don't expand the leaves
            }
        }

        public virtual string GetDefinition()
        {
            string ipath = this.GetIncludePath();
            if (string.IsNullOrEmpty(ipath))
            {
                ipath = this.GetSchemaLocation();
            }
            if (string.IsNullOrEmpty(ipath))
            {
                var e = this.GetIdRef();
                if (e != null)
                {
                    this.XmlTreeView.SelectedNode = e;
                    return null;
                }
            }
            if (string.IsNullOrEmpty(ipath))
            {
                ipath = this.GetTypeInfo();
            }

            // todo: other forms of goto...
            return ipath;
        }

        public virtual string GetIncludePath()
        {

            if (this.NodeType == XmlNodeType.Attribute)
            {
                XmlTreeNode e = this.Parent as XmlTreeNode;
                return e.GetIncludePath();
            }

            XmlNode n = this.Node;
            if (n != null && n.NamespaceURI == XmlIncludeReader.XIncludeNamespaceUri)
            {
                string href = n.Attributes["href"].Value;
                Uri resolved = ResolveUri(href);
                return resolved.LocalPath;
            }
            return null;
        }

        public virtual string GetSchemaLocation()
        {
            XmlNode n = this.Node;
            if (this.NodeType == XmlNodeType.Attribute)
            {
                if (n.NamespaceURI == "http://www.w3.org/2001/XMLSchema-instance")
                {
                    if (n.LocalName == "noNamespaceSchemaLocation")
                    {
                        string href = n.Value;
                        Uri resolved = ResolveUri(href);
                        return resolved.LocalPath;
                    }
                }
            }
            return null;
        }

        public virtual XmlSchemaAnnotated GetSchemaObject()
        {
            if (this.Node == null)
            {
                return null;
            }
            XmlSchemaInfo si = this.XmlTreeView.Model.GetTypeInfo(this.Node);
            if (si != null)
            {
                XmlSchemaElement e = si.SchemaElement;
                if (e != null) return e;

                XmlSchemaAttribute a = si.SchemaAttribute;
                if (a != null) return a;

                XmlSchemaSimpleType st = si.MemberType;
                if (st != null) return st;

                return si.SchemaType;
            }
            return null;
        }

        public virtual string GetIdAttributeValue()
        {
            XmlSchemaAttribute found = null;
            XmlSchemaObject o = GetSchemaObject();
            if (o is XmlSchemaElement e && e.ElementSchemaType is XmlSchemaComplexType ct)
            {
                foreach (var attr in ct.Attributes)
                {
                    if (attr is XmlSchemaAttribute a && a.SchemaTypeName.Name == "ID")
                    {
                        found = a;
                        break;
                    }
                }
            }

            if (found != null)
            {
                // then this is an ID attribute so return the attribute value.
                return this.GetAttributeValue(found.Name);
            }
            return null;
        }

        public virtual XmlTreeNode GetIdRef()
        {
            XmlSchemaAttribute found = null;
            XmlSchemaObject o = GetSchemaObject();
            if (o is XmlSchemaElement e)
            {
                if (e.ElementSchemaType is XmlSchemaComplexType ct)
                {
                    foreach (var attr in ct.Attributes)
                    {
                        if (attr is XmlSchemaAttribute a && a.SchemaTypeName.Name == "IDREF")
                        {
                            found = a;
                            break;
                        }
                    }
                }
            }
            else if (o is XmlSchemaAttribute a)
            {
                found = a;
            }

            if (found != null && found.SchemaTypeName.Name == "IDREF")
            {
                // then this is an IDREF we can jump to some other place in the document that has the matching "ID".
                string id = this.GetAttributeValue(found.Name);
                if (this._view != null)
                {
                    return _view.FindElementById(id);
                }
            }
            return null;
        }

        private string GetAttributeValue(string name)
        {
            if (this.Node is XmlElement e && e.HasAttributes)
            {
                return e.GetAttribute(name);
            }
            else if (this.Node is XmlAttribute a && a.Name == name)
            {
                return a.Value;
            }
            return null;
        }
        

        public virtual string GetTypeInfo()
        {
            XmlSchemaObject o = GetSchemaObject();
            if (o != null) return GetSourceUri(o);
            return null;
        }

        public virtual string GetToolTip()
        {
            if (this._node == null) return null;
            XmlSchemaInfo si = this.XmlTreeView.Model.GetTypeInfo(this.Node);
            if (si != null)
            {
                List<XmlSchemaAnnotated> toSearch = GetTypeInfoSearch(si);

                foreach (XmlSchemaAnnotated a in toSearch)
                {
                    string s = SchemaCache.GetAnnotation(a, SchemaCache.AnnotationNode.Tooltip, (string)_settings["Language"]);
                    if (!string.IsNullOrEmpty(s)) return s;
                }
            }
            return null;
        }

        private List<XmlSchemaAnnotated> GetTypeInfoSearch(XmlSchemaInfo si)
        {

            List<XmlSchemaAnnotated> toSearch = new List<XmlSchemaAnnotated>();

            // expand also the "RefName" referenced elements or attributes.                
            if (si.SchemaElement != null)
            {
                toSearch.Add(si.SchemaElement);
                if (si.SchemaElement.RefName != null && !si.SchemaElement.RefName.IsEmpty)
                {
                    // find the referenced element.
                    XmlSchemaElement re = this.XmlTreeView.Model.GetElementType(si.SchemaElement.RefName);
                    if (re != null)
                    {
                        toSearch.Add(re);
                    }
                }
            }
            if (si.SchemaAttribute != null)
            {
                toSearch.Add(si.SchemaAttribute);
                if (si.SchemaAttribute.RefName != null && !si.SchemaAttribute.RefName.IsEmpty)
                {
                    // find the referenced attribute.
                    XmlSchemaAttribute ra = this.XmlTreeView.Model.GetAttributeType(si.SchemaAttribute.RefName);
                    if (ra != null)
                    {
                        toSearch.Add(ra);
                    }
                }
            }
            if (si.MemberType != null)
            {
                toSearch.Add(si.MemberType);
            }
            if (si.SchemaType != null)
            {
                toSearch.Add(si.SchemaType);
            }
            return toSearch;
        }

        public virtual XmlDocument GetDocumentation()
        {
            if (this._node == null) return null;
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement root = xmlDoc.CreateElement("documentation");
            xmlDoc.AppendChild(root);
            XmlSchemaInfo si = this.XmlTreeView.Model.GetTypeInfo(this.Node);
            bool found = false;
            if (si != null)
            {
                List<XmlSchemaAnnotated> toSearch = GetTypeInfoSearch(si);

                foreach (XmlSchemaAnnotated a in toSearch)
                {
                    if (a != null)
                    {
                        foreach (XmlSchemaDocumentation d in SchemaCache.GetDocumentation(a, (string)_settings["Language"]))
                        {
                            if (null != d && d.Markup != null && d.Markup.Length > 0)
                            {
                                foreach (XmlNode n in d.Markup)
                                {
                                    XmlNode node = xmlDoc.ImportNode(n, true);
                                    root.AppendChild(node);
                                    found = true;
                                }
                            }
                            root.AppendChild(xmlDoc.CreateElement("br"));
                        }
                    }
                    if (found)
                    {
                        return xmlDoc;
                    }
                }
            }
            return null;
        }

        string GetSourceUri(XmlSchemaObject o)
        {
            while (o != null)
            {
                string s = o.SourceUri;
                if (!string.IsNullOrEmpty(s)) return s;
                o = o.Parent;
            }
            return null;
        }

        public Uri ResolveUri(string href)
        {
            XmlNode n = this.Node;
            Uri baseUri = new Uri(this.XmlTreeView.Model.FileName);
            if (n != null)
            {
                string uri = n.BaseURI;
                if (!string.IsNullOrEmpty(uri))
                    baseUri = new Uri(uri);
            }
            Uri resolved = new Uri(baseUri, href);
            return resolved;

        }
    }

    public class XmlTreeNodeCollection : TreeNodeCollection, IEnumerable<XmlTreeNode>
    {
        private XmlTreeNode _parent;
        private XmlNode _node;
        private XmlTreeView _treeView;
        private List<XmlTreeNode> _children;

        public XmlTreeNodeCollection(XmlTreeView treeView, XmlNode parent)
        {
            this._node = parent;
            this._treeView = treeView;
        }

        public XmlTreeNodeCollection(XmlTreeNode parent)
        {
            this._treeView = parent.XmlTreeView;
            this._parent = parent;
            if (parent != null) this._children = parent._children;
            this._node = parent.Node;
        }

        IEnumerator<XmlTreeNode> IEnumerable<XmlTreeNode>.GetEnumerator()
        {
            Populate();
            return this._children.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            IEnumerable<XmlTreeNode> ie = (IEnumerable<XmlTreeNode>)this;
            return ie.GetEnumerator();
        }

        public override IEnumerator<TreeNode> GetEnumerator()
        {
            System.Collections.IEnumerable e = (System.Collections.IEnumerable)this;
            foreach (XmlTreeNode xn in e)
            {
                yield return xn;
            }
        }

        public override int Count
        {
            get
            {
                Populate();
                return this._children == null ? 0 : this._children.Count;
            }
        }

        public override int GetIndex(TreeNode node)
        {
            Populate();
            XmlTreeNode xn = ((XmlTreeNode)node);
            return this._children.IndexOf(xn);
        }

        public override void Add(TreeNode node)
        {
            node.Parent = this._parent;
            XmlTreeNode xn = ((XmlTreeNode)node);
            xn.XmlTreeView = this._treeView;
            Populate();
            this._children.Add(xn);
        }

        public override void Insert(int i, TreeNode node)
        {
            node.Parent = this._parent;
            XmlTreeNode xn = ((XmlTreeNode)node);
            xn.XmlTreeView = this._treeView;
            Populate();
            if (i > this._children.Count) i = this._children.Count;
            this._children.Insert(i, xn);
        }

        public override void Remove(TreeNode child)
        {
            if (this._children != null)
            {
                XmlTreeNode xn = ((XmlTreeNode)child);
                if (this._children.Contains(xn))
                {
                    this._children.Remove(xn);
                    return;
                }
            }
            throw new InvalidOperationException(SR.NotAChild);
        }

        public override TreeNode this[int i]
        {
            get
            {
                Populate();
                return this._children[i] as TreeNode;
            }
        }

        void Populate()
        {
            if (this._children == null)
            {
                List<XmlTreeNode> children = new List<XmlTreeNode>();
                if (_node != null && !(_node is XmlAttribute))
                {
                    if (_node.Attributes != null)
                    {
                        foreach (XmlAttribute a in _node.Attributes)
                        {
                            if (a.Specified)
                            {
                                XmlTreeNode c = _treeView.CreateTreeNode(_parent, a);
                                c.XmlTreeView = this._treeView;
                                children.Add(c);
                            }
                        }
                    }
                    if (_node.HasChildNodes)
                    {
                        foreach (XmlNode n in _node.ChildNodes)
                        {
                            XmlTreeNode c = _treeView.CreateTreeNode(_parent, n);
                            c.XmlTreeView = this._treeView;
                            children.Add(c);
                        }
                    }
                }
                this._children = children;
                if (_parent != null) _parent._children = children;
                if (this._treeView != null) this._treeView.PerformLayout();
            }
        }


    }

    public sealed class StringHelper
    {
        StringHelper() { }

        public static bool IsNullOrEmpty(string s)
        {
            return (s == null || s.Length == 0);
        }
    }

    public class NodeChangeEventArgs : EventArgs
    {
        private XmlTreeNode _node;

        public XmlTreeNode Node
        {
            get { return _node; }
            set { _node = value; }
        }

        public NodeChangeEventArgs(XmlTreeNode node)
        {
            this._node = node;
        }
    }

    public class NodeSelectedEventArgs : EventArgs
    {
        private XmlTreeNode _node;

        public XmlTreeNode Node
        {
            get { return _node; }
            set { _node = value; }
        }

        public NodeSelectedEventArgs(XmlTreeNode node)
        {
            this._node = node;
        }
    }

    public class XmlTreeViewDropFeedback : TreeViewDropFeedback
    {
        private int _autoScrollCount;

        public override Point Position
        {
            get { return base.Position; }
            set
            {
                base.Position = value;
                CheckAutoScroll(value);
            }
        }

        void CheckAutoScroll(Point pt)
        {
            TreeView tv = this.TreeView;
            Point local = tv.PointToClient(pt);
            Point pos = tv.ApplyScrollOffset(local);
            XmlTreeView parent = (XmlTreeView)tv.Parent;
            int height = tv.ItemHeight;
            int halfheight = height / 2;
            TreeNode node = null;
            bool nearEnd = false;
            if (local.Y - height < tv.Top)
            {
                node = tv.FindNodeAt(pos.X, pos.Y - height);
                nearEnd = (local.Y - halfheight < tv.Top);
            }
            else if (local.Y + height > tv.Bottom)
            {
                node = tv.FindNodeAt(pos.X, pos.Y + height);
                nearEnd = (local.Y + halfheight > tv.Bottom);
            }
            if (node != null)
            {
                if (nearEnd || _autoScrollCount > 1)
                {
                    parent.ScrollIntoView(node);
                    _autoScrollCount = 0;
                }
                else
                {
                    _autoScrollCount++;
                }
                ResetToggleCount();
            }
        }
    }
}