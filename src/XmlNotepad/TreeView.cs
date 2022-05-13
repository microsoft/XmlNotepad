using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace XmlNotepad
{
    // This file contains a new implementation of TreeView that virtualizes the storage of the
    // tree node data so it can come from a separate model, for example, an XmlDocument.
    // It also removes some limitations that TreeView has like maximum of height of 32k pixels.
    public class TreeView : UserControl, IEditableView
    {
        private ImageList _imageList;
        private bool _editable;
        private TreeNode _focus;
        private ArrayList _selection = new ArrayList();
        private TreeNodeCollection _nodes;
        private Color _lineColor = SystemColors.ControlDark;
        private int _treeIndent = 12;
        private TypeToFindHandler _ttf;
        private readonly TextEditorOverlay _editor;
        internal TreeViewDropFeedback _dff;
        private readonly Timer _timer = new Timer();
        private int _mouseDownEditDelay = 400;
        private Point _scrollPosition;
        private AccessibleTree _acc;

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public event EventHandler<NodeLabelEditEventArgs> BeforeLabelEdit;
        public event EventHandler<NodeLabelEditEventArgs> AfterLabelEdit;
        public event EventHandler<TreeViewEventArgs> BeforeCollapse;
        public event EventHandler<TreeViewEventArgs> AfterCollapse;
        public event EventHandler<TreeViewEventArgs> BeforeExpand;
        public event EventHandler<TreeViewEventArgs> AfterExpand;
        public event EventHandler<TreeViewEventArgs> AfterSelect;
        public event EventHandler AfterBatchUpdate;
        public event ItemDragEventHandler ItemDrag;

        public TreeView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Selectable, true);

            InitializeComponent();

            this.SuspendLayout();
            this._editor = new TextEditorOverlay(this);
            this._editor.AutoSize = true;
            this._editor.CommitEdit += new EventHandler<TextEditorEventArgs>(OnCommitEdit);
            this._editor.LayoutEditor += new EventHandler<TextEditorLayoutEventArgs>(OnLayoutEditor);

            _ttf = new TypeToFindHandler(this, 2000);
            _ttf.FindString += new TypeToFindEventHandler(FindString);
            _timer.Tick += new EventHandler(timer_Tick);

            this.AccessibleRole = AccessibleRole.List;
            this.AccessibleName = "TreeView";
            this.ResumeLayout();
        }

        #region Component Designer generated code

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (this._ttf != null)
            {
                this._ttf.Dispose();
                this._ttf = null;
            }
            base.Dispose(disposing);
        }

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();

            this.AccessibleRole = AccessibleRole.List;
            this.Name = this.AccessibleName = "TreeView";

            this.ResumeLayout(false);
        }

        #endregion

        public void Close()
        {
            this._editor.Dispose();
        }

        public void SetSite(ISite site)
        {
            this.Site = this._editor.Site = site;
        }

        public Point ScrollPosition
        {
            get { return this._scrollPosition; }
            set
            {
                this._scrollPosition = value;
                Invalidate();
            }
        }

        public TextEditorOverlay Editor
        {
            get { return this._editor; }
        }

        public void StartIncrementalSearch()
        {
            _ttf.StartIncrementalSearch();
            this.Focus();
        }

        void FindString(object sender, string toFind)
        {
            TreeNode node = this.SelectedNode;
            if (node == null) node = this.FirstVisibleNode;
            TreeNode start = node;
            while (node != null)
            {
                string s = node.Label;
                if (s != null && s.StartsWith(toFind, StringComparison.CurrentCultureIgnoreCase))
                {
                    this.SelectedNode = node;
                    return;
                }
                node = node.NextVisibleNode;
                if (node == null) node = this.FirstVisibleNode;
                if (node == start)
                    break;
            }
        }

        public int MouseDownEditDelay
        {
            get { return this._mouseDownEditDelay; }
            set { this._mouseDownEditDelay = value; }
        }

        internal void OnRemoveNode(TreeNode node)
        {
            if (node != null && this.SelectedNode != null &&
                (node == this.SelectedNode || node.Contains(this.SelectedNode)))
            {
                TreeNodeCollection col = (node.Parent == null) ? this.Nodes : node.Parent.Children;
                if (col != null)
                {
                    int count = col.Count;
                    TreeNode selected;
                    if (node.Index == count - 1)
                    {
                        selected = node.PrevVisibleNode;
                    }
                    else
                    {
                        // get next visible node after this one (and after all it's children).
                        TreeNode next = col[node.Index + 1];
                        selected = (!next.IsVisible) ? next.NextVisibleNode : next;
                    }
                    this.SelectedNode = selected;
                }
                else
                {
                    this.SelectedNode = null;
                }
            }
            InvalidateLayout();
        }

        internal void OnBeforeExpand(TreeNode node)
        {
            if (this.BeforeExpand != null) this.BeforeExpand(this, new TreeViewEventArgs(node, TreeViewAction.Expanded));
        }
        internal void OnAfterExpand(TreeNode node)
        {
            if (this.AfterExpand != null) this.AfterExpand(this, new TreeViewEventArgs(node, TreeViewAction.Expanded));
        }
        internal void OnBeforeCollapse(TreeNode node)
        {
            if (this.BeforeCollapse != null) this.BeforeCollapse(this, new TreeViewEventArgs(node, TreeViewAction.Collapsed));
        }
        internal void OnAfterCollapse(TreeNode node)
        {
            if (this.AfterCollapse != null) this.AfterCollapse(this, new TreeViewEventArgs(node, TreeViewAction.Collapsed));

            TreeNode sel = this.SelectedNode;
            if (sel != null && node.Contains(sel))
            {
                this.SelectedNode = node;
            }
        }

        public TreeNodeCollection Nodes
        {
            get { return this._nodes; }
            set
            {
                ClearSelection();
                this._nodes = value;
                PerformLayout();
            }
        }

        public TreeNode SelectedNode
        {
            get { return _selection.Count > 0 ? (TreeNode)_selection[0] : null; }
            set
            {
                if (this.SelectedNode != value)
                {
                    if (this.InBatchUpdate && this._selection.Count == 1)
                    {
                        if (value == null)
                        {
                            this._selection.Clear();
                            this._focus = null;
                        }
                        else
                        {
                            this._selection[0] = value;
                            this._focus = value;
                        }
                    }
                    else
                    {
                        ClearSelection();
                        if (value != null)
                        {
                            _selection.Add(value);
                        }
                        SetFocus(value);
                        OnSelectionChanged();
                    }
                }
            }
        }

        public void OnSelectionChanged()
        {
            if (AfterSelect != null) AfterSelect(this, new TreeViewEventArgs(this.SelectedNode, TreeViewAction.None));
        }

        protected override void OnGotFocus(EventArgs e)
        {
            if (this.SelectedNode == null)
            {
                SelectedNode = this.FirstVisibleNode;
            }
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
        }

        Point mouseDown;
        Point lastMousePos;
        TreeNode downSel;
        TreeNode downNode;
        TreeNode hitNode;

        public Point ApplyScrollOffset(int x, int y)
        {
            return new Point(x - this._scrollPosition.X, y - this._scrollPosition.Y);
        }
        public Point ApplyScrollOffset(Point p)
        {
            return new Point(p.X - this._scrollPosition.X, p.Y - this._scrollPosition.Y);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            CurrentEvent.Event = e;
            EndEdit(false);

            if (this.downNode != null) return; // work around for unit testing problem...

            Point p = ApplyScrollOffset(e.X, e.Y);
            int x = p.X; int y = p.Y;
            bool wasFocussed = this.Focused;
            this.Focus();
            TreeNode node = this.FindNodeAt(x, y);
            TreeNode sel = this.SelectedNode;
            this.SelectedNode = node;
            this.downSel = this.downNode = null;
            this.hitNode = null;
            if (e.Button == MouseButtons.Left)
            {
                if (node != null)
                {
                    Size imgSize = GetImageSize();
                    int lineIndent = (imgSize.Width / 2);
                    int indent = TreeIndent + lineIndent + BoxWidth;
                    if (e.Clicks > 1)
                    {
                        node.Toggle();
                    }
                    else if (wasFocussed && node.LabelAndImageBounds(imgSize, indent).Contains(x, y))
                    {
                        this.hitNode = node;
                        if (node.LabelBounds.Contains(x, y))
                        {
                            this.downSel = sel;
                            this.downNode = node;
                            mouseDown = new Point(x, y);
                        }
                    }
                    else
                    {
                        Rectangle r = GetBoxBounds(this.Margin.Left + lineIndent, node.LabelBounds.Top, this.ItemHeight, node.Depth, indent);
                        int slop = (this.ItemHeight - r.Height) / 2;
                        r.Inflate(slop, slop); // make it a bit easier to hit.
                        if (r.Contains(x, y))
                        {
                            node.Toggle();
                        }
                    }
                }
            }
        }


#if !WHIDBEY
        Rectangle Margin = new Rectangle(3,3,3,3);
#endif

        const int DragThreshold = 5;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point pt = ApplyScrollOffset(e.X, e.Y);
            this.lastMousePos = pt;
            bool left = e.Button == MouseButtons.Left;
            if (left && hitNode != null)
            {
                int dx = pt.X - mouseDown.X;
                int dy = pt.Y - mouseDown.Y;
                if (Math.Sqrt((double)(dx * dx + dy * dy)) > DragThreshold)
                {
                    if (this.ItemDrag != null)
                    {
                        this.ItemDrag(this, new ItemDragEventArgs(e.Button, hitNode));
                    }
                    this.downNode = this.downSel = null;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && this.downSel != null)
            {
                Point p = ApplyScrollOffset(e.X, e.Y);
                int x = p.X; int y = p.Y;
                TreeNode node = this.FindNodeAt(x, y);
                if (node != null && node.LabelBounds.Contains(x, y))
                {
                    mouseDown = new Point(e.X, e.Y);
                    if (this.downSel == node)
                    {
                        _timer.Interval = this._mouseDownEditDelay;
                        _timer.Start();
                        _timer.Enabled = true;
                        return;
                    }
                }
                this.downSel = null;
            }
            this.downNode = null;
            this.hitNode = null;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            if (this.downSel != null && this.downSel == this.SelectedNode && this.downSel.LabelBounds.Contains(this.lastMousePos))
            {
                this.BeginEdit(null);
            }
            this.downSel = null;
            this.downNode = null;
            this.hitNode = null;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = (keyData & ~Keys.Modifiers);
            switch (key)
            {
                case Keys.Home:
                case Keys.End:
                case Keys.Down:
                case Keys.Up:
                case Keys.PageDown:
                case Keys.PageUp:
                case Keys.Right:
                case Keys.Left:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            CurrentEvent.Event = e;
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                HandleKeyDown(e);
            }
        }

        public void BubbleKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
        }

        public void HandleKeyDown(KeyEventArgs e)
        {

            TreeNode sel = this.SelectedNode;

            bool isLetterOrDigit = ((e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z) ||
                                   (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)) &&
                                   (e.Modifiers == Keys.Shift || e.Modifiers == 0);

            if (e.Modifiers == Keys.Control && (e.KeyCode == Keys.Home || e.KeyCode == Keys.End))
            {
                // drop through in this case.
            }
            else if (e.Modifiers != 0 && !isLetterOrDigit)
            {
                // Reserve use of modifiers for other things, like multi-select expansion and so on.
                return;
            }

            TreeNode n = this.SelectedNode;

            switch (e.KeyCode)
            {
                case Keys.Left:
                    if (sel != null)
                    {
                        if (sel.IsExpanded)
                        {
                            sel.Collapse();
                        }
                        else if (sel.Parent != null)
                        {
                            this.SelectedNode = sel.Parent;
                        }
                    }
                    e.Handled = true;
                    break;
                case Keys.Right:
                    if (sel != null && !sel.IsExpanded)
                    {
                        sel.Expand();
                    }
                    e.Handled = true;
                    break;
                case Keys.Up:
                    if (sel != null)
                    {
                        TreeNode prev = sel.PrevVisibleNode;
                        if (prev != null)
                        {
                            this.SelectedNode = prev;
                        }
                    }
                    else
                    {
                        this.SelectedNode = this.LastVisibleNode;
                    }
                    e.Handled = true;
                    break;
                case Keys.Down:
                    if (sel != null)
                    {
                        TreeNode next = sel.NextVisibleNode;
                        if (next != null)
                        {
                            this.SelectedNode = next;
                        }
                    }
                    else
                    {
                        this.SelectedNode = this.FirstVisibleNode;
                    }
                    e.Handled = true;
                    break;
                case Keys.Home:
                    this.SelectedNode = this.FirstVisibleNode;
                    e.Handled = true;
                    break;
                case Keys.End:
                    this.SelectedNode = this.LastVisibleNode;
                    e.Handled = true;
                    break;
                case Keys.PageDown:
                    this.HandlePageDown();
                    e.Handled = true;
                    return;
                case Keys.PageUp:
                    this.HandlePageUp();
                    e.Handled = true;
                    return;

                case Keys.Multiply:
                    if (n != null)
                    {
                        n.ExpandAll();
                        e.Handled = true;
                    }
                    return;
                case Keys.Add:
                    if (n != null)
                    {
                        n.Expand();
                        e.Handled = true;
                    }
                    return;
                case Keys.Subtract:
                    if (n != null)
                    {
                        n.Collapse();
                        e.Handled = true;
                    }
                    break;
                default:
                    if (isLetterOrDigit && !e.Handled && this.ContainsFocus)
                    {
                        if (_ttf.Started)
                        {
                            e.Handled = true;
                        }
                        else if (!this.IsEditing)
                        {
                            char ch = Convert.ToChar(e.KeyValue);
                            if (!e.Shift) ch = Char.ToLower(ch);
                            if (Char.IsLetter(ch) && this.BeginEdit(ch.ToString()))
                            {
                                this._editor.SelectEnd();
                                e.Handled = true;
                            }
                        }
                    }
                    break;
            }
        }

        public void HandlePageDown()
        {
            int visibleRows = VisibleRows;
            TreeNode sel = this.SelectedNode;
            if (sel == null)
            {
                sel = this.FirstVisibleNode;
            }
            if (sel != null)
            {
                TreeNode n = sel.NextVisibleNode;
                while (n != null && visibleRows > 0)
                {
                    if (n.NextVisibleNode == null)
                        break;
                    n = n.NextVisibleNode;
                    visibleRows--;
                }
                if (n != null)
                {
                    this.SelectedNode = n;
                }
            }
        }

        public void HandlePageUp()
        {
            int visibleRows = VisibleRows;
            TreeNode sel = this.SelectedNode;
            if (sel == null)
                sel = this.LastVisibleNode;

            if (sel != null)
            {
                TreeNode n = sel;
                while (n != null && visibleRows > 0)
                {
                    if (n.PrevVisibleNode == null)
                        break;
                    n = n.PrevVisibleNode;
                    visibleRows--;
                }
                if (n != null)
                {
                    this.SelectedNode = n;
                }
            }
        }

        public int VisibleRows
        {
            get
            {
                return this.ClientRectangle.Height / this.ItemHeight;
            }
        }

        void OnLayoutEditor(object sender, TextEditorLayoutEventArgs args)
        {
            if (this.SelectedNode == null)
                return;
            Rectangle r = this.SelectedNode.LabelBounds;
            r.Offset(this._scrollPosition);
            args.PreferredBounds = r;
            r.Width = this.Width - r.Left + this.Left - 20;
            args.MaxBounds = r;
        }

        #region IEditableView

        public bool BeginEdit(string value)
        {
            TreeNode sel = this.SelectedNode;
            if (this._editable && sel != null && sel.IsLabelEditable)
            {
                string text = value != null ? value : sel.Label;
                if (this.BeforeLabelEdit != null)
                {
                    NodeLabelEditEventArgs args = new NodeLabelEditEventArgs(sel, text);
                    this.BeforeLabelEdit(this, args);
                    if (args.CancelEdit)
                        return false;
                }

                IIntellisenseProvider provider = this.GetService(typeof(IIntellisenseProvider)) as IIntellisenseProvider;
                if (provider != null)
                {
                    provider.SetContextNode(sel as IXmlTreeNode);
                    if (!provider.IsNameEditable)
                        return false;
                }
                this._editor.BeginEdit(text, provider, EditMode.Name, sel.ForeColor, this.Focused);
                return true;
            }
            return false;
        }


        public bool IsEditing
        {
            get
            {
                return this._editor.IsEditing;
            }
        }

        public void SelectText(int index, int length)
        {
            if (this._editor.IsEditing)
            {
                this._editor.Select(index, length);
            }
        }

        public bool ReplaceText(int index, int length, string replacement)
        {
            if (this._editor.IsEditing)
            {
                bool rc = this._editor.Replace(index, length, replacement);
                this.EndEdit(false);
                return rc;
            }
            return false;
        }

        public Rectangle EditorBounds
        {
            get
            {
                return this._editor.Bounds;
            }
        }

        public bool EndEdit(bool cancel)
        {
            return this._editor.EndEdit(cancel);
        }

        public int SelectionStart { get { return this._editor.SelectionStart; } }

        public int SelectionLength { get { return this._editor.SelectionLength; } }

        #endregion

        void OnCommitEdit(object sender, TextEditorEventArgs args)
        {
            TreeNode sel = this.SelectedNode;
            if (sel != null && this.IsEditing)
            {
                //string text = sel.Label;
                bool cancel = args.Cancelled;
                if (this.AfterLabelEdit != null)
                {
                    NodeLabelEditEventArgs a = new NodeLabelEditEventArgs(sel, args.Text);
                    a.CancelEdit = cancel;
                    this.AfterLabelEdit(this, a);
                    cancel = args.Cancelled = a.CancelEdit;
                }
                if (!cancel)
                {
                    sel.Label = args.Text;
                    // [chris] this breaks the find dialog...
                    //this.Focus();
                }
                InvalidateLayout(); // LabelBounds needs recalculating.
                InvalidateNode(sel);
            }
        }


        void ClearSelection()
        {
            EndEdit(true);
            EndEdit(false);
            this._focus = null;
            foreach (TreeNode node in _selection)
            {
                InvalidateNode(node);
            }
            _selection.Clear();
        }

        void SetFocus(TreeNode node)
        {
            if (this._focus != node)
            {
                InvalidateNode(this._focus);
                this._focus = node;
                InvalidateNode(node);
                EnsureVisible(node);
            }
        }

        public static void EnsureVisible(TreeNode node)
        {
            TreeNode p = node.Parent;
            while (p != null)
            {
                if (!p.IsExpanded)
                {
                    p.Expand();
                }
                p = p.Parent;
            }

        }

        public void InvalidateNode(TreeNode node)
        {
            if (node != null)
            {
                Rectangle r = new Rectangle(0, node.LabelBounds.Top, this.Width, this.ItemHeight);
                r.Offset(this._scrollPosition);
                Invalidate(r);
            }
        }

        public TreeNode[] GetSelectedNodes()
        {
            return (TreeNode[])_selection.ToArray(typeof(TreeNode));
        }

        public void SetSelectedNodes(TreeNode[] value)
        {
            ClearSelection();
            if (value != null)
            {
                _selection = new ArrayList(value);
                foreach (TreeNode node in value)
                {
                    InvalidateNode(node);
                }
            }
            this.OnSelectionChanged();
        }

        public bool IsSelected(TreeNode node)
        {
            return this._selection != null && this._selection.Contains(node);
        }

        public int ItemHeight
        {
            get
            {
                return this.Font.Height;
            }
        }

        // Dead code
        //public TreeNode GetTopNode(Rectangle bounds) {
        //    int y = 0;
        //    return this.FindTopNode(this.nodes, bounds, ref y);
        //}

        public bool LabelEdit
        {
            get { return this._editable; }
            set { this._editable = value; }
        }

        public ImageList ImageList
        {
            get { return this._imageList; }
            set { this._imageList = value; }
        }

        public TreeNode FirstVisibleNode
        {
            get
            {
                if (this._nodes == null) return null;
                foreach (TreeNode node in this._nodes)
                {
                    if (node.IsVisible)
                    {
                        return node;
                    }
                }
                return null;
            }
        }

        public TreeNode LastVisibleNode
        {
            get
            {
                return TreeNode.GetLastVisibleNode(this.Nodes);
            }
        }

        int updateDepth;

        public void InvalidateLayout()
        {
            if (updateDepth == 0)
            {
                this.PerformLayout();
                Invalidate();
            }
        }

        public bool InBatchUpdate
        {
            get { return this.updateDepth > 0; }
        }

        public void BeginUpdate()
        {
            this.updateDepth++;
        }

        public void EndUpdate()
        {
            this.updateDepth--;
            if (updateDepth == 0)
            {
                TreeNode node = this.SelectedNode;
                // fix up selection & layout.
                ClearSelection();
                _selection.Add(node);
                SetFocus(node);
                OnSelectionChanged();
                this.PerformLayout();
                Invalidate();
            }
            if (AfterBatchUpdate != null)
                AfterBatchUpdate(this, EventArgs.Empty);
        }

        public void ExpandAll()
        {
            ExpandAll(this, this.Nodes);
        }

        public virtual void ExpandAll(TreeView view, TreeNodeCollection nodes)
        {
            if (nodes == null || nodes.Count == 0) return;
            if (view != null) view.BeginUpdate();
            try
            {
                foreach (TreeNode n in nodes)
                {
                    if (n.Children.Count > 0 && n.CanExpandAll)
                    {
                        if (!n.IsExpanded) n.Expand();
                        ExpandAll(view, n.Children);
                    }
                }
            }
            finally
            {
                if (view != null) view.EndUpdate();
            }
        }

        public void CollapseAll()
        {
            CollapseAll(this, this.Nodes);
        }

        public virtual void CollapseAll(TreeView view, TreeNodeCollection nodes)
        {
            if (nodes == null) return;
            if (view != null) view.BeginUpdate();
            try
            {
                foreach (TreeNode n in nodes)
                {
                    if (n.IsExpanded)
                    {
                        CollapseAll(view, n.Children);
                        n.Collapse();
                    }
                }
            }
            finally
            {
                if (view != null) view.EndUpdate();
            }
        }

        public int TreeIndent
        {
            get { return this._treeIndent; }
            set { this._treeIndent = value; }
        }

        public Color LineColor
        {
            get { return this._lineColor; }
            set { this._lineColor = value; }
        }

        Pen linePen;
        Pen plusPen;
        Pen boxPen;
        Brush backBrush;

        protected override void OnPaint(PaintEventArgs e)
        {
            //PerfTimer t = new PerfTimer();
            //t.Start();
            //base.OnPaint(e);

            if (this._nodes == null) return;
            Graphics g = e.Graphics;
            //g.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF clipF = g.ClipBounds;
            Matrix m = g.Transform;
            m.Translate(this._scrollPosition.X, this._scrollPosition.Y);
            g.Transform = m;

            this.linePen = new Pen(this.LineColor, 1);
            this.linePen.DashStyle = DashStyle.Dot;
            this.linePen.LineJoin = LineJoin.Round;

            this.plusPen = new Pen(this.ForeColor);
            this.plusPen.Alignment = PenAlignment.Center;
            this.boxPen = new Pen(this.LineColor);
            this.boxPen.Alignment = PenAlignment.Inset;
            this.backBrush = new SolidBrush(this.BackColor);

            Rectangle clip = new Rectangle((int)clipF.X - this._scrollPosition.X, (int)clipF.Y - this._scrollPosition.Y, (int)clipF.Width, (int)clipF.Height);
            g.FillRectangle(backBrush, clip);

            //Trace.WriteLine("MyTreeView: clip="+clip.ToString());

            DrawNodes(g, ref clip, new LineStates(), TreeIndent, 0, this._nodes);

            if (this._focus != null && !this._editor.IsEditing)
            {
                Rectangle r = this._focus.LabelBounds;
                if (clip.IntersectsWith(r))
                {
                    Pen focusPen = new Pen(Color.Black, 1);
                    focusPen.DashStyle = DashStyle.Dot;
                    using (focusPen)
                    {
                        g.DrawRectangle(focusPen, r.Left, r.Top, r.Width - 1, r.Height - 1);
                    }
                }
            }

            if (_dff != null)
            {
                _dff.Draw(g);
            }

            linePen.Dispose();
            linePen = null;
            plusPen.Dispose();
            plusPen = null;
            boxPen.Dispose();
            boxPen = null;
            backBrush.Dispose();
            backBrush = null;

            //t.Stop();
            //Trace.WriteLine("MyTreeView.OnPaint: " + t.GetDuration());
        }

        int DrawNodes(Graphics g, ref Rectangle clip, LineStates state, int indent, int y, TreeNodeCollection nodes)
        {
            if (nodes == null) return y;
            Font f = this.Font;
            Size imgSize = GetImageSize();
            int count = nodes.Count;
            int pos = 0;
            int h = this.ItemHeight;
            //int w = this.Width;
            int x = this.Margin.Left;
            int lineIndent = (imgSize.Width / 2);

            foreach (TreeNode node in nodes)
            {
                if (node.IsVisible)
                {
                    LineState ls = LineState.None;
                    if (pos == 0) ls |= LineState.First;
                    if (pos + 1 == count) ls |= LineState.Last;
                    state.Push(ls);
                    //Rectangle bounds = new Rectangle(0, y, w, h);                    
                    bool visible = (y + h) >= clip.Top && y <= clip.Bottom; // clip.IntersectsWith(bounds);
                    if (visible)
                    {
                        int index = node.ImageIndex;
                        Image img = index < 0 ? null : this._imageList.Images[index];
                        bool isSelected = this.IsSelected(node);
                        node.Draw(g, f, linePen, state, h, indent + lineIndent + BoxWidth, x, y, ref imgSize, img, isSelected);
                    }
                    int y2 = y;
                    y += h;
                    if (node.Children.Count > 0)
                    {
                        int depth = state.Depth - 1;
                        if (node.IsExpanded)
                        {
                            y = DrawNodes(g, ref clip, state, indent, y, node.Children);
                            // Draw boxes on the way out.
                            if (visible)
                            {
                                DrawPlusMinus(g, x + lineIndent, y2, h, depth, indent + lineIndent + BoxWidth, false);
                            }
                        }
                        else
                        {
                            if (visible)
                            {
                                DrawPlusMinus(g, x + lineIndent, y2, h, depth, indent + lineIndent + BoxWidth, true);
                            }
                        }
                    }
                    state.Pop();
                    if (y > clip.Bottom)
                        return y; // no point continuing...
                }
                pos++;
            }
            return y;
        }

        // Plus minux box dimensions.
        const int BoxWidth = 8;
        const int BoxHeight = 8;
        const int PlusWidth = 4;
        const int PlusHeight = 4;

        static Rectangle GetBoxBounds(int margin, int y, int height, int depth, int indent)
        {
            int x = margin + (depth * indent) - (BoxWidth / 2);
            y += (height - BoxHeight) / 2; // center it vertically.
            return new Rectangle(x, y, BoxWidth, BoxHeight);
        }

        void DrawPlusMinus(Graphics g, int x, int y, int height, int depth, int indent, bool plus)
        {
            // todo: scale box based on font size?

            Rectangle box = GetBoxBounds(x, y, height, depth, indent);
            x = box.Left;
            y = box.Top;
            int dx = (BoxWidth - PlusWidth) / 2;
            int dy = (BoxHeight - PlusHeight) / 2;
            int y2 = y + (BoxHeight / 2);
            int x2 = x + (BoxWidth / 2);
            g.FillRectangle(this.backBrush, box);
            g.DrawRectangle(this.boxPen, box);
            g.DrawLine(this.plusPen, x + dx, y2, x + dx + PlusWidth, y2);
            if (plus)
            {
                g.DrawLine(this.plusPen, x2, y + dy, x2, y + dy + PlusHeight);
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            this.virtualHeight = 0;
            if (this._nodes != null)
            {
                Size imgSize = GetImageSize();
                int lineIndent = (imgSize.Width / 2);
                Graphics g = this.CreateGraphics();
                using (g)
                {
                    int w = this.virtualWidth;
                    this.virtualWidth = 0;
                    this.virtualHeight = LayoutNodes(g, TreeIndent + lineIndent + BoxWidth, 1, 0, this.Nodes);
                    if (w != this.virtualWidth)
                    {
                        this.Parent.PerformLayout();
                    }
                }
            }
        }

        int virtualHeight;

        public int VirtualHeight
        {
            get { return virtualHeight; }
            set { virtualHeight = value; }
        }


        int virtualWidth;

        public int VirtualWidth
        {
            get { return virtualWidth; }
            set { virtualWidth = value; }
        }


        int LayoutNodes(Graphics g, int indent, int depth, int y, TreeNodeCollection nodes)
        {
            Size imgSize = new Size();
            if (this._imageList != null)
            {
                imgSize = this._imageList.ImageSize;
            }
            int x = this.Margin.Left;
            int h = this.ItemHeight;
            Font f = this.Font;

            foreach (TreeNode node in nodes)
            {
                if (node.IsVisible)
                {
                    node.Layout(g, f, h, x, indent, depth, y, imgSize);
                    y += h;
                    this.virtualWidth = Math.Max(this.virtualWidth, node.LabelBounds.Right);
                    if (node.IsExpanded && node.Children.Count > 0)
                    {
                        y = LayoutNodes(g, indent, depth + 1, y, node.Children);
                    }
                    node._bottom = y;
                }
            }
            return y;
        }

        // Dead code!
        //TreeNode FindTopNode(TreeNodeCollection nodes, Rectangle visibleBounds, ref int y) {
        //    if (nodes == null) return null;
        //    int h = this.ItemHeight;
        //    int w = this.Width;
        //    foreach (TreeNode node in this.nodes) {
        //        if (node.IsVisible) {
        //            Rectangle bounds = new Rectangle(0, y, w, h);
        //            if (visibleBounds.IntersectsWith(bounds)) {
        //                return node;
        //            }
        //            if (node.IsExpanded && node.Nodes.Count > 0) {
        //                TreeNode child = FindTopNode(node.Nodes, visibleBounds, ref y);
        //                if (child != null) return child;
        //            }
        //            y += h;
        //        }
        //    }
        //    return null;
        //}

        Size GetImageSize()
        {
            Size imgSize = new Size();
            if (this._imageList != null)
            {
                imgSize = this._imageList.ImageSize;
                int imgHeight = imgSize.Height;
                int imgWidth = imgSize.Width;
                int h = this.ItemHeight;
                if (imgHeight > h)
                {
                    // scale image down to fit.
                    imgWidth = (imgWidth * h) / imgHeight;
                    imgHeight = h;
                    imgSize = new Size(imgWidth, imgHeight);
                }
            }
            return imgSize;
        }

        public TreeNode FindNodeAt(int x, int y)
        {
            return FindNodeAt(this.Nodes, x, y);
        }

        TreeNode FindNodeAt(TreeNodeCollection nodes, int x, int y)
        {
            if (nodes != null)
            {
                foreach (TreeNode n in nodes)
                {
                    if (n.IsVisible)
                    {
                        Rectangle r = new Rectangle(0, n.LabelBounds.Top, this.Width, this.ItemHeight);
                        if (r.Contains(x, y))
                        {
                            return n;
                        }
                        if (n.IsExpanded && n.LabelBounds.Top <= y && n._bottom >= y)
                        {
                            TreeNode result = FindNodeAt(n.Children, x, y);
                            if (result != null) return result;
                        }
                    }
                }
            }
            return null;
        }

        protected override AccessibleObject CreateAccessibilityInstance()
        {
            if (this._acc == null) this._acc = new AccessibleTree(this);
            return this._acc;
        }

        protected override AccessibleObject GetAccessibilityObjectById(int objectId)
        {
            return base.GetAccessibilityObjectById(objectId);
        }
    }

    // MyTreeNode is an abstract wrapper on the tree data that keeps track of UI state.
    public abstract class TreeNode
    {
        private TreeNode _parent;
        private bool _visible = true;
        private bool _expanded;
        private bool _selected;
        private Rectangle _labelBounds; // for hit testing
        private TreeView _view;
        internal int _bottom; // Y coordinate of bottom of last grandchild.
        private AccessibleObject _acc;

        protected TreeNode()
        {
        }

        protected TreeNode(TreeNode parent)
        {
            this._parent = parent;
        }

        public int Index
        {
            get
            {
                if (this._parent != null)
                {
                    return this._parent.Children.GetIndex(this);
                }
                else if (this._view != null)
                {
                    return this._view.Nodes.GetIndex(this);
                }
                return -1;
            }
        }

        public AccessibleObject AccessibleObject
        {
            get
            {
                if (this._acc == null) this._acc = new AccessibleNode(this);
                return this._acc;
            }
        }

        public abstract string Label { get; set; }
        public abstract bool IsLabelEditable { get; }
        public abstract TreeNodeCollection Children { get; }
        public abstract int ImageIndex { get; }
        public abstract Color ForeColor { get; }
        public abstract string Text { get; set; }

        TreeNodeCollection ParentCollection
        {
            get
            {
                var parent = this._parent;
                return parent == null ? (_view != null ? _view.Nodes : null) : parent.Children;
            }
        }

        public TreeNode PrevNode
        {
            get
            {
                var col = ParentCollection;
                if (col == null) return null;
                int i = col.GetIndex(this) - 1;
                if (i >= 0)
                {
                    TreeNode n = col[i];
                    return n;
                }
                return _parent;
            }
        }

        public TreeNode NextSiblingNode
        {
            get
            {
                if (this.ParentCollection != null &&
                    this.ParentCollection.Count > this.Index + 1)
                {
                    return this.ParentCollection[this.Index + 1];
                }
                else
                {
                    return null;
                }
            }
        }

        public TreeNode NextNode
        {
            get
            {
                TreeNode child = GetFirstVisibleChild(this);
                if (child != this) return child;
                TreeNode parent = this._parent;
                TreeNode node = this;
                do
                {
                    TreeNodeCollection col = parent == null ? (_view != null ? _view.Nodes : null) : parent.Children;
                    int i = col.GetIndex(node) + 1;
                    int count = col.Count;
                    if (i < count)
                    {
                        TreeNode n = col[i];
                        return n;
                    }
                    node = parent;
                    if (parent != null) parent = parent.Parent as TreeNode;
                } while (node != null);
                return null;
            }
        }

        public TreeView TreeView
        {
            get
            {
                return this._view;
            }
            set
            {
                this._view = value;
            }
        }

        public virtual void RemoveChildren()
        {
            TreeView view = this._view;
            if (view != null) view.BeginUpdate();
            try
            {
                List<TreeNode> snapshot = new List<TreeNode>(this.Children);
                foreach (TreeNode child in snapshot)
                {
                    child.Remove();
                }
            }
            finally
            {
                if (view != null) view.EndUpdate();
            }
        }

        public virtual void Remove()
        {
            TreeNode parent = this.Parent;
            TreeNodeCollection pc = this.ParentCollection;
            TreeView view = this._view;
            if (view != null) view.BeginUpdate();
            try
            {
                if (view != null)
                {
                    view.OnRemoveNode(this);
                    this._view = null;
                }
                pc.Remove(this);
                if (parent != null && parent.Children.Count == 0 && this._parent.IsExpanded)
                {
                    this._parent.Collapse();
                }
            }
            finally
            {
                if (view != null) view.EndUpdate();
            }
        }

        public virtual TreeNode Parent
        {
            get { return this._parent; }
            set { this._parent = value as TreeNode; }
        }

        public bool IsExpanded
        {
            get { return this._expanded; }
        }

        // Whether to allow this node to be expanded during expand-all.
        public virtual bool CanExpandAll
        {
            get { return true; }
        }

        public bool IsVisible
        {
            get { return this._visible; }
            set
            {
                if (this._visible != value)
                {
                    this._visible = value;
                    if (this._view != null) this._view.InvalidateLayout();
                }
            }
        }

        public bool Selected
        {
            get { return this._selected; }
            set { this._selected = value; }
        }

        public Rectangle LabelBounds
        {
            get { return this._labelBounds; }
            set { 
                this._labelBounds = value;
                if (this.Label == "PLAY" && value.X == 33)
                {
                    Debug.WriteLine(string.Format("{0},{1}", this._labelBounds.X, this._labelBounds.Y));
                }
            }
        }

        public void ExpandAll()
        {
            if (!this.IsExpanded) this.Expand();
            this._view.ExpandAll(this._view, this.Children);
        }


        public void CollapseAll()
        {
            this._view.CollapseAll(this._view, this.Children);
            if (this.IsExpanded)
            {
                this.Collapse();
            }
        }

        internal void Draw(Graphics g, Font f, Pen pen, LineStates state, int lineHeight, int indent, int x, int y, ref Size imgSize, Image img, bool selected)
        {
            int startX = x;
            Debug.Assert(this._view != null);
            for (int i = 0; i < state.Depth; i++)
            {
                LineState ls = state[i];
                int x2 = x + (imgSize.Width / 2);
                int x3 = x + indent;
                int y2 = y + lineHeight / 2;
                int y3 = y + lineHeight;

                bool leaf = i + 1 == state.Depth;

                if (leaf)
                {
                    g.DrawLine(pen, x2, y2, x3, y2); // horizontal bar
                }
                if ((ls & LineState.Last) == LineState.Last)
                {
                    if (((ls & LineState.HasParent) == LineState.HasParent ||
                        ((ls & LineState.First) == 0 && (ls & LineState.Last) == LineState.Last)) && leaf)
                    {
                        g.DrawLine(pen, x2, y, x2, y2); // half vertical bar connecting to parent.
                    }
                }
                else if ((ls & LineState.HasParent) == LineState.HasParent || ((ls & LineState.First) == 0))
                {
                    g.DrawLine(pen, x2, y, x2, y3); // full vertical bar
                }
                else if ((ls & LineState.First) == LineState.First)
                { // we know it's also not the last, so
                    g.DrawLine(pen, x2, y2, x2, y3); // half vertical bar connecting to next child.
                }

                x += indent;
            }
            // draw +/- box
            // draw node image.

            int imgWidth = imgSize.Width;
            int imgHeight = imgSize.Height;
            if (img != null)
            {
                int iy = y;
                if (imgHeight < lineHeight)
                {
                    iy += (lineHeight - imgHeight) / 2; // center it
                }
                Rectangle rect = new Rectangle(x, iy, imgWidth, imgHeight);
                g.DrawImage(img, rect);
            }
            string text = this.Label;
            if (text != null && _view != null)
            {
                Brush brush;
                if (selected && _view.IsEditing)
                {
                    return;
                }
                if (selected && _view.Focused)
                {
                    brush = Brushes.HighlightTextBrush(this.ForeColor);
                    g.FillRectangle(SystemBrushes.Highlight, this._labelBounds);
                }
                else
                {
                    brush = new SolidBrush(this.ForeColor);
                }
                Layout(g, f, lineHeight, startX, indent, state.Depth, y, imgSize);
                g.DrawString(text, f, brush, this._labelBounds.Left, this._labelBounds.Top, StringFormat.GenericTypographic);
                brush.Dispose();
            }
        }

        public void Layout(Graphics g, Font f, int lineHeight, int x, int indent, int depth, int y, Size imgSize)
        {
            int width = 10;
            if (this.Label != null)
            {
                SizeF s = g.MeasureString(this.Label, f);
                width = Math.Max(width, (int)s.Width);
            }
            int gap = imgSize.Width + GetGap(indent); // small gap
            this.LabelBounds = new Rectangle(x + (indent * depth) + gap, y, width, lineHeight);
        }

        internal static int GetGap(int indent)
        {
            return indent / 5;
        }

        public void Toggle()
        {
            if (this.IsExpanded)
            {
                this.Collapse();
            }
            else
            {
                this.Expand();
            }
        }

        public TreeNode PrevVisibleNode
        {
            get
            {
                for (TreeNode n = this.PrevNode; n != null; n = n.PrevNode)
                {
                    if (n.IsVisible)
                    {
                        if (n == this._parent) return n;
                        return GetLastVisibleChild(n);
                    }
                }
                return null;
            }
        }


        public static TreeNode GetLastVisibleNode(TreeNodeCollection nodes)
        {
            if (nodes == null) return null;
            for (int i = (nodes.Count - 1); i >= 0; i--)
            {
                TreeNode child = nodes[i];
                if (child.IsVisible)
                {
                    if (!child.IsExpanded) return child;
                    return GetLastVisibleNode(child.Children);
                }
            }
            return null;
        }

        internal TreeNode GetLastVisibleChild(TreeNode n)
        {
            TreeNode last = n;
            if (n.IsExpanded && n.Children.Count > 0)
            {
                TreeNode child = GetLastVisibleNode(n.Children);
                if (child != null) last = child;
            }
            return last;
        }

        internal static TreeNode GetFirstVisibleChild(TreeNode n)
        {
            if (n.IsExpanded && n.Children.Count > 0)
            {
                foreach (TreeNode child in n.Children)
                {
                    if (child.IsVisible)
                    {
                        return child;
                    }
                }
            }
            return n;
        }


        public TreeNode NextVisibleNode
        {
            get
            {
                for (TreeNode n = this.NextNode; n != null; n = n.NextNode)
                {
                    if (n.IsVisible)
                    {
                        return n;
                    }
                }
                return null;
            }
        }

        public void BeginEdit()
        {
            Debug.Assert(this._view != null);
            if (this._view != null)
            {
                this._view.SelectedNode = this;
                this._view.BeginEdit(null);
            }
        }

        // dead code.
        //public void BeginEdit(string name) {
        //    Debug.Assert(this.view != null);
        //    if (this.view != null) {
        //        this.view.SelectedNode = this;
        //        this.view.BeginEdit(name);
        //    }
        //}

        public bool EndEdit(bool cancel)
        {
            Debug.Assert(this._view != null);
            if (this._view != null)
            {
                return this._view.EndEdit(cancel);
            }
            return true;
        }

        public bool IsEditing
        {
            get { return this._view != null && this._view.IsEditing; }
        }

        public virtual void Expand()
        {
            if (this.Children.Count > 0)
            {
                if (this._view != null) this._view.OnBeforeExpand(this);
                this._expanded = true;
                Invalidate();
                if (this._view != null) this._view.OnAfterExpand(this);
            }
        }

        public virtual void Collapse()
        {
            if (this._expanded)
            {
                if (this._view != null) this._view.OnBeforeCollapse(this);
                this._expanded = false;
                Invalidate();
                if (this._view != null) this._view.OnAfterCollapse(this);
            }
        }

        public virtual void Invalidate()
        {
            if (this._view != null) this._view.InvalidateLayout();
        }

        public int Depth
        {
            get
            {
                int depth = 0;
                TreeNode parent = this.Parent;
                while (parent != null)
                {
                    depth++;
                    parent = parent.Parent;
                }
                return depth;
            }
        }

        public bool Contains(TreeNode node)
        {
            TreeNode parent = node.Parent;
            while (parent != null)
            {
                if (parent == this) return true;
                parent = parent.Parent;
            }
            return false;
        }

        internal Rectangle LabelAndImageBounds(Size imgSize, int indent)
        {
            int gap = GetGap(indent);
            return new Rectangle(this.LabelBounds.Left - imgSize.Width - gap, this.LabelBounds.Top, this.LabelBounds.Width + imgSize.Width, this.LabelBounds.Height);
        }

    }

    public abstract class TreeNodeCollection : IEnumerable<TreeNode>
    {
        public abstract int Count { get; }
        public abstract void Add(TreeNode node);
        public abstract void Insert(int i, TreeNode node);
        public abstract void Remove(TreeNode child);
        public abstract TreeNode this[int i] { get; }
        public abstract int GetIndex(TreeNode node);
        public abstract IEnumerator<TreeNode> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TreeNode>)this).GetEnumerator();
        }
    }

    public class NodeLabelEditEventArgs : EventArgs
    {
        private TreeNode _node;
        private bool _cancel;
        private string _label;

        public NodeLabelEditEventArgs(TreeNode node, string label)
        {
            this._node = node;
            this._label = label;
        }

        public string Label { get { return this._label; } }

        public TreeNode Node { get { return this._node; } }

        public bool CancelEdit
        {
            get { return this._cancel; }
            set { this._cancel = value; }
        }
    }

    public enum TreeViewAction { None, Expanded, Collapsed }

    public class TreeViewEventArgs : EventArgs
    {
        private readonly TreeNode _node;
        private readonly TreeViewAction _action;

        public TreeViewEventArgs(TreeNode node, TreeViewAction action)
        {
            this._node = node;
            this._action = action;
        }
        public TreeViewAction Action { get { return this._action; } }
        public TreeNode Node { get { return this._node; } }
    }

    class AccessibleTree : Control.ControlAccessibleObject
    {
        private readonly TreeView _tree;

        public AccessibleTree(TreeView view) : base(view)
        {
            this._tree = view;
        }
        public override Rectangle Bounds
        {
            get
            {
                return _tree.RectangleToScreen(_tree.ClientRectangle);
            }
        }
        public override string DefaultAction
        {
            get
            {
                return "Toggle";
            }
        }
        public override void DoDefaultAction()
        {
            if (_tree.SelectedNode != null)
            {
                _tree.SelectedNode.Toggle();
            }
        }
        public override int GetChildCount()
        {
            return _tree.Nodes.Count;
        }
        public override AccessibleObject GetChild(int index)
        {
            return _tree.Nodes[index].AccessibleObject;
        }
        public override AccessibleObject GetFocused()
        {
            return GetSelected();
        }
        public override int GetHelpTopic(out string fileName)
        {
            fileName = "TBD";
            return 0;
        }
        public override AccessibleObject GetSelected()
        {
            if (_tree.SelectedNode != null)
            {
                return _tree.SelectedNode.AccessibleObject;
            }
            return this;
        }
        public override AccessibleObject HitTest(int x, int y)
        {
            Point pt = _tree.PointToClient(new Point(x, y));
            pt = _tree.ApplyScrollOffset(pt);
            TreeNode node = _tree.FindNodeAt(pt.X, pt.Y);
            if (node != null)
            {
                return node.AccessibleObject;
            }
            return this;
        }
        public override AccessibleObject Navigate(AccessibleNavigation navdir)
        {
            TreeNode node = null;
            TreeNodeCollection children = _tree.Nodes;
            int count = children.Count;
            switch (navdir)
            {
                case AccessibleNavigation.Down:
                case AccessibleNavigation.FirstChild:
                case AccessibleNavigation.Left:
                    if (count > 0) node = children[0];
                    break;
                case AccessibleNavigation.LastChild:
                    return _tree.Editor.CompletionSet.AccessibilityObject;
                case AccessibleNavigation.Next:
                case AccessibleNavigation.Previous:
                case AccessibleNavigation.Right:
                case AccessibleNavigation.Up:
                    if (count > 0) node = children[count - 1];
                    break;
            }
            if (node != null)
            {
                return node.AccessibleObject;
            }
            return null;
        }
        public override AccessibleObject Parent
        {
            get
            {
                return _tree.Parent.AccessibilityObject;
            }
        }
        public override AccessibleRole Role
        {
            get
            {
                return _tree.AccessibleRole;
            }
        }
        public override void Select(AccessibleSelection flags)
        {
            this._tree.Focus();
        }
        public override AccessibleStates State
        {
            get
            {
                AccessibleStates result = AccessibleStates.Focusable | AccessibleStates.Selectable |
                    AccessibleStates.Sizeable;
                if (_tree.Focused) result |= AccessibleStates.Focused;
                if (!_tree.Visible) result |= AccessibleStates.Invisible;
                return result;
            }
        }

        public override string Value
        {
            get
            {
                return "";
            }
            set
            {
                //???
            }
        }
    }

    class AccessibleNode : AccessibleObject
    {
        private readonly TreeNode _node;

        public AccessibleNode(TreeNode node)
        {
            this._node = node;
        }

        public override Rectangle Bounds
        {
            get
            {
                Rectangle bounds = _node.LabelBounds;
                bounds.Offset(_node.TreeView.ScrollPosition);
                return _node.TreeView.RectangleToScreen(bounds);
            }
        }

        public override string DefaultAction
        {
            get
            {
                return "Toggle";
            }
        }

        public override string Description
        {
            get
            {
                return "TreeNode";
            }
        }

        public override void DoDefaultAction()
        {
            _node.Toggle();
        }

        public override int GetChildCount()
        {
            return _node.Children.Count;
        }

        public override AccessibleObject GetChild(int index)
        {
            return _node.Children[index].AccessibleObject;
        }

        public override AccessibleObject GetFocused()
        {
            return GetSelected();
        }

        public override int GetHelpTopic(out string fileName)
        {
            fileName = "TBD";
            return 0;
        }

        public override AccessibleObject GetSelected()
        {
            if (_node.Selected)
            {
                return this;
            }
            return _node.TreeView.AccessibilityObject.GetSelected();
        }

        public override string Help
        {
            get
            {
                // pack the expanded state in the help field...
                return _node.IsExpanded ? "expanded" : "collapsed";
            }
        }

        public override AccessibleObject HitTest(int x, int y)
        {
            return _node.TreeView.AccessibilityObject.HitTest(x, y);
        }

        public override string KeyboardShortcut
        {
            get
            {
                return "???";
            }
        }

        public override string Name
        {
            get
            {
                return _node.Label;
            }
            set
            {
                // hack alert - this is breaking architectural layering!
                XmlTreeNode xnode = (XmlTreeNode)_node;
                xnode.XmlTreeView.UndoManager.Push(new EditNodeName(xnode, value));
            }
        }

        public override AccessibleObject Navigate(AccessibleNavigation navdir)
        {
            TreeNode result = null;
            TreeNodeCollection children = _node.Children;
            int count = children.Count;
            switch (navdir)
            {
                case AccessibleNavigation.Down:
                case AccessibleNavigation.Next:
                    result = _node.NextVisibleNode;
                    if (result == null)
                    {
                        return _node.TreeView.Editor.CompletionSet.AccessibilityObject;
                    }
                    break;
                case AccessibleNavigation.FirstChild:
                    if (count > 0) result = children[0];
                    if (!_node.IsExpanded) _node.Expand();
                    break;
                case AccessibleNavigation.Left:
                    // like the left key, this navigates up the parent hierarchy.
                    TreeNode parent = _node.Parent as TreeNode;
                    return parent.AccessibleObject;

                case AccessibleNavigation.Right:
                    // hack - this breaks architectural layering!!!
                    // but it's such a cool feature to be able to navigate via accessibility
                    // over to the NodeTextView.
                    if (_node is XmlTreeNode)
                    {
                        XmlTreeNode xn = (XmlTreeNode)_node;
                        AccessibleNodeTextView av = (AccessibleNodeTextView)xn.XmlTreeView.NodeTextView.AccessibilityObject;
                        return av.Wrap(_node);
                    }
                    break;
                case AccessibleNavigation.LastChild:
                    if (count > 0) result = children[count - 1];
                    if (!_node.IsExpanded) _node.Expand();
                    break;
                case AccessibleNavigation.Previous:
                case AccessibleNavigation.Up:
                    result = _node.PrevVisibleNode;
                    break;
            }
            if (result != null)
            {
                return result.AccessibleObject;
            }
            return this;
        }

        public override AccessibleObject Parent
        {
            get
            {
                if (_node.Parent != null)
                {
                    TreeNode parent = _node.Parent as TreeNode;
                    return parent.AccessibleObject;
                }
                else
                {
                    return _node.TreeView.AccessibilityObject;
                }
            }
        }

        public override AccessibleRole Role
        {
            get
            {
                // this gives us tree view, but we then can't do selection
                //return AccessibleRole.OutlineItem;
                // Selection is more useful so we do List item.
                return AccessibleRole.ListItem;
            }
        }

        public override void Select(AccessibleSelection flags)
        {
            _node.TreeView.Focus();
            if ((flags & AccessibleSelection.TakeSelection) != 0 ||
                (flags & AccessibleSelection.AddSelection) != 0)
            {
                _node.TreeView.SelectedNode = _node;
            }
            else if ((flags & AccessibleSelection.RemoveSelection) != 0)
            {
                if (_node.TreeView.SelectedNode == this._node)
                {
                    _node.TreeView.SelectedNode = null;
                }
            }
        }

        public override AccessibleStates State
        {
            get
            {
                AccessibleStates result = AccessibleStates.Focusable | AccessibleStates.Selectable;
                if (_node.Selected) result |= AccessibleStates.Focused | AccessibleStates.Selected;
                if (!_node.IsVisible) result |= AccessibleStates.Invisible;
                if (_node.IsExpanded) result |= AccessibleStates.Expanded;
                else result |= AccessibleStates.Collapsed;
                return result;
            }
        }

        // The ValuePattern on this node should set the node label (node name).
        public override string Value
        {
            get
            {
                //string s = node.Text;
                //if (s == null) s = "";
                //return s;
                return _node.Label;
            }
            set
            {
                //// hack alert - this is breaking architectural layering!
                //XmlTreeNode xnode = (XmlTreeNode)node;
                //XmlTreeView xview = xnode.XmlTreeView;
                //xview.UndoManager.Push(new EditNodeValue(xview, xnode, value));

                // hack alert - this is breaking architectural layering!
                XmlTreeNode xnode = (XmlTreeNode)_node;
                xnode.XmlTreeView.UndoManager.Push(new EditNodeName(xnode, value));
            }
        }
    }

    internal enum LineState { None = 0, First = 1, Last = 2, HasParent = 4 }

    internal class LineStates
    {
        private LineState[] _states = new LineState[10];
        private int _used;

        public void Push(LineState state)
        {
            if (_used > 0) state |= LineState.HasParent;
            if (_states.Length == _used)
            {
                LineState[] na = new LineState[_used * 2];
                Array.Copy(_states, na, _used);
                _states = na;
            }
            _states[_used++] = state;
        }
        public void Pop()
        {
            Debug.Assert(_used > 0);
            if (_used > 0) _used--;
        }
        public LineState this[int depth]
        {
            get
            {
                Debug.Assert(depth < _used);
                return (depth < _used) ? _states[depth] : LineState.None;
            }
        }
        public int Depth { get { return this._used; } }
    }

    public class TreeViewDropFeedback : IDisposable
    {
        private TreeView _treeView;
        private TreeNode _start;
        private Point _position;
        private int _indent;
        private Pen _pen;
        private Brush _brush;
        private GraphicsPath _shape;
        private int _count;
        private Point _lastPos;
        private TreeNode _toggled;
        private TreeNode _lastNode;
        // This indicates where the user has chosen to do the drop, either before the "before"
        // node or "after" the after node.
        private TreeNode _after;
        private TreeNode _before;
        private Rectangle _bounds;

        public Rectangle Bounds
        {
            get { return this._bounds; }
            set
            {
                Invalidate();
                this._bounds = value;
                Invalidate();
            }
        }

        public TreeNode After
        {
            get { return _after; }
            set { _after = value; }
        }

        public TreeNode Before
        {
            get { return _before; }
            set { _before = value; }
        }
        public Point Location
        {
            get { return this.Bounds.Location; }
            set
            {
                Invalidate();
                this._bounds.Location = value;
                Invalidate();
            }
        }
        bool visible;
        public bool Visible
        {
            get
            {
                return this.visible;
            }
            set
            {
                if (this.visible != value)
                {
                    this.visible = value;
                    Invalidate();
                }
            }
        }

        void Invalidate()
        {
            if (this._treeView != null)
            {
                Rectangle r = Bounds;
                r.Offset(this._treeView.ScrollPosition);
                this._treeView.Invalidate(r);
            }
        }

        public TreeViewDropFeedback()
        {
            _pen = new Pen(Color.Navy);
            _pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            _brush = new SolidBrush(Color.FromArgb(50, Color.Navy));
            this.Visible = false;
        }

        ~TreeViewDropFeedback()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_pen != null)
            {
                _pen.Dispose();
                _pen = null;
            }
            if (_brush != null)
            {
                _brush.Dispose();
                _brush = null;
            }
            if (_shape != null)
            {
                _shape.Dispose();
                _shape = null;
            }
        }

        public void Cancel()
        {
            this._before = this._after = null;
            this.Visible = false;
        }

        public void Finish(bool cancelled)
        {
            if (cancelled)
            {
                Cancel();
            }
            this.Visible = false;
            if (this._treeView != null) this._treeView._dff = null;
        }

        public TreeNode Item
        {
            get { return this._start; }
            set { this._start = value; }
        }

        public TreeView TreeView
        {
            get { return this._treeView; }
            set
            {
                this._treeView = value;
                this._treeView._dff = this;
                this._indent = value.TreeIndent - 5;
                int height = this._treeView.ItemHeight / 2;
                int width = 0;
                if (this._treeView.Nodes.Count > 0)
                {
                    // todo: get the size of the item being dragged (from TreeData);
                    TreeNode n = this._treeView.Nodes[0];
                    Rectangle p = n.LabelBounds;
                    width = p.Width;
                }
                _shape = new GraphicsPath();
                int h = height / 2;
                int w = width;
                Graphics g = value.CreateGraphics();
                using (g)
                {
                    SizeF size = g.MeasureString(_start.Label, this._treeView.Font);
                    h = (int)Math.Max(h, size.Height / 2);
                    w = (int)size.Width;
                }
                _shape.AddLines(
                    new Point[] {
                                    new Point(0, h/2),
                                    new Point(_indent, h/2),
                                    new Point(_indent, 0),
                                    new Point(_indent + w, 0),
                                    new Point(_indent + w, h),
                                    new Point(_indent, h),
                                    new Point(_indent, h/2)
                                });
                RectangleF r = _shape.GetBounds();
                this.Bounds = new Rectangle(0, 0, (int)r.Width + 1, (int)r.Height + 1);
            }
        }

        public void ResetToggleCount()
        {
            _count = 0;
        }

        public virtual Point Position
        {
            get { return this._position; }
            set
            {
                if (_lastPos == value)
                {
                    _count++;
                }
                else
                {
                    _count = 0;
                }
                _lastPos = value;
                if (value == new Point(0, 0))
                {
                    this._position = value;
                    this.Visible = false;
                }
                else if (this._treeView.Nodes.Count > 0)
                {
                    Point local = this._treeView.PointToClient(value);
                    Point pos = this._treeView.ApplyScrollOffset(local);
                    TreeNode node = this._treeView.FirstVisibleNode;
                    while (node != null)
                    {
                        Rectangle bounds = node.LabelBounds;
                        CheckToggle(node, pos);
                        TreeNode next = node.NextVisibleNode;
                        int h = (int)_shape.GetBounds().Height;
                        int iconWidth = this._treeView.ImageList.ImageSize.Width;
                        if (next != null)
                        {
                            CheckToggle(next, pos);
                            Rectangle nb = next.LabelBounds;
                            int dy = pos.Y - (bounds.Top + bounds.Bottom) / 2;
                            int dy2 = (nb.Top + nb.Bottom) / 2 - pos.Y;
                            if (dy >= 0 && dy2 >= 0)
                            {
                                if (!ContainsNode(_start, node) && !ContainsNode(_start, next))
                                {
                                    Point midpt;
                                    if (dy < dy2)
                                    {
                                        this._before = null;
                                        this._after = node;
                                        midpt = new Point(bounds.Left - _indent - iconWidth, bounds.Bottom - h / 2);
                                    }
                                    else
                                    {
                                        this._after = null;
                                        this._before = next;
                                        midpt = new Point(nb.Left - _indent - iconWidth, nb.Top - h / 2);
                                    }
                                    if (midpt != this._position)
                                    {
                                        this._position = midpt;
                                        this.Location = this._position;
                                        if (!this.Visible)
                                            this.Visible = true;
                                    }
                                }
                                break;
                            }
                        }
                        else
                        {
                            Point midpt = new Point(bounds.Left - _indent - iconWidth, bounds.Bottom - h / 2);
                            if (midpt != this._position && !ContainsNode(_start, node))
                            {
                                this._after = node;
                                this._before = null;
                                this._position = midpt;
                                this.Location = this._position;
                                if (!this.Visible)
                                    this.Visible = true;
                            }
                        }
                        node = next;
                    }
                }
            }
        }

        void CheckToggle(TreeNode node, Point pos)
        {
            if (node.LabelBounds.Contains(pos.X, pos.Y))
            {
                if (node != _toggled)
                {
                    if (_count > 10 && _toggled != _lastNode)
                    {
                        if (node.Children.Count > 0)
                        {
                            node.Toggle();
                        }
                        _count = 0;
                        _toggled = node;
                    }
                }
                if (_lastNode == _toggled && node != _toggled)
                {
                    if (_toggled != null && !ContainsNode(_toggled, node))
                        _toggled.Toggle(); // revert back so we don't end up expanding the whole tree!
                    _toggled = null;
                }
                _lastNode = node;
            }
        }

        bool ContainsNode(TreeNode parent, TreeNode node)
        {
            if (node == null) return false;
            if (parent.Children.Count > 0)
            {
                foreach (TreeNode n in parent.Children)
                {
                    if (n == node)
                        return true;
                    if (ContainsNode(n, node))
                        return true;
                }
            }
            return false;
        }

        public void Draw(Graphics g)
        {
            if (this._shape != null)
            {
                Matrix saved = g.Transform;
                Matrix m = saved.Clone();
                m.Translate(this.Bounds.Left, this.Bounds.Top);
                g.Transform = m;
                g.FillPath(this._brush, this._shape);
                g.DrawPath(this._pen, this._shape);
                g.Transform = saved;
            }
        }
    }

    public delegate void TypeToFindEventHandler(object sender, string toFind);

    public class TypeToFindHandler : IDisposable
    {
        private int _start;
        private readonly Control _control;
        private string _typedSoFar;
        private readonly int _resetDelay;
        private TypeToFindEventHandler _handler;
        private bool _started;
        private Cursor _cursor;

        public TypeToFindEventHandler FindString
        {
            get { return this._handler; }
            set { this._handler = value; }
        }

        public TypeToFindHandler(Control c, int resetDelayInMilliseconds)
        {
            this._control = c;
            this._resetDelay = resetDelayInMilliseconds;
            this._control.KeyPress += new KeyPressEventHandler(OnControlKeyPress);
            this._control.KeyDown += new KeyEventHandler(OnControlKeyDown);
        }

        ~TypeToFindHandler()
        {
            Dispose(false);
        }

        public bool Started
        {
            get
            {
                if (Cursor.Current != this._cursor) _started = false;
                return _started;
            }
        }

        void OnControlKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.I:
                    if ((e.Modifiers & Keys.Control) != 0)
                    {
                        StartIncrementalSearch();
                    }
                    break;
                case Keys.Escape:
                    if (_started)
                    {
                        StopIncrementalSearch();
                        e.Handled = true;
                    }
                    break;
                case Keys.Enter:
                case Keys.Home:
                case Keys.End:
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    StopIncrementalSearch();
                    break;
                default:
                    if (_started && !e.Control && !e.Alt)
                        e.Handled = true;
                    break;
            }
        }


        public Cursor Cursor
        {
            get
            {
                if (_cursor == null)
                {
                    using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("XmlNotepad.Resources.isearch.cur"))
                    {
                        this._cursor = new Cursor(stream);
                    }
                }
                return this._cursor;
            }
        }

        public void StartIncrementalSearch()
        {
            Cursor.Current = this.Cursor;
            _started = true;
        }

        public void StopIncrementalSearch()
        {
            Cursor.Current = Cursors.Arrow;
            _started = false;
            _typedSoFar = "";
        }

        void OnControlKeyPress(object sender, KeyPressEventArgs e)
        {
            if (_started)
            {
                char ch = e.KeyChar;
                if (ch < 0x20) return; // don't process control characters
                int tick = Environment.TickCount;
                if (tick < _start || tick < this._resetDelay || _start < tick - this._resetDelay)
                {
                    _typedSoFar = ch.ToString();
                }
                else
                {
                    _typedSoFar += ch.ToString();
                }
                _start = tick;
                if (FindString != null) FindString(this, _typedSoFar);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (_cursor != null)
            {
                _cursor.Dispose();
                _cursor = null;
            }
        }
    }
}