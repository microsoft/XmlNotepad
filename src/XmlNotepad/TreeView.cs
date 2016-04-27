using System;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace XmlNotepad {
    // This file contains a new implementation of TreeView that virtualizes the storage of the
    // tree node data so it can come from a separate model, for example, an XmlDocument.
    // It also removes some limitations that TreeView has like maximum of height of 32k pixels.
    public class TreeView : UserControl, IEditableView {
        ImageList imageList;
        bool editable;
        TreeNode focus;
        ArrayList selection = new ArrayList();
        TreeNodeCollection nodes;
        Color lineColor = SystemColors.ControlDark;
        int treeIndent = 30;
        TypeToFindHandler ttf;
        private TextEditorOverlay editor;
        internal TreeViewDropFeedback dff;
        Timer timer = new Timer();
        int mouseDownEditDelay = 400;
        Point scrollPosition;
        AccessibleTree acc;
        
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

        public TreeView() {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Selectable, true);

            InitializeComponent();

            this.SuspendLayout();
            this.editor = new TextEditorOverlay(this);
            this.editor.AutoSize = true;
            this.editor.CommitEdit += new EventHandler<TextEditorEventArgs>(OnCommitEdit);
            this.editor.LayoutEditor += new EventHandler<TextEditorLayoutEventArgs>(OnLayoutEditor);

            ttf = new TypeToFindHandler(this, 2000);
            ttf.FindString += new TypeToFindEventHandler(FindString);
            timer.Tick += new EventHandler(timer_Tick);

            this.AccessibleRole = AccessibleRole.List;
            this.AccessibleName = "TreeView";
            this.ResumeLayout();
        }

        #region Component Designer generated code

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            if (this.ttf != null) {
                this.ttf.Dispose();
                this.ttf = null;
            }
            base.Dispose(disposing);
        }

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();

            this.AccessibleRole = AccessibleRole.List;
            this.Name = this.AccessibleName = "TreeView";
            
            this.ResumeLayout(false);
        }

        #endregion

        public void Close() {
            this.editor.Dispose();
        }

        public void SetSite(ISite site) {
            this.Site = this.editor.Site = site;
        }

        public Point ScrollPosition {
            get { return this.scrollPosition; }
            set {
                this.scrollPosition = value;
                Invalidate();
            }
        }

        public TextEditorOverlay Editor {
            get { return this.editor; }
        }

        public void StartIncrementalSearch() {
            ttf.StartIncrementalSearch(); 
            this.Focus();
        }

        void FindString(object sender, string toFind) {
            TreeNode node = this.SelectedNode;
            if (node == null) node = this.FirstVisibleNode;
            TreeNode start = node;
            while (node != null) {
                string s = node.Label;
                if (s != null && s.StartsWith(toFind, StringComparison.CurrentCultureIgnoreCase)) {
                    this.SelectedNode = node;
                    return;
                }
                node = node.NextVisibleNode;
                if (node == null) node = this.FirstVisibleNode;
                if (node == start)
                    break;
            }
        }

        public int MouseDownEditDelay {
            get { return this.mouseDownEditDelay; }
            set { this.mouseDownEditDelay = value; }
        }

        internal void OnRemoveNode(TreeNode node) {
            if (node != null && this.SelectedNode != null &&
                (node == this.SelectedNode || node.Contains(this.SelectedNode))) {
                TreeNodeCollection col = (node.Parent == null) ? this.Nodes : node.Parent.Nodes;
                if (col != null) {
                    int count = col.Count;
                    TreeNode selected = null;
                    if (node.Index == count - 1) {
                        selected = node.PrevVisibleNode;
                    } else {
                        // get next visible node after this one (and after all it's children).
                        TreeNode next = col[node.Index + 1];
                        selected = (!next.IsVisible) ? next.NextVisibleNode : next;
                    }
                    this.SelectedNode = selected;
                } else {
                    this.SelectedNode = null;
                }
            }
            InvalidateLayout();
        }

        internal void OnBeforeExpand(TreeNode node) {
            if (this.BeforeExpand != null) this.BeforeExpand(this, new TreeViewEventArgs(node, TreeViewAction.Expanded));
        }
        internal void OnAfterExpand(TreeNode node) {
            if (this.AfterExpand != null) this.AfterExpand(this, new TreeViewEventArgs(node, TreeViewAction.Expanded));
        }
        internal void OnBeforeCollapse(TreeNode node) {
            if (this.BeforeCollapse != null) this.BeforeCollapse(this, new TreeViewEventArgs(node, TreeViewAction.Collapsed));
        }
        internal void OnAfterCollapse(TreeNode node) {
            if (this.AfterCollapse != null) this.AfterCollapse(this, new TreeViewEventArgs(node, TreeViewAction.Collapsed));

            TreeNode sel = this.SelectedNode;
            if (sel != null && node.Contains(sel)) {
                this.SelectedNode = node;
            }
        }

        public TreeNodeCollection Nodes {
            get { return this.nodes; }
            set {
                ClearSelection();
                this.nodes = value;
                PerformLayout();
            }
        }

        public TreeNode SelectedNode {
            get { return selection.Count > 0 ? (TreeNode)selection[0] : null; }
            set {
                if (this.SelectedNode != value) {
                    if (this.InBatchUpdate && this.selection.Count == 1) {
                        if (value == null) {
                            this.selection.Clear();
                            this.focus = null;
                        } else {
                            this.selection[0] = value;
                            this.focus = value;
                        }
                    } else {
                        ClearSelection();
                        if (value != null) {
                            selection.Add(value);
                        }
                        SetFocus(value);
                        OnSelectionChanged();
                    }
                }
            }
        }

        public void OnSelectionChanged() {
            if (AfterSelect != null) AfterSelect(this, new TreeViewEventArgs(this.SelectedNode, TreeViewAction.None));
        }

        protected override void OnGotFocus(EventArgs e) {
            if (this.SelectedNode == null) {
                SelectedNode = this.FirstVisibleNode;
            }
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e) {
            Invalidate();
        }

        Point mouseDown;
        Point lastMousePos;
        TreeNode downSel;
        TreeNode downNode; 
        TreeNode hitNode;

        public Point ApplyScrollOffset(int x, int y) {
            return new Point(x - this.scrollPosition.X, y - this.scrollPosition.Y);
        }
        public Point ApplyScrollOffset(Point p) {
            return new Point(p.X - this.scrollPosition.X, p.Y - this.scrollPosition.Y);
        }

        protected override void OnMouseDown(MouseEventArgs e) {
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
            if (e.Button == MouseButtons.Left) {
                if (node != null) {
                    Size imgSize = GetImageSize();
                    if (e.Clicks > 1) {
                        node.Toggle();
                    } else if (wasFocussed && node.LabelAndImageBounds(imgSize, TreeIndent).Contains(x, y)) {
                        this.hitNode = node;
                        if (node.LabelBounds.Contains(x, y)) {
                            this.downSel = sel;
                            this.downNode = node;
                            mouseDown = new Point(x, y);                     
                        }
                    } else {
                        Rectangle r = GetBoxBounds(this.Margin.Left + (imgSize.Width / 2), node.LabelBounds.Top, this.ItemHeight, node.Depth, TreeIndent);
                        int slop = (this.ItemHeight - r.Height) / 2;
                        r.Inflate(slop, slop); // make it a bit easier to hit.
                        if (r.Contains(x, y)) {
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

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            Point pt = ApplyScrollOffset(e.X, e.Y);
            this.lastMousePos = pt;
            bool left = e.Button == MouseButtons.Left;
            if (left && hitNode != null) {
                int dx = pt.X - mouseDown.X;
                int dy = pt.Y - mouseDown.Y;
                if (Math.Sqrt(dx * dx + dy * dy) > DragThreshold) {
                    if (this.ItemDrag != null) {
                        this.ItemDrag(this, new ItemDragEventArgs(e.Button, hitNode));
                    }
                    this.downNode = this.downSel = null;                    
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && this.downSel != null) {
                Point p = ApplyScrollOffset(e.X, e.Y);
                int x = p.X; int y = p.Y;
                TreeNode node = this.FindNodeAt(x, y);
                if (node != null && node.LabelBounds.Contains(x, y)) {
                    mouseDown = new Point(e.X, e.Y);
                    if (this.downSel == node) {
                        timer.Interval = this.mouseDownEditDelay;
                        timer.Start();
                        timer.Enabled = true;
                        return;
                    }
                }
                this.downSel = null;
            }
            this.downNode = null;
            this.hitNode = null;
        }

        void timer_Tick(object sender, EventArgs e) {
            timer.Stop();
            if (this.downSel != null && this.downSel == this.SelectedNode && this.downSel.LabelBounds.Contains(this.lastMousePos)) {
                this.BeginEdit(null);
            }
            this.downSel = null;
            this.downNode = null;
            this.hitNode = null;
        }

        protected override bool IsInputKey(Keys keyData) {
            Keys key = (keyData & ~Keys.Modifiers);
            switch (key) {
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

        protected override void OnKeyDown(KeyEventArgs e) {
            CurrentEvent.Event = e;
            base.OnKeyDown(e);
            if (!e.Handled) {
                HandleKeyDown(e);
            }
        }

        public void BubbleKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
        }

        public void HandleKeyDown(KeyEventArgs e) {

            TreeNode sel = this.SelectedNode;

            bool isLetterOrDigit = ((e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z) ||
                                   (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)) && 
                                   (e.Modifiers == Keys.Shift || e.Modifiers == 0);

            if (e.Modifiers == Keys.Control && (e.KeyCode == Keys.Home || e.KeyCode == Keys.End)) {
                // drop through in this case.
            } else if (e.Modifiers != 0 && !isLetterOrDigit) {
                // Reserve use of modifiers for other things, like multi-select expansion and so on.
                return;
            }

            TreeNode n =this.SelectedNode;

            switch (e.KeyCode) {
                case Keys.Left:
                    if (sel != null) {
                        if (sel.IsExpanded) {
                            sel.Collapse();
                        } else if (sel.Parent != null) {
                            this.SelectedNode = sel.Parent;
                        }
                    }
                    e.Handled = true;
                    break;
                case Keys.Right:
                    if (sel != null && !sel.IsExpanded) {
                        sel.Expand();
                    }
                    e.Handled = true;
                    break;
                case Keys.Up:
                    if (sel != null) {
                        TreeNode prev = sel.PrevVisibleNode;
                        if (prev != null) {
                            this.SelectedNode = prev;
                        }
                    } else {
                        this.SelectedNode = this.LastVisibleNode;
                    }
                    e.Handled = true;
                    break;
                case Keys.Down:
                    if (sel != null) {
                        TreeNode next = sel.NextVisibleNode;
                        if (next != null) {
                            this.SelectedNode = next;
                        }
                    } else {
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
                    if (n != null) {
                        n.ExpandAll();
                        e.Handled = true;
                    }
                    return;
                case Keys.Add:
                    if (n != null) {
                        n.Expand();
                        e.Handled = true;
                    }
                    return;
                case Keys.Subtract:
                    if (n != null) {
                        n.Collapse();
                        e.Handled = true;
                    }
                    break;
                default:
                    if (isLetterOrDigit && !e.Handled && this.ContainsFocus) {
                        if (ttf.Started) {
                            e.Handled = true;
                        } else if (!this.IsEditing) {
                            char ch = Convert.ToChar(e.KeyValue);
                            if (!e.Shift) ch = Char.ToLower(ch);
                            if (Char.IsLetter(ch) && this.BeginEdit(ch.ToString())) {
                                this.editor.SelectEnd();
                                e.Handled = true;
                            }
                        }
                    }
                    break;
            }
        }

        public void HandlePageDown() {
            int visibleRows = VisibleRows;
            TreeNode sel = this.SelectedNode;
            if (sel == null) {
                sel = this.FirstVisibleNode;
            }
            if (sel != null) {
                TreeNode n = sel.NextVisibleNode;
                while (n != null && visibleRows > 0) {
                    if (n.NextVisibleNode == null)
                        break;
                    n = n.NextVisibleNode;
                    visibleRows--;
                }
                if (n != null) {
                    this.SelectedNode = n;
                }
            }
        }

        public void HandlePageUp() {
            int visibleRows = VisibleRows;
            TreeNode sel = this.SelectedNode;
            if (sel == null)
                sel = this.LastVisibleNode;

            if (sel != null) {
                TreeNode n = sel;
                while (n != null && visibleRows > 0) {
                    if (n.PrevVisibleNode == null)
                        break;
                    n = n.PrevVisibleNode;
                    visibleRows--;
                }
                if (n != null) {
                    this.SelectedNode = n;
                }
            }
        }

        public int VisibleRows {
            get {
                return this.ClientRectangle.Height / this.ItemHeight;
            }
        }

        void OnLayoutEditor(object sender, TextEditorLayoutEventArgs args) {
            if (this.SelectedNode == null)
                return;
            Rectangle r = this.SelectedNode.LabelBounds;
            r.Offset(this.scrollPosition);
            args.PreferredBounds = r;
            r.Width = this.Width - r.Left + this.Left - 20;
            args.MaxBounds = r;
        }

        #region IEditableView

        public bool BeginEdit(string value) {
            TreeNode sel = this.SelectedNode;
            if (this.editable && sel != null && sel.IsLabelEditable) {
                string text = value != null ? value : sel.Label;
                if (this.BeforeLabelEdit != null) {
                    NodeLabelEditEventArgs args = new NodeLabelEditEventArgs(sel, text);
                    this.BeforeLabelEdit(this, args);
                    if (args.CancelEdit)
                        return false;
                }

                IIntellisenseProvider provider = this.GetService(typeof(IIntellisenseProvider)) as IIntellisenseProvider;
                if (provider != null) {
                    provider.SetContextNode(sel);
                    if (!provider.IsNameEditable)
                        return false;
                }
                this.editor.BeginEdit(text, provider, EditMode.Name, sel.ForeColor, this.Focused);
                return true;
            }
            return false;
        }


        public bool IsEditing {
            get {
                return this.editor.IsEditing;
            }
        }

        public void SelectText(int index, int length) {
            if (this.editor.IsEditing) {
                this.editor.Select(index, length);
            }
        }

        public bool ReplaceText(int index, int length, string replacement) {
            if (this.editor.IsEditing) {
                return this.editor.Replace(index, length, replacement);
            }
            return false;
        }

        public Rectangle EditorBounds {
            get {
                return this.editor.Bounds;
            }
        }

        public bool EndEdit(bool cancel) {
            return this.editor.EndEdit(cancel);
        }

        public int SelectionStart { get { return this.editor.SelectionStart; } }

        public int SelectionLength { get { return this.editor.SelectionLength; } }

        #endregion

        void OnCommitEdit(object sender, TextEditorEventArgs args) {
            TreeNode sel = this.SelectedNode;
            if (sel != null && this.IsEditing) {
                //string text = sel.Label;
                bool cancel = args.Cancelled;
                if (this.AfterLabelEdit != null) {
                    NodeLabelEditEventArgs a = new NodeLabelEditEventArgs(sel, args.Text);
                    a.CancelEdit = cancel;
                    this.AfterLabelEdit(this, a);
                    cancel = args.Cancelled = a.CancelEdit;
                }
                if (!cancel) {
                    sel.Label = args.Text;
                    // [chris] this breaks the find dialog...
                    //this.Focus();
                }
                InvalidateLayout(); // LabelBounds needs recalculating.
                InvalidateNode(sel);
            }
        }


        void ClearSelection() {
            EndEdit(true);
            EndEdit(false);
            this.focus = null;
            foreach (TreeNode node in selection) {
                InvalidateNode(node);
            }
            selection.Clear();
        }

        void SetFocus(TreeNode node) {
            if (this.focus != node) {
                InvalidateNode(this.focus);
                this.focus = node;
                InvalidateNode(node);
                EnsureVisible(node);
            }
        }

        public static void EnsureVisible(TreeNode node) {
            TreeNode p = node.Parent;
            while (p != null) {
                if (!p.IsExpanded) {
                    p.Expand();
                }
                p = p.Parent;
            }

        }

        public void InvalidateNode(TreeNode node) {
            if (node != null) {
                Rectangle r = new Rectangle(0, node.LabelBounds.Top, this.Width, this.ItemHeight);
                r.Offset(this.scrollPosition);
                Invalidate(r);
            }
        }

        public TreeNode[] GetSelectedNodes() {
            return (TreeNode[])selection.ToArray(typeof(TreeNode));
        }

        public void SetSelectedNodes(TreeNode[] value) {
            ClearSelection();
            if (value != null) {
                selection = new ArrayList(value);
                foreach (TreeNode node in value) {
                    InvalidateNode(node);
                }
            }
            this.OnSelectionChanged();
        }

        public bool IsSelected(TreeNode node) {
            return this.selection != null && this.selection.Contains(node);
        }

        public int ItemHeight {
            get {
                return this.Font.Height;
            }
        }

        // Dead code
        //public TreeNode GetTopNode(Rectangle bounds) {
        //    int y = 0;
        //    return this.FindTopNode(this.nodes, bounds, ref y);
        //}

        public bool LabelEdit {
            get { return this.editable; }
            set { this.editable = value; }
        }

        public ImageList ImageList {
            get { return this.imageList; }
            set { this.imageList = value; }
        }

        public TreeNode FirstVisibleNode {
            get {
                if (this.nodes == null) return null;
                foreach (TreeNode node in this.nodes) {
                    if (node.IsVisible) {
                        return node;
                    }
                }
                return null;
            }
        }

        public TreeNode LastVisibleNode {
            get {
                return TreeNode.GetLastVisibleNode(this.Nodes);                
            }
        }

        int updateDepth;

        public void InvalidateLayout() {
            if (updateDepth == 0) {
                this.PerformLayout();
                Invalidate();
            }
        }

        public bool InBatchUpdate {
            get { return this.updateDepth > 0; }
        }

        public void BeginUpdate() {
            this.updateDepth++;
        }

        public void EndUpdate() {
            this.updateDepth--;
            if (updateDepth == 0) {
                TreeNode node = this.SelectedNode;
                // fix up selection & layout.
                ClearSelection();
                selection.Add(node);
                SetFocus(node);
                OnSelectionChanged();
                this.PerformLayout();
                Invalidate();
            }
            if (AfterBatchUpdate != null)
                AfterBatchUpdate(this, EventArgs.Empty);
        }

        public void ExpandAll() {
            ExpandAll(this, this.Nodes);
        }

        public virtual void ExpandAll(TreeView view, TreeNodeCollection nodes) {
            if (nodes == null || nodes.Count == 0) return;
            if (view != null) view.BeginUpdate();
            try {
                foreach (TreeNode n in nodes) {
                    if (n.Nodes.Count > 0 && n.CanExpandAll) {
                        if (!n.IsExpanded) n.Expand();
                        ExpandAll(view, n.Nodes);
                    }
                }
            } finally {
                if (view != null) view.EndUpdate();
            }
        }

        public void CollapseAll() {
            CollapseAll(this, this.Nodes);
        }

        public virtual void CollapseAll(TreeView view, TreeNodeCollection nodes) {
            if (nodes == null) return;
            if (view != null) view.BeginUpdate();
            try {
                foreach (TreeNode n in nodes) {
                    if (n.IsExpanded) {
                        CollapseAll(view, n.Nodes);
                        n.Collapse();
                    }
                }
            } finally {
                if (view != null) view.EndUpdate();
            }
        }

        public int TreeIndent {
            get { return this.treeIndent; }
            set { this.treeIndent = value; }
        }

        public Color LineColor {
            get { return this.lineColor; }
            set { this.lineColor = value; }
        }

        Pen linePen;
        Pen plusPen;
        Pen boxPen;
        Brush backBrush;

        protected override void OnPaint(PaintEventArgs e) {
            //PerfTimer t = new PerfTimer();
            //t.Start();
            //base.OnPaint(e);

            if (this.nodes == null) return;
            Graphics g = e.Graphics;
            //g.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF clipF = g.ClipBounds;
            Matrix m = g.Transform;
            m.Translate(this.scrollPosition.X, this.scrollPosition.Y);
            g.Transform = m;

            this.linePen = new Pen(this.LineColor, 1);
            this.linePen.DashStyle = DashStyle.Dot;
            this.linePen.LineJoin = LineJoin.Round;

            this.plusPen = new Pen(this.ForeColor);
            this.plusPen.Alignment = PenAlignment.Center;
            this.boxPen = new Pen(this.LineColor);
            this.boxPen.Alignment = PenAlignment.Inset;
            this.backBrush = new SolidBrush(this.BackColor);

            Rectangle clip = new Rectangle((int)clipF.X - this.scrollPosition.X, (int)clipF.Y - this.scrollPosition.Y, (int)clipF.Width, (int)clipF.Height);
            g.FillRectangle(backBrush, clip);

            //Trace.WriteLine("MyTreeView: clip="+clip.ToString());

            DrawNodes(g, ref clip, new LineStates(), TreeIndent, 0, this.nodes);

            if (this.focus != null && !this.editor.IsEditing) {
                Rectangle r = this.focus.LabelBounds;
                if (clip.IntersectsWith(r)) {
                    Pen focusPen = new Pen(Color.Black, 1);
                    focusPen.DashStyle = DashStyle.Dot;
                    using (focusPen) {
                        g.DrawRectangle(focusPen, r.Left, r.Top, r.Width - 1, r.Height - 1);
                    }
                }
            }

            if (dff != null) {
                dff.Draw(g);
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

        int DrawNodes(Graphics g, ref Rectangle clip, LineStates state, int indent, int y, TreeNodeCollection nodes) {
            if (nodes == null) return y;
            Font f = this.Font;
            Size imgSize = GetImageSize();
            int count = nodes.Count;
            int pos = 0;
            int h = this.ItemHeight;
            //int w = this.Width;
            int x = this.Margin.Left;

            foreach (TreeNode node in nodes) {
                if (node.IsVisible) {
                    LineState ls = LineState.None;
                    if (pos == 0) ls |= LineState.First;
                    if (pos + 1 == count) ls |= LineState.Last;
                    state.Push(ls);
                    //Rectangle bounds = new Rectangle(0, y, w, h);                    
                    bool visible = (y + h) >= clip.Top && y <= clip.Bottom; // clip.IntersectsWith(bounds);
                    if (visible) {
                        int index = node.ImageIndex;
                        Image img = index < 0 ? null : this.imageList.Images[index];
                        bool isSelected = this.IsSelected(node);
                        node.Draw(g, f, linePen, state, h, indent, x, y, ref imgSize, img, isSelected);
                    }
                    int y2 = y;
                    y += h;
                    if (node.Nodes.Count > 0) {
                        int depth = state.Depth - 1;
                        if (node.IsExpanded) {
                            y = DrawNodes(g, ref clip, state, indent, y, node.Nodes);
                            // Draw boxes on the way out.
                            if (visible) {
                                DrawPlusMinus(g, x + (imgSize.Width / 2), y2, h, depth, indent, false);
                            }
                        } else {
                            if (visible) {
                                DrawPlusMinus(g, x + (imgSize.Width / 2), y2, h, depth, indent, true);
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

        static Rectangle GetBoxBounds(int margin, int y, int height, int depth, int indent) {
            int x = margin + (depth * indent) - (BoxWidth / 2);
            y = y + (height - BoxHeight) / 2; // center it vertically.
            return new Rectangle(x, y, BoxWidth, BoxHeight);
        }

        void DrawPlusMinus(Graphics g, int x, int y, int height, int depth, int indent, bool plus) {
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
            if (plus) {
                g.DrawLine(this.plusPen, x2, y + dy, x2, y + dy + PlusHeight);
            }
        }

        protected override void OnLayout(LayoutEventArgs levent) {
            this.virtualHeight = 0;
            if (this.nodes != null) {
                Graphics g = this.CreateGraphics();
                using (g) {
                    int w = this.virtualWidth;
                    this.virtualWidth = 0;
                    this.virtualHeight = LayoutNodes(g, TreeIndent, 1, 0, this.Nodes);
                    if (w != this.virtualWidth) {
                        this.Parent.PerformLayout();
                    }
                }
            }
        }

        int virtualHeight;

        public int VirtualHeight {
            get { return virtualHeight; }
            set { virtualHeight = value; }
        }


        int virtualWidth;

        public int VirtualWidth {
            get { return virtualWidth; }
            set { virtualWidth = value; }
        }


        int LayoutNodes(Graphics g, int indent, int depth, int y, TreeNodeCollection nodes) {
            Size imgSize = new Size();
            if (this.imageList != null) {
                imgSize = this.imageList.ImageSize;
            }
            int x = this.Margin.Left;
            int h = this.ItemHeight;
            Font f = this.Font;

            foreach (TreeNode node in nodes) {
                if (node.IsVisible) {
                    node.Layout(g, f, h, x, indent, depth, y, imgSize);
                    y += h;
                    this.virtualWidth = Math.Max(this.virtualWidth, node.LabelBounds.Right);
                    if (node.IsExpanded && node.Nodes.Count > 0) {
                        y = LayoutNodes(g, indent, depth + 1, y, node.Nodes);
                    }
                    node.bottom = y;
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

        Size GetImageSize() {
            Size imgSize = new Size();
            if (this.imageList != null) {
                imgSize = this.imageList.ImageSize;
                int imgHeight = imgSize.Height;
                int imgWidth = imgSize.Width;
                int h = this.ItemHeight;
                if (imgHeight > h) {
                    // scale image down to fit.
                    imgWidth = (imgWidth * h) / imgHeight;
                    imgHeight = h;
                    imgSize = new Size(imgWidth, imgHeight);
                }
            }
            return imgSize;
        }

        public TreeNode FindNodeAt(int x, int y) {
            return FindNodeAt(this.Nodes, x, y);
        }

        TreeNode FindNodeAt(TreeNodeCollection nodes, int x, int y) {
            if (nodes != null) {
                foreach (TreeNode n in nodes) {
                    if (n.IsVisible) {
                        Rectangle r = new Rectangle(0, n.LabelBounds.Top, this.Width, this.ItemHeight);
                        if (r.Contains(x, y)) {
                            return n;
                        }
                        if (n.IsExpanded && n.LabelBounds.Top <= y && n.bottom >= y) {
                            TreeNode result = FindNodeAt(n.Nodes, x, y);
                            if (result != null) return result;
                        }
                    }
                }
            }
            return null;
        }

        protected override AccessibleObject CreateAccessibilityInstance() {
            if (this.acc == null) this.acc = new AccessibleTree(this);
            return this.acc;
        }

        protected override AccessibleObject GetAccessibilityObjectById(int objectId) {
            return base.GetAccessibilityObjectById(objectId);
        }
    }

    // MyTreeNode is an abstract wrapper on the tree data that keeps track of UI state.
    public abstract class TreeNode  {
        TreeNode parent;
        bool visible = true;
        bool expanded;
        bool selected;        
        Rectangle labelBounds; // for hit testing
        TreeView view;
        internal int bottom; // Y coordinate of bottom of last grandchild.
        AccessibleObject acc;

        protected TreeNode() {
        }

        protected TreeNode(TreeNode parent) {
            this.parent = parent;
        }
 
        public int Index {
            get {
                if (this.parent != null) {
                    return this.parent.Nodes.GetIndex(this);
                } else if (this.view != null) {
                    return this.view.Nodes.GetIndex(this);
                }
                return -1;
            }
        }

        public AccessibleObject AccessibleObject {
            get {
                if (this.acc == null) this.acc = new AccessibleNode(this);
                return this.acc;
            }
        }

        public abstract string Label { get ; set; }
        public abstract bool IsLabelEditable { get; }
        public abstract TreeNodeCollection Nodes { get; }
        public abstract int ImageIndex { get; }        
        public abstract Color ForeColor { get; }
        public abstract string Text { get; set; }

        TreeNodeCollection ParentCollection {
            get {
                TreeNode parent = this.parent;
                return parent == null ? (view != null ? view.Nodes : null) : parent.Nodes;
            }
        }

        public TreeNode PrevNode {
            get {
                TreeNodeCollection col = ParentCollection;
                if (col == null) return null;
                int i = col.GetIndex(this) - 1;
                if (i >= 0) {
                    TreeNode n = col[i];
                    return n;
                }
                return parent;
            }
        }

        public TreeNode NextSiblingNode {
            get {
                if(this.ParentCollection!=null && 
                    this.ParentCollection.Count >  this.Index+1) {
                    return this.ParentCollection[this.Index+1];
                }
                else {
                    return null;
                }
            }
        }

        public TreeNode NextNode {
            get {
                TreeNode child = GetFirstVisibleChild(this);
                if (child != this) return child;
                TreeNode parent = this.parent;
                TreeNode node = this;
                do {
                    TreeNodeCollection col = parent == null ? (view != null ? view.Nodes : null) : parent.Nodes;
                    int i = col.GetIndex(node) + 1;
                    int count = col.Count;
                    if (i < count) {
                        TreeNode n = col[i];
                        return n;
                    }
                    node = parent;
                    if (parent != null) parent = parent.Parent;
                } while (node != null);
                return null;
            }
        }

        public TreeView TreeView  {
            get {
                return this.view;
            }
            set {
                this.view = value;
            }
        }

        public virtual void RemoveChildren() {
            TreeView view = this.view;
            if (view != null) view.BeginUpdate();
            try {
                List<TreeNode>  snapshot = new List<TreeNode>(this.Nodes);
                foreach (TreeNode child in snapshot) {
                    child.Remove();
                }
            } finally {
                if (view != null) view.EndUpdate();
            }
        }

        public virtual void Remove() {
            TreeNode parent = this.Parent;
            TreeNodeCollection pc = this.ParentCollection;
            TreeView view = this.view;
            if (view != null) view.BeginUpdate();
            try {
                if (view != null) {
                    view.OnRemoveNode(this);
                    this.view = null;
                }
                pc.Remove(this);
                if (parent != null && parent.Nodes.Count == 0 && this.parent.IsExpanded) {
                    this.parent.Collapse();
                }
            } finally {
                if (view != null) view.EndUpdate();
            }
        }

        public virtual TreeNode Parent {
            get { return this.parent; }
            set { this.parent = value; }
        }

        public bool IsExpanded {
            get { return this.expanded; }
        }

        // Whether to allow this node to be expanded during expand-all.
        public virtual bool CanExpandAll {
            get { return true; }
        }

        public bool IsVisible {
            get { return this.visible; }
            set {
                if (this.visible != value) {
                    this.visible = value;
                    if (this.view != null) this.view.InvalidateLayout();
                }
            }
        }

        public bool Selected {
            get { return this.selected; }
            set { this.selected = value; }
        }

        public Rectangle LabelBounds {
            get { return this.labelBounds; }
            set { this.labelBounds = value; }
        }

        public void ExpandAll() {
            if (!this.IsExpanded) this.Expand();
            this.view.ExpandAll(this.view, this.Nodes);
        }

        
        public void CollapseAll() {
            this.view.CollapseAll(this.view, this.Nodes);            
            if (this.IsExpanded) {
                this.Collapse();
            }
        }

        internal void Draw(Graphics g, Font f, Pen pen, LineStates state, int lineHeight, int indent, int x, int y, ref Size imgSize, Image img, bool selected) {
            int startX = x;
            Debug.Assert(this.view != null);
            for (int i = 0; i < state.Depth; i++) {
                LineState ls = state[i];
                int x2 = x + (imgSize.Width / 2);
                int x3 = x + indent;
                int y2 = y + lineHeight / 2;
                int y3 = y + lineHeight;

                bool leaf = i + 1 == state.Depth;

                if (leaf) {
                    g.DrawLine(pen, x2, y2, x3, y2); // horizontal bar
                }
                if ((ls & LineState.Last) == LineState.Last) {
                    if (((ls & LineState.HasParent) == LineState.HasParent || 
                        ((ls & LineState.First) == 0 && (ls & LineState.Last) == LineState.Last)) && leaf) {
                        g.DrawLine(pen, x2, y, x2, y2); // half vertical bar connecting to parent.
                    }
                } else if ((ls & LineState.HasParent) == LineState.HasParent || ((ls & LineState.First) == 0)) {
                    g.DrawLine(pen, x2, y, x2, y3); // full vertical bar
                } else if ((ls & LineState.First) == LineState.First ){ // we know it's also not the last, so
                    g.DrawLine(pen, x2, y2, x2, y3); // half vertical bar connecting to next child.
                }

                x += indent;
            }
            // draw +/- box
            // draw node image.
            
            int imgWidth =imgSize.Width;
            int imgHeight = imgSize.Height;
            if (img != null) {
                int iy = y;
                if (imgHeight < lineHeight) {
                    iy += (lineHeight - imgHeight) / 2; // center it
                }
                Rectangle rect = new Rectangle(x, iy, imgWidth, imgHeight);
                g.DrawImage(img, rect);
            }
            string text = this.Label;
            if (text != null && view != null) {
                Brush brush = null;
                if (selected && view.IsEditing) {
                    return;
                }
                if (selected && view.Focused) {
                    brush = Utilities.HighlightTextBrush(this.ForeColor);
                    g.FillRectangle(SystemBrushes.Highlight, this.labelBounds);
                } else {
                    brush = new SolidBrush(this.ForeColor);
                }
                Layout(g, f, lineHeight, startX, indent, state.Depth, y, imgSize);
                g.DrawString(text, f, brush, this.labelBounds.Left, this.labelBounds.Top, StringFormat.GenericTypographic);
                brush.Dispose();
            }
        }

        public void Layout(Graphics g, Font f, int lineHeight, int x, int indent, int depth, int y, Size imgSize) {
            int width = 10;
            if (this.Label != null) {
                SizeF s = g.MeasureString(this.Label, f);
                width = Math.Max(width, (int)s.Width);
            }
            int gap = imgSize.Width + GetGap(indent); // small gap
            this.LabelBounds = new Rectangle(x + (indent * depth) + gap, y, width, lineHeight);
        }

        internal static int GetGap(int indent) {
            return indent / 5;
        }

        public void Toggle() {
            if (this.IsExpanded) {
                this.Collapse();
            } else {
                this.Expand();
            }
        }

        public TreeNode PrevVisibleNode {
            get {
                for (TreeNode n = this.PrevNode; n != null; n = n.PrevNode) {
                    if (n.IsVisible) {
                        if (n == this.parent) return n;
                        return GetLastVisibleChild(n);
                    }
                }
                return null;
            }
        }


        public static TreeNode GetLastVisibleNode(TreeNodeCollection nodes) {
            if (nodes == null) return null;
            for (int i = (nodes.Count - 1); i >= 0; i--) {
                TreeNode child = nodes[i];
                if (child.IsVisible) {
                    if (!child.IsExpanded) return child;
                    return GetLastVisibleNode(child.Nodes);
                }
            }
            return null;
        }

        internal TreeNode GetLastVisibleChild(TreeNode n) {
            TreeNode last = n;
            if (n.IsExpanded && n.Nodes.Count > 0) {
                TreeNode child = GetLastVisibleNode(n.Nodes);
                if (child != null) last = child;
            }
            return last;
        }

        internal static TreeNode GetFirstVisibleChild(TreeNode n) {
            if (n.IsExpanded && n.Nodes.Count > 0) {
                foreach (TreeNode child in n.Nodes) {
                    if (child.IsVisible) {
                        return child;
                    }
                }
            }
            return n;
        }


        public TreeNode NextVisibleNode {
            get {
                for (TreeNode n = this.NextNode; n != null; n = n.NextNode) {
                    if (n.IsVisible) {
                        return n;
                    }
                }
                return null;
            }
        }

        public void BeginEdit() {
            Debug.Assert(this.view != null);
            if (this.view != null) {
                this.view.SelectedNode = this;
                this.view.BeginEdit(null);
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

        public bool EndEdit(bool cancel) {
            Debug.Assert(this.view != null);
            if (this.view != null) {
                return this.view.EndEdit(cancel);
            }
            return true;
        }

        public bool IsEditing {
            get { return this.view != null && this.view.IsEditing; }
        }

        public virtual void Expand() {
            if (this.Nodes.Count > 0) {
                if (this.view != null) this.view.OnBeforeExpand(this);
                this.expanded = true;
                Invalidate();
                if (this.view != null) this.view.OnAfterExpand(this);
            }
        }

        public virtual void Collapse() {
            if (this.expanded) {
                if (this.view != null) this.view.OnBeforeCollapse(this);
                this.expanded = false;
                Invalidate();
                if (this.view != null) this.view.OnAfterCollapse(this);
            }
        }

        public virtual void Invalidate() {
            if (this.view != null) this.view.InvalidateLayout();
        }

        public int Depth {
            get {
                int depth = 0;
                TreeNode parent = this.parent;
                while (parent != null) {
                    depth++;
                    parent = parent.parent;
                }
                return depth;
            }
        }

        public bool Contains(TreeNode node) {
            TreeNode parent = node.Parent;
            while (parent != null) {
                if (parent == this) return true;
                parent = parent.Parent;
            }
            return false;
        }

        internal Rectangle LabelAndImageBounds( Size imgSize, int indent) {
            int gap = GetGap(indent);
            return new Rectangle(this.LabelBounds.Left - imgSize.Width - gap, this.LabelBounds.Top, this.LabelBounds.Width + imgSize.Width, this.LabelBounds.Height);
        }

    }

    public abstract class TreeNodeCollection : IEnumerable<TreeNode> {
        public abstract int Count { get; }
        public abstract void Add(TreeNode node);
        public abstract void Insert(int i, TreeNode node);
        public abstract void Remove(TreeNode child);
        public abstract TreeNode this[int i] { get; }
        public abstract int GetIndex(TreeNode node);
        public abstract IEnumerator<TreeNode> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable<TreeNode>)this).GetEnumerator();
        }
    }

    public class NodeLabelEditEventArgs : EventArgs {
        TreeNode node;
        bool cancel;
        string label;

        public NodeLabelEditEventArgs(TreeNode node, string label) {
            this.node = node;
            this.label = label;
        }
        public string Label { get { return this.label; } }
        public TreeNode Node { get { return this.node; } }
        public bool CancelEdit { 
            get { return this.cancel; }
            set { this.cancel = value; }
        }
    }

    public enum TreeViewAction { None, Expanded, Collapsed }

    public class TreeViewEventArgs : EventArgs {
        TreeNode node;
        TreeViewAction action;

        public TreeViewEventArgs(TreeNode node, TreeViewAction action) {
            this.node = node;
            this.action = action;
        }
        public TreeViewAction Action { get { return this.action; } }
        public TreeNode Node { get { return this.node; } }
    }    

    class AccessibleTree : Control.ControlAccessibleObject {
        TreeView tree;
        public AccessibleTree(TreeView view) : base(view) {
            this.tree = view;
        }
        public override Rectangle Bounds {
            get {
                return tree.RectangleToScreen(tree.ClientRectangle);
            }
        }
        public override string DefaultAction {
            get {
                return "Toggle";
            }
        }
        public override void DoDefaultAction() {
            if (tree.SelectedNode != null) {
                tree.SelectedNode.Toggle();
            }
        }
        public override int GetChildCount() {
            return tree.Nodes.Count;
        }
        public override AccessibleObject GetChild(int index) {
            return tree.Nodes[index].AccessibleObject;
        }
        public override AccessibleObject GetFocused() {
            return GetSelected();
        }
        public override int GetHelpTopic(out string fileName) {
            fileName = "TBD";
            return 0;
        }
        public override AccessibleObject GetSelected() {           
            if (tree.SelectedNode != null) {
                return tree.SelectedNode.AccessibleObject;
            }
            return this;
        }
        public override AccessibleObject HitTest(int x, int y) {
            Point pt = tree.PointToClient(new Point(x, y));
            pt = tree.ApplyScrollOffset(pt);
            TreeNode node = tree.FindNodeAt(pt.X, pt.Y);
            if (node != null) {
                return node.AccessibleObject;
            }
            return this;
        }
        public override AccessibleObject Navigate(AccessibleNavigation navdir) {
            TreeNode node = null;
            TreeNodeCollection children = tree.Nodes;
            int count = children.Count;
            switch (navdir) {
                case AccessibleNavigation.Down:
                case AccessibleNavigation.FirstChild:
                case AccessibleNavigation.Left:
                    if (count > 0) node = children[0];
                    break;
                case AccessibleNavigation.LastChild:
                    return tree.Editor.CompletionSet.AccessibilityObject;                    
                case AccessibleNavigation.Next:
                case AccessibleNavigation.Previous:
                case AccessibleNavigation.Right:
                case AccessibleNavigation.Up:
                    if (count > 0) node = children[count-1];
                    break;
            }
            if (node != null) {
                return node.AccessibleObject;
            }
            return null;
        }
        public override AccessibleObject Parent {
            get {
                return tree.Parent.AccessibilityObject;
            }
        }
        public override AccessibleRole Role {
            get {
                return tree.AccessibleRole;
            }
        }
        public override void Select(AccessibleSelection flags) {
            this.tree.Focus();
        }
        public override AccessibleStates State {
            get {
                AccessibleStates result = AccessibleStates.Focusable | AccessibleStates.Selectable |
                    AccessibleStates.Sizeable;
                if (tree.Focused) result |= AccessibleStates.Focused;
                if (!tree.Visible) result |= AccessibleStates.Invisible;
                return result;
            }
        }

        public override string Value {
            get {
                return "";
            }
            set {
                //???
            }
        }        
    }

    class AccessibleNode : AccessibleObject {
        TreeNode node;
        public AccessibleNode(TreeNode node) {
            this.node = node;
        }
        public override Rectangle Bounds {
            get {
                Rectangle bounds = node.LabelBounds;
                bounds.Offset(node.TreeView.ScrollPosition);
                return node.TreeView.RectangleToScreen(bounds);
            }
        }
        public override string DefaultAction {
            get {
                return "Toggle";
            }
        }
        public override string Description {
            get {
                return "TreeNode";
            }
        }
        public override void DoDefaultAction() {
            node.Toggle();
        }
        public override int GetChildCount() {
            return node.Nodes.Count;
        }
        public override AccessibleObject GetChild(int index) {
            return node.Nodes[index].AccessibleObject;
        }
        public override AccessibleObject GetFocused() {
            return GetSelected();
        }
        public override int GetHelpTopic(out string fileName) {
            fileName = "TBD";
            return 0;
        }
        public override AccessibleObject GetSelected() {
            if (node.Selected) {
                return this;
            }
            return node.TreeView.AccessibilityObject.GetSelected();
        }
        public override string Help {
            get {
                // pack the expanded state in the help field...
                return node.IsExpanded ? "expanded" : "collapsed";
            }
        }
        public override AccessibleObject HitTest(int x, int y) {
            return node.TreeView.AccessibilityObject.HitTest(x, y);
        }
        public override string KeyboardShortcut {
            get {
                return "???";
            }
        }
        public override string Name {
            get {
                return node.Label;
            }
            set {
                // hack alert - this is breaking architectural layering!
                XmlTreeNode xnode = (XmlTreeNode)node;
                xnode.XmlTreeView.UndoManager.Push(new EditNodeName(xnode, value));
            }
        }
        public override AccessibleObject Navigate(AccessibleNavigation navdir) {
            TreeNode result = null;
            TreeNodeCollection children = node.Nodes;
            int count = children.Count;
            switch (navdir) {
                case AccessibleNavigation.Down:
                case AccessibleNavigation.Next:
                    result = node.NextVisibleNode;
                    if (result == null) {
                        return node.TreeView.Editor.CompletionSet.AccessibilityObject;
                    }
                    break;
                case AccessibleNavigation.FirstChild:
                    if (count > 0) result = children[0];
                    if (!node.IsExpanded) node.Expand();
                    break;
                case AccessibleNavigation.Left:
                    // like the left key, this navigates up the parent hierarchy.
                    return node.Parent.AccessibleObject;

                case AccessibleNavigation.Right:
                    // hack - this breaks architectural layering!!!
                    // but it's such a cool feature to be able to navigate via accessibility
                    // over to the NodeTextView.
                    if (node is XmlTreeNode) {
                        XmlTreeNode xn = (XmlTreeNode)node;
                        AccessibleNodeTextView av = (AccessibleNodeTextView)xn.XmlTreeView.NodeTextView.AccessibilityObject;
                        return av.Wrap(node);
                    }
                    break;
                case AccessibleNavigation.LastChild:
                    if (count > 0) result = children[count - 1];
                    if (!node.IsExpanded) node.Expand();
                    break;
                case AccessibleNavigation.Previous:
                case AccessibleNavigation.Up:
                    result = node.PrevVisibleNode;
                    break;
            }
            if (result != null) {
                return result.AccessibleObject;
            }
            return this;
        }
        public override AccessibleObject Parent {
            get {
                if (node.Parent != null) {
                    return node.Parent.AccessibleObject;
                } else {
                    return node.TreeView.AccessibilityObject;
                }
            }
        }
        public override AccessibleRole Role {
            get {
                // this gives us tree view, but we then can't do selection
                //return AccessibleRole.OutlineItem;
                // Selection is more useful so we do List item.
                return AccessibleRole.ListItem;
            }
        }
        public override void Select(AccessibleSelection flags) {
            node.TreeView.Focus();
            if ((flags & AccessibleSelection.TakeSelection) != 0 ||
                (flags & AccessibleSelection.AddSelection) != 0) {
                node.TreeView.SelectedNode = node;
            } else if ((flags & AccessibleSelection.RemoveSelection) != 0) {
                if (node.TreeView.SelectedNode == this.node) {
                    node.TreeView.SelectedNode = null;
                }
            }
        }
        public override AccessibleStates State {
            get {
                AccessibleStates result = AccessibleStates.Focusable | AccessibleStates.Selectable;
                if (node.Selected) result |= AccessibleStates.Focused | AccessibleStates.Selected;
                if (!node.IsVisible) result |= AccessibleStates.Invisible;
                if (node.IsExpanded) result |= AccessibleStates.Expanded;
                else result |= AccessibleStates.Collapsed;
                return result;
            }
        }

        // The ValuePattern on this node should set the node label (node name).
        public override string Value {
            get {
                //string s = node.Text;
                //if (s == null) s = "";
                //return s;
                return node.Label;
            }
            set {
                //// hack alert - this is breaking architectural layering!
                //XmlTreeNode xnode = (XmlTreeNode)node;
                //XmlTreeView xview = xnode.XmlTreeView;
                //xview.UndoManager.Push(new EditNodeValue(xview, xnode, value));

                // hack alert - this is breaking architectural layering!
                XmlTreeNode xnode = (XmlTreeNode)node;
                xnode.XmlTreeView.UndoManager.Push(new EditNodeName(xnode, value));
            }
        }
    }

    internal enum LineState { None = 0, First = 1, Last = 2, HasParent = 4 }

    internal class LineStates {
        LineState[] states = new LineState[10];
        int used;

        public void Push(LineState state) {
            if (used > 0) state |= LineState.HasParent;
            if (states.Length == used) {
                LineState[] na = new LineState[used * 2];
                Array.Copy(states, na, used);
                states = na;
            }
            states[used++] = state;
        }
        public void Pop() {
            Debug.Assert(used > 0);
            if (used > 0) used--;
        }
        public LineState this[int depth] {
            get {
                Debug.Assert(depth < used);
                return (depth < used) ? states[depth] : LineState.None;
            }
        }
        public int Depth { get { return this.used; } }
    }

    public class TreeViewDropFeedback : IDisposable {
        TreeView treeView;
        TreeNode start;
        Point position;
        int indent;
        Pen pen;
        Brush brush;
        GraphicsPath shape;
        int count;
        Point lastPos;
        TreeNode toggled;
        TreeNode lastNode;
        // This indicates where the user has chosen to do the drop, either before the "before"
        // node or "after" the after node.
        TreeNode after;
        TreeNode before;
        Rectangle bounds;

        public Rectangle Bounds {
            get { return this.bounds; }
            set {
                Invalidate();
                this.bounds = value;
                Invalidate();
            }
        }

        public TreeNode After {
            get { return after; }
            set { after = value; }
        }

        public TreeNode Before {
            get { return before; }
            set { before = value; }
        }
        public Point Location {
            get { return this.Bounds.Location; }
            set {
                Invalidate();
                this.bounds.Location = value;
                Invalidate();
            }
        }
        bool visible;
        public bool Visible {
            get {
                return this.visible;
            }
            set {
                if (this.visible != value) {
                    this.visible = value;
                    Invalidate();
                }
            }
        }

        void Invalidate() {
            if (this.treeView != null) {
                Rectangle r = Bounds;
                r.Offset(this.treeView.ScrollPosition);
                this.treeView.Invalidate(r);
            }
        }

        public TreeViewDropFeedback(){
            pen = new Pen(Color.Navy);
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            brush = new SolidBrush(Color.FromArgb(50, Color.Navy));
            this.Visible = false;
        }

        ~TreeViewDropFeedback() {
            Dispose(false);
        }

        public void Dispose(){
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (pen != null){
                pen.Dispose();
                pen = null;
            }
            if (brush != null) {
                brush.Dispose();
                brush = null;
            }
            if (shape != null){
                shape.Dispose();
                shape = null;
            }
        }

        public void Cancel(){
            this.before = this.after = null;
            this.Visible = false;
        }

        public void Finish(bool cancelled){
            if (cancelled){
                Cancel();
            }
            this.Visible = false;
            if (this.treeView != null) this.treeView.dff = null;
        }

        public TreeNode Item {
            get { return this.start; }
            set { this.start = value; }
        }

        public TreeView TreeView {
            get { return this.treeView; }
            set { 
                this.treeView = value;
                this.treeView.dff = this;
                this.indent = value.TreeIndent - 5;
                int height = this.treeView.ItemHeight/2;
                int width = 0;
                if (this.treeView.Nodes.Count>0){
                    // todo: get the size of the item being dragged (from TreeData);
                    TreeNode n = this.treeView.Nodes[0];
                    Rectangle p = n.LabelBounds;
                    width = p.Width;
                }
                shape = new GraphicsPath();
                int h = height/2;
                int w = width;
                Graphics g = value.CreateGraphics();
                using (g) {
                    SizeF size = g.MeasureString(start.Label, this.treeView.Font);
                    h = (int)Math.Max(h, size.Height /2);
                    w = (int)size.Width;
                }
                shape.AddLines(
                    new Point[] {
                                    new Point(0, h/2), 
                                    new Point(indent, h/2),
                                    new Point(indent, 0),
                                    new Point(indent + w, 0),
                                    new Point(indent + w, h),
                                    new Point(indent, h),
                                    new Point(indent, h/2)                        
                                });
                RectangleF r = shape.GetBounds();
                this.Bounds = new Rectangle(0, 0, (int)r.Width+1, (int)r.Height+1);                
            }
        }

        public void ResetToggleCount() {
            count = 0;
        }

        public virtual Point Position {
            get { return this.position; }
            set { 
                if (lastPos == value){
                    count++;
                } else {
                    count = 0;
                }
                lastPos = value;
                if (value == new Point(0,0)){
                    this.position = value;
                    this.Visible = false;
                } else if (this.treeView.Nodes.Count>0 ){
                    Point local = this.treeView.PointToClient(value);
                    Point pos = this.treeView.ApplyScrollOffset(local);
                    TreeNode node = this.treeView.FirstVisibleNode;
                    while (node != null){
                        Rectangle bounds = node.LabelBounds;
                        CheckToggle(node, pos);
                        TreeNode next = node.NextVisibleNode;
                        int h = (int)shape.GetBounds().Height;
                        int iconWidth = this.treeView.ImageList.ImageSize.Width;
                        if (next != null){
                            CheckToggle(next, pos);
                            Rectangle nb = next.LabelBounds;
                            int dy = pos.Y - (bounds.Top+bounds.Bottom)/2;
                            int dy2 = (nb.Top+nb.Bottom)/2 - pos.Y;
                            if (dy >= 0 && dy2 >= 0 ){
                                if (!ContainsNode(start, node) && !ContainsNode(start, next)){
                                    Point midpt;
                                    if (dy < dy2) {
                                        this.before = null;
                                        this.after = node;
                                        midpt = new Point(bounds.Left-indent-iconWidth, bounds.Bottom-h/2);
                                    } else {
                                        this.after = null;
                                        this.before = next;
                                        midpt = new Point(nb.Left-indent-iconWidth, nb.Top-h/2);
                                    }
                                    if (midpt != this.position){
                                        this.position = midpt;                                    
                                        this.Location = this.position;  
                                        if (!this.Visible)
                                            this.Visible = true;
                                    }
                                }
                                break;
                            }
                        } else {
                            Point midpt = new Point(bounds.Left-indent-iconWidth, bounds.Bottom-h/2);
                            if (midpt != this.position && !ContainsNode(start,node)){
                                this.after = node;
                                this.before = null;
                                this.position = midpt;
                                this.Location = this.position;
                                if (!this.Visible)
                                    this.Visible = true;
                            }
                        }
                        node = next;
                    }                  
                }
            }
        }

        void CheckToggle(TreeNode node, Point pos) {
            if (node.LabelBounds.Contains(pos.X, pos.Y)) {
                if (node != toggled){
                    if (count>10 && toggled != lastNode){
                        if (node.Nodes.Count>0){
                            node.Toggle();
                        }
                        count = 0;
                        toggled = node;
                    }                    
                }
                if (lastNode == toggled && node != toggled) {
                    if (toggled != null && !ContainsNode(toggled, node)) 
                        toggled.Toggle(); // revert back so we don't end up expanding the whole tree!
                    toggled = null;
                }
                lastNode = node;
            }
        }

        bool ContainsNode(TreeNode parent, TreeNode node) {
            if (node == null) return false;
            if (parent.Nodes.Count>0){
                foreach (TreeNode n in parent.Nodes) {
                    if (n == node)
                        return true;
                    if (ContainsNode(n, node))
                        return true;
                }
            }
            return false;
        }

        public void Draw(Graphics g) {
            if (this.shape != null) {
                Matrix saved = g.Transform;
                Matrix m = saved.Clone();
                m.Translate(this.Bounds.Left, this.Bounds.Top);
                g.Transform = m;
                g.FillPath(this.brush, this.shape);
                g.DrawPath(this.pen, this.shape);
                g.Transform = saved;
            }
        }
    }

    public delegate void TypeToFindEventHandler(object sender, string toFind);

    public class TypeToFindHandler : IDisposable {
        int start;
        Control control;
        string typedSoFar;
        int resetDelay;
        TypeToFindEventHandler handler;
        bool started;
        Cursor cursor;

        public TypeToFindEventHandler FindString {
            get { return this.handler; }
            set { this.handler = value; }
        }

        public TypeToFindHandler(Control c, int resetDelayInMilliseconds) {
            this.control = c;
            this.resetDelay = resetDelayInMilliseconds;
            this.control.KeyPress += new KeyPressEventHandler(control_KeyPress);
            this.control.KeyDown += new KeyEventHandler(control_KeyDown);
        }

        ~TypeToFindHandler() {
            Dispose(false);
        }

        public bool Started {
            get {
                if (Cursor.Current != this.cursor) started = false;
                return started;
            }
        }

        void control_KeyDown(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.I:
                    if ((e.Modifiers & Keys.Control) != 0) {
                        StartIncrementalSearch();
                    }
                    break;
                case Keys.Escape:
                    if (started) {
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
                    if (started && !e.Control && !e.Alt)
                        e.Handled = true;
                    break;
            }
        }


        public Cursor Cursor {
            get {
                if (cursor == null) {
                    using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("XmlNotepad.Resources.isearch.cur")) {
                        this.cursor = new Cursor(stream);
                    }
                }
                return this.cursor;
            }
        }

        public void StartIncrementalSearch() {
            Cursor.Current = this.Cursor;
            started = true;
        }

        public void StopIncrementalSearch() {
            Cursor.Current = Cursors.Arrow;
            started = false;
            typedSoFar = "";
        }

        void control_KeyPress(object sender, KeyPressEventArgs e) {
            if (started) {
                char ch = e.KeyChar;
                if (ch < 0x20) return; // don't process control characters
                int tick = Environment.TickCount;
                if (tick < start || tick < this.resetDelay || start < tick - this.resetDelay) {
                    typedSoFar = ch.ToString();
                } else {
                    typedSoFar += ch.ToString();
                }
                start = tick;
                if (FindString != null) FindString(this, typedSoFar);
            }
        }

        #region IDisposable Members

        public void Dispose() {
            Dispose(true);
        }

        #endregion

        protected virtual void Dispose(bool disposing){
            if (cursor != null) {
                cursor.Dispose();
                cursor = null;
            }
        }
    }
}