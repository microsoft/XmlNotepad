using System;
using System.Threading.Tasks;
using System.Xml.Xsl;
using System.Xml;
using System.IO;
using System.Drawing;
using System.Threading;
using SysTask = System.Threading.Tasks.Task;
using System.Diagnostics;
using System.Runtime.Remoting.Contexts;
using System.Windows.Forms;
using System.Collections.Generic;
using SR = XmlNotepad.StringResources;
using System.ComponentModel;
using System.Xml.XPath;

namespace XmlNotepad
{

    internal class AsyncXsltContext
    {
        public Uri baseUri;
        public XmlDocument document;
        // The xslt file to use</param>
        public string xsltfilename;
        // Output file name hint and updated to real output path when finished.
        public string outpath;
        // Whether output name is non-negotiable.
        public bool userSpecifiedOutput;
        // whether DOM has <?xsl-output instruction.
        public bool hasDefaultXsltOutput;
        public bool ignoreDTD;
        public bool enableScripts;
        public bool disableOutputFile;
        public XmlUrlResolver resolver;
        public CancellationTokenSource token = new CancellationTokenSource();
        public string defaultSSResource;
        public bool running;
        public PerformanceInfo info = null;
        // The transformed output!
        public string output;
        internal XmlIncludeReader reader;
        internal ProgressiveStream writer;
        internal long estimatedOutputSize;

        public void Cancel()
        {
            token.Cancel();
            if (reader != null)
            {
                reader.Cancel();
            }
            if (writer != null)
            {
                writer.Cancel();
            }
        }

        public long Position
        {
            get
            {
                var pos = (reader != null) ? reader.Position : 0;
                if (writer != null)
                {
                    pos += writer.Position;
                }
                return pos;
            }
        }

        public long Size
        {
            get
            {                
                var size = reader != null ? reader.Size : 0;
                if (writer != null)
                {
                    size += writer.EstimatedSize;
                }
                return size;
            }
        }
    }

    internal class ProgressiveStream : Stream
    {
        Stream inner;
        bool cancelled;
        bool disposed;
        long lastLength;
        long lastPosition;

        public ProgressiveStream(Stream inner, long estimatedSize)
        {
            this.inner = inner;
            this.EstimatedSize = estimatedSize;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.inner.Dispose();
            }
            this.disposed = true;
            base.Dispose(disposing);
        }

        public void Cancel()
        {
            cancelled = true;
        }

        public long EstimatedSize { get; set; }

        public override bool CanRead => this.inner.CanRead;

        public override bool CanSeek => this.inner.CanSeek;

        public override bool CanWrite => this.inner.CanWrite;

        public override long Length 
        {
            get 
            {
                if (!disposed)
                {
                    lastLength = this.inner.Length;
                }
                return lastLength;
            }
        }

        public override long Position
        {
            get
            {
                if (!disposed)
                {
                    lastPosition = this.inner.Position;
                }
                return lastPosition;
            }
            set => this.inner.Position = value; 
        }

        public override void Flush()
        {
            this.inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (cancelled)
            {
                throw new OperationCanceledException();
            }
            return this.inner.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.inner.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this.inner.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.inner.Write(buffer, offset, count);
            if (this.EstimatedSize < this.Position)
            {
                this.EstimatedSize = this.Position * 2;
            }
            if (cancelled)
            {
                disposed = true;
                throw new OperationCanceledException();
            }
        }
    }

    internal class AsyncXslt
    {
        private XslCompiledTransform _xslt;
        private XmlDocument _xsltdoc;
        private XslCompiledTransform _defaultss;
        private Uri _xsltUri;
        private DateTime _loaded;
        private string _tempFile;
        private bool _usingDefaultXslt;
        private Settings _settings;
        AsyncXsltContext _context;
        private readonly IDictionary<Uri, bool> _trusted = new Dictionary<Uri, bool>();
        private ITrustService _trustService;

        public AsyncXslt(ISite site)
        {
            _settings = Settings.Instance;
            _trustService = (ITrustService)site.GetService(typeof(ITrustService));
        }

        public void Close()
        {
            if (_context != null)
            {
                _context.Cancel();
            }
            this.CleanupTempFile();
        }

        public bool UsingDefaultXslt => this._usingDefaultXslt;

        public void ResetDefaultStyleSheet()
        {
            _defaultss = null;
        }

        /// <summary>
        /// Run an XSLT transform and show the results.
        /// </summary>
        /// <param name="context">The info for the transform</param>
        public async System.Threading.Tasks.Task<string> TransformDocumentAsync(AsyncXsltContext context)
        {
            this.CleanupTempFile();
            this._context = context;
            await SysTask.Run(RunTransform);
            return context.outpath;
        }

        async System.Threading.Tasks.Task RunTransform()
        {
            Uri resolved = null;
            string outpath = this._context.outpath;
            XmlDocument context = this._context.document;
            this._context.running = true;
            bool trustRetry = true;
            while (trustRetry)
            {
                trustRetry = false;
                try
                {
                    XslCompiledTransform transform;
                    if (string.IsNullOrEmpty(this._context.xsltfilename))
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
                        resolved = new Uri(_context.baseUri, this._context.xsltfilename);
                        if (resolved != this._xsltUri || IsModified())
                        {
                            _xslt = new XslCompiledTransform();
                            this._loaded = DateTime.Now;
                            var settings = new XsltSettings(true, this._context.enableScripts);
                            settings.EnableScript = (_trusted.ContainsKey(resolved));
                            var rs = new XmlReaderSettings();
                            rs.DtdProcessing = this._context.ignoreDTD ? DtdProcessing.Ignore : DtdProcessing.Parse;
                            rs.XmlResolver = this._context.resolver;
                            using (XmlReader r = XmlReader.Create(resolved.AbsoluteUri, rs))
                            {
                                _xslt.Load(r, settings, this._context.resolver);
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
                        if (!_context.disableOutputFile)
                        {
                            if (!string.IsNullOrEmpty(this._context.xsltfilename))
                            {
                                outpath = this.GetXsltOutputFileName(this._context.xsltfilename);
                            }
                            else
                            {
                                // default stylesheet produces html
                                this._tempFile = outpath = GetWritableFileName("DefaultXsltOutput.htm");
                            }
                        }
                    }
                    else if (!_context.userSpecifiedOutput)
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
                        settings.XmlResolver = this._context.resolver;
                        settings.DtdProcessing = this._context.ignoreDTD ? DtdProcessing.Ignore : DtdProcessing.Parse;
                        var xmlReader = XmlIncludeReader.CreateIncludeReader(context, settings, _context.baseUri.AbsoluteUri);
                        this._context.reader = xmlReader;
                        if (string.IsNullOrEmpty(outpath))
                        {
                            using (StringWriter writer = new StringWriter())
                            {
                                transform.Transform(xmlReader, null, writer);
                                this._xsltUri = resolved;
                                this._context.output = writer.ToString();
                            }
                        }
                        else
                        {
                            bool noBom = false;
                            Settings appSettings = this._settings;
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
                                    EncodingHelpers.WriteFileWithoutBOM(ms, outpath);
                                }
                            }
                            else
                            {
                                if (this._context.estimatedOutputSize == 0)
                                {
                                    this._context.estimatedOutputSize = xmlReader.Size * 4;
                                }
                                using (FileStream writer = new FileStream(outpath, FileMode.OpenOrCreate, FileAccess.Write))
                                {
                                    var wrapper = new ProgressiveStream(writer, this._context.estimatedOutputSize);
                                    this._context.writer = wrapper;
                                    Stopwatch watch = new Stopwatch();
                                    watch.Start();
                                    transform.Transform(xmlReader, null, wrapper);
                                    watch.Stop();
                                    this._context.info = new PerformanceInfo();
                                    this._context.info.XsltMilliseconds = watch.ElapsedMilliseconds;
                                    Debug.WriteLine("Transform in {0} milliseconds", watch.ElapsedMilliseconds);
                                    this._xsltUri = resolved;
                                    writer.Flush();
                                }
                                this._context.estimatedOutputSize = new FileInfo(outpath).Length;
                            }
                        }
                    }
                }
                catch (System.Xml.Xsl.XsltException x)
                {
                    if (x.Message.Contains("XsltSettings"))
                    {
                        if (!_trusted.ContainsKey(resolved))
                        {
                            if (await this._trustService.CanTrustUrl(resolved))
                            {
                                _trusted[resolved] = true;
                                trustRetry = true;
                                continue;
                            }
                        }
                    }
                    WriteError(x);
                }
                catch (Exception x)
                {
                    WriteError(x);
                }
            }

            this._context.outpath = outpath;
            this._context.running = false;
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


        private string GetXsltOutputFileName(string xsltfilename)
        {
            // pick a good default filename ... this means we need to know the <xsl:output method> and unfortunately 
            // XslCompiledTransform doesn't give us that so we need to get it outselves.
            var ext = GetDefaultOutputExtension();
            string outpath = null;
            if (string.IsNullOrEmpty(xsltfilename))
            {
                var basePath = Path.GetFileNameWithoutExtension(this._context.baseUri.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped));
                outpath = basePath + "_output" + ext;
            }
            else
            {
                outpath = Path.GetFileNameWithoutExtension(xsltfilename) + "_output" + ext;
            }
            return GetWritableFileName(outpath);
        }

        public string GetDefaultOutputExtension(string customFileName = null)
        {
            string ext = ".xml";
            try
            {
                if (this._xsltdoc == null)
                {
                    string path = customFileName;
                    var resolved = new Uri(_context.baseUri, path);
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
                _context.output = writer.ToString();
            }
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
                var resolved = new Uri(this._context.baseUri, uri);

                // If the XML file is from HTTP then put XSLT output in the %TEMP% folder.
                if (resolved.Scheme != "file")
                {
                    uri = new Uri(Path.GetTempPath());
                    this._tempFile = new Uri(uri, fileName).LocalPath;
                    return this._tempFile;
                }

                string path = resolved.LocalPath;
                if (resolved == this._context.baseUri)
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

        XslCompiledTransform GetDefaultStylesheet()
        {
            if (_defaultss != null)
            {
                return _defaultss;
            }
            using (Stream stream = this.GetType().Assembly.GetManifestResourceStream(_context.defaultSSResource))
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
                    throw new Exception(string.Format("You have a build problem: resource '{0} not found", _context.defaultSSResource));
                }
            }
            return _defaultss;
        }

        string GetDefaultStyles(string html)
        {
            var font = (string)this._settings["FontFamily"];
            var fontSize = (double)this._settings["FontSize"];
            html = html.Replace("$FONT_FAMILY", font != null ? font : "Consolas, Courier New");
            html = html.Replace("$FONT_SIZE", font != null ? fontSize + "pt" : "10pt");

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
            html = html.Replace("$OUTPUT_TIP_DISPLAY", this._context.hasDefaultXsltOutput ? "none" : "block");
            return html;
        }

        string GetHexColor(Color c)
        {
            return System.Drawing.ColorTranslator.ToHtml(c);
        }

    }
}
