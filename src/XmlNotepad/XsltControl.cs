using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
using SR = XmlNotepad.StringResources;

namespace XmlNotepad
{
    public partial class XsltControl : UserControl
    {
        Stopwatch urlWatch = new Stopwatch();
        string html;
        DateTime loaded;
        Uri baseUri;
        PerformanceInfo info = null;
        XslCompiledTransform xslt;
        XmlDocument xsltdoc;
        XslCompiledTransform defaultss;
        Uri xsltUri;
        XsltSettings settings;
        ISite site;
        XmlUrlResolver resolver;
        string defaultSSResource = "XmlNotepad.DefaultSS.xslt";
        IDictionary<Uri, bool> trusted = new Dictionary<Uri, bool>();
        bool webInitialized;
        string tempFile;

        public XsltControl()
        {
            InitializeComponent();
            settings = new XsltSettings(true, true);
            resolver = new XmlUrlResolver();
            this.webBrowser2.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;
            this.Load += XsltControl_Load;
        }

        /// <summary>
        /// Performs in-memory xslt transform only.  Note that file based transforms
        /// work better if you want local includes to work with that file (css, images, etc).
        /// </summary>
        public bool DisableOutputFile { get; set; }

        public void OnClosed()
        {
            CleanupTempFile();
        }

        void CleanupTempFile() 
        { 
            if (!string.IsNullOrEmpty(tempFile) && File.Exists(tempFile))
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch { }
            }
            this.tempFile = null;
        }

        private async void XsltControl_Load(object sender, EventArgs e)
        {
            try
            {
                CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions()
                {
                    AllowSingleSignOnUsingOSPrimaryAccount = true
                };
                CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(userDataFolder: WebViewUserCache, options: options);
                await this.webBrowser2.EnsureCoreWebView2Async(environment);
            } catch
            {
                // fall back on old web browser control
                WebBrowserFallback();
            }
        }

        public virtual string WebViewUserCache
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                Debug.Assert(!string.IsNullOrEmpty(path));
                return System.IO.Path.Combine(path, "Microsoft", "Xml Notepad", "WebView2Cache");
            }
        }

        private void OnWebDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (LoadCompleted != null && this.info != null)
            {
                this.info.BrowserMilliseconds = this.urlWatch.ElapsedMilliseconds;
                LoadCompleted(this, this.info);
            }
        }

        private void OnCoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (this.webBrowser2.CoreWebView2 != null)
            {
                this.webBrowser2.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
                this.webBrowser2.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                this.webBrowser2.Visible = true;
                this.webBrowser1.Visible = false;
                this.webBrowser2.ZoomFactor = 1.25;
            }
            else
            {
                WebBrowserFallback();
            }
            webInitialized = true;
        }

        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess && !DisableOutputFile)
            {
                Debug.WriteLine(e.WebErrorStatus);
            }
        }

        void WebBrowserFallback()
        {
            // WebView2 is not installed, so fall back on the old WebBrowser component.
            this.webBrowser2.Visible = false;
            this.webBrowser1.ScriptErrorsSuppressed = true;
            this.webBrowser1.WebBrowserShortcutsEnabled = true;
            this.webBrowser1.DocumentCompleted += OnWebDocumentCompleted;

            this.webBrowser1.Visible = true;
            this.webInitialized = true; // no callback from this guy.
        }

        private void CoreWebView2_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            if (LoadCompleted != null && this.info != null)
            {
                this.info.BrowserMilliseconds = this.urlWatch.ElapsedMilliseconds;
                LoadCompleted(this, this.info);
            }
        }

        public event EventHandler<PerformanceInfo> LoadCompleted;

        public void DisplayFile(string filename)
        {
            this.html = null;
            urlWatch.Reset();
            urlWatch.Start();
            if (this.webBrowser1.Visible)
            {
                this.webBrowser1.Navigate(filename);
            }
            else
            {
                var uri = new Uri(filename);
                if (this.webBrowser2.Source == uri)
                {
                    this.webBrowser2.Reload();
                }
                else
                {
                    this.webBrowser2.Source = uri;
                }
            }
        }

        public Uri BaseUri
        {
            get { return this.baseUri; }
            set { this.baseUri = value; }
        }

        Uri GetBaseUri()
        {
            if (this.baseUri == null)
            {   
                this.baseUri = new Uri(Application.StartupPath + "/");
            }

            return this.baseUri;
        }

        public Control VisibleBrowser
        {
            get
            {
                return this.webBrowser1.Visible ? (Control)this.webBrowser1 : (Control)this.webBrowser2;
            }
        }

        private void Display(string content)
        {
            CleanupTempFile();
            if (content != this.html && webInitialized)
            {
                urlWatch.Reset();
                urlWatch.Start();
                if (this.webBrowser1.Visible)
                {
                    this.webBrowser1.DocumentText = content;
                }
                else
                {
                    if (content.Length > 1000000)
                    {
                        content = content.Substring(0, 1000000) + "<h1>content truncated...";
                    }
                    // this has a 1mb limit for some unknown reason.
                    this.webBrowser2.NavigateToString(content);
                }
                this.html = content;
            }
        }

        public bool IgnoreDTD { get; set; }

        public string DefaultStylesheetResource
        {
            get { return this.defaultSSResource; }
            set { this.defaultSSResource = value; }
        }

        public void SetSite(ISite site)
        {
            this.site = site;
            IServiceProvider sp = (IServiceProvider)site;
            this.resolver = new XmlProxyResolver(sp);
        }

        /// <summary>
        /// Run an XSLT transform and show the results.
        /// </summary>
        /// <param name="context">The document to transform</param>
        /// <param name="xsltfilename">The xslt file to use</param>
        /// <param name="outpath">Output file name hint.</param>
        /// <returns>The output file name or null if DisableOutputFile is true</returns>
        public string DisplayXsltResults(XmlDocument context, string xsltfilename, string outpath = null)
        {
            this.CleanupTempFile();
            Uri resolved = null;
            try
            {
                XslCompiledTransform transform;
                if (string.IsNullOrEmpty(xsltfilename))
                {
                    transform = GetDefaultStylesheet();
                }
                else
                {
                    resolved = new Uri(baseUri, xsltfilename);
                    if (resolved != this.xsltUri || IsModified())
                    {
                        xslt = new XslCompiledTransform();
                        this.loaded = DateTime.Now;
                        settings.EnableScript = (trusted.ContainsKey(resolved));
                        XmlReaderSettings rs = new XmlReaderSettings();
                        rs.DtdProcessing = this.IgnoreDTD ? DtdProcessing.Ignore : DtdProcessing.Parse;
                        rs.XmlResolver = resolver;
                        using (XmlReader r = XmlReader.Create(resolved.AbsoluteUri, rs))
                        {
                            xslt.Load(r, settings, resolver);
                        }

                        // the XSLT DOM is also handy to have around for GetOutputMethod
                        this.xsltdoc = new XmlDocument();
                        this.xsltdoc.Load(resolved.AbsoluteUri);
                    }
                    transform = xslt;
                }

                if (string.IsNullOrEmpty(outpath))
                {
                    if (!DisableOutputFile)
                    {
                        if (!string.IsNullOrEmpty(xsltfilename))
                        {
                            // pick a good default filename ... this means we need to know the <xsl:output method> and unfortunately 
                            // XslCompiledTransform doesn't give us that so we need to get it outselves.

                            var ext = GetDefaultOutputExtension();
                            outpath = Path.GetFileNameWithoutExtension(xsltfilename) + "_output" + ext;

                            var safeUri = GetWritableBaseUri(outpath);
                            this.tempFile = outpath = new Uri(safeUri, outpath).LocalPath;
                        }
                        else
                        {
                            // default stylesheet produces html
                            var safeUri = new Uri(Path.GetTempPath());
                            outpath = new Uri(safeUri, "xmlnotepaddefaultoutput.htm").LocalPath;
                            this.tempFile = outpath;
                        }
                    }
                }
                else
                {
                    var safeUri = GetWritableBaseUri(outpath);
                    this.tempFile = outpath = new Uri(safeUri, outpath).LocalPath;
                }

                if (null != transform)
                {
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.XmlResolver = new XmlProxyResolver(this.site);
                    settings.DtdProcessing = this.IgnoreDTD ? DtdProcessing.Ignore : DtdProcessing.Parse;
                    var xmlReader = XmlIncludeReader.CreateIncludeReader(context, settings, GetBaseUri().AbsoluteUri);
                    if (string.IsNullOrEmpty(outpath))
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
                                Stopwatch watch = new Stopwatch();
                                watch.Start();
                                transform.Transform(xmlReader, null, writer);
                                watch.Stop();
                                this.info = new PerformanceInfo();
                                this.info.XsltMilliseconds = watch.ElapsedMilliseconds;
                                Debug.WriteLine("Transform in {0} milliseconds", watch.ElapsedMilliseconds);
                                this.xsltUri = resolved;
                            }
                        }

                        DisplayFile(outpath);
                    }
                }
            }
            catch (System.Xml.Xsl.XsltException x)
            {
                if (x.Message.Contains("XsltSettings"))
                {
                    if (!trusted.ContainsKey(resolved) &&
                        MessageBox.Show(this, SR.XslScriptCodePrompt, SR.XslScriptCodeCaption,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                    {
                        trusted[resolved] = true;
                        return DisplayXsltResults(context, xsltfilename, outpath);
                    }
                }
                WriteError(x);
            }
            catch (Exception x)
            {
                WriteError(x);
            }

            return outpath;
        }

        private Uri GetWritableBaseUri(string fileName)
        {
            try
            {
                Uri test = new Uri(fileName, UriKind.RelativeOrAbsolute);
                if (test.IsAbsoluteUri && test.Scheme == "file")
                {
                    string dir = Path.GetDirectoryName(fileName);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    return test;
                }
            } catch (Exception) { }

            if (this.baseUri.Scheme != "file")
            {
                return new Uri(Path.GetTempPath());
            }

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "__temp_xslt_test";
            }

            string testPath = new Uri(this.baseUri, fileName).LocalPath;

            try
            {
                using (FileStream fs = new FileStream(testPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    fs.Write(new byte[] { 0 }, 0, 1);
                }

                // we can create the file then!
                File.Delete(testPath);
            }
            catch
            {
                // We don't have write permissions
                return new Uri(Path.GetTempPath());
            }

            return this.baseUri;
        }

        public string GetOutputFileFilter(string customFileName = null)
        {
            // return something like this:
            // XML files (*.xml)|*.xml|XSL files (*.xsl)|*.xsl|XSD files (*.xsd)|*.xsd|All files (*.*)|*.*
            var ext = GetDefaultOutputExtension(customFileName);
            switch (ext)
            {
                case ".xml":
                    return "XML files(*.xml) | *.xml|All files (*.*)|*.*";
                case ".htm":
                    return "HTML files(*.htm;*.html) | *.htm;*.html|All files (*.*)|*.*";
                default:
                    return "Text files(*.txt) | *.txt|All files (*.*)|*.*";
            }
        }


        public string GetDefaultOutputExtension(string customFileName = null)
        {
            string ext = ".xml";
            try
            {
                if (this.xsltdoc == null)
                {
                    string path = customFileName;
                    var resolved = new Uri(baseUri, path);
                    this.xsltdoc = new XmlDocument();
                    this.xsltdoc.Load(resolved.AbsoluteUri);
                }
                var method = GetOutputMethod(this.xsltdoc);
                if (method.ToLower() == "html")
                {
                    ext = ".htm";
                }
                else if (method.ToLower() == "text")
                {
                    ext = ".txt";
                }
            }
            catch (Exception)
            {
            }
            return ext;
        }

        string GetOutputMethod(XmlDocument xsltdoc)
        {
            var ns = xsltdoc.DocumentElement.NamespaceURI;
            string method = "xml"; // the default.
            var mgr = new XmlNamespaceManager(xsltdoc.NameTable);
            mgr.AddNamespace("xsl", ns);
            XmlElement e = xsltdoc.SelectSingleNode("//xsl:output", mgr) as XmlElement;
            if (e != null)
            {
                var specifiedMethod = e.GetAttribute("method");
                if (!string.IsNullOrEmpty(specifiedMethod))
                {
                    return specifiedMethod;
                }
            }

            // then we need to figure out the default method which is xml unless there's an html element here
            foreach (XmlNode node in xsltdoc.DocumentElement.ChildNodes)
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

        private void WriteError(Exception e)
        {
            StringWriter writer = new StringWriter();
            writer.WriteLine("<html><body><h3>");
            writer.WriteLine(SR.TransformErrorCaption);
            writer.WriteLine("</h3></body></html>");
            while (e != null)
            {
                writer.WriteLine(e.Message);
                e = e.InnerException;
            }
            Display(writer.ToString());
        }

        XslCompiledTransform GetDefaultStylesheet()
        {
            if (defaultss != null)
            {
                return defaultss;
            }
            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream(this.defaultSSResource))
            {
                if (null != stream)
                {
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        XslCompiledTransform t = new XslCompiledTransform();
                        t.Load(reader);
                        defaultss = t;
                    }
                    // the XSLT DOM is also handy to have around for GetOutputMethod
                    stream.Seek(0, SeekOrigin.Begin);
                    this.xsltdoc = new XmlDocument();
                    this.xsltdoc.Load(stream);
                }
                else
                {
                    throw new Exception(string.Format("You have a build problem: resource '{0} not found", this.defaultSSResource));                    
                }
            }
            return defaultss;
        }

        bool IsModified()
        {
            if (this.xsltUri.IsFile)
            {
                string path = this.xsltUri.LocalPath;
                DateTime lastWrite = File.GetLastWriteTime(path);
                return this.loaded < lastWrite;
            }
            return false;
        }


        private Guid cmdGuid = new Guid("ED016940-BD5B-11CF-BA4E-00C04FD70816");

        private enum OLECMDEXECOPT
        {
#pragma warning disable CA1712 // Do not prefix enum values with type name
            OLECMDEXECOPT_DODEFAULT = 0,
            OLECMDEXECOPT_PROMPTUSER = 1,
            OLECMDEXECOPT_DONTPROMPTUSER = 2,
            OLECMDEXECOPT_SHOWHELP = 3
#pragma warning restore CA1712 // Do not prefix enum values with type name
        }

        private enum MiscCommandTarget
        {
            Find = 1,
            ViewSource,
            Options
        }

        private mshtml.HTMLDocument GetDocument()
        {
            try
            {
                mshtml.HTMLDocument htm = (mshtml.HTMLDocument)this.webBrowser1.Document.DomDocument;
                return htm;
            }
            catch
            {
                throw (new Exception("Cannot retrieve the document from the WebBrowser control"));
            }
        }

        public void ViewSource()
        {
            IOleCommandTarget cmdt;
            Object o = new object();
            try
            {
                cmdt = (IOleCommandTarget)GetDocument();
                cmdt.Exec(ref cmdGuid, (uint)MiscCommandTarget.ViewSource,
                (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, ref o, ref o);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
        }

        public void Find()
        {
            IOleCommandTarget cmdt;
            Object o = new object();
            try
            {
                cmdt = (IOleCommandTarget)GetDocument();
                cmdt.Exec(ref cmdGuid, (uint)MiscCommandTarget.Find,
                (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, ref o, ref o);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct OLECMDTEXT
        {
            public uint cmdtextf;
            public uint cwActual;
            public uint cwBuf;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
            public char rgwz;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct OLECMD
        {
            public uint cmdID;
            public uint cmdf;
        }

        // Interop definition for IOleCommandTarget. 
        [ComImport, Guid("b722bccb-4e68-101b-a2bc-00aa00404770"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleCommandTarget
        {
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
}