using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml.Schema;
using System.Xml;
using System.Diagnostics;

namespace XmlNotepad {
    /// <summary>
    /// FormSchemas provides a simple grid view interface on top of the SchemaCache and provides
    /// a way to add and remove schemas from the cache.  You can also "disable" certain schemas
    /// from being used in validation by checking the disabled checkbox next to the schema.
    /// All this is persisted in the Settings class so it's remembered across sessions.
    /// </summary>
    public partial class FormSchemas : Form {
        SchemaCache cache;
        UndoManager undoManager = new UndoManager(1000);
        bool inUndoRedo;
        // private cache so we can get the "old" values for undo/redo support.
        List<SchemaItem> items = new List<SchemaItem>();

        public FormSchemas() {
            InitializeComponent();
            DataGridViewBrowseCell template = new DataGridViewBrowseCell();
            template.UndoManager = this.undoManager;
            template.OpenFileDialog = this.openFileDialog1;
            this.columnBrowse.CellTemplate = template;
            this.dataGridView1.CellValidating += new DataGridViewCellValidatingEventHandler(dataGridView1_CellValidating);
            this.undoManager.StateChanged += new EventHandler(undoManager_StateChanged);                                
        }

        void undoManager_StateChanged(object sender, EventArgs e) {
            this.UpdateMenuState();
        }

        protected override void OnLoad(EventArgs e) {            
            HelpProvider hp = this.Site.GetService(typeof(HelpProvider)) as HelpProvider;
            if (hp != null) {
                hp.SetHelpKeyword(this, "Schemas");
                hp.SetHelpNavigator(this, HelpNavigator.KeywordIndex);
            }
            UpdateMenuState();
            LoadSchemas();
            this.dataGridView1.CellValueChanged += new DataGridViewCellEventHandler(dataGridView1_CellValueChanged);
            this.dataGridView1.RowsAdded += new DataGridViewRowsAddedEventHandler(dataGridView1_RowsAdded);
        }

        void dataGridView1_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e) {
            if (!inUndoRedo) {
                SchemaDialogNewRow cmd = new SchemaDialogNewRow(this.dataGridView1, this.items, this.dataGridView1.Rows[e.RowIndex]);
                Push(cmd);
            }
            return;
        }

        void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e) {
            if (e.ColumnIndex == 2) {
                DataGridViewRow row = this.dataGridView1.Rows[e.RowIndex];
                DataGridViewCell cell = row.Cells[2];
                string filename = cell.Value as string;
                if (!inUndoRedo) {
                    SchemaDialogNewRow newRow = this.undoManager.Peek() as SchemaDialogNewRow;
                    SchemaDialogEditCommand cmd = new SchemaDialogEditCommand(this.dataGridView1, this.items, row, filename);
                    if (newRow != null) {
                        this.undoManager.Pop(); // merge new row command with this edit command.
                        cmd.IsNewRow = true;
                    }
                    Push(cmd);
                }
            }
        }


        void dataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e) {
            if (e.ColumnIndex == 2) {
                DataGridViewRow row = this.dataGridView1.Rows[e.RowIndex];
                string filename = e.FormattedValue as string;
                if (string.IsNullOrEmpty(filename))
                    return;
                if (SchemaDialogCommand.ValidateSchema(row, filename) == null) {
                    e.Cancel = true;
                }
            }
        }

        void LoadSchemas() {
            if (this.cache == null) {
                if (this.Site != null) {
                    this.cache = (SchemaCache)this.Site.GetService(typeof(SchemaCache));
                }
            }
            items.Clear();
            DataGridViewRowCollection col = this.dataGridView1.Rows;
            col.Clear();
            if (this.cache != null) {
                foreach (CacheEntry e in this.cache.GetSchemas()) {
                    Uri uri = e.Location;
                    string filename = uri.IsFile ? uri.LocalPath : uri.AbsoluteUri;
                    SchemaItem item = new SchemaItem(e.Disabled, e.TargetNamespace, filename);
                    items.Add(item);
                    int i = col.Add(item.Values);
                    col[i].Tag = item;
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e) {
            if (!Cancel())
                e.Cancel = true;
        }

        public bool Cancel() {
            if (this.undoManager.CanUndo) {
                if (MessageBox.Show(this, SR.DiscardChanges, SR.DiscardChangesCaption, MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel) {
                    return false;
                }
            }
            return true;
        }
        
        private void buttonOk_Click(object sender, EventArgs e) {
            Ok();
        }

        public void Ok() {
            if (Commit()) {
                this.DialogResult = DialogResult.OK;
            }
        }

        public bool Commit() {
            IList<CacheEntry> oldList = cache.GetSchemas();
            XmlResolver resolver = this.cache.Resolver;
            foreach (DataGridViewRow row in this.dataGridView1.Rows) {
                string filename = row.Cells[2].Value as string;
                SchemaItem item = (SchemaItem)row.Tag;
                if (!string.IsNullOrEmpty(filename)) {
                    CacheEntry ce = this.cache.FindSchemaByUri(filename);
                    bool isNew = (ce == null);
                    if (ce == null || ce.Schema == null) {
                        try {
                            XmlSchema s = resolver.GetEntity(new Uri(filename), "", typeof(XmlSchema)) as XmlSchema;
                            if (ce == null) {
                                ce = this.cache.Add(s);
                            } else {
                                ce.Schema = s;
                            }
                        } catch (Exception e) {
                            DialogResult rc = MessageBox.Show(this, string.Format(SR.SchemaLoadError, filename, e.Message),
                                SR.SchemaError, MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                            if (rc == DialogResult.Cancel) {
                                row.Selected = true;
                                return false;
                            }
                        }
                    } 
                    if (!isNew){
                        oldList.Remove(ce);
                    }
                    if (row.Cells[0].Value != null) {
                        ce.Disabled = (bool)row.Cells[0].Value;
                    }
                }
            }            
            // Remove schemas from the cache that have been removed from the grid view.
            foreach (CacheEntry toRemove in oldList) {
                cache.Remove(toRemove);
            }
            this.undoManager.Clear();
            return true;
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e) {
            foreach (DataGridViewRow row in this.dataGridView1.Rows) {
                row.Selected = true;
            }
            Push(new SchemaDialogCutCommand(this.dataGridView1, this.items));
        }

        private void addSchemasToolStripMenuItem_Click(object sender, EventArgs e) {
            this.openFileDialog1.Multiselect = true;
            if (this.openFileDialog1.ShowDialog(this) == DialogResult.OK) {
                Push(new SchemaDialogAddFiles(this.dataGridView1, this.items, this.openFileDialog1.FileNames));                
            }
        }

        void Push(Command cmd) {
            this.inUndoRedo = true;
            undoManager.Push(cmd);
            UpdateMenuState();
            this.inUndoRedo = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {            
            bool isControl = e.Modifiers == Keys.Control;
            bool isGridEditing = this.dataGridView1.EditingControl != null;
            bool handled = false;
            switch (e.KeyCode & ~Keys.Modifiers)
            {
                case Keys.V:
                    if (isControl && !isGridEditing)
                    {
                        pasteToolStripMenuItem_Click(this, e);
                        handled = true;
                    }
                    break;
                case Keys.X:
                    if (isControl && !isGridEditing)
                    {
                        cutToolStripMenuItem_Click(this, e);
                        handled = true;
                    }
                    break;
                case Keys.C:
                    if (isControl && !isGridEditing)
                    {
                        copyToolStripMenuItem_Click(this, e);
                        handled = true;
                    }
                    break;
                case Keys.Delete:
                    if (!isGridEditing)
                    {
                        deleteToolStripMenuItem_Click(this, e);
                        handled = true;
                    }
                    break;
                case Keys.Enter:
                    if (!isGridEditing)
                    {
                        handled = true;
                        Ok();
                    }
                    break;
                case Keys.Escape:
                    if (!isGridEditing)
                    {
                        handled = true;
                        Close();
                    }
                    break;
            }
            if (!handled)
            {
                base.OnKeyDown(e);
            }
            else
            {
                e.Handled = true;
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e) {            
            SchemaDialogCutCommand cmd = new SchemaDialogCutCommand(this.dataGridView1, this.items);
            Push(cmd);
            if (!string.IsNullOrEmpty(cmd.Clip)) {
                Clipboard.SetText(cmd.Clip);
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e) {
            SchemaDialogCutCommand cmd = new SchemaDialogCutCommand(this.dataGridView1, this.items);
            StringBuilder clip = new StringBuilder();            
            cmd.ProcessSelectedRows(delegate(DataGridViewRow row, SchemaItem item) {
                Debug.WriteLine("copyToolStripMenuItem_Click selected row " + row.Index);
                cmd.AddEscapedUri(clip, row.Cells[2].Value as string);
            });
            if (clip.Length > 0) {
                Clipboard.SetText(clip.ToString());
            }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e) {
            string text = Clipboard.GetText();
            if (!string.IsNullOrEmpty(text)) {
                Push(new SchemaDialogAddFiles(this.dataGridView1, this.items,
                    text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)));
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e) {
            Push(new SchemaDialogCutCommand(this.dataGridView1, this.items));        
        }

        void UpdateMenuState() {
            this.undoToolStripMenuItem.Enabled = this.undoManager.CanUndo;
            this.redoToolStripMenuItem.Enabled = this.undoManager.CanRedo;
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e) {
            inUndoRedo = true;
            this.undoManager.Undo();
            UpdateMenuState();
            inUndoRedo = false;
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e) {
            inUndoRedo = true;
            this.undoManager.Redo();
            UpdateMenuState();
            inUndoRedo = false;
        }
    }

    public class SchemaItem {
        bool disabled;
        string targetNamespace;
        string filename;
        XmlSchema schema;

        public SchemaItem() {
        }

        public SchemaItem(bool disabled, string targetNamespace, string filename) {
            this.disabled = disabled;
            this.targetNamespace = targetNamespace;
            this.filename = filename;
        }

        public bool Disabled {
            get { return disabled; }
            set { disabled = value; }
        }

        public string TargetNamespace {
            get { return targetNamespace; }
            set { targetNamespace = value; }
        }

        public string Filename {
            get { return filename; }
            set { filename = value; }
        }

        public XmlSchema Schema {
            get { return schema; }
            set { schema = value; }
        }

        public object[] Values {
            get { return new object[] { this.disabled, this.targetNamespace, this.filename }; }
        }

    }

    public class DataGridViewBrowseCell : DataGridViewButtonCell {
        OpenFileDialog fd;
        UndoManager undoManager;

        public DataGridViewBrowseCell(OpenFileDialog fd, UndoManager um) {
            this.UseColumnTextForButtonValue = true;
            this.undoManager = um;
            this.fd = fd;
        }

        public DataGridViewBrowseCell() {
            this.UseColumnTextForButtonValue = true;
        }

        public UndoManager UndoManager {
            get { return this.undoManager; }
            set { this.undoManager = value; }
        }

        public OpenFileDialog OpenFileDialog {
            get { return this.fd; }
            set { this.fd = value; }
        }

        protected override AccessibleObject CreateAccessibilityInstance()
        {
            return new DataGridViewBrowseCellAutomationElement(this);
        }

        class DataGridViewBrowseCellAutomationElement : DataGridViewButtonCellAccessibleObject
        {
            public DataGridViewBrowseCellAutomationElement(DataGridViewCell cell)
                : base(cell)
            {
            }

            public override string Name
            {
                get
                {
                    return string.Format("Browse Row {0}", this.Owner.OwningRow.Index);
                }
            }
        }

        public override object Clone() {
            return new DataGridViewBrowseCell(this.fd, this.undoManager);
        }

        protected override object GetFormattedValue(object value, int rowIndex, ref DataGridViewCellStyle cellStyle, TypeConverter valueTypeConverter, TypeConverter formattedValueTypeConverter, DataGridViewDataErrorContexts context) {
            object x = base.GetFormattedValue(value, rowIndex, ref cellStyle, valueTypeConverter, formattedValueTypeConverter, context);
            return "...";
        }

        protected override void OnClick(DataGridViewCellEventArgs e) {
            DataGridViewRow row = this.OwningRow;
            DataGridView view = row.DataGridView;
            string filename = row.Cells[2].Value as string;
            if (!string.IsNullOrEmpty(filename)) {
                Uri uri = new Uri(filename);
                if (uri.IsFile) {
                    fd.FileName = uri.LocalPath;
                }
            }
            fd.Multiselect = false;
            if (fd.ShowDialog(this.DataGridView.FindForm()) == DialogResult.OK) {
                if (SchemaDialogCommand.ValidateSchema(row, fd.FileName) != null) {
                    row.Cells[2].Value = fd.FileName;
                }
            }
        }
    }

    abstract class SchemaDialogCommand : Command {

        DataGridView view;
        List<SchemaItem> items;

        public SchemaDialogCommand(DataGridView view, List<SchemaItem> items) {
            this.view = view;
            this.items = items;
        }

        public DataGridView View {
            get { return view; }
        }

        public override bool IsNoop {
            get {                
                return false;
            }
        }

        public void InvalidateRow(DataGridViewRow row) {
            foreach (DataGridViewRow vr in this.view.Rows) {
                View.InvalidateRow(vr.Index);
            }
        }

        protected void SelectRows(IList<DataGridViewRow> list) {
            this.view.ClearSelection();
            foreach (DataGridViewRow vr in list) {
                vr.Selected = true;                
            }
        }

        public bool IsSamePath(string a, string b) {
            try {
                if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b);
                Uri ua = new Uri(a);
                Uri ub = new Uri(b);
                return ua == ub;
            } catch {
                return a == b;
            }
        }

        public DataGridViewRow FindExistingRow(string schema) {
            foreach (DataGridViewRow row in this.view.Rows) {
                DataGridViewCell cell = row.Cells[2];
                string path = cell.Value as string;
                if (IsSamePath(path, schema)) {
                    // already there!!
                    return row;
                }
            }
            return null;
        }

        public SchemaItem FindExistingItem(string schema) {
            foreach (SchemaItem row in this.items) {
                if (IsSamePath(row.Filename, schema)) {                    
                    return row;
                }
            }
            return null;
        }

        public DataGridViewRow InsertRow(string schema) {
            XmlSchema s = LoadSchema(schema);
            if (s != null) {
                int i = InsertRow(false, s.TargetNamespace, schema);
                return this.view.Rows[i];
            }            
            return null;
        }

        public int InsertRow(bool disabled, string targetNamespace, string filename) {
            SchemaItem item = new SchemaItem(disabled, targetNamespace, filename);
            int i = this.view.Rows.Add(item.Values);
            DataGridViewRow row = this.view.Rows[i];
            AttachItem(row, item);
            return i;
        }

        public void InsertRow(int i, DataGridViewRow row) {
            this.view.Rows.Insert(i, row);
            if (row.Tag != null) {
                AttachItem(row, row.Tag as SchemaItem);
            }
        }

        public void AttachItem(DataGridViewRow row, SchemaItem item) {
            row.Tag = item;
            this.items.Insert(row.Index, item);
        }        

        public delegate void DataRowHandler(DataGridViewRow row, SchemaItem item);

        public void ProcessSelectedRows(DataRowHandler handler) {
            this.view.SuspendLayout();
            foreach (DataGridViewRow row in this.view.SelectedRows) {
                handler(row, row.Tag as SchemaItem);
            }
            this.view.ResumeLayout();
        }

        public void AddEscapedUri(StringBuilder sb, string filename) {
            if (!string.IsNullOrEmpty(filename)) {
                try {
                    Uri uri = new Uri(filename);
                    if (sb.Length > 0) {
                        sb.Append("\r\n");
                    }
                    sb.Append(uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped));
                } catch {
                }
            }
        }

        public void AddRows(IList<DataGridViewRow> rows) {
            DataGridViewRowCollection col = this.view.Rows;
            foreach (DataGridViewRow row in rows) {
                int i = col.Add(row);
                SchemaItem item = row.Tag as SchemaItem;
                if (item != null) {
                    items.Insert(i, item);
                }
            }
            SelectRows(rows);
        }

        public DataGridViewRow RemoveRow(DataGridViewRow row) {
            if (row.Index != -1) {
                this.view.Rows.Remove(row);
            }
            SchemaItem item = row.Tag as SchemaItem;
            if (item != null) {
                this.items.Remove(item);
            }
            return row;
        }

        public IList<DataGridViewRow> RemoveRows(IList<DataGridViewRow> rows) {
            DataGridViewRowCollection col = this.view.Rows;
            foreach (DataGridViewRow row in rows) {
                col.Remove(row);
                SchemaItem item = row.Tag as SchemaItem;
                if (item != null) {
                    this.items.Remove(item);
                }
            }
            return rows;
        }

        protected void Verify() {
            foreach (DataGridViewRow row in this.view.Rows) {
                string matches = "null";
                if (row.Tag != null) {
                    SchemaItem item = (SchemaItem)row.Tag;
                    if (item != items[row.Index]) {
                        matches = "false";
                    } else {
                        matches = "true";
                    }
                }
                string name = row.Cells[2].Value as string;                
                Trace.WriteLine("row[" + name + "]=" + matches);
            }
        }

        public static XmlSchema ValidateSchema(DataGridViewRow row, string filename) {
            try {
                XmlSchema schema = SchemaDialogCommand.LoadSchema(filename); // make sure we can load it!            
                return schema;
            } catch (Exception ex) {
                MessageBox.Show(string.Format(SR.SchemaLoadError, filename, ex.Message),
                    SR.SchemaError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        

        public static XmlSchema LoadSchema(string filename) {
            if (string.IsNullOrEmpty(filename)) return null;
            return XmlSchema.Read(new XmlTextReader(filename, new NameTable()), null);
        }

    }

    class SchemaDialogNewRow : SchemaDialogCommand {
        DataGridViewRow row;
        int index;
        public SchemaDialogNewRow(DataGridView view, List<SchemaItem> items, DataGridViewRow row)
            : base(view, items) {
            this.row = row;
        }

        public override string Name {
            get { return "New Row"; }
        }

        public override bool IsNoop {
            get {
                return this.index == View.Rows.Count;
            }
        }

        public override void Do() {
            this.index = row.Index;
            Verify();
        }

        public override void Undo() {
            RemoveRow(row);
            Verify();
        }

        public override void Redo() {
            InsertRow(index, row);
            this.index = row.Index;
            Verify();
        }
    }

    class SchemaDialogEditCommand : SchemaDialogCommand {
        DataGridViewRow row;
        string newSchema;
        string newNamespace;
        string oldSchema;
        XmlSchema schema;
        string oldNamespace;
        bool isNewRow;
        int index;

        public SchemaDialogEditCommand(DataGridView view, List<SchemaItem> items, DataGridViewRow row, string newSchema)
            : base(view, items) {
            this.newSchema = newSchema;
            this.row = row;
            SchemaItem item = row.Tag as SchemaItem;
            if (item != null) {
                oldSchema = item.Filename;
                oldNamespace = item.TargetNamespace;
                schema = item.Schema; 
            }
            // should succeed because previous code already validated the filename.
            schema = SchemaDialogCommand.LoadSchema(newSchema);
            newNamespace = schema == null ? "" : schema.TargetNamespace;
            index = row.Index;
        }

        public override string Name {
            get { return SR.EditSchemaCommand; }
        }

        public override bool IsNoop {
            get {
                return newSchema == oldSchema;
            }
        }

        public bool IsNewRow {
            get { return isNewRow; }
            set { isNewRow = value; }
        }

        public override void Do() {
            SchemaItem item = row.Tag as SchemaItem;
            if (row.Index == this.View.Rows.Count - 1) {
                isNewRow = true;
            }
            this.View.CurrentCell = row.Cells[2];
            this.View.NotifyCurrentCellDirty(true);
            row.Cells[1].Value = newNamespace;
            if (item == null) { // then it was a new row
                item = new SchemaItem(false, newNamespace, newSchema);
                item.Schema = schema;
                AttachItem(row, item);
            } else {
                item.TargetNamespace = newNamespace;
                item.Filename = newSchema;
            }
            InvalidateRow(row);
            this.View.NotifyCurrentCellDirty(false);
            Verify();
        }

        public override void Undo() {
            if (IsNewRow) {
                row = RemoveRow(row);
            } else {
                row.Cells[2].Value = oldSchema;
                row.Cells[1].Value = oldNamespace;
                SchemaItem item = row.Tag as SchemaItem;
                if (item != null) {
                    item.Filename = oldSchema;
                    item.TargetNamespace = oldNamespace;
                }
                InvalidateRow(row);
            }
            Verify();
        }

        public override void Redo() {
            if (IsNewRow) {
                this.InsertRow(index, row);
            } else {
                row.Cells[2].Value = newSchema;
                row.Cells[1].Value = newNamespace;
                InvalidateRow(row);
            }
            Verify();
        }
    }

    class SchemaDialogCutCommand : SchemaDialogCommand {
        string clip;
        IList<DataGridViewRow> deletedRows = new List<DataGridViewRow>();

        public SchemaDialogCutCommand(DataGridView view, List<SchemaItem> items)
            : base(view, items) {
        }

        public string Clip {
            get { return clip; }
            set { clip = value; }
        }

        public override string Name {
            get { return SR.CutSchemaCommand; }
        }

        public override void Do() {
            // This builds what should go on clipboard, but doesn't actually
            // mess with the clipboard - the caller does that so this command
            // can also be used for plain delete oepration.
            StringBuilder sb = new StringBuilder();
            ProcessSelectedRows(delegate(DataGridViewRow row, SchemaItem item) {
                string uri = row.Cells[2].Value as string;
                AddEscapedUri(sb, uri);
                if (!string.IsNullOrEmpty(uri)) {
                    deletedRows.Add(row);
                    this.RemoveRow(row);
                }                
            });
            this.clip = sb.ToString();
            Verify();
        }

        public override void Undo() {
            AddRows(deletedRows);
            Verify();
        }

        public override void Redo() {
            deletedRows = RemoveRows(deletedRows);
            Verify();
        }
    }

    class SchemaDialogAddFiles : SchemaDialogCommand {
        string[] files;
        IList<DataGridViewRow> newRows = new List<DataGridViewRow>();

        public SchemaDialogAddFiles(DataGridView view, List<SchemaItem> items, string[] files)
            : base(view, items) {
            this.files = files;
        }
        public override string Name {
            get { return SR.AddSchemaCommand; }
        }

        public override void Do() {
            List<DataGridViewRow> list = new List<DataGridViewRow>();
            foreach (string file in this.files) {
                try {
                    Uri uri = new Uri(file);
                    string path = uri.IsFile ? uri.LocalPath : uri.AbsoluteUri;
                    DataGridViewRow row = FindExistingRow(path);
                    if (row != null) {
                        XmlSchema s = LoadSchema(path);
                        if (s != null) {
                            SchemaItem item = row.Tag as SchemaItem;
                            if (item == null) {
                                item = new SchemaItem(false, s.TargetNamespace, path);
                                AttachItem(row, item);
                            }
                            row.Cells[1].Value = s.TargetNamespace;
                            item.TargetNamespace = s.TargetNamespace;
                            row.Cells[2].Value = path;
                            item.Filename = path;
                        }
                    } else {
                        row = InsertRow(path);
                        if (row != null) {
                            newRows.Add(row);
                            list.Add(row);
                        }
                    }
                    list.Add(row);
                } catch {
#if DEBUG
                    Trace.WriteLine("Bad file name:" + file);
#endif
                }
            }
            SelectRows(list);
            Verify();
        }

        public override void Undo() {
            newRows = RemoveRows(newRows);
            Verify();
        }

        public override void Redo() {
            AddRows(newRows);
            Verify();
        }
    }

}