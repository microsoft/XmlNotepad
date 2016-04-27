namespace XmlNotepad {
    partial class FormSearch {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSearch));
            this.checkBoxMatchCase = new System.Windows.Forms.CheckBox();
            this.checkBoxWholeWord = new System.Windows.Forms.CheckBox();
            this.checkBoxRegex = new System.Windows.Forms.CheckBox();
            this.checkBoxXPath = new System.Windows.Forms.CheckBox();
            this.dataGridViewNamespaces = new System.Windows.Forms.DataGridView();
            this.prefixDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.namespaceDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataSet1 = new System.Data.DataSet();
            this.dataTableNamespaces = new System.Data.DataTable();
            this.dataColumnPrefix = new System.Data.DataColumn();
            this.dataColumnNamespace = new System.Data.DataColumn();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioButtonDown = new System.Windows.Forms.RadioButton();
            this.radioButtonUp = new System.Windows.Forms.RadioButton();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.comboBoxFilter = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.comboBoxReplace = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.comboBoxFind = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonReplace = new System.Windows.Forms.Button();
            this.buttonReplaceAll = new System.Windows.Forms.Button();
            this.buttonFindNext = new System.Windows.Forms.Button();
            this.tableLayoutPanelRoot = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewNamespaces)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataSet1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTableNamespaces)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel5.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.tableLayoutPanel6.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanelRoot.SuspendLayout();
            this.SuspendLayout();
            // 
            // checkBoxMatchCase
            // 
            resources.ApplyResources(this.checkBoxMatchCase, "checkBoxMatchCase");
            this.checkBoxMatchCase.Name = "checkBoxMatchCase";
            this.checkBoxMatchCase.UseVisualStyleBackColor = true;
            // 
            // checkBoxWholeWord
            // 
            resources.ApplyResources(this.checkBoxWholeWord, "checkBoxWholeWord");
            this.checkBoxWholeWord.Name = "checkBoxWholeWord";
            this.checkBoxWholeWord.UseVisualStyleBackColor = true;
            // 
            // checkBoxRegex
            // 
            resources.ApplyResources(this.checkBoxRegex, "checkBoxRegex");
            this.checkBoxRegex.Name = "checkBoxRegex";
            this.checkBoxRegex.UseVisualStyleBackColor = true;
            this.checkBoxRegex.CheckedChanged += new System.EventHandler(this.checkBoxRegex_CheckedChanged);
            // 
            // checkBoxXPath
            // 
            resources.ApplyResources(this.checkBoxXPath, "checkBoxXPath");
            this.checkBoxXPath.Name = "checkBoxXPath";
            this.checkBoxXPath.UseVisualStyleBackColor = true;
            this.checkBoxXPath.CheckedChanged += new System.EventHandler(this.checkBoxXPath_CheckedChanged);
            // 
            // dataGridViewNamespaces
            // 
            this.dataGridViewNamespaces.AutoGenerateColumns = false;
            this.dataGridViewNamespaces.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewNamespaces.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridViewNamespaces.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewNamespaces.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.prefixDataGridViewTextBoxColumn,
            this.namespaceDataGridViewTextBoxColumn});
            this.dataGridViewNamespaces.DataMember = "TableNamespaces";
            this.dataGridViewNamespaces.DataSource = this.dataSet1;
            resources.ApplyResources(this.dataGridViewNamespaces, "dataGridViewNamespaces");
            this.dataGridViewNamespaces.Name = "dataGridViewNamespaces";
            // 
            // prefixDataGridViewTextBoxColumn
            // 
            this.prefixDataGridViewTextBoxColumn.DataPropertyName = "Prefix";
            resources.ApplyResources(this.prefixDataGridViewTextBoxColumn, "prefixDataGridViewTextBoxColumn");
            this.prefixDataGridViewTextBoxColumn.Name = "prefixDataGridViewTextBoxColumn";
            // 
            // namespaceDataGridViewTextBoxColumn
            // 
            this.namespaceDataGridViewTextBoxColumn.DataPropertyName = "Namespace";
            resources.ApplyResources(this.namespaceDataGridViewTextBoxColumn, "namespaceDataGridViewTextBoxColumn");
            this.namespaceDataGridViewTextBoxColumn.Name = "namespaceDataGridViewTextBoxColumn";
            // 
            // dataSet1
            // 
            this.dataSet1.DataSetName = "Namespaces";
            this.dataSet1.Tables.AddRange(new System.Data.DataTable[] {
            this.dataTableNamespaces});
            // 
            // dataTableNamespaces
            // 
            this.dataTableNamespaces.Columns.AddRange(new System.Data.DataColumn[] {
            this.dataColumnPrefix,
            this.dataColumnNamespace});
            this.dataTableNamespaces.TableName = "TableNamespaces";
            // 
            // dataColumnPrefix
            // 
            this.dataColumnPrefix.Caption = "Prefix";
            this.dataColumnPrefix.ColumnName = "Prefix";
            // 
            // dataColumnNamespace
            // 
            this.dataColumnNamespace.Caption = "Namespace";
            this.dataColumnNamespace.ColumnName = "Namespace";
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.radioButtonDown);
            this.groupBox1.Controls.Add(this.radioButtonUp);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // radioButtonDown
            // 
            resources.ApplyResources(this.radioButtonDown, "radioButtonDown");
            this.radioButtonDown.Checked = true;
            this.radioButtonDown.Name = "radioButtonDown";
            this.radioButtonDown.TabStop = true;
            this.radioButtonDown.UseVisualStyleBackColor = true;
            // 
            // radioButtonUp
            // 
            resources.ApplyResources(this.radioButtonUp, "radioButtonUp");
            this.radioButtonUp.Name = "radioButtonUp";
            this.radioButtonUp.TabStop = true;
            this.radioButtonUp.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.BackColor = System.Drawing.SystemColors.Control;
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel5, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.groupBox1, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel4, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.buttonFindNext, 1, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // tableLayoutPanel5
            // 
            resources.ApplyResources(this.tableLayoutPanel5, "tableLayoutPanel5");
            this.tableLayoutPanel5.Controls.Add(this.tableLayoutPanel7, 1, 0);
            this.tableLayoutPanel5.Controls.Add(this.tableLayoutPanel6, 0, 0);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            // 
            // tableLayoutPanel7
            // 
            resources.ApplyResources(this.tableLayoutPanel7, "tableLayoutPanel7");
            this.tableLayoutPanel7.Controls.Add(this.groupBox2, 0, 0);
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.comboBoxFilter);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // comboBoxFilter
            // 
            resources.ApplyResources(this.comboBoxFilter, "comboBoxFilter");
            this.comboBoxFilter.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.comboBoxFilter.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxFilter.FormattingEnabled = true;
            this.comboBoxFilter.Name = "comboBoxFilter";
            // 
            // tableLayoutPanel6
            // 
            resources.ApplyResources(this.tableLayoutPanel6, "tableLayoutPanel6");
            this.tableLayoutPanel6.Controls.Add(this.checkBoxMatchCase, 0, 0);
            this.tableLayoutPanel6.Controls.Add(this.checkBoxWholeWord, 0, 1);
            this.tableLayoutPanel6.Controls.Add(this.checkBoxRegex, 0, 2);
            this.tableLayoutPanel6.Controls.Add(this.checkBoxXPath, 0, 3);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
            // 
            // tableLayoutPanel4
            // 
            resources.ApplyResources(this.tableLayoutPanel4, "tableLayoutPanel4");
            this.tableLayoutPanel4.Controls.Add(this.comboBoxReplace, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            // 
            // comboBoxReplace
            // 
            resources.ApplyResources(this.comboBoxReplace, "comboBoxReplace");
            this.comboBoxReplace.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxReplace.FormattingEnabled = true;
            this.comboBoxReplace.Name = "comboBoxReplace";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // tableLayoutPanel3
            // 
            resources.ApplyResources(this.tableLayoutPanel3, "tableLayoutPanel3");
            this.tableLayoutPanel3.Controls.Add(this.comboBoxFind, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            // 
            // comboBoxFind
            // 
            resources.ApplyResources(this.comboBoxFind, "comboBoxFind");
            this.comboBoxFind.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.comboBoxFind.FormattingEnabled = true;
            this.comboBoxFind.Name = "comboBoxFind";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.buttonReplace, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.buttonReplaceAll, 0, 1);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // buttonReplace
            // 
            resources.ApplyResources(this.buttonReplace, "buttonReplace");
            this.buttonReplace.Name = "buttonReplace";
            this.buttonReplace.UseVisualStyleBackColor = true;
            // 
            // buttonReplaceAll
            // 
            resources.ApplyResources(this.buttonReplaceAll, "buttonReplaceAll");
            this.buttonReplaceAll.Name = "buttonReplaceAll";
            this.buttonReplaceAll.UseVisualStyleBackColor = true;
            // 
            // buttonFindNext
            // 
            resources.ApplyResources(this.buttonFindNext, "buttonFindNext");
            this.buttonFindNext.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonFindNext.Name = "buttonFindNext";
            this.buttonFindNext.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanelRoot
            // 
            resources.ApplyResources(this.tableLayoutPanelRoot, "tableLayoutPanelRoot");
            this.tableLayoutPanelRoot.Controls.Add(this.dataGridViewNamespaces, 0, 1);
            this.tableLayoutPanelRoot.Controls.Add(this.tableLayoutPanel1, 0, 0);
            this.tableLayoutPanelRoot.Name = "tableLayoutPanelRoot";
            // 
            // FormSearch
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanelRoot);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormSearch";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewNamespaces)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataSet1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataTableNamespaces)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.tableLayoutPanel7.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.tableLayoutPanel6.ResumeLayout(false);
            this.tableLayoutPanel6.PerformLayout();
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanelRoot.ResumeLayout(false);
            this.tableLayoutPanelRoot.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxMatchCase;
        private System.Windows.Forms.CheckBox checkBoxWholeWord;
        private System.Windows.Forms.CheckBox checkBoxRegex;
        private System.Windows.Forms.CheckBox checkBoxXPath;
        private System.Windows.Forms.DataGridView dataGridViewNamespaces;
        private System.Data.DataSet dataSet1;
        private System.Data.DataTable dataTableNamespaces;
        private System.Data.DataColumn dataColumnPrefix;
        private System.Data.DataColumn dataColumnNamespace;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioButtonDown;
        private System.Windows.Forms.RadioButton radioButtonUp;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button buttonFindNext;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button buttonReplace;
        private System.Windows.Forms.Button buttonReplaceAll;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.ComboBox comboBoxReplace;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.ComboBox comboBoxFind;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel6;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ComboBox comboBoxFilter;
        private System.Windows.Forms.DataGridViewTextBoxColumn prefixDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn namespaceDataGridViewTextBoxColumn;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelRoot;
    }
}