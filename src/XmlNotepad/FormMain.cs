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

namespace XmlNotepad
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public class FormMain : Form, ISite {

        UndoManager undoManager;
        Settings settings;
        string[] args;
        DataFormats.Format urlFormat;
        RecentFilesMenu recentFiles;
        TaskList taskList;
        XsltControl dynamicHelpViewer;
        bool loading;
        FormSearch search;
        IIntellisenseProvider ip;
        OpenFileDialog od;
        WebProxyService proxyService;
        bool firstActivate = true;
        int batch;
        bool includesExpanded;
        bool helpAvailableHint = true;
        Analytics analytics;
        Updater updater;
        DelayedActions delayedActions = new DelayedActions();
        System.CodeDom.Compiler.TempFileCollection tempFiles = new System.CodeDom.Compiler.TempFileCollection();
        private ContextMenuStrip contextMenu1;
        private ToolStripSeparator ctxMenuItem20;
        private ToolStripSeparator ctxMenuItem23;
        private ToolStripMenuItem ctxcutToolStripMenuItem;
        private ToolStripMenuItem ctxMenuItemCopy;
        private ToolStripMenuItem ctxMenuItemPaste;
        private ToolStripMenuItem ctxMenuItemExpand;
        private ToolStripMenuItem ctxMenuItemCollapse;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem newToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem reloadToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem2;
        private ToolStripMenuItem recentFilesToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem3;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem undoToolStripMenuItem;
        private ToolStripMenuItem redoToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem4;
        private ToolStripMenuItem cutToolStripMenuItem;
        private ToolStripMenuItem copyToolStripMenuItem;
        private ToolStripMenuItem pasteToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem5;
        private ToolStripMenuItem deleteToolStripMenuItem;
        private ToolStripMenuItem repeatToolStripMenuItem;
        private ToolStripMenuItem insertToolStripMenuItem;
        private ToolStripMenuItem duplicateToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem6;
        private ToolStripMenuItem nudgeToolStripMenuItem;
        private ToolStripMenuItem upToolStripMenuItem;
        private ToolStripMenuItem downToolStripMenuItem;
        private ToolStripMenuItem leftToolStripMenuItem;
        private ToolStripMenuItem rightToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem7;
        private ToolStripMenuItem findToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem expandAllToolStripMenuItem;
        private ToolStripMenuItem collapseAllToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem8;
        private ToolStripMenuItem statusBarToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem9;
        private ToolStripMenuItem sourceToolStripMenuItem;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem attributeToolStripMenuItem;
        private ToolStripMenuItem textToolStripMenuItem;
        private ToolStripMenuItem commentToolStripMenuItem;
        private ToolStripMenuItem CDATAToolStripMenuItem;
        private ToolStripMenuItem PIToolStripMenuItem;
        private ToolStripMenuItem contentsToolStripMenuItem;
        private ToolStripMenuItem indexToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem10;
        private ToolStripMenuItem aboutXMLNotepadToolStripMenuItem;
        private ToolStrip toolStrip1;
        private ToolStripButton toolStripButtonNew;
        private ToolStripButton toolStripButtonOpen;
        private ToolStripButton toolStripButtonSave;
        private ToolStripButton toolStripButtonUndo;
        private ToolStripButton toolStripButtonRedo;
        private ToolStripButton toolStripButtonCut;
        private ToolStripButton toolStripButtonCopy;
        private ToolStripButton toolStripButtonPaste;
        private ToolStripButton toolStripButtonDelete;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton toolStripButtonNudgeUp;
        private ToolStripButton toolStripButtonNudgeDown;
        private ToolStripButton toolStripButtonNudgeLeft;
        private ToolStripButton toolStripButtonNudgeRight;
        private HelpProvider helpProvider1;
        private ToolStripMenuItem elementToolStripMenuItem;
        private ToolStripMenuItem elementAfterToolStripMenuItem;
        private ToolStripMenuItem elementBeforeToolStripMenuItem;
        private ToolStripMenuItem elementChildToolStripMenuItem;
        private ToolStripMenuItem attributeBeforeToolStripMenuItem;
        private ToolStripMenuItem attributeAfterToolStripMenuItem;
        private ToolStripMenuItem attributeChildToolStripMenuItem;
        private ToolStripMenuItem textBeforeToolStripMenuItem;
        private ToolStripMenuItem textAfterToolStripMenuItem;
        private ToolStripMenuItem textChildToolStripMenuItem;
        private ToolStripMenuItem commentBeforeToolStripMenuItem;
        private ToolStripMenuItem commentAfterToolStripMenuItem;
        private ToolStripMenuItem commentChildToolStripMenuItem;
        private ToolStripMenuItem cdataBeforeToolStripMenuItem;
        private ToolStripMenuItem cdataAfterToolStripMenuItem;
        private ToolStripMenuItem cdataChildToolStripMenuItem;
        private ToolStripMenuItem PIBeforeToolStripMenuItem;
        private ToolStripMenuItem PIAfterToolStripMenuItem;
        private ToolStripMenuItem PIChildToolStripMenuItem;
        private ToolStripMenuItem ctxElementToolStripMenuItem;
        private ToolStripMenuItem ctxElementBeforeToolStripMenuItem;
        private ToolStripMenuItem ctxElementAfterToolStripMenuItem;
        private ToolStripMenuItem ctxElementChildToolStripMenuItem;
        private ToolStripMenuItem ctxAttributeToolStripMenuItem;
        private ToolStripMenuItem ctxAttributeAfterToolStripMenuItem;
        private ToolStripMenuItem ctxAttributeChildToolStripMenuItem;
        private ToolStripMenuItem ctxTextToolStripMenuItem;
        private ToolStripMenuItem ctxTextBeforeToolStripMenuItem;
        private ToolStripMenuItem ctxTextAfterToolStripMenuItem;
        private ToolStripMenuItem ctxTextChildToolStripMenuItem;
        private ToolStripMenuItem ctxCommentToolStripMenuItem;
        private ToolStripMenuItem ctxCommentBeforeToolStripMenuItem;
        private ToolStripMenuItem ctxCommentAfterToolStripMenuItem;
        private ToolStripMenuItem ctxCommentChildToolStripMenuItem;
        private ToolStripMenuItem ctxCdataToolStripMenuItem;
        private ToolStripMenuItem ctxCdataBeforeToolStripMenuItem;
        private ToolStripMenuItem ctxCdataAfterToolStripMenuItem;
        private ToolStripMenuItem ctxCdataChildToolStripMenuItem;
        private ToolStripMenuItem ctxPIToolStripMenuItem;
        private ToolStripMenuItem ctxPIBeforeToolStripMenuItem;
        private ToolStripMenuItem ctxPIAfterToolStripMenuItem;
        private ToolStripMenuItem ctxPIChildToolStripMenuItem;
        private ToolStripMenuItem windowToolStripMenuItem;
        private ToolStripMenuItem newWindowToolStripMenuItem;
        private ToolStripMenuItem schemasToolStripMenuItem;
        private System.ComponentModel.IContainer components;
        private ToolStripSeparator toolStripMenuItem11;
        private ToolStripMenuItem nextErrorToolStripMenuItem;
        private XmlCache model;
        private PaneResizer resizer;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem compareXMLFilesToolStripMenuItem;
        private NoBorderTabControl tabControlLists;
        private NoBorderTabPage tabPageTaskList;
        private NoBorderTabPage tabPageDynamicHelp;
        private NoBorderTabControl tabControlViews;
        protected NoBorderTabPage tabPageTreeView;
        protected NoBorderTabPage tabPageHtmlView;
        private XmlTreeView xmlTreeView1;
        private XsltViewer xsltViewer;

        private string undoLabel;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem ctxGotoDefinitionToolStripMenuItem;
        private ToolStripMenuItem gotoDefinitionToolStripMenuItem;
        private ToolStripMenuItem expandXIncludesToolStripMenuItem;
        private ToolStripMenuItem exportErrorsToolStripMenuItem;
        private ToolStripMenuItem toolStripMenuItemUpdate;
        private ToolStripSeparator toolStripSeparator4;
        private ComboBox comboBoxLocation;
        // ChangeTo menu
        private ToolStripMenuItem changeToToolStripMenuItem;
        private ToolStripMenuItem changeToElementToolStripMenuItem1;
        private ToolStripMenuItem changeToAttributeToolStripMenuItem1;
        private ToolStripMenuItem changeToTextToolStripMenuItem1;
        private ToolStripMenuItem changeToCDATAToolStripMenuItem1;
        private ToolStripMenuItem changeToCommentToolStripMenuItem1;
        private ToolStripMenuItem changeToProcessingInstructionToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem12;
        // ChangeTo Context menu...
        private ToolStripMenuItem changeToContextMenuItem;
        private ToolStripMenuItem changeToAttributeContextMenuItem;
        private ToolStripMenuItem changeToTextContextMenuItem;
        private ToolStripMenuItem changeToCDATAContextMenuItem;
        private ToolStripMenuItem changeToCommentContextMenuItem;
        private ToolStripMenuItem changeToProcessingInstructionContextMenuItem;
        private ToolStripMenuItem incrementalSearchToolStripMenuItem;
        private ToolStripMenuItem deleteToolStripMenuItem1;
        private ToolStripMenuItem renameToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem13;
        private ToolStripMenuItem renameToolStripMenuItem1;
        private ToolStripMenuItem insertToolStripMenuItem1;
        private ToolStripMenuItem duplicateToolStripMenuItem1;
        private ToolStripMenuItem replaceToolStripMenuItem;
        private ToolStripMenuItem ctxAttributeBeforeToolStripMenuItem;
        private ToolStripMenuItem fileAssociationsToolStripMenuItem;
        private ToolStripMenuItem statsToolStripMenuItem;
        private ToolStripMenuItem checkUpdatesToolStripMenuItem;
        private ToolStripMenuItem sampleToolStripMenuItem;
        private ToolStripMenuItem changeToElementContextMenuItem;
        private StatusStrip statusStrip1;
        private StatusStrip statusStrip2;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private string redoLabel;


        public FormMain()
        {
            this.settings = new Settings();
            SetDefaultSettings();

            this.model = (XmlCache)GetService(typeof(XmlCache));
            this.ip = (XmlIntellisenseProvider)GetService(typeof(XmlIntellisenseProvider));
            //this.model = new XmlCache((ISynchronizeInvoke)this);
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

            this.xsltViewer.SetSite(this);
            this.xsltViewer.Completed += OnXsltComplete;
            this.dynamicHelpViewer.SetSite(this);

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

            this.settings.Changed += new SettingsEventHandler(settings_Changed);

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

            System.Threading.Tasks.Task.Run(CheckNetwork);
            System.Threading.Tasks.Task.Run((Action)CheckNetwork);
        }

        private void OnXsltComplete(object sender, PerformanceInfo e)
        {
            ShowStatus(string.Format(SR.TransformedTimeStatus, e.XsltMilliseconds, e.BrowserMilliseconds));
        }

        private void CheckNetwork()
        {
            if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                System.Net.WebClient client = new System.Net.WebClient();
                client.UseDefaultCredentials = true;
                try
                {
                    string html = client.DownloadString(Utilities.HelpBaseUri);
                    if (html.Contains("XML Notepad"))
                    {
                        this.BeginInvoke(new Action(FoundOnlineHelp));
                    }
                }
                catch (Exception)
                {
                    // online help is not reachable
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
            this.settings["NewLineChars"] = UserSettings.Escape("\r\n");
            this.settings["Language"] = "";
            this.settings["NoByteOrderMark"] = false;

            this.settings["AppRegistered"] = false;
            this.settings["MaximumLineLength"] = 10000;
            this.settings["MaximumValueLength"] = (int)short.MaxValue;
            this.settings["AutoFormatLongLines"] = false;
            this.settings["IgnoreDTD"] = false;

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
            : this() {
            this.args = args;
        }

        public Settings Settings => settings;

        public XmlCache Model {
            get { return model; }
            set { model = value; }
        }

        public PaneResizer Resizer {
            get { return resizer; }
            set { resizer = value; }
        }

        public NoBorderTabControl TabControlLists {
            get { return tabControlLists; }
            set { tabControlLists = value; }
        }

        public NoBorderTabControl TabControlViews {
            get { return this.tabControlViews; }
            set { tabControlViews = value; }
        }

        public XmlTreeView XmlTreeView {
            get { return xmlTreeView1; }
            set { xmlTreeView1 = value; }
        }

        void InitializeTreeView() {
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

        protected virtual void InitializeHelp(HelpProvider hp) {
            hp.SetHelpNavigator(this, HelpNavigator.TableOfContents);
            hp.Site = this;
            // in case subclass has already set HelpNamespace
            if (string.IsNullOrEmpty(hp.HelpNamespace) || Utilities.DynamicHelpEnabled)
            {
                hp.HelpNamespace = Utilities.DefaultHelp;
                Utilities.DynamicHelpEnabled = true;
            }
        }

        void FocusNextPanel(bool reverse) {
            Control[] panels = new Control[] { this.xmlTreeView1.TreeView, this.xmlTreeView1.NodeTextView, this.tabControlLists.SelectedTab.Controls[0] };
            for (int i = 0; i < panels.Length; i++) {
                Control c = panels[i];
                if (c.ContainsFocus) {
                    int j = i + 1;
                    if (reverse) {
                        j = i - 1;
                        if (j < 0) j = panels.Length - 1;
                    } else if (j >= panels.Length) {
                        j = 0;
                    }
                    SelectTreeView();
                    panels[j].Focus();
                    break;
                }
            }            
        }

        void treeView1_KeyDown(object sender, KeyEventArgs e) {
            // Note if e.SuppressKeyPress is true, then this event is bubbling up from
            // the TextEditorOverlay - so we have to be careful not to interfere with
            // intellisense editing here unless we really want to.  Turns out the following
            // keystrokes all want to interrupt intellisense. 
            if (e.Handled) return;
            switch (e.KeyCode) {
                case Keys.Space:
                    if ((e.Modifiers & Keys.Control) == Keys.Control) {
                        this.xmlTreeView1.Commit();
                        Rectangle r = this.xmlTreeView1.TreeView.Bounds;
                        XmlTreeNode node = this.xmlTreeView1.SelectedNode;
                        if (node != null) {
                            r = node.LabelBounds;
                            r.Offset(this.xmlTreeView1.TreeView.ScrollPosition);
                        }
                        r = this.xmlTreeView1.RectangleToScreen(r);
                        this.contextMenu1.Show(r.Left + (r.Width / 2), r.Top + (r.Height / 2));
                    }
                    break;
                case Keys.F3:
                    if (this.search != null) {
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

        void taskList_KeyDown(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.F6:
                    FocusNextPanel((e.Modifiers & Keys.Shift) != 0);
                    break;
                case Keys.Enter:
                    taskList.NavigateSelectedError();
                    break;
            }
        }

        protected override void OnLoad(EventArgs e) {
            this.updater = new Updater(this.settings);
            this.updater.Title = this.Caption;
            this.updater.UpdateRequired += new EventHandler<bool>(OnUpdateRequired);
            LoadConfig();
            this.xmlTreeView1.OnLoaded();
            base.OnLoad(e);
        }

        void OnUpdateRequired(object sender, bool updateAvailable) {
            if (this.Disposing)
            {
                return;
            }
            ISynchronizeInvoke si = (ISynchronizeInvoke)this;
            if (si.InvokeRequired) {
                // get on the right thread.
                si.Invoke(new EventHandler<bool>(OnUpdateRequired), new object[2] { sender, updateAvailable } );
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
            this.delayedActions.StartDelayedAction(HideUpdateButtonAction, () => {
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

        protected override void OnClosing(CancelEventArgs e) {
            this.xmlTreeView1.Commit();
            if (this.model.Dirty){
                SelectTreeView();
                DialogResult rc = MessageBox.Show(this, SR.SaveChangesPrompt, SR.SaveChangesCaption, 
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
                if (rc == DialogResult.Yes){
                    this.Save();
                } else if (rc == DialogResult.Cancel){
                    e.Cancel = true;
                    return;
                }
            }
            SaveConfig();
            base.OnClosing (e);
        }

        protected override void OnClosed(EventArgs e) {
            this.xmlTreeView1.Close();
            base.OnClosed(e);
            CleanupTempFiles();
            if (this.updater != null) {
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
            this.toolStrip1.Size = new Size(w, 24);
            int top = this.toolStrip1.Bottom;
            int sbHeight = 0;
            if (this.statusStrip1.Visible) {
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
            base.OnLayout(levent);
        }

        
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing ) {
            if( disposing ) {
                if (components != null) {
                    components.Dispose();
                }
                if (this.settings != null) {
                    this.settings.Dispose();
                    this.settings = null;
                }
                if (this.model != null) {
                    this.model.Dispose();
                    this.model = null;
                }
                IDisposable d = this.ip as IDisposable;
                if (d != null) {
                    d.Dispose();
                }
                this.ip = null;
            }
            base.Dispose( disposing );
        }

        protected virtual XmlTreeView CreateTreeView() {
            return new XmlTreeView();
        }

        protected virtual void CreateTabControl() {
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

        void dynamicHelpViewer_VisibleChanged(object sender, EventArgs e) {
            this.DisplayHelp();
        }

        protected virtual void TabControlLists_Selected(object sender, NoBorderTabControlEventArgs e) {
            if (e.TabPage == this.tabPageDynamicHelp) {
                this.DisplayHelp();
            }
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        [STAThread]
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.changeToElementContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenu1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ctxcutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxMenuItemCopy = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxMenuItemPaste = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem13 = new System.Windows.Forms.ToolStripSeparator();
            this.deleteToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.insertToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.renameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.duplicateToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.changeToContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeToAttributeContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeToTextContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeToCDATAContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeToCommentContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeToProcessingInstructionContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.ctxGotoDefinitionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxMenuItem20 = new System.Windows.Forms.ToolStripSeparator();
            this.ctxElementToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxElementBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxElementAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxElementChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxAttributeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxAttributeBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxAttributeAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxAttributeChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxTextBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxTextAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxTextChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCommentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCommentBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCommentAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCommentChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCdataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCdataBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCdataAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCdataChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxPIToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxPIBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxPIAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxPIChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxMenuItem23 = new System.Windows.Forms.ToolStripSeparator();
            this.ctxMenuItemExpand = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxMenuItemCollapse = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportErrorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.recentFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.repeatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renameToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.duplicateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeToToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.changeToElementToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.changeToAttributeToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.changeToTextToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.changeToCDATAToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.changeToCommentToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.changeToProcessingInstructionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem12 = new System.Windows.Forms.ToolStripSeparator();
            this.gotoDefinitionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.expandXIncludesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripSeparator();
            this.nudgeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.upToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.downToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.leftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripSeparator();
            this.findToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.replaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.incrementalSearchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.expandAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.collapseAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripSeparator();
            this.statusBarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem9 = new System.Windows.Forms.ToolStripSeparator();
            this.sourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.schemasToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileAssociationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem11 = new System.Windows.Forms.ToolStripSeparator();
            this.nextErrorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.compareXMLFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.insertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.elementToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.elementBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.elementAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.elementChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.attributeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.attributeBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.attributeAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.attributeChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.textToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.textBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.textAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.textChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.commentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.commentBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.commentAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.commentChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CDATAToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cdataBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cdataAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cdataChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PIToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PIBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PIAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PIChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.windowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.indexToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sampleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.checkUpdatesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem10 = new System.Windows.Forms.ToolStripSeparator();
            this.aboutXMLNotepadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItemUpdate = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonNew = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonOpen = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSave = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonUndo = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonRedo = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonCut = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonCopy = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonPaste = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonNudgeUp = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonNudgeDown = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonNudgeLeft = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonNudgeRight = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.helpProvider1 = new System.Windows.Forms.HelpProvider();
            this.comboBoxLocation = new System.Windows.Forms.ComboBox();
            this.tabControlViews = new XmlNotepad.NoBorderTabControl();
            this.tabPageTreeView = new XmlNotepad.NoBorderTabPage();
            this.xmlTreeView1 = new XmlNotepad.XmlTreeView();
            this.tabPageHtmlView = new XmlNotepad.NoBorderTabPage();
            this.xsltViewer = new XmlNotepad.XsltViewer();
            this.resizer = new XmlNotepad.PaneResizer();
            this.tabPageTaskList = new XmlNotepad.NoBorderTabPage();
            this.tabPageDynamicHelp = new XmlNotepad.NoBorderTabPage();
            this.taskList = new XmlNotepad.TaskList();
            this.dynamicHelpViewer = new XmlNotepad.XsltControl();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.statusStrip2 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.contextMenu1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.tabControlViews.SuspendLayout();
            this.tabPageTreeView.SuspendLayout();
            this.tabPageHtmlView.SuspendLayout();
            this.statusStrip2.SuspendLayout();
            this.SuspendLayout();
            // 
            // changeToElementContextMenuItem
            // 
            this.changeToElementContextMenuItem.Name = "changeToElementContextMenuItem";
            resources.ApplyResources(this.changeToElementContextMenuItem, "changeToElementContextMenuItem");
            this.changeToElementContextMenuItem.Click += new System.EventHandler(this.changeToElementContextMenuItem_Click);
            // 
            // contextMenu1
            // 
            this.contextMenu1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxcutToolStripMenuItem,
            this.ctxMenuItemCopy,
            this.ctxMenuItemPaste,
            this.toolStripMenuItem13,
            this.deleteToolStripMenuItem1,
            this.insertToolStripMenuItem1,
            this.renameToolStripMenuItem,
            this.duplicateToolStripMenuItem1,
            this.changeToContextMenuItem,
            this.toolStripSeparator3,
            this.ctxGotoDefinitionToolStripMenuItem,
            this.ctxMenuItem20,
            this.ctxElementToolStripMenuItem,
            this.ctxAttributeToolStripMenuItem,
            this.ctxTextToolStripMenuItem,
            this.ctxCommentToolStripMenuItem,
            this.ctxCdataToolStripMenuItem,
            this.ctxPIToolStripMenuItem,
            this.ctxMenuItem23,
            this.ctxMenuItemExpand,
            this.ctxMenuItemCollapse});
            this.contextMenu1.Name = "contextMenuStrip1";
            this.helpProvider1.SetShowHelp(this.contextMenu1, ((bool)(resources.GetObject("contextMenu1.ShowHelp"))));
            resources.ApplyResources(this.contextMenu1, "contextMenu1");
            // 
            // ctxcutToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxcutToolStripMenuItem, "ctxcutToolStripMenuItem");
            this.ctxcutToolStripMenuItem.Name = "ctxcutToolStripMenuItem";
            // 
            // ctxMenuItemCopy
            // 
            resources.ApplyResources(this.ctxMenuItemCopy, "ctxMenuItemCopy");
            this.ctxMenuItemCopy.Name = "ctxMenuItemCopy";
            // 
            // ctxMenuItemPaste
            // 
            resources.ApplyResources(this.ctxMenuItemPaste, "ctxMenuItemPaste");
            this.ctxMenuItemPaste.Name = "ctxMenuItemPaste";
            // 
            // toolStripMenuItem13
            // 
            this.toolStripMenuItem13.Name = "toolStripMenuItem13";
            resources.ApplyResources(this.toolStripMenuItem13, "toolStripMenuItem13");
            // 
            // deleteToolStripMenuItem1
            // 
            resources.ApplyResources(this.deleteToolStripMenuItem1, "deleteToolStripMenuItem1");
            this.deleteToolStripMenuItem1.Name = "deleteToolStripMenuItem1";
            this.deleteToolStripMenuItem1.Click += new System.EventHandler(this.deleteToolStripMenuItem1_Click);
            // 
            // insertToolStripMenuItem1
            // 
            this.insertToolStripMenuItem1.Name = "insertToolStripMenuItem1";
            resources.ApplyResources(this.insertToolStripMenuItem1, "insertToolStripMenuItem1");
            this.insertToolStripMenuItem1.Click += new System.EventHandler(this.insertToolStripMenuItem1_Click);
            // 
            // renameToolStripMenuItem
            // 
            this.renameToolStripMenuItem.Name = "renameToolStripMenuItem";
            resources.ApplyResources(this.renameToolStripMenuItem, "renameToolStripMenuItem");
            this.renameToolStripMenuItem.Click += new System.EventHandler(this.renameToolStripMenuItem_Click);
            // 
            // duplicateToolStripMenuItem1
            // 
            this.duplicateToolStripMenuItem1.Name = "duplicateToolStripMenuItem1";
            resources.ApplyResources(this.duplicateToolStripMenuItem1, "duplicateToolStripMenuItem1");
            this.duplicateToolStripMenuItem1.Click += new System.EventHandler(this.duplicateToolStripMenuItem1_Click);
            // 
            // changeToContextMenuItem
            // 
            this.changeToContextMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.changeToElementContextMenuItem,
            this.changeToAttributeContextMenuItem,
            this.changeToTextContextMenuItem,
            this.changeToCDATAContextMenuItem,
            this.changeToCommentContextMenuItem,
            this.changeToProcessingInstructionContextMenuItem});
            this.changeToContextMenuItem.Name = "changeToContextMenuItem";
            resources.ApplyResources(this.changeToContextMenuItem, "changeToContextMenuItem");
            // 
            // changeToAttributeContextMenuItem
            // 
            this.changeToAttributeContextMenuItem.Name = "changeToAttributeContextMenuItem";
            resources.ApplyResources(this.changeToAttributeContextMenuItem, "changeToAttributeContextMenuItem");
            this.changeToAttributeContextMenuItem.Click += new System.EventHandler(this.changeToAttributeContextMenuItem_Click);
            // 
            // changeToTextContextMenuItem
            // 
            this.changeToTextContextMenuItem.Name = "changeToTextContextMenuItem";
            resources.ApplyResources(this.changeToTextContextMenuItem, "changeToTextContextMenuItem");
            this.changeToTextContextMenuItem.Click += new System.EventHandler(this.changeToTextToolStripMenuItem_Click);
            // 
            // changeToCDATAContextMenuItem
            // 
            this.changeToCDATAContextMenuItem.Name = "changeToCDATAContextMenuItem";
            resources.ApplyResources(this.changeToCDATAContextMenuItem, "changeToCDATAContextMenuItem");
            this.changeToCDATAContextMenuItem.Click += new System.EventHandler(this.changeToCDATAContextMenuItem_Click);
            // 
            // changeToCommentContextMenuItem
            // 
            this.changeToCommentContextMenuItem.Name = "changeToCommentContextMenuItem";
            resources.ApplyResources(this.changeToCommentContextMenuItem, "changeToCommentContextMenuItem");
            this.changeToCommentContextMenuItem.Click += new System.EventHandler(this.changeToCommentContextMenuItem_Click);
            // 
            // changeToProcessingInstructionContextMenuItem
            // 
            this.changeToProcessingInstructionContextMenuItem.Name = "changeToProcessingInstructionContextMenuItem";
            resources.ApplyResources(this.changeToProcessingInstructionContextMenuItem, "changeToProcessingInstructionContextMenuItem");
            this.changeToProcessingInstructionContextMenuItem.Click += new System.EventHandler(this.changeToProcessingInstructionContextMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            // 
            // ctxGotoDefinitionToolStripMenuItem
            // 
            this.ctxGotoDefinitionToolStripMenuItem.Name = "ctxGotoDefinitionToolStripMenuItem";
            resources.ApplyResources(this.ctxGotoDefinitionToolStripMenuItem, "ctxGotoDefinitionToolStripMenuItem");
            this.ctxGotoDefinitionToolStripMenuItem.Click += new System.EventHandler(this.ctxGotoDefinitionToolStripMenuItem_Click);
            // 
            // ctxMenuItem20
            // 
            this.ctxMenuItem20.Name = "ctxMenuItem20";
            resources.ApplyResources(this.ctxMenuItem20, "ctxMenuItem20");
            // 
            // ctxElementToolStripMenuItem
            // 
            this.ctxElementToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxElementBeforeToolStripMenuItem,
            this.ctxElementAfterToolStripMenuItem,
            this.ctxElementChildToolStripMenuItem});
            this.ctxElementToolStripMenuItem.Name = "ctxElementToolStripMenuItem";
            resources.ApplyResources(this.ctxElementToolStripMenuItem, "ctxElementToolStripMenuItem");
            // 
            // ctxElementBeforeToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxElementBeforeToolStripMenuItem, "ctxElementBeforeToolStripMenuItem");
            this.ctxElementBeforeToolStripMenuItem.Name = "ctxElementBeforeToolStripMenuItem";
            this.ctxElementBeforeToolStripMenuItem.Click += new System.EventHandler(this.elementBeforeToolStripMenuItem_Click);
            // 
            // ctxElementAfterToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxElementAfterToolStripMenuItem, "ctxElementAfterToolStripMenuItem");
            this.ctxElementAfterToolStripMenuItem.Name = "ctxElementAfterToolStripMenuItem";
            this.ctxElementAfterToolStripMenuItem.Click += new System.EventHandler(this.elementAfterToolStripMenuItem_Click);
            // 
            // ctxElementChildToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxElementChildToolStripMenuItem, "ctxElementChildToolStripMenuItem");
            this.ctxElementChildToolStripMenuItem.Name = "ctxElementChildToolStripMenuItem";
            this.ctxElementChildToolStripMenuItem.Click += new System.EventHandler(this.elementChildToolStripMenuItem_Click);
            // 
            // ctxAttributeToolStripMenuItem
            // 
            this.ctxAttributeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxAttributeBeforeToolStripMenuItem,
            this.ctxAttributeAfterToolStripMenuItem,
            this.ctxAttributeChildToolStripMenuItem});
            this.ctxAttributeToolStripMenuItem.Name = "ctxAttributeToolStripMenuItem";
            resources.ApplyResources(this.ctxAttributeToolStripMenuItem, "ctxAttributeToolStripMenuItem");
            // 
            // ctxAttributeBeforeToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxAttributeBeforeToolStripMenuItem, "ctxAttributeBeforeToolStripMenuItem");
            this.ctxAttributeBeforeToolStripMenuItem.Name = "ctxAttributeBeforeToolStripMenuItem";
            this.ctxAttributeBeforeToolStripMenuItem.Click += new System.EventHandler(this.attributeBeforeToolStripMenuItem_Click);
            // 
            // ctxAttributeAfterToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxAttributeAfterToolStripMenuItem, "ctxAttributeAfterToolStripMenuItem");
            this.ctxAttributeAfterToolStripMenuItem.Name = "ctxAttributeAfterToolStripMenuItem";
            this.ctxAttributeAfterToolStripMenuItem.Click += new System.EventHandler(this.attributeAfterToolStripMenuItem_Click);
            // 
            // ctxAttributeChildToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxAttributeChildToolStripMenuItem, "ctxAttributeChildToolStripMenuItem");
            this.ctxAttributeChildToolStripMenuItem.Name = "ctxAttributeChildToolStripMenuItem";
            this.ctxAttributeChildToolStripMenuItem.Click += new System.EventHandler(this.attributeChildToolStripMenuItem_Click);
            // 
            // ctxTextToolStripMenuItem
            // 
            this.ctxTextToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxTextBeforeToolStripMenuItem,
            this.ctxTextAfterToolStripMenuItem,
            this.ctxTextChildToolStripMenuItem});
            this.ctxTextToolStripMenuItem.Name = "ctxTextToolStripMenuItem";
            resources.ApplyResources(this.ctxTextToolStripMenuItem, "ctxTextToolStripMenuItem");
            // 
            // ctxTextBeforeToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxTextBeforeToolStripMenuItem, "ctxTextBeforeToolStripMenuItem");
            this.ctxTextBeforeToolStripMenuItem.Name = "ctxTextBeforeToolStripMenuItem";
            this.ctxTextBeforeToolStripMenuItem.Click += new System.EventHandler(this.textBeforeToolStripMenuItem_Click);
            // 
            // ctxTextAfterToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxTextAfterToolStripMenuItem, "ctxTextAfterToolStripMenuItem");
            this.ctxTextAfterToolStripMenuItem.Name = "ctxTextAfterToolStripMenuItem";
            this.ctxTextAfterToolStripMenuItem.Click += new System.EventHandler(this.textAfterToolStripMenuItem_Click);
            // 
            // ctxTextChildToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxTextChildToolStripMenuItem, "ctxTextChildToolStripMenuItem");
            this.ctxTextChildToolStripMenuItem.Name = "ctxTextChildToolStripMenuItem";
            this.ctxTextChildToolStripMenuItem.Click += new System.EventHandler(this.textChildToolStripMenuItem_Click);
            // 
            // ctxCommentToolStripMenuItem
            // 
            this.ctxCommentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxCommentBeforeToolStripMenuItem,
            this.ctxCommentAfterToolStripMenuItem,
            this.ctxCommentChildToolStripMenuItem});
            this.ctxCommentToolStripMenuItem.Name = "ctxCommentToolStripMenuItem";
            resources.ApplyResources(this.ctxCommentToolStripMenuItem, "ctxCommentToolStripMenuItem");
            // 
            // ctxCommentBeforeToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxCommentBeforeToolStripMenuItem, "ctxCommentBeforeToolStripMenuItem");
            this.ctxCommentBeforeToolStripMenuItem.Name = "ctxCommentBeforeToolStripMenuItem";
            this.ctxCommentBeforeToolStripMenuItem.Click += new System.EventHandler(this.commentBeforeToolStripMenuItem_Click);
            // 
            // ctxCommentAfterToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxCommentAfterToolStripMenuItem, "ctxCommentAfterToolStripMenuItem");
            this.ctxCommentAfterToolStripMenuItem.Name = "ctxCommentAfterToolStripMenuItem";
            this.ctxCommentAfterToolStripMenuItem.Click += new System.EventHandler(this.commentAfterToolStripMenuItem_Click);
            // 
            // ctxCommentChildToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxCommentChildToolStripMenuItem, "ctxCommentChildToolStripMenuItem");
            this.ctxCommentChildToolStripMenuItem.Name = "ctxCommentChildToolStripMenuItem";
            this.ctxCommentChildToolStripMenuItem.Click += new System.EventHandler(this.commentChildToolStripMenuItem_Click);
            // 
            // ctxCdataToolStripMenuItem
            // 
            this.ctxCdataToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxCdataBeforeToolStripMenuItem,
            this.ctxCdataAfterToolStripMenuItem,
            this.ctxCdataChildToolStripMenuItem});
            this.ctxCdataToolStripMenuItem.Name = "ctxCdataToolStripMenuItem";
            resources.ApplyResources(this.ctxCdataToolStripMenuItem, "ctxCdataToolStripMenuItem");
            // 
            // ctxCdataBeforeToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxCdataBeforeToolStripMenuItem, "ctxCdataBeforeToolStripMenuItem");
            this.ctxCdataBeforeToolStripMenuItem.Name = "ctxCdataBeforeToolStripMenuItem";
            this.ctxCdataBeforeToolStripMenuItem.Click += new System.EventHandler(this.cdataBeforeToolStripMenuItem_Click);
            // 
            // ctxCdataAfterToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxCdataAfterToolStripMenuItem, "ctxCdataAfterToolStripMenuItem");
            this.ctxCdataAfterToolStripMenuItem.Name = "ctxCdataAfterToolStripMenuItem";
            this.ctxCdataAfterToolStripMenuItem.Click += new System.EventHandler(this.cdataAfterToolStripMenuItem_Click);
            // 
            // ctxCdataChildToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxCdataChildToolStripMenuItem, "ctxCdataChildToolStripMenuItem");
            this.ctxCdataChildToolStripMenuItem.Name = "ctxCdataChildToolStripMenuItem";
            this.ctxCdataChildToolStripMenuItem.Click += new System.EventHandler(this.cdataChildToolStripMenuItem_Click);
            // 
            // ctxPIToolStripMenuItem
            // 
            this.ctxPIToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxPIBeforeToolStripMenuItem,
            this.ctxPIAfterToolStripMenuItem,
            this.ctxPIChildToolStripMenuItem});
            this.ctxPIToolStripMenuItem.Name = "ctxPIToolStripMenuItem";
            resources.ApplyResources(this.ctxPIToolStripMenuItem, "ctxPIToolStripMenuItem");
            // 
            // ctxPIBeforeToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxPIBeforeToolStripMenuItem, "ctxPIBeforeToolStripMenuItem");
            this.ctxPIBeforeToolStripMenuItem.Name = "ctxPIBeforeToolStripMenuItem";
            this.ctxPIBeforeToolStripMenuItem.Click += new System.EventHandler(this.PIBeforeToolStripMenuItem_Click);
            // 
            // ctxPIAfterToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxPIAfterToolStripMenuItem, "ctxPIAfterToolStripMenuItem");
            this.ctxPIAfterToolStripMenuItem.Name = "ctxPIAfterToolStripMenuItem";
            this.ctxPIAfterToolStripMenuItem.Click += new System.EventHandler(this.PIAfterToolStripMenuItem_Click);
            // 
            // ctxPIChildToolStripMenuItem
            // 
            resources.ApplyResources(this.ctxPIChildToolStripMenuItem, "ctxPIChildToolStripMenuItem");
            this.ctxPIChildToolStripMenuItem.Name = "ctxPIChildToolStripMenuItem";
            this.ctxPIChildToolStripMenuItem.Click += new System.EventHandler(this.PIChildToolStripMenuItem_Click);
            // 
            // ctxMenuItem23
            // 
            this.ctxMenuItem23.Name = "ctxMenuItem23";
            resources.ApplyResources(this.ctxMenuItem23, "ctxMenuItem23");
            // 
            // ctxMenuItemExpand
            // 
            this.ctxMenuItemExpand.Name = "ctxMenuItemExpand";
            resources.ApplyResources(this.ctxMenuItemExpand, "ctxMenuItemExpand");
            // 
            // ctxMenuItemCollapse
            // 
            this.ctxMenuItemCollapse.Name = "ctxMenuItemCollapse";
            resources.ApplyResources(this.ctxMenuItemCollapse, "ctxMenuItemCollapse");
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.insertToolStripMenuItem,
            this.windowToolStripMenuItem,
            this.helpToolStripMenuItem,
            this.toolStripMenuItemUpdate});
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Name = "menuStrip1";
            this.helpProvider1.SetShowHelp(this.menuStrip1, ((bool)(resources.GetObject("menuStrip1.ShowHelp"))));
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.reloadToolStripMenuItem,
            this.toolStripMenuItem1,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.exportErrorsToolStripMenuItem,
            this.toolStripMenuItem2,
            this.recentFilesToolStripMenuItem,
            this.toolStripMenuItem3,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            resources.ApplyResources(this.fileToolStripMenuItem, "fileToolStripMenuItem");
            // 
            // newToolStripMenuItem
            // 
            resources.ApplyResources(this.newToolStripMenuItem, "newToolStripMenuItem");
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            resources.ApplyResources(this.openToolStripMenuItem, "openToolStripMenuItem");
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // reloadToolStripMenuItem
            // 
            resources.ApplyResources(this.reloadToolStripMenuItem, "reloadToolStripMenuItem");
            this.reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
            this.reloadToolStripMenuItem.Click += new System.EventHandler(this.reloadToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            resources.ApplyResources(this.toolStripMenuItem1, "toolStripMenuItem1");
            // 
            // saveToolStripMenuItem
            // 
            resources.ApplyResources(this.saveToolStripMenuItem, "saveToolStripMenuItem");
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            resources.ApplyResources(this.saveAsToolStripMenuItem, "saveAsToolStripMenuItem");
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // exportErrorsToolStripMenuItem
            // 
            resources.ApplyResources(this.exportErrorsToolStripMenuItem, "exportErrorsToolStripMenuItem");
            this.exportErrorsToolStripMenuItem.Name = "exportErrorsToolStripMenuItem";
            this.exportErrorsToolStripMenuItem.Click += new System.EventHandler(this.exportErrorsToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            resources.ApplyResources(this.toolStripMenuItem2, "toolStripMenuItem2");
            // 
            // recentFilesToolStripMenuItem
            // 
            resources.ApplyResources(this.recentFilesToolStripMenuItem, "recentFilesToolStripMenuItem");
            this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            resources.ApplyResources(this.toolStripMenuItem3, "toolStripMenuItem3");
            // 
            // exitToolStripMenuItem
            // 
            resources.ApplyResources(this.exitToolStripMenuItem, "exitToolStripMenuItem");
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem,
            this.redoToolStripMenuItem,
            this.toolStripMenuItem4,
            this.cutToolStripMenuItem,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.toolStripMenuItem5,
            this.deleteToolStripMenuItem,
            this.repeatToolStripMenuItem,
            this.renameToolStripMenuItem1,
            this.duplicateToolStripMenuItem,
            this.changeToToolStripMenuItem,
            this.toolStripMenuItem12,
            this.gotoDefinitionToolStripMenuItem,
            this.expandXIncludesToolStripMenuItem,
            this.toolStripMenuItem6,
            this.nudgeToolStripMenuItem,
            this.toolStripMenuItem7,
            this.findToolStripMenuItem,
            this.replaceToolStripMenuItem,
            this.incrementalSearchToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            resources.ApplyResources(this.editToolStripMenuItem, "editToolStripMenuItem");
            // 
            // undoToolStripMenuItem
            // 
            resources.ApplyResources(this.undoToolStripMenuItem, "undoToolStripMenuItem");
            this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripMenuItem_Click);
            // 
            // redoToolStripMenuItem
            // 
            resources.ApplyResources(this.redoToolStripMenuItem, "redoToolStripMenuItem");
            this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
            this.redoToolStripMenuItem.Click += new System.EventHandler(this.redoToolStripMenuItem_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            resources.ApplyResources(this.toolStripMenuItem4, "toolStripMenuItem4");
            // 
            // cutToolStripMenuItem
            // 
            resources.ApplyResources(this.cutToolStripMenuItem, "cutToolStripMenuItem");
            this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
            this.cutToolStripMenuItem.Click += new System.EventHandler(this.cutToolStripMenuItem_Click);
            // 
            // copyToolStripMenuItem
            // 
            resources.ApplyResources(this.copyToolStripMenuItem, "copyToolStripMenuItem");
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            // 
            // pasteToolStripMenuItem
            // 
            resources.ApplyResources(this.pasteToolStripMenuItem, "pasteToolStripMenuItem");
            this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            this.pasteToolStripMenuItem.Click += new System.EventHandler(this.pasteToolStripMenuItem_Click);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            resources.ApplyResources(this.toolStripMenuItem5, "toolStripMenuItem5");
            // 
            // deleteToolStripMenuItem
            // 
            resources.ApplyResources(this.deleteToolStripMenuItem, "deleteToolStripMenuItem");
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // repeatToolStripMenuItem
            // 
            resources.ApplyResources(this.repeatToolStripMenuItem, "repeatToolStripMenuItem");
            this.repeatToolStripMenuItem.Name = "repeatToolStripMenuItem";
            this.repeatToolStripMenuItem.Click += new System.EventHandler(this.repeatToolStripMenuItem_Click);
            // 
            // renameToolStripMenuItem1
            // 
            resources.ApplyResources(this.renameToolStripMenuItem1, "renameToolStripMenuItem1");
            this.renameToolStripMenuItem1.Name = "renameToolStripMenuItem1";
            this.renameToolStripMenuItem1.Click += new System.EventHandler(this.renameToolStripMenuItem1_Click);
            // 
            // duplicateToolStripMenuItem
            // 
            resources.ApplyResources(this.duplicateToolStripMenuItem, "duplicateToolStripMenuItem");
            this.duplicateToolStripMenuItem.Name = "duplicateToolStripMenuItem";
            this.duplicateToolStripMenuItem.Click += new System.EventHandler(this.duplicateToolStripMenuItem_Click);
            // 
            // changeToToolStripMenuItem
            // 
            resources.ApplyResources(this.changeToToolStripMenuItem, "changeToToolStripMenuItem");
            this.changeToToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.changeToElementToolStripMenuItem1,
            this.changeToAttributeToolStripMenuItem1,
            this.changeToTextToolStripMenuItem1,
            this.changeToCDATAToolStripMenuItem1,
            this.changeToCommentToolStripMenuItem1,
            this.changeToProcessingInstructionToolStripMenuItem});
            this.changeToToolStripMenuItem.Name = "changeToToolStripMenuItem";
            // 
            // changeToElementToolStripMenuItem1
            // 
            resources.ApplyResources(this.changeToElementToolStripMenuItem1, "changeToElementToolStripMenuItem1");
            this.changeToElementToolStripMenuItem1.Name = "changeToElementToolStripMenuItem1";
            this.changeToElementToolStripMenuItem1.Click += new System.EventHandler(this.elementToolStripMenuItem1_Click);
            // 
            // changeToAttributeToolStripMenuItem1
            // 
            resources.ApplyResources(this.changeToAttributeToolStripMenuItem1, "changeToAttributeToolStripMenuItem1");
            this.changeToAttributeToolStripMenuItem1.Name = "changeToAttributeToolStripMenuItem1";
            this.changeToAttributeToolStripMenuItem1.Click += new System.EventHandler(this.changeToAttributeToolStripMenuItem1_Click);
            // 
            // changeToTextToolStripMenuItem1
            // 
            resources.ApplyResources(this.changeToTextToolStripMenuItem1, "changeToTextToolStripMenuItem1");
            this.changeToTextToolStripMenuItem1.Name = "changeToTextToolStripMenuItem1";
            this.changeToTextToolStripMenuItem1.Click += new System.EventHandler(this.changeToTextToolStripMenuItem1_Click);
            // 
            // changeToCDATAToolStripMenuItem1
            // 
            resources.ApplyResources(this.changeToCDATAToolStripMenuItem1, "changeToCDATAToolStripMenuItem1");
            this.changeToCDATAToolStripMenuItem1.Name = "changeToCDATAToolStripMenuItem1";
            this.changeToCDATAToolStripMenuItem1.Click += new System.EventHandler(this.changeToCDATAToolStripMenuItem1_Click);
            // 
            // changeToCommentToolStripMenuItem1
            // 
            resources.ApplyResources(this.changeToCommentToolStripMenuItem1, "changeToCommentToolStripMenuItem1");
            this.changeToCommentToolStripMenuItem1.Name = "changeToCommentToolStripMenuItem1";
            this.changeToCommentToolStripMenuItem1.Click += new System.EventHandler(this.changeToCommentToolStripMenuItem1_Click);
            // 
            // changeToProcessingInstructionToolStripMenuItem
            // 
            resources.ApplyResources(this.changeToProcessingInstructionToolStripMenuItem, "changeToProcessingInstructionToolStripMenuItem");
            this.changeToProcessingInstructionToolStripMenuItem.Name = "changeToProcessingInstructionToolStripMenuItem";
            this.changeToProcessingInstructionToolStripMenuItem.Click += new System.EventHandler(this.changeToProcessingInstructionToolStripMenuItem_Click);
            // 
            // toolStripMenuItem12
            // 
            this.toolStripMenuItem12.Name = "toolStripMenuItem12";
            resources.ApplyResources(this.toolStripMenuItem12, "toolStripMenuItem12");
            // 
            // gotoDefinitionToolStripMenuItem
            // 
            resources.ApplyResources(this.gotoDefinitionToolStripMenuItem, "gotoDefinitionToolStripMenuItem");
            this.gotoDefinitionToolStripMenuItem.Name = "gotoDefinitionToolStripMenuItem";
            this.gotoDefinitionToolStripMenuItem.Click += new System.EventHandler(this.gotoDefinitionToolStripMenuItem_Click);
            // 
            // expandXIncludesToolStripMenuItem
            // 
            resources.ApplyResources(this.expandXIncludesToolStripMenuItem, "expandXIncludesToolStripMenuItem");
            this.expandXIncludesToolStripMenuItem.Name = "expandXIncludesToolStripMenuItem";
            this.expandXIncludesToolStripMenuItem.Click += new System.EventHandler(this.expandXIncludesToolStripMenuItem_Click);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            resources.ApplyResources(this.toolStripMenuItem6, "toolStripMenuItem6");
            // 
            // nudgeToolStripMenuItem
            // 
            this.nudgeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.upToolStripMenuItem,
            this.downToolStripMenuItem,
            this.leftToolStripMenuItem,
            this.rightToolStripMenuItem});
            this.nudgeToolStripMenuItem.Name = "nudgeToolStripMenuItem";
            resources.ApplyResources(this.nudgeToolStripMenuItem, "nudgeToolStripMenuItem");
            // 
            // upToolStripMenuItem
            // 
            resources.ApplyResources(this.upToolStripMenuItem, "upToolStripMenuItem");
            this.upToolStripMenuItem.Name = "upToolStripMenuItem";
            this.upToolStripMenuItem.Click += new System.EventHandler(this.upToolStripMenuItem_Click);
            // 
            // downToolStripMenuItem
            // 
            resources.ApplyResources(this.downToolStripMenuItem, "downToolStripMenuItem");
            this.downToolStripMenuItem.Name = "downToolStripMenuItem";
            this.downToolStripMenuItem.Click += new System.EventHandler(this.downToolStripMenuItem_Click);
            // 
            // leftToolStripMenuItem
            // 
            resources.ApplyResources(this.leftToolStripMenuItem, "leftToolStripMenuItem");
            this.leftToolStripMenuItem.Name = "leftToolStripMenuItem";
            this.leftToolStripMenuItem.Click += new System.EventHandler(this.leftToolStripMenuItem_Click);
            // 
            // rightToolStripMenuItem
            // 
            resources.ApplyResources(this.rightToolStripMenuItem, "rightToolStripMenuItem");
            this.rightToolStripMenuItem.Name = "rightToolStripMenuItem";
            this.rightToolStripMenuItem.Click += new System.EventHandler(this.rightToolStripMenuItem_Click);
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            resources.ApplyResources(this.toolStripMenuItem7, "toolStripMenuItem7");
            // 
            // findToolStripMenuItem
            // 
            resources.ApplyResources(this.findToolStripMenuItem, "findToolStripMenuItem");
            this.findToolStripMenuItem.Name = "findToolStripMenuItem";
            this.findToolStripMenuItem.Click += new System.EventHandler(this.findToolStripMenuItem_Click);
            // 
            // replaceToolStripMenuItem
            // 
            resources.ApplyResources(this.replaceToolStripMenuItem, "replaceToolStripMenuItem");
            this.replaceToolStripMenuItem.Name = "replaceToolStripMenuItem";
            this.replaceToolStripMenuItem.Click += new System.EventHandler(this.replaceToolStripMenuItem_Click);
            // 
            // incrementalSearchToolStripMenuItem
            // 
            resources.ApplyResources(this.incrementalSearchToolStripMenuItem, "incrementalSearchToolStripMenuItem");
            this.incrementalSearchToolStripMenuItem.Name = "incrementalSearchToolStripMenuItem";
            this.incrementalSearchToolStripMenuItem.Click += new System.EventHandler(this.incrementalSearchToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.expandAllToolStripMenuItem,
            this.collapseAllToolStripMenuItem,
            this.toolStripMenuItem8,
            this.statusBarToolStripMenuItem,
            this.toolStripMenuItem9,
            this.sourceToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.schemasToolStripMenuItem,
            this.statsToolStripMenuItem,
            this.fileAssociationsToolStripMenuItem,
            this.toolStripMenuItem11,
            this.nextErrorToolStripMenuItem,
            this.toolStripSeparator2,
            this.compareXMLFilesToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            resources.ApplyResources(this.viewToolStripMenuItem, "viewToolStripMenuItem");
            // 
            // expandAllToolStripMenuItem
            // 
            resources.ApplyResources(this.expandAllToolStripMenuItem, "expandAllToolStripMenuItem");
            this.expandAllToolStripMenuItem.Name = "expandAllToolStripMenuItem";
            this.expandAllToolStripMenuItem.Click += new System.EventHandler(this.expandAllToolStripMenuItem_Click);
            // 
            // collapseAllToolStripMenuItem
            // 
            resources.ApplyResources(this.collapseAllToolStripMenuItem, "collapseAllToolStripMenuItem");
            this.collapseAllToolStripMenuItem.Name = "collapseAllToolStripMenuItem";
            this.collapseAllToolStripMenuItem.Click += new System.EventHandler(this.collapseAllToolStripMenuItem_Click);
            // 
            // toolStripMenuItem8
            // 
            this.toolStripMenuItem8.Name = "toolStripMenuItem8";
            resources.ApplyResources(this.toolStripMenuItem8, "toolStripMenuItem8");
            // 
            // statusBarToolStripMenuItem
            // 
            resources.ApplyResources(this.statusBarToolStripMenuItem, "statusBarToolStripMenuItem");
            this.statusBarToolStripMenuItem.Name = "statusBarToolStripMenuItem";
            this.statusBarToolStripMenuItem.Click += new System.EventHandler(this.statusBarToolStripMenuItem_Click);
            // 
            // toolStripMenuItem9
            // 
            this.toolStripMenuItem9.Name = "toolStripMenuItem9";
            resources.ApplyResources(this.toolStripMenuItem9, "toolStripMenuItem9");
            // 
            // sourceToolStripMenuItem
            // 
            resources.ApplyResources(this.sourceToolStripMenuItem, "sourceToolStripMenuItem");
            this.sourceToolStripMenuItem.Name = "sourceToolStripMenuItem";
            this.sourceToolStripMenuItem.Click += new System.EventHandler(this.sourceToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            resources.ApplyResources(this.optionsToolStripMenuItem, "optionsToolStripMenuItem");
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Click += new System.EventHandler(this.optionsToolStripMenuItem_Click);
            // 
            // schemasToolStripMenuItem
            // 
            resources.ApplyResources(this.schemasToolStripMenuItem, "schemasToolStripMenuItem");
            this.schemasToolStripMenuItem.Name = "schemasToolStripMenuItem";
            this.schemasToolStripMenuItem.Click += new System.EventHandler(this.schemasToolStripMenuItem_Click);
            // 
            // statsToolStripMenuItem
            // 
            this.statsToolStripMenuItem.Name = "statsToolStripMenuItem";
            resources.ApplyResources(this.statsToolStripMenuItem, "statsToolStripMenuItem");
            this.statsToolStripMenuItem.Click += new System.EventHandler(this.statsToolStripMenuItem_Click);
            // 
            // fileAssociationsToolStripMenuItem
            // 
            this.fileAssociationsToolStripMenuItem.Name = "fileAssociationsToolStripMenuItem";
            resources.ApplyResources(this.fileAssociationsToolStripMenuItem, "fileAssociationsToolStripMenuItem");
            this.fileAssociationsToolStripMenuItem.Click += new System.EventHandler(this.fileAssociationsToolStripMenuItem_Click);
            // 
            // toolStripMenuItem11
            // 
            this.toolStripMenuItem11.Name = "toolStripMenuItem11";
            resources.ApplyResources(this.toolStripMenuItem11, "toolStripMenuItem11");
            // 
            // nextErrorToolStripMenuItem
            // 
            resources.ApplyResources(this.nextErrorToolStripMenuItem, "nextErrorToolStripMenuItem");
            this.nextErrorToolStripMenuItem.Name = "nextErrorToolStripMenuItem";
            this.nextErrorToolStripMenuItem.Click += new System.EventHandler(this.nextErrorToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // compareXMLFilesToolStripMenuItem
            // 
            resources.ApplyResources(this.compareXMLFilesToolStripMenuItem, "compareXMLFilesToolStripMenuItem");
            this.compareXMLFilesToolStripMenuItem.Name = "compareXMLFilesToolStripMenuItem";
            this.compareXMLFilesToolStripMenuItem.Click += new System.EventHandler(this.compareXMLFilesToolStripMenuItem_Click);
            // 
            // insertToolStripMenuItem
            // 
            resources.ApplyResources(this.insertToolStripMenuItem, "insertToolStripMenuItem");
            this.insertToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.elementToolStripMenuItem,
            this.attributeToolStripMenuItem,
            this.textToolStripMenuItem,
            this.commentToolStripMenuItem,
            this.CDATAToolStripMenuItem,
            this.PIToolStripMenuItem});
            this.insertToolStripMenuItem.Name = "insertToolStripMenuItem";
            // 
            // elementToolStripMenuItem
            // 
            this.elementToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.elementBeforeToolStripMenuItem,
            this.elementAfterToolStripMenuItem,
            this.elementChildToolStripMenuItem});
            this.elementToolStripMenuItem.Name = "elementToolStripMenuItem";
            resources.ApplyResources(this.elementToolStripMenuItem, "elementToolStripMenuItem");
            // 
            // elementBeforeToolStripMenuItem
            // 
            resources.ApplyResources(this.elementBeforeToolStripMenuItem, "elementBeforeToolStripMenuItem");
            this.elementBeforeToolStripMenuItem.Name = "elementBeforeToolStripMenuItem";
            this.elementBeforeToolStripMenuItem.Click += new System.EventHandler(this.elementBeforeToolStripMenuItem_Click);
            // 
            // elementAfterToolStripMenuItem
            // 
            resources.ApplyResources(this.elementAfterToolStripMenuItem, "elementAfterToolStripMenuItem");
            this.elementAfterToolStripMenuItem.Name = "elementAfterToolStripMenuItem";
            this.elementAfterToolStripMenuItem.Click += new System.EventHandler(this.elementAfterToolStripMenuItem_Click);
            // 
            // elementChildToolStripMenuItem
            // 
            resources.ApplyResources(this.elementChildToolStripMenuItem, "elementChildToolStripMenuItem");
            this.elementChildToolStripMenuItem.Name = "elementChildToolStripMenuItem";
            this.elementChildToolStripMenuItem.Click += new System.EventHandler(this.elementChildToolStripMenuItem_Click);
            // 
            // attributeToolStripMenuItem
            // 
            this.attributeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.attributeBeforeToolStripMenuItem,
            this.attributeAfterToolStripMenuItem,
            this.attributeChildToolStripMenuItem});
            this.attributeToolStripMenuItem.Name = "attributeToolStripMenuItem";
            resources.ApplyResources(this.attributeToolStripMenuItem, "attributeToolStripMenuItem");
            // 
            // attributeBeforeToolStripMenuItem
            // 
            resources.ApplyResources(this.attributeBeforeToolStripMenuItem, "attributeBeforeToolStripMenuItem");
            this.attributeBeforeToolStripMenuItem.Name = "attributeBeforeToolStripMenuItem";
            this.attributeBeforeToolStripMenuItem.Click += new System.EventHandler(this.attributeBeforeToolStripMenuItem_Click);
            // 
            // attributeAfterToolStripMenuItem
            // 
            resources.ApplyResources(this.attributeAfterToolStripMenuItem, "attributeAfterToolStripMenuItem");
            this.attributeAfterToolStripMenuItem.Name = "attributeAfterToolStripMenuItem";
            this.attributeAfterToolStripMenuItem.Click += new System.EventHandler(this.attributeAfterToolStripMenuItem_Click);
            // 
            // attributeChildToolStripMenuItem
            // 
            resources.ApplyResources(this.attributeChildToolStripMenuItem, "attributeChildToolStripMenuItem");
            this.attributeChildToolStripMenuItem.Name = "attributeChildToolStripMenuItem";
            this.attributeChildToolStripMenuItem.Click += new System.EventHandler(this.attributeChildToolStripMenuItem_Click);
            // 
            // textToolStripMenuItem
            // 
            this.textToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.textBeforeToolStripMenuItem,
            this.textAfterToolStripMenuItem,
            this.textChildToolStripMenuItem});
            this.textToolStripMenuItem.Name = "textToolStripMenuItem";
            resources.ApplyResources(this.textToolStripMenuItem, "textToolStripMenuItem");
            // 
            // textBeforeToolStripMenuItem
            // 
            resources.ApplyResources(this.textBeforeToolStripMenuItem, "textBeforeToolStripMenuItem");
            this.textBeforeToolStripMenuItem.Name = "textBeforeToolStripMenuItem";
            this.textBeforeToolStripMenuItem.Click += new System.EventHandler(this.textBeforeToolStripMenuItem_Click);
            // 
            // textAfterToolStripMenuItem
            // 
            resources.ApplyResources(this.textAfterToolStripMenuItem, "textAfterToolStripMenuItem");
            this.textAfterToolStripMenuItem.Name = "textAfterToolStripMenuItem";
            this.textAfterToolStripMenuItem.Click += new System.EventHandler(this.textAfterToolStripMenuItem_Click);
            // 
            // textChildToolStripMenuItem
            // 
            resources.ApplyResources(this.textChildToolStripMenuItem, "textChildToolStripMenuItem");
            this.textChildToolStripMenuItem.Name = "textChildToolStripMenuItem";
            this.textChildToolStripMenuItem.Click += new System.EventHandler(this.textChildToolStripMenuItem_Click);
            // 
            // commentToolStripMenuItem
            // 
            resources.ApplyResources(this.commentToolStripMenuItem, "commentToolStripMenuItem");
            this.commentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.commentBeforeToolStripMenuItem,
            this.commentAfterToolStripMenuItem,
            this.commentChildToolStripMenuItem});
            this.commentToolStripMenuItem.Name = "commentToolStripMenuItem";
            // 
            // commentBeforeToolStripMenuItem
            // 
            resources.ApplyResources(this.commentBeforeToolStripMenuItem, "commentBeforeToolStripMenuItem");
            this.commentBeforeToolStripMenuItem.Name = "commentBeforeToolStripMenuItem";
            this.commentBeforeToolStripMenuItem.Click += new System.EventHandler(this.commentBeforeToolStripMenuItem_Click);
            // 
            // commentAfterToolStripMenuItem
            // 
            resources.ApplyResources(this.commentAfterToolStripMenuItem, "commentAfterToolStripMenuItem");
            this.commentAfterToolStripMenuItem.Name = "commentAfterToolStripMenuItem";
            this.commentAfterToolStripMenuItem.Click += new System.EventHandler(this.commentAfterToolStripMenuItem_Click);
            // 
            // commentChildToolStripMenuItem
            // 
            resources.ApplyResources(this.commentChildToolStripMenuItem, "commentChildToolStripMenuItem");
            this.commentChildToolStripMenuItem.Name = "commentChildToolStripMenuItem";
            this.commentChildToolStripMenuItem.Click += new System.EventHandler(this.commentChildToolStripMenuItem_Click);
            // 
            // CDATAToolStripMenuItem
            // 
            this.CDATAToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cdataBeforeToolStripMenuItem,
            this.cdataAfterToolStripMenuItem,
            this.cdataChildToolStripMenuItem});
            this.CDATAToolStripMenuItem.Name = "CDATAToolStripMenuItem";
            resources.ApplyResources(this.CDATAToolStripMenuItem, "CDATAToolStripMenuItem");
            // 
            // cdataBeforeToolStripMenuItem
            // 
            resources.ApplyResources(this.cdataBeforeToolStripMenuItem, "cdataBeforeToolStripMenuItem");
            this.cdataBeforeToolStripMenuItem.Name = "cdataBeforeToolStripMenuItem";
            this.cdataBeforeToolStripMenuItem.Click += new System.EventHandler(this.cdataBeforeToolStripMenuItem_Click);
            // 
            // cdataAfterToolStripMenuItem
            // 
            resources.ApplyResources(this.cdataAfterToolStripMenuItem, "cdataAfterToolStripMenuItem");
            this.cdataAfterToolStripMenuItem.Name = "cdataAfterToolStripMenuItem";
            this.cdataAfterToolStripMenuItem.Click += new System.EventHandler(this.cdataAfterToolStripMenuItem_Click);
            // 
            // cdataChildToolStripMenuItem
            // 
            resources.ApplyResources(this.cdataChildToolStripMenuItem, "cdataChildToolStripMenuItem");
            this.cdataChildToolStripMenuItem.Name = "cdataChildToolStripMenuItem";
            this.cdataChildToolStripMenuItem.Click += new System.EventHandler(this.cdataChildToolStripMenuItem_Click);
            // 
            // PIToolStripMenuItem
            // 
            this.PIToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PIBeforeToolStripMenuItem,
            this.PIAfterToolStripMenuItem,
            this.PIChildToolStripMenuItem});
            this.PIToolStripMenuItem.Name = "PIToolStripMenuItem";
            resources.ApplyResources(this.PIToolStripMenuItem, "PIToolStripMenuItem");
            // 
            // PIBeforeToolStripMenuItem
            // 
            resources.ApplyResources(this.PIBeforeToolStripMenuItem, "PIBeforeToolStripMenuItem");
            this.PIBeforeToolStripMenuItem.Name = "PIBeforeToolStripMenuItem";
            this.PIBeforeToolStripMenuItem.Click += new System.EventHandler(this.PIBeforeToolStripMenuItem_Click);
            // 
            // PIAfterToolStripMenuItem
            // 
            resources.ApplyResources(this.PIAfterToolStripMenuItem, "PIAfterToolStripMenuItem");
            this.PIAfterToolStripMenuItem.Name = "PIAfterToolStripMenuItem";
            this.PIAfterToolStripMenuItem.Click += new System.EventHandler(this.PIAfterToolStripMenuItem_Click);
            // 
            // PIChildToolStripMenuItem
            // 
            resources.ApplyResources(this.PIChildToolStripMenuItem, "PIChildToolStripMenuItem");
            this.PIChildToolStripMenuItem.Name = "PIChildToolStripMenuItem";
            this.PIChildToolStripMenuItem.Click += new System.EventHandler(this.PIChildToolStripMenuItem_Click);
            // 
            // windowToolStripMenuItem
            // 
            resources.ApplyResources(this.windowToolStripMenuItem, "windowToolStripMenuItem");
            this.windowToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newWindowToolStripMenuItem});
            this.windowToolStripMenuItem.Name = "windowToolStripMenuItem";
            // 
            // newWindowToolStripMenuItem
            // 
            resources.ApplyResources(this.newWindowToolStripMenuItem, "newWindowToolStripMenuItem");
            this.newWindowToolStripMenuItem.Name = "newWindowToolStripMenuItem";
            this.newWindowToolStripMenuItem.Click += new System.EventHandler(this.newWindowToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            resources.ApplyResources(this.helpToolStripMenuItem, "helpToolStripMenuItem");
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.contentsToolStripMenuItem,
            this.indexToolStripMenuItem,
            this.sampleToolStripMenuItem,
            this.checkUpdatesToolStripMenuItem,
            this.toolStripMenuItem10,
            this.aboutXMLNotepadToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            // 
            // contentsToolStripMenuItem
            // 
            resources.ApplyResources(this.contentsToolStripMenuItem, "contentsToolStripMenuItem");
            this.contentsToolStripMenuItem.Name = "contentsToolStripMenuItem";
            this.contentsToolStripMenuItem.Click += new System.EventHandler(this.contentsToolStripMenuItem_Click);
            // 
            // indexToolStripMenuItem
            // 
            resources.ApplyResources(this.indexToolStripMenuItem, "indexToolStripMenuItem");
            this.indexToolStripMenuItem.Name = "indexToolStripMenuItem";
            this.indexToolStripMenuItem.Click += new System.EventHandler(this.indexToolStripMenuItem_Click);
            // 
            // sampleToolStripMenuItem
            // 
            this.sampleToolStripMenuItem.Name = "sampleToolStripMenuItem";
            resources.ApplyResources(this.sampleToolStripMenuItem, "sampleToolStripMenuItem");
            this.sampleToolStripMenuItem.Click += new System.EventHandler(this.sampleToolStripMenuItem_Click);
            // 
            // checkUpdatesToolStripMenuItem
            // 
            this.checkUpdatesToolStripMenuItem.Name = "checkUpdatesToolStripMenuItem";
            resources.ApplyResources(this.checkUpdatesToolStripMenuItem, "checkUpdatesToolStripMenuItem");
            this.checkUpdatesToolStripMenuItem.Click += new System.EventHandler(this.checkUpdatesToolStripMenuItem_Click);
            // 
            // toolStripMenuItem10
            // 
            this.toolStripMenuItem10.Name = "toolStripMenuItem10";
            resources.ApplyResources(this.toolStripMenuItem10, "toolStripMenuItem10");
            // 
            // aboutXMLNotepadToolStripMenuItem
            // 
            resources.ApplyResources(this.aboutXMLNotepadToolStripMenuItem, "aboutXMLNotepadToolStripMenuItem");
            this.aboutXMLNotepadToolStripMenuItem.Name = "aboutXMLNotepadToolStripMenuItem";
            this.aboutXMLNotepadToolStripMenuItem.Click += new System.EventHandler(this.aboutXMLNotepadToolStripMenuItem_Click);
            // 
            // toolStripMenuItemUpdate
            // 
            resources.ApplyResources(this.toolStripMenuItemUpdate, "toolStripMenuItemUpdate");
            this.toolStripMenuItemUpdate.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripMenuItemUpdate.BackColor = System.Drawing.Color.MediumAquamarine;
            this.toolStripMenuItemUpdate.Name = "toolStripMenuItemUpdate";
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonNew,
            this.toolStripButtonOpen,
            this.toolStripButtonSave,
            this.toolStripButtonUndo,
            this.toolStripButtonRedo,
            this.toolStripButtonCut,
            this.toolStripButtonCopy,
            this.toolStripButtonPaste,
            this.toolStripButtonDelete,
            this.toolStripSeparator4,
            this.toolStripButtonNudgeUp,
            this.toolStripButtonNudgeDown,
            this.toolStripButtonNudgeLeft,
            this.toolStripButtonNudgeRight,
            this.toolStripSeparator1});
            resources.ApplyResources(this.toolStrip1, "toolStrip1");
            this.toolStrip1.Name = "toolStrip1";
            this.helpProvider1.SetShowHelp(this.toolStrip1, ((bool)(resources.GetObject("toolStrip1.ShowHelp"))));
            // 
            // toolStripButtonNew
            // 
            resources.ApplyResources(this.toolStripButtonNew, "toolStripButtonNew");
            this.toolStripButtonNew.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNew.Name = "toolStripButtonNew";
            this.toolStripButtonNew.Click += new System.EventHandler(this.toolStripButtonNew_Click);
            // 
            // toolStripButtonOpen
            // 
            resources.ApplyResources(this.toolStripButtonOpen, "toolStripButtonOpen");
            this.toolStripButtonOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonOpen.Name = "toolStripButtonOpen";
            this.toolStripButtonOpen.Click += new System.EventHandler(this.toolStripButtonOpen_Click);
            // 
            // toolStripButtonSave
            // 
            resources.ApplyResources(this.toolStripButtonSave, "toolStripButtonSave");
            this.toolStripButtonSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSave.Name = "toolStripButtonSave";
            this.toolStripButtonSave.Click += new System.EventHandler(this.toolStripButtonSave_Click);
            // 
            // toolStripButtonUndo
            // 
            resources.ApplyResources(this.toolStripButtonUndo, "toolStripButtonUndo");
            this.toolStripButtonUndo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonUndo.Name = "toolStripButtonUndo";
            this.toolStripButtonUndo.Click += new System.EventHandler(this.toolStripButtonUndo_Click);
            // 
            // toolStripButtonRedo
            // 
            resources.ApplyResources(this.toolStripButtonRedo, "toolStripButtonRedo");
            this.toolStripButtonRedo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRedo.Name = "toolStripButtonRedo";
            this.toolStripButtonRedo.Click += new System.EventHandler(this.toolStripButtonRedo_Click);
            // 
            // toolStripButtonCut
            // 
            resources.ApplyResources(this.toolStripButtonCut, "toolStripButtonCut");
            this.toolStripButtonCut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonCut.Name = "toolStripButtonCut";
            this.toolStripButtonCut.Click += new System.EventHandler(this.toolStripButtonCut_Click);
            // 
            // toolStripButtonCopy
            // 
            resources.ApplyResources(this.toolStripButtonCopy, "toolStripButtonCopy");
            this.toolStripButtonCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonCopy.Name = "toolStripButtonCopy";
            this.toolStripButtonCopy.Click += new System.EventHandler(this.toolStripButtonCopy_Click);
            // 
            // toolStripButtonPaste
            // 
            resources.ApplyResources(this.toolStripButtonPaste, "toolStripButtonPaste");
            this.toolStripButtonPaste.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonPaste.Name = "toolStripButtonPaste";
            this.toolStripButtonPaste.Click += new System.EventHandler(this.toolStripButtonPaste_Click);
            // 
            // toolStripButtonDelete
            // 
            resources.ApplyResources(this.toolStripButtonDelete, "toolStripButtonDelete");
            this.toolStripButtonDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonDelete.Name = "toolStripButtonDelete";
            this.toolStripButtonDelete.Click += new System.EventHandler(this.toolStripButtonDelete_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
            // 
            // toolStripButtonNudgeUp
            // 
            resources.ApplyResources(this.toolStripButtonNudgeUp, "toolStripButtonNudgeUp");
            this.toolStripButtonNudgeUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNudgeUp.Name = "toolStripButtonNudgeUp";
            this.toolStripButtonNudgeUp.Click += new System.EventHandler(this.toolStripButtonNudgeUp_Click);
            // 
            // toolStripButtonNudgeDown
            // 
            resources.ApplyResources(this.toolStripButtonNudgeDown, "toolStripButtonNudgeDown");
            this.toolStripButtonNudgeDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNudgeDown.Name = "toolStripButtonNudgeDown";
            this.toolStripButtonNudgeDown.Click += new System.EventHandler(this.toolStripButtonNudgeDown_Click);
            // 
            // toolStripButtonNudgeLeft
            // 
            resources.ApplyResources(this.toolStripButtonNudgeLeft, "toolStripButtonNudgeLeft");
            this.toolStripButtonNudgeLeft.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNudgeLeft.Name = "toolStripButtonNudgeLeft";
            this.toolStripButtonNudgeLeft.Click += new System.EventHandler(this.toolStripButtonNudgeLeft_Click);
            // 
            // toolStripButtonNudgeRight
            // 
            resources.ApplyResources(this.toolStripButtonNudgeRight, "toolStripButtonNudgeRight");
            this.toolStripButtonNudgeRight.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNudgeRight.Name = "toolStripButtonNudgeRight";
            this.toolStripButtonNudgeRight.Click += new System.EventHandler(this.toolStripButtonNudgeRight_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // comboBoxLocation
            // 
            resources.ApplyResources(this.comboBoxLocation, "comboBoxLocation");
            this.comboBoxLocation.FormattingEnabled = true;
            this.comboBoxLocation.Name = "comboBoxLocation";
            this.helpProvider1.SetShowHelp(this.comboBoxLocation, ((bool)(resources.GetObject("comboBoxLocation.ShowHelp"))));
            // 
            // tabControlViews
            // 
            resources.ApplyResources(this.tabControlViews, "tabControlViews");
            this.tabControlViews.Controls.Add(this.tabPageTreeView);
            this.tabControlViews.Controls.Add(this.tabPageHtmlView);
            this.tabControlViews.Name = "tabControlViews";
            this.tabControlViews.SelectedIndex = 0;
            this.tabControlViews.SelectedTab = this.tabPageTreeView;
            this.helpProvider1.SetShowHelp(this.tabControlViews, ((bool)(resources.GetObject("tabControlViews.ShowHelp"))));
            this.tabControlViews.Selected += new XmlNotepad.NoBorderTabControlEventHandler(this.TabControlViews_Selected);
            // 
            // tabPageTreeView
            // 
            resources.ApplyResources(this.tabPageTreeView, "tabPageTreeView");
            this.tabPageTreeView.Controls.Add(this.xmlTreeView1);
            this.tabPageTreeView.Name = "tabPageTreeView";
            this.helpProvider1.SetShowHelp(this.tabPageTreeView, ((bool)(resources.GetObject("tabPageTreeView.ShowHelp"))));
            // 
            // xmlTreeView1
            // 
            resources.ApplyResources(this.xmlTreeView1, "xmlTreeView1");
            this.xmlTreeView1.BackColor = System.Drawing.SystemColors.Window;
            this.xmlTreeView1.Name = "xmlTreeView1";
            this.xmlTreeView1.ResizerPosition = 200;
            this.xmlTreeView1.ScrollPosition = new System.Drawing.Point(0, 0);
            this.xmlTreeView1.SelectedNode = null;
            this.helpProvider1.SetShowHelp(this.xmlTreeView1, ((bool)(resources.GetObject("xmlTreeView1.ShowHelp"))));
            // 
            // tabPageHtmlView
            // 
            resources.ApplyResources(this.tabPageHtmlView, "tabPageHtmlView");
            this.tabPageHtmlView.Controls.Add(this.xsltViewer);
            this.tabPageHtmlView.Name = "tabPageHtmlView";
            this.helpProvider1.SetShowHelp(this.tabPageHtmlView, ((bool)(resources.GetObject("tabPageHtmlView.ShowHelp"))));
            // 
            // xsltViewer
            // 
            resources.ApplyResources(this.xsltViewer, "xsltViewer");
            this.xsltViewer.Name = "xsltViewer";
            this.helpProvider1.SetShowHelp(this.xsltViewer, ((bool)(resources.GetObject("xsltViewer.ShowHelp"))));
            // 
            // resizer
            // 
            resources.ApplyResources(this.resizer, "resizer");
            this.resizer.Border3DStyle = System.Windows.Forms.Border3DStyle.Raised;
            this.resizer.Name = "resizer";
            this.resizer.Pane1 = null;
            this.resizer.Pane2 = null;
            this.resizer.PaneWidth = 5;
            this.helpProvider1.SetShowHelp(this.resizer, ((bool)(resources.GetObject("resizer.ShowHelp"))));
            this.resizer.Vertical = false;
            // 
            // tabPageTaskList
            // 
            resources.ApplyResources(this.tabPageTaskList, "tabPageTaskList");
            this.tabPageTaskList.Name = "tabPageTaskList";
            this.helpProvider1.SetShowHelp(this.tabPageTaskList, ((bool)(resources.GetObject("tabPageTaskList.ShowHelp"))));
            // 
            // tabPageDynamicHelp
            // 
            resources.ApplyResources(this.tabPageDynamicHelp, "tabPageDynamicHelp");
            this.tabPageDynamicHelp.Name = "tabPageDynamicHelp";
            this.helpProvider1.SetShowHelp(this.tabPageDynamicHelp, ((bool)(resources.GetObject("tabPageDynamicHelp.ShowHelp"))));
            // 
            // taskList
            // 
            resources.ApplyResources(this.taskList, "taskList");
            this.taskList.Name = "taskList";
            this.helpProvider1.SetShowHelp(this.taskList, ((bool)(resources.GetObject("taskList.ShowHelp"))));
            // 
            // dynamicHelpViewer
            // 
            resources.ApplyResources(this.dynamicHelpViewer, "dynamicHelpViewer");
            this.dynamicHelpViewer.BaseUri = null;
            this.dynamicHelpViewer.DefaultStylesheetResource = "XmlNotepad.DefaultSS.xslt";
            this.dynamicHelpViewer.DisableOutputFile = true;
            this.dynamicHelpViewer.IgnoreDTD = false;
            this.dynamicHelpViewer.Name = "dynamicHelpViewer";
            this.helpProvider1.SetShowHelp(this.dynamicHelpViewer, ((bool)(resources.GetObject("dynamicHelpViewer.ShowHelp"))));
            // 
            // statusStrip1
            // 
            resources.ApplyResources(this.statusStrip1, "statusStrip1");
            this.statusStrip1.Name = "statusStrip1";
            // 
            // statusStrip2
            // 
            this.statusStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            resources.ApplyResources(this.statusStrip2, "statusStrip2");
            this.statusStrip2.Name = "statusStrip2";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            resources.ApplyResources(this.toolStripStatusLabel1, "toolStripStatusLabel1");
            // 
            // FormMain
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.statusStrip2);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.comboBoxLocation);
            this.Controls.Add(this.tabControlViews);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.resizer);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FormMain";
            this.helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
            this.contextMenu1.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tabControlViews.ResumeLayout(false);
            this.tabPageTreeView.ResumeLayout(false);
            this.tabPageHtmlView.ResumeLayout(false);
            this.statusStrip2.ResumeLayout(false);
            this.statusStrip2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        const string HideUpdateButtonAction = "HideUpdateButton";

        private void checkUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.delayedActions.CancelDelayedAction(HideUpdateButtonAction);
            this.updater.CheckNow();
        }

        protected virtual void TabControlViews_Selected(object sender, NoBorderTabControlEventArgs e) {
            if (e.TabPage == this.tabPageHtmlView) {
                this.DisplayXsltResults();
            }
        }

        #endregion

        void EnableFileMenu(){
            bool hasFile = (model.FileName != null);
            this.toolStripButtonSave.Enabled = this.saveToolStripMenuItem.Enabled = true;
            this.reloadToolStripMenuItem.Enabled = hasFile;
            this.saveAsToolStripMenuItem.Enabled = true;
        }

        public virtual void DisplayXsltResults() {
            this.xsltViewer.DisplayXsltResults();
            this.analytics.RecordXsltView();
        }

        void SelectTreeView() {
            if (tabControlViews.SelectedIndex != 0) {
                tabControlViews.SelectedIndex = 0;
            }
            if (!xmlTreeView1.ContainsFocus) {
                xmlTreeView1.Focus();
            }
        }

        public virtual void New(){
            SelectTreeView();
            if (!SaveIfDirty(true))
                return;  
            model.Clear();
            includesExpanded = false;
            EnableFileMenu();
            this.settings["FileName"] = new Uri("/", UriKind.RelativeOrAbsolute);
            UpdateMenuState();
        }

        protected virtual IIntellisenseProvider CreateIntellisenseProvider(XmlCache model, ISite site) {
            return new XmlIntellisenseProvider(this.model, site);
        }

        protected override object GetService(Type service) {
            if (service == typeof(UndoManager)){
                return this.undoManager;
            } else if (service == typeof(SchemaCache)) {
                return this.model.SchemaCache;
            } else if (service == typeof(TreeView)) {
                XmlTreeView view = (XmlTreeView)GetService(typeof(XmlTreeView));
                return view.TreeView;
            } else if (service == typeof(XmlTreeView)) {
                if (this.xmlTreeView1 == null) {
                    this.xmlTreeView1 = this.CreateTreeView();
                }
                return this.xmlTreeView1;
            } else if (service == typeof(XmlCache)) {
                if (null == this.model)
                {
                    this.model = new XmlCache((IServiceProvider)this, (ISynchronizeInvoke)this);
                }
                return this.model;
            } else if (service == typeof(Settings)){
                return this.settings;
            } else if (service == typeof(IIntellisenseProvider)) {
                if (this.ip == null) this.ip = CreateIntellisenseProvider(this.model, this);
                return this.ip;
            } else if (service == typeof(HelpProvider)) {
                return this.helpProvider1;
            } else if (service == typeof(WebProxyService)) {
                if (this.proxyService == null)
                    this.proxyService = new WebProxyService((IServiceProvider)this);
                return this.proxyService;
            } else if (service == typeof(UserSettings)) {
                return new UserSettings(this.settings);
            }
            return base.GetService (service);
        }

        public OpenFileDialog OpenFileDialog {
            get { return this.od; }
        }

        public virtual void OpenDialog(string dir = null) {
            SelectTreeView();
            if (!SaveIfDirty(true))
                return;
            if (od == null) od = new OpenFileDialog();
            if (model.FileName != null) {
                Uri uri = new Uri(model.FileName);
                if (uri.Scheme == "file"){
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
            for (int i = 1, n = parts.Length; i < n; i += 2 ) {
                if (parts[i] == "*.*") {
                    index = (i / 2)+1;
                    break;
                }
            }
            od.FilterIndex = index;
            if (od.ShowDialog(this) == DialogResult.OK){
                Open(od.FileName);
            }
        }

        public virtual void ShowStatus(string msg) {
            this.toolStripStatusLabel1.Text = msg;
            this.delayedActions.StartDelayedAction("ClearStatus", ClearStatus, TimeSpan.FromSeconds(20));
        }

        private void ClearStatus()
        {
            this.toolStripStatusLabel1.Text = "";
        }

        public virtual void Open(string filename) {
            try {
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
            } catch (Exception e){
                if (MessageBox.Show(this,
                    string.Format(SR.LoadErrorPrompt, filename, e.Message),
                    SR.LoadErrorCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes) {
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

        private void InternalOpen(string filename) {
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

        bool CheckXIncludes() {
            if (includesExpanded) {
                if (MessageBox.Show(this, SR.SaveExpandedIncludesPrompt, SR.SaveExpandedIncludesCaption,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No) {
                    return false;
                }
                includesExpanded = false;
            }
            return true;
        }

        public virtual bool SaveIfDirty(bool prompt) {
            if (model.Dirty){
                if (prompt){
                    SelectTreeView();
                    DialogResult rc = MessageBox.Show(this, SR.SaveChangesPrompt,
                        SR.SaveChangesCaption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);                
                    if (rc == DialogResult.Cancel){
                        return false;
                    } else if (rc == DialogResult.Yes){
                        return Save();
                    }
                } else {
                    return Save();
                }
            }
            return true;
        }

        public virtual bool Save() {
            this.xmlTreeView1.Commit();
            if (!CheckXIncludes()) return false;                
            string fname = model.FileName;
            if (fname == null){
                SaveAs();
            } else {
                try
                {
                    this.xmlTreeView1.BeginSave();
                    if (CheckReadOnly(fname)) {
                        model.Save();
                        ShowStatus(SR.SavedStatus);
                    }
                } catch (Exception e){
                    MessageBox.Show(this, e.Message, SR.SaveErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    this.xmlTreeView1.EndSave();
                }
            }
            return true;
        }

        public bool CheckReadOnly(string fname) {
            if (model.IsReadOnly(fname)) {
                SelectTreeView();
                if (MessageBox.Show(this, string.Format(SR.ReadOnly, fname),
                    SR.ReadOnlyCaption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation) == DialogResult.Yes) {
                    model.MakeReadWrite(fname);
                    return true;
                } else {
                    return false;
                }
            }
            return true;    
        }

        public virtual void Save(string newName) {
            this.xmlTreeView1.Commit();
            this.xmlTreeView1.BeginSave();
            try {
                bool hasFile = (model.FileName != null);
                if (!hasFile && string.IsNullOrEmpty(newName)) {
                    SaveAs();
                }
                if (CheckReadOnly(newName)) {
                    model.Save(newName);
                    UpdateCaption();
                    ShowStatus(SR.SavedStatus);
                    this.settings["FileName"] = model.Location;
                    EnableFileMenu();
                    this.recentFiles.AddRecentFile(model.Location);
                }
            } catch (Exception e){
                MessageBox.Show(this, e.Message, SR.SaveErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);                
            }
            finally
            {
                this.xmlTreeView1.EndSave();
            }
        }

        public virtual void SaveAs() {
            SelectTreeView();
            SaveFileDialog sd = new SaveFileDialog();
            if (model.IsFile) sd.FileName = model.FileName;
            sd.Filter = SR.SaveAsFilter;
            if (sd.ShowDialog(this) == DialogResult.OK){
                string fname = sd.FileName;
                if (CheckReadOnly(fname)) {
                    Save(fname);
                }
            }
        }

        string caption = null;

        public string Caption {
            get {
                if (string.IsNullOrEmpty(caption))
                    caption = SR.MainFormTitle;
                return caption; }
            set { caption = value; }
        }

        public virtual void UpdateCaption() {
            string caption = this.Caption + " - " + model.FileName;
            if (this.model.Dirty){
                caption += "*";
            }            
            this.Text = caption;
            sourceToolStripMenuItem.Enabled = this.model.FileName != null;
        }

        void OnFileChanged(object sender, EventArgs e) {
            if (!prompting) OnFileChanged();
        }

        bool prompting = false;

        protected virtual void OnFileChanged() {
            prompting = true;
            try {
                if (this.WindowState == FormWindowState.Minimized) {
                    this.WindowState = FormWindowState.Normal;
                }
                SelectTreeView();
                if (MessageBox.Show(this, SR.FileChagedOnDiskPrompt, SR.FileChagedOnDiskCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes) {
                    string location = this.model.Location.LocalPath;
                    this.model.Clear();
                    this.Open(location);                                                     
                }
            } finally {
                prompting = false;
            }
        }

        private void undoManager_StateChanged(object sender, EventArgs e) {
            this.undoToolStripMenuItem.Enabled = toolStripButtonUndo.Enabled = this.undoManager.CanUndo;
            this.redoToolStripMenuItem.Enabled = toolStripButtonRedo.Enabled = this.undoManager.CanRedo;
            Command cmd = this.undoManager.Peek();
            this.undoToolStripMenuItem.Text = this.undoLabel + " " + (cmd == null ? "" : cmd.Name);
            cmd = this.undoManager.Current;
            this.redoToolStripMenuItem.Text = this.redoLabel + " " + (cmd == null ? "" : cmd.Name);
        }

        public virtual string ConfigFile {
            get { 
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

        public virtual void LoadConfig() {
            string path = null;
            this.loading = true;
            if (this.args != null && this.args.Length > 0) {
                // When user passes arguments we skip the config file
                // This is for unit testing where we need consistent config!
                path = this.args[0];
                this.settings.FileName = this.ConfigFile;
            } else {
                // allow user to have a local settings file (xcopy deployable).
                path = this.LocalConfigFile;
                if (!File.Exists(path))
                {
                    path = this.ConfigFile;
                }

                if (File.Exists(path)) {
                    settings.Load(path);

                    UserSettings.AddDefaultColors(settings, "LightColors", ColorTheme.Light);
                    UserSettings.AddDefaultColors(settings, "DarkColors", ColorTheme.Dark);

                    string newLines = (string)this.settings["NewLineChars"];

                    Uri location = (Uri)this.settings["FileName"];
                    // Load up the last file we were editing before - if it is local and still exists.
                    if (location != null && location.OriginalString != "/" && location.IsFile && File.Exists(location.LocalPath)) {
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
            this.loading = false;

            CheckAnalytics();
        }

        private void CheckAnalytics()
        {
            if ((string)this.Settings["AnalyticsClientId"] == "")
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

            analytics = new Analytics((string)this.Settings["AnalyticsClientId"], (bool)this.Settings["AllowAnalytics"]);
            analytics.RecordAppLaunched();
        }

        public virtual void SaveConfig() {
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
        IComponent ISite.Component{
            get { return this; }
        }

        public static Type ResourceType { get { return typeof(SR); } }

        string ISite.Name {
            get { return this.Name; }
            set { this.Name = value; } 
        }

        IContainer ISite.Container {
            get { return this.Container; }
        }

        bool ISite.DesignMode {
            get { return this.DesignMode;}
        }
        object IServiceProvider.GetService(Type serviceType) {
            return this.GetService(serviceType);
        }
        #endregion

        void OnModelChanged(object sender, ModelChangedEventArgs e) {
            if (e.ModelChangeType == ModelChangeType.Reloaded) {
                this.undoManager.Clear();
                this.taskList.Clear();
            }
            if (e.ModelChangeType == ModelChangeType.BeginBatchUpdate) {
                batch++;
            } else if (e.ModelChangeType == ModelChangeType.EndBatchUpdate) {
                batch--;
            }
            if (batch == 0) OnModelChanged();
        }

        protected virtual void OnModelChanged() {
            TaskHandler handler = new TaskHandler(this.taskList);
            handler.Start();
            this.model.ValidateModel(handler);
            handler.Finish();
            UpdateCaption();
        }

        private void settings_Changed(object sender, string name) {
            // Make sure it's on the right thread...
            ISynchronizeInvoke si = (ISynchronizeInvoke)this;
            if (si.InvokeRequired) {
                si.BeginInvoke(new SettingsEventHandler(OnSettingsChanged),
                    new object[] { sender, name });
            } else {
                OnSettingsChanged(sender, name);
            }
        }

        protected virtual void OnSettingsChanged(object sender, string name) {        
            switch (name){
                case "File":
                    this.settings.Reload(); // just do it!!                    
                    break;
                case "WindowBounds":
                    if (loading) { // only if loading first time!
                        Rectangle r = (Rectangle)this.settings["WindowBounds"];
                        if (!r.IsEmpty) {
                            Screen s = Screen.FromRectangle(r);
                            if (s.Bounds.Contains(r)) {
                                this.Bounds = r;
                                this.StartPosition = FormStartPosition.Manual;
                            }
                        }
                    }
                    break;
                case "TreeViewSize":
                    int pos = (int)this.settings["TreeViewSize"];
                    if (pos != 0) {
                        this.xmlTreeView1.ResizerPosition = pos;
                    }
                    break;
                case "TaskListSize":
                    int height = (int)this.settings["TaskListSize"];
                    if (height != 0) {
                        this.tabControlLists.Height = height;
                    } 
                    break;
                case "Font":
                    this.Font = (Font)this.settings["Font"];
                    break;
                case "RecentFiles":
                    Uri[] files = (Uri[])this.settings["RecentFiles"];
                    if (files != null) {
                        this.recentFiles.SetFiles(files);
                    }
                    break;
            }
        }

        public void SaveErrors(string filename) {
            this.taskList.Save(filename);
        }

        void OnRecentFileSelected(object sender, RecentFileEventArgs e) {
            if (!this.SaveIfDirty(true))
                return;                                       
            string fileName = e.FileName.OriginalString;
            Open(fileName);
        }

        private void treeView1_SelectionChanged(object sender, EventArgs e) {
            UpdateMenuState();
            DisplayHelp();
        }

        private void DisplayHelp() {
            // display documentation
            if (null == xmlTreeView1.SelectedNode) {
                this.dynamicHelpViewer.DisplayXsltResults(new XmlDocument(), null);
                return;
            }
            XmlDocument xmlDoc = xmlTreeView1.SelectedNode.GetDocumentation();
            if (this.dynamicHelpViewer.Visible) {
                helpAvailableHint = false;
                if (null == xmlDoc) {
                    xmlDoc = new XmlDocument();
                    if (taskList.Count > 0) {
                        xmlDoc.AppendChild(xmlDoc.CreateElement("errors"));
                    } else {
                        xmlDoc.AppendChild(xmlDoc.CreateElement("nothing"));
                    }
                }
                this.dynamicHelpViewer.DisplayXsltResults(xmlDoc, null);
            } else if (helpAvailableHint && xmlDoc != null) {
                helpAvailableHint = false;
                ShowStatus(SR.DynamicHelpAvailable);
            }
        }

        private void treeView1_NodeChanged(object sender, NodeChangeEventArgs e) {
            UpdateMenuState();
        }

        protected virtual void UpdateMenuState() {

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

        void EnableNodeItems(XmlNodeType nt, ToolStripMenuItem c1, ToolStripMenuItem m1, ToolStripMenuItem c2, ToolStripMenuItem m2, ToolStripMenuItem c3, ToolStripMenuItem m3){
            c1.Enabled = m1.Enabled = this.xmlTreeView1.CanInsertNode(InsertPosition.Before, nt);
            c2.Enabled = m2.Enabled = this.xmlTreeView1.CanInsertNode(InsertPosition.After, nt);
            c3.Enabled = m3.Enabled = this.xmlTreeView1.CanInsertNode(InsertPosition.Child, nt);
        }

        protected virtual void OpenNotepad(string path) {
            if (this.SaveIfDirty(true)){
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
                if (File.Exists(notepad)) {
                    ProcessStartInfo pi = new ProcessStartInfo(notepad, "\"" + path + "\"");
                    Process.Start(pi);
                }
            }
        }

		void treeView1_ClipboardChanged(object sender, EventArgs e) {
			CheckClipboard();
		}

		void CheckClipboard() {
            this.toolStripButtonPaste.Enabled = this.pasteToolStripMenuItem.Enabled = this.ctxMenuItemPaste.Enabled = TreeData.HasData;
		}


		protected override void OnActivated(EventArgs e) {
			CheckClipboard();
            if (firstActivate) {
                this.comboBoxLocation.Focus();
                firstActivate = false;
            }
            if (this.xmlTreeView1.TreeView.IsEditing) {
                this.xmlTreeView1.TreeView.Focus();
            } else if (this.xmlTreeView1.NodeTextView.IsEditing) {
                this.xmlTreeView1.NodeTextView.Focus();
            }
		}

        void taskList_Navigate(object sender, Task task) {
            XmlNode node = task.Data as XmlNode;
            if (node != null) {
                XmlTreeNode tn = this.xmlTreeView1.FindNode(node);
                if (tn != null) {
                    this.xmlTreeView1.SelectedNode = tn;
                    this.SelectTreeView();
                }
            }
        }

        private void Form1_DragOver(object sender, DragEventArgs e) {
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


        private void Form1_DragDrop(object sender, DragEventArgs e) {
            IDataObject data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop)){
                Array a = data.GetData(DataFormats.FileDrop) as Array;
                if (a != null){
                    if (a.Length>0 && a.GetValue(0) is string){
                        string filename = (string)a.GetValue(0);
                        if (!this.SaveIfDirty(true))
                            return;
                        this.Open(filename);
                    }
                }
            } else if (data.GetDataPresent(this.urlFormat.Name)){
                Stream stm = data.GetData(this.urlFormat.Name) as Stream;
                if (stm != null) {
                    try {
                        // Note: for some reason sr.ReadToEnd doesn't work right.
                        StreamReader sr = new StreamReader(stm, Encoding.Unicode);
                        StringBuilder sb = new StringBuilder();
                        while (true) {
                            int i = sr.Read();
                            if (i != 0) {
                                sb.Append(Convert.ToChar(i));
                            } else {
                                break;
                            }
                        }
                        string url = sb.ToString();
                        if (!this.SaveIfDirty(true))
                            return;
                        this.Open(url);
                    } catch (Exception){
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

        private void newToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.CancelEdit();
            New();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.CancelEdit();
            OpenDialog();
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView(); 
            if (model.Dirty) {                
                if (MessageBox.Show(this, SR.DiscardChanges, SR.DiscardChangesCaption,
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel) {
                    return;
                }                    
            }
            Open(this.model.FileName);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.Commit();
            Save();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.Commit();
            SaveAs();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.Commit();
            this.Close();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e) {
            try {
                this.xmlTreeView1.CancelEdit();
                this.undoManager.Undo();
                SelectTreeView();
                UpdateMenuState();
            } catch (Exception ex) {
                MessageBox.Show(this, ex.Message, SR.UndoError, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e) {
            try {
                if (this.xmlTreeView1.Commit())
                    this.undoManager.Redo();
                SelectTreeView();
                UpdateMenuState();
            } catch (Exception ex) {
                MessageBox.Show(this, ex.Message, SR.RedoError, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit()) 
                this.xmlTreeView1.Cut();
            UpdateMenuState();
            SelectTreeView();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.Commit();
            this.xmlTreeView1.Copy();
            SelectTreeView();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.Paste(InsertPosition.Child);
            UpdateMenuState();
            SelectTreeView();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e) {
            DeleteSelectedNode();
        }

        void DeleteSelectedNode() {
            this.xmlTreeView1.Commit();
            this.xmlTreeView1.Delete();
            UpdateMenuState();
            SelectTreeView();
        }

        private void repeatToolStripMenuItem_Click(object sender, EventArgs e) {
            this.RepeatSelectedNode();
        }

        void RepeatSelectedNode() {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.Insert();
            UpdateMenuState();
            SelectTreeView();
        }

        private void duplicateToolStripMenuItem_Click(object sender, EventArgs e) {
            DuplicateSelectedNode();
        }

        void DuplicateSelectedNode() {
            try {
                if (this.xmlTreeView1.Commit())
                    this.xmlTreeView1.Duplicate();
                UpdateMenuState();
                SelectTreeView();
            } catch (Exception ex) {
                MessageBox.Show(this, ex.Message, SR.DuplicateErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }            
        }

        private void upToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())                    
                this.xmlTreeView1.NudgeNode(this.xmlTreeView1.SelectedNode, NudgeDirection.Up);
            SelectTreeView();
        }

        private void downToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.NudgeNode(this.xmlTreeView1.SelectedNode, NudgeDirection.Down);
            SelectTreeView();
        }

        private void leftToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.NudgeNode(this.xmlTreeView1.SelectedNode, NudgeDirection.Left);
            SelectTreeView();
        }

        private void rightToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.NudgeNode(this.xmlTreeView1.SelectedNode, NudgeDirection.Right);
            SelectTreeView();
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e) {            
            Search(false);
        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e) {
            Search(true);
        }

        void Search(bool replace) {
            if (this.tabControlViews.SelectedTab == this.tabPageHtmlView) {
                // TBD...
                return;
            }

            if (search == null || !search.Visible) {
                search = new FormSearch(search, (ISite)this);
                search.Owner = this;
                this.analytics.RecordFormSearch();
            } else {
                search.Activate();
            }
            search.Target = new XmlTreeViewFindTarget(this.xmlTreeView1);
            search.ReplaceMode = replace;

            if (!search.Visible) {
                search.Show(this); // modeless
            }
        }

        private void expandToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            XmlTreeNode s = this.xmlTreeView1.SelectedNode;
            if (s != null) {
                s.ExpandAll();
            }
        }

        private void collapseToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            XmlTreeNode s = this.xmlTreeView1.SelectedNode;
            if (s != null) {
                s.CollapseAll();
            }
        }

        private void expandAllToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            this.xmlTreeView1.ExpandAll();
        }

        private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            this.xmlTreeView1.CollapseAll();
        }

        private void statusBarToolStripMenuItem_Click(object sender, EventArgs e) {
            bool visible = !statusBarToolStripMenuItem.Checked;
            statusBarToolStripMenuItem.Checked = visible;
            int h = this.ClientSize.Height - this.toolStrip1.Bottom - 2;
            if (visible) {
                h -= this.statusStrip1.Height;
            }
            this.tabControlViews.Height = h;
            this.statusStrip1.Visible = visible;
            this.PerformLayout();
        }

        private void sourceToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.tabControlViews.SelectedTab == this.tabPageHtmlView) {
                // TBD
            } else {
                OpenNotepad(this.model.FileName);
            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e) {
            string oldLocation = (string)settings["UpdateLocation"];
            FormOptions options = new FormOptions();
            options.Owner = this;
            options.Site = this;
            if (options.ShowDialog(this) == DialogResult.OK) {
                this.updater.OnUserChange(oldLocation);
            }
            analytics.RecordFormOptions();
        }


        private void contentsToolStripMenuItem_Click(object sender, EventArgs e) {
            Help.ShowHelp(this, this.helpProvider1.HelpNamespace, HelpNavigator.TableOfContents);
        }

        private void indexToolStripMenuItem_Click(object sender, EventArgs e) {
            Help.ShowHelp(this, this.helpProvider1.HelpNamespace, HelpNavigator.Index);
        }

        private void aboutXMLNotepadToolStripMenuItem_Click(object sender, EventArgs e) {
            FormAbout frm = new FormAbout();
            frm.ShowDialog(this);
        }

        private void toolStripButtonNew_Click(object sender, EventArgs e) {
            this.xmlTreeView1.CancelEdit();
            this.New();
        }

        private void toolStripButtonOpen_Click(object sender, EventArgs e) {
            this.xmlTreeView1.CancelEdit();
            this.OpenDialog();
        }

        private void toolStripButtonSave_Click(object sender, EventArgs e) {
            this.xmlTreeView1.Commit();
            this.Save();
        }

        private void toolStripButtonUndo_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.undoManager.Undo();
            UpdateMenuState();
        }

        private void toolStripButtonRedo_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.undoManager.Redo();
            UpdateMenuState();
        }

        private void toolStripButtonCut_Click(object sender, EventArgs e) {
            SelectTreeView();
            this.xmlTreeView1.Cut();
            UpdateMenuState();
        }

        private void toolStripButtonCopy_Click(object sender, EventArgs e) {
            SelectTreeView();
            this.xmlTreeView1.Copy();
        }

        private void toolStripButtonPaste_Click(object sender, EventArgs e) {
            SelectTreeView();
            this.xmlTreeView1.Paste(InsertPosition.Child);
            UpdateMenuState();
        }

        private void toolStripButtonDelete_Click(object sender, EventArgs e) {
            SelectTreeView();
            this.xmlTreeView1.CancelEdit();
            this.xmlTreeView1.Delete();
            UpdateMenuState();
        }

        private void toolStripButtonNudgeUp_Click(object sender, EventArgs e) {
            this.upToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonNudgeDown_Click(object sender, EventArgs e) {
            this.downToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonNudgeLeft_Click(object sender, EventArgs e) {
            this.leftToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonNudgeRight_Click(object sender, EventArgs e) {
            this.rightToolStripMenuItem_Click(sender, e);
        }

        // Insert Menu Items.

        private void elementAfterToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.Element);
        }

        private void elementBeforeToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.Element);
        }

        private void elementChildToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.Element);
        }

        private void attributeBeforeToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.Attribute);
        }

        private void attributeAfterToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.Attribute);
        }

        private void attributeChildToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.Attribute);
        }

        private void textBeforeToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.Text);
        }

        private void textAfterToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.Text);
        }

        private void textChildToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.Text);
        }

        private void commentBeforeToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.Comment);
        }

        private void commentAfterToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.Comment);
        }

        private void commentChildToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.Comment);
        }

        private void cdataBeforeToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.CDATA);
        }

        private void cdataAfterToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.CDATA);
        }

        private void cdataChildToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.CDATA);
        }

        private void PIBeforeToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.ProcessingInstruction);
        }

        private void PIAfterToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.ProcessingInstruction);
        }

        private void PIChildToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.ProcessingInstruction);
        }

        void Launch(string exeFileName, string args) {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = exeFileName;
            info.Arguments = "/offset " + args;
            Process p = new Process();
            p.StartInfo = info;
            if (!p.Start()) {
                MessageBox.Show(this, string.Format(SR.ErrorCreatingProcessPrompt, exeFileName), SR.LaunchErrorPrompt, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }            
        }

        private void newWindowToolStripMenuItem_Click(object sender, EventArgs e){
            this.SaveIfDirty(true);
            this.OpenNewWindow(this.model.FileName);
        }


        private void schemasToolStripMenuItem_Click(object sender, EventArgs e) {
            FormSchemas frm = new FormSchemas();
            frm.Owner = this;
            frm.Site = this;
            if (frm.ShowDialog(this) == DialogResult.OK) {
                OnModelChanged();
            }
            this.analytics.RecordFormSchemas();
        }

        private void nextErrorToolStripMenuItem_Click(object sender, EventArgs e) {
            this.taskList.NavigateNextError();
        }

        private void compareXMLFilesToolStripMenuItem_Click(object sender, EventArgs e) {
            if (string.IsNullOrEmpty(this.model.FileName))
            {
                MessageBox.Show(this, SR.XmlDiffEmptyPrompt, SR.XmlDiffErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SelectTreeView();
            OpenFileDialog ofd = new OpenFileDialog();
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

        string GetEmbeddedString(string name)
        {
            using (Stream stream = typeof(XmlNotepad.FormMain).Assembly.GetManifestResourceStream(name))
            {
                StreamReader sr = new StreamReader(stream);
                return sr.ReadToEnd();
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
            TextWriter resultHtml) {

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

        void CleanupTempFiles() {
            try {
                this.tempFiles.Delete();
            } catch {
            }
        }

        private void DoCompare(string changed) {
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
            using (XmlReader reader = XmlReader.Create(changed, settings)) {
                doc.Load(reader);
            }

            string startupPath = Application.StartupPath;
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

            if (isEqual) {
                //This means the files were identical for given options.
                MessageBox.Show(this, SR.FilesAreIdenticalPrompt, SR.FilesAreIdenticalCaption,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return; 
            }

            string tempFile = Path.Combine(Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".htm");
            tempFiles.AddFile(tempFile, false);
            
            using (XmlReader diffGram = XmlReader.Create(diffFile, settings)) {
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
        
        string ApplicationPath {
            get {
                string path = Application.ExecutablePath;
                if (path.EndsWith("vstesthost.exe", StringComparison.CurrentCultureIgnoreCase)) {
                    // must be running UnitTests
                    Uri baseUri = new Uri(this.GetType().Assembly.Location);
                    Uri resolved = new Uri(baseUri, @"..\..\..\Application\bin\debug\XmlNotepad.exe");
                    path = resolved.LocalPath;
                }
                return path;
            }
        }

        public virtual void OpenNewWindow(string path){
            if (!string.IsNullOrEmpty(path)) {
                Uri uri = new Uri(path);
                if (uri.IsFile) {
                    path = uri.LocalPath;
                    if (!File.Exists(path)) {
                        DialogResult dr = MessageBox.Show(
                            String.Format(SR.CreateFile, path), SR.CreateNodeFileCaption,
                            MessageBoxButtons.OKCancel);
                        if (dr.Equals(DialogResult.OK)) {
                            try {
                                XmlDocument include = new XmlDocument();
                                include.InnerXml = "<root/>";
                                include.Save(path);
                            } catch (Exception e) {
                                MessageBox.Show(this, e.Message, SR.SaveErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        } else {
                            return;
                        }
                    }
                }
            }
            Launch(this.ApplicationPath, "\"" + path + "\"");
        }

        private void GotoDefinition() {
            SelectTreeView();
            this.SaveIfDirty(true);

            XmlTreeNode node = xmlTreeView1.SelectedNode;
            if (node == null) return;

            string ipath = node.GetDefinition();

            if (!string.IsNullOrEmpty(ipath)) {
                OpenNewWindow(ipath);
            }
            
        }

        private void gotoDefinitionToolStripMenuItem_Click(object sender, EventArgs e) {
            this.GotoDefinition();
        }

        private void ctxGotoDefinitionToolStripMenuItem_Click(object sender, EventArgs e) {
            this.GotoDefinition();
        }

        private void expandXIncludesToolStripMenuItem_Click(object sender, EventArgs e) {
            SelectTreeView();
            this.model.ExpandIncludes();
            includesExpanded = true;
        }

        private void exportErrorsToolStripMenuItem_Click(object sender, EventArgs e) {
            SaveAsErrors();
        }

        void SaveAsErrors() {
            SaveFileDialog sd = new SaveFileDialog();
            sd.Filter = SR.SaveAsFilter;
            sd.Title = SR.SaveErrorsCaption;
            if (sd.ShowDialog(this) == DialogResult.OK) {
                string fname = sd.FileName;
                if (CheckReadOnly(fname)) {
                    SaveErrors(fname);
                }
            }
        }

        private void changeToAttributeToolStripMenuItem1_Click(object sender, EventArgs e) {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Attribute);
        }

        private void changeToTextToolStripMenuItem1_Click(object sender, EventArgs e) {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Text);
        }

        private void changeToCDATAToolStripMenuItem1_Click(object sender, EventArgs e) {
            this.xmlTreeView1.ChangeTo(XmlNodeType.CDATA);
        }

        private void changeToCommentToolStripMenuItem1_Click(object sender, EventArgs e) {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Comment);
        }

        private void changeToProcessingInstructionToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.ChangeTo(XmlNodeType.ProcessingInstruction);
        }

        private void changeToElementContextMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Element);
        }

        private void changeToAttributeContextMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Attribute);
        }

        private void changeToTextToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Text);
        }

        private void changeToCDATAContextMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.ChangeTo(XmlNodeType.CDATA);
        }

        private void changeToCommentContextMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.ChangeTo(XmlNodeType.Comment);
        }

        private void changeToProcessingInstructionContextMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.ChangeTo(XmlNodeType.ProcessingInstruction);
        }

        private void incrementalSearchToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.StartIncrementalSearch();
        }

        private void renameToolStripMenuItem1_Click(object sender, EventArgs e) {
            this.xmlTreeView1.BeginEditNodeName();
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.BeginEditNodeName();
        }

        private void insertToolStripMenuItem1_Click(object sender, EventArgs e) {
            this.RepeatSelectedNode();
        }

        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e) {
            this.DeleteSelectedNode();
        }

        private void duplicateToolStripMenuItem1_Click(object sender, EventArgs e) {
            this.DuplicateSelectedNode();
        }

        private void fileAssociationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                FileAssociation.AddXmlProgids();
            } 
            catch (Exception)
            {
            }

            var message = string.Format("Please go to Windows Settings for 'Default Apps' and select 'Choose default apps by file type' add XML Notepad for each file type you want associated with it.", this.Text);
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