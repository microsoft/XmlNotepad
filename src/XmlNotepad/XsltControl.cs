using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
using SR = XmlNotepad.StringResources;

namespace XmlNotepad
{
    public class WebView2Exception : Exception
    {
        public WebView2Exception(string msg) : base(msg) { }
    }

    public partial class XsltControl : UserControl
    {
        private readonly Stopwatch _urlWatch = new Stopwatch();
        private string _html;
        private string _fileName;
        private DateTime _loaded;
        private Uri _baseUri;
        private PerformanceInfo _info = null;
        private XslCompiledTransform _xslt;
        private XmlDocument _xsltdoc;
        private XslCompiledTransform _defaultss;
        private Uri _xsltUri;
        private ISite _site;
        private XmlUrlResolver _resolver;
        private Settings _settings;
        private string _defaultSSResource = "XmlNotepad.DefaultSS.xslt";
        private readonly IDictionary<Uri, bool> _trusted = new Dictionary<Uri, bool>();
        private bool _webInitialized;
        private bool _webView2Supported;
        private string _tempFile;
        private string _previousOutputFile;
        private bool _usingDefaultXslt;
        private bool _hasXsltOutput; // whether DOM has <?xsl-output instruction.

        public event EventHandler<Exception> WebBrowserException;

        public event EventHandler<PerformanceInfo> LoadCompleted;

        public XsltControl()
        {
            InitializeComponent();
            _resolver = new XmlUrlResolver();
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
            if (!string.IsNullOrEmpty(_tempFile) && File.Exists(_tempFile))
            {
                try
                {
                    File.Delete(_tempFile);
                }
                catch { }
            }
            this._tempFile = null;
        }

        private async void InitializeBrowser(string version)
        {
            try
            {
                this.BrowserVersion = version;
                if (version == "WebView2")
                {
                    if (!this._webView2Supported)
                    {
                        this.webBrowser2.CoreWebView2InitializationCompleted -= OnCoreWebView2InitializationCompleted;
                        this.webBrowser2.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;
                        CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions()
                        {
                            AllowSingleSignOnUsingOSPrimaryAccount = true
                        };
                        CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(userDataFolder: WebViewUserCache, options: options);
                        await this.webBrowser2.EnsureCoreWebView2Async(environment);
                    }
                }
                else
                {
                    WebBrowserFallback();
                }

                Reload();
            }
            catch (Exception ex)
            {
                // fall back on old web browser control
                RaiseBrowserException(new WebView2Exception(ex.Message));
                this.BrowserVersion = "WebBrowser";
                this._settings["BrowserVersion"] = "WebBrowser";
                WebBrowserFallback();
                this._settings["WebView2Exception"] = ex.Message;
            }
        }

        private void Reload()
        {
            if (!string.IsNullOrEmpty(this._fileName))
            {
                DisplayFile(_fileName);
            }
            else if (!string.IsNullOrEmpty(_html))
            {
                Display(_html);
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
            if (LoadCompleted != null && this._info != null)
            {
                this._info.BrowserMilliseconds = this._urlWatch.ElapsedMilliseconds;
                this._info.BrowserName = this.webBrowser1.Visible ? "WebBrowser" : "WebView2";
                LoadCompleted(this, this._info);
            }
        }

        private void OnCoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                if (e.InitializationException != null)
                {
                    // save this error someplace so we can show it to the user later when they try and enable WebView2.
                    RaiseBrowserException(new WebView2Exception(e.InitializationException.Message));
                }
            }
            if (this.webBrowser2.CoreWebView2 != null)
            {
                this.webBrowser2.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
                this.webBrowser2.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                this.webBrowser2.Visible = true;
                this.webBrowser1.Visible = false;
                this._webView2Supported = true;
            }
            else
            {
                WebBrowserFallback();
            }
            _webInitialized = true;
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
            this.webBrowser1.Visible = true;
            this.webBrowser1.ScriptErrorsSuppressed = true;
            this.webBrowser1.WebBrowserShortcutsEnabled = true;
            this.webBrowser1.DocumentCompleted += OnWebDocumentCompleted;
            this._webInitialized = true; // no callback from this guy.
        }

        private void CoreWebView2_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            if (LoadCompleted != null && this._info != null)
            {
                this._info.BrowserMilliseconds = this._urlWatch.ElapsedMilliseconds;
                this._info.BrowserName = this.webBrowser1.Visible ? "WebBrowser" : "WebView2";
                LoadCompleted(this, this._info);
            }
        }

        internal void DeletePreviousOutput()
        {
            if (!string.IsNullOrEmpty(this._previousOutputFile) && this._tempFile != this._previousOutputFile)
            {
                if (File.Exists(this._previousOutputFile))
                {
                    try
                    {
                        File.Delete(this._previousOutputFile);
                        this._previousOutputFile = null;
                    }
                    catch { }
                }
            }
        }

        private bool UseWebView2()
        {
            return this._webView2Supported && this.BrowserVersion == "WebView2";
        }

        public void DisplayFile(string filename)
        {
            if (!this._webInitialized)
            {
                return;
            }
            this._html = null;
            this._fileName = filename;
            _urlWatch.Reset();
            _urlWatch.Start();

            this.webBrowser2.Visible = UseWebView2();
            this.webBrowser1.Visible = !UseWebView2();

            if (UseWebView2())
            {
                var uri = new Uri(filename);
                try
                {
                    if (this.webBrowser2.Source == uri)
                    {
                        this.webBrowser2.Reload();
                    }
                    else
                    {
                        this.webBrowser2.Source = uri;
                    }
                }
                catch (Exception e)
                {
                    RaiseBrowserException(e);
                    // revert, did user uninstall WebView2?
                    this._webView2Supported = false;
                    WebBrowserFallback();
                }
            }

            // fallback on webbrowser 1
            if (this.webBrowser1.Visible)
            {
                try
                {
                    this.webBrowser1.Navigate(filename);
                }
                catch (Exception e)
                {
                    // tell the user?
                    RaiseBrowserException(e);
                }
            }
        }

        public Uri BaseUri
        {
            get { return this._baseUri; }
            set { this._baseUri = value; }
        }

        Uri GetBaseUri()
        {
            if (this._baseUri == null)
            {
                this._baseUri = new Uri(Application.StartupPath + "/");
            }

            return this._baseUri;
        }

        private void Display(string content)
        {
            CleanupTempFile();
            if (content != this._html && _webInitialized)
            {
                _urlWatch.Reset();
                _urlWatch.Start();

                this.webBrowser2.Visible = UseWebView2();
                this.webBrowser1.Visible = !UseWebView2();

                if (UseWebView2())
                {
                    try
                    {
                        if (content.Length > 1000000)
                        {
                            content = content.Substring(0, 1000000) + "<h1>content truncated...";
                        }
                        // this has a 1mb limit for some unknown reason.
                        this.webBrowser2.NavigateToString(content);
                        this._html = content;
                        this._fileName = null;
                        return;
                    }
                    catch (Exception e)
                    {
                        RaiseBrowserException(e);
                        // revert, did user uninstall WebView2?
                        this.webBrowser2.Visible = false;
                        this.webBrowser1.Visible = true;
                        this._webView2Supported = false;

                    }
                }
                // Fallback in case webBrowser2 fails.
                if (this.webBrowser1.Visible)
                {
                    try
                    {
                        this.webBrowser1.DocumentText = content;
                    }
                    catch (Exception e)
                    {
                        RaiseBrowserException(e);
                    }
                    this._html = content;
                    this._fileName = null;
                }
            }
        }

        private void RaiseBrowserException(Exception e)
        {
            if (WebBrowserException != null)
            {
                WebBrowserException(this, e);
            }
        }

        public bool IgnoreDTD { get; set; }

        public bool EnableScripts { get; set; }

        public string BrowserVersion { get; set; }

        public string DefaultStylesheetResource
        {
            get { return this._defaultSSResource; }
            set { this._defaultSSResource = value; }
        }

        public bool HasXsltOutput
        {
            get => _hasXsltOutput;
            set
            {
                if (value != _hasXsltOutput)
                {
                    _defaultss = null;
                    _hasXsltOutput = value;
                }
            }
        }

        public void SetSite(ISite site)
        {
            this._site = site;
            IServiceProvider sp = (IServiceProvider)site;
            this._resolver = new XmlProxyResolver(sp);
            this._settings = (Settings)sp.GetService(typeof(Settings));
            this._settings.Changed -= OnSettingsChanged;
            this._settings.Changed += OnSettingsChanged;

            // initial settings.
            this.IgnoreDTD = this._settings.GetBoolean("IgnoreDTD");
            this.EnableScripts = this._settings.GetBoolean("EnableXsltScripts");
            this.InitializeBrowser(this._settings.GetString("BrowserVersion"));
        }

        private void OnSettingsChanged(object sender, string name)
        {
            if (name == "IgnoreDTD")
            {
                this.IgnoreDTD = this._settings.GetBoolean("IgnoreDTD");
            }
            else if (name == "EnableXsltScripts")
            {
                this.EnableScripts = this._settings.GetBoolean("EnableXsltScripts");
            }
            else if (name == "BrowserVersion")
            {
                this.InitializeBrowser(this._settings.GetString("BrowserVersion"));
            }
            else if (name == "Font" || name == "Theme" || name == "Colors" || name == "LightColors" || name == "DarkColors")
            {
                _defaultss = null;
                if (this._usingDefaultXslt)
                {
                    string id = this.Handle.ToString(); // make sure action is unique to this control instance since we have 2!
                    _settings.DelayedActions.StartDelayedAction("Transform" + id, UpdateTransform, TimeSpan.FromMilliseconds(50));
                }
            }
        }

        private void UpdateTransform()
        {
            if (_previousTransform != null)
            {
                DisplayXsltResults(_previousTransform.document, _previousTransform.xsltfilename, _previousTransform.outpath, 
                    _previousTransform.userSpecifiedOutput);
            }
        }

        public Uri ResolveRelativePath(string filename)
        {
            try
            {
                return new Uri(_baseUri, filename);
            }
            catch
            {
                return null;
            }
        }

        class Context
        {
            public XmlDocument document;
            public string xsltfilename;
            public string outpath;
            public bool userSpecifiedOutput;
        }

        Context _previousTransform;

        /// <summary>
        /// Run an XSLT transform and show the results.
        /// </summary>
        /// <param name="context">The document to transform</param>
        /// <param name="xsltfilename">The xslt file to use</param>
        /// <param name="outpath">Output file name hint.</param>
        /// <param name="userSpecifiedOutput">Whether output name is non-negotiable.</param>
        /// <returns>The output file name or null if DisableOutputFile is true</returns>
        public string DisplayXsltResults(XmlDocument context, string xsltfilename, string outpath = null, bool userSpecifiedOutput = false)
        {
            if (!this._webInitialized)
            {
                return null;
            }

            _previousTransform = new Context()
            {
                document = context,
                xsltfilename = xsltfilename,
                outpath = outpath,
                userSpecifiedOutput = userSpecifiedOutput
            };

            this.CleanupTempFile();
            Uri resolved = null;
            try
            {
                XslCompiledTransform transform;
                if (string.IsNullOrEmpty(xsltfilename))
                {
                    transform = GetDefaultStylesheet();
                    this._usingDefaultXslt = true;
                    if (this._settings.GetBoolean("DisableDefaultXslt"))
                    {
                        context = new XmlDocument();
                        context.LoadXml("<Note>Default styling of your XML documents is disabled in your Options</Note>");
                    }
                }
                else
                {
                    resolved = new Uri(_baseUri, xsltfilename);
                    if (resolved != this._xsltUri || IsModified())
                    {
                        _xslt = new XslCompiledTransform();
                        this._loaded = DateTime.Now;
                        var settings = new XsltSettings(true, this.EnableScripts);
                        settings.EnableScript = (_trusted.ContainsKey(resolved));
                        var rs = new XmlReaderSettings();
                        rs.DtdProcessing = this.IgnoreDTD ? DtdProcessing.Ignore : DtdProcessing.Parse;
                        rs.XmlResolver = _resolver;
                        using (XmlReader r = XmlReader.Create(resolved.AbsoluteUri, rs))
                        {
                            _xslt.Load(r, settings, _resolver);
                        }

                        // the XSLT DOM is also handy to have around for GetOutputMethod
                        this._xsltdoc = new XmlDocument();
                        this._xsltdoc.Load(resolved.AbsoluteUri);
                    }
                    transform = _xslt;
                    this._usingDefaultXslt = false;
                }

                if (string.IsNullOrEmpty(outpath))
                {
                    if (!DisableOutputFile)
                    {
                        if (!string.IsNullOrEmpty(xsltfilename))
                        {
                            outpath = this.GetXsltOutputFileName(xsltfilename);
                        }
                        else
                        {
                            // default stylesheet produces html
                            this._tempFile = outpath = GetWritableFileName("DefaultXsltOutput.htm");
                        }
                    }
                }
                else if (!userSpecifiedOutput)
                {
                    var ext = GetDefaultOutputExtension();
                    var basePath = Path.Combine(Path.GetDirectoryName(outpath), Path.GetFileNameWithoutExtension(outpath));
                    outpath = basePath + ext;
                    outpath = GetWritableFileName(outpath);
                }
                else
                {
                    outpath = GetWritableFileName(outpath);
                }

                if (null != transform)
                {
                    var dir = Path.GetDirectoryName(outpath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    var settings = new XmlReaderSettings();
                    settings.XmlResolver = new XmlProxyResolver(this._site);
                    settings.DtdProcessing = this.IgnoreDTD ? DtdProcessing.Ignore : DtdProcessing.Parse;
                    var xmlReader = XmlIncludeReader.CreateIncludeReader(context, settings, GetBaseUri().AbsoluteUri);
                    if (string.IsNullOrEmpty(outpath))
                    {
                        using (StringWriter writer = new StringWriter())
                        {
                            transform.Transform(xmlReader, null, writer);
                            this._xsltUri = resolved;
                            Display(writer.ToString());
                        }
                    }
                    else
                    {
                        bool noBom = false;
                        Settings appSettings = (Settings)this._site.GetService(typeof(Settings));
                        if (appSettings != null)
                        {
                            noBom = (bool)appSettings["NoByteOrderMark"];
                        }
                        if (noBom)
                        {
                            // cache to an inmemory stream so we can strip the BOM.
                            using (MemoryStream ms = new MemoryStream())
                            {
                                transform.Transform(xmlReader, null, ms);
                                ms.Seek(0, SeekOrigin.Begin);
                                Utilities.WriteFileWithoutBOM(ms, outpath);
                            }
                        }
                        else
                        {
                            using (FileStream writer = new FileStream(outpath, FileMode.OpenOrCreate, FileAccess.Write))
                            {
                                Stopwatch watch = new Stopwatch();
                                watch.Start();
                                transform.Transform(xmlReader, null, writer);
                                watch.Stop();
                                this._info = new PerformanceInfo();
                                this._info.XsltMilliseconds = watch.ElapsedMilliseconds;
                                Debug.WriteLine("Transform in {0} milliseconds", watch.ElapsedMilliseconds);
                                this._xsltUri = resolved;
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
                    if (!_trusted.ContainsKey(resolved) &&
                        MessageBox.Show(this, SR.XslScriptCodePrompt, SR.XslScriptCodeCaption,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                    {
                        _trusted[resolved] = true;
                        return DisplayXsltResults(context, xsltfilename, outpath);
                    }
                }
                WriteError(x);
            }
            catch (Exception x)
            {
                WriteError(x);
            }

            this._previousOutputFile = outpath;
            return outpath;
        }

        private string GetXsltOutputFileName(string xsltfilename)
        {
            // pick a good default filename ... this means we need to know the <xsl:output method> and unfortunately 
            // XslCompiledTransform doesn't give us that so we need to get it outselves.
            var ext = GetDefaultOutputExtension();
            string outpath = null;
            if (string.IsNullOrEmpty(xsltfilename))
            {
                var basePath = Path.GetFileNameWithoutExtension(this._baseUri.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped));
                outpath = basePath + "_output" + ext;
            }
            else
            {
                outpath = Path.GetFileNameWithoutExtension(xsltfilename) + "_output" + ext;
            }
            return GetWritableFileName(outpath);
        }

        private string GetWritableFileName(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = GetXsltOutputFileName(null);
                }

                // if the fileName is a full path then honor that request.
                Uri uri = new Uri(fileName, UriKind.RelativeOrAbsolute);
                var resolved = new Uri(this._baseUri, uri);

                // If the XML file is from HTTP then put XSLT output in the %TEMP% folder.
                if (resolved.Scheme != "file")
                {
                    uri = new Uri(Path.GetTempPath());
                    this._tempFile = new Uri(uri, fileName).LocalPath;
                    return this._tempFile;
                }

                string path = resolved.LocalPath;
                if (resolved == this._baseUri)
                {
                    // can't write to the same location as the XML file or we will lose the XML file!
                    path = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_output" + Path.GetExtension(path));
                }

                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // make sure we can write to the location.
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    var test = System.Text.UTF8Encoding.UTF8.GetBytes("test");
                    fs.Write(test, 0, test.Length);
                }

                return path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("XsltControl.GetWritableBaseUri exception " + ex.Message);
            }

            // We don't have write permissions?
            Uri baseUri = new Uri(Path.GetTempPath());
            this._tempFile = new Uri(baseUri, fileName).LocalPath;
            return this._tempFile;
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
                if (this._xsltdoc == null)
                {
                    string path = customFileName;
                    var resolved = new Uri(_baseUri, path);
                    this._xsltdoc = new XmlDocument();
                    this._xsltdoc.Load(resolved.AbsoluteUri);
                }
                var method = GetOutputMethod(this._xsltdoc);
                if (method.ToLower() == "html")
                {
                    ext = ".htm";
                }
                else if (method.ToLower() == "text")
                {
                    ext = ".txt";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("XsltControl.GetDefaultOutputExtension exception " + ex.Message);
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
                                if ((string.IsNullOrEmpty(grandchild.NamespaceURI) || grandchild.NamespaceURI.Contains("xhtml"))
                                    && string.Compare(grandchild.LocalName, "html", StringComparison.OrdinalIgnoreCase) == 0)
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
            using (StringWriter writer = new StringWriter())
            {
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
        }

        string GetHexColor(Color c)
        {
            return System.Drawing.ColorTranslator.ToHtml(c);
        }

        string GetDefaultStyles(string html)
        {
            var font = (Font)this._settings["Font"];
            html = html.Replace("$FONT_FAMILY", font != null ? font.FontFamily.Name : "Consolas, Courier New");
            html = html.Replace("$FONT_SIZE", font != null ? font.SizeInPoints + "pt" : "10pt");

            var theme = (ColorTheme)_settings["Theme"];
            var colors = (ThemeColors)_settings[theme == ColorTheme.Light ? "LightColors" : "DarkColors"];
            html = html.Replace("$BACKGROUND_COLOR", GetHexColor(colors.ContainerBackground));
            html = html.Replace("$ATTRIBUTE_NAME_COLOR", GetHexColor(colors.Attribute));
            html = html.Replace("$ATTRIBUTE_VALUE_COLOR", GetHexColor(colors.Text));
            html = html.Replace("$PI_COLOR", GetHexColor(colors.PI));
            html = html.Replace("$TEXT_COLOR", GetHexColor(colors.Text));
            html = html.Replace("$COMMENT_COLOR", GetHexColor(colors.Comment));
            html = html.Replace("$ELEMENT_COLOR", GetHexColor(colors.Element));
            html = html.Replace("$MARKUP_COLOR", GetHexColor(colors.Markup));
            html = html.Replace("$SIDENOTE_COLOR", GetHexColor(colors.EditorBackground));
            html = html.Replace("$OUTPUT_TIP_DISPLAY", this.HasXsltOutput ? "none" : "block");            
            return html;
        }

        XslCompiledTransform GetDefaultStylesheet()
        {
            if (_defaultss != null)
            {
                return _defaultss;
            }
            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream(this._defaultSSResource))
            {
                if (null != stream)
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        string html = null;
                        html = GetDefaultStyles(sr.ReadToEnd());

                        using (XmlReader reader = XmlReader.Create(new StringReader(html)))
                        {
                            XslCompiledTransform t = new XslCompiledTransform();
                            t.Load(reader);
                            _defaultss = t;
                        }
                        // the XSLT DOM is also handy to have around for GetOutputMethod
                        stream.Seek(0, SeekOrigin.Begin);
                        this._xsltdoc = new XmlDocument();
                        this._xsltdoc.Load(stream);
                    }
                }
                else
                {
                    throw new Exception(string.Format("You have a build problem: resource '{0} not found", this._defaultSSResource));
                }
            }
            return _defaultss;
        }

        bool IsModified()
        {
            if (this._xsltUri.IsFile)
            {
                string path = this._xsltUri.LocalPath;
                DateTime lastWrite = File.GetLastWriteTime(path);
                return this._loaded < lastWrite;
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