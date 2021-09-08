//#define WHIDBEY_MENUS

using Microsoft.Xml;
using Microsoft.XmlDiffPatch;
using Sgml;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SR = XmlNotepad.StringResources;

namespace XmlNotepad
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public partial class FormMain : Form, ISite
    {
        readonly UndoManager undoManager;
        Settings settings;
        readonly string[] args;
        readonly DataFormats.Format urlFormat;
        readonly RecentFilesMenu recentFiles;
        TaskList taskList;
        XsltControl dynamicHelpViewer;
        FormSearch search;
        IIntellisenseProvider ip;
        OpenFileDialog od;
        WebProxyService proxyService;
        bool loading;
        bool firstActivate = true;
        int batch;
        bool includesExpanded;
        bool helpAvailableHint = true;
        Analytics analytics;
        Updater updater;
        readonly DelayedActions delayedActions;
        readonly System.CodeDom.Compiler.TempFileCollection tempFiles = new System.CodeDom.Compiler.TempFileCollection();

        private XmlCache model;

        readonly private string undoLabel; readonly private string redoLabel;

        public FormMain()
        {
            this.DoubleBuffered = true;
            this.settings = new Settings
            {
                StartupPath = Application.StartupPath,
                ExecutablePath = Application.ExecutablePath,
                Resolver = new XmlProxyResolver(this)
            };

            this.delayedActions = settings.DelayedActions = new DelayedActions((action) =>
            {
                DispatchAction(action);
            });

            SetDefaultSettings();
            this.settings.Changed += new SettingsEventHandler(OnSettingsChanged);

            this.model = (XmlCache)GetService(typeof(XmlCache));
            this.ip = (XmlIntellisenseProvider)GetService(typeof(XmlIntellisenseProvider));
            this.undoManager = new UndoManager(1000);
            this.undoManager.StateChanged += new EventHandler(undoManager_StateChanged);

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

            this.undoLabel = this.undoToolStripMenuItem.Text;
            this.redoLabel = this.redoToolStripMenuItem.Text;

            CreateTabControl();

            this.ResumeLayout();

            this.menuStrip1.SizeChanged += OnMenuStripSizeChanged;

            InitializeHelp(this.helpProvider1);

            this.dynamicHelpViewer.DefaultStylesheetResource = "XmlNotepad.DynamicHelp.xslt";
            this.dynamicHelpViewer.DisableOutputFile = true;
            model.FileChanged += new EventHandler(OnFileChanged);
            model.ModelChanged += new EventHandler<ModelChangedEventArgs>(OnModelChanged);

            recentFiles = new RecentFilesMenu(recentFilesToolStripMenuItem, this.comboBoxLocation);
            this.recentFiles.RecentFileSelected += new RecentFileHandler(OnRecentFileSelected);

            //this.resizer.Pane1 = this.xmlTreeView1;
            this.resizer.Pane1 = this.tabControlViews;
            this.resizer.Pane2 = this.tabControlLists;
            this.Controls.SetChildIndex(this.resizer, 0);
            this.taskList.Site = this;

            // now that we have a font, override the tabControlViews font setting.
            this.xmlTreeView1.Font = this.Font;

            // Event wiring
            this.xmlTreeView1.SetSite(this);
            this.xmlTreeView1.SelectionChanged += new EventHandler(treeView1_SelectionChanged);
            this.xmlTreeView1.ClipboardChanged += new EventHandler(treeView1_ClipboardChanged);
            this.xmlTreeView1.NodeChanged += new EventHandler<NodeChangeEventArgs>(treeView1_NodeChanged);
            this.xmlTreeView1.KeyDown += new KeyEventHandler(treeView1_KeyDown);
            this.taskList.GridKeyDown += new KeyEventHandler(taskList_KeyDown);

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

            this.urlFormat = DataFormats.GetFormat("UniformResourceLocatorW");

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

            this.settings["SchemaCache"] = this.model.SchemaCache;

            _ = AsyncSetup();
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

            await CheckNetwork();
        }

        private async System.Threading.Tasks.Task CheckNetwork()
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.UseDefaultCredentials = true;
                    try
                    {
                        string html = await client.DownloadStringTaskAsync(Utilities.HelpBaseUri);
                        if (html.Contains("XML Notepad"))
                        {
                            this.BeginInvoke(new Action(FoundOnlineHelp));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("online help is not reachable " + ex.Message);
                    }
                }
            }

            if (!Directory.Exists(Path.Combine(Application.StartupPath, "Help")))
            {
                // Must use online help in this case since we have no offline help
                Utilities.OnlineHelpAvailable = true;
            }
        }

        private void FoundOnlineHelp()
        {
            Utilities.OnlineHelpAvailable = true;
            InitializeHelp(this.helpProvider1);
        }

        protected virtual void SetDefaultSettings()
        {
            // populate default settings and provide type info.
            Font f = new Font("Courier New", 10, FontStyle.Regular);
            this.settings["Font"] = f;
            this.settings["Theme"] = ColorTheme.Light;
            this.settings["LightColors"] = UserSettings.GetDefaultColors(ColorTheme.Light);
            this.settings["DarkColors"] = UserSettings.GetDefaultColors(ColorTheme.Dark);
            this.settings["FileName"] = new Uri("/", UriKind.RelativeOrAbsolute);
            this.settings["WindowBounds"] = new Rectangle(0, 0, 0, 0);
            this.settings["TaskListSize"] = 0;
            this.settings["TreeViewSize"] = 0;
            this.settings["RecentFiles"] = new Uri[0];
            this.settings["SearchWindowLocation"] = new Point(0, 0);
            this.settings["SearchSize"] = new Size(0, 0);
            this.settings["FindMode"] = false;
            this.settings["SearchXPath"] = false;
            this.settings["SearchWholeWord"] = false;
            this.settings["SearchRegex"] = false;
            this.settings["SearchMatchCase"] = false;

            this.settings["LastUpdateCheck"] = DateTime.Now;
            this.settings["UpdateFrequency"] = TimeSpan.FromDays(20);
            this.settings["UpdateLocation"] = UserSettings.DefaultUpdateLocation;
            this.settings["UpdateEnabled"] = true;

            this.settings["AutoFormatOnSave"] = true;
            this.settings["IndentLevel"] = 2;
            this.settings["IndentChar"] = IndentChar.Space;
            this.settings["NewLineChars"] = Utilities.Escape("\r\n");
            this.settings["Language"] = "";
            this.settings["NoByteOrderMark"] = false;

            this.settings["AppRegistered"] = false;
            this.settings["MaximumLineLength"] = 10000;
            this.settings["MaximumValueLength"] = (int)short.MaxValue;
            this.settings["AutoFormatLongLines"] = false;
            this.settings["IgnoreDTD"] = false;

            // XSLT options
            this.settings["BrowserVersion"] = "";
            this.settings["EnableXsltScripts"] = true;
            this.settings["WebView2Exception"] = "";

            // XmlDiff options
            this.settings["XmlDiffIgnoreChildOrder"] = false;
            this.settings["XmlDiffIgnoreComments"] = false;
            this.settings["XmlDiffIgnorePI"] = false;
            this.settings["XmlDiffIgnoreWhitespace"] = false;
            this.settings["XmlDiffIgnoreNamespaces"] = false;
            this.settings["XmlDiffIgnorePrefixes"] = false;
            this.settings["XmlDiffIgnoreXmlDecl"] = false;
            this.settings["XmlDiffIgnoreDtd"] = false;

            // analytics question has been answered...
            this.Settings["AllowAnalytics"] = false;
            this.Settings["AnalyticsClientId"] = "";

            // default text editor
            string sysdir = Environment.SystemDirectory;
            this.Settings["TextEditor"] = Path.Combine(sysdir, "notepad.exe");
        }

        public FormMain(string[] args)
            : this()
        {
            this.args = args;
        }

        public Settings Settings => settings;

        public XmlCache Model
        {
            get { return model; }
            set { model = value; }
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
            if (string.IsNullOrEmpty(hp.HelpNamespace) || Utilities.DynamicHelpEnabled)
            {
                hp.HelpNamespace = Utilities.DefaultHelp;
                Utilities.DynamicHelpEnabled = true;
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
                    if (this.search != null)
                    {
                        this.search.FindAgain(e.Shift);
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
                    taskList.NavigateSelectedError();
                    break;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            this.updater = new Updater(this.settings, this.delayedActions);
            this.updater.Title = this.Caption;
            this.updater.UpdateRequired += new EventHandler<bool>(OnUpdateRequired);
            LoadConfig();
            this.xmlTreeView1.OnLoaded();
            EnsureWindowBounds();
            base.OnLoad(e);
        }

        private void EnsureWindowBounds()
        {
            Rectangle r = (Rectangle)this.settings["WindowBounds"];
            if (r.Width == 0)
            {
                // trigger the settings change that makes the window visible the firs time.
                this.settings["WindowBounds"] = this.Bounds;
            }
        }

        void OnUpdateRequired(object sender, bool updateAvailable)
        {
            if (this.Disposing)
            {
                return;
            }
            this.toolStripMenuItemUpdate.Tag = updateAvailable;
            if (updateAvailable)
            {
                this.toolStripMenuItemUpdate.Text = SR.UpdateAvailableCaption;
                this.toolStripMenuItemUpdate.ToolTipText = string.Format(SR.UpdateAvailableTooltip, this.caption, updater.Version) + "\r\n" +
                    SR.ShowInstallPage;
                this.menuStrip1.ShowItemToolTips = true;
            }
            else
            {
                this.toolStripMenuItemUpdate.Text = SR.UpToDate;
                this.toolStripMenuItemUpdate.ToolTipText = string.Format(SR.UpToDateTooltip, updater.Version) + "\r\n" +
                    SR.ShowUpdateHistory;
                this.menuStrip1.ShowItemToolTips = true;
            }
            this.toolStripMenuItemUpdate.Visible = true;
            this.delayedActions.StartDelayedAction(HideUpdateButtonAction, () =>
            {
                this.toolStripMenuItemUpdate.Visible = false;
            }, TimeSpan.FromSeconds(30));
        }

        void toolStripMenuItemUpdate_Click(object sender, EventArgs e)
        {
            if (this.toolStripMenuItemUpdate.Tag is bool updateAvailable && updateAvailable)
            {
                Utilities.OpenUrl(this.Handle, this.updater.InstallerLocation);
            }
            else if (this.updater.InstallerHistory != null)
            {
                Utilities.OpenUrl(this.Handle, this.updater.InstallerHistory);
            }
            else if (this.updater.UpdateLocation != null)
            {
                Utilities.OpenUrl(this.Handle, this.updater.UpdateLocation.ToString());
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            this.xmlTreeView1.Commit();
            this.xsltViewer.OnClosed();
            if (this.model.Dirty)
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
            this.delayedActions.Close();
            SaveConfig();
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            this.xmlTreeView1.Close();
            base.OnClosed(e);
            CleanupTempFiles();
            if (this.updater != null)
            {
                this.updater.Dispose();
            }
            this.delayedActions.Close();
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
            //base.OnLayout(levent);
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
                if (this.settings != null)
                {
                    this.settings.Dispose();
                    this.settings = null;
                }
                if (this.model != null)
                {
                    this.model.Dispose();
                    this.model = null;
                }
                IDisposable d = this.ip as IDisposable;
                if (d != null)
                {
                    d.Dispose();
                }
                this.ip = null;
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
            this.tabPageTaskList.Controls.Add(this.taskList);
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
            this.taskList.Dock = DockStyle.Fill;
            this.taskList.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.taskList.Location = new System.Drawing.Point(3, 3);
            this.taskList.Margin = new Padding(0);
            this.taskList.Name = "taskList";
            this.taskList.Size = new System.Drawing.Size(722, 62);
            this.taskList.TabIndex = 2;
            this.taskList.Navigate += new XmlNotepad.NavigateEventHandler(this.taskList_Navigate);

            //
            // tabPageDynamicHelp
            //
            this.dynamicHelpViewer.Dock = DockStyle.Fill;
            this.dynamicHelpViewer.VisibleChanged += new EventHandler(dynamicHelpViewer_VisibleChanged);
            this.tabPageDynamicHelp.Controls.Add(dynamicHelpViewer);
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
                this.DisplayHelp();
            }
        }

        const string HideUpdateButtonAction = "HideUpdateButton";

        private async void checkUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var saved = this.statusStrip1.BackColor;
            this.statusStrip1.BackColor = this.toolStripMenuItemUpdate.BackColor;
            this.toolStripMenuItemUpdate.Visible = false;
            this.delayedActions.CancelDelayedAction(HideUpdateButtonAction);
            ShowStatus("Checking for updates...");
            try
            {
                bool update = await this.updater.CheckNow();

                string line = this.toolStripMenuItemUpdate.Text.Split('\r')[0];
                ShowStatus(line);

                AnimateUpdateButton(update);
            }
            catch
            {
            }
            this.statusStrip1.BackColor = saved;
        }

        private void AnimateUpdateButton(bool updateAvailable)
        {
            string caption = updateAvailable ? SR.UpdateAvailableCaption : SR.UpToDate;
            //
            // animationCanvas1
            //
            var animationCanvas1 = new XmlNotepad.AnimationCanvas();
            animationCanvas1.Dock = System.Windows.Forms.DockStyle.Fill;
            animationCanvas1.Location = new System.Drawing.Point(0, 0);
            animationCanvas1.Margin = new System.Windows.Forms.Padding(0);
            animationCanvas1.Name = "animationCanvas1";
            animationCanvas1.Size = this.Size;
            animationCanvas1.TabIndex = 0;
            this.Controls.Add(animationCanvas1);

            // give the AnimationCanvas the background image of the current state of this application so
            // that the animation looks like it is floating over this form.  Using transparent Controls
            // doesn't work nicely with double buffered controls, and does an inefficient amount of repainting
            // and DrawToBitmap simply doesn't work at all for WebBrowser controls.
            Rectangle screenRectangle = this.RectangleToScreen(this.ClientRectangle);
            animationCanvas1.InitializeBackgroundFromScreen(screenRectangle.X, screenRectangle.Y + menuStrip1.Height + toolStrip1.Height);

            int buttonWidth = 100;
            using (var g = this.CreateGraphics())
            {
                var size = g.MeasureString(caption, toolStripMenuItemUpdate.Font, this.Width);
                buttonWidth = (int)size.Width + (4 * 2);
            }

            RectangleShape shape = new RectangleShape()
            {
                Fill = new System.Drawing.SolidBrush(this.toolStripMenuItemUpdate.BackColor),
                Label = caption,
                Font = this.Font,
                Foreground = new System.Drawing.SolidBrush(this.ForeColor)
            };

            var animation = new BoundsAnimation()
            {
                Duration = TimeSpan.FromSeconds(1),
                End = new Rectangle(this.Width - 100, 0, 100, 30),
                Start = new Rectangle(0, this.Height - 30, this.Width, 30),
                TargetProperty = "Bounds",
                Function = new AnimationEaseInFunction()
            };
            animation.Completed += (s, e) =>
            {
                this.Controls.Remove(animationCanvas1);
                OnUpdateRequired(this, updateAvailable);
            };

            shape.BeginAnimation(animation);
            animationCanvas1.Shapes.Add(shape);
            animationCanvas1.BringToFront();
        }

        protected virtual void TabControlViews_Selected(object sender, NoBorderTabControlEventArgs e)
        {
            if (e.TabPage == this.tabPageHtmlView)
            {
                this.DisplayXsltResults();
            }
            else
            {
                this.xsltViewer.OnClosed(); // good time to cleanup temp files.
            }
        }

        void EnableFileMenu()
        {
            bool hasFile = (model.FileName != null);
            this.toolStripButtonSave.Enabled = this.saveToolStripMenuItem.Enabled = true;
            this.reloadToolStripMenuItem.Enabled = hasFile;
            this.saveAsToolStripMenuItem.Enabled = true;
        }

        public virtual void DisplayXsltResults()
        {
            this.xsltViewer.DisplayXsltResults();
            this.analytics.RecordXsltView();
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
            model.Clear();
            includesExpanded = false;
            EnableFileMenu();
            this.settings["FileName"] = new Uri("/", UriKind.RelativeOrAbsolute);
            UpdateMenuState();
        }

        protected virtual IIntellisenseProvider CreateIntellisenseProvider(XmlCache model, ISite site)
        {
            return new XmlIntellisenseProvider(this.model, site);
        }

        protected override object GetService(Type service)
        {
            if (service == typeof(UndoManager))
            {
                return this.undoManager;
            }
            else if (service == typeof(SchemaCache))
            {
                return this.model.SchemaCache;
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
                if (null == this.model)
                {
                    this.model = new XmlCache((IServiceProvider)this, this.delayedActions);
                }
                return this.model;
            }
            else if (service == typeof(Settings))
            {
                return this.settings;
            }
            else if (service == typeof(IIntellisenseProvider))
            {
                if (this.ip == null) this.ip = CreateIntellisenseProvider(this.model, this);
                return this.ip;
            }
            else if (service == typeof(HelpProvider))
            {
                return this.helpProvider1;
            }
            else if (service == typeof(WebProxyService))
            {
                if (this.proxyService == null)
                    this.proxyService = new WebProxyService((IServiceProvider)this);
                return this.proxyService;
            }
            else if (service == typeof(UserSettings))
            {
                return new UserSettings(this.settings);
            }
            else if (service == typeof(DelayedActions))
            {
                return this.delayedActions;
            }
            return base.GetService(service);
        }

        public OpenFileDialog OpenFileDialog
        {
            get { return this.od; }
        }

        public virtual void OpenDialog(string dir = null)
        {
            SelectTreeView();
            if (!SaveIfDirty(true))
                return;
            if (od == null) od = new OpenFileDialog();
            if (model.FileName != null)
            {
                Uri uri = new Uri(model.FileName);
                if (uri.Scheme == "file")
                {
                    od.FileName = model.FileName;
                }
            }
            if (!string.IsNullOrEmpty(dir))
            {
                od.InitialDirectory = dir;
            }
            string filter = SR.OpenFileFilter;
            od.Filter = filter;
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
            od.FilterIndex = index;
            if (od.ShowDialog(this) == DialogResult.OK)
            {
                Open(od.FileName);
            }
        }

        public virtual void ShowStatus(string msg)
        {
            this.toolStripStatusLabel1.Text = msg;
            this.delayedActions.StartDelayedAction("ClearStatus", ClearStatus, TimeSpan.FromSeconds(20));
        }

        private void ClearStatus()
        {
            this.toolStripStatusLabel1.Text = "";
        }

        public virtual void Open(string filename, bool recentFile = false)
        {
            try
            {
                // Make sure you've called SaveIfDirty before calling this method.
                string ext = System.IO.Path.GetExtension(filename).ToLowerInvariant();
                switch (ext)
                {
                    case ".csv":
                        ImportCsv(filename);
                        break;
                    case ".htm":
                    case ".html":
                        ImportHtml(filename);
                        break;
                    default:
                        InternalOpen(filename);
                        break;
                }
            }
            catch (Exception e)
            {
                Uri uri = null;
                try
                {
                    uri = new Uri(filename);
                }
                catch
                {
                    var msg = string.Format(SR.InvalidFileName, filename);
                    MessageBox.Show(this, msg, SR.LoadErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (recentFile && this.recentFiles.Contains(uri))
                {
                    if (MessageBox.Show(this, SR.RecentFileNotFoundMessage, SR.RecentFileNotFoundCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        this.recentFiles.RemoveRecentFile(uri);
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

        private void ImportHtml(string filename)
        {
            includesExpanded = false;
            DateTime start = DateTime.Now;

            using (var html = new StreamReader(filename, true))
            {
                using (var reader = new SgmlReader())
                {
                    reader.DocType = "HTML";
                    reader.CaseFolding = CaseFolding.ToLower;
                    reader.InputStream = html;
                    reader.WhitespaceHandling = WhitespaceHandling.Significant;
                    this.model.Load(reader, filename);
                }
            }

            DateTime finish = DateTime.Now;
            TimeSpan diff = finish - start;
            string s = diff.ToString();
            this.settings["FileName"] = this.model.Location;
            this.UpdateCaption();
            ShowStatus(string.Format(SR.LoadedTimeStatus, s));
            EnableFileMenu();
            this.recentFiles.AddRecentFile(this.model.Location);
            SelectTreeView();
        }

        private void ImportCsv(string filename)
        {
            FormCsvImport importForm = new XmlNotepad.FormCsvImport();
            importForm.FileName = filename;
            if (importForm.ShowDialog() == DialogResult.OK)
            {
                // then import it for real...
                using (StreamReader reader = new StreamReader(filename))
                {
                    string xmlFile = Path.Combine(Path.GetDirectoryName(filename),
                        Path.GetFileNameWithoutExtension(filename) + ".xml");

                    XmlCsvReader csv = new XmlCsvReader(reader, new Uri(filename), new NameTable());
                    csv.Delimiter = importForm.Deliminter;
                    csv.FirstRowHasColumnNames = importForm.FirstRowIsHeader;

                    includesExpanded = false;
                    DateTime start = DateTime.Now;
                    this.model.Load(csv, xmlFile);
                    DateTime finish = DateTime.Now;
                    TimeSpan diff = finish - start;
                    string s = diff.ToString();
                    this.settings["FileName"] = this.model.Location;
                    this.UpdateCaption();
                    ShowStatus(string.Format(SR.LoadedTimeStatus, s));
                    EnableFileMenu();
                    this.recentFiles.AddRecentFile(this.model.Location);
                    SelectTreeView();
                }

                this.analytics.RecordCsvImport();
            }
        }

        private void InternalOpen(string filename)
        {
            includesExpanded = false;
            DateTime start = DateTime.Now;
            this.model.Load(filename);
            DateTime finish = DateTime.Now;
            TimeSpan diff = finish - start;
            string s = diff.ToString();
            this.settings["FileName"] = this.model.Location;
            this.UpdateCaption();
            ShowStatus(string.Format(SR.LoadedTimeStatus, s));
            EnableFileMenu();
            this.recentFiles.AddRecentFile(this.model.Location);
            SelectTreeView();
        }

        bool CheckXIncludes()
        {
            if (includesExpanded)
            {
                if (MessageBox.Show(this, SR.SaveExpandedIncludesPrompt, SR.SaveExpandedIncludesCaption,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
                {
                    return false;
                }
                includesExpanded = false;
            }
            return true;
        }

        public virtual bool SaveIfDirty(bool prompt)
        {
            if (model.Dirty)
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
            string fname = model.FileName;
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
                        model.Save();
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
            if (model.IsReadOnly(fname))
            {
                SelectTreeView();
                if (MessageBox.Show(this, string.Format(SR.ReadOnly, fname),
                    SR.ReadOnlyCaption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                {
                    model.MakeReadWrite(fname);
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
                bool hasFile = (model.FileName != null);
                if (!hasFile && string.IsNullOrEmpty(newName))
                {
                    SaveAs();
                }
                if (CheckReadOnly(newName))
                {
                    model.Save(newName);
                    UpdateCaption();
                    ShowStatus(SR.SavedStatus);
                    this.settings["FileName"] = model.Location;
                    EnableFileMenu();
                    this.recentFiles.AddRecentFile(model.Location);
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
                if (model.IsFile) sd.FileName = model.FileName;
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
            string caption = this.Caption + " - " + model.FileName;
            if (this.model.Dirty)
            {
                caption += "*";
            }
            this.Text = caption;
            sourceToolStripMenuItem.Enabled = this.model.FileName != null;
        }

        void OnFileChanged(object sender, EventArgs e)
        {
            if (!prompting) OnFileChanged();
        }

        bool prompting = false;
        private bool showingOptions;

        protected virtual void OnFileChanged()
        {
            prompting = true;
            try
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
                SelectTreeView();
                if (MessageBox.Show(this, SR.FileChagedOnDiskPrompt, SR.FileChagedOnDiskCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    string location = this.model.Location.LocalPath;
                    this.model.Clear();
                    this.Open(location);
                }
            }
            finally
            {
                prompting = false;
            }
        }

        private void undoManager_StateChanged(object sender, EventArgs e)
        {
            this.undoToolStripMenuItem.Enabled = toolStripButtonUndo.Enabled = this.undoManager.CanUndo;
            this.redoToolStripMenuItem.Enabled = toolStripButtonRedo.Enabled = this.undoManager.CanRedo;
            Command cmd = this.undoManager.Peek();
            this.undoToolStripMenuItem.Text = this.undoLabel + " " + (cmd == null ? "" : cmd.Name);
            cmd = this.undoManager.Current;
            this.redoToolStripMenuItem.Text = this.redoLabel + " " + (cmd == null ? "" : cmd.Name);
        }

        public virtual string ConfigFile
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                Debug.Assert(!string.IsNullOrEmpty(path));
                return System.IO.Path.Combine(path, "Microsoft", "Xml Notepad", "XmlNotepad.settings");
            }
        }

        public virtual string LocalConfigFile
        {
            get
            {
                string path = Path.GetDirectoryName(this.GetType().Assembly.Location);
                Debug.Assert(!string.IsNullOrEmpty(path));
                return System.IO.Path.Combine(path, "XmlNotepad.settings");
            }
        }

        public virtual void LoadConfig()
        {
            string path;
            try
            {
                this.loading = true;
                if (this.args != null && this.args.Length > 0)
                {
                    // When user passes arguments we skip the config file
                    // This is for unit testing where we need consistent config!
                    path = this.args[0];
                    this.settings.FileName = this.ConfigFile;
                }
                else
                {
                    // allow user to have a local settings file (xcopy deployable).
                    path = this.LocalConfigFile;
                    if (!File.Exists(path))
                    {
                        path = this.ConfigFile;
                    }

                    if (File.Exists(path))
                    {
                        settings.Load(path);

                        UserSettings.AddDefaultColors(settings, "LightColors", ColorTheme.Light);
                        UserSettings.AddDefaultColors(settings, "DarkColors", ColorTheme.Dark);

                        string newLines = (string)this.settings["NewLineChars"];

                        Uri location = (Uri)this.settings["FileName"];
                        // Load up the last file we were editing before - if it is local and still exists.
                        if (location != null && location.OriginalString != "/" && location.IsFile && File.Exists(location.LocalPath))
                        {
                            path = location.LocalPath;
                        }

                        string updates = (string)this.settings["UpdateLocation"];
                        if (string.IsNullOrEmpty(updates) ||
                            updates.Contains("download.microsoft.com") ||
                            updates.Contains("lovettsoftware.com"))
                        {
                            this.settings["UpdateLocation"] = UserSettings.DefaultUpdateLocation;
                        }
                    }
                }
            }
            finally
            {
                this.loading = false;
            }

            CheckAnalytics();
            InitializeXsltViewer();
        }

        private void InitializeXsltViewer()
        {
            // now that we have loaded the settings, we can finish initializing the XsltControls.
            this.xsltViewer.SetSite(this);
            this.xsltViewer.Completed += OnXsltComplete;
            this.xsltViewer.GetXsltControl().WebBrowserException += OnWebBrowserException;
            this.dynamicHelpViewer.SetSite(this);
        }

        private void OnWebBrowserException(object sender, Exception e)
        {
            if (this.showingOptions && e is WebView2Exception)
            {
                MessageBox.Show(this, string.Format(SR.WebView2Error, e.Message), "WebView2 Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public bool AllowAnalytics { get; set; }

        private void CheckAnalytics()
        {
            if ((string)this.Settings["AnalyticsClientId"] == "" && AllowAnalytics)
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

            analytics = new Analytics((string)this.Settings["AnalyticsClientId"], (bool)this.Settings["AllowAnalytics"] && this.AllowAnalytics);
            analytics.RecordAppLaunched();
        }

        public virtual void SaveConfig()
        {
            this.settings.StopWatchingFileChanges();
            Rectangle r = (this.WindowState == FormWindowState.Normal) ? this.Bounds : this.RestoreBounds;
            this.settings["WindowBounds"] = r;
            this.settings["TaskListSize"] = this.tabControlLists.Height;
            this.settings["TreeViewSize"] = this.xmlTreeView1.ResizerPosition;
            this.settings["RecentFiles"] = this.recentFiles.ToArray();
            var path = this.settings.FileName;
            if (string.IsNullOrEmpty(path))
            {
                path = this.ConfigFile;
            }
            this.settings.Save(path);
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
                this.undoManager.Clear();
                this.taskList.Clear();
            }
            if (e.ModelChangeType == ModelChangeType.BeginBatchUpdate)
            {
                batch++;
            }
            else if (e.ModelChangeType == ModelChangeType.EndBatchUpdate)
            {
                batch--;
            }
            if (batch == 0) OnModelChanged();
        }

        protected virtual void OnModelChanged()
        {
            TaskHandler handler = new TaskHandler(this.taskList);
            handler.Start();
            this.model.ValidateModel(handler);
            handler.Finish();
            UpdateCaption();
        }

        protected virtual void OnSettingsChanged(object sender, string name)
        {
            switch (name)
            {
                case "File":
                    this.settings.Reload(); // just do it!!
                    break;
                case "WindowBounds":
                    if (this.loading)
                    {
                        Rectangle r = (Rectangle)this.settings["WindowBounds"];
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
                    int pos = (int)this.settings["TreeViewSize"];
                    if (pos != 0)
                    {
                        this.xmlTreeView1.ResizerPosition = pos;
                    }
                    break;
                case "TaskListSize":
                    int height = (int)this.settings["TaskListSize"];
                    if (height != 0)
                    {
                        this.tabControlLists.Height = height;
                    }
                    break;
                case "Font":
                    this.Font = (Font)this.settings["Font"];
                    break;
                case "RecentFiles":
                    Uri[] files = (Uri[])this.settings["RecentFiles"];
                    if (files != null)
                    {
                        this.recentFiles.SetFiles(files);
                    }
                    break;
            }
        }

        public void SaveErrors(string filename)
        {
            this.taskList.Save(filename);
        }

        void OnRecentFileSelected(object sender, RecentFileEventArgs e)
        {
            if (!this.SaveIfDirty(true))
                return;
            string fileName = e.FileName.OriginalString;
            Open(fileName, true);
        }

        private void treeView1_SelectionChanged(object sender, EventArgs e)
        {
            UpdateMenuState();
            DisplayHelp();
        }

        private void DisplayHelp()
        {
            // display documentation
            if (null == xmlTreeView1.SelectedNode)
            {
                this.dynamicHelpViewer.DisplayXsltResults(new XmlDocument(), null);
                return;
            }
            XmlDocument xmlDoc = xmlTreeView1.SelectedNode.GetDocumentation();
            if (this.dynamicHelpViewer.Visible)
            {
                helpAvailableHint = false;
                if (null == xmlDoc)
                {
                    xmlDoc = new XmlDocument();
                    if (taskList.Count > 0)
                    {
                        xmlDoc.AppendChild(xmlDoc.CreateElement("errors"));
                    }
                    else
                    {
                        xmlDoc.AppendChild(xmlDoc.CreateElement("nothing"));
                    }
                }
                this.dynamicHelpViewer.DisplayXsltResults(xmlDoc, null);
            }
            else if (helpAvailableHint && xmlDoc != null)
            {
                helpAvailableHint = false;
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
            this.toolStripButtonCopy.Enabled = this.copyToolStripMenuItem.Enabled = this.ctxMenuItemCopy.Enabled = hasXmlNode;
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
            this.undoToolStripMenuItem.Enabled = toolStripButtonUndo.Enabled = this.undoManager.CanUndo;
            this.redoToolStripMenuItem.Enabled = toolStripButtonRedo.Enabled = this.undoManager.CanRedo;

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
                    model.SaveCopy(path);
                }
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
            if (firstActivate)
            {
                this.comboBoxLocation.Focus();
                firstActivate = false;
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
                data.GetDataPresent(this.urlFormat.Name) ||
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


        private void Form1_DragDrop(object sender, DragEventArgs e)
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
                        this.Open(filename);
                    }
                }
            }
            else if (data.GetDataPresent(this.urlFormat.Name))
            {
                Stream stm = data.GetData(this.urlFormat.Name) as Stream;
                if (stm != null)
                {
                    try
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
                        this.Open(url);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error reading drag/drop data: " + ex.Message);
                    }
                }
            }
            else if (data.GetDataPresent("UniformResourceLocator"))
            {
                string uri = (string)data.GetData(DataFormats.UnicodeText);
                if (!string.IsNullOrEmpty(uri))
                {
                    this.Open(uri);
                }
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.CancelEdit();
            New();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.CancelEdit();
            OpenDialog();
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (model.Dirty)
            {
                if (MessageBox.Show(this, SR.DiscardChanges, SR.DiscardChangesCaption,
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel)
                {
                    return;
                }
            }
            Open(this.model.FileName);
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
                this.undoManager.Undo();
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
                    this.undoManager.Redo();
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

            if (search == null || !search.Visible)
            {
                search = new FormSearch(search, (ISite)this);
                search.Owner = this;
                this.analytics.RecordFormSearch();
            }
            else
            {
                search.Activate();
            }
            search.Target = new XmlTreeViewFindTarget(this.xmlTreeView1);
            search.ReplaceMode = replace;

            if (!search.Visible)
            {
                search.Show(this); // modeless
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
                OpenNotepad(this.model.FileName);
            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string oldLocation = (string)settings["UpdateLocation"];
            FormOptions options = new FormOptions();
            options.Owner = this;
            options.Site = this;
            this.showingOptions = true;
            if (options.ShowDialog(this) == DialogResult.OK)
            {
                this.updater.OnUserChange(oldLocation);
            }
            this.showingOptions = false;
            analytics.RecordFormOptions();
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

        private void toolStripButtonOpen_Click(object sender, EventArgs e)
        {
            this.xmlTreeView1.CancelEdit();
            this.OpenDialog();
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
                this.undoManager.Undo();
            UpdateMenuState();
        }

        private void toolStripButtonRedo_Click(object sender, EventArgs e)
        {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.undoManager.Redo();
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
            this.OpenNewWindow(this.model.FileName);
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
            this.analytics.RecordFormSchemas();
        }

        private void nextErrorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.taskList.NavigateNextError();
        }

        private void compareXMLFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.model.FileName))
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
                    if (secondFile.ToUpperInvariant() == this.model.FileName.ToUpperInvariant())
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

        string GetEmbeddedString(string name)
        {
            using (Stream stream = typeof(XmlNotepad.FormMain).Assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                {
                    throw new Exception(string.Format("You have a build problem: resource '{0} not found", name));
                }
                using (StreamReader sr = new StreamReader(stream))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// The html header used by XmlNotepad.
        /// </summary>
        /// <param name="sourceXmlFile">name of baseline xml data</param>
        /// <param name="changedXmlFile">name of file being compared</param>
        /// <param name="resultHtml">Output file</param>
        private void SideBySideXmlNotepadHeader(
            string sourceXmlFile,
            string changedXmlFile,
            TextWriter resultHtml)
        {

            // this initializes the html
            resultHtml.WriteLine("<html><head>");
            resultHtml.WriteLine("<style TYPE='text/css'>");
            resultHtml.WriteLine(GetEmbeddedString("XmlNotepad.Resources.XmlReportStyles.css"));
            resultHtml.WriteLine("</style>");
            resultHtml.WriteLine("</head>");
            resultHtml.WriteLine(GetEmbeddedString("XmlNotepad.Resources.XmlDiffHeader.html"));

            resultHtml.WriteLine(string.Format(SR.XmlDiffBody,
                    System.IO.Path.GetDirectoryName(sourceXmlFile),
                    sourceXmlFile,
                    System.IO.Path.GetDirectoryName(changedXmlFile),
                    changedXmlFile
            ));

        }

        void CleanupTempFiles()
        {
            try
            {
                this.tempFiles.Delete();
            }
            catch
            {
            }
        }

        private void DoCompare(string changed)
        {
            CleanupTempFiles();

            // todo: add UI for setting XmlDiffOptions.

            XmlDiffOptions options = XmlDiffOptions.None;

            if ((bool)this.settings["XmlDiffIgnoreChildOrder"])
            {
                options |= XmlDiffOptions.IgnoreChildOrder;
            }
            if ((bool)this.settings["XmlDiffIgnoreComments"])
            {
                options |= XmlDiffOptions.IgnoreComments;
            }
            if ((bool)this.settings["XmlDiffIgnorePI"])
            {
                options |= XmlDiffOptions.IgnorePI;
            }
            if ((bool)this.settings["XmlDiffIgnoreWhitespace"])
            {
                options |= XmlDiffOptions.IgnoreWhitespace;
            }
            if ((bool)this.settings["XmlDiffIgnoreNamespaces"])
            {
                options |= XmlDiffOptions.IgnoreNamespaces;
            }
            if ((bool)this.settings["XmlDiffIgnorePrefixes"])
            {
                options |= XmlDiffOptions.IgnorePrefixes;
            }
            if ((bool)this.settings["XmlDiffIgnoreXmlDecl"])
            {
                options |= XmlDiffOptions.IgnoreXmlDecl;
            }
            if ((bool)this.settings["XmlDiffIgnoreDtd"])
            {
                options |= XmlDiffOptions.IgnoreDtd;
            }

            this.xmlTreeView1.Commit();
            this.SaveIfDirty(false);
            string filename = this.model.FileName;

            // load file from disk, as saved doc can be slightly different
            // (e.g. it might now have an XML declaration).  This ensures we
            // are diffing the exact same doc as we see loaded below on the
            // diffView.Load call.
            XmlDocument original = new XmlDocument();
            XmlReaderSettings settings = model.GetReaderSettings();
            using (XmlReader reader = XmlReader.Create(filename, settings))
            {
                original.Load(reader);
            }

            XmlDocument doc = new XmlDocument();
            settings = model.GetReaderSettings();
            using (XmlReader reader = XmlReader.Create(changed, settings))
            {
                doc.Load(reader);
            }

            //output diff file.
            string diffFile = Path.Combine(Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".xml");
            this.tempFiles.AddFile(diffFile, false);

            bool isEqual = false;
            XmlTextWriter diffWriter = new XmlTextWriter(diffFile, Encoding.UTF8);
            diffWriter.Formatting = Formatting.Indented;
            using (diffWriter)
            {
                XmlDiff diff = new XmlDiff(options);
                isEqual = diff.Compare(original, doc, diffWriter);
            }

            if (isEqual)
            {
                //This means the files were identical for given options.
                MessageBox.Show(this, SR.FilesAreIdenticalPrompt, SR.FilesAreIdenticalCaption,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            string tempFile = Path.Combine(Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".htm");
            tempFiles.AddFile(tempFile, false);

            using (XmlReader diffGram = XmlReader.Create(diffFile, settings))
            {
                XmlDiffView diffView = new XmlDiffView();
                using (var reader = new XmlTextReader(filename))
                {
                    diffView.Load(reader, diffGram);
                    using (TextWriter htmlWriter = new StreamWriter(tempFile, false, Encoding.UTF8))
                    {
                        SideBySideXmlNotepadHeader(this.model.FileName, changed, htmlWriter);
                        diffView.GetHtml(htmlWriter);
                        htmlWriter.WriteLine("</body></html>");
                    }
                }
            }

            Utilities.OpenUrl(this.Handle, tempFile);
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
                            String.Format(SR.CreateFile, path), SR.CreateNodeFileCaption,
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
            this.model.ExpandIncludes();
            includesExpanded = true;
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
            if (this.model == null || string.IsNullOrEmpty(this.model.FileName))
            {
                return;
            }
            this.xmlTreeView1.Commit();
            this.SaveIfDirty(false);

            var temp = Path.GetTempPath();
            var scratch = Path.Combine(temp, "XmlNotepad");
            Directory.CreateDirectory(scratch);

            string exePath = Path.Combine(scratch, "XmlStats.exe");

            if (!Utilities.ExtractEmbeddedResourceAsFile("XmlNotepad.Resources.XmlStats.exe", exePath))
            {
                return;
            }

            string fileNameFile = Path.Combine(scratch, "names.txt");

            // need proper utf-8 encoding of the file name which can't be done with "cmd /k" command line.
            using (TextWriter cmdFile = new StreamWriter(fileNameFile, false, Encoding.UTF8))
            {
                cmdFile.WriteLine(this.model.FileName);
            }

            // now we can use "xmlstats -f names.txt" to generate the stats in a console window, this
            // way the user learns they can use xmlstats from the command line.
            string tempFile = Path.Combine(scratch, "stats.cmd");
            using (TextWriter cmdFile = new StreamWriter(tempFile, false, Encoding.Default))
            {
                cmdFile.WriteLine("@echo off");
                cmdFile.WriteLine("echo XML stats for: " + this.model.FileName);
                cmdFile.WriteLine("echo ---------------" + new string('-', this.model.FileName.Length));
                cmdFile.WriteLine("xmlstats -f \"{0}\"", fileNameFile);
                cmdFile.WriteLine("echo ---------------" + new string('-', this.model.FileName.Length));
                cmdFile.WriteLine("echo You can explore other options using : xmlstats -? " + this.model.FileName);
            }

            string cmd = Path.Combine(Environment.GetEnvironmentVariable("WINDIR"), "System32", "cmd.exe");

            ProcessStartInfo pi = new ProcessStartInfo(cmd, "/K " + tempFile);
            pi.UseShellExecute = true;
            pi.WorkingDirectory = scratch;
            Process.Start(pi);
        }

        private void sampleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string path = typeof(XmlNotepad.FormMain).Assembly.Location;
            string dir = System.IO.Path.GetDirectoryName(path);
            while (!string.IsNullOrEmpty(dir))
            {
                string samples = System.IO.Path.Combine(dir, "Samples");
                if (Directory.Exists(samples))
                {
                    OpenDialog(samples);
                    return;
                }
                if (System.IO.Path.GetFileName(dir) == "xmln..tion_ab3ea86545595e2b_0002.0008_ab0a31dd50b50bdb")
                {
                    // don't venture outside our clickonce sandbox.
                    break;
                }
                dir = System.IO.Path.GetDirectoryName(dir);
            }
            MessageBox.Show(this, SR.SamplesNotFound);
        }
    }

}