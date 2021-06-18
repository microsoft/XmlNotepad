
namespace XmlNotepad
{
    partial class XsltControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.webBrowser2 = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            ((System.ComponentModel.ISupportInitialize)(this.webBrowser2)).BeginInit();
            this.SuspendLayout();
            // 
            // webBrowser2
            // 
            this.webBrowser2.CreationProperties = null;
            this.webBrowser2.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webBrowser2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser2.Location = new System.Drawing.Point(0, 0);
            this.webBrowser2.Name = "webBrowser2";
            this.webBrowser2.Size = new System.Drawing.Size(657, 469);
            this.webBrowser2.TabIndex = 3;
            this.webBrowser2.ZoomFactor = 1D;
            // 
            // webBrowser1
            // 
            this.webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser1.Location = new System.Drawing.Point(0, 0);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(657, 469);
            this.webBrowser1.TabIndex = 4;
            this.webBrowser1.Visible = false;
            // 
            // XsltControl
            // 
            this.Controls.Add(this.webBrowser1);
            this.Controls.Add(this.webBrowser2);
            this.Name = "XsltControl";
            this.Size = new System.Drawing.Size(657, 469);
            ((System.ComponentModel.ISupportInitialize)(this.webBrowser2)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webBrowser2;
        private System.Windows.Forms.WebBrowser webBrowser1;
    }
}
