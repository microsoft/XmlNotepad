using System.Windows.Forms;

namespace XmlNotepad {
    partial class FormAbout : Form {

        private Label label2;
        private Label labelVersion;
        private Label label1;
        private Label labelURL;
        private TableLayoutPanel tableLayoutPanel1;
        private PictureBox pictureBox1;
        private LinkLabel linkLabel1;
        private Button buttonOK;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            // Create controls
            this.pictureBox1 = new PictureBox();
            this.linkLabel1 = new LinkLabel();
            this.buttonOK = new Button();
            this.label2 = new Label();
            this.labelVersion = new Label();
            this.label1 = new Label();
            this.labelURL = new Label();
            this.tableLayoutPanel1 = new TableLayoutPanel();

            // Initialize controls
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();

            // pictureBox1
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;

            // linkLabel1
            this.linkLabel1.BackColor = System.Drawing.Color.Transparent;
            this.linkLabel1.LinkBehavior = LinkBehavior.HoverUnderline;
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.TabStop = true;
            this.linkLabel1.LinkClicked += new LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);

            // buttonOK
            this.buttonOK.DialogResult = DialogResult.OK;
            this.buttonOK.Name = "buttonOK";

            // label2
            this.label2.Name = "label2";

            // labelVersion
            this.labelVersion.Name = "labelVersion";

            // label1
            this.label1.Name = "label1";

            // labelURL
            this.labelURL.Name = "labelURL";

            // tableLayoutPanel1
            this.tableLayoutPanel1.BackColor = System.Drawing.Color.Transparent;
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 7;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelURL, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.labelVersion, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.linkLabel1, 0, 5);

            // FormAbout
            this.AcceptButton = this.buttonOK;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.CancelButton = this.buttonOK;
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.tableLayoutPanel1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.ShowInTaskbar = false;
            this.ResumeLayout(false);
        }
    }
}
