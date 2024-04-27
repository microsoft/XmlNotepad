using Microsoft.Web.WebView2.Core;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace XmlNotepad
{
    public class WebView2Exception : Exception
    {
        public WebView2Exception(string msg) : base(msg) { }
    }

    public interface ITrustService
    {
        bool? CanTrustUrl(Uri location);

        Task<bool> PromptUser(Uri location);
    }

    public partial class XsltControl : UserControl
    {
        private readonly Stopwatch _urlWatch = new Stopwatch();
        private string _html;
        private string _fileName;
        private Uri _baseUri;
        private PerformanceInfo _info = null;
        private ISite _site;
        private XmlUrlResolver _resolver;
        private Settings _settings;
        private bool _webInitialized;
        private bool _webView2Initialized;
        private bool _webView2Supported;
        private AsyncXslt _asyncXslt;
        private FormTransformProgress _progress;
        private ITrustService _trustService;
        private CoreWebView2Environment _environment;
        private DelayedActions _delayedActions;

        public event EventHandler<Exception> WebBrowserException;

        public event EventHandler<PerformanceInfo> LoadCompleted;

        public XsltControl()
        {
            InitializeComponent();
            _resolver = new XmlUrlResolver();
            _delayedActions = new DelayedActions();
        }

        /// <summary>
        /// Performs in-memory xslt transform only.  Note that file based transforms
        /// work better if you want local includes to work with that file (css, images, etc).
        /// </summary>
        public bool DisableOutputFile { get; set; }

        public void OnClosed()
        {
            this._fileName = null;
            // This serves 2 purposes, it reclaims memory while XSLT output is not visible
            // and it clears the Find dialog so it does not float over the XmlTreeView.
            this.Display("<html></html>");
            this.StopAsyncTransform();
            _asyncXslt.Close();
        }

        private async void EnsureCoreWebView2(CoreWebView2Environment environment)
        {
            try
            {
                await this.webBrowser2.EnsureCoreWebView2Async(environment);
                this._delayedActions.StartDelayedAction("CompleteCoreWebView2", CompleteCoreWebView2, TimeSpan.FromMilliseconds(1));
            }
            catch (Exception ex)
            {
                HandleWebView2Exception(ex);
            }
        }

        private void HandleWebView2Exception(Exception ex)
        {
            // fall back on old web browser control
            RaiseBrowserException(new WebView2Exception(ex.Message));
            this.BrowserVersion = "WebBrowser";
            WebBrowserFallback();
            this._settings["BrowserVersion"] = "WebBrowser";
            this._settings["WebView2Exception"] = ex.Message;
        }

        private void CompleteCoreWebView2()
        {
            if (this._webView2Initialized)
            {
                if (this.webBrowser2.CoreWebView2 != null)
                {
                    this.webBrowser2.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
                    this.webBrowser2.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                    this.webBrowser2.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
                    this.webBrowser2.Visible = true;
                    this.webBrowser1.Visible = false;
                    this._webView2Supported = true;
                }
            }
            if (!this._webView2Supported)
            {
                WebBrowserFallback();
            }
        }

        private async void InitializeBrowser(string version)
        {
            this._webInitialized = false;
            try
            {
                this.BrowserVersion = version;
                if (version == "WebView2")
                {
                    if (!this._webView2Supported)
                    {
                        this.webBrowser2.CoreWebView2InitializationCompleted -= OnCoreWebView2InitializationCompleted;
                        this.webBrowser2.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;
                        if (this._environment == null)
                        {
                            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions()
                            {
                                AllowSingleSignOnUsingOSPrimaryAccount = true
                            };

                            this._environment = await CoreWebView2Environment.CreateAsync(userDataFolder: WebViewUserCache, options: options);
                            this._delayedActions.StartDelayedAction("EnsureCoreWebView2", () => EnsureCoreWebView2(this._environment), TimeSpan.FromMilliseconds(1));
                        }
                    }
                    else
                    {
                        // we already know webView2 is supported.
                        this._webInitialized = true;
                    }
                }
                else
                {
                    WebBrowserFallback();
                }

                this._settings["WebView2Exception"] = "";
                Reload();
            }
            catch (Exception ex)
            {
                HandleWebView2Exception(ex);
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
            else
            {
                this._webView2Initialized = true;
                this._webInitialized = true;
            }
        }

        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            WebBrowser.OpenUrl(this.Handle, e.Uri);
            e.Handled = true;
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

            bool useWebView2 = UseWebView2();
            this.webBrowser2.Visible = useWebView2;
            this.webBrowser1.Visible = !useWebView2;

            if (useWebView2)
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
            if (content != this._html && _webInitialized)
            {
                _urlWatch.Reset();
                _urlWatch.Start();

                bool useWebView2 = UseWebView2();
                this.webBrowser2.Visible = useWebView2;
                this.webBrowser1.Visible = !useWebView2;

                if (useWebView2)
                {
                    try
                    {
                        if (content.Length > 1000000)
                        {
                            // NavigateToString is unfortunately limited to 1 mb.
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

        private string _defaultSSResource = "XmlNotepad.DefaultSS.xslt";

        public string DefaultStylesheetResource
        {
            get { return this._defaultSSResource; }
            set { this._defaultSSResource = value; }
        }

        // An override for hasxsltoutput.
        public bool? HasXsltOutput { get; set; }

        public void SetSite(ISite site)
        {
            this._site = site;

            _trustService = (ITrustService)site.GetService(typeof(ITrustService));
            _asyncXslt = new AsyncXslt(_trustService);

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
                this._asyncXslt.ResetDefaultStyleSheet();
                if (this._asyncXslt.UsingDefaultXslt)
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
                _ = DisplayXsltResults(_previousTransform.document, _previousTransform.xsltfilename, _previousTransform.outpath, 
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

        AsyncXsltContext _previousTransform;
        bool _promptingUser = false;

        /// <summary>
        /// Run an XSLT transform and show the results.
        /// </summary>
        /// <param name="doc">The document to transform</param>
        /// <param name="xsltfilename">The xslt file to use</param>
        /// <param name="outpath">Output file name hint.</param>
        /// <param name="userSpecifiedOutput">Whether output name is non-negotiable.</param>
        /// <param name="hasDefaultXsltOutput">Whether document has path to use for xslt output file</param>
        public async Task<string> DisplayXsltResults(XmlDocument doc, string xsltfilename, string outpath = null, bool userSpecifiedOutput = false, bool hasDefaultXsltOutput = false)
        {
            if (!this._webInitialized || this._asyncXslt == null)
            {
                return null;
            }

            if (_previousTransform != null)
            {
                _previousTransform.Cancel();
            }

            if (HasXsltOutput.HasValue)
            {
                hasDefaultXsltOutput = HasXsltOutput.Value;
            }

            this._info = new PerformanceInfo();

            var context = new AsyncXsltContext()
            {
                document = doc,
                xsltfilename = xsltfilename,
                outpath = outpath,
                userSpecifiedOutput = userSpecifiedOutput,
                hasDefaultXsltOutput = hasDefaultXsltOutput,
                defaultSSResource = this._defaultSSResource,
                baseUri = this.GetBaseUri(),
                ignoreDTD = this.IgnoreDTD,
                enableScripts = this.EnableScripts,
                disableOutputFile = this.DisableOutputFile,
                resolver = this._resolver,
                info = this._info,
            };

            if (_previousTransform != null && _previousTransform.xsltfilename == xsltfilename)
            {
                context.estimatedOutputSize = _previousTransform.estimatedOutputSize;
            }
            _previousTransform = context;

            this._delayedActions.StartDelayedAction("SlowTransformProgress",
                OnSlowTransform, TimeSpan.FromSeconds(1));

            bool tryAgain = false;
            string path = null;
            do 
            {
                tryAgain = false;
                try
                {
                    path = await this._asyncXslt.TransformDocumentAsync(_previousTransform);
                }
                catch (System.Xml.Xsl.XsltException x)
                {
                    StopAsyncTransform();
                    if (x.Message.Contains("XsltSettings"))
                    {
                        var resolved = new Uri(context.baseUri, context.xsltfilename);
                        if (_trustService.CanTrustUrl(resolved) == null)
                        {
                            _promptingUser = true;
                            if (await this._trustService.PromptUser(resolved))
                            {
                                // try again
                                tryAgain = true;
                                context.output = null;
                                context.outpath = null;
                            }
                            _promptingUser = false;
                        }
                    }
                    if (!tryAgain)
                    {
                        this._asyncXslt.WriteError(x);
                    }
                }
                catch (Exception x)
                {
                    this._asyncXslt.WriteError(x);
                }
            } while (tryAgain);

            StopAsyncTransform();

            if (context == this._previousTransform)
            {
                if (!string.IsNullOrEmpty(context.output))
                {
                    Display(context.output);
                }
                else if (!string.IsNullOrEmpty(context.outpath))
                {
                    if (File.Exists(context.outpath))
                    {
                        var size = new FileInfo(context.outpath).Length;
                        Debug.WriteLine($"Display {context.outpath} ({size})");
                        DisplayFile(context.outpath);
                    }
                    else
                    {
                        Debug.WriteLine($"Display {context.outpath} (FILE NOT FOUND)");
                    }
                }
            }
            return path;
        }


        private void StopAsyncTransform()
        {
            this._delayedActions.CancelDelayedAction("SlowTransformProgress");
            if (this._progress != null)
            {
                this._progress.Close();
                this._progress = null;
            }

        }

        private void OnSlowTransform()
        {
            if (_previousTransform != null && _previousTransform.running && !_promptingUser)
            {
                this._progress = new FormTransformProgress();
                this._progress.SetProgress(0, (int)this._previousTransform.Size, (int)this._previousTransform.Position);
                this._delayedActions.StartDelayedAction("UpdateTransformProgress", UpdateTransformProgress, TimeSpan.FromMilliseconds(30));
                if (this._progress.ShowDialog() == DialogResult.Cancel)
                {
                    this._previousTransform.Cancel();
                }
            }
        }

        private void UpdateTransformProgress()
        {
            if (this._progress != null)
            {
                this._progress.SetProgress(0, (int)this._previousTransform.Size, (int)this._previousTransform.Position);
                this._delayedActions.StartDelayedAction("UpdateTransformProgress", UpdateTransformProgress, TimeSpan.FromMilliseconds(30));
            }
        }

        public string GetOutputFileFilter(string customFileName = null)
        {
            // return something like this:
            // XML files (*.xml)|*.xml|XSL files (*.xsl)|*.xsl|XSD files (*.xsd)|*.xsd|All files (*.*)|*.*
            var ext = this._asyncXslt.GetDefaultOutputExtension(customFileName);
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