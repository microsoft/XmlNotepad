//#define WHIDBEY_MENUS

using Microsoft.Xml;
using Microsoft.XmlDiffPatch;
using Newtonsoft.Json;
using Sgml;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using SR = XmlNotepad.StringResources;
using SystemTask = System.Threading.Tasks.Task;

namespace XmlNotepad
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public partial class FormMain : Form, ISite
    {
        private readonly UndoManager _undoManager;
        private Settings _settings = new Settings();
        private SettingsLoader _loader;
        private readonly DataFormats.Format _urlFormat;
        private readonly RecentFiles _recentFiles;
        private readonly RecentFiles _recentXsltFiles;
        private readonly RecentFilesMenu _recentFileMenu;
        private readonly RecentFilesComboBox _recentFilesCombo;
        private TaskList _taskList;
        private XsltControl _dynamicHelpViewer;
        private FormSearch _search;
        private IIntellisenseProvider _ip;
        private OpenFileDialog _od;
        private WebProxyService _proxyService;
        private bool _loading;
        private bool _firstActivate = true;
        private int _batch;
        private bool _includesExpanded;
        private bool _helpAvailableHint = true;
        private AppAnalytics _analytics;
        private Updater _updater;
        private SchemaCache _schemaCache;
        private readonly DelayedActions _delayedActions;
        private readonly HelpService _helpService = new HelpService();
        private XmlDiffWrapper _diffWrapper = new XmlDiffWrapper();

        private XmlCache _model;
        private SettingsLocation _loc;

        readonly private string _undoLabel;
        readonly private string _redoLabel;

        public FormMain(SettingsLocation location)
        {
            this._loc = location;
            bool testing = (location == SettingsLocation.Test || location == SettingsLocation.PortableTemplate);
            this.DoubleBuffered = true;
            this._settings = new Settings()
            {
                Comparer = SettingValueMatches,
                StartupPath = Application.StartupPath,
                ExecutablePath = Application.ExecutablePath,
                Resolver = new XmlProxyResolver(this)
            };
            this._loader = new SettingsLoader();

            this._delayedActions = _settings.DelayedActions = new DelayedActions((action) =>
            {
                DispatchAction(action);
            });
            SetDefaultSettings();

            this._settings.Changed += new SettingsEventHandler(OnSettingsChanged);

            this._model = (XmlCache)GetService(typeof(XmlCache));
            this._ip = (XmlIntellisenseProvider)GetService(typeof(XmlIntellisenseProvider));
            this._undoManager = new UndoManager(1000);
            this._undoManager.StateChanged += new EventHandler(undoManager_StateChanged);

            this.SuspendLayout();

            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            // Separated out so we can have virtual CreateTreeView without causing WinForms designer to barf.
            InitializeTreeView();

            this.xmlTreeView1.Dock = DockStyle.None;
            this.xmlTreeView1.Size = this.tabPageTreeView.ClientSize;
            this.xmlTreeView1.Dock = DockStyle.Fill;

            this._undoLabel = this.undoToolStripMenuItem.Text;
            this._redoLabel = this.redoToolStripMenuItem.Text;

            CreateTabControl();

            this.ResumeLayout();

            this.menuStrip1.SizeChanged += OnMenuStripSizeChanged;

            InitializeHelp(this.helpProvider1);

            this._dynamicHelpViewer.DefaultStylesheetResource = "XmlNotepad.DynamicHelp.xslt";
            this._dynamicHelpViewer.DisableOutputFile = true;
            _model.FileChanged += new EventHandler(OnFileChanged);
            _model.ModelChanged += new EventHandler<ModelChangedEventArgs>(OnModelChanged);

            _recentFiles = new RecentFiles();
            _recentFiles.RecentFileSelected += OnRecentFileSelected;
            _recentFileMenu = new RecentFilesMenu(_recentFiles, recentFilesToolStripMenuItem);
            _recentFilesCombo = new RecentFilesComboBox(_recentFiles, this.comboBoxLocation);
            _recentFilesCombo.SelectFirstItemByDefault = true;

            _recentXsltFiles = new RecentFiles();

            //this.resizer.Pane1 = this.xmlTreeView1;
            this.resizer.Pane1 = this.tabControlViews;
            this.resizer.Pane2 = this.tabControlLists;
            this.Controls.SetChildIndex(this.resizer, 0);
            this._taskList.Site = this;

            // now that we have a font, override the tabControlViews font setting.
            this.xmlTreeView1.Font = this.Font;

            // Event wiring
            this.xmlTreeView1.SetSite(this);
            this.xmlTreeView1.SelectionChanged += new EventHandler<NodeSelectedEventArgs>(treeView1_SelectionChanged);
            this.xmlTreeView1.ClipboardChanged += new EventHandler(treeView1_ClipboardChanged);
            this.xmlTreeView1.NodeChanged += new EventHandler<NodeChangeEventArgs>(treeView1_NodeChanged);
            this.xmlTreeView1.KeyDown += new KeyEventHandler(treeView1_KeyDown);
            this._taskList.GridKeyDown += new KeyEventHandler(taskList_KeyDown);

            this.toolStripButtonUndo.Enabled = false;
            this.toolStripButtonRedo.Enabled = false;

            this.statusBarToolStripMenuItem.Checked = true;

            this.duplicateToolStripMenuItem.Enabled = false;
            this.findToolStripMenuItem.Enabled = true;
            this.replaceToolStripMenuItem.Enabled = true;

            this.DragOver += new DragEventHandler(Form1_DragOver);
            this.xmlTreeView1.TreeView.DragOver += new DragEventHandler(Form1_DragOver);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
            this.xmlTreeView1.TreeView.DragDrop += new DragEventHandler(Form1_DragDrop);
            this.AllowDrop = true;

            this._urlFormat = DataFormats.GetFormat("UniformResourceLocatorW");

            ctxcutToolStripMenuItem.Click += new EventHandler(this.cutToolStripMenuItem_Click);
            ctxcutToolStripMenuItem.ImageIndex = this.cutToolStripMenuItem.ImageIndex;
            ctxMenuItemCopy.Click += new EventHandler(this.copyToolStripMenuItem_Click);
            ctxMenuItemCopy.ImageIndex = copyToolStripMenuItem.ImageIndex;
            ctxMenuItemPaste.Click += new EventHandler(this.pasteToolStripMenuItem_Click);
            ctxMenuItemPaste.ImageIndex = pasteToolStripMenuItem.ImageIndex;
            ctxMenuItemExpand.Click += new EventHandler(this.expandToolStripMenuItem_Click);
            ctxMenuItemCollapse.Click += new EventHandler(this.collapseToolStripMenuItem_Click);

            this.toolStripMenuItemUpdate.Visible = false;
            this.toolStripMenuItemUpdate.Click += new EventHandler(toolStripMenuItemUpdate_Click);

            this.ContextMenuStrip = this.contextMenu1;
            New();

            if (!testing)
            {
                _ = AsyncSetup();
            }
        }

        private void DispatchAction(Action action)
        {
            if (!this.Disposing)
            {
                ISynchronizeInvoke si = (ISynchronizeInvoke)this;
                if (si.InvokeRequired)
                {
                    // get on the right thread.
                    si.Invoke(action, null);
                    return;
                }
                else
                {
                    action();
                }
            }
        }

        private void OnXsltComplete(object sender, PerformanceInfo e)
        {
            ShowStatus(string.Format(SR.TransformedTimeStatus, e.XsltMilliseconds, e.BrowserMilliseconds, e.BrowserName));
        }

        private async System.Threading.Tasks.Task AsyncSetup()
        {
            // this is is on a background thread.
            await System.Threading.Tasks.Task.Delay(1);

            // install Xml notepad as an available editor for .xml files.
            FileAssociation.AddXmlProgids(Application.ExecutablePath);
        }

        protected virtual void SetDefaultSettings()
        {
            this._settings.SetDefaults();
            this._settings["SchemaCache"] = this._schemaCache = new SchemaCache(this);
        }

        public static bool SettingValueMatches(object existing, object newValue)
        {
            // just implement additional WinForms types not already implemented in the Settings class.
            if (existing is Font f1)
            {
                return newValue is Font f2 && f1 == f2;
            }
            else if (existing is Rectangle w1)
            {
                return newValue is Rectangle w2 && w1 == w2;
            }
            else if (existing is Point p1)
            {
                return newValue is Point p2 && p1 == p2;
            }
            else if (existing is Size z1)
            {
                return newValue is Size z2 && z1 == z2;
            }
            else
            {
                return false;
            }
        }

        public Settings Settings => _settings;

        public XmlCache Model
        {
            get { return _model; }
            set { _model = value; }
        }

        public PaneResizer Resizer
        {
            get { return resizer; }
            set { resizer = value; }
        }

        public NoBorderTabControl TabControlLists
        {
            get { return tabControlLists; }
            set { tabControlLists = value; }
        }

        public NoBorderTabControl TabControlViews
        {
            get { return this.tabControlViews; }
            set { tabControlViews = value; }
        }

        public XmlTreeView XmlTreeView
        {
            get { return xmlTreeView1; }
            set { xmlTreeView1 = value; }
        }

        void InitializeTreeView()
        {
            // Now remove the WinForm designer generated tree view and call the virtual CreateTreeView method
            // so a subclass of this form can plug in their own tree view.
            this.tabPageTreeView.Controls.Remove(this.xmlTreeView1);

            this.xmlTreeView1.Close();

            this.xmlTreeView1 = CreateTreeView();

            //
            // xmlTreeView1
            //
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            resources.ApplyResources(this.xmlTreeView1, "xmlTreeView1");
            this.xmlTreeView1.ResizerPosition = 200;
            this.xmlTreeView1.BackColor = System.Drawing.Color.White;
            this.xmlTreeView1.Location = new System.Drawing.Point(0, 52);
            this.xmlTreeView1.Name = "xmlTreeView1";
            this.xmlTreeView1.SelectedNode = null;
            this.xmlTreeView1.TabIndex = 1;

            this.tabPageTreeView.Controls.Add(this.xmlTreeView1);
            this.tabPageTreeView.Controls.SetChildIndex(this.xmlTreeView1, 0);

        }

        protected virtual void InitializeHelp(HelpProvider hp)
        {
            hp.SetHelpNavigator(this, HelpNavigator.TableOfContents);
            hp.Site = this;
            // in case subclass has already set HelpNamespace
            if (string.IsNullOrEmpty(hp.HelpNamespace) || this._helpService.DynamicHelpEnabled)
            {
                hp.HelpNamespace = this._helpService.DefaultHelp;
                this._helpService.DynamicHelpEnabled = true;
            }
        }

        void FocusNextPanel(bool reverse)
        {
            Control[] panels = new Control[] { this.xmlTreeView1.TreeView, this.xmlTreeView1.NodeTextView, this.tabControlLists.SelectedTab.Controls[0] };
            for (int i = 0; i < panels.Length; i++)
            {
                Control c = panels[i];
                if (c.ContainsFocus)
                {
                    int j = i + 1;
                    if (reverse)
                    {
                        j = i - 1;
                        if (j < 0) j = panels.Length - 1;
                    }
                    else if (j >= panels.Length)
                    {
                        j = 0;
                    }
                    SelectTreeView();
                    panels[j].Focus();
                    break;
                }
            }
        }

        void treeView1_KeyDown(object sender, KeyEventArgs e)
        {
            // Note if e.SuppressKeyPress is true, then this event is bubbling up from
            // the TextEditorOverlay - so we have to be careful not to interfere with
            // intellisense editing here unless we really want to.  Turns out the following
            // keystrokes all want to interrupt intellisense.
            if (e.Handled) return;
            switch (e.KeyCode)
            {
                case Keys.Space:
                    if ((e.Modifiers & Keys.Control) == Keys.Control)
                    {
                        this.xmlTreeView1.Commit();
                        Rectangle r = this.xmlTreeView1.TreeView.Bounds;
                        XmlTreeNode node = this.xmlTreeView1.SelectedNode;
                        if (node != null)
                        {
                            r = node.LabelBounds;
                            r.Offset(this.xmlTreeView1.TreeView.ScrollPosition);
                        }
                        r = this.xmlTreeView1.RectangleToScreen(r);
                        this.contextMenu1.Show(r.Left + (r.Width / 2), r.Top + (r.Height / 2));
                    }
                    break;
                case Keys.F3:
                    if (this._search != null)
                    {
                        this._search.FindAgain(e.Shift);
                        e.Handled = true;
                    }
                    break;
                case Keys.F6:
                    FocusNextPanel((e.Modifiers & Keys.Shift) != 0);
                    e.Handled = true;
                    break;
            }
        }

        void taskList_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F6:
                    FocusNextPanel((e.Modifiers & Keys.Shift) != 0);
                    break;
                case Keys.Enter:
                    _taskList.NavigateSelectedError();
                    break;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            LoadConfig();

            this._updater = new Updater(this._settings, this._delayedActions);
            this._updater.Title = this.Caption;
            this._updater.UpdateAvailable += new EventHandler<UpdateStatus>(OnUpdateAvailable);

            this.xmlTreeView1.OnLoaded();
            EnsureWindowBounds();
            base.OnLoad(e);
        }

        private void EnsureWindowBounds()
        {
            Rectangle r = (Rectangle)this._settings["WindowBounds"];
            if (r.Width == 0)
            {
                // trigger the settings change that makes the window visible the firs time.
                this._settings["WindowBounds"] = this.Bounds;
            }
        }

        void OnUpdateAvailable(object sender, UpdateStatus status)
        {
            if (this.Disposing)
            {
                return;
            }

            if (UpdateTooltips(status) != null)
            {
                ShowUpdateButton();
            }
        }

        void ShowUpdateButton()
        {
            this.toolStripMenuItemUpdate.Visible = true;
            this._delayedActions.StartDelayedAction(HideUpdateButtonAction, () =>
            {
                this.toolStripMenuItemUpdate.Visible = false;
            }, TimeSpan.FromSeconds(30));
        }

        string UpdateTooltips(UpdateStatus status)
        {
            if (this.Disposing)
            {
                return null;
            }

            string label = null;

            // happens from a background update check
            if (status.Error != null)
            {
                ShowStatus(status.Error);
            }
            else
            {
                bool updateAvailable = UpdateRequired(status.Latest);

                this.toolStripMenuItemUpdate.Tag = updateAvailable;
                if (updateAvailable)
                {
                    this.toolStripMenuItemUpdate.Text = label = SR.UpdateAvailableCaption;
                    this.toolStripMenuItemUpdate.ToolTipText = string.Format(SR.UpdateAvailableTooltip, this.caption, _updater.Version) + "\r\n" +
                        SR.ShowInstallPage;
                    this.menuStrip1.ShowItemToolTips = true;
                }
                else
                {
                    this.toolStripMenuItemUpdate.Text = label = SR.UpToDate;
                    this.toolStripMenuItemUpdate.ToolTipText = string.Format(SR.UpToDateTooltip, _updater.Version) + "\r\n" +
                        SR.ShowUpdateHistory;
                    this.menuStrip1.ShowItemToolTips = true;
                }
            }

            return label;
        }

        void toolStripMenuItemUpdate_Click(object sender, EventArgs e)
        {
            if (this.toolStripMenuItemUpdate.Tag is bool updateAvailable && updateAvailable)
            {
                WebBrowser.OpenUrl(this.Handle, this._updater.InstallerLocation);
            }
            else if (this._updater.InstallerHistory != null)
            {
                WebBrowser.OpenUrl(this.Handle, this._updater.InstallerHistory);
            }
            else if (this._updater.UpdateLocation != null)
            {
                WebBrowser.OpenUrl(this.Handle, this._updater.UpdateLocation.ToString());
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.xmlTreeView1.Commit();
            this.xsltViewer.OnClosed();
            if (this._model.Dirty)
            {
                SelectTreeView();
                DialogResult rc = MessageBox.Show(this, SR.SaveChangesPrompt, SR.SaveChangesCaption,
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
                if (rc == DialogResult.Yes)
                {
                    this.Save();
                }
                else if (rc == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }
            this._delayedActions.Close();
            SaveConfig();
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            this.xmlTreeView1.Close();
            this._diffWrapper.CleanupTempFiles();
            if (this._updater != null)
            {
                this._updater.Dispose();
            }
            this._delayedActions.Close();
            base.OnClosed(e);
        }

        private void OnMenuStripSizeChanged(object sender, EventArgs e)
        {
            Invalidate();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            this.PerformLayout();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            Size s = this.ClientSize;
            int w = s.Width;
            int h = s.Height;
            this.toolStrip1.Size = new Size(w, this.toolStrip1.Size.Height);
            int top = this.toolStrip1.Bottom;
            int sbHeight = 0;
            if (this.statusStrip1.Visible)
            {
                sbHeight = this.statusStrip1.Height;
                this.statusStrip1.Size = new Size(w, sbHeight);
            }
            this.tabControlViews.Location = new Point(0, top);
            this.comboBoxLocation.Location = new Point(this.comboBoxLocation.Location.X, this.menuStrip1.Height);
            this.tabControlViews.Size = new Size(w, h - top - sbHeight - this.tabControlLists.Height - this.resizer.Height);
            //this.tabControlViews.Padding = new Point(0, 0);
            //this.xmlTreeView1.Location = new Point(0, top);
            //this.xmlTreeView1.Size = new Size(w, h - top - sbHeight - this.tabControlViews.Height);
            this.resizer.Size = new Size(w, this.resizer.Height);
            this.resizer.Location = new Point(0, top + this.tabControlViews.Height);
            //this.taskList.Size = new Size(w, this.taskList.Height);
            //this.taskList.Location = new Point(0, top + this.xmlTreeView1.Height + this.resizer.Height);
            this.tabControlLists.Size = new Size(w, this.tabControlLists.Height);
            this.tabControlLists.Location = new Point(0, top + this.tabControlViews.Height + this.resizer.Height);
        }


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
                if (this._settings != null)
                {
                    this._settings.Dispose();
                    this._settings = null;
                }
                if (this._model != null)
                {
                    this._model.Dispose();
                    this._model = null;
                }
                IDisposable d = this._ip as IDisposable;
                if (d != null)
                {
                    d.Dispose();
                }
                this._ip = null;
            }
            base.Dispose(disposing);
        }

        protected virtual XmlTreeView CreateTreeView()
        {
            return new XmlTreeView();
        }

        protected virtual void CreateTabControl()
        {
            //
            // tabPageTaskList
            //
            this.tabPageTaskList.Controls.Add(this._taskList);
            this.tabPageTaskList.Location = new System.Drawing.Point(4, 24);
            this.tabPageTaskList.Name = "tabPageTaskList";
            this.tabPageTaskList.AccessibleName = "tabPageTaskList";
            this.tabPageTaskList.Padding = new Padding(0);
            this.tabPageTaskList.Size = new System.Drawing.Size(728, 68);
            this.tabPageTaskList.TabIndex = 1;
            this.tabPageTaskList.Text = SR.ErrorListTab;
            //
            // taskList
            //
            this._taskList.Dock = DockStyle.Fill;
            this._taskList.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this._taskList.Location = new System.Drawing.Point(3, 3);
            this._taskList.Margin = new Padding(0);
            this._taskList.Name = "taskList";
            this._taskList.Size = new System.Drawing.Size(722, 62);
            this._taskList.TabIndex = 2;
            this._taskList.Navigate += new XmlNotepad.NavigateEventHandler(this.taskList_Navigate);

            //
            // tabPageDynamicHelp
            //
            this._dynamicHelpViewer.Dock = DockStyle.Fill;
            this._dynamicHelpViewer.VisibleChanged += new EventHandler(dynamicHelpViewer_VisibleChanged);
            this.tabPageDynamicHelp.Controls.Add(_dynamicHelpViewer);
            this.tabPageDynamicHelp.Location = new System.Drawing.Point(4, 24);
            this.tabPageDynamicHelp.Name = "tabPageDynamicHelp";
            this.tabPageDynamicHelp.Padding = new Padding(0);
            this.tabPageDynamicHelp.Size = new System.Drawing.Size(728, 68);
            this.tabPageDynamicHelp.TabIndex = 2;
            this.tabPageDynamicHelp.Text = SR.DynamicHelpTab;
            //
            // tabControlLists
            //
            this.tabControlLists = new NoBorderTabControl();
            this.tabControlLists.Controls.AddRange(new Control[]{
                this.tabPageTaskList,
                this.tabPageDynamicHelp});
            this.tabControlLists.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.tabControlLists.Location = new System.Drawing.Point(0, 300);
            this.tabControlLists.Name = "tabControlLists";
            this.tabControlLists.SelectedIndex = 0;
            this.tabControlLists.Size = new System.Drawing.Size(736, 90);
            this.tabControlLists.TabIndex = 9;
            this.tabControlLists.Selected += new NoBorderTabControlEventHandler(TabControlLists_Selected);

            this.Controls.Add(this.tabControlLists);

        }

        void dynamicHelpViewer_VisibleChanged(object sender, EventArgs e)
        {
            this.DisplayHelp();
        }

        protected virtual void TabControlLists_Selected(object sender, NoBorderTabControlEventArgs e)
        {
            if (e.TabPage == this.tabPageDynamicHelp)
            {
                this._settings["DynamicHelpVisible"] = true;
                this.DisplayHelp();
            }
            else
            {
                this._settings["DynamicHelpVisible"] = false;
            }
        }

        const string HideUpdateButtonAction = "HideUpdateButton";

        private bool UpdateRequired(Version v)
        {
            if (v != null)
            {
                Version v2 = GetType().Assembly.GetName().Version;
                return v > v2;
            }
            return false;
        }

        private async void checkUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var saved = this.statusStrip1.BackColor;
            this.statusStrip1.BackColor = this.toolStripMenuItemUpdate.BackColor;
            this.toolStripMenuItemUpdate.Visible = false;
            this._delayedActions.CancelDelayedAction(HideUpdateButtonAction);
            ShowStatus(SR.CheckUpdatesStatus);
            try
            {
                var update = await this._updater.CheckNow();
                if (update.Error != null)
                {
                    ShowStatus(update.Error);
                }
                else
                {
                    var label = UpdateTooltips(update);
                    string line = label.Split('\r')[0];
                    ShowStatus(line);
                    ShowUpdateButton();
                    this._delayedActions.StartDelayedAction("AnimateUpdateButton", () => AnimateUpdateButton(null), TimeSpan.FromMilliseconds(0));
                }
            }
            catch
            {
            }
            this.statusStrip1.BackColor = saved;
        }

        private void AnimateUpdateButton(string target)
        {
            if (target == null)
            {
                target = this.toolStripMenuItemUpdate.Text;
                this.toolStripMenuItemUpdate.Text = "";
            }
            else if (target == this.toolStripMenuItemUpdate.Text)
            {
                return; // we are finished.
            }
            else
            {
                // reveal single char at a time to catch the eye.
                this.toolStripMenuItemUpdate.Text = target.Substring(0, this.toolStripMenuItemUpdate.Text.Length + 1);
            }
            this._delayedActions.StartDelayedAction("AnimateUpdateButton", () => AnimateUpdateButton(target), TimeSpan.FromMilliseconds(20));
        }

        protected virtual void TabControlViews_Selected(object sender, NoBorderTabControlEventArgs e)
        {
            if (e.TabPage == this.tabPageHtmlView)
            {
                this.helpProvider1.HelpNamespace = this._helpService.XsltHelp;
                this.CheckWebViewVersion();
                this.DisplayXsltResults();
            }
            else
            {
                this.helpProvider1.HelpNamespace = this._helpService.DefaultHelp;
                this.xsltViewer.OnClosed(); // good time to cleanup temp files.
            }
        }

        private void CheckWebViewVersion()
        {
            if (this._settings.GetBoolean("WebView2PromptInstall") &&
                !string.IsNullOrEmpty(this._settings.GetString("WebView2Exception")))
            {
                // prompt user once and only once so as not to be too annoying.
                this._settings["WebView2PromptInstall"] = false;
                var rc = MessageBox.Show(this.Owner, SR.WebView2InstallPrompt, SR.WebView2InstallTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (rc == DialogResult.Yes)
                {
                    WebView2PromptInstall();
                }
            }
        }

        private void WebView2PromptInstall()
        {
            // prompt and download the setup program so user can easily run it.
            var rc = MessageBox.Show(this.Owner, SR.WebView2InstallReady, SR.WebView2InstallTitle, MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (rc == DialogResult.OK)
            {
                WebBrowser.OpenUrl(this.Handle, "https://go.microsoft.com/fwlink/p/?LinkId=2124703");
            }
        }

        void EnableFileMenu()
        {
            bool hasFile = (_model.FileName != null);
            this.toolStripButtonSave.Enabled = this.saveToolStripMenuItem.Enabled = true;
            this.reloadToolStripMenuItem.Enabled = hasFile;
            this.saveAsToolStripMenuItem.Enabled = true;
        }

        public virtual void DisplayXsltResults()
        {
            if (_search != null)
            {
                _search.Close();
            }
            this.xsltViewer.DisplayXsltResults();
            this._analytics.RecordXsltView();
        }

        void SelectTreeView()
        {
            if (tabControlViews.SelectedIndex != 0)
            {
                tabControlViews.SelectedIndex = 0;
            }
            if (!xmlTreeView1.ContainsFocus)
            {
                xmlTreeView1.Focus();
            }
        }

        public virtual void New()
        {
            SelectTreeView();
            if (!SaveIfDirty(true))
                return;
            _model.Clear();
            _includesExpanded = false;
            EnableFileMenu();
            this._settings["FileName"] = new Uri("/", UriKind.RelativeOrAbsolute);
            UpdateMenuState();
        }

        protected virtual IIntellisenseProvider CreateIntellisenseProvider(XmlCache model, ISite site)
        {
            return new XmlIntellisenseProvider(this._model);
        }

        protected override object GetService(Type service)
        {
            if (service == typeof(UndoManager))
            {
                return this._undoManager;
            }
            else if (service == typeof(SchemaCache))
            {
                return this._schemaCache;
            }
            else if (service == typeof(HelpService))
            {
                return this._helpService;
            }
            else if (service == typeof(TreeView))
            {
                XmlTreeView view = (XmlTreeView)GetService(typeof(XmlTreeView));
                return view.TreeView;
            }
            else if (service == typeof(XmlTreeView))
            {
                if (this.xmlTreeView1 == null)
                {
                    this.xmlTreeView1 = this.CreateTreeView();
                }
                return this.xmlTreeView1;
            }
            else if (service == typeof(XmlCache))
            {
                if (null == this._model)
                {
                    this._model = new XmlCache((IServiceProvider)this, this._schemaCache, this._delayedActions);
                }
                return this._model;
            }
            else if (service == typeof(Settings))
            {
                return this._settings;
            }
            else if (service == typeof(IIntellisenseProvider))
            {
                if (this._ip == null) this._ip = CreateIntellisenseProvider(this._model, this);
                return this._ip;
            }
            else if (service == typeof(HelpProvider))
            {
                return this.helpProvider1;
            }
            else if (service == typeof(WebProxyService))
            {
                if (this._proxyService == null)
                    this._proxyService = new WebProxyService((IServiceProvider)this);
                return this._proxyService;
            }
            else if (service == typeof(DelayedActions))
            {
                return this._delayedActions;
            }
            return base.GetService(service);
        }

        public OpenFileDialog OpenFileDialog
        {
            get { return this._od; }
        }

        public virtual async SystemTask OpenDialog(string dir = null)
        {
            SelectTreeView();
            if (!SaveIfDirty(true))
                return;
            if (_od == null) _od = new OpenFileDialog();
            if (_model.FileName != null)
            {
                Uri uri = new Uri(_model.FileName);
                if (uri.Scheme == "file")
                {
                    _od.FileName = _model.FileName;
                }
            }
            if (!string.IsNullOrEmpty(dir))
            {
                _od.InitialDirectory = dir;
            }
            string filter = SR.OpenFileFilter;
            _od.Filter = filter;
            string[] parts = filter.Split('|');
            int index = -1;
            for (int i = 1, n = parts.Length; i < n; i += 2)
            {
                if (parts[i] == "*.*")
                {
                    index = (i / 2) + 1;
                    break;
                }
            }
            _od.FilterIndex = index;
            if (_od.ShowDialog(this) == DialogResult.OK)
            {
                await Open(_od.FileName);
            }
        }

        public virtual void ShowStatus(string msg)
        {
            this.toolStripStatusLabel1.Text = msg;
            this._delayedActions.StartDelayedAction("ClearStatus", ClearStatus, TimeSpan.FromSeconds(20));
        }

        private void ClearStatus()
        {
            this.toolStripStatusLabel1.Text = "";
        }

        public virtual async System.Threading.Tasks.Task Open(string filename, bool recentFile = false)
        {
            try
            {
                // Make sure you've called SaveIfDirty before calling this method.
                FileEntity entity = await FileEntity.Fetch(filename);
                switch (entity.MimeType)
                {
                    case "text/csv":
                        ImportCsv(entity);
                        break;
                    case "text/json":
                        await ImportJson(entity);
                        break;
                    case "text/html":
                        ImportHtml(entity);
                        break;
                    default:
                        InternalOpen(entity);
                        break;
                }
            }
            catch (Exception e)
            {
                Uri uri = null;
                bool prompt = true;
                try
                {
                    uri = new Uri(filename);
                    if (uri.Scheme == "file" && !File.Exists(uri.LocalPath))
                    {
                        MessageBox.Show(this, SR.FileRenamedOrDeleted, SR.LoadErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        prompt = false;
                    }
                }
                catch
                {
                    var msg = string.Format(SR.InvalidFileName, filename);
                    MessageBox.Show(this, msg, SR.LoadErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error); 
                    prompt = false;
                }

                if (recentFile && this._recentFiles.Contains(uri))
                {
                    if (prompt || MessageBox.Show(this, SR.RecentFileNotFoundMessage, SR.RecentFileNotFoundCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        this._recentFiles.RemoveRecentFile(uri);
                    }
                }
                else if (MessageBox.Show(this,
                    string.Format(SR.LoadErrorPrompt, filename, e.Message),
                    SR.LoadErrorCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                {
                    OpenNotepad(filename);
                }
            }
        }

        private void ImportHtml(FileEntity entity)
        {
            entity.Close();
            DateTime start = DateTime.Now;
            using (var reader = new SgmlReader())
            {
                reader.DocType = "HTML";
                reader.CaseFolding = CaseFolding.ToLower;
                reader.InputStream = new StreamReader(entity.Stream, entity.Encoding);
                reader.WhitespaceHandling = WhitespaceHandling.Significant;
                this._model.Load(reader, entity.Uri.OriginalString);
                FinishLoad(entity, start);
            }            
        }

        private async System.Threading.Tasks.Task ImportJson(FileEntity entity)
        {
            DateTime start = DateTime.Now;

            var json = await entity.ReadText();
            XmlDocument doc = JsonConvert.DeserializeXmlNode(json, "root");

            this._model.Load(new XmlNodeReader(doc), entity.LocalPath);

            FinishLoad(entity, start);
        }

        private void ImportCsv(FileEntity entity)
        {
            entity.Close();
            FormCsvImport importForm = new XmlNotepad.FormCsvImport();
            importForm.File = entity;
            if (importForm.ShowDialog() == DialogResult.OK)
            {
                // then import it for real...
                using (StreamReader reader = new StreamReader(entity.Stream, entity.Encoding))
                {
                    string xmlFile = Path.Combine(Path.GetDirectoryName(entity.LocalPath),
                        Path.GetFileNameWithoutExtension(entity.LocalPath) + ".xml");

                    XmlCsvReader csv = new XmlCsvReader(reader, entity.Uri, new NameTable());
                    csv.Delimiter = importForm.Deliminter;
                    csv.FirstRowHasColumnNames = importForm.FirstRowIsHeader;

                    DateTime start = DateTime.Now;
                    this._model.Load(csv, xmlFile);

                    FinishLoad(entity, start);
                }

                this._analytics.RecordCsvImport();
            }
        }

        private void FinishLoad(FileEntity entity, DateTime startLoadTime)
        {
            DateTime finish = DateTime.Now;
            TimeSpan diff = finish - startLoadTime;
            _includesExpanded = false;
            string s = diff.ToString();
            this._settings["FileName"] = entity.Uri.OriginalString;
            this.UpdateCaption();
            ShowStatus(string.Format(SR.LoadedTimeStatus, s));
            EnableFileMenu();
            this._recentFiles.AddRecentFile(entity.Uri);
            SelectTreeView();
        }

        private void InternalOpen(FileEntity entity)
        {
            entity.Close();
            DateTime start = DateTime.Now;
            this._model.Load(entity.Uri.OriginalString);
            FinishLoad(entity, start);
        }

        bool CheckXIncludes()
        {
            if (_includesExpanded)
            {
                if (MessageBox.Show(this, SR.SaveExpandedIncludesPrompt, SR.SaveExpandedIncludesCaption,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
                {
                    return false;
                }
                _includesExpanded = false;
            }
            return true;
        }

        public virtual bool SaveIfDirty(bool prompt)
        {
            if (_model.Dirty)
            {
                if (prompt)
                {
                    SelectTreeView();
                    DialogResult rc = MessageBox.Show(this, SR.SaveChangesPrompt,
                        SR.SaveChangesCaption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
                    if (rc == DialogResult.Cancel)
                    {
                        return false;
                    }
                    else if (rc == DialogResult.Yes)
                    {
                        return Save();
                    }
                }
                else
                {
                    return Save();
                }
            }
            return true;
        }

        public virtual bool Save()
        {
            this.xmlTreeView1.Commit();
            if (!CheckXIncludes()) return false;
            string fname = _model.FileName;
            if (fname == null)
            {
                SaveAs();
            }
            else
            {
                try
                {
                    this.xmlTreeView1.BeginSave();
                    if (CheckReadOnly(fname))
                    {
                        _model.Save();
                        ShowStatus(SR.SavedStatus);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(this, e.Message, SR.SaveErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    this.xmlTreeView1.EndSave();
                }
            }
            return true;
        }

        public bool CheckReadOnly(string fname)
        {
            if (_model.IsReadOnly(fname))
            {
                SelectTreeView();
                if (MessageBox.Show(this, string.Format(SR.ReadOnly, fname),
                    SR.ReadOnlyCaption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                {
                    _model.MakeReadWrite(fname);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public virtual void Save(string newName)
        {
            this.xmlTreeView1.Commit();
            this.xmlTreeView1.BeginSave();
            try
            {
                bool hasFile = (_model.FileName != null);
                if (!hasFile && string.IsNullOrEmpty(newName))
                {
                    SaveAs();
                }
                if (CheckReadOnly(newName))
                {
                    _model.Save(newName);
                    UpdateCaption();
                    ShowStatus(SR.SavedStatus);
                    this._settings["FileName"] = _model.Location;
                    EnableFileMenu();
                    this._recentFiles.AddRecentFile(_model.Location);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, SR.SaveErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.xmlTreeView1.EndSave();
            }
        }

        public virtual void SaveAs()
        {
            SelectTreeView();
            using (SaveFileDialog sd = new SaveFileDialog())
            {
                if (_model.IsFile) sd.FileName = _model.FileName;
                sd.Filter = SR.SaveAsFilter;
                if (sd.ShowDialog(this) == DialogResult.OK)
                {
                    string fname = sd.FileName;
                    if (CheckReadOnly(fname))
                    {
                        Save(fname);
                    }
                }
            }
        }

        string caption = null;

        public string Caption
        {
            get
            {
                if (string.IsNullOrEmpty(caption))
                    caption = SR.MainFormTitle;
                return caption;
            }
            set { caption = value; }
        }

        public virtual void UpdateCaption()
        {
            string fname = "";
            if (!string.IsNullOrEmpty(_model.FileName))
            {
                try
                {
                    fname = System.IO.Path.GetFileName(_model.FileName);
                }
                catch { }
            }

            string caption = string.IsNullOrEmpty(fname) ? this.Caption : fname;
            if (this._model.Dirty)
            {
                caption += "*";
            }
            this.Text = caption;
            sourceToolStripMenuItem.Enabled = this._model.FileName != null;
        }

        void OnFileChanged(object sender, EventArgs e)
        {
            if (!prompting) OnFileChanged();
        }

        bool prompting = false;
        private bool showingOptions;

        protected virtual async void OnFileChanged()
        {
            prompting = true;
            try
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
                SelectTreeView();
                if (!string.IsNullOrEmpty(this._model.NewName))
                {
                    if (MessageBox.Show(this, SR.FileRenamedDiskPrompt, SR.FileChagedOnDiskCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        string location = this.Model.NewName;
                        this._model.Clear();
                        await this.Open(location);

                    }
                }
                else if (MessageBox.Show(this, SR.FileChangedOnDiskPrompt, SR.FileChagedOnDiskCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    string location = this._model.Location.LocalPath;
                    this._model.Clear();
                    await this.Open(location);
                }
            }
            finally
            {
                prompting = false;
            }
        }

        private void undoManager_StateChanged(object sender, EventArgs e)
        {
            this.undoToolStripMenuItem.Enabled = toolStripButtonUndo.Enabled = this._undoManager.CanUndo;
            this.redoToolStripMenuItem.Enabled = toolStripButtonRedo.Enabled = this._undoManager.CanRedo;
            Command cmd = this._undoManager.Peek();
            this.undoToolStripMenuItem.Text = this._undoLabel + " " + (cmd == null ? "" : cmd.Name);
            cmd = this._undoManager.Current;
            this.redoToolStripMenuItem.Text = this._redoLabel + " " + (cmd == null ? "" : cmd.Name);
        }

        public virtual void LoadConfig()
        {
            try
            {
                this._loading = true;

                // allow user to have a local settings file (xcopy deployable).
                _loader.LoadSettings(_settings, this._loc);

                // convert old format to the new one
                object oldFont = this._settings["Font"];
                if (oldFont is string s && s != "deleted")
                {
                    // migrate old setting to the new setting
                    TypeConverter tc = TypeDescriptor.GetConverter(typeof(Font));
                    try
                    {
                        Font f = (Font)tc.ConvertFromString(s);
                        this._settings.SetFont(f);
                        this.Font = this.xmlTreeView1.Font = f;
                        this._settings.Remove("Font");
                    }
                    catch { }
                }
                else
                {
                    // convert serialized settings to a winforms Font object.
                    this._settings.Remove("Font");
                    this.Font = this.xmlTreeView1.Font = this._settings.GetFont();
                }

                var lightColors = _settings.AddDefaultColors("LightColors", ColorTheme.Light);
                if (lightColors.EditorBackground == Color.LightSteelBlue)
                {
                    // migrate to new default that looks better.
                    lightColors.EditorBackground = Color.FromArgb(255, 250, 205); // lemon chiffon.
                }
                _settings.AddDefaultColors("DarkColors", ColorTheme.Dark);

                string updates = (string)this._settings["UpdateLocation"];
                if (string.IsNullOrEmpty(updates) ||
                    updates.Contains("download.microsoft.com") ||
                    updates.Contains("lovettsoftware.com"))
                {
                    this._settings["UpdateLocation"] = XmlNotepad.Settings.DefaultUpdateLocation;
                }
            }
            finally
            {
                this._loading = false;
            }

            CheckAnalytics();
            InitializeXsltViewer();
            InitializeHelpViewer();
        }

        private void InitializeXsltViewer()
        {
            // now that we have loaded the settings, we can finish initializing the XsltControls.
            this.xsltViewer.SetSite(this);
            this.xsltViewer.SetRecentFiles(_recentXsltFiles);
            this.xsltViewer.Completed += OnXsltComplete;
            this.xsltViewer.GetXsltControl().WebBrowserException += OnWebBrowserException;
        }

        private void InitializeHelpViewer()
        {
            this._dynamicHelpViewer.SetSite(this);
            if (this._settings.GetBoolean("DynamicHelpVisible", false))
            {
                this.tabControlLists.SelectedTab = this.tabPageDynamicHelp;
            }
        }

        private void OnWebBrowserException(object sender, Exception e)
        {
            if (this.showingOptions && e is WebView2Exception)
            {
                var rc = MessageBox.Show(this, string.Format(SR.WebView2Error, e.Message), SR.WebView2ErrorTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                if (rc == DialogResult.OK)
                {
                    this.WebView2PromptInstall();
                }
            }
        }

        public bool AllowAnalytics { get; set; }

        private void CheckAnalytics()
        {
            if ((string)this.Settings["AnalyticsClientId"] == "" && AllowAnalytics && this.Settings.GetBoolean("AllowAnalytics", true))
            {
                // have not yet asked for permission!
                this.Settings["AnalyticsClientId"] = Guid.NewGuid().ToString();
                FormAnalytics fa = new FormAnalytics();
                var rc = fa.ShowDialog();
                if (rc == DialogResult.Yes)
                {
                    this.Settings["AllowAnalytics"] = true;
                }
                else
                {
                    this.Settings["AllowAnalytics"] = false;
                }
            }

            _analytics = new AppAnalytics((string)this.Settings["AnalyticsClientId"], (bool)this.Settings["AllowAnalytics"] && this.AllowAnalytics);
            _analytics.RecordAppLaunched();
        }

        public virtual void SaveConfig()
        {
            if (this._search != null)
            {
                this._search.SaveSettings();
            }
            this._settings.StopWatchingFileChanges();
            Rectangle r = (this.WindowState == FormWindowState.Normal) ? this.Bounds : this.RestoreBounds;
            this._settings["WindowBounds"] = r;
            this._settings["TaskListSize"] = this.tabControlLists.Height;
            this._settings["TreeViewSize"] = this.xmlTreeView1.ResizerPosition;
            this._settings["RecentFiles"] = this._recentFiles.ToArray();
            this._settings["RecentXsltFiles"] = this._recentXsltFiles.ToArray();
            var path = this._settings.FileName;
            this._settings.Save(path);
        }

        #region  ISite implementation
        IComponent ISite.Component
        {
            get { return this; }
        }

        public static Type ResourceType { get { return typeof(SR); } }

        string ISite.Name
        {
            get { return this.Name; }
            set { this.Name = value; }
        }

        IContainer ISite.Container
        {
            get { return this.Container; }
        }

        bool ISite.DesignMode
        {
            get { return this.DesignMode; }
        }
        object IServiceProvider.GetService(Type serviceType)
        {
            return this.GetService(serviceType);
        }
        #endregion

        void OnModelChanged(object sender, ModelChangedEventArgs e)
        {
            if (e.ModelChangeType == ModelChangeType.Reloaded)
            {
                this._undoManager.Clear();
                this._taskList.Clear();
            }
            if (e.ModelChangeType == ModelChangeType.BeginBatchUpdate)
            {
                _batch++;
            }
            else if (e.ModelChangeType == ModelChangeType.EndBatchUpdate)
            {
                _batch--;
            }
            if (_batch == 0) OnModelChanged();
        }

        protected virtual void OnModelChanged()
        {
            TaskHandler handler = new TaskHandler(this._taskList);
            handler.Start();
            this._model.ValidateModel(handler);
            handler.Finish();
            UpdateCaption();
        }

        bool _settinsReloadLock;

        protected virtual void OnSettingsChanged(object sender, string name)
        {
            switch (name)
            {
                case "File":
                    // load the new settiongs but don't move the window or anything if another instances of xmlnotepad.exe changed
                    // the settings.xml file.
                    if (!this._loading)
                    {
                        try
                        {
                            _settinsReloadLock = true;
                            this.LoadConfig();
                        }
                        finally
                        {
                            _settinsReloadLock = false;
                        }
                    }
                    break;
                case "SettingsLocation":
                    if (!this._loading)
                    {
                        try
                        {
                            _loader.MoveSettings(this._settings);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error moving settings: " + ex.Message + "\r\nSettings were not moved.", "Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    break;
                case "WindowBounds":
                    if (this._loading && !_settinsReloadLock)
                    {
                        Rectangle r = (Rectangle)this._settings["WindowBounds"];
                        if (!r.IsEmpty)
                        {
                            Screen s = Screen.FromRectangle(r);
                            if (s.Bounds.Contains(r))
                            {
                                this.Bounds = r;
                                this.StartPosition = FormStartPosition.Manual;
                            }
                        }
                    }
                    break;
                case "TreeViewSize":
                    int pos = (int)this._settings["TreeViewSize"];
                    if (pos != 0)
                    {
                        this.xmlTreeView1.ResizerPosition = pos;
                    }
                    break;
                case "TaskListSize":
                    int height = (int)this._settings["TaskListSize"];
                    if (height != 0)
                    {
                        this.tabControlLists.Height = height;
                    }
                    break;
                case "FontFamily":
                case "FontSize":
                case "FontStyle":
                case "FontWeight":
                    // this.Font = GetFont();
                    break;
                case "RecentFiles":
                    {
                        Uri[] files = (Uri[])this._settings["RecentFiles"];
                        if (files != null)
                        {
                            this._recentFiles.SetFiles(files);
                        }
                        break;
                    }
                case "RecentXsltFiles":
                    {
                        Uri[] files = (Uri[])this._settings["RecentXsltFiles"];
                        if (files != null)
                        {
                            this._recentXsltFiles.SetFiles(files);
                        }
                        break;
                    }
                case "DisableUpdateUI":
                    this.checkUpdatesToolStripMenuItem.Visible = !this._settings.GetBoolean("DisableUpdateUI", false);
                    break;

                case "AllowAnalytics":
                    if (this._analytics != null)
                    {
                        this._analytics.SetEnabled(this._settings.GetBoolean("AllowAnalytics", false));
                    }
                    break;
            }
        }

        public void SaveErrors(string filename)
        {
            this._taskList.Save(filename);
        }

        async void OnRecentFileSelected(object sender, MostRecentlyUsedEventArgs e)
        {
            if (!this.SaveIfDirty(true))
                return;
            string fileName = e.Selection;
            await Open(fileName, true);
        }

        private void treeView1_SelectionChanged(object sender, NodeSelectedEventArgs e)
        {
            UpdateMenuState();
            DisplayHelp();
            if (e != null)
            {
                DisplayLineInfo(e.Node);
            }
        }

        private void DisplayLineInfo(XmlTreeNode node)
        {
            if (node != null && node.Node != null && _model != null)
            {
                LineInfo info = _model.GetLineInfo(node.Node);
                if (info != null)
                {
                    ShowStatus(string.Format("Line {0}, Column {1}", info.LineNumber, info.LinePosition));
                }
            }
        }

        private void DisplayHelp()
        {
            // display documentation
            if (null == xmlTreeView1.SelectedNode)
            {
                this._dynamicHelpViewer.DisplayXsltResults(new XmlDocument(), null);
                return;
            }
            XmlDocument xmlDoc = xmlTreeView1.SelectedNode.GetDocumentation();
            if (this.tabControlLists.SelectedTab == this.tabPageDynamicHelp)
            {
                if (null == xmlDoc)
                {
                    xmlDoc = new XmlDocument();
                    if (_taskList.Count > 0)
                    {
                        xmlDoc.AppendChild(xmlDoc.CreateElement("errors"));
                    }
                    else
                    {
                        xmlDoc.AppendChild(xmlDoc.CreateElement("nothing"));
                    }
                }
                this._dynamicHelpViewer.DisplayXsltResults(xmlDoc, null);
            }
            else if (_helpAvailableHint && xmlDoc != null)
            {
                _helpAvailableHint = false;
                ShowStatus(SR.DynamicHelpAvailable);
            }
        }

        private void treeView1_NodeChanged(object sender, NodeChangeEventArgs e)
        {
            UpdateMenuState();
        }

        protected virtual void UpdateMenuState()
        {

            XmlTreeNode node = this.xmlTreeView1.SelectedNode as XmlTreeNode;
            XmlNode xnode = (node != null) ? node.Node : null;
            bool hasSelection = node != null;
            bool hasXmlNode = xnode != null;

            this.toolStripButtonCut.Enabled = this.cutToolStripMenuItem.Enabled = this.ctxcutToolStripMenuItem.Enabled = hasXmlNode;
            this.toolStripButtonDelete.Enabled = this.deleteToolStripMenuItem.Enabled = hasSelection;
            this.toolStripButtonCopy.Enabled = this.copyToolStripMenuItem.Enabled = this.ctxMenuItemCopy.Enabled = this.copyXPathToolStripMenuItem.Enabled = this.ctxMenuItemCopyXPath.Enabled = hasXmlNode;
            this.duplicateToolStripMenuItem.Enabled = hasXmlNode;

            this.changeToAttributeContextMenuItem.Enabled = this.changeToAttributeToolStripMenuItem1.Enabled = hasSelection;
            this.changeToCDATAContextMenuItem.Enabled = this.changeToCDATAToolStripMenuItem1.Enabled = hasSelection;
            this.changeToCommentContextMenuItem.Enabled = this.changeToCommentToolStripMenuItem1.Enabled = hasSelection;
            this.changeToElementContextMenuItem.Enabled = this.changeToElementToolStripMenuItem1.Enabled = hasSelection;
            this.changeToProcessingInstructionContextMenuItem.Enabled = this.changeToProcessingInstructionToolStripMenuItem.Enabled = hasSelection;
            this.changeToTextContextMenuItem.Enabled = this.changeToTextToolStripMenuItem1.Enabled = hasSelection;

            this.toolStripButtonNudgeUp.Enabled = upToolStripMenuItem.Enabled = this.xmlTreeView1.CanNudgeNode((XmlTreeNode)this.xmlTreeView1.SelectedNode, NudgeDirection.Up);
            this.toolStripButtonNudgeDown.Enabled = downToolStripMenuItem.Enabled = this.xmlTreeView1.CanNudgeNode((XmlTreeNode)this.xmlTreeView1.SelectedNode, NudgeDirection.Down);
            this.toolStripButtonNudgeLeft.Enabled = leftToolStripMenuItem.Enabled = this.xmlTreeView1.CanNudgeNode((XmlTreeNode)this.xmlTreeView1.SelectedNode, NudgeDirection.Left);
            this.toolStripButtonNudgeRight.Enabled = rightToolStripMenuItem.Enabled = this.xmlTreeView1.CanNudgeNode((XmlTreeNode)this.xmlTreeView1.SelectedNode, NudgeDirection.Right);

            this.repeatToolStripMenuItem.Enabled = hasSelection && xnode != null && this.xmlTreeView1.CanInsertNode(InsertPosition.After, xnode.NodeType);
            this.undoToolStripMenuItem.Enabled = toolStripButtonUndo.Enabled = this._undoManager.CanUndo;
            this.redoToolStripMenuItem.Enabled = toolStripButtonRedo.Enabled = this._undoManager.CanRedo;

            EnableNodeItems(XmlNodeType.Element, this.ctxElementBeforeToolStripMenuItem, this.elementBeforeToolStripMenuItem,
                this.ctxElementAfterToolStripMenuItem, this.elementAfterToolStripMenuItem,
                this.ctxElementChildToolStripMenuItem, this.elementChildToolStripMenuItem);
            EnableNodeItems(XmlNodeType.Attribute, this.ctxAttributeBeforeToolStripMenuItem, this.attributeBeforeToolStripMenuItem,
                this.ctxAttributeAfterToolStripMenuItem, this.attributeAfterToolStripMenuItem,
                this.ctxAttributeChildToolStripMenuItem, this.attributeChildToolStripMenuItem);
            EnableNodeItems(XmlNodeType.Text, this.ctxTextBeforeToolStripMenuItem, this.textBeforeToolStripMenuItem,
                this.ctxTextAfterToolStripMenuItem, this.textAfterToolStripMenuItem,
                this.ctxTextChildToolStripMenuItem, this.textChildToolStripMenuItem);
            EnableNodeItems(XmlNodeType.CDATA, this.ctxCdataBeforeToolStripMenuItem, this.cdataBeforeToolStripMenuItem,
                this.ctxCdataAfterToolStripMenuItem, this.cdataAfterToolStripMenuItem,
                this.ctxCdataChildToolStripMenuItem, this.cdataChildToolStripMenuItem);
            EnableNodeItems(XmlNodeType.Comment, this.ctxCommentBeforeToolStripMenuItem, this.commentBeforeToolStripMenuItem,
                this.ctxCommentAfterToolStripMenuItem, this.commentAfterToolStripMenuItem,
                this.ctxCommentChildToolStripMenuItem, this.commentChildToolStripMenuItem);
            EnableNodeItems(XmlNodeType.ProcessingInstruction, this.ctxPIBeforeToolStripMenuItem, this.PIBeforeToolStripMenuItem,
                this.ctxPIAfterToolStripMenuItem, this.PIAfterToolStripMenuItem,
                this.ctxPIChildToolStripMenuItem, this.PIChildToolStripMenuItem);
        }

        void EnableNodeItems(XmlNodeType nt, ToolStripMenuItem c1, ToolStripMenuItem m1, ToolStripMenuItem c2, ToolStripMenuItem m2, ToolStripMenuItem c3, ToolStripMenuItem m3)
        {
            c1.Enabled = m1.Enabled = this.xmlTreeView1.CanInsertNode(InsertPosition.Before, nt);
            c2.Enabled = m2.Enabled = this.xmlTreeView1.CanInsertNode(InsertPosition.After, nt);
            c3.Enabled = m3.Enabled = this.xmlTreeView1.CanInsertNode(InsertPosition.Child, nt);
        }

        protected virtual void OpenNotepad(string path)
        {
            if (this.SaveIfDirty(true))
            {
                if (path.StartsWith("http://") || path.StartsWith("https://") || path.StartsWith("ftp://"))
                {
                    path = System.IO.Path.GetTempFileName();
                    _model.SaveCopy(path);
                }
                OpenTextFile(path);
            }
        }

        protected void OpenTextFile(string path)
        {
            string notepad = (string)this.Settings["TextEditor"];
            if (string.IsNullOrEmpty(notepad) || !File.Exists(notepad))
            {
                string sysdir = Environment.SystemDirectory;
                notepad = Path.Combine(sysdir, "notepad.exe");
            }
            if (File.Exists(notepad))
            {
                ProcessStartInfo pi = new ProcessStartInfo(notepad, "\"" + path + "\"");
                Process.Start(pi);
            }
        }

        void treeView1_ClipboardChanged(object sender, EventArgs e)
        {
            CheckClipboard();
        }

        void CheckClipboard()
        {
            this.toolStripButtonPaste.Enabled = this.pasteToolStripMenuItem.Enabled = this.ctxMenuItemPaste.Enabled = TreeData.HasData;
        }


        protected override void OnActivated(EventArgs e)
        {
            CheckClipboard();
            if (_firstActivate)
            {
                this.comboBoxLocation.Focus();
                _firstActivate = false;
            }
            if (this.xmlTreeView1.TreeView.IsEditing)
            {
                this.xmlTreeView1.TreeView.Focus();
            }
            else if (this.xmlTreeView1.NodeTextView.IsEditing)
            {
                this.xmlTreeView1.NodeTextView.Focus();
            }
        }

        void taskList_Navigate(object sender, Task task)
        {
            XmlNode node = task.Data as XmlNode;
            if (node != null)
            {
                XmlTreeNode tn = this.xmlTreeView1.FindNode(node);
                if (tn != null)
                {
                    this.xmlTreeView1.SelectedNode = tn;
                    this.SelectTreeView();
                }
            }
        }

        private void Form1_DragOver(object sender, DragEventArgs e)
        {
            IDataObject data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop) ||
                data.GetDataPresent(this._urlFormat.Name) ||
                data.GetDataPresent("UniformResourceLocator"))
            {
                if ((e.AllowedEffect & DragDropEffects.Copy) == DragDropEffects.Copy)
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else if ((e.AllowedEffect & DragDropEffects.Link) == DragDropEffects.Link)
                {
                    e.Effect = DragDropEffects.Link;
                }
            }
            return;
        }

        bool dropping;

        private async void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (dropping)
            {
                return;
            }
            dropping = true;
            try
            {
                IDataObject data = e.Data;
                if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    Array a = data.GetData(DataFormats.FileDrop) as Array;
                    if (a != null)
                    {
                        if (a.Length > 0 && a.GetValue(0) is string)
                        {
                            string filename = (string)a.GetValue(0);
                            if (!this.SaveIfDirty(true))
                                return;
                            await this.Open(filename);
                        }
                    }
                }
                else if (data.GetDataPresent(this._urlFormat.Name))
                {
                    Stream stm = data.GetData(this._urlFormat.Name) as Stream;
                    if (stm != null)
                    {
                        // Note: for some reason sr.ReadToEnd doesn't work right.
                        StringBuilder sb = new StringBuilder();
                        using (StreamReader sr = new StreamReader(stm, Encoding.Unicode))
                        {
                            while (true)
                            {
                                int i = sr.Read();
                                if (i != 0)
                                {
                                    sb.Append(Convert.ToChar(i));
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        string url = sb.ToString();
                        if (!this.SaveIfDirty(true))
                            return;
                        await this.Open(url);
                    }
                }
                else if (data.GetDataPresent("UniformResourceLocator"))
                {
                    string uri = (string)data.GetData(DataFormats.UnicodeText);
                    if (!string.IsNullOrEmpty(uri))
                    {
                        await this.Open(uri);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading drag/drop data: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                dropping = false;
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.CancelEdit();
            New();
        }

        private async void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openToolStripMenuItem.Enabled = false;
            try
            {
                this.xmlTreeView1.CancelEdit();
                await OpenDialog();
            } 
            finally
            {
                openToolStripMenuItem.Enabled = true;
            }
        }

        private async void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            reloadToolStripMenuItem.Enabled = false;
            try
            {
                SelectTreeView();
                if (_model.Dirty)
                {
                    if (MessageBox.Show(this, SR.DiscardChanges, SR.DiscardChangesCaption,
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel)
                    {
                        return;
                    }
                }
                await Open(this._model.FileName);
            } 
            finally
            {
                reloadToolStripMenuItem.Enabled = true;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.Commit();
            Save();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.Commit();
            SaveAs();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.Commit();
            this.Close();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                this.xmlTreeView1.CancelEdit();
                this._undoManager.Undo();
                SelectTreeView();
                UpdateMenuState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, SR.UndoError, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.xmlTreeView1.Commit())
                    this._undoManager.Redo();
                SelectTreeView();
                UpdateMenuState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, SR.RedoError, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.Cut();
            UpdateMenuState();
            SelectTreeView();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.Commit();
            this.xmlTreeView1.Copy();
            SelectTreeView();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.Paste(InsertPosition.Child);
            UpdateMenuState();
            SelectTreeView();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteSelectedNode();
        }

        void DeleteSelectedNode()
        {
            this.xmlTreeView1.Commit();
            this.xmlTreeView1.Delete();
            UpdateMenuState();
            SelectTreeView();
        }

        private void repeatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.RepeatSelectedNode();
        }

        void RepeatSelectedNode()
        {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.Insert();
            UpdateMenuState();
            SelectTreeView();
        }

        private void duplicateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DuplicateSelectedNode();
        }

        void DuplicateSelectedNode()
        {
            try
            {
                if (this.xmlTreeView1.Commit())
                    this.xmlTreeView1.Duplicate();
                UpdateMenuState();
                SelectTreeView();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, SR.DuplicateErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void upToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.NudgeNode(this.xmlTreeView1.SelectedNode, NudgeDirection.Up);
            SelectTreeView();
        }

        private void downToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.NudgeNode(this.xmlTreeView1.SelectedNode, NudgeDirection.Down);
            SelectTreeView();
        }

        private void leftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.NudgeNode(this.xmlTreeView1.SelectedNode, NudgeDirection.Left);
            SelectTreeView();
        }

        private void rightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.NudgeNode(this.xmlTreeView1.SelectedNode, NudgeDirection.Right);
            SelectTreeView();
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Search(false);
        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Search(true);
        }

        void Search(bool replace)
        {
            if (this.tabControlViews.SelectedTab == this.tabPageHtmlView)
            {
                // TBD...
                return;
            }

            if (_search == null || !_search.Visible)
            {
                _search = new FormSearch(_search, (ISite)this);
                _search.Owner = this;
                this._analytics.RecordFormSearch();
            }
            else
            {
                _search.Activate();
            }
            _search.Target = new XmlTreeViewFindTarget(this.xmlTreeView1);
            _search.ReplaceMode = replace;

            if (!_search.Visible)
            {
                _search.Show(this); // modeless
            }
        }

        private void expandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            XmlTreeNode s = this.xmlTreeView1.SelectedNode;
            if (s != null)
            {
                s.ExpandAll();
            }
        }

        private void collapseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            XmlTreeNode s = this.xmlTreeView1.SelectedNode;
            if (s != null)
            {
                s.CollapseAll();
            }
        }

        private void expandAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            this.xmlTreeView1.ExpandAll();
        }

        private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            this.xmlTreeView1.CollapseAll();
        }

        private void statusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool visible = !statusBarToolStripMenuItem.Checked;
            statusBarToolStripMenuItem.Checked = visible;
            int h = this.ClientSize.Height - this.toolStrip1.Bottom - 2;
            if (visible)
            {
                h -= this.statusStrip1.Height;
            }
            this.tabControlViews.Height = h;
            this.statusStrip1.Visible = visible;
            this.PerformLayout();
        }

        private void sourceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.tabControlViews.SelectedTab == this.tabPageHtmlView)
            {
                // TBD
            }
            else
            {
                OpenNotepad(this._model.FileName);
            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string oldLocation = (string)_settings["UpdateLocation"];
            FormOptions options = new FormOptions();
            options.Owner = this;
            options.SelectedFont = this.Font;
            options.Site = this;
            this.showingOptions = true;
            if (options.ShowDialog(this) == DialogResult.OK)
            {
                this._updater.OnUserChange(oldLocation);
                // this one doesn't go through the _settings object.
                this.Font = this.xmlTreeView1.Font = options.SelectedFont;
            }
            this.showingOptions = false;
            _analytics.RecordFormOptions();
        }


        private void contentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this, this.helpProvider1.HelpNamespace, HelpNavigator.TableOfContents);
        }

        private void aboutXMLNotepadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormAbout frm = new FormAbout();
            frm.ShowDialog(this);
        }

        private void toolStripButtonNew_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.CancelEdit();
            this.New();
        }

        private async void toolStripButtonOpen_Click(object sender, EventArgs e)
        {
            toolStripButtonOpen.Enabled = false;
            try
            {
                this.xmlTreeView1.CancelEdit();
                await this.OpenDialog();
            }
            finally
            {
                toolStripButtonOpen.Enabled = true;
            }
        }

        private void toolStripButtonSave_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.Commit();
            this.Save();
        }

        private void toolStripButtonUndo_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this._undoManager.Undo();
            UpdateMenuState();
        }

        private void toolStripButtonRedo_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this._undoManager.Redo();
            UpdateMenuState();
        }

        private void toolStripButtonCut_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            this.xmlTreeView1.Cut();
            UpdateMenuState();
        }

        private void toolStripButtonCopy_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            this.xmlTreeView1.Copy();
        }

        private void copyXPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.CopyXPath();
        }

        private void ctxMenuItemCopyXPath_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.CopyXPath();
        }

        private void toolStripButtonPaste_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            this.xmlTreeView1.Paste(InsertPosition.Child);
            UpdateMenuState();
        }

        private void toolStripButtonDelete_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            this.xmlTreeView1.CancelEdit();
            this.xmlTreeView1.Delete();
            UpdateMenuState();
        }

        private void toolStripButtonNudgeUp_Click(object sender, EventArgs e)
        {
            this.upToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonNudgeDown_Click(object sender, EventArgs e)
        {
            this.downToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonNudgeLeft_Click(object sender, EventArgs e)
        {
            this.leftToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonNudgeRight_Click(object sender, EventArgs e)
        {
            this.rightToolStripMenuItem_Click(sender, e);
        }

        // Insert Menu Items.

        private void elementAfterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.Element);
        }

        private void elementBeforeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.Element);
        }

        private void elementChildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.Element);
        }

        private void attributeBeforeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.Attribute);
        }

        private void attributeAfterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.Attribute);
        }

        private void attributeChildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.Attribute);
        }

        private void textBeforeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.Text);
        }

        private void textAfterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.Text);
        }

        private void textChildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.Text);
        }

        private void commentBeforeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.Comment);
        }

        private void commentAfterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.Comment);
        }

        private void commentChildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.Comment);
        }

        private void cdataBeforeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.CDATA);
        }

        private void cdataAfterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.CDATA);
        }

        private void cdataChildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.CDATA);
        }

        private void PIBeforeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.ProcessingInstruction);
        }

        private void PIAfterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.ProcessingInstruction);
        }

        private void PIChildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.ProcessingInstruction);
        }

        void Launch(string exeFileName, string args)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = exeFileName;
            info.Arguments = "/offset " + args;
            Process p = new Process();
            p.StartInfo = info;
            if (!p.Start())
            {
                MessageBox.Show(this, string.Format(SR.ErrorCreatingProcessPrompt, exeFileName), SR.LaunchErrorPrompt, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void newWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SaveIfDirty(true);
            this.OpenNewWindow(this._model.FileName);
        }


        private void schemasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSchemas frm = new FormSchemas();
            frm.Owner = this;
            frm.Site = this;
            if (frm.ShowDialog(this) == DialogResult.OK)
            {
                OnModelChanged();
            }
            this._analytics.RecordFormSchemas();
        }

        private void nextErrorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this._taskList.NavigateNextError();
        }

        private void compareXMLFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this._model.FileName))
            {
                MessageBox.Show(this, SR.XmlDiffEmptyPrompt, SR.XmlDiffErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SelectTreeView();
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = SR.SaveAsFilter;
                bool retry = true;
                while (retry && ofd.ShowDialog(this) == DialogResult.OK)
                {
                    string secondFile = ofd.FileName;
                    if (secondFile.ToUpperInvariant() == this._model.FileName.ToUpperInvariant())
                    {
                        MessageBox.Show(this, SR.XmlDiffSameFilePrompt, SR.XmlDiffErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        retry = false;
                        try
                        {
                            DoCompare(secondFile);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this, ex.Message, SR.XmlDiffErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
        private void DoCompare(string otherXmlFile)
        {
            this.xmlTreeView1.Commit();
            this.SaveIfDirty(false);
            var options = this._settings.GetXmlDiffOptions();
            bool omitIdentical = this._settings.GetBoolean("XmlDiffHideIdentical");
            this._diffWrapper.DoCompare(this, this._model, otherXmlFile, options, omitIdentical);
        }

        string ApplicationPath
        {
            get
            {
                string path = Application.ExecutablePath;
                if (path.EndsWith("vstesthost.exe", StringComparison.CurrentCultureIgnoreCase))
                {
                    // must be running UnitTests
                    Uri baseUri = new Uri(this.GetType().Assembly.Location);
                    Uri resolved = new Uri(baseUri, @"..\..\..\Application\bin\debug\XmlNotepad.exe");
                    path = resolved.LocalPath;
                }
                return path;
            }
        }

        public virtual void OpenNewWindow(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                Uri uri = new Uri(path);
                if (uri.IsFile)
                {
                    path = uri.LocalPath;
                    if (!File.Exists(path))
                    {
                        DialogResult dr = MessageBox.Show(
                            String.Format(SR.CreateFile, path), SR.CreateNewFileCaption,
                            MessageBoxButtons.OKCancel);
                        if (dr.Equals(DialogResult.OK))
                        {
                            try
                            {
                                XmlDocument include = new XmlDocument();
                                include.InnerXml = "<root/>";
                                include.Save(path);
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show(this, e.Message, SR.SaveErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }
            Launch(this.ApplicationPath, "\"" + path + "\"");
        }

        private void GotoDefinition()
        {
            SelectTreeView();
            this.SaveIfDirty(true);

            XmlTreeNode node = xmlTreeView1.SelectedNode;
            if (node == null) return;

            string ipath = node.GetDefinition();

            if (!string.IsNullOrEmpty(ipath))
            {
                OpenNewWindow(ipath);
            }

        }

        private void gotoDefinitionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.GotoDefinition();
        }

        private void ctxGotoDefinitionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.GotoDefinition();
        }

        private void expandXIncludesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            this._model.ExpandIncludes();
            _includesExpanded = true;
        }

        private void exportErrorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAsErrors();
        }

        void SaveAsErrors()
        {
            using (SaveFileDialog sd = new SaveFileDialog())
            {
                sd.Filter = SR.SaveAsFilter;
                sd.Title = SR.SaveErrorsCaption;
                if (sd.ShowDialog(this) == DialogResult.OK)
                {
                    string fname = sd.FileName;
                    if (CheckReadOnly(fname))
                    {
                        SaveErrors(fname);
                    }
                }
            }
        }

        private void changeToAttributeToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Attribute);
        }

        private void changeToTextToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Text);
        }

        private void changeToCDATAToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.ChangeTo(XmlNodeType.CDATA);
        }

        private void changeToCommentToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Comment);
        }

        private void changeToProcessingInstructionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.ChangeTo(XmlNodeType.ProcessingInstruction);
        }

        private void changeToElementContextMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Element);
        }

        private void changeToAttributeContextMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Attribute);
        }

        private void changeToTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Text);
        }

        private void changeToCDATAContextMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.ChangeTo(XmlNodeType.CDATA);
        }

        private void changeToCommentContextMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Comment);
        }

        private void changeToProcessingInstructionContextMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.ChangeTo(XmlNodeType.ProcessingInstruction);
        }

        private void incrementalSearchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.StartIncrementalSearch();
        }

        private void renameToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.BeginEditNodeName();
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.BeginEditNodeName();
        }

        private void insertToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.RepeatSelectedNode();
        }

        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.DeleteSelectedNode();
        }

        private void duplicateToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.DuplicateSelectedNode();
        }

        private void fileAssociationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                FileAssociation.AddXmlProgids(Application.ExecutablePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error adding XmlProgids: " + ex.Message);
            }

            var message = StringResources.ConfigureDefaultApps;
            MessageBox.Show(this, message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void elementToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Element);
        }

        private void statsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this._model == null || string.IsNullOrEmpty(this._model.FileName))
            {
                return;
            }
            this.xmlTreeView1.Commit();
            this.SaveIfDirty(false);

            var path = GetWritableApplicationPath();

            string exePath = Path.Combine(path, "XmlStats.exe");

            if (!ExtractEmbeddedResourceAsFile("XmlNotepad.Resources.XmlStats.exe", exePath))
            {
                return;
            }

            string fileNameFile = Path.Combine(path, "names.txt");

            // need proper utf-8 encoding of the file name which can't be done with "cmd /k" command line.
            using (TextWriter cmdFile = new StreamWriter(fileNameFile, false, Encoding.UTF8))
            {
                cmdFile.WriteLine(this._model.FileName);
            }

            // now we can use "xmlstats -f names.txt" to generate the stats in a console window, this
            // way the user learns they can use xmlstats from the command line.
            string tempFile = Path.Combine(path, "stats.cmd");
            using (TextWriter cmdFile = new StreamWriter(tempFile, false, Encoding.Default))
            {
                cmdFile.WriteLine("@echo off");
                cmdFile.WriteLine("echo XML stats for: " + this._model.FileName);
                cmdFile.WriteLine("echo ---------------" + new string('-', this._model.FileName.Length));
                cmdFile.WriteLine("xmlstats -f \"{0}\"", fileNameFile);
                cmdFile.WriteLine("echo ---------------" + new string('-', this._model.FileName.Length));
                cmdFile.WriteLine("echo You can explore other options using : xmlstats -? " + this._model.FileName);
            }

            string cmd = Path.Combine(Environment.GetEnvironmentVariable("WINDIR"), "System32", "cmd.exe");

            ProcessStartInfo pi = new ProcessStartInfo(cmd, string.Format("/K \"{0}\"", tempFile));
            pi.UseShellExecute = true;
            pi.WorkingDirectory = path;
            Process.Start(pi);

            this._analytics.RecordStatistics();
        }

        private string GetWritableApplicationPath()
        {
            var path = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
            try
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(path, "names.txt"), "test");
                return path;
            } 
            catch (Exception)
            {
                // nope! 
            }

            // Ok, try the %USERPROFILE%\AppData\Local\Programs.
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            path = Path.Combine(local, "Programs");
            try
            {
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                path = System.IO.Path.Combine(path, "XmlNotepad");
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                System.IO.File.WriteAllText(System.IO.Path.Combine(path, "names.txt"), "test");
                return path;
            } 
            catch (Exception)
            {
                // nope?!
            }

            // fall back on last resort!
            var temp = Path.GetTempPath();
            var scratch = Path.Combine(temp, "XmlNotepad");
            Directory.CreateDirectory(scratch);
            return scratch;
        }

        private async void sampleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            sampleToolStripMenuItem.Enabled = false;
            try
            {
                string path = typeof(XmlNotepad.FormMain).Assembly.Location;
                string dir = System.IO.Path.GetDirectoryName(path);
                while (!string.IsNullOrEmpty(dir))
                {
                    string samples = System.IO.Path.Combine(dir, "Samples");
                    if (Directory.Exists(samples))
                    {
                        await OpenDialog(samples);
                        return;
                    }
                    if (System.IO.Path.GetFileName(dir) == "xmln..tion_ab3ea86545595e2b_0002.0008_ab0a31dd50b50bdb")
                    {
                        // don't venture outside our clickonce sandbox.
                        break;
                    }
                    if (System.IO.Path.GetFileName(dir) == "xmln..tion_d2e0d325f5b08396_0002.0009_e553c0f4e25a40dc")
                    {
                        // don't venture outside our msix sandbox.
                        break;
                    }
                    dir = System.IO.Path.GetDirectoryName(dir);
                }
            } 
            finally
            {
                sampleToolStripMenuItem.Enabled = true;
            }
            MessageBox.Show(this, SR.SamplesNotFound);
        }

        public bool ExtractEmbeddedResourceAsFile(string name, string path)
        {
            using (Stream s = this.GetType().Assembly.GetManifestResourceStream(name))
            {
                if (s == null)
                {
                    return false;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    s.CopyTo(fs);
                }
            }
            return true;
        }

        private async void openSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openSettingsToolStripMenuItem.Enabled = false;
            try
            {
                if (File.Exists(this._settings.FileName))
                {
                    await Open(this._settings.FileName);
                }
            } 
            finally
            {
                openSettingsToolStripMenuItem.Enabled = true;
            }
        }

        private void openXmlDiffStylesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.OpenTextFile(this._diffWrapper.GetOrCreateLocalStyles());
        }
        
        private void gCCollectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GC.Collect();
        }

        private void goToLineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this._model != null)
            {
                var form = new FormGotoLine();
                form.MaxLineNumber = this._model.GetLastLine();
                if (this.xmlTreeView1.SelectedNode != null)
                {
                    var xmlNode = this.xmlTreeView1.SelectedNode.Node;
                    var info = this._model.GetLineInfo(xmlNode);
                    if (info != null)
                    {
                        form.LineNumber = info.LineNumber;
                    }
                }
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    // Now find this line number in the line info cache.
                    var node = this._model.FindNodeAt(form.LineNumber, form.Column);
                    if (node != null)
                    {
                        XmlTreeNode tn = this.xmlTreeView1.FindNode(node);
                        if (tn != null)
                        {
                            this.xmlTreeView1.SelectedNode = tn;
                            this.SelectTreeView();
                        }
                    }
                }
            }
        }

        #region Debug Mouse Position for Testing
        TextBox xPos;
        TextBox yPos;
        TextBox status;

        internal void ShowMousePosition()
        {
            this.Controls.Clear();
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.BackColor = Color.Turquoise;
            panel.MouseMove += Panel_MouseMove;
            panel.Dock = DockStyle.Fill;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle() { Width = 100, SizeType = SizeType.Percent });
            panel.ColumnStyles.Add(new ColumnStyle() { Width = 100, SizeType = SizeType.Absolute });
            panel.ColumnStyles.Add(new ColumnStyle() { Width = 100, SizeType = SizeType.Absolute });

            xPos = new TextBox();
            xPos.Width = 100;
            xPos.AccessibleName = "XPosition";
            xPos.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            xPos.Margin = new Padding(10);
            panel.Controls.Add(xPos);

            yPos = new TextBox();
            yPos.Width = 100;
            yPos.AccessibleName = "YPosition";
            yPos.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            yPos.Margin = new Padding(10);
            panel.Controls.Add(yPos);

            status = new TextBox();
            status.Width = 100;
            status.AccessibleName = "Status";
            status.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            status.Margin = new Padding(10);
            panel.Controls.Add(status);

            panel.SetRow(xPos, 0);
            panel.SetRow(yPos, 0);
            panel.SetRow(status, 1);
            panel.SetColumn(xPos, 1);
            panel.SetColumn(yPos, 2);
            panel.SetColumn(status, 0);

            this.Controls.Add(panel);
        }

        private void Panel_MouseMove(object sender, MouseEventArgs e)
        {
            xPos.Text = e.X.ToString();
            yPos.Text = e.Y.ToString();
        }
        #endregion

    }

}