using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Text;
using System.Net;
using System.Net.Cache;
using System.Timers;

namespace XmlNotepad
{
    /// <summary>
    /// XmlCache wraps an XmlDocument and provides the stuff necessary for an "editor" in terms
    /// of watching for changes on disk, notification when the file has been reloaded, and keeping
    /// track of the current file name and dirty state.
    /// </summary>
    public class XmlCache : IDisposable
    {
        private string _fileName;
        private string _renamed;
        private bool _dirty;
        private DomLoader _loader;
        private XmlDocument _doc;
        private FileSystemWatcher _watcher;
        private int _retries;
        private SchemaCache _schemaCache;
        private Dictionary<XmlNode, XmlSchemaInfo> _typeInfo;
        private int _batch;
        private DateTime _lastModified;
        private Checker _checker;
        private IServiceProvider _site;
        private DelayedActions _actions;
        private Settings _settings;

        public event EventHandler FileChanged;
        public event EventHandler<ModelChangedEventArgs> ModelChanged;

        public XmlCache(IServiceProvider site, DelayedActions handler)
        {
            this._loader = new DomLoader(site);
            this._schemaCache = new SchemaCache(site);
            this._site = site;
            this.Document = new XmlDocument();
            this._actions = handler;
            this._settings = (Settings)this._site.GetService(typeof(Settings));
        }

        ~XmlCache()
        {
            Dispose(false);
        }

        public Uri Location => new Uri(this._fileName);

        public string FileName => this._fileName;
        public string NewName => this._renamed;

        public bool IsFile
        {
            get
            {
                if (!string.IsNullOrEmpty(this._fileName))
                {
                    return this.Location.IsFile;
                }
                return false;
            }
        }

        /// <summary>
        /// File path to (optionally user-specified) xslt file.
        /// </summary>
        public string XsltFileName { get; set; }

        /// <summary>
        /// File path to (optionally user-specified) to use for xslt output.
        /// </summary>
        public string XsltDefaultOutput { get; set; }

        public bool Dirty => this._dirty;

        public Settings Settings => this._settings;

        public XmlResolver SchemaResolver => this._schemaCache.Resolver;

        public XPathNavigator Navigator
        {
            get
            {
                XPathDocument xdoc = new XPathDocument(this._fileName);
                XPathNavigator nav = xdoc.CreateNavigator();
                return nav;
            }
        }

        public void ValidateModel(ErrorHandler handler)
        {
            this._checker = new Checker(handler);
            _checker.Validate(this);
        }


        public XmlDocument Document
        {
            get { return this._doc; }
            set
            {
                if (this._doc != null)
                {
                    this._doc.NodeChanged -= new XmlNodeChangedEventHandler(OnDocumentChanged);
                    this._doc.NodeInserted -= new XmlNodeChangedEventHandler(OnDocumentChanged);
                    this._doc.NodeRemoved -= new XmlNodeChangedEventHandler(OnDocumentChanged);
                }
                this._doc = value;
                if (this._doc != null)
                {
                    this._doc.NodeChanged += new XmlNodeChangedEventHandler(OnDocumentChanged);
                    this._doc.NodeInserted += new XmlNodeChangedEventHandler(OnDocumentChanged);
                    this._doc.NodeRemoved += new XmlNodeChangedEventHandler(OnDocumentChanged);
                }
            }
        }

        public Dictionary<XmlNode, XmlSchemaInfo> TypeInfoMap
        {
            get { return this._typeInfo; }
            set { this._typeInfo = value; }
        }

        public XmlSchemaInfo GetTypeInfo(XmlNode node)
        {
            if (this._typeInfo == null) return null;
            if (this._typeInfo.ContainsKey(node))
            {
                return this._typeInfo[node];
            }
            return null;
        }

        public XmlSchemaElement GetElementType(XmlQualifiedName xmlQualifiedName)
        {
            if (this._schemaCache != null)
            {
                return this._schemaCache.GetElementType(xmlQualifiedName);
            }
            return null;
        }

        public XmlSchemaAttribute GetAttributeType(XmlQualifiedName xmlQualifiedName)
        {
            if (this._schemaCache != null)
            {
                return this._schemaCache.GetAttributeType(xmlQualifiedName);
            }
            return null;
        }

        /// <summary>
        /// Provides schemas used for validation.
        /// </summary>
        public SchemaCache SchemaCache
        {
            get { return this._schemaCache; }
            set { this._schemaCache = value; }
        }

        /// <summary>
        /// Loads an instance of xml.
        /// Load updated to handle validation when instance doc refers to schema.
        /// </summary>
        /// <param name="file">Xml instance document</param>
        /// <returns></returns>
        public void Load(string file)
        {
            Uri uri = new Uri(file, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
            {
                Uri resolved = new Uri(new Uri(Directory.GetCurrentDirectory() + "\\"), uri);
                file = resolved.LocalPath;
                uri = resolved;
            }

            XmlReaderSettings settings = GetReaderSettings();
            settings.ValidationEventHandler += new ValidationEventHandler(OnValidationEvent);
            using (XmlReader reader = XmlReader.Create(file, settings))
            {
                this.Load(reader, file);
            }
        }

        public void Load(XmlReader reader, string fileName)
        {
            this.Clear();
            _loader = new DomLoader(this._site);
            StopFileWatch();

            Uri uri = new Uri(fileName, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
            {
                Uri resolved = new Uri(new Uri(Directory.GetCurrentDirectory() + "\\"), uri);
                fileName = resolved.LocalPath;
                uri = resolved;
            }

            this._fileName = fileName;
            this._lastModified = this.LastModTime;
            this._dirty = false;
            StartFileWatch();

            this.Document = _loader.Load(reader);
            this.XsltFileName = this._loader.XsltFileName;
            this.XsltDefaultOutput = this._loader.XsltDefaultOutput;

            // calling this event will cause the XmlTreeView to populate
            FireModelChanged(ModelChangeType.Reloaded, this._doc);
        }

        public XmlReaderSettings GetReaderSettings()
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = this._settings.GetBoolean("IgnoreDTD") ? DtdProcessing.Ignore : DtdProcessing.Parse;
            settings.CheckCharacters = false;
            settings.XmlResolver = Settings.Instance.Resolver;
            return settings;
        }

        public void ExpandIncludes()
        {
            if (this.Document != null)
            {
                this._dirty = true;
                XmlReaderSettings s = new XmlReaderSettings();
                s.DtdProcessing = this._settings.GetBoolean("IgnoreDTD") ? DtdProcessing.Ignore : DtdProcessing.Parse;
                s.XmlResolver = Settings.Instance.Resolver;
                using (XmlReader r = XmlIncludeReader.CreateIncludeReader(this.Document, s, this.FileName))
                {
                    this.Document = _loader.Load(r);
                }

                // calling this event will cause the XmlTreeView to populate
                FireModelChanged(ModelChangeType.Reloaded, this._doc);
            }
        }

        public void BeginUpdate()
        {
            if (_batch == 0)
                FireModelChanged(ModelChangeType.BeginBatchUpdate, this._doc);
            _batch++;
        }

        public void EndUpdate()
        {
            _batch--;
            if (_batch == 0)
                FireModelChanged(ModelChangeType.EndBatchUpdate, this._doc);
        }

        public LineInfo GetLineInfo(XmlNode node)
        {
            return _loader.GetLineInfo(node);
        }

        void OnValidationEvent(object sender, ValidationEventArgs e)
        {
            // todo: log errors in error list window.
        }

        public void Reload()
        {
            string filename = this._fileName;
            Clear();
            Load(filename);
        }

        public void Clear()
        {
            this._renamed = null;
            this.Document = new XmlDocument();
            StopFileWatch();
            this._fileName = null;
            FireModelChanged(ModelChangeType.Reloaded, this._doc);
        }

        public void Save()
        {
            Save(this._fileName);
        }

        public Encoding GetEncoding()
        {
            XmlDeclaration xmldecl = _doc.FirstChild as XmlDeclaration;
            Encoding result = null;
            if (xmldecl != null)
            {
                string name = "";
                try
                {
                    name = xmldecl.Encoding;
                    if (!string.IsNullOrEmpty(name))
                    {
                        result = Encoding.GetEncoding(name);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("Error getting encoding '{0}': {1}", name, ex.Message));
                }
            }
            if (result == null)
            {
                // default is UTF-8.
                result = Encoding.UTF8;
            }
            return result;
        }

        public void AddXmlDeclarationWithEncoding()
        {
            XmlDeclaration xmldecl = _doc.FirstChild as XmlDeclaration;
            if (xmldecl == null)
            {
                _doc.InsertBefore(_doc.CreateXmlDeclaration("1.0", "utf-8", null), _doc.FirstChild);
            }
            else
            {
                string e = xmldecl.Encoding;
                if (string.IsNullOrEmpty(e))
                {
                    xmldecl.Encoding = "utf-8";
                }
            }
        }

        public void Save(string name)
        {
            SaveCopy(name);
            this._dirty = false;
            this._fileName = name;
            this._lastModified = this.LastModTime;
            FireModelChanged(ModelChangeType.Saved, this._doc);
        }

        public void SaveCopy(string filename)
        {
            try
            {
                StopFileWatch();
                XmlWriterSettings s = new XmlWriterSettings();
                Utilities.InitializeWriterSettings(s, this._site);

                var encoding = GetEncoding();
                s.Encoding = encoding;
                bool noBom = false;
                if (this._site != null)
                {
                    Settings settings = (Settings)this._site.GetService(typeof(Settings));
                    if (settings != null)
                    {
                        noBom = (bool)settings["NoByteOrderMark"];
                        if (noBom)
                        {
                            // then we must have an XML declaration with an encoding attribute.
                            AddXmlDeclarationWithEncoding();
                        }
                    }
                }
                if (noBom)
                {
                    MemoryStream ms = new MemoryStream();
                    using (XmlWriter w = XmlWriter.Create(ms, s))
                    {
                        _doc.Save(w);
                    }
                    ms.Seek(0, SeekOrigin.Begin);

                    Utilities.WriteFileWithoutBOM(ms, filename);

                }
                else
                {
                    using (XmlWriter w = XmlWriter.Create(filename, s))
                    {
                        _doc.Save(w);
                    }
                }
            }
            finally
            {
                StartFileWatch();
            }
        }

        public bool IsReadOnly(string filename)
        {
            return File.Exists(filename) &&
                (File.GetAttributes(filename) & FileAttributes.ReadOnly) != 0;
        }

        public void MakeReadWrite(string filename)
        {
            if (!File.Exists(filename))
                return;

            StopFileWatch();
            try
            {
                FileAttributes attrsMinusReadOnly = File.GetAttributes(this._fileName) & ~FileAttributes.ReadOnly;
                File.SetAttributes(filename, attrsMinusReadOnly);
            }
            finally
            {
                StartFileWatch();
            }
        }

        void StopFileWatch()
        {
            if (this._watcher != null)
            {
                this._watcher.Dispose();
                this._watcher = null;
            }
        }
        private void StartFileWatch()
        {
            if (this._fileName != null && Location.IsFile && File.Exists(this._fileName))
            {
                string dir = Path.GetDirectoryName(this._fileName) + "\\";
                this._watcher = new FileSystemWatcher(dir, "*.*");
                this._watcher.Changed += new FileSystemEventHandler(watcher_Changed);
                this._watcher.Renamed += new RenamedEventHandler(watcher_Renamed);
                this._watcher.EnableRaisingEvents = true;
            }
            else
            {
                StopFileWatch();
            }
        }

        class ReloadAction
        {
            public XmlCache Cache;
            public string FileName;
            public bool Renamed;

            public void HandleReload()
            {
                if (!Renamed)
                {
                    Cache.CheckReload(FileName);
                }
            }
        }

        ReloadAction pending;

        void StartReload()
        {
            // Apart from retrying, the DelayedActions has the nice side effect of also 
            // collapsing multiple file system events into one action callback.
            _retries = 3;
            if (pending == null)
            {
                pending = new ReloadAction() { FileName = this._fileName, Cache = this };
                _actions.StartDelayedAction("reload", () => pending.HandleReload(), TimeSpan.FromSeconds(1));
            }
        }

        DateTime LastModTime
        {
            get
            {
                if (Location.IsFile) return File.GetLastWriteTime(this._fileName);
                return DateTime.Now;
            }
        }

        public void CheckReload(string fileName)
        {
            if (!File.Exists(fileName))
            {
                // file was deleted...
                return;
            }
            pending = null;
            try
            {
                // Only do the reload if the file on disk really is different from
                // what we last loaded.
                if (this._lastModified < LastModTime && this._fileName == fileName)
                {
                    // Test if we can open the file (it might still be locked).
                    using (FileStream fs = new FileStream(this._fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fs.Close();
                    }

                    FireFileChanged();
                }
            }
            finally
            {
                _retries--;
                if (_retries > 0)
                {
                    _actions.StartDelayedAction("reload", Reload, TimeSpan.FromSeconds(1));
                }
            }
        }

        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed &&
                IsSamePath(this._fileName, e.FullPath))
            {
                Debug.WriteLine("### File changed " + this._fileName);
                StartReload();
            }
        }

        private void watcher_Renamed(object sender, RenamedEventArgs e)
        {
            // Some editors rename the file to *.bak then save the new version and
            // in that case we do not want XmlNotepad to switch to the .bak file.
            if (IsSamePath(this._fileName, e.OldFullPath))
            {
                Debug.WriteLine("### File renamed to " + e.FullPath);
                var p = pending;
                if (p != null)
                {
                    // we have a situation were file was modified AND renamed.  Tricky!
                    p.Renamed = true;
                }
                
                // switch to UI thread
                if (renamePending == null)
                {
                    renamePending = new RenameAction { OldName = e.OldFullPath, NewName = e.FullPath, Cache = this };
                    _actions.StartDelayedAction("renamed", renamePending.HandleEvent, TimeSpan.FromMilliseconds(1));
                }
            }
        }

        class RenameAction {
            public string OldName;
            public string NewName;
            public XmlCache Cache;
            public void HandleEvent()
            {
                Cache.OnRenamed(OldName, NewName);
            }
        }

        RenameAction renamePending;

        private void OnRenamed(string oldName, string newName)
        {
            this.renamePending = null;
            this._dirty = true;
            if (System.IO.Path.GetFullPath(this._fileName) == oldName)
            {
                this._renamed = newName;
                FireFileChanged();
            }
        }

        static bool IsSamePath(string a, string b)
        {
            return string.Compare(a, b, true) == 0;
        }

        void FireFileChanged()
        {
            if (this.FileChanged != null)
            {
                FileChanged(this, EventArgs.Empty);
            }
        }

        void FireModelChanged(ModelChangeType t, XmlNode node)
        {
            if (this.ModelChanged != null)
                this.ModelChanged(this, new ModelChangedEventArgs(t, node));
        }

        void OnPIChange(XmlNodeChangedEventArgs e)
        {
            XmlProcessingInstruction pi = (XmlProcessingInstruction)e.Node;
            if (pi.Name == "xml-stylesheet")
            {
                if (e.Action == XmlNodeChangedAction.Remove)
                {
                    // see if there's another!
                    pi = this._doc.SelectSingleNode("processing-instruction('xml-stylesheet')") as XmlProcessingInstruction;
                }
                if (pi != null)
                {
                    this.XsltFileName = DomLoader.ParseXsltArgs(pi.Data);
                }
                else
                {
                    this.XsltFileName = null;
                }
            }
            else if (pi.Name == "xsl-output")
            {
                if (e.Action == XmlNodeChangedAction.Remove)
                {
                    // see if there's another!
                    pi = this._doc.SelectSingleNode("processing-instruction('xsl-output')") as XmlProcessingInstruction;
                }
                if (pi != null)
                {
                    this.XsltDefaultOutput = DomLoader.ParseXsltOutputArgs(pi.Data);
                }
                else
                {
                    this.XsltDefaultOutput = null;
                }
            }
        }

        private void OnDocumentChanged(object sender, XmlNodeChangedEventArgs e)
        {
            // initialize t
            ModelChangeType t = ModelChangeType.NodeChanged;
            if (e.Node is XmlProcessingInstruction)
            {
                OnPIChange(e);
            }

            if (XmlHelpers.IsXmlnsNode(e.NewParent) || XmlHelpers.IsXmlnsNode(e.Node))
            {

                // we flag a namespace change whenever an xmlns attribute changes.
                t = ModelChangeType.NamespaceChanged;
                XmlNode node = e.Node;
                if (e.Action == XmlNodeChangedAction.Remove)
                {
                    node = e.OldParent; // since node.OwnerElement link has been severed!
                }
                this._dirty = true;
                FireModelChanged(t, node);
            }
            else
            {
                switch (e.Action)
                {
                    case XmlNodeChangedAction.Change:
                        t = ModelChangeType.NodeChanged;
                        break;
                    case XmlNodeChangedAction.Insert:
                        t = ModelChangeType.NodeInserted;
                        break;
                    case XmlNodeChangedAction.Remove:
                        t = ModelChangeType.NodeRemoved;
                        break;
                }
                this._dirty = true;
                FireModelChanged(t, e.Node);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this._actions.CancelDelayedAction("reload");
            StopFileWatch();
        }

    }

    public enum ModelChangeType
    {
        Reloaded,
        Saved,
        NodeChanged,
        NodeInserted,
        NodeRemoved,
        NamespaceChanged,
        BeginBatchUpdate,
        EndBatchUpdate
    }

    public class ModelChangedEventArgs : EventArgs
    {
        ModelChangeType type;
        XmlNode node;

        public ModelChangedEventArgs(ModelChangeType t, XmlNode node)
        {
            this.type = t;
            this.node = node;
        }

        public XmlNode Node
        {
            get { return node; }
            set { node = value; }
        }

        public ModelChangeType ModelChangeType
        {
            get { return this.type; }
            set { this.type = value; }
        }

    }

    public enum IndentChar
    {
        Space,
        Tab
    }
}