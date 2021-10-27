using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace XmlNotepad
{
    public delegate void SettingsEventHandler(object sender, string name);

    public enum ColorTheme
    {
        Light,
        Dark
    }

    /// <summary>
    /// Settings is a container for persistent settings that you want to store in a file
    /// like XmlNotepad.settings.  Each setting has a name and some typed value.  The
    /// deserialization process returns strings by default.  If you want a typed value
    /// returned then you need to initialize the settings class with a default typed value
    /// so it can figure out what type to return.  The type information is not stored
    /// in the settings file.  Any type that has a corresponding TypeConverter is supported as 
    /// well as Hashtable, Array and any IXmlSerializable object.
    /// This class also provides some useful features that most
    /// people expect to get out of their settings files, namely:
    /// <list>
    /// <item>
    /// Watching changes on disk and automatically reloading the file, then generating
    /// an event so that the hosting application can react to those changes.</item>
    /// <item>
    /// Transform any Uri setting to a persistent file name using the PersistentFileNames class.
    /// </item>
    /// </list>
    /// </summary>
    public class Settings : IDisposable
    {
        private static Settings _instance;
        private string _filename;
        private FileSystemWatcher _watcher;
        private Hashtable _map = new Hashtable();
        private System.Threading.Timer _timer;
        private PersistentFileNames _pfn;

        public static string DefaultUpdateLocation = "https://lovettsoftwarestorage.blob.core.windows.net/downloads/XmlNotepad/Updates.xml";

        /// <summary>
        /// This event is raised when a particular setting has been changed.
        /// A special setting named "File" is raised when the settings 
        /// file has changed on disk.  You can listen to this event and call
        /// Reload() if you want to automatically reload settings in this case.
        /// </summary>
        public event SettingsEventHandler Changed;

        /// <summary>
        /// Note this is an IDisposable object, so remember to call Dispose() on it during
        /// application shutdown.
        /// </summary>
        public Settings()
        {
            _instance = this;
        }

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new Exception("Settings is not yet available!");
                }
                return _instance;
            }
        }

        /// <summary>
        /// The application startup path.
        /// </summary>
        public string StartupPath { get; set; }

        /// <summary>
        /// The application executable path.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// The XML resolver to use in loading XML
        /// </summary>
        public XmlResolver Resolver { get; set; }

        /// <summary>
        /// Object used to raise the change events on the right thread.
        /// </summary>
        public DelayedActions DelayedActions { get; set; }

        /// <summary>
        /// This method is usually called right before you update the settings and save
        /// them to disk.
        /// </summary>
        public void StopWatchingFileChanges()
        {
            if (this._watcher != null)
            {
                this._watcher.Dispose();
                this._watcher = null;
            }
        }

        /// <summary>
        /// Call this method if you know a particular setting object has changed.
        /// This raises the Changed event.  This will happen automatically if you
        /// change the setting object instance below.
        /// </summary>
        /// <param name="name"></param>
        public void OnChanged(string name)
        {
            var handler = this.Changed;
            if (DelayedActions != null && handler != null)
            {
                this.DelayedActions.StartDelayedAction("On" + name + "Changed", () =>
                {
                    Changed(this, name);
                }, TimeSpan.FromMilliseconds(0)); // timespan of zero makes these immediate.
            }
        }

        /// <summary>
        /// Get or set a named setting passing the typed object to be serialized.
        /// </summary>
        /// <param name="name">The setting name</param>
        /// <returns>The setting value or null if not found.</returns>
        public object this[string name]
        {
            get { return this._map[name]; }
            set
            {
                if (this._map[name] != value)
                {
                    this._map[name] = value;
                    OnChanged(name);
                }
            }
        }

        /// <summary>
        /// Reloads the settings from the current file on disk.
        /// </summary>
        public void Reload()
        {
            Load(this._filename);
        }

        /// <summary>
        /// Loads the specified settings file and deserializes values. It uses the existing 
        /// settings to figure out the type to convert the strings to.  
        /// </summary>
        /// <param name="filename">XmlNotepad settings xml file.</param>
        public void Load(string filename)
        {
            _pfn = new PersistentFileNames(Settings.Instance.StartupPath);

            // we don't use the serializer because it's too slow to fire up.
            try
            {
                using (var r = new XmlTextReader(filename))
                {
                    if (r.IsStartElement("Settings"))
                    {
                        while (r.Read())
                        {
                            if (r.NodeType == XmlNodeType.Element)
                            {
                                string name = r.Name;
                                object o = _map[name];
                                if (o != null)
                                {
                                    object value = null;
                                    if (o is Hashtable)
                                    {
                                        ReadHashTable(r, (Hashtable)o);
                                    }
                                    else if (o is Array)
                                    {
                                        value = ReadArray(name, (Array)o, r);
                                    }
                                    else if (o is IXmlSerializable)
                                    {
                                        IXmlSerializable xs = (IXmlSerializable)o;
                                        xs.ReadXml(r);
                                    }
                                    else
                                    {
                                        string s = r.ReadString();
                                        value = ConvertToType(s, o.GetType());
                                    }
                                    if (value != null)
                                    {
                                        this[name] = value;
                                    }
                                }
                                OnChanged(name);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Hey, at least we tried!
                Debug.WriteLine("Load settings failed: " + ex.Message);
            }

            this.FileName = filename;
        }

        public string FileName
        {
            get { return this._filename; }
            set
            {
                if (this._filename != value)
                {
                    this._filename = value;

                    StopWatchingFileChanges();

                    this._watcher = new FileSystemWatcher(Path.GetDirectoryName(_filename),
                        Path.GetFileName(_filename));

                    this._watcher.Changed += new FileSystemEventHandler(watcher_Changed);
                    this._watcher.EnableRaisingEvents = true;
                }
            }
        }

        string ConvertToString(object value)
        {
            if (value is Uri)
            {
                return _pfn.GetPersistentFileName((Uri)value);
            }
            else if (value is string)
            {
                return (string)value;
            }
            else
            {
                TypeConverter tc = TypeDescriptor.GetConverter(value.GetType());
                if (tc != null)
                {
                    string s = tc.ConvertToString(value);
                    return s;
                }
                throw new ApplicationException(string.Format(Strings.TypeConvertError, value.GetType().FullName));
            }
        }

        object ConvertToType(string value, Type type)
        {
            if (type == typeof(Uri))
            {
                return _pfn.GetAbsoluteFileName(value);
            }
            else if (type == typeof(string))
            {
                return value;
            }
            else
            {
                TypeConverter tc = TypeDescriptor.GetConverter(type);
                if (tc != null)
                {
                    return tc.ConvertFromString(value);
                }
                throw new ApplicationException(string.Format(Strings.TypeConvertError, type.FullName));
            }
        }

        /// <summary>
        /// Serializes property values to the settings file.
        /// </summary>
        /// <param name="filename">The name of the settings file to write to.</param>
        public void Save(string filename)
        {
            // make sure directory exists!
            Directory.CreateDirectory(Path.GetDirectoryName(filename));
            try
            {
                using (var w = new XmlTextWriter(filename, System.Text.Encoding.UTF8))
                {
                    w.Formatting = Formatting.Indented;
                    w.WriteStartElement("Settings");
                    foreach (string key in _map.Keys)
                    {
                        object value = _map[key];
                        if (value != null)
                        {
                            if (value is Hashtable)
                            {
                                w.WriteStartElement(key); // container element      
                                WriteHashTable(w, (Hashtable)value);
                                w.WriteEndElement();
                            }
                            else if (value is Array)
                            {
                                WriteArray(w, key, (Array)value);
                            }
                            else if (value is IXmlSerializable)
                            {
                                w.WriteStartElement(key); // container element      
                                IXmlSerializable xs = (IXmlSerializable)value;
                                xs.WriteXml(w);
                                w.WriteEndElement();
                            }
                            else
                            {
                                string s = ConvertToString(value);
                                if (s != null) w.WriteElementString(key, s);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void ReadHashTable(XmlReader r, Hashtable ht)
        {
            Type et = typeof(string);
            foreach (DictionaryEntry item in ht)
            {
                if (item.Value != null)
                {
                    et = item.Value.GetType();
                    break;
                }
            }
            if (!r.IsEmptyElement)
            {
                while (r.Read() && r.NodeType != XmlNodeType.EndElement)
                {
                    if (r.NodeType == XmlNodeType.Element)
                    {
                        string key = XmlConvert.DecodeName(r.LocalName);
                        string value = r.ReadString();
                        ht[key] = ConvertToType(value, et);
                    }
                }
            }
        }

        private void WriteHashTable(XmlWriter w, Hashtable value)
        {
            try
            {
                foreach (DictionaryEntry item in value)
                {
                    string key = XmlConvert.EncodeName(item.Key.ToString());
                    w.WriteStartElement(key);
                    object o = item.Value;
                    w.WriteString(this.ConvertToString(o));
                    w.WriteEndElement();
                }
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
        }

        Array ReadArray(string name, Array a, XmlReader r)
        {
            Type et = a.GetType().GetElementType();
            ArrayList list = new ArrayList();
            if (!r.IsEmptyElement)
            {
                while (r.Read() && r.NodeType != XmlNodeType.EndElement)
                {
                    if (r.NodeType == XmlNodeType.Element)
                    {
                        string value = r.ReadString();
                        list.Add(ConvertToType(value, et));
                    }
                }
            }
            return (Array)list.ToArray(et);
        }

        void WriteArray(XmlWriter w, string key, Array array)
        {
            w.WriteStartElement(key); // container element
            try
            {
                string name = array.GetType().GetElementType().Name;
                foreach (object o in array)
                {
                    string s = ConvertToString(o);
                    if (s != null) w.WriteElementString(name, s);
                }
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
            w.WriteEndElement();
        }

        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            // The trick here is that the file system seems to generate lots of
            // events and we don't want to have lots of dialogs popping up asking the
            // user to reload settings, so we insert a delay to let the events
            // settle down, then we tell the hosting app that the settings have changed.
            if (e.ChangeType == WatcherChangeTypes.Changed && this._timer == null)
            {
                this._timer = new System.Threading.Timer(new TimerCallback(OnDelay), this, 2000, Timeout.Infinite);
            }
        }

        void OnDelay(object state)
        {
            OnChanged("File");
            if (this._timer != null)
            {
                this._timer.Dispose();
                this._timer = null;
            }
        }

        ~Settings()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.StopWatchingFileChanges();
            if (this._timer != null)
            {
                this._timer.Dispose();
            }
            this._timer = null;
            GC.SuppressFinalize(this);
        }

        //================================================================================
        // Strong typed settings
        //================================================================================
        public bool GetBoolean(string settingName, bool defaultValue = false)
        {
            object settingValue = this[settingName];
            return settingValue is bool value ? value : defaultValue;
        }

        public int GetInteger(string settingName, int defaultValue = 0)
        {
            object settingValue = this[settingName];
            return settingValue is int value ? value : defaultValue;
        }

        public string GetString(string settingName, string defaultValue = "")
        {
            object settingValue = this[settingName];
            return settingValue != null ? settingValue.ToString() : defaultValue;
        }

        public Hashtable GetDefaultColors(ColorTheme theme)
        {
            if (theme == ColorTheme.Light)
            {
                System.Collections.Hashtable light = new System.Collections.Hashtable();
                light["Element"] = Color.FromArgb(0, 64, 128);
                light["Attribute"] = Color.Maroon;
                light["Text"] = Color.Black;
                light["Comment"] = Color.Green;
                light["PI"] = Color.Purple;
                light["CDATA"] = Color.Gray;
                light["Background"] = Color.White;
                light["ContainerBackground"] = Color.AliceBlue;
                light["EditorBackground"] = Color.LightSteelBlue;
                return light;
            }
            else
            {
                System.Collections.Hashtable dark = new System.Collections.Hashtable();
                dark["Element"] = Color.FromArgb(0x35, 0x7D, 0xCE);
                dark["Attribute"] = Color.FromArgb(0x92, 0xCA, 0xF3);
                dark["Text"] = Color.FromArgb(0x94, 0xB7, 0xC8);
                dark["Comment"] = Color.FromArgb(0x45, 0x62, 0x23);
                dark["PI"] = Color.FromArgb(0xAC, 0x91, 0x6A);
                dark["CDATA"] = Color.FromArgb(0xC2, 0xCB, 0x85);
                dark["Background"] = Color.FromArgb(0x1e, 0x1e, 0x1e);
                dark["ContainerBackground"] = Color.FromArgb(0x25, 0x25, 0x26);
                dark["EditorBackground"] = Color.FromArgb(24, 24, 44);
                return dark;
            }
        }

        public void AddDefaultColors(string name, ColorTheme theme)
        {
            Hashtable table = (Hashtable)this[name];
            if (table == null)
            {
                table = new Hashtable();
                this[name] = table;
            }

            Hashtable defaults = this.GetDefaultColors(theme);

            // Merge any undefined colors.
            foreach (string key in defaults.Keys)
            {
                if (!table.ContainsKey(key))
                {
                    table.Add(key, defaults[key]);
                }
            }
        }

    }

    /// <summary>
    /// This class takes care of converting file names to a relative form that makes it easier to 
    /// move the host application to different machines and still have relative file names work 
    /// correctly. It also replaces well known paths with the variables %StartupPath%, %ProgramFiles, 
    /// %UserProfile% and %SystemRoot%.  
    /// </summary>
    class PersistentFileNames
    {
        private readonly Hashtable _variables = new Hashtable();

        public PersistentFileNames(string startupPath)
        {
            _variables["StartupPath"] = startupPath;
            _variables["ProgramFiles"] = Environment.GetEnvironmentVariable("ProgramFiles");
            _variables["UserProfile"] = Environment.GetEnvironmentVariable("UserProfile");
            _variables["SystemRoot"] = Environment.GetEnvironmentVariable("SystemRoot");
        }

        public string GetPersistentFileName(Uri uri)
        {
            string result = null;
            try
            {
                if (!uri.IsAbsoluteUri) return uri.OriginalString;
                result = uri.OriginalString;
                int len = 0;
                string path = uri.AbsolutePath;
                if (uri.IsFile && !File.Exists(uri.LocalPath)) // sanity check!
                    return null;

                // replace absolute paths with variables.
                foreach (string key in _variables.Keys)
                {
                    string baseDir = (string)_variables[key];
                    if (!baseDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                        baseDir += Path.DirectorySeparatorChar;
                    Uri baseUri = new Uri(baseDir);
                    Uri rel = baseUri.MakeRelativeUri(uri);
                    string relPath = rel.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped);
                    Uri test = new Uri(relPath, UriKind.RelativeOrAbsolute);
                    if (!test.IsAbsoluteUri)
                    {
                        // Keep track of the shortest relative path.
                        if (len == 0 || relPath.Length < len)
                        {
                            result = "%" + key + "%" + relPath;
                            len = relPath.Length;
                        }
                    }
                }
            }
            catch (UriFormatException e)
            {
                // swallow any bad URI noise.
                Trace.WriteLine(e.Message);
            }
            return result;
        }

        public Uri GetAbsoluteFileName(string filename)
        {
            try
            {
                // replace variables with absolute paths.
                foreach (string key in _variables.Keys)
                {
                    string var = "%" + key + "%";
                    if (filename.StartsWith(var, StringComparison.CurrentCultureIgnoreCase))
                    {
                        string baseDir = (string)_variables[key];
                        string relPath = filename.Substring(var.Length);
                        if (!baseDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                            baseDir += Path.DirectorySeparatorChar;
                        Uri resolved = new Uri(new Uri(baseDir), relPath);
                        return resolved;
                    }
                }
            }
            catch (UriFormatException e)
            {
                // swallow any bad URI noise.
                Trace.WriteLine(e.Message);
            }
            return new Uri(filename, UriKind.RelativeOrAbsolute);
        }
    }
}
