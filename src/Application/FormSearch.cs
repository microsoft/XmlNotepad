using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using SR = XmlNotepad.StringResources;

namespace XmlNotepad
{
    public partial class FormSearch : Form
    {
        private IFindTarget _target;
        private Settings _settings;
        private bool _findOnly;
        private TabNavigator _tnav;
        private FindFlags _lastFlags = FindFlags.Normal;
        private string _lastExpression;
        private MostRecentlyUsed _recentFindStrings;
        private RecentlyUsedComboBox _recentFindCombo;
        private MostRecentlyUsed _recentReplaceStrings;
        private RecentlyUsedComboBox _recentReplaceCombo;

        private SearchFilter _filter;

        public FormSearch()
        {
            this.SetStyle(ControlStyles.Selectable, true);
            this.KeyPreview = true;
            InitializeComponent();
            this.buttonFindNext.Click += new EventHandler(buttonFindNext_Click);
            this.buttonReplace.Click += new EventHandler(buttonReplace_Click);
            this.buttonReplaceAll.Click += new EventHandler(buttonReplaceAll_Click);
            this.comboBoxFind.KeyDown += new KeyEventHandler(comboBoxFind_KeyDown);

            this.comboBoxFilter.Items.AddRange(new object[] { SearchFilter.Everything, SearchFilter.Names, SearchFilter.Text, SearchFilter.Comments });
            this.comboBoxFilter.SelectedItem = this._filter;
            this.comboBoxFilter.SelectedValueChanged += new EventHandler(comboBoxFilter_SelectedValueChanged);
            this._tnav = new TabNavigator(this);

            _recentFindStrings = new MostRecentlyUsed();
            _recentFindCombo = new RecentlyUsedComboBox(_recentFindStrings, this.comboBoxFind);
            _recentFindCombo.SelectFirstItemByDefault = false;

            _recentReplaceStrings = new MostRecentlyUsed();
            _recentReplaceCombo = new RecentlyUsedComboBox(_recentReplaceStrings, this.comboBoxReplace);
            _recentReplaceCombo.SelectFirstItemByDefault = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.AutoSizeMode = AutoSizeMode.GrowOnly;
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            this.PerformLayout();
        }
        
        void comboBoxFilter_SelectedValueChanged(object sender, EventArgs e)
        {
            this._filter = (SearchFilter)this.comboBoxFilter.SelectedItem;
        }

        public FormSearch(FormSearch old, ISite site) : this()
        {
            if (old != null)
            {
                this.comboBoxFilter.SelectedItem = old.comboBoxFilter.SelectedItem;
            }
            this.Site = site;
        }

        public override ISite Site
        {
            get
            {
                return base.Site;
            }
            set
            {
                base.Site = value;
                OnSiteChanged();
            }
        }

        public SearchFilter Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        void buttonFindNext_Click(object sender, EventArgs e)
        {
            DoFind();
        }

        void buttonReplace_Click(object sender, EventArgs e)
        {
            DoReplace();
        }

        Control Window
        {
            get { return this.IsDisposed ? null : this; }
        }

        void OnNotFound()
        {
            MessageBox.Show(this.Window, SR.TextNotFoundPrompt, SR.FindErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.comboBoxFind.Focus();
        }

        void OnFindDone(bool didReplace)
        {
            MessageBox.Show(this.Window, SR.FindNextDonePrompt, didReplace ? SR.ReplaceCompleteCaption : SR.FindErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.comboBoxFind.Focus();
        }

        void OnError(Exception e, string caption)
        {
            MessageBox.Show(this.Window, e.Message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.comboBoxFind.Focus();
        }

        void DoFind()
        {
            try
            {
                FindNext(false);
            }
            catch (Exception ex)
            {
                OnError(ex, SR.FindErrorCaption);
            }
        }

        void DoReplace()
        {
            try
            {
                string replacement = this.comboBoxReplace.Text;
                bool didReplace = _target.ReplaceCurrent(replacement);
                FindNext(false, didReplace);
            }
            catch (Exception ex)
            {
                OnError(ex, SR.ReplaceErrorCaption);
            }
        }

        void buttonReplaceAll_Click(object sender, EventArgs e)
        {

            // todo: replace all is too slow on large files, it should do this in batch mode.
            // todo: would be nice if XPath could select elements or attribute for deletion.

            UndoManager mgr = (UndoManager)this.Site.GetService(typeof(UndoManager));
            mgr.OpenCompoundAction("Replace All");
            try
            {
                string replacement = this.comboBoxReplace.Text;
                _target.ReplaceCurrent(replacement);
                bool rc = FindNext(false);
                while (rc)
                {
                    Application.DoEvents();
                    rc = _target.ReplaceCurrent(replacement);
                    if (rc)
                    {
                        rc = FindNext(true);
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(ex, SR.ReplaceErrorCaption);
            }
            finally
            {
                mgr.CloseCompoundAction();
            }
        }

        bool FindNext(bool quiet, bool didReplace = false)
        {

            FindFlags flags = FindFlags.Normal;
            if (this.checkBoxRegex.Checked) flags |= FindFlags.Regex;
            else if (this.checkBoxXPath.Checked) flags |= FindFlags.XPath;
            if (this.checkBoxMatchCase.Checked) flags |= FindFlags.MatchCase;
            if (this.checkBoxWholeWord.Checked) flags |= FindFlags.WholeWord;
            if (this.radioButtonUp.Checked) flags |= FindFlags.Backwards;

            string expr = this.Expression;
            if (expr.Contains(" ") && this.checkBoxWholeWord.Checked)
            {
                throw new Exception(SR.FindWholeWordOnlyFindsWords);
            }
            this._recentFindStrings.AddItem(expr);

            _lastFlags = flags;
            _lastExpression = expr;

            FindResult rc = _target.FindNext(expr, flags, _filter);
            if (rc == FindResult.Found && !this.IsDisposed)
            {
                this.MoveFindDialog(_target.MatchRect);
            }
            if (!quiet)
            {
                if (didReplace && rc == FindResult.None)
                {
                    rc = FindResult.NoMore;
                }
                if (rc == FindResult.None)
                {
                    OnNotFound();
                }
                else if (rc == FindResult.NoMore)
                {
                    OnFindDone(didReplace);
                }
            }
            return rc == FindResult.Found;
        }

        public void FindAgain(bool reverse)
        {
            // The find dialog might have been disposed, so we can only find using previous 
            // find state information.
            if (string.IsNullOrEmpty(_lastExpression))
            {
                return;
            }
            try
            {
                if (reverse)
                {
                    _lastFlags |= FindFlags.Backwards;
                }
                else
                {
                    _lastFlags &= ~FindFlags.Backwards;
                }
                FindResult rc = _target.FindNext(_lastExpression, _lastFlags, _filter);
                if (rc == FindResult.Found && !this.IsDisposed)
                {
                    this.MoveFindDialog(_target.MatchRect);
                }
                if (rc == FindResult.None)
                {
                    OnNotFound();
                }
                else if (rc == FindResult.NoMore)
                {
                    OnFindDone(false);
                }
            }
            catch (Exception ex)
            {
                OnError(ex, SR.FindErrorCaption);
            }
        }

        void MoveFindDialog(Rectangle selection)
        {
            Rectangle r = this.Bounds;
            if (r.IntersectsWith(selection))
            {
                // find smallest adjustment (left,right,up,down) that still fits on screen.
                List<Adjustment> list = new List<Adjustment>();
                list.Add(new Adjustment(Direction.Up, this, selection));
                list.Add(new Adjustment(Direction.Down, this, selection));
                list.Add(new Adjustment(Direction.Left, this, selection));
                list.Add(new Adjustment(Direction.Right, this, selection));
                list.Sort();

                Adjustment smallest = list[0];
                smallest.AdjustDialog();
                return;
            }
        }

        enum Direction { Up, Down, Left, Right };
        class Adjustment : IComparable
        {
            Direction dir;
            Form dialog;
            Rectangle selection;
            Rectangle formBounds;

            public Adjustment(Direction dir, Form dialog, Rectangle selection)
            {
                this.dir = dir;
                this.dialog = dialog;
                this.selection = selection;
                this.formBounds = this.dialog.Bounds;
            }

            public int Delta
            {
                get
                {
                    int delta = 0;
                    Rectangle screen = Screen.FromControl(dialog).Bounds;
                    switch (this.dir)
                    {
                        case Direction.Up:
                            delta = formBounds.Bottom - selection.Top;
                            if (formBounds.Top - delta < screen.Top)
                            {
                                delta = Int32.MaxValue; // don't choose this one then.
                            }
                            break;
                        case Direction.Down:
                            delta = selection.Bottom - formBounds.Top;
                            if (formBounds.Bottom + delta > screen.Bottom)
                            {
                                delta = Int32.MaxValue; // don't choose this one then.
                            }
                            break;
                        case Direction.Left:
                            delta = formBounds.Right - selection.Left;
                            if (formBounds.Right - delta < screen.Left)
                            {
                                delta = Int32.MaxValue; // don't choose this one then.
                            }
                            break;
                        case Direction.Right:
                            delta = selection.Right - formBounds.Left;
                            if (formBounds.Right + delta > screen.Right)
                            {
                                delta = Int32.MaxValue; // don't choose this one then.
                            }
                            break;
                    }
                    return delta;
                }
            }

            public void AdjustDialog()
            {
                if (this.Delta == Int32.MaxValue)
                    return; // none of the choices were ideal

                switch (this.dir)
                {
                    case Direction.Up:
                        this.dialog.Top -= this.Delta;
                        break;
                    case Direction.Down:
                        this.dialog.Top += this.Delta;
                        break;
                    case Direction.Left:
                        this.dialog.Left -= this.Delta;
                        break;
                    case Direction.Right:
                        this.dialog.Left += this.Delta;
                        break;
                }
            }

            public int CompareTo(object obj)
            {
                Adjustment a = obj as Adjustment;
                if (a != null)
                {
                    return this.Delta - a.Delta;
                }
                return 0;
            }
        }

        void SetCheckBoxValue(CheckBox box, string name)
        {
            object value = this._settings[name];
            if (value != null)
            {
                box.Checked = (bool)value;
            }
        }

        public virtual void OnSiteChanged()
        {
            HelpProvider hp = this.Site.GetService(typeof(HelpProvider)) as HelpProvider;
            HelpService hs = this.Site.GetService(typeof(HelpService)) as HelpService;
            if (hp != null && hs.DynamicHelpEnabled)
            {
                hp.HelpNamespace = hs.FindHelp;
            }

            this.SuspendLayout();

            _settings = (Settings)this.Site.GetService(typeof(Settings));

            SetCheckBoxValue(this.checkBoxXPath, "SearchXPath");
            SetCheckBoxValue(this.checkBoxWholeWord, "SearchWholeWord");
            SetCheckBoxValue(this.checkBoxRegex, "SearchRegex");
            SetCheckBoxValue(this.checkBoxMatchCase, "SearchMatchCase");

            _recentFindStrings.SetValues(this._settings["RecentFindStrings"] as string[]);
            _recentReplaceStrings.SetValues(this._settings["RecentReplaceStrings"] as string[]);

            object o = this._settings["FindMode"];
            if (o != null)
            {
                this._findOnly = (bool)o;
                SetFindModeControls(!this._findOnly);
            }

            Size s = this.ClientSize;
            object size = this._settings["SearchSize"];
            if (size != null && (Size)size != Size.Empty)
            {
                Size cs = (Size)size;
                s = new Size(Math.Max(s.Width, cs.Width), Math.Max(s.Height, cs.Height));
                this.ClientSize = s;
            }

            object location = this._settings["SearchWindowLocation"];
            if (location != null && (Point)location != Point.Empty)
            {
                Control ctrl = this.Site as Control;
                if (ctrl != null)
                {
                    Rectangle ownerBounds = ctrl.TopLevelControl.Bounds;
                    this.Location = this.MoveOnscreen((Point)location, ownerBounds);
                    this.StartPosition = FormStartPosition.Manual;
                }
            }

            this.ResumeLayout();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            SaveSettings();
            base.OnClosing(e);
        }

        public void SaveSettings()
        { 
            this._settings["SearchWindowLocation"] = this.Location;
            // save replace mode size, since we will shink the size next time findOnly is set.
            this._settings["SearchSize"] = this.ClientSize;
            this._settings["FindMode"] = this._findOnly;
            this._settings["SearchXPath"] = this.checkBoxXPath.Checked;
            this._settings["SearchWholeWord"] = this.checkBoxWholeWord.Checked;
            this._settings["SearchRegex"] = this.checkBoxRegex.Checked;
            this._settings["SearchMatchCase"] = this.checkBoxMatchCase.Checked;

            this._settings["RecentFindStrings"] = _recentFindStrings.GetLatestValues();
            this._settings["RecentReplaceStrings"] = _recentReplaceStrings.GetLatestValues();

            HelpProvider hp = this.Site.GetService(typeof(HelpProvider)) as HelpProvider;
            HelpService hs = this.Site.GetService(typeof(HelpService)) as HelpService;
            if (hp != null && hs.DynamicHelpEnabled)
            {
                hp.HelpNamespace = hs.DefaultHelp;
            }
        }

        public IFindTarget Target
        {
            get { return this._target; }
            set { this._target = value; OnTargetChanged(); }
        }

        public bool ReplaceMode
        {
            get { return !_findOnly; }
            set
            {
                if (_findOnly != !value)
                {
                    _findOnly = !value;
                    SetFindModeControls(!_findOnly);
                }
            }
        }

        void SetFindModeControls(bool visible)
        {
            this.comboBoxReplace.Visible = visible;
            this.label2.Visible = visible;
            this.buttonReplace.Visible = visible;
            this.buttonReplaceAll.Visible = visible;
            this.Text = visible ? SR.FindWindowReplaceTitle : SR.FindWindowFindTitle;
        }

        public string Expression
        {
            get { return this.comboBoxFind.Text; }
            set { this.comboBoxFind.Text = value; }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            HandleKeyDown(e);
            if (!e.Handled)
            {
                base.OnKeyDown(e);
            }
        }

        void comboBoxFind_KeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyDown(e);
        }
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Tab || (keyData == (Keys.Tab | Keys.Shift)))
            {
                _tnav.HandleTab(new KeyEventArgs(keyData));
                return true;
            }
            else
            {
                return base.ProcessDialogKey(keyData);
            }
        }

        void HandleKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                if (this.buttonReplace.Focused)
                {
                    DoReplace();
                }
                else
                {
                    DoFind();
                }
                e.Handled = true;
            }
            else if ((e.Modifiers & Keys.Control) != 0)
            {
                if (e.KeyCode == Keys.H)
                {
                    ReplaceMode = true;
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.F)
                {
                    ReplaceMode = false;
                    e.Handled = true;
                }
            }
        }

        void OnTargetChanged()
        {
            this.dataTableNamespaces.Clear();
            if (_target != null && ShowNamespaces)
            {
                this.Expression = _target.Location;
                XmlNamespaceManager nsmgr = _target.Namespaces;
                foreach (string prefix in nsmgr)
                {
                    if (!string.IsNullOrEmpty(prefix) && prefix != "xmlns")
                    {
                        string uri = nsmgr.LookupNamespace(prefix);
                        this.dataTableNamespaces.Rows.Add(new object[] { prefix, uri });
                    }
                }
            }
            else
            {
                this.Expression = null;
            }
        }

        private void checkBoxXPath_CheckedChanged(object sender, EventArgs e)
        {
            bool namespaces = this.ShowNamespaces;

            if (namespaces && string.IsNullOrEmpty(this.comboBoxFind.Text))
            {
                OnTargetChanged();
            }
            dataGridViewNamespaces.Visible = namespaces;
            if (checkBoxRegex.Checked)
            {
                ManualToggle(checkBoxRegex, false);
            }
        }

        bool ShowNamespaces
        {
            get { return checkBoxXPath.Checked; }
        }

        bool checkBoxLatch;
        void ManualToggle(CheckBox box, bool value)
        {
            if (!checkBoxLatch)
            {
                checkBoxLatch = true;
                box.Checked = value;
                checkBoxLatch = false;
            }
        }

        private void checkBoxRegex_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxRegex.Checked)
            {
                ManualToggle(checkBoxXPath, false);
            }
        }
    }
}