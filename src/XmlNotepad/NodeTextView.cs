using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using SR = XmlNotepad.StringResources;

namespace XmlNotepad
{
    //========================================================================================
    /// <summary>
    /// Displays the text of the attributes, comments, text, cdata and leaf element nodes and 
    /// provides type-to-find and editing of those values.
    /// </summary>
    public class NodeTextView : UserControl, IEditableView
    {

        private Dictionary<TreeNode, string> _visibleTextCache;
        private Color _containerBackground = Color.AliceBlue;
        private TreeNode _selectedNode;
        private Settings _settings;
        private TypeToFindHandler _ttf;
        private Point _scrollPosition;
        private TextEditorOverlay _editor;
        private TreeNodeCollection _nodes;

        public event EventHandler<TreeViewEventArgs> AfterSelect;

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public NodeTextView()
        {
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.Selectable, true);

            InitializeComponent();

            this.SuspendLayout();
            this._editor = new TextEditorOverlay(this);
            this._editor.MultiLine = true;
            this._editor.CommitEdit += new EventHandler<TextEditorEventArgs>(OnCommitEdit);
            this._editor.LayoutEditor += new EventHandler<TextEditorLayoutEventArgs>(OnLayoutEditor);

            this._ttf = new TypeToFindHandler(this, 2000);
            this._ttf.FindString += new TypeToFindEventHandler(FindString);
            this.ResumeLayout();
            this.AccessibleRole = System.Windows.Forms.AccessibleRole.List;

            _visibleTextCache = new Dictionary<TreeNode, string>();
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
            this.Name = this.AccessibleName = "NodeTextView";
            this.AccessibleDescription = "Right hand side of the XmlTreeView for editing node values";

            this.ResumeLayout(false);
        }

        #endregion

        public void Close()
        {
            this._editor.Dispose();
            ClearCache();
        }

        void ClearCache()
        {
            if (_visibleTextCache != null)
                _visibleTextCache.Clear();
        }

        // The nodes to display.
        public TreeNodeCollection Nodes
        {
            get { return this._nodes; }
            set
            {
                this._selectedNode = null;
                this._nodes = value;
                ClearCache();
                PerformLayout();
            }
        }

        [System.ComponentModel.Browsable(false)]
        public UndoManager UndoManager
        {
            get { return (UndoManager)this.Site.GetService(typeof(UndoManager)); }
        }

        [System.ComponentModel.Browsable(false)]
        public IIntellisenseProvider IntellisenseProvider
        {
            get
            {
                return (IIntellisenseProvider)this.Site.GetService(typeof(IIntellisenseProvider));
            }
        }

        public void SetSite(ISite site)
        {
            // Overriding the Site property directly breaks the WinForms designer.
            this.Site = site;
            this._editor.Site = site;

            XmlCache model = (XmlCache)site.GetService(typeof(XmlCache));
            model.ModelChanged += new EventHandler<ModelChangedEventArgs>(OnModelChanged);
        }

        internal void OnLoaded()
        {
            this._settings = (Settings)this.Site.GetService(typeof(Settings));
            if (this._settings != null)
            {
                this._settings.Changed -= new SettingsEventHandler(OnSettingsChanged);
                this._settings.Changed += new SettingsEventHandler(OnSettingsChanged);
            }
            OnSettingsChanged(this, "Colors");
            OnSettingsChanged(this, "MaximumValueLength");
        }


        void OnModelChanged(object sender, ModelChangedEventArgs e)
        {
            ClearCache();
        }

        public Point ScrollPosition
        {
            get { return this._scrollPosition; }
            set { this._scrollPosition = value; }
        }

        public Point ApplyScrollOffset(int x, int y)
        {
            return new Point(x - this._scrollPosition.X, y - this._scrollPosition.Y);
        }

        public Point ApplyScrollOffset(Point pt)
        {
            return new Point(pt.X - this._scrollPosition.X, pt.Y - this._scrollPosition.Y);
        }

        private void OnSettingsChanged(object sender, string name)
        {
            // change the colors.
            Invalidate();
            if (this._settings != null)
            {
                switch (name)
                {
                    case "Colors":
                        var theme = (ColorTheme)this._settings["Theme"];
                        string colorSetName = theme == ColorTheme.Light ? "LightColors" : "DarkColors";
                        ThemeColors colors = (ThemeColors)this._settings[colorSetName];
                        if (colors != null)
                        {
                            this._containerBackground = colors.ContainerBackground;
                            this._editor.EditorBackgroundColor = colors.EditorBackground;
                        }
                        break;
                    case "MaximumValueLength":
                        // text editor maximum length.
                        var max = (int)_settings["MaximumValueLength"];
                        if (max == 0)
                        {
                            max = short.MaxValue;
                        }
                        this._editor.MaximumLineLength = max;
                        break;
                }
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            if (this._selectedNode != null)
            {
                Invalidate(this._selectedNode);
            }
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            if (this._selectedNode != null)
            {
                Invalidate(this._selectedNode);
            }
            base.OnLostFocus(e);
        }

        public void Reset()
        {
            this._editor.EndEdit(false);
            this._selectedNode = null;
            Invalidate();
        }

        public TreeNode SelectedNode
        {
            get { return this._selectedNode; }
            set
            {
                if (this._selectedNode != value)
                {
                    this._editor.EndEdit(false);
                    InternalSelect(value);
                    if (AfterSelect != null)
                    {
                        AfterSelect(this, new TreeViewEventArgs(value, TreeViewAction.None));
                    }
                }
            }
        }

        internal void InternalSelect(TreeNode node)
        {
            this._selectedNode = node;
            Invalidate();
        }

        static bool IsTextEditable(TreeNode node)
        {
            NodeImage img = (NodeImage)(node.ImageIndex + 1);
            return !(img == NodeImage.Element || img == NodeImage.OpenElement);

        }

        protected override void OnMove(EventArgs e)
        {
            this._editor.EndEdit(false);
            base.OnMove(e);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            ClearCache();
            if (this._editor.IsEditing)
            {
                this._editor.PerformLayout();
            }
        }

        void OnLayoutEditor(object sender, TextEditorLayoutEventArgs args)
        {
            Rectangle r = this.GetTextBounds(this._selectedNode);
            r.Offset(this._scrollPosition);
            args.PreferredBounds = r;
            args.MaxBounds = r;
        }

        string CheckTextLength(string text, out bool cancelled)
        {
            cancelled = false;
            if (text == null) return "";
            int maxLine = GetMaxLineLength(text);
            if (maxLine > (int)this._settings["MaximumLineLength"])
            {

                DialogResult rc = DialogResult.No;

                if ((bool)this._settings["AutoFormatLongLines"])
                {
                    rc = DialogResult.Yes;
                }
                else
                {
                    rc = MessageBox.Show(this, SR.LongLinePrompt, SR.LongLineCaption,
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
                }

                if (rc == DialogResult.Cancel)
                {
                    cancelled = true;
                    return text;
                }
                if (rc == DialogResult.No)
                {
                    return text;
                }
                return FormatLines(text);
            }
            return text;
        }

        private static string FormatLines(string text)
        {
            StringBuilder sb = new StringBuilder();
            int lineStart = 0;
            int j = 0;
            int spaces = 0;
            for (int i = 0, len = text.Length; i < len; i++)
            {
                char c = text[i];
                bool lineEnd = false;
                if (c == '\r')
                {
                    if (i + 1 < len && text[i + 1] == '\n') i++;
                    lineEnd = true;
                }
                else if (c == '\n')
                {
                    lineEnd = true;
                }
                else if (c == ' ' || c == '\t')
                {
                    spaces++;
                }

                if (lineEnd)
                {
                    string line = text.Substring(lineStart, i + 1 - lineStart);
                    sb.Append(line);
                    lineStart = i + 1;
                    j = 0;
                }
                else if (++j >= 80)
                {
                    if (spaces == 0 || (c == ' ' || c == '\t'))
                    {
                        // try and split on word boundary.
                        string line = text.Substring(lineStart, j);
                        sb.Append(line);
                        sb.Append("\r\n");
                        lineStart = i + 1;
                        j = 0;
                    }
                }
                else if (i + 1 == len)
                {
                    string line = text.Substring(lineStart);
                    sb.Append(line);
                }
            }
            return sb.ToString();
        }

        private static int GetMaxLineLength(string text)
        {
            int maxLine = 0;
            int lastLine = 0;
            for (int i = 0, len = text.Length; i < len; i++)
            {
                char c = text[i];
                bool lineEnd = false;
                if (c == '\r')
                {
                    if (i + 1 < len && text[i + 1] == '\n') i++;
                    lineEnd = true;
                }
                else if (c == '\n')
                {
                    lineEnd = true;
                }
                if (lineEnd || i + 1 == len)
                {
                    int linelen = i - lastLine;
                    lastLine = i + 1;
                    maxLine = Math.Max(maxLine, linelen);
                }
            }
            return maxLine;
        }

        public bool FocusBeginEdit(string value)
        {
            this.Focus();
            return BeginEdit(value);
        }

        #region IEditableView
        public bool BeginEdit(string value)
        {
            if (this._selectedNode != null)
            {
                if (string.IsNullOrEmpty(this._selectedNode.Label))
                {
                    MessageBox.Show(this, SR.NodeNameRequiredPrompt, SR.NodeNameRequiredCaption,
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                else if (IsTextEditable(this._selectedNode))
                {
                    // see if control has possible values that cannot be known in xsd
                    IIntellisenseProvider provider = this.IntellisenseProvider;
                    string text = value != null ? value : GetNodeText(this._selectedNode);
                    if (provider != null)
                    {
                        provider.SetContextNode(this._selectedNode as IXmlTreeNode);
                        if (!provider.IsValueEditable)
                        {
                            return false;
                        }
                    }
                    bool cancel = false;
                    text = CheckTextLength(text, out cancel);
                    if (cancel) return false;
                    this._editor.BeginEdit(text, provider, EditMode.Value, this._selectedNode.ForeColor, this.Focused);
                    return true;
                }
            }
            return false;
        }

        public bool IsEditing
        {
            get { return this._editor.IsEditing; }
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
                return this._editor.Replace(index, length, replacement);
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

        public TextEditorOverlay Editor
        {
            get { return this._editor; }
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
            if (!args.Cancelled)
            {
                SetNodeText(this._selectedNode, args.Text);
            }
        }

        public void StartIncrementalSearch()
        {
            _ttf.StartIncrementalSearch();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = (keyData & ~Keys.Modifiers);
            switch (key)
            {
                case Keys.F2:
                case Keys.Enter:
                case Keys.Home:
                case Keys.End:
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

            bool isLetterOrDigit = ((e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z) ||
                       (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)) &&
                       (e.Modifiers == Keys.Shift || e.Modifiers == 0);

            switch (e.KeyCode)
            {
                case Keys.F2:
                case Keys.Enter:
                    BeginEdit(null);
                    e.Handled = true;
                    break;
                case Keys.Left:
                    // do not give to tree view, this will do a shift-tab instead.
                    break;
                default:
                    if (isLetterOrDigit && !e.Handled && this.ContainsFocus)
                    {
                        if (_ttf.Started)
                        {
                            e.Handled = true; // let ttf handle it!
                        }
                        else
                        {
                            char ch = Convert.ToChar(e.KeyValue);
                            if (!e.Shift) ch = Char.ToLower(ch);
                            if (this.BeginEdit(ch.ToString()))
                            {
                                this._editor.SelectEnd();
                                e.Handled = true;
                            }
                        }
                    }
                    break;
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle clip = e.ClipRectangle;
            Graphics g = e.Graphics;
            Matrix m = g.Transform;
            m.Translate(this._scrollPosition.X, this._scrollPosition.Y);
            g.Transform = m;

            base.OnPaint(e);
            if (this._nodes != null)
            {
                clip = new Rectangle(this.ApplyScrollOffset(clip.X, clip.Y), new Size(clip.Width, clip.Height));
                PaintNodes(this._nodes, g, ref clip);
            }
        }

        void PaintNodes(TreeNodeCollection nodes, Graphics g, ref Rectangle clip)
        {
            if (nodes == null) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (TreeNode n in nodes)
            {
                Rectangle r = GetTextBounds(n);
                if (r.Top > clip.Bottom) return;
                if (r.IntersectsWith(clip))
                {
                    DrawItem(r, n, g);
                }
                if (n.IsExpanded)
                {
                    PaintNodes(n.Children, g, ref clip);
                }
            }
        }

        static string GetNodeText(TreeNode n)
        {
            string text = n.Text;
            return NormalizeNewLines(text);
        }

        public static string NormalizeNewLines(string text)
        {
            if (text == null) return null;
            StringBuilder sb = new StringBuilder();
            for (int i = 0, n = text.Length; i < n; i++)
            {
                char ch = text[i];
                if (ch == '\r')
                {
                    if (i + 1 < n && text[i + 1] == '\n')
                        i++;
                    sb.Append("\r\n");
                }
                else if (ch == '\n')
                {
                    sb.Append("\r\n");
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        static void SetNodeText(TreeNode n, string value)
        {
            n.Text = value;
        }

        public void Invalidate(TreeNode n)
        {
            if (n != null)
            {
                Rectangle r = this.GetTextBounds(n);
                r.Offset(this._scrollPosition);
                Invalidate(r);
            }
        }

        private void DrawItem(Rectangle bounds, TreeNode tn, Graphics g)
        {

            //g.SmoothingMode = SmoothingMode.AntiAlias;
            Color c = tn.ForeColor;

            Brush myBrush = null;
            bool focusSelected = false;
            if (this.Focused && tn == this.SelectedNode)
            {
                focusSelected = true;
                g.FillRectangle(SystemBrushes.Highlight, bounds);
                myBrush = Brushes.HighlightTextBrush(c);
            }
            else
            {
                myBrush = new SolidBrush(c);
            }

            Font font = this.Font;
            Rectangle inset = new Rectangle(bounds.Left + 3, bounds.Top, bounds.Width - 3, bounds.Height);

            string value = null;
            if (this._visibleTextCache.ContainsKey(tn))
            {
                value = this._visibleTextCache[tn];
            }
            else
            {
                value = GetNodeText(tn);
            }
            if (value == null && !focusSelected)
            {
                using (Brush b = new SolidBrush(_containerBackground))
                {
                    g.FillRectangle(b, bounds);
                }
            }

            if (value != null && value.Length > 0)
            {

                //inset.Inflate(-3, -2);

                char ellipsis = Convert.ToChar(0x2026);

                value = value.Trim();

                int i = value.IndexOfAny(new char[] { '\r', '\n' });
                if (i > 0)
                {
                    value = value.Substring(0, i) + ellipsis;
                }

                // Figure out how much of the text we can display                
                int width = inset.Width; ;
                //int height = inset.Height;
                string s = value;
                if (width < 0) return;
                int length = value.Length;
                SizeF size = SizeF.Empty;
                bool measurable = false;
                while (!measurable)
                {
                    try
                    {
                        if (s.Length >= 65536)
                        {
                            // MeasureString tops out at 64kb strings.
                            s = s.Substring(0, 65535);
                        }
                        size = g.MeasureString(s, font, width + 1000, StringFormat.GenericTypographic);
                        measurable = true;
                    }
                    catch (Exception)
                    {
                        // perhaps the string is just too long!
                        s = s.Substring(0, s.Length / 2);
                    }
                }
                int j = s.Length;
                int dy = (font.Height - (int)Math.Ceiling(size.Height)) / 2;
                if (dy < 0) dy = 0;
                char[] ws = new char[] { ' ', '\t' };
                if ((int)size.Width > width && j > 1)
                { // line wrap?
                    int start = 0;
                    int w = 0;
                    int k = value.IndexOfAny(ws);
                    while (k > 0)
                    {
                        s = value.Substring(0, k) + ellipsis;
                        size = g.MeasureString(s, font, width + 1000, StringFormat.GenericTypographic);
                        if ((int)size.Width < width && k < length)
                        {
                            start = k;
                            w = (int)size.Width;
                            while (start < length && (value[start] == ' ' || value[start] == '\t'))
                            {
                                start++;
                            }
                            k = value.IndexOfAny(ws, start);
                        }
                        else
                        {
                            break;
                        }
                    }
                    j = start;
                    if (w < width / 2)
                    {
                        // if we have a really long word (e.g. binhex) then just take characters
                        // up to the end of the line.                        
                        while ((int)w < width && j < length)
                        {
                            j++;
                            s = value.Substring(0, j) + ellipsis;
                            size = g.MeasureString(s, font, width + 1000, StringFormat.GenericTypographic);
                            w = (int)size.Width;
                        }
                    }
                    if (j <= 0)
                    {
                        s = "";
                    }
                    else if (j < length)
                    {
                        s = value.Substring(0, j - 1) + ellipsis;
                    }

                    this._visibleTextCache[tn] = s;
                }


                // Draw the current item text based on the current Font and the custom brush settings.
                g.DrawString(s, font, myBrush, inset.Left, dy + inset.Top, StringFormat.GenericTypographic);
            }

            // If the ListBox has focus, draw a focus rectangle around the selected item.
            if (tn == this.SelectedNode)
            {
                g.SmoothingMode = SmoothingMode.Default;
                Pen p = new Pen(Color.Black, 1);
                p.DashStyle = DashStyle.Dot;
                p.Alignment = PenAlignment.Inset;
                p.LineJoin = LineJoin.Round;
                bounds.Width--;
                bounds.Height--;
                g.DrawRectangle(p, bounds);
                p.Dispose();
            }

            myBrush.Dispose();

        }


        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            CurrentEvent.Event = e;
            this._editor.EndEdit(false);
            if (this._nodes != null)
            {
                Point p = this.ApplyScrollOffset(e.X, e.Y);
                TreeNode tn = this.FindNodeAt(this._nodes, p.X, p.Y);
                if (tn != null)
                {
                    if (this.SelectedNode == tn && this.Focused)
                    {
                        if (e.Button == MouseButtons.Left)
                        {
                            this.BeginEdit(null);
                        }
                        return;
                    }
                    else
                    {
                        this.SelectedNode = tn;
                    }
                }
            }
            this.Focus();
        }

        public TreeNode FindNodeAt(TreeNodeCollection nodes, int x, int y)
        {
            if (nodes == null) return null;
            foreach (TreeNode n in nodes)
            {
                Rectangle r = GetTextBounds(n);
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
            return null;
        }

        public Rectangle GetTextBounds(TreeNode n)
        {
            Rectangle r = new Rectangle(0, n.LabelBounds.Top, this.Width, n.LabelBounds.Height);
            return r;
        }

        void FindString(object sender, string toFind)
        {
            TreeNode node = this.SelectedNode;

            if (node == null) node = this.FirstVisibleNode;
            TreeNode start = node;
            while (node != null)
            {
                string s = GetNodeText(node);
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

        AccessibleNodeTextView acc;
        protected override AccessibleObject CreateAccessibilityInstance()
        {
            if (this.acc == null) this.acc = new AccessibleNodeTextView(this);
            return this.acc;
        }
    }

    class AccessibleNodeTextView : Control.ControlAccessibleObject
    {
        private NodeTextView _view;
        private Dictionary<TreeNode, AccessibleObject> _cache = new Dictionary<TreeNode, AccessibleObject>();

        public AccessibleNodeTextView(NodeTextView view)
            : base(view)
        {
            this._view = view;
        }

        public NodeTextView View { get { return this._view; } }

        public override Rectangle Bounds
        {
            get
            {
                return _view.RectangleToScreen(_view.ClientRectangle);
            }
        }
        public override string DefaultAction
        {
            get
            {
                return "Edit";
            }
        }
        public override void DoDefaultAction()
        {
            if (_view.SelectedNode != null)
            {
                _view.FocusBeginEdit(null);
            }
        }
        public override int GetChildCount()
        {
            return _view.Nodes.Count;
        }
        public override AccessibleObject GetChild(int index)
        {
            TreeNode node = _view.Nodes[index];
            return Wrap(node);
        }

        public AccessibleObject Wrap(TreeNode node)
        {
            if (node == null) return null;
            AccessibleObject a;
            _cache.TryGetValue(node, out a);
            if (a == null)
            {
                a = new AccessibleNodeTextViewNode(this, node);
                _cache[node] = a;
            }
            return a;
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
            if (_view.SelectedNode != null)
            {
                return Wrap(_view.SelectedNode);
            }
            return this;
        }
        public override AccessibleObject HitTest(int x, int y)
        {
            Point pt = _view.PointToClient(new Point(x, y));
            pt = _view.ApplyScrollOffset(pt);
            TreeNode node = _view.FindNodeAt(_view.Nodes, pt.X, pt.Y);
            if (node != null)
            {
                return Wrap(node);
            }
            return this;
        }

        public override AccessibleObject Navigate(AccessibleNavigation navdir)
        {
            TreeNode node = null;
            TreeNodeCollection children = _view.Nodes;
            int count = children.Count;
            switch (navdir)
            {
                case AccessibleNavigation.Left:
                case AccessibleNavigation.Down:
                case AccessibleNavigation.FirstChild:
                    if (count > 0) node = children[0];
                    break;
                case AccessibleNavigation.Next:
                case AccessibleNavigation.Previous:
                case AccessibleNavigation.Right:
                case AccessibleNavigation.Up:
                    if (count > 0) node = children[count - 1];
                    break;

                case AccessibleNavigation.LastChild:
                    // special meaning for us, it means find the intellisense popup window!
                    return _view.Editor.CompletionSet.AccessibilityObject;
            }
            if (node != null)
            {
                return Wrap(node);
            }
            return this;
        }
        public override AccessibleObject Parent
        {
            get
            {
                return _view.Parent.AccessibilityObject;
            }
        }
        public override AccessibleRole Role
        {
            get
            {
                return _view.AccessibleRole;
            }
        }
        public override void Select(AccessibleSelection flags)
        {
            this._view.Focus();
        }
        public override AccessibleStates State
        {
            get
            {
                AccessibleStates result = AccessibleStates.Focusable | AccessibleStates.Selectable |
                    AccessibleStates.Sizeable;
                if (_view.Focused) result |= AccessibleStates.Focused;
                if (!_view.Visible) result |= AccessibleStates.Invisible;
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

    class AccessibleNodeTextViewNode : AccessibleObject
    {
        private TreeNode _node;
        private NodeTextView _view;
        private AccessibleNodeTextView _acc;

        public AccessibleNodeTextViewNode(AccessibleNodeTextView acc, TreeNode node)
        {
            this._acc = acc;
            this._view = acc.View;
            this._node = node;
        }
        public override Rectangle Bounds
        {
            get
            {
                Rectangle bounds = _view.GetTextBounds(_node);
                bounds.Offset(_view.ScrollPosition);
                return _view.RectangleToScreen(bounds);
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
                return "TextNode";
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
            TreeNode child = this._node.Children[index];
            return _acc.Wrap(child);
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
            return _acc.GetSelected();
        }
        public override string Help
        {
            get
            {
                return "TBD";
            }
        }
        public override AccessibleObject HitTest(int x, int y)
        {
            return _acc.HitTest(x, y);
        }
        public override string KeyboardShortcut
        {
            get
            {
                return "TBD";
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
                _view.UndoManager.Push(new EditNodeName(xnode, value));
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
                        return _view.Editor.CompletionSet.AccessibilityObject;
                    }
                    break;
                case AccessibleNavigation.FirstChild:
                    if (count > 0) result = children[0];
                    if (!_node.IsExpanded) _node.Expand();
                    break;
                case AccessibleNavigation.Left:
                    return _node.AccessibleObject;
                case AccessibleNavigation.Right:
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
                return _acc.Wrap(result);
            }
            return this;
        }
        public override AccessibleObject Parent
        {
            get
            {
                if (_node.Parent != null)
                {
                    return _acc.Wrap(_node.Parent);
                }
                else
                {
                    return _acc;
                }
            }
        }
        public override AccessibleRole Role
        {
            get
            {
                return AccessibleRole.ListItem;
            }
        }
        public override void Select(AccessibleSelection flags)
        {
            _view.Focus();
            if ((flags & AccessibleSelection.TakeSelection) != 0 ||
                (flags & AccessibleSelection.AddSelection) != 0)
            {
                _view.SelectedNode = _node;
            }
            else if ((flags & AccessibleSelection.RemoveSelection) != 0)
            {
                if (_view.SelectedNode == this._node)
                {
                    _view.SelectedNode = null;
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
        public override string Value
        {
            get
            {
                string s = this._node.Text;
                if (s == null) s = "";
                return s;
            }
            set
            {
                // hack alert - this is breaking architectural layering!
                XmlTreeNode xnode = (XmlTreeNode)_node;
                XmlTreeView xview = xnode.XmlTreeView;
                _view.UndoManager.Push(new EditNodeValue(xview, xnode, value));
            }
        }
    }

}