using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace XmlNotepad
{
    public delegate void SettingsEventHandler(object sender, string name);
    public delegate bool ValueMatchHandler(object existing, object newValue);

    public enum ColorTheme
    {
        Light,
        Dark
    }

    // order is serialization dependent here, so don't change them
    public enum SettingsLocation
    {
        Portable,
        Local,
        Roaming,
        PortableTemplate,
        Temporary,
        Test,
        Auto
    }

    public class ThemeColors : IXmlSerializable
    {
        public Color Element = Color.Transparent;
        public Color SchemaAwareTextColor = Color.Transparent;
        public Color Attribute = Color.Transparent;
        public Color Text = Color.Transparent;
        public Color Comment = Color.Transparent;
        public Color PI = Color.Transparent;
        public Color CDATA = Color.Transparent;
        public Color Background = Color.Transparent;
        public Color ContainerBackground = Color.Transparent;
        public Color EditorBackground = Color.Transparent;
        public Color Markup = Color.Transparent;
        static readonly TypeConverter tc = TypeDescriptor.GetConverter(typeof(Color));

        public override int GetHashCode()
        {
            return Element.GetHashCode() + SchemaAwareTextColor.GetHashCode() + Attribute.GetHashCode() + Text.GetHashCode() + Comment.GetHashCode() +
                PI.GetHashCode() + CDATA.GetHashCode() + Background.GetHashCode() + ContainerBackground.GetHashCode() +
                EditorBackground.GetHashCode() + Markup.GetHashCode();
        }

        public static ThemeColors GetDefaultColors(ColorTheme theme)
        {
            if (theme == ColorTheme.Light)
            {
                return new ThemeColors()
                {
                    Element = Color.FromArgb(0, 64, 128),
                    SchemaAwareTextColor = Color.FromArgb(0, 134, 198),
                    Attribute = Color.Maroon,
                    Text = Color.Black,
                    Comment = Color.Green,
                    PI = Color.Purple,
                    CDATA = Color.Gray,
                    Background = Color.White,
                    ContainerBackground = Color.AliceBlue,
                    EditorBackground = Color.FromArgb(255, 250, 205),
                    Markup = Color.FromArgb(80, 80, 80),
                };
            }
            else
            {
                return new ThemeColors()
                {
                    Element = Color.FromArgb(0x35, 0x7D, 0xCE),
                    SchemaAwareTextColor = Color.FromArgb(13, 85, 166),
                    Attribute = Color.FromArgb(0x92, 0xCA, 0xF3),
                    Text = Color.FromArgb(0xC0, 0xC0, 0xC0),
                    Comment = Color.FromArgb(0x45, 0x8A, 0x23),
                    PI = Color.FromArgb(0xAC, 0x91, 0x6A),
                    CDATA = Color.FromArgb(0xC2, 0xCB, 0x85),
                    Background = Color.FromArgb(0x1e, 0x1e, 0x1e),
                    ContainerBackground = Color.FromArgb(0x25, 0x25, 0x26),
                    EditorBackground = Color.FromArgb(24, 24, 44),
                    Markup = Color.FromArgb(100, 100, 100),
                };
            }
        }

        private Color ConvertToColor(string value)
        {
            if (tc != null)
            {
                return (Color)tc.ConvertFromString(value);
            }
            throw new ApplicationException(string.Format(Strings.TypeConvertError, "Color"));
        }

        internal void Merge(ThemeColors defaults)
        {
            Element = MergeColor(this.Element, defaults.Element);
            SchemaAwareTextColor = MergeColor(this.SchemaAwareTextColor, defaults.SchemaAwareTextColor);
            Attribute = MergeColor(this.Attribute, defaults.Attribute);
            Text = MergeColor(this.Text, defaults.Text);
            Comment = MergeColor(this.Comment, defaults.Comment);
            PI = MergeColor(this.PI, defaults.PI);
            CDATA = MergeColor(this.CDATA, defaults.CDATA);
            Background = MergeColor(this.Background, defaults.Background);
            ContainerBackground = MergeColor(this.ContainerBackground, defaults.ContainerBackground);
            EditorBackground = MergeColor(this.EditorBackground, defaults.EditorBackground);
        }

        private Color MergeColor(Color c1, Color c2)
        {
            return c1 == Color.Transparent ? c2 : c1;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            ThemeColors t = obj as ThemeColors;
            if (t != null)
            {
                return this.Element == t.Element &&
                    this.SchemaAwareTextColor == t.SchemaAwareTextColor && 
                    this.Attribute == t.Attribute &&
                    this.Text == t.Text &&
                    this.Comment == t.Comment &&
                    this.PI == t.PI &&
                    this.CDATA == t.CDATA &&
                    this.Background == t.Background &&
                    this.ContainerBackground == t.ContainerBackground &&
                    this.EditorBackground == t.EditorBackground;
            }
            return false;
        }

        public static bool operator ==(ThemeColors left, ThemeColors right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ThemeColors left, ThemeColors right)
        {
            return !left.Equals(right);
        }

        #region IXmlSerializable
        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader r)
        {
            if (!r.IsEmptyElement)
            {
                while (r.Read() && r.NodeType != XmlNodeType.EndElement)
                {
                    if (r.NodeType == XmlNodeType.Element)
                    {
                        string name = r.LocalName;
                        string value = r.ReadString();
                        try
                        {
                            Color c = ConvertToColor(value);

                            switch (name)
                            {
                                case "Element":
                                    this.Element = c;
                                    break;
                                case "SchemaAwareTextColor":
                                    this.SchemaAwareTextColor = c;
                                    break;
                                case "Attribute":
                                    this.Attribute = c;
                                    break;
                                case "Text":
                                    this.Text = c;
                                    break;
                                case "Comment":
                                    this.Comment = c;
                                    break;
                                case "PI":
                                    this.PI = c;
                                    break;
                                case "CDATA":
                                    this.CDATA = c;
                                    break;
                                case "Background":
                                    this.Background = c;
                                    break;
                                case "ContainerBackground":
                                    this.ContainerBackground = c;
                                    break;
                                case "EditorBackground":
                                    this.EditorBackground = c;
                                    break;
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteElementString("Element", tc.ConvertToString(this.Element));
            writer.WriteElementString("SchemaAwareTextColor", tc.ConvertToString(this.SchemaAwareTextColor));
            writer.WriteElementString("Attribute", tc.ConvertToString(this.Attribute));
            writer.WriteElementString("Text", tc.ConvertToString(this.Text));
            writer.WriteElementString("Comment", tc.ConvertToString(this.Comment));
            writer.WriteElementString("PI", tc.ConvertToString(this.PI));
            writer.WriteElementString("CDATA", tc.ConvertToString(this.CDATA));
            writer.WriteElementString("Background", tc.ConvertToString(this.Background));
            writer.WriteElementString("ContainerBackground", tc.ConvertToString(this.ContainerBackground));
        }

        #endregion IXmlSerializable
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
        private readonly Hashtable _map = new Hashtable();
        private DelayedActions _delayedActions = null;
        private PersistentFileNames _pfn;

        public static string DefaultUpdateLocation = "https://lovettsoftwarestorage.blob.core.windows.net/downloads/XmlNotepad/Updates.xml";

        /// <summary>
        /// This event is raised when a particular setting has been changed.
        /// A special setting named "File" is raised when the settings 
        /// file has changed on disk.  You can listen to this event and call
        /// Reload() if you want to automatically reload settings in this case.
        /// </summary>
        public event SettingsEventHandler Changed;

        private ValueMatchHandler comparer;

        /// <summary>
        /// Note this is an IDisposable object, so remember to call Dispose() on it during
        /// application shutdown.
        /// </summary>
        public Settings()
        {
            _instance = this;
        }

        public ValueMatchHandler Comparer
        {
            get => comparer;
            set => comparer = value;
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
        public DelayedActions DelayedActions
        {
            get => _delayedActions;
            set => _delayedActions = value;
        }

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
            if (this._delayedActions != null && handler != null)
            {
                this._delayedActions.StartDelayedAction("On" + name + "Changed", () =>
                {
                    Changed(this, name);
                }, TimeSpan.FromMilliseconds(0)); // timespan of zero makes these immediate.
            }
        }


        public void Remove(string name)
        {
            if (this._map.Contains(name))
            {
                this._map.Remove(name);
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
                if (!this.SettingValueMatches(this._map[name], value))
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
                                    if (o is Hashtable ht)
                                    {
                                        ReadHashTable(r, ht);
                                    }
                                    else if (o is Array a)
                                    {
                                        value = ReadArray(a, r);
                                    }
                                    else if (o is IXmlSerializable xs)
                                    {
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
                    if (this._filename != null)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(this._filename));
                        StopWatchingFileChanges();

                        this._watcher = new FileSystemWatcher(Path.GetDirectoryName(_filename),
                            Path.GetFileName(_filename));

                        this._watcher.Changed += new FileSystemEventHandler(OnWatcherChanged);
                        this._watcher.EnableRaisingEvents = true;
                    }
                }
            }
        }

        string ConvertToString(object value)
        {
            if (value is Uri uri)
            {
                if (_pfn == null)
                {
                    _pfn = new PersistentFileNames(Settings.Instance.StartupPath);
                }
                return _pfn.GetPersistentFileName(uri);
            }
            else if (value is string s)
            {
                return s;
            }
            else
            {
                TypeConverter tc = TypeDescriptor.GetConverter(value.GetType());
                if (tc != null)
                {
                    return tc.ConvertToString(value);
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
                    // create save stability.
                    List<string> keys = new List<string>();
                    foreach (string key in _map.Keys)
                    {
                        keys.Add(key);
                    }
                    keys.Sort();
                    foreach (string key in keys)
                    {
                        object value = _map[key];
                        if (value != null)
                        {
                            if (value is Hashtable ht)
                            {
                                w.WriteStartElement(key); // container element      
                                WriteHashTable(w, ht);
                                w.WriteEndElement();
                            }
                            else if (value is Array va)
                            {
                                WriteArray(w, key, va);
                            }
                            else if (value is IXmlSerializable xs)
                            {
                                w.WriteStartElement(key); // container element   
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

        Array ReadArray(Array a, XmlReader r)
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

        private void OnWatcherChanged(object sender, FileSystemEventArgs e)
        {
            // The trick here is that the file system seems to generate lots of
            // events and we don't want to have lots of dialogs popping up asking the
            // user to reload settings, so we insert a delay to let the events
            // settle down, then we tell the hosting app that the settings have changed
            // and StartDelayedAction does the "consolodation" raising only one OnDelay.
            if (e.ChangeType == WatcherChangeTypes.Changed && this._delayedActions != null)
            {
                this._delayedActions.StartDelayedAction("OnDelay", () => OnDelay(10), TimeSpan.FromMilliseconds(250));
            }
        }

        void OnDelay(int retries)
        {
            try
            {
                // make sure file is not still locked by the writer.
                string text = File.ReadAllText(this._filename);
                OnChanged("File");
            }
            catch (Exception)
            {
                if (retries > 0)
                {
                    this._delayedActions.StartDelayedAction("OnDelay", () => OnDelay(retries - 1), TimeSpan.FromMilliseconds(100));
                }
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
            this._delayedActions?.Close();
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

        public double GetDouble(string settingName, double defaultValue = 0)
        {
            object settingValue = this[settingName];
            if (settingValue is double value)
                return value;
            else if (settingValue is float v)
                return (double)v;
            else if (settingValue is int i)
                return (double)i;
            return defaultValue;
        }

        public string GetString(string settingName, string defaultValue = "")
        {
            object settingValue = this[settingName];
            return settingValue != null ? settingValue.ToString() : defaultValue;
        }

        public T GetEnum<T>(string settingName, T defaultValue)
        {
            object settingValue = this[settingName];
            return settingValue != null ? (T)Enum.Parse(typeof(T), settingValue.ToString()) : defaultValue;
        }

        public SettingsLocation GetLocation()
        {
            var sloc = this.GetInteger("SettingsLocation", -1);
            if (sloc != -1)
            {
                // migrate off using an integer
                return (SettingsLocation)sloc;
            }
            else
            {
                // use the proper enum and default to SettingsLocation.Auto.
                return this.GetEnum<SettingsLocation>("SettingsLocation", SettingsLocation.Auto);
            }
        }

        public void SetLocation(SettingsLocation loc)
        {
            this["SettingsLocation"] = loc.ToString();
        }

        public ThemeColors AddDefaultColors(string name, ColorTheme theme)
        {
            ThemeColors table = (ThemeColors)this[name];
            if (table == null)
            {
                table = new ThemeColors();
                this[name] = table;
            }

            ThemeColors defaults = ThemeColors.GetDefaultColors(theme);
            table.Merge(defaults);
            return table;
        }


        private bool SettingValueMatches(object existing, object newValue)
        {
            if (existing == null && newValue != null)
            {
                return false;
            }
            else if (existing != null && newValue == null)
            {
                return false;
            }
            else if (existing is IXmlSerializable)
            {
                // then object comparison is enough.
                return existing == newValue;
            }
            else if (existing is int i1)
            {
                return newValue is int i2 && i1 == i2;
            }
            else if (existing is string s1)
            {
                return newValue is String s2 && s1 == s2;
            }
            else if (existing is bool b1)
            {
                return newValue is bool b2 && b1 == b2;
            }
            else if (existing is ColorTheme ct1)
            {
                return newValue is ColorTheme ct2 && ct1 == ct2;
            }
            else if (existing is Uri u1)
            {
                return newValue is Uri u2 && u1 == u2;
            }
            else if (existing is DateTime dt1)
            {
                return newValue is DateTime dt2 && dt1 == dt2;
            }
            else if (existing is TimeSpan ts1)
            {
                return newValue is TimeSpan ts2 && ts1 == ts2;
            }
            else if (existing is IndentChar ic1)
            {
                return newValue is IndentChar ic2 && ic1 == ic2;
            }
            else if (existing is Color c1)
            {
                return newValue is Color c2 && c1 == c2;
            }
            else if (existing is ThemeColors tc1)
            {
                return newValue is ThemeColors tc2 && tc1 == tc2;
            }
            else if (existing is Hashtable h1)
            {
                if (newValue is Hashtable h2)
                {
                    foreach (var key in h1.Keys)
                    {
                        var v1 = h1[key];
                        var v2 = h2[key];
                        if (!SettingValueMatches(v1, v2))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }
            else if (existing is Array a1)
            {
                if (newValue is Array a2)
                {
                    if (a1.Length != a2.Length)
                    {
                        return false;
                    }
                    for (int i = 0; i < a1.Length; i++)
                    {
                        object v1 = a1.GetValue(i);
                        object v2 = a2.GetValue(i);
                        if (!SettingValueMatches(v1, v2))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }
            else if (this.comparer != null)
            {
                return this.comparer(existing, newValue);
            }
            else
            {
                return false;
            }
        }

        public static string EscapeNewLines(string nl)
        {
            return nl.Replace("\r", "\\r").Replace("\n", "\\n");
        }
        public static string UnescapeNewLines(string nl)
        {
            return nl.Replace("\\r", "\r").Replace("\\n", "\n");
        }

        public void SetDefaults()
        {
            // populate default settings and provide type info.
            this["Font"] = "deleted";
            this["FontFamily"] = "Courier New";
            this["FontSize"] = 10.0;
            this["TreeIndent"] = 12;
            this["FontStyle"] = "Normal";
            this["FontWeight"] = "Normal";
            this["Theme"] = ColorTheme.Light;
            this["LightColors"] = ThemeColors.GetDefaultColors(ColorTheme.Light);
            this["DarkColors"] = ThemeColors.GetDefaultColors(ColorTheme.Dark);
            this["FileName"] = new Uri("/", UriKind.RelativeOrAbsolute);
            this["WindowBounds"] = new Rectangle(0, 0, 0, 0);
            this["TaskListSize"] = 0;
            this["TreeViewSize"] = 0;
            this["RecentFiles"] = new Uri[0];
            this["RecentXsltFiles"] = new Uri[0];
            this["RecentFindStrings"] = new string[0];
            this["RecentReplaceStrings"] = new string[0];
            this["SearchWindowLocation"] = new Point(0, 0);
            this["SearchSize"] = new Size(0, 0);
            this["OptionsWindowLocation"] = new Point(0, 0);
            this["OptionsWindowSize"] = new Size(0, 0);
            this["DynamicHelpVisible"] = false;
            this["FindMode"] = false;
            this["SearchXPath"] = false;
            this["SearchWholeWord"] = false;
            this["SearchRegex"] = false;
            this["SearchMatchCase"] = false;
            this["SchemaAwareText"] = true;
            this["SchemaAwareNames"] = "id,name,title,key";

            this["LastUpdateCheck"] = DateTime.Now;
            this["UpdateFrequency"] = TimeSpan.FromDays(20);
            this["UpdateLocation"] = XmlNotepad.Settings.DefaultUpdateLocation;
            this["UpdateEnabled"] = true;
            this["DisableUpdateUI"] = false;

            this["DisableDefaultXslt"] = false;
            this["AutoFormatOnSave"] = true;
            this["IndentLevel"] = 2;
            this["IndentChar"] = IndentChar.Space;
            this["NewLineChars"] = Settings.EscapeNewLines("\r\n");
            this["PreserveWhitespace"] = false;
            this["Language"] = "";
            this["NoByteOrderMark"] = false;

            this["AppRegistered"] = false;
            this["MaximumLineLength"] = 10000;
            this["MaximumValueLength"] = (int)short.MaxValue;
            this["AutoFormatLongLines"] = false;
            this["IgnoreDTD"] = false;

            // XSLT options
            this["BrowserVersion"] = "";
            this["EnableXsltScripts"] = true;
            this["WebView2Exception"] = "";
            this["WebView2PromptInstall"] = true;

            // XmlDiff options
            this["XmlDiffIgnoreChildOrder"] = false;
            this["XmlDiffIgnoreComments"] = false;
            this["XmlDiffIgnorePI"] = false;
            this["XmlDiffIgnoreWhitespace"] = false;
            this["XmlDiffIgnoreNamespaces"] = false;
            this["XmlDiffIgnorePrefixes"] = false;
            this["XmlDiffIgnoreXmlDecl"] = false;
            this["XmlDiffIgnoreDtd"] = false;
            this["XmlDiffHideIdentical"] = false;

            // analytics question has been answered...
            this["AllowAnalytics"] = false;
            this["AnalyticsClientId"] = "";

            // default text editor
            string sysdir = Environment.SystemDirectory;
            this["TextEditor"] = Path.Combine(sysdir, "notepad.exe");
            this["MouseCalibration"] = new Point[0];
            this["PrimaryScreenSize"] = new Size();
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
