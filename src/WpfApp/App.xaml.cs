using System;
using System.IO;
using System.Windows;

namespace XmlNotepad
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Settings _settings;
        DelayedActions delayedActions;
        const string SaveSettingsAction = "DelayedSave";

        public App()
        {
            this.delayedActions = new DelayedActions((action) =>
            {
                this.Dispatcher.Invoke(action);
            });
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            this._settings = new Settings()
            {
                Comparer = SettingValueMatches,
                StartupPath = Directory.GetCurrentDirectory(),
                ExecutablePath = System.AppDomain.CurrentDomain.BaseDirectory,
                //   Resolver = new XmlProxyResolver(this)
                DelayedActions = this.delayedActions
            };
            this.SetDefaultSettings();
            this.LoadSettings();
            this._settings.Changed += OnSettingsChanged;

            base.OnStartup(e);
        }

        public virtual string ConfigFile
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return System.IO.Path.Combine(path, "Microsoft", "Xml Notepad", "XmlNotepad.settings");
            }
        }

        public virtual string LocalConfigFile
        {
            get
            {
                string path = Path.GetDirectoryName(this.GetType().Assembly.Location);
                return System.IO.Path.Combine(path, "XmlNotepad.settings");
            }
        }

        private void OnSettingsChanged(object sender, string name)
        {
            this.delayedActions.StartDelayedAction(SaveSettingsAction, OnSaveSettings, TimeSpan.FromSeconds(10));
        }

        private void LoadSettings()
        {
            var path = this.LocalConfigFile;
            if (!File.Exists(path))
            {
                path = this.ConfigFile;
            }

            if (File.Exists(path))
            {
                _settings.Load(path);
            }
        }

        private void OnSaveSettings()
        {
            var path = this._settings.FileName;
            if (string.IsNullOrEmpty(path))
            {
                path = this.ConfigFile;
            }
            this._settings.Save(path);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (this.delayedActions.CancelDelayedAction(SaveSettingsAction))
            {
                this.OnSaveSettings();
            }
            this.delayedActions.Close();
            base.OnExit(e);
        }

        private bool SettingValueMatches(object existing, object newValue)
        {
            // handle Wpf specific types
            if (existing is FontStyle fs1 && newValue is FontStyle fs2)
            {
                return fs1.ToString() == fs2.ToString();
            }
            else if (existing is Rect r1 && newValue is Rect r2)
            {
                return r1.Equals(r2);
            }
            return false;
        }

        protected void SetDefaultSettings()
        {
            // populate default settings and provide type info.
            this._settings["FontFamily"] = "Courier New";
            this._settings["FontSize"] = 10.0;
            this._settings["FontStyle"] = FontStyles.Normal;

            this._settings["Theme"] = ColorTheme.Light;
            this._settings["LightColors"] = ThemeColors.GetDefaultColors(ColorTheme.Light);
            this._settings["DarkColors"] = ThemeColors.GetDefaultColors(ColorTheme.Dark);
            this._settings["FileName"] = new Uri("/", UriKind.RelativeOrAbsolute);
            this._settings["WindowBounds"] = new Rect(0, 0, 0, 0);
            this._settings["TaskListSize"] = 0;
            this._settings["TreeViewSize"] = 0;
            this._settings["RecentFiles"] = new Uri[0];
            this._settings["RecentXsltFiles"] = new Uri[0];
            this._settings["SearchWindowLocation"] = new Point(0, 0);
            this._settings["SearchSize"] = new Size(0, 0);
            this._settings["DynamicHelpVisible"] = false;
            this._settings["FindMode"] = false;
            this._settings["SearchXPath"] = false;
            this._settings["SearchWholeWord"] = false;
            this._settings["SearchRegex"] = false;
            this._settings["SearchMatchCase"] = false;

            this._settings["LastUpdateCheck"] = DateTime.MinValue;
            this._settings["UpdateFrequency"] = TimeSpan.FromDays(20);
            this._settings["UpdateLocation"] = XmlNotepad.Settings.DefaultUpdateLocation;
            this._settings["UpdateEnabled"] = true;

            this._settings["DisableDefaultXslt"] = false;
            this._settings["AutoFormatOnSave"] = true;
            this._settings["IndentLevel"] = 2;
            this._settings["IndentChar"] = IndentChar.Space;
            this._settings["NewLineChars"] = Settings.EscapeNewLines("\r\n");
            this._settings["Language"] = "";
            this._settings["NoByteOrderMark"] = false;

            this._settings["AppRegistered"] = false;
            this._settings["MaximumLineLength"] = 10000;
            this._settings["MaximumValueLength"] = (int)short.MaxValue;
            this._settings["AutoFormatLongLines"] = false;
            this._settings["IgnoreDTD"] = false;

            // XSLT options
            this._settings["BrowserVersion"] = "";
            this._settings["EnableXsltScripts"] = true;
            this._settings["WebView2Exception"] = "";

            // XmlDiff options
            this._settings["XmlDiffIgnoreChildOrder"] = false;
            this._settings["XmlDiffIgnoreComments"] = false;
            this._settings["XmlDiffIgnorePI"] = false;
            this._settings["XmlDiffIgnoreWhitespace"] = false;
            this._settings["XmlDiffIgnoreNamespaces"] = false;
            this._settings["XmlDiffIgnorePrefixes"] = false;
            this._settings["XmlDiffIgnoreXmlDecl"] = false;
            this._settings["XmlDiffIgnoreDtd"] = false;

            this._settings["SchemaCache"] = new SchemaCache();

            bool allowAnalytics = (Environment.GetEnvironmentVariable("XML_NOTEPAD_DISABLE_ANALYTICS") != "1");
            this._settings["AllowAnalytics"] = allowAnalytics;
            this._settings["AnalyticsClientId"] = "";

            // default text editor
            string sysdir = Environment.SystemDirectory;
            this._settings["TextEditor"] = Path.Combine(sysdir, "notepad.exe");
        }

        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }

}
