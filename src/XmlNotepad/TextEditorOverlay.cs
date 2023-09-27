using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Schema;
using SR = XmlNotepad.StringResources;

namespace XmlNotepad
{

    public class TextEditorEventArgs : EventArgs
    {
        private string _text;
        private bool _cancelled;

        public string Text
        {
            get { return _text; }
        }

        public bool Cancelled
        {
            get { return _cancelled; }
            set { this._cancelled = value; }
        }

        public TextEditorEventArgs(string text, bool cancelled)
        {
            this._text = text;
            this._cancelled = cancelled;
        }
    }

    public class TextEditorLayoutEventArgs : EventArgs
    {
        private string _text;
        private Rectangle _preferredBounds;
        private Rectangle _maxBounds;

        public string Text
        {
            get { return _text; }
        }

        public Rectangle PreferredBounds
        {
            get { return _preferredBounds; }
            set { _preferredBounds = value; }
        }

        public Rectangle MaxBounds
        {
            get { return _maxBounds; }
            set { _maxBounds = value; }
        }

        public TextEditorLayoutEventArgs(string text)
        {
            this._text = text;
        }
    }

    public enum EditMode { Name, Value };

    public class TextEditorOverlay : IDisposable
    {
        private TextBox _textEditor;
        private Control _parent;
        private bool _autoSize;
        private CompletionSet _cset;
        private IXmlEditor _editor;
        private Control _currentEditor;
        private XmlSchemaType _schemaType;
        private ISite _site;

        public event EventHandler<TextEditorEventArgs> CommitEdit;
        public event EventHandler<TextEditorLayoutEventArgs> LayoutEditor;

        public TextEditorOverlay(Control parent)
        {
            this._parent = parent;
            this._textEditor = new TextBox();
            string name = parent.Name + "Editor";
            this._textEditor.Name = name;
            this._textEditor.AccessibleName = name;
            this._textEditor.Visible = false;
            this._textEditor.BorderStyle = BorderStyle.None;
            this._textEditor.BackColor = Color.LightSteelBlue;
            this._textEditor.AutoSize = true;
            this._textEditor.Multiline = true; // this fixes layout problems in single line case also.
            this._textEditor.Margin = new Padding(1, 0, 0, 0);
            this._textEditor.HideSelection = false;
            parent.Controls.Add(this._textEditor);
            this._textEditor.KeyDown += new KeyEventHandler(editor_KeyDown);
            this._textEditor.LostFocus += new EventHandler(editor_LostFocus);
            this._textEditor.GotFocus += new EventHandler(editor_GotFocus);
            this._textEditor.TextChanged += new EventHandler(editor_TextChanged);
            this._currentEditor = this._textEditor;

            this._cset = new CompletionSet(this._textEditor);
            this._cset.KeyDown += new KeyEventHandler(editor_KeyDown);
            this._cset.DoubleClick += new EventHandler(cset_DoubleClick);
        }

        ~TextEditorOverlay()
        {
            Dispose(false);
        }

        public Color EditorBackgroundColor
        {
            get { return this._textEditor.BackColor; }
            set { this._textEditor.BackColor = value; }
        }

        public ISite Site
        {
            get { return _site; }
            set { _site = this._cset.Site = value; }
        }

        public int MaximumLineLength
        {
            get { return this._textEditor.MaxLength; }
            set { this._textEditor.MaxLength = value; }
        }

        public bool AutoSize
        {
            get { return this._autoSize; }
            set { this._autoSize = value; }
        }

        public bool MultiLine
        {
            get
            {
                return this._textEditor.Multiline;
            }
            set
            {
                this._textEditor.Multiline = value;
            }
        }

        public bool IsEditing { get { return this._currentEditor.Visible; } }

        public Rectangle Bounds
        {
            get { return _currentEditor.RectangleToScreen(_currentEditor.ClientRectangle); }
        }

        internal CompletionSet CompletionSet
        {
            get { return this._cset; }
        }

        public void PerformLayout()
        {
            if (this.LayoutEditor != null)
            {
                TextEditorLayoutEventArgs args = new TextEditorLayoutEventArgs(this._currentEditor.Text);
                LayoutEditor(this, args);
                SetEditorBounds(args);
            }
        }

        public void BeginEdit(string text, IIntellisenseProvider provider, EditMode mode, Color color, bool focus)
        {
            IXmlBuilder builder = null;

            IIntellisenseList list = null;
            if (focus)
            {
                switch (mode)
                {
                    case EditMode.Value:
                        builder = provider.Builder;
                        this._cset.Builder = builder;
                        this._editor = provider.Editor;
                        if (this._editor != null)
                        {
                            this._editor.Site = this._site;
                        }
                        list = provider.GetExpectedValues();
                        break;
                    case EditMode.Name:
                        list = provider.GetExpectedNames();
                        break;
                }
            }
            this._schemaType = provider.GetSchemaType();

            if (this._editor != null)
            {
                this._currentEditor = this._editor.Editor as Control;
                _parent.Controls.Add(this._currentEditor);
                this._editor.SchemaType = this._schemaType;
                this._currentEditor.KeyDown += new KeyEventHandler(editor_KeyDown);
                this._editor.XmlValue = text;
            }
            else
            {
                this._currentEditor = this._textEditor;
                this._currentEditor.Text = text;
            }

            this._currentEditor.ForeColor = color;
            PerformLayout();
            this._currentEditor.Visible = true;
            if (focus)
            {
                this._currentEditor.Focus();
                if (this._currentEditor == this._textEditor)
                {
                    this._textEditor.SelectAll();
                }

                // see if this node needs a dropdown.
                if (builder != null || (list != null && list.Count > 0))
                {
                    _cset.BeginEdit(list, this._schemaType);
                }
            }

        }

        public void SelectEnd()
        {
            if (this._currentEditor == this._textEditor && this._currentEditor.Visible)
            {
                this._textEditor.SelectionStart = this._textEditor.Text.Length;
            }
        }

        public void Select(int index, int length)
        {
            if (this._currentEditor == this._textEditor && this._currentEditor.Visible)
            {
                this._textEditor.SelectionStart = index;
                this._textEditor.SelectionLength = length;
            }
        }

        public int SelectionStart  => this._textEditor.SelectionStart;

        public int SelectionLength { get { return this._textEditor.SelectionLength; } }

        public string Text { 
            get { 
                return this._textEditor?.Text; 
            } 
        }

        public bool Replace(int index, int length, string replacement)
        {
            if (this._currentEditor == this._textEditor && this._currentEditor.Visible)
            {
                int end = index + length;
                string s = this._currentEditor.Text;
                string head = (index > 0) ? s.Substring(0, index) : "";
                string tail = (end < s.Length) ? s.Substring(end) : "";
                this._currentEditor.Text = head + replacement + tail;
                return true;
            }
            return false;
        }

        bool cancel;
        Timer at;
        public void StartEndEdit(bool cancel)
        {
            this.cancel = cancel;
            if (at == null)
            {
                at = new Timer();
                at.Interval = 10;
                at.Tick += new EventHandler(OnEndTick);
            }
            at.Start();
        }

        void OnEndTick(object sender, EventArgs e)
        {
            at.Stop();
            EndEdit(cancel);
        }

        bool ending;
        public bool EndEdit(bool cancel)
        {
            if (ending) return false; // don't let it be re-entrant!
            ending = true;
            bool success = true;
            _cset.EndEdit(cancel);
            try
            {
                if (this._currentEditor.Visible)
                {
                    bool hideEdit = true;
                    if (this.CommitEdit != null)
                    {
                        string value = (this._editor != null) ? this._editor.XmlValue : this._currentEditor.Text;
                        TextEditorEventArgs args = new TextEditorEventArgs(value, cancel);
                        CommitEdit(this, args);
                        if (args.Cancelled && !cancel)
                        {
                            success = false;
                            hideEdit = false;
                        }                    
                    }
                    if (hideEdit)
                    {
                        HideEdit();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, SR.EditErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally
            {
                ending = false;
            }
            return success;
        }

        const ushort WM_CHAR = 0x0102;
        Keys lastKey;

        private void editor_KeyDown(object sender, KeyEventArgs e)
        {
            CurrentEvent.Event = e;
            this.lastKey = e.KeyCode;
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (e.Modifiers == 0)
                    {
                        e.SuppressKeyPress = true;
                        e.Handled = true;
                        StartEndEdit(false); // must be async!
                    }
                    break;
                case Keys.Escape:
                    e.Handled = true;
                    if (this._cset.ToolTipVisible)
                    {
                        this._cset.HideToolTip();
                    }
                    else if (this._cset.Visible)
                    {
                        this._cset.EndEdit(true);
                        this._currentEditor.Focus();
                    }
                    else
                    {
                        StartEndEdit(true);
                    }
                    break;
                default:
                    IEditableView v = this._parent as IEditableView;
                    if (v != null)
                    {
                        bool old = e.SuppressKeyPress;
                        e.SuppressKeyPress = true;
                        try
                        {
                            e.Handled = false;
                            v.BubbleKeyDown(e);
                        }
                        finally
                        {
                            e.SuppressKeyPress = old;
                        }
                    }
                    break;
            }
        }

        void cset_DoubleClick(object sender, EventArgs e)
        {
            EndEdit(false);
        }

        private void editor_LostFocus(object sender, EventArgs e)
        {
            if (!_parent.ContainsFocus && !_cset.ContainsFocus)
            {
                EndEdit(false);
            }
        }

        private void editor_GotFocus(object sender, EventArgs e)
        {
            return;
        }

        private void editor_TextChanged(object sender, EventArgs e)
        {
            PerformLayout();
        }

        void AdjustBounds(string text, Graphics g, TextEditorLayoutEventArgs args)
        {
            Rectangle r = args.PreferredBounds;
            if (AutoSize)
            {
                if (string.IsNullOrEmpty(text))
                    text = "W"; // double the size if it was empty to begin with.
                text += "W"; // leave room to grow by one more char
            }
            if (text.EndsWith("\n")) text += "."; // cause MeasureString to include space for newlines.
            SizeF size = SizeF.Empty;
            int maxHeight = (this._parent.Height * 2) / 3;
            try
            {
                if (text.Length >= 10000)
                {
                    // MeasureString gets too slow after a certain size.
                    text = text.Substring(0, 10000);
                }
                size = g.MeasureString(text, this._parent.Font, this._parent.Width, StringFormat.GenericDefault);
            }
            catch (Exception)
            {
                // string might be too long to measure!
                size = new SizeF(r.Width, maxHeight);
            }
            // make sure we don't measure smaller than r.Height!
            int h = (int)Math.Max(r.Height, Math.Ceiling(size.Height));

            if (h > r.Height * 2)
            {
                // when multiline, add padding so that it aligns correctly.
                h += this._parent.Font.Height / 2;
            }

            if (h > maxHeight) { 
                h = maxHeight;
                // and we need scrollbars.
                this._textEditor.ScrollBars = ScrollBars.Vertical;
            }
            else
            {
                this._textEditor.ScrollBars = ScrollBars.None;
            }

            r.Height = h; // no more than 2/3rd of the window.
            if (AutoSize)
            {
                r.Width = Math.Max(r.Width, (int)size.Width + 2);
            }
            if (r.Right > args.MaxBounds.Right)
                r.Width = args.MaxBounds.Right - r.Left;
            args.PreferredBounds = r;
        }

        void SetEditorBounds(TextEditorLayoutEventArgs args)
        {
            string text = this._currentEditor.Text;
            Graphics g = this._parent.CreateGraphics();
            using (g)
            {
                AdjustBounds(text, g, args);
            }
            Rectangle r = args.PreferredBounds;
            if (r.Bottom > this._parent.Height)
            {
                // todo: scroll the view so we don't have to pop-up backwards, but this is tricky because
                // we may need to scroll more than the XmlTreeView scrollbar maximum in the case where the 
                // last node has a lot of text...
                r.Offset(new Point(0, this._parent.Height - r.Bottom));
            }
            int maxHeight = (this._parent.Height * 2) / 3;
            if (r.Height < maxHeight)
            {
                int h = this._currentEditor.PreferredSize.Height;
                if (r.Height < h)
                {
                    r.Y -= (h - r.Height) / 2;
                    if (r.Y < 0) r.Y = 0;
                    r.Height = h;
                }
            }
            Rectangle or = this._currentEditor.Bounds;
            if (or.Left != r.Left || or.Right != r.Right || or.Top != r.Top || or.Bottom != r.Bottom)
            {
                this._currentEditor.Bounds = r;
            }
        }

        void HideEdit()
        {
            if (this._currentEditor.Visible)
            {
                bool wasFocused = this._currentEditor.ContainsFocus || this._cset.ContainsFocus;
                this._currentEditor.Visible = false;
                this._cset.Visible = false;
                if (this._editor != null && this._currentEditor != this._textEditor)
                {
                    _parent.Controls.Remove(this._currentEditor);
                    this._currentEditor.KeyDown -= new KeyEventHandler(editor_KeyDown);
                    this._currentEditor.LostFocus -= new EventHandler(editor_LostFocus);
                    this._currentEditor = this._textEditor;
                    DisposeEditor();
                }
                if (wasFocused) this._parent.Focus();
            }
        }

        private void DisposeEditor()
        {
            if (this._editor is IDisposable d)
            {
                d.Dispose();
            }
            this._editor = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this._textEditor != null)
            {
                this._parent.Controls.Remove(this._textEditor);
                this._textEditor.Dispose();
                this._textEditor = null;
                this._textEditor = null;
            }
            if (this._cset != null)
            {
                this._cset.Builder = null;
                this._cset.Dispose();
                this._cset = null;
            }

            DisposeEditor();
        }
    }

    // This class is used to display a set of names and icons, track user edits
    // in associated text box and automatically select the item that best matches
    // what the user is doing.
    class CompletionSet : Control, IHostWindow
    {
        private bool _parented;
        private TextBox _editor;
        private ListBox _listBox;
        private IXmlBuilder _builder;
        private Button _button;
        private XmlSchemaType _type;
        private IIntellisenseList _list;
        private IntelliTip _tip;

        public CompletionSet(TextBox editor)
        {
            this.SetStyle(ControlStyles.Selectable, true);
            this._listBox = new ListBox();
            this._listBox.Name = "CompletionList";
            this._listBox.AccessibleName = "CompletionList";
            this._listBox.BorderStyle = BorderStyle.Fixed3D;
            this._listBox.KeyDown += new KeyEventHandler(listBox_KeyDown);
            this._listBox.DoubleClick += new EventHandler(listBox_DoubleClick);
            this._listBox.AutoSize = true;
            this._listBox.SelectedIndexChanged += new EventHandler(listBox_SelectedIndexChanged);

            this._editor = editor;
            this._editor.TextChanged += new EventHandler(editor_TextChanged);
            this._editor.KeyDown += new KeyEventHandler(editor_KeyDown);

            this._button = new Button();
            this._button.Name = "BuilderButton";
            this._button.AccessibleName = "BuilderButton";
            this._button.Visible = false;
            this._button.Click += new EventHandler(button_Click);
            this._button.AutoSize = true;
            this._button.KeyDown += new KeyEventHandler(editor_KeyDown);

            this.Visible = false;
            this._tip = new IntelliTip(editor);
            this._tip.AddWatch(this._listBox);
            this._tip.ShowToolTip += new IntelliTipEventHandler(OnShowToolTip);

            this.Controls.Add(this._listBox);
            this.Controls.Add(this._button);

            this.AccessibleName = "CompletionSet";
        }

        void listBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.HideToolTip();
            this._tip.OnShowToolTip();
        }

        public bool ToolTipVisible
        {
            get { return this._tip.Visible; }
        }

        public void HideToolTip()
        {
            this._tip.Hide();
        }

        void OnShowToolTip(object sender, IntelliTipEventArgs args)
        {
            if (_list != null)
            {
                int i = this._listBox.SelectedIndex;
                if (args.Type == TipRequestType.Hover)
                {
                    Point pt = args.Location;
                    for (int j = 0, n = this._listBox.Items.Count; j < n; j++)
                    {
                        Rectangle r = this._listBox.GetItemRectangle(j);
                        if (r.Contains(pt))
                        {
                            i = j;
                            break;
                        }
                    }
                }
                if (i >= 0 && i < _list.Count)
                {
                    string t = _list.GetTooltip(i);
                    if (!string.IsNullOrEmpty(t))
                    {
                        Rectangle r = this._listBox.GetItemRectangle(i);
                        Point p = new Point(r.Right, r.Top);
                        p = this._listBox.PointToScreen(p);
                        Screen screen = Screen.FromPoint(p);
                        using (Graphics g = this.CreateGraphics())
                        {
                            SizeF s = g.MeasureString(t, SystemFonts.MenuFont);
                            if (p.X + s.Width > screen.Bounds.Right)
                            {
                                p.X = screen.Bounds.Right - (int)s.Width;
                                if (p.X < 0) p.X = 0;
                                p.Y += this._listBox.ItemHeight;
                            }
                        }
                        args.Location = args.Focus.PointToClient(p);
                        args.ToolTip = t;
                    }
                }
            }
        }

        void button_Click(object sender, EventArgs e)
        {
            if (_builder != null)
            {
                string result = this._editor.Text;
                if (_builder.EditValue(this, _type, result, out result))
                {
                    this._editor.Text = result;
                }
            }
        }

        void listBox_DoubleClick(object sender, EventArgs e)
        {
            this.OnDoubleClick(e);
        }

        void listBox_KeyDown(object sender, KeyEventArgs e)
        {
            CurrentEvent.Event = e;
            this.OnKeyDown(e);
        }

        public IXmlBuilder Builder
        {
            get { return this._builder; }
            set
            {
                this._builder = value;
                if (value != null)
                {
                    value.Site = this.Site;
                }
            }
        }

        void editor_KeyDown(object sender, KeyEventArgs e)
        {
            CurrentEvent.Event = e;
            if (e.Handled) return; // some other listener already handled the event.
            int i = this._listBox.SelectedIndex;
            switch (e.KeyCode)
            {
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
                    i = this._listBox.Items.Count - 1;
                    break;
                case Keys.PageUp:
                    i -= this.Height / this._listBox.ItemHeight;
                    break;
                case Keys.PageDown:
                    i += this.Height / this._listBox.ItemHeight;
                    break;
                case Keys.Enter:
                    OnKeyDown(e);
                    break;
            }
            if (i != this._listBox.SelectedIndex && this._listBox.Items.Count > 0)
            {
                if (i > this._listBox.Items.Count - 1) i = this._listBox.Items.Count - 1;
                if (i < 0) i = 0;
                this._listBox.SelectedIndex = i;
            }
        }

        const uint WS_POPUP = 0x80000000;
        const uint WS_EX_TOPMOST = 0x00000008;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= ForceCast(WS_POPUP);
                cp.ExStyle |= ForceCast(WS_EX_TOPMOST);
                return cp;
            }
        }

        static int ForceCast(uint i)
        {
            unchecked
            {
                return (int)i;
            }
        }

        void OnWindowMoved(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                PositionPopup();
            }
        }

        void editor_TextChanged(object sender, EventArgs e)
        {
            // find best match & scroll it into view and select it
            string s = this._editor.Text;
            if (string.IsNullOrEmpty(s))
            {
                this._listBox.SelectedItem = null;
            }
            else
            {
                int i = this._listBox.FindString(s);
                if (i >= 0)
                {
                    this._listBox.SelectedIndex = i;
                    return;
                }
                else
                {
                    // Find case-insensitive.
                    i = 0;
                    foreach (string value in this._listBox.Items)
                    {
                        if (value.StartsWith(s, StringComparison.CurrentCultureIgnoreCase))
                        {
                            this._listBox.SelectedIndex = i;
                            return;
                        }
                        i++;
                    }
                }
                this._listBox.SelectedItem = null;
            }
        }

        public void BeginEdit(IIntellisenseList list, XmlSchemaType type)
        {
            this._type = type;
            this._listBox.Font = this._button.Font = this._editor.Font;
            this._listBox.ForeColor = this._editor.ForeColor;
            this._list = list;

            // populate the list and display it under (or above) the editor.
            this._listBox.Items.Clear();
            if (list != null)
            {
                for (int i = 0, n = list.Count; i < n; i++)
                {
                    string s = list.GetValue(i);
                    int j = this._listBox.Items.Add(s);
                    if (s == this._editor.Text)
                    {
                        this._listBox.SelectedIndex = j;
                    }
                }

            }
            if (!_parented)
            {
                SetParent(this.Handle, IntPtr.Zero); // popup on desktop.
                _parented = true;
                Control p = this._editor;
                while (p.Parent != null)
                {
                    p = p.Parent;
                }
                p.Move += new EventHandler(OnWindowMoved);
            }
            PositionPopup();
            this.Visible = true;
            this._editor.Focus();
        }

        public void EndEdit(bool cancel)
        {
            this.HideToolTip();
            if (!cancel && this.Visible && this._listBox.SelectedItem != null)
            {
                this._editor.Text = (string)this._listBox.SelectedItem;
                this._editor.Focus();
            }
            this.Visible = false;
        }

        void PositionPopup()
        {
            PerformLayout();
            Rectangle r = this._editor.Parent.RectangleToScreen(this._editor.Bounds);
            Screen s = Screen.FromRectangle(r);
            Point p = new Point(r.Left, r.Bottom);
            if (r.Bottom + this.Height > s.WorkingArea.Height)
            {
                // pop up instead of down!
                p = new Point(r.Left, r.Top - this.Height);
            }
            this.Location = p;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            Rectangle r = this._editor.Parent.RectangleToScreen(this._editor.Bounds);
            Screen s = Screen.FromRectangle(r);
            Size max = new Size(s.WorkingArea.Width / 3, (this._listBox.ItemHeight * 10) + 4);
            this._listBox.MaximumSize = max;
            Size size = new Size(0, 0);
            bool listVisible = (this._listBox.Items.Count > 0);
            if (!listVisible)
            {
                this._listBox.Size = size;
                this._listBox.Visible = false;
            }
            else
            {
                this._listBox.Visible = true;
                size = this._listBox.PreferredSize;
                if (size.Height > max.Height)
                {
                    size.Height = max.Height;
                }
                this._listBox.Size = size;
            }
            size = this._listBox.Size; // just in case listBox snapped to a different bounds.
            if (this._builder != null)
            {
                this._button.Text = this._builder.Caption;
                this._button.Visible = true;
                this._button.Size = new Size(10, 10);
                this._button.Size = this._button.PreferredSize;
                if (size.Width < this._button.Width)
                {
                    size.Width = this._button.Width;
                    if (listVisible) this._listBox.Width = size.Width;
                }
                this._button.Width = size.Width;
                this._listBox.Location = new Point(0, this._button.Height);
                size.Height += this._button.Height;
            }
            else
            {
                this._listBox.Location = new Point(0, 0);
                this._button.Visible = false;
            }
            this.Size = size;
        }

        [DllImport("User32.dll", EntryPoint = "SetParent")]
        internal extern static IntPtr SetParent(IntPtr hwndChild, IntPtr hwndParent);
    }
}
