using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
using System.Linq;
using System.Xml.Linq;
using System.Net;
using System.Security;
using System.Security.Permissions;

namespace XmlNotepad {
    public partial class XsltViewer : UserControl {
        Uri baseUri;
        XslCompiledTransform defaultss;
        XslCompiledTransform xslt;
        Uri xsltUri;
        DateTime loaded;
        XsltSettings settings;
        XmlUrlResolver resolver;
        ISite site;
        XmlCache model;
        XmlDocument doc;
        XmlDocument xsltdoc;
        bool showFileStrip = true;
        string defaultSSResource = "XmlNotepad.DefaultSS.xslt";
        IDictionary<Uri, bool> trusted = new Dictionary<Uri, bool>();
        int stripHeight;
        string html;

        public XsltViewer() {
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            InitializeComponent();

            stripHeight = this.WebBrowser1.Top;

            xslt = new XslCompiledTransform();
            settings = new XsltSettings(true, true);
            resolver = new XmlUrlResolver();

            toolTip1.SetToolTip(this.BrowseButton, SR.BrowseButtonTooltip);
            toolTip1.SetToolTip(this.SourceFileName, SR.XslFileNameTooltip);
            toolTip1.SetToolTip(this.TransformButton, SR.TransformButtonTooltip);
            toolTip1.SetToolTip(this.OutputFileName, SR.XslOutputFileNameTooltip);

            BrowseButton.Click += new EventHandler(BrowseButton_Click);
            this.SourceFileName.KeyDown += new KeyEventHandler(OnSourceFileNameKeyDown);
            this.OutputFileName.KeyDown += new KeyEventHandler(OnOutputFileNameKeyDown);

            this.WebBrowser1.ScriptErrorsSuppressed = true;
            this.WebBrowser1.WebBrowserShortcutsEnabled = true;

            TransformButton.SizeChanged += TransformButton_SizeChanged;
        }

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

        public string DefaultStylesheetResource {
            get { return this.defaultSSResource; }
            set { this.defaultSSResource = value; }
        }

        public bool DisableOutputFile { get; set; }

        public bool ShowFileStrip {
            get { return this.showFileStrip; }
            set {
                if (value != this.showFileStrip) {
                    this.showFileStrip = value;
                    if (value) {
                        AnchorStyles saved = this.WebBrowser1.Anchor;
                        this.WebBrowser1.Anchor = AnchorStyles.None;
                        this.WebBrowser1.Location = new Point(0, stripHeight);
                        this.WebBrowser1.Height = this.Height - stripHeight;
                        this.WebBrowser1.Anchor = saved;
                        this.panel1.Controls.Remove(this.tableLayoutPanel1);
                        this.panel1.Controls.SetChildIndex(this.tableLayoutPanel1, 0);
                    } else {
                        AnchorStyles saved = this.WebBrowser1.Anchor;
                        this.WebBrowser1.Location = new Point(0, 0);
                        this.WebBrowser1.Height = this.Height;
                        this.WebBrowser1.Anchor = saved;
                        this.panel1.Controls.Add(this.tableLayoutPanel1);
                    }
                    this.TransformButton.Visible = value;
                    this.BrowseButton.Visible = value;
                    this.SourceFileName.Visible = value;
                    this.OutputFileName.Visible = value;
                }
            }
        }

        void OnSourceFileNameKeyDown(object sender, KeyEventArgs e) {
            this.OutputFileName.Text = ""; // need to recompute this then...
            if (e.KeyCode == Keys.Enter) {
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


        XslCompiledTransform GetDefaultStylesheet() {
            if (defaultss != null) {
                return defaultss;
            }
            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream(this.defaultSSResource)) {
                if (null != stream) {
                    using (XmlReader reader = XmlReader.Create(stream)) {
                        XslCompiledTransform t = new XslCompiledTransform();
                        t.Load(reader);
                        defaultss = t;
                    }
                    // the XSLT DOM is also handy to have around for GetOutputMethod
                    stream.Seek(0, SeekOrigin.Begin);
                    this.xsltdoc = new XmlDocument();
                    this.xsltdoc.Load(stream);
                }
            }
            return defaultss;
        }
        protected override void OnPaint(PaintEventArgs e) {
            if (this.showFileStrip && this.WebBrowser1.Top > 0 && this.Width > 0) {
                Graphics g = e.Graphics;
                Rectangle r = new Rectangle(0, 0, this.Width, this.WebBrowser1.Top);
                Color c1 = Color.FromArgb(250, 249, 245);
                Color c2 = Color.FromArgb(192, 192, 168);
                Color s1 = SystemColors.ControlLight;
                using (LinearGradientBrush brush = new LinearGradientBrush(r, c1, c2, LinearGradientMode.Vertical)) {
                    g.FillRectangle(brush, r);
                }
            }
        }

        public void SetSite(ISite site) {
            this.site = site;
            IServiceProvider sp = (IServiceProvider)site;
            this.resolver = new XmlProxyResolver(sp);
            this.model = (XmlCache)site.GetService(typeof(XmlCache));
            this.model.ModelChanged -= new EventHandler<ModelChangedEventArgs>(OnModelChanged);
            this.model.ModelChanged += new EventHandler<ModelChangedEventArgs>(OnModelChanged);
        }

        void OnModelChanged(object sender, ModelChangedEventArgs e) {
            OnModelChanged();
        }

        void OnModelChanged() {
            this.doc = model.Document;
            try {
                if (!string.IsNullOrEmpty(model.FileName)) {
                    var uri = new Uri(model.FileName);
                    if (uri != this.baseUri)
                    {
                        this.baseUri = uri;
                        this.OutputFileName.Text = ""; // reset it since the file type might need to change...
                    }
                }
                this.SourceFileName.Text = model.XsltFileName;
            } catch (Exception) {
            }
        }

        Uri GetBaseUri() {
            if (this.baseUri == null) {
                OnModelChanged();
                if (this.baseUri == null) {
                    this.baseUri = new Uri(Application.StartupPath + "/");
                }
            }
            return this.baseUri;
        }

        protected override void OnLayout(LayoutEventArgs e) {
            base.OnLayout(e);
            if (showFileStrip) {
                this.WebBrowser1.Top = this.tableLayoutPanel1.Bottom;
                this.WebBrowser1.Height = this.Height - this.tableLayoutPanel1.Height;
            } else {
                this.WebBrowser1.Top = 0;
                this.WebBrowser1.Height = this.Height;
            }
        }

        public void DisplayXsltResults() {
            DisplayXsltResults(doc);
        }

        public void DisplayXsltResults(XmlDocument context) {

            Uri resolved = null;
            try {
                XslCompiledTransform transform;
                string path = null;
                if (this.showFileStrip) {
                    path = this.SourceFileName.Text.Trim();
                }
                string outpath = null;
                if (this.showFileStrip)
                {
                    outpath = this.OutputFileName.Text.Trim();
                }
                if (string.IsNullOrEmpty(path)) {
                    transform = GetDefaultStylesheet();
                } else {
                    resolved = new Uri(baseUri, path);
                    if (resolved != this.xsltUri || IsModified()) {
                        this.loaded = DateTime.Now;
                        settings.EnableScript = (trusted.ContainsKey(resolved));
                        XmlReaderSettings rs = new XmlReaderSettings();
                        rs.DtdProcessing = model.GetSettingBoolean("IgnoreDTD") ? DtdProcessing.Ignore : DtdProcessing.Parse;
                        rs.XmlResolver = resolver;
                        using (XmlReader r = XmlReader.Create(resolved.AbsoluteUri, rs)) {
                            xslt.Load(r, settings, resolver);
                        }

                        // the XSLT DOM is also handy to have around for GetOutputMethod
                        this.xsltdoc = new XmlDocument();
                        this.xsltdoc.Load(resolved.AbsoluteUri);
                    }

                    transform = xslt;
                }

                var method = GetOutputMethod(this.xsltdoc);

                if (string.IsNullOrEmpty(outpath) && !DisableOutputFile)
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        // pick a good default filename ... this means we need to know the <xsl:output method> and unfortunately 
                        // XslCompiledTransform doesn't give us that so we need to get it outselves.
                        
                        string ext = ".xml";
                        if (method.ToLower() == "html")
                        {
                            ext = ".htm";
                        }
                        else if (method.ToLower() == "text")
                        {
                            ext = ".txt";
                        }
                        outpath = Path.GetFileNameWithoutExtension(path) + "_output" + ext;

                        var safeUri = GetWritableBaseUri(outpath);
                        if (safeUri != this.baseUri)
                        {
                            this.OutputFileName.Text = new Uri(safeUri, outpath).LocalPath;
                        }
                        else
                        {
                            this.OutputFileName.Text = outpath;
                        }
                        outpath = new Uri(safeUri, outpath).LocalPath;
                    }
                }
                else if (!string.IsNullOrEmpty(outpath))
                {
                    outpath = new Uri(baseUri, outpath).LocalPath;
                }

                if (null != transform)
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.XmlResolver = new XmlProxyResolver(this.site);
                    settings.DtdProcessing = model.GetSettingBoolean("IgnoreDTD") ? DtdProcessing.Ignore : DtdProcessing.Parse;
                    var xmlReader = XmlIncludeReader.CreateIncludeReader(context, settings, GetBaseUri().AbsoluteUri);
                    if (DisableOutputFile || string.IsNullOrEmpty(outpath))
                    {
                        StringWriter writer = new StringWriter();
                        transform.Transform(xmlReader, null, writer);
                        this.xsltUri = resolved;
                        Display(writer.ToString());
                    }
                    else
                    {
                        bool noBom = false;
                        Settings appSettings = (Settings)this.site.GetService(typeof(Settings));
                        if (appSettings != null)
                        {
                            noBom = (bool)appSettings["NoByteOrderMark"];
                        }
                        if (noBom)
                        {
                            // cache to an inmemory stream so we can strip the BOM.
                            MemoryStream ms = new MemoryStream();
                            transform.Transform(xmlReader, null, ms);

                            ms.Seek(0, SeekOrigin.Begin);
                            Utilities.WriteFileWithoutBOM(ms, outpath);
                        }
                        else
                        {
                            using (FileStream writer = new FileStream(outpath, FileMode.OpenOrCreate, FileAccess.Write))
                            {
                                transform.Transform(xmlReader, null, writer);
                                this.xsltUri = resolved;
                            }
                        }

                        DisplayFile(outpath);
                    }
                }
            } catch (System.Xml.Xsl.XsltException x) {
                if (x.Message.Contains("XsltSettings")) {
                    if (!trusted.ContainsKey(resolved) &&
                        MessageBox.Show(this, SR.XslScriptCodePrompt, SR.XslScriptCodeCaption,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes) {
                        trusted[resolved] = true;
                        DisplayXsltResults();
                        return;
                    }
                }
                WriteError(x);
            } catch (Exception x) {
                WriteError(x);
            }
        }

        private Uri GetWritableBaseUri(string fileName)
        {
            if (this.baseUri.Scheme != "file")
            {
                return new Uri(Path.GetTempPath());
            }

            string testPath = new Uri(this.baseUri, fileName).LocalPath;

            try
            {
                using (FileStream fs = new FileStream(testPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    fs.Write(new byte[] { 0 }, 0, 1);
                }
                // we created the file then!
            }
            catch
            {
                // We don't have write permissions
                return new Uri(Path.GetTempPath());
            }
            return this.baseUri;
        }

        string GetOutputMethod(XmlDocument xsltdoc)
        {
            var ns = xsltdoc.DocumentElement.NamespaceURI;
            string method = "xml"; // the default.
            var mgr = new XmlNamespaceManager(xsltdoc.NameTable);
            mgr.AddNamespace("xsl", ns);
            XmlElement e = (XmlElement)xsltdoc.SelectSingleNode("//xsl:ouput", mgr);
            if (e != null)
            {
                var specifiedMethod = e.GetAttribute("method");
                if (!string.IsNullOrEmpty(specifiedMethod))
                {
                    return specifiedMethod;
                }
            }

            // then we need to figure out the default method which is xml unless there's an html element here
            foreach(XmlNode node in xsltdoc.DocumentElement.ChildNodes)
            {
                if (node is XmlElement child)
                {
                    if (string.IsNullOrEmpty(child.NamespaceURI) && string.Compare(child.LocalName, "html", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return "html";
                    }
                    else
                    {
                        // might be an <xsl:template> so look inside these too...
                        foreach (XmlNode subnode in child.ChildNodes)
                        {
                            if (subnode is XmlElement grandchild)
                            {
                                if (string.IsNullOrEmpty(grandchild.NamespaceURI) && string.Compare(grandchild.LocalName, "html", StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    return "html";
                                }
                            }
                        }
                    }
                }
            }
            return method;
        }

        bool IsModified() {
            if (this.xsltUri.IsFile) {
                string path = this.xsltUri.LocalPath;
                DateTime lastWrite = File.GetLastWriteTime(path);
                return this.loaded < lastWrite;
            }
            return false;
        }

        private void WriteError(Exception e) {
            StringWriter writer = new StringWriter();
            writer.WriteLine("<html><body><h3>");
            writer.WriteLine(SR.TransformErrorCaption);
            writer.WriteLine("</h3></body></html>");
            while (e != null) {
                writer.WriteLine(e.Message);
                e = e.InnerException;
            }
            Display(writer.ToString());
        }

        private void Display(string content) {
            if (content != this.html) {
                this.WebBrowser1.DocumentText = content;
                this.html = content;
            }
        }

        private void DisplayFile(string filename)
        {
            this.html = null;
            this.WebBrowser1.Url = new Uri(filename);
        }

        private void BrowseButton_Click(object sender, EventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = SR.XSLFileFilter;
            if (ofd.ShowDialog(this) == DialogResult.OK) {
                this.SourceFileName.Text = ofd.FileName;
            }
        }

        private void TransformButton_Click(object sender, EventArgs e) {
            this.html = null; // force update of html
            this.DisplayXsltResults();
        }

        private Guid cmdGuid = new Guid("ED016940-BD5B-11CF-BA4E-00C04FD70816");

        private enum OLECMDEXECOPT {
            OLECMDEXECOPT_DODEFAULT         = 0,
            OLECMDEXECOPT_PROMPTUSER        = 1,
            OLECMDEXECOPT_DONTPROMPTUSER    = 2,
            OLECMDEXECOPT_SHOWHELP          = 3
        }

        private enum MiscCommandTarget {
            Find = 1,
            ViewSource,
            Options
        }
	
        private mshtml.HTMLDocument GetDocument() {
            try {
                mshtml.HTMLDocument htm = (mshtml.HTMLDocument)this.WebBrowser1.Document.DomDocument;
                return htm;
            } catch {
                throw (new Exception("Cannot retrieve the document from the WebBrowser control"));
            }
        }

        public void ViewSource() {
            IOleCommandTarget cmdt;
            Object o = new object();
            try {
                cmdt = (IOleCommandTarget)GetDocument();
                cmdt.Exec(ref cmdGuid, (uint)MiscCommandTarget.ViewSource,
                (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, ref o, ref o);
            } catch (Exception e) {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
        }

        public void Find() {
            IOleCommandTarget cmdt;
            Object o = new object();
            try {
                cmdt = (IOleCommandTarget)GetDocument();
                cmdt.Exec(ref cmdGuid, (uint)MiscCommandTarget.Find,
                (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, ref o, ref o);
            } catch (Exception e) {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
        }

    }

    [CLSCompliant(false), StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct OLECMDTEXT {
        public uint cmdtextf;
        public uint cwActual;
        public uint cwBuf;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public char rgwz;
    }

    [CLSCompliant(false), StructLayout(LayoutKind.Sequential)]
    public struct OLECMD {
        public uint cmdID;
        public uint cmdf;
    }

    // Interop definition for IOleCommandTarget. 
    [CLSCompliant(false), ComImport, Guid("b722bccb-4e68-101b-a2bc-00aa00404770"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IOleCommandTarget {
        //IMPORTANT: The order of the methods is critical here. You
        //perform early binding in most cases, so the order of the methods
        //here MUST match the order of their vtable layout (which is determined
        //by their layout in IDL). The interop calls key off the vtable ordering,
        //not the symbolic names. Therefore, if you switched these method declarations
        //and tried to call the Exec method on an IOleCommandTarget interface from your
        //application, it would translate into a call to the QueryStatus method instead.
        void QueryStatus(ref Guid pguidCmdGroup, UInt32 cCmds,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] OLECMD[] prgCmds, ref OLECMDTEXT CmdText);
        void Exec(ref Guid pguidCmdGroup, uint nCmdId, uint nCmdExecOpt, ref object pvaIn, ref object pvaOut);
    }

}