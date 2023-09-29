
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace XmlNotepad
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public partial class FormMain : Form, ISite
    {
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
        private ToolStripStatusLabel toolStripStatusLabel1;

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        [STAThread]
        private void InitializeComponent()
        {
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
            this.openSettingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.goToLineToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this.gCCollectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
            this._taskList = new XmlNotepad.TaskList();
            this._dynamicHelpViewer = new XmlNotepad.XsltControl();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.openXmlDiffStylesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenu1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.tabControlViews.SuspendLayout();
            this.tabPageTreeView.SuspendLayout();
            this.tabPageHtmlView.SuspendLayout();
            this.statusStrip1.SuspendLayout();
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
            this.openSettingsToolStripMenuItem,
            this.openXmlDiffStylesToolStripMenuItem,
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
            // openSettingsToolStripMenuItem
            // 
            resources.ApplyResources(this.openSettingsToolStripMenuItem, "openSettingsToolStripMenuItem");
            this.openSettingsToolStripMenuItem.Name = "openSettingsToolStripMenuItem";
            this.openSettingsToolStripMenuItem.Click += new System.EventHandler(this.openSettingsToolStripMenuItem_Click);
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
            this.goToLineToolStripMenuItem,
            this.expandXIncludesToolStripMenuItem,
            this.toolStripMenuItem6,
            this.nudgeToolStripMenuItem,
            this.toolStripMenuItem7,
            this.findToolStripMenuItem,
            this.replaceToolStripMenuItem,
            this.incrementalSearchToolStripMenuItem,
            this.gCCollectToolStripMenuItem});
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
            // goToLineToolStripMenuItem
            // 
            this.goToLineToolStripMenuItem.Name = "goToLineToolStripMenuItem";
            resources.ApplyResources(this.goToLineToolStripMenuItem, "goToLineToolStripMenuItem");
            this.goToLineToolStripMenuItem.Click += new System.EventHandler(this.goToLineToolStripMenuItem_Click);
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
            // gCCollectToolStripMenuItem
            // 
            this.gCCollectToolStripMenuItem.Name = "gCCollectToolStripMenuItem";
            resources.ApplyResources(this.gCCollectToolStripMenuItem, "gCCollectToolStripMenuItem");
            this.gCCollectToolStripMenuItem.Click += new System.EventHandler(this.gCCollectToolStripMenuItem_Click);
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
            this.resizer.Border3DStyle = System.Windows.Forms.Border3DStyle.Flat;
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
            // _taskList
            // 
            resources.ApplyResources(this._taskList, "_taskList");
            this._taskList.Name = "_taskList";
            this.helpProvider1.SetShowHelp(this._taskList, ((bool)(resources.GetObject("_taskList.ShowHelp"))));
            // 
            // _dynamicHelpViewer
            // 
            resources.ApplyResources(this._dynamicHelpViewer, "_dynamicHelpViewer");
            this._dynamicHelpViewer.BaseUri = null;
            this._dynamicHelpViewer.BrowserVersion = null;
            this._dynamicHelpViewer.DefaultStylesheetResource = "XmlNotepad.DefaultSS.xslt";
            this._dynamicHelpViewer.DisableOutputFile = false;
            this._dynamicHelpViewer.EnableScripts = false;
            this._dynamicHelpViewer.HasXsltOutput = false;
            this._dynamicHelpViewer.IgnoreDTD = false;
            this._dynamicHelpViewer.Name = "_dynamicHelpViewer";
            this.helpProvider1.SetShowHelp(this._dynamicHelpViewer, ((bool)(resources.GetObject("_dynamicHelpViewer.ShowHelp"))));
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            resources.ApplyResources(this.statusStrip1, "statusStrip1");
            this.statusStrip1.Name = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            resources.ApplyResources(this.toolStripStatusLabel1, "toolStripStatusLabel1");
            // 
            // openXmlDiffStylesToolStripMenuItem
            // 
            this.openXmlDiffStylesToolStripMenuItem.Name = "openXmlDiffStylesToolStripMenuItem";
            resources.ApplyResources(this.openXmlDiffStylesToolStripMenuItem, "openXmlDiffStylesToolStripMenuItem");
            this.openXmlDiffStylesToolStripMenuItem.Click += new System.EventHandler(this.openXmlDiffStylesToolStripMenuItem_Click);
            // 
            // FormMain
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
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
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ToolStripMenuItem openSettingsToolStripMenuItem;
        private ToolStripMenuItem gCCollectToolStripMenuItem;
        private ToolStripMenuItem goToLineToolStripMenuItem;
        private ToolStripMenuItem openXmlDiffStylesToolStripMenuItem;
    }
}