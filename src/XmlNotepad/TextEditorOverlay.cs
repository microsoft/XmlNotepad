using System;
using System.Xml;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.ComponentModel;
using System.Text;
using System.Runtime.InteropServices;
using System.Xml.Schema;

namespace XmlNotepad {
    
    public class TextEditorEventArgs : EventArgs {
        string text;
        bool cancelled;

        public string Text {
            get { return text; }
        }

        public bool Cancelled {
            get { return cancelled; }
            set { this.cancelled = value; }
        }

        public TextEditorEventArgs(string text, bool cancelled) {
            this.text = text;
            this.cancelled = cancelled;
        }
    }

    public class TextEditorLayoutEventArgs : EventArgs {
        string text;
        Rectangle preferredBounds;
        Rectangle maxBounds;
        
        public string Text {
            get { return text; }
        }
        
        public Rectangle PreferredBounds {
            get { return preferredBounds; }
            set { preferredBounds = value; }
        }

        public Rectangle MaxBounds {
            get { return maxBounds; }
            set { maxBounds = value; }
        }

        public TextEditorLayoutEventArgs(string text) {
            this.text = text; 
        }
    }

    public enum EditMode { Name, Value };

    public class TextEditorOverlay : IDisposable {
        private TextBox textEditor;
        private Control parent;
        private bool autoSize;
        private CompletionSet cset;
        private IXmlEditor editor;
        private Control currentEditor;
        private XmlSchemaType schemaType;
        private ISite site;

        public event EventHandler<TextEditorEventArgs> CommitEdit;
        public event EventHandler<TextEditorLayoutEventArgs> LayoutEditor;

        public TextEditorOverlay(Control parent) {
            this.parent = parent;
            this.textEditor = new TextBox();
            string name = parent.Name + "Editor"; 
            this.textEditor.Name = name;
            this.textEditor.AccessibleName = name;
            this.textEditor.Visible = false;
            this.textEditor.BorderStyle = BorderStyle.None;
            this.textEditor.BackColor = Color.LightSteelBlue;
            this.textEditor.AutoSize = false;
            this.textEditor.Multiline = true; // this fixes layout problems in single line case also.
            this.textEditor.Margin = new Padding(1, 0, 0, 0);
            this.textEditor.HideSelection = false;
            parent.Controls.Add(this.textEditor);
            this.textEditor.KeyDown += new KeyEventHandler(editor_KeyDown);
            this.textEditor.LostFocus += new EventHandler(editor_LostFocus);
            this.textEditor.GotFocus += new EventHandler(editor_GotFocus);
            this.textEditor.TextChanged += new EventHandler(editor_TextChanged);
            this.currentEditor = this.textEditor;

            this.cset = new CompletionSet(this.textEditor);
            this.cset.KeyDown += new KeyEventHandler(editor_KeyDown);
            this.cset.DoubleClick += new EventHandler(cset_DoubleClick);
        }

        ~TextEditorOverlay() {
            Dispose(false);
        }

        public ISite Site {
            get { return site; }
            set { site = this.cset.Site = value; }
        }

        public bool AutoSize {
            get { return this.autoSize; }
            set { this.autoSize = value; }
        }

        public bool MultiLine {
            get { 
                return this.textEditor.Multiline;
            }
            set {
                this.textEditor.ScrollBars = value ? ScrollBars.Vertical : ScrollBars.None;
                this.textEditor.Multiline = value;  
            }
        }

        public bool IsEditing { get { return this.currentEditor.Visible; } }

        public Rectangle Bounds {
            get { return currentEditor.RectangleToScreen(currentEditor.ClientRectangle); }
        }

        internal CompletionSet CompletionSet {
            get { return this.cset; }
        }

        public void PerformLayout() {
            if (this.LayoutEditor != null) {
                TextEditorLayoutEventArgs args = new TextEditorLayoutEventArgs(this.currentEditor.Text);
                LayoutEditor(this, args);
                SetEditorBounds(args);
            }
        }        

        public void BeginEdit(string text, IIntellisenseProvider provider, EditMode mode, Color color, bool focus) {
            IXmlBuilder builder = null;

            IIntellisenseList list = null;
            if (focus) {
                switch (mode) {
                    case EditMode.Value:
                        builder = provider.Builder;
                        this.cset.Builder = builder;
                        this.editor = provider.Editor;
                        if (this.editor != null) {
                            this.editor.Site = this.site;
                        }
                        list = provider.GetExpectedValues();
                        break;
                    case EditMode.Name:
                        list = provider.GetExpectedNames();
                        break;
                }
            }
            this.schemaType = provider.GetSchemaType();

            if (this.editor != null) {
                this.currentEditor = this.editor.Editor;
                parent.Controls.Add(this.currentEditor);
                this.editor.SchemaType = this.schemaType;    
                this.currentEditor.KeyDown += new KeyEventHandler(editor_KeyDown);
                this.editor.XmlValue = text;
            } else {
                this.currentEditor = this.textEditor;
                this.currentEditor.Text = text;
            }

            this.currentEditor.ForeColor = color;            
            PerformLayout();
            this.currentEditor.Visible = true;
            if (focus) {
                this.currentEditor.Focus();
                if (this.currentEditor == this.textEditor) {
                    this.textEditor.SelectAll();
                }

                // see if this node needs a dropdown.
                if (builder != null || (list != null && list.Count > 0)) {
                    cset.BeginEdit(list, this.schemaType);
                } 
            }

        }

        public void SelectEnd() {
            if (this.currentEditor == this.textEditor && this.currentEditor.Visible) {
                this.textEditor.SelectionStart = this.textEditor.Text.Length;
            }
        }

        public void Select(int index, int length) {
            if (this.currentEditor == this.textEditor && this.currentEditor.Visible) {
                this.textEditor.SelectionStart = index;
                this.textEditor.SelectionLength = length;
            }
        }

        public int SelectionStart { get { return this.textEditor.SelectionStart; } }

        public int SelectionLength { get { return this.textEditor.SelectionLength; } }

        public bool Replace(int index, int length, string replacement) {
            if (this.currentEditor == this.textEditor && this.currentEditor.Visible) {
                int end = index + length;
                string s = this.currentEditor.Text;
                string head = (index > 0) ? s.Substring(0, index) : "";
                string tail = (end < s.Length) ? s.Substring(end) : "";
                this.currentEditor.Text = head + replacement + tail;
                return true;
            }
            return false;
        }

        bool cancel;
        Timer at;
        public void StartEndEdit(bool cancel) {
            this.cancel = cancel;
            if (at == null) {
                at = new Timer();
                at.Interval = 10;
                at.Tick += new EventHandler(OnEndTick);
            }
            at.Start();
        }

        void OnEndTick(object sender, EventArgs e) {
            at.Stop();
            EndEdit(cancel);
        }

        bool ending;
        public bool EndEdit(bool cancel) {
            if (ending) return false; // don't let it be re-entrant!
            ending = true;
                            
            cset.EndEdit(cancel);
            try {
                if (this.currentEditor.Visible) {
                    if (this.CommitEdit != null) {
                        string value = (this.editor != null) ? this.editor.XmlValue : this.currentEditor.Text;
                        TextEditorEventArgs args = new TextEditorEventArgs(value, cancel);
                        CommitEdit(this, args);
                        if (args.Cancelled && !cancel)
                            return false;
                    }
                    HideEdit();
                }
            } catch (Exception e) {
                MessageBox.Show(e.Message, SR.EditErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            } finally {                
                ending = false;
            }
            return true;
        }

        const ushort WM_CHAR = 0x0102;
        Keys lastKey;

        private void editor_KeyDown(object sender, KeyEventArgs e) {
            CurrentEvent.Event = e;
            this.lastKey = e.KeyCode;
            switch (e.KeyCode) {
                case Keys.Enter:
                    if (e.Modifiers == 0) {
                        e.SuppressKeyPress = true;
                        e.Handled = true;
                        StartEndEdit(false); // must be async!
                    }
                    break;
                case Keys.Escape:
                    e.Handled = true;
                    if (this.cset.ToolTipVisible) {
                        this.cset.HideToolTip();
                    } else if (this.cset.Visible) {
                        this.cset.EndEdit(true);
                        this.currentEditor.Focus();
                    } else {
                        StartEndEdit(true);
                    }
                    break;
                default:
                    IEditableView v = this.parent as IEditableView;
                    if (v != null){
                        bool old = e.SuppressKeyPress;
                        e.SuppressKeyPress = true;
                        try {
                            e.Handled = false;
                            v.BubbleKeyDown(e);
                        } finally {
                            e.SuppressKeyPress = old;
                        }
                    }
                    break;
            }
        }

        void cset_DoubleClick(object sender, EventArgs e) {
            EndEdit(false);
        }

        private void editor_LostFocus(object sender, EventArgs e){
            if (!parent.ContainsFocus && !cset.ContainsFocus){
                EndEdit(false);
            }            
        }

        private void editor_GotFocus(object sender, EventArgs e) {
            return;
        }

        private void editor_TextChanged(object sender, EventArgs e) {
            PerformLayout();
        }

        void AdjustBounds(string text, Graphics g, TextEditorLayoutEventArgs args) {
            Rectangle r = args.PreferredBounds;
            if (AutoSize) {
                if (string.IsNullOrEmpty(text))
                    text = "W"; // double the size if it was empty to begin with.
                text += "W"; // leave room to grow by one more char
            }
            if (text.EndsWith("\n")) text += "."; // cause MeasureString to include space for newlines.
            SizeF size = SizeF.Empty;
            int maxHeight = (this.parent.Height * 2) / 3;
            try {
                if (text.Length >= 10000) {
                    // MeasureString gets too slow after a certain size.
                    text = text.Substring(0, 10000);
                }
                size = g.MeasureString(text, this.parent.Font, this.parent.Width, StringFormat.GenericDefault);
            } catch (Exception) {
                // string might be too long to measure!
                size = new SizeF(r.Width, maxHeight);
            }
            int h = (int)Math.Max(r.Height, Math.Ceiling(size.Height));
            if (h > r.Height ) {
                // when multiline, add padding so that it aligns correctly.
                h += this.parent.Font.Height / 2;
            }            
            r.Height = Math.Min(maxHeight, h); // no more than 2/3rd of the window.
            if (AutoSize) {
                r.Width = Math.Max(r.Width, (int)size.Width + 2);
            }
            if (r.Right > args.MaxBounds.Right)
                r.Width = args.MaxBounds.Right - r.Left;
            args.PreferredBounds = r;
        }

        void SetEditorBounds(TextEditorLayoutEventArgs args) {
            string text = this.currentEditor.Text;
            Graphics g = this.parent.CreateGraphics();            
            using (g) {
                AdjustBounds(text, g, args);
            }
            Rectangle r = args.PreferredBounds;
            if (r.Bottom > this.parent.Height) {
                // todo: scroll the view so we don't have to pop-up backwards, but this is tricky because
                // we may need to scroll more than the XmlTreeView scrollbar maximum in the case where the 
                // last node has a lot of text...
                r.Offset(new Point(0, this.parent.Height - r.Bottom));
            }
            int maxHeight = (this.parent.Height * 2) / 3;
            if (r.Height < maxHeight) {
                int h = this.currentEditor.PreferredSize.Height;
                if (r.Height < h) {
                    r.Y -= (h - r.Height) / 2;
                    if (r.Y < 0) r.Y = 0;
                    r.Height = h;
                }
            }
            Rectangle or = this.currentEditor.Bounds;
            if (or.Left != r.Left || or.Right != r.Right || or.Top != r.Top || or.Bottom != r.Bottom) {
                this.currentEditor.Bounds = r;
            }
        }

        void HideEdit() {
            if (this.currentEditor.Visible) {
                bool wasFocused = this.currentEditor.ContainsFocus || this.cset.ContainsFocus;
                this.currentEditor.Visible = false;
                this.cset.Visible = false;
                if (this.editor != null && this.currentEditor != this.textEditor) {
                    parent.Controls.Remove(this.currentEditor);
                    this.currentEditor.KeyDown -= new KeyEventHandler(editor_KeyDown);
                    this.currentEditor.LostFocus -= new EventHandler(editor_LostFocus);
                    this.currentEditor = this.textEditor;
                    this.editor = null;
                }
                if (wasFocused) this.parent.Focus();
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (this.textEditor != null) {
                this.parent.Controls.Remove(this.textEditor);
                this.textEditor.Dispose();
                this.textEditor = null;
                this.textEditor = null;
            }
            if (this.cset != null) {
                this.cset.Builder = null;
                this.cset.Dispose();
                this.cset = null;
            }
            // do NOT dispose this guy, it's owned by someone else.
            this.editor = null;
        }
    }

    // This class is used to display a set of names and icons, track user edits
    // in associated text box and automatically select the item that best matches
    // what the user is doing.
    class CompletionSet : Control {
        bool parented;
        TextBox editor;
        ListBox listBox;
        IXmlBuilder builder;
        Button button;
        XmlSchemaType type;
        IIntellisenseList list;
        IntelliTip tip;

        public CompletionSet(TextBox editor) {
            this.SetStyle(ControlStyles.Selectable, true);
            this.listBox = new ListBox();
            this.listBox.Name = "CompletionList";
            this.listBox.AccessibleName = "CompletionList";
            this.listBox.BorderStyle = BorderStyle.Fixed3D;
            this.listBox.KeyDown += new KeyEventHandler(listBox_KeyDown);
            this.listBox.DoubleClick += new EventHandler(listBox_DoubleClick);
            this.listBox.AutoSize = true;
            this.listBox.SelectedIndexChanged += new EventHandler(listBox_SelectedIndexChanged);

            this.editor = editor;
            this.editor.TextChanged += new EventHandler(editor_TextChanged);
            this.editor.KeyDown += new KeyEventHandler(editor_KeyDown);

            this.button = new Button();
            this.button.Name = "BuilderButton";
            this.button.AccessibleName = "BuilderButton";
            this.button.Visible = false;
            this.button.Click += new EventHandler(button_Click);
            this.button.AutoSize = true;
            this.button.KeyDown += new KeyEventHandler(editor_KeyDown);

            this.Visible = false;
            this.tip = new IntelliTip(editor);
            this.tip.AddWatch(this.listBox);
            this.tip.ShowToolTip += new IntelliTipEventHandler(OnShowToolTip);

            this.Controls.Add(this.listBox);
            this.Controls.Add(this.button);

            this.AccessibleName = "CompletionSet";
        }

        void listBox_SelectedIndexChanged(object sender, EventArgs e) {
            this.HideToolTip();
            this.tip.OnShowToolTip();
        }

        public bool ToolTipVisible {
            get { return this.tip.Visible;  }
        }

        public void HideToolTip() {
            this.tip.Hide(); 
        }


        void OnShowToolTip(object sender, IntelliTipEventArgs args) {
            if (list != null) {
                int i = this.listBox.SelectedIndex;
                if (args.Type == TipRequestType.Hover) {
                    Point pt = args.Location;
                    for (int j = 0, n = this.listBox.Items.Count; j < n; j++) {
                        Rectangle r = this.listBox.GetItemRectangle(j);
                        if (r.Contains(pt)) {
                            i = j;
                            break;
                        }
                    }
                }
                if (i >= 0 && i < list.Count) {
                    string t = list.GetTooltip(i);
                    if (!string.IsNullOrEmpty(t)) {
                        Rectangle r = this.listBox.GetItemRectangle(i);
                        Point p = new Point(r.Right, r.Top);
                        p = this.listBox.PointToScreen(p);
                        Screen screen = Screen.FromPoint(p);
                        using (Graphics g = this.CreateGraphics()) {
                            SizeF s = g.MeasureString(t, SystemFonts.MenuFont);
                            if (p.X + s.Width > screen.Bounds.Right) {
                                p.X = screen.Bounds.Right - (int)s.Width;
                                if (p.X < 0) p.X = 0;
                                p.Y += this.listBox.ItemHeight;
                            }
                        }
                        args.Location = args.Focus.PointToClient(p);
                        args.ToolTip = t;
                    }
                }
            }
        }

        void button_Click(object sender, EventArgs e) {
            if (builder != null) {
                string result = this.editor.Text;
                if (builder.EditValue(this, type, result, out result)) {
                    this.editor.Text = result;
                }
            }
        }

        void listBox_DoubleClick(object sender, EventArgs e) {
            this.OnDoubleClick(e);
        }

        void listBox_KeyDown(object sender, KeyEventArgs e) {
            CurrentEvent.Event = e;
            this.OnKeyDown(e);
        }

        public IXmlBuilder Builder {
            get { return this.builder; }
            set { 
                this.builder = value;
                if (value != null) {
                    value.Site = this.Site;
                }
            }
        }        

        void editor_KeyDown(object sender, KeyEventArgs e) {
            CurrentEvent.Event = e;
            if (e.Handled) return; // some other listener already handled the event.
            int i = this.listBox.SelectedIndex;
            switch (e.KeyCode) {
                case Keys.Down:
                    i++;
                    break;
                case Keys.Up:
                    i--;
                    break;
                case Keys.Home:
                    i = 0;
                    break;
                case Keys.End:
                    i = this.listBox.Items.Count - 1;
                    break;
                case Keys.PageUp:
                    i -= this.Height / this.listBox.ItemHeight;
                    break;
                case Keys.PageDown:
                    i += this.Height / this.listBox.ItemHeight;
                    break;
                case Keys.Enter:
                    OnKeyDown(e);
                    break;                
            }
            if (i != this.listBox.SelectedIndex && this.listBox.Items.Count > 0) {
                if (i > this.listBox.Items.Count - 1) i = this.listBox.Items.Count - 1;
                if (i < 0) i = 0;
                this.listBox.SelectedIndex = i;
            }
        }

        const uint WS_POPUP =  0x80000000;
        const uint WS_EX_TOPMOST = 0x00000008;

        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.Style |= ForceCast(WS_POPUP);
                cp.ExStyle |= ForceCast(WS_EX_TOPMOST);
                return cp;
            }
        }

        static int ForceCast(uint i) {
            unchecked {
                return (int)i;
            }
        }
        
        void OnWindowMoved(object sender, EventArgs e) {
            if (this.Visible) {
                PositionPopup();
            }
        }

        void editor_TextChanged(object sender, EventArgs e) {
            // find best match & scroll it into view and select it
            string s = this.editor.Text;
            if (string.IsNullOrEmpty(s)) {
                this.listBox.SelectedItem = null;
            } else {
                int i = this.listBox.FindString(s);
                if (i >= 0) {
                    this.listBox.SelectedIndex = i;
                    return;
                } else {
                    // Find case-insensitive.
                    i = 0;
                    foreach (string value in this.listBox.Items) {
                        if (value.StartsWith(s, StringComparison.CurrentCultureIgnoreCase)) {
                            this.listBox.SelectedIndex = i;
                            return;
                        }
                        i++;
                    }
                }
                this.listBox.SelectedItem = null;
            }
        }

        public void BeginEdit(IIntellisenseList list, XmlSchemaType type) {
            this.type = type;
            this.listBox.Font = this.button.Font = this.editor.Font;
            this.listBox.ForeColor = this.editor.ForeColor;
            this.list = list;

            // populate the list and display it under (or above) the editor.
            this.listBox.Items.Clear();
            if (list != null) {
                for (int i = 0, n = list.Count; i<n; i++) {
                    string s = list.GetValue(i);
                    int j = this.listBox.Items.Add(s);
                    if (s == this.editor.Text) {
                        this.listBox.SelectedIndex = j;
                    }
                }
                
            }
            if (!parented) {
                SetParent(this.Handle, IntPtr.Zero); // popup on desktop.
                parented = true;
                Control p = this.editor;
                while (p.Parent != null) {
                    p = p.Parent;
                }
                p.Move += new EventHandler(OnWindowMoved);            
            }
            PositionPopup();
            this.Visible = true;
            this.editor.Focus();
        }

        public void EndEdit(bool cancel) {
            this.HideToolTip();
            if (!cancel && this.Visible && this.listBox.SelectedItem != null) {
                this.editor.Text = (string)this.listBox.SelectedItem;
                this.editor.Focus();
            }
            this.Visible = false;
        }

        void PositionPopup() {
            PerformLayout();
            Rectangle r = this.editor.Parent.RectangleToScreen(this.editor.Bounds);
            Screen s = Screen.FromRectangle(r);
            Point p = new Point(r.Left, r.Bottom);
            if (r.Bottom + this.Height > s.WorkingArea.Height) {
                // pop up instead of down!
                p = new Point(r.Left, r.Top - this.Height);
            }
            this.Location = p;
        }

        protected override void OnLayout(LayoutEventArgs levent) {
            Rectangle r = this.editor.Parent.RectangleToScreen(this.editor.Bounds);
            Screen s = Screen.FromRectangle(r);
            Size max = new Size(s.WorkingArea.Width / 3, (this.listBox.ItemHeight * 10) + 4);
            this.listBox.MaximumSize = max;
            Size size = new Size(0, 0);
            bool listVisible = (this.listBox.Items.Count > 0);
            if (!listVisible) {
                this.listBox.Size = size;
                this.listBox.Visible = false;
            } else {
                this.listBox.Visible = true;
                size = this.listBox.PreferredSize;
                if (size.Height > max.Height) {
                    size.Height = max.Height;
                }
                this.listBox.Size = size;
            }
            size = this.listBox.Size; // just in case listBox snapped to a different bounds.
            if (this.builder != null) {
                this.button.Text = this.builder.Caption;
                this.button.Visible = true;
                this.button.Size = new Size(10, 10);
                this.button.Size = this.button.PreferredSize;
                if (size.Width < this.button.Width) {
                    size.Width = this.button.Width;
                    if (listVisible) this.listBox.Width = size.Width;
                } 
                this.button.Width = size.Width;
                this.listBox.Location = new Point(0, this.button.Height);                
                size.Height += this.button.Height;
            } else {
                this.listBox.Location = new Point(0, 0);
                this.button.Visible = false;
            }
            this.Size = size;            
        }

        [DllImport("User32.dll", EntryPoint = "SetParent")]
        internal extern static IntPtr SetParent(IntPtr hwndChild, IntPtr hwndParent);
    }
}
