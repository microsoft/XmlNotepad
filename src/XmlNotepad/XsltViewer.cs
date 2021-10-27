using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SR = XmlNotepad.StringResources;


namespace XmlNotepad
{
    public partial class XsltViewer : UserControl
    {
        private ISite _site;
        private XmlCache _model;
        private bool _userSpecifiedOutput;

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

            TransformButton.SizeChanged += TransformButton_SizeChanged;

            xsltControl.LoadCompleted += OnXsltLoadCompleted;
        }

        public XsltControl GetXsltControl()
        {
            return this.xsltControl;
        }

        public void OnClosed()
        {
            this.xsltControl.OnClosed();
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
            else
            {
                _userSpecifiedOutput = true;
            }
        }

        public void DisplayXsltResults()
        {
            string xpath = this.SourceFileName.Text.Trim();
            string output = this.OutputFileName.Text.Trim();
            if (!_userSpecifiedOutput && !string.IsNullOrEmpty(this._model.XsltDefaultOutput))
            {
                output = this._model.XsltDefaultOutput;
            }
            output = this.xsltControl.DisplayXsltResults(this._model.Document, xpath, output);
            if (!string.IsNullOrWhiteSpace(output))
            {
                this.OutputFileName.Text = MakeRelative(output);
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
            base.OnPaint(e);
        }

        public void SetSite(ISite site)
        {
            this._site = site;
            this.xsltControl.SetSite(site);
            IServiceProvider sp = (IServiceProvider)site;
            this._model = (XmlCache)site.GetService(typeof(XmlCache));
            this._model.ModelChanged -= new EventHandler<ModelChangedEventArgs>(OnModelChanged);
            this._model.ModelChanged += new EventHandler<ModelChangedEventArgs>(OnModelChanged);
        }

        void OnModelChanged(object sender, ModelChangedEventArgs e)
        {
            OnModelChanged(e);
        }

        void OnModelChanged(ModelChangedEventArgs e)
        {
            var doc = _model.Document;
            try
            {
                if (!string.IsNullOrEmpty(_model.FileName))
                {
                    var uri = new Uri(_model.FileName);
                    if (uri != this.xsltControl.BaseUri)
                    {
                        this.xsltControl.BaseUri = uri;
                        this.OutputFileName.Text = ""; // reset it since the file type might need to change...
                        _userSpecifiedOutput = false;
                    }
                }
                this.SourceFileName.Text = _model.XsltFileName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("XsltViewer.OnModelChanged exception " + ex.Message);
            }
        }

        private string MakeRelative(string path)
        {
            if (path.StartsWith(System.IO.Path.GetTempPath()))
            {
                return path; // don't relativize temp dir.
            }
            var uri = new Uri(path, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
            {
                return path;
            }
            var relative = this.xsltControl.BaseUri.MakeRelativeUri(uri);
            if (relative.IsAbsoluteUri)
            {
                return relative.LocalPath;
            }
            return relative.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped).Replace('/', '\\');
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = SR.XSLFileFilter;
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    this.SourceFileName.Text = MakeRelative(ofd.FileName);
                }
            }
        }

        private void BrowseOutputButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = this.xsltControl.GetOutputFileFilter(this.SourceFileName.Text.Trim());
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    this.OutputFileName.Text = MakeRelative(ofd.FileName);
                }
            }
        }

        private void TransformButton_Click(object sender, EventArgs e)
        {
            this.xsltControl.DeletePreviousOutput();
            this.DisplayXsltResults();
        }
    }
}