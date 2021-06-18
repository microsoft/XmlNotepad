using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;

namespace XmlNotepad
{
    public partial class XsltViewer : UserControl
    {
        ISite site;
        XmlCache model;

        public XsltViewer()
        {
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            InitializeComponent();

            toolTip1.SetToolTip(this.BrowseButton, SR.BrowseButtonTooltip);
            toolTip1.SetToolTip(this.SourceFileName, SR.XslFileNameTooltip);
            toolTip1.SetToolTip(this.TransformButton, SR.TransformButtonTooltip);
            toolTip1.SetToolTip(this.OutputFileName, SR.XslOutputFileNameTooltip);

            BrowseButton.Click += new EventHandler(BrowseButton_Click);
            BrowseOutputButton.Click += new EventHandler(BrowseOutputButton_Click);
            this.SourceFileName.KeyDown += new KeyEventHandler(OnSourceFileNameKeyDown);
            this.OutputFileName.KeyDown += new KeyEventHandler(OnOutputFileNameKeyDown);

            this.xsltControl.DefaultStylesheetResource = "XmlNotepad.DefaultSS.xslt";
            this.xsltControl.DisableOutputFile = false;

            TransformButton.SizeChanged += TransformButton_SizeChanged;

            xsltControl.LoadCompleted += OnXsltLoadCompleted;
        }

        private void OnXsltLoadCompleted(object sender, PerformanceInfo info)
        {
            if (info != null)
            {
                if (Completed != null)
                {
                    Completed(this, info);
                }
                Debug.WriteLine("Browser loaded in {0} milliseconds", info.BrowserMilliseconds);
                info = null;
            }
        }

        public event EventHandler<PerformanceInfo> Completed;

        private void TransformButton_SizeChanged(object sender, EventArgs e)
        {
            CenterInputBoxes();
        }

        private void CenterInputBoxes()
        {
            // TextBoxes don't stretch when you set Anchor Top + Bottom, so we center the
            // Text Boxes manually so they look ok.
            int center = (tableLayoutPanel1.Height - SourceFileName.Height) / 2;
            SourceFileName.Margin = new Padding(0, center, 3, 3);
            OutputFileName.Margin = new Padding(0, center, 3, 3);
        }

        void OnSourceFileNameKeyDown(object sender, KeyEventArgs e)
        {
            this.OutputFileName.Text = ""; // need to recompute this then...
            if (e.KeyCode == Keys.Enter)
            {
                this.DisplayXsltResults();
            }
        }

        private void OnOutputFileNameKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.DisplayXsltResults();
            }
        }

        public void DisplayXsltResults()
        {
            string xpath = this.SourceFileName.Text.Trim();
            string output = this.OutputFileName.Text.Trim();
            string filename = this.xsltControl.DisplayXsltResults(this.model.Document, xpath, output);
            if (!string.IsNullOrEmpty(filename))
            {
                this.OutputFileName.Text = MakeRelative(filename);
            }
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.xsltControl.Top > 0 && this.Width > 0)
            {
                Graphics g = e.Graphics;
                Rectangle r = new Rectangle(0, 0, this.Width, this.xsltControl.Top);
                Color c1 = Color.FromArgb(250, 249, 245);
                Color c2 = Color.FromArgb(192, 192, 168);
                Color s1 = SystemColors.ControlLight;
                using (LinearGradientBrush brush = new LinearGradientBrush(r, c1, c2, LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brush, r);
                }
            }
        }

        public void SetSite(ISite site)
        {
            this.site = site;
            this.xsltControl.SetSite(site);
            IServiceProvider sp = (IServiceProvider)site;
            this.model = (XmlCache)site.GetService(typeof(XmlCache));
            this.model.ModelChanged -= new EventHandler<ModelChangedEventArgs>(OnModelChanged);
            this.model.ModelChanged += new EventHandler<ModelChangedEventArgs>(OnModelChanged);
            OnModelChanged();
        }

        void OnModelChanged(object sender, ModelChangedEventArgs e)
        {
            OnModelChanged();
        }

        void OnModelChanged()
        {
            var doc = model.Document;
            try
            {
                if (!string.IsNullOrEmpty(model.FileName))
                {
                    var uri = new Uri(model.FileName);
                    if (uri != this.xsltControl.BaseUri)
                    {
                        this.xsltControl.BaseUri = uri;
                        this.OutputFileName.Text = ""; // reset it since the file type might need to change...
                    }
                }
                this.SourceFileName.Text = model.XsltFileName;
                this.xsltControl.IgnoreDTD = model.GetSettingBoolean("IgnoreDTD");
            }
            catch (Exception)
            {
            }
        }

        private string MakeRelative(string path)
        {
            var uri = new Uri(path);
            var relative = this.xsltControl.BaseUri.MakeRelativeUri(uri);
            return relative.OriginalString;
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = SR.XSLFileFilter;
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                this.SourceFileName.Text = MakeRelative(ofd.FileName);
            }
        }

        private void BrowseOutputButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = this.xsltControl.GetOutputFileFilter(this.SourceFileName.Text.Trim());
            if (ofd.ShowDialog(this) == DialogResult.OK)
            {
                this.OutputFileName.Text = MakeRelative(ofd.FileName);
            }
        }

        private void TransformButton_Click(object sender, EventArgs e)
        {
            this.DisplayXsltResults();
        }
    }
}