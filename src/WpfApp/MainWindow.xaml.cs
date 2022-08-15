using LovettSoftware.Utilities;
using ModernWpf;
using ModernWpf.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using XmlNotepad.Utilities;

namespace XmlNotepad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IServiceProvider
    {
        UndoManager undoManager = new UndoManager(1000);
        Settings settings;
        AppAnalytics analytics;
        Updater updater;
        DelayedActions delayedActions;
        XmlCache model;
        XmlIntellisenseProvider xip;
        HelpProvider helpProvider = new HelpProvider();
        RecentFiles recentFiles = new RecentFiles();
        RecentFilesComboBox recentFilesCombo;
        bool initialized;

        public MainWindow()
        {
            this.Visibility = Visibility.Hidden;
            this.settings = Settings.Instance;
            this.settings.StartupPath = System.IO.Path.GetDirectoryName(Application.Current.StartupUri.LocalPath);
            this.settings.ExecutablePath = Application.Current.StartupUri.LocalPath;

            delayedActions = new DelayedActions((action) =>
            {
                this.Dispatcher.Invoke(action);
            });

            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            this.SizeChanged += OnWindowSizeChanged;
            this.LocationChanged += OnWindowLocationChanged;

            this.recentFiles.RecentFileSelected += OnRecentFileSelected;
            this.recentFilesCombo = new RecentFilesComboBox(this.recentFiles, this.ComboBoxAddress);

            this.RestoreSettings();
            this.initialized = true;
        }

        private void RestoreSettings()
        {
            object value = this.settings["WindowBounds"];
            if (value is Rect r && !r.IsEmpty)
            {
                // make sure it is visible on the user's current screen configuration.
                var bounds = new Rect(
                    XamlExtensions.ConvertFromDeviceIndependentPixels(r.X),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(r.Y),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(r.Width),
                    XamlExtensions.ConvertFromDeviceIndependentPixels(r.Height));
                var virtualScreen = new Rect(SystemParameters.VirtualScreenLeft,
                    SystemParameters.VirtualScreenTop,
                    SystemParameters.VirtualScreenWidth,
                    SystemParameters.VirtualScreenHeight);
                if (virtualScreen.Contains(bounds))
                {
                    this.Left = XamlExtensions.ConvertToDeviceIndependentPixels((int)bounds.X);
                    this.Top = XamlExtensions.ConvertToDeviceIndependentPixels((int)bounds.Y);
                    this.Width = XamlExtensions.ConvertToDeviceIndependentPixels((int)bounds.Width);
                    this.Height = XamlExtensions.ConvertToDeviceIndependentPixels((int)bounds.Height);
                }
            }

            UpdateTheme();

            this.recentFiles.SetFiles(this.settings["RecentFiles"] as Uri[]);

            this.Visibility = Visibility.Visible;
        }

        private void UpdateTheme()
        {
            if (this.settings["Theme"] is ColorTheme theme)
            {
                switch (theme)
                {
                    case ColorTheme.Light:
                        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                        break;
                    case ColorTheme.Dark:
                        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                        break;
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.updater = new Updater(this.settings, this.delayedActions);
            this.updater.Title = this.Title;
            this.updater.UpdateAvailable += OnUpdateAvailable;
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            if (this.initialized)
            {
                this.settings["WindowBounds"] = this.RestoreBounds;
            }
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.initialized)
            {
                this.settings["WindowBounds"] = this.RestoreBounds;
            }
        }

        private void OnUpdateAvailable(object sender, UpdateStatus e)
        {
            // show UI
            Debug.WriteLine("New version available: " + e.Latest.ToString());
        }

        private void OnDarkTheme(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            this.settings["Theme"] = ColorTheme.Dark;
        }

        private void OnLightTheme(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
            this.settings["Theme"] = ColorTheme.Light;

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        private void OnExit(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void OnNewWindow(object sender, RoutedEventArgs e)
        {
            // hmmm, this is problematic because the new window needs a new XmlCache, and SchemaCache and
            // so on, so better to start a new process here.
            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = this.GetType().Assembly.Location,
                Arguments = string.Join(" ", Environment.GetCommandLineArgs()),
                WorkingDirectory = Directory.GetCurrentDirectory()
            };

            Process.Start(info);
        }

        public object GetService(Type service)
        {
            if (service == typeof(UndoManager))
            {
                return this.undoManager;
            }
            else if (service == typeof(SchemaCache))
            {
                return this.settings["SchemaCache"];
            }
            //else if (service == typeof(TreeView))
            //{
            //    XmlTreeView view = (XmlTreeView)GetService(typeof(XmlTreeView));
            //    return view.TreeView;
            //}
            //else if (service == typeof(XmlTreeView))
            //{
            //    if (this.xmlTreeView1 == null)
            //    {
            //        this.xmlTreeView1 = this.CreateTreeView();
            //    }
            //    return this.xmlTreeView1;
            //}
            else if (service == typeof(XmlCache))
            {
                if (null == this.model)
                {
                    this.model = new XmlCache((IServiceProvider)this, (SchemaCache)this.settings["SchemaCache"], this.delayedActions);
                }
                return this.model;
            }
            else if (service == typeof(Settings))
            {
                return this.settings;
            }
            else if (service == typeof(IIntellisenseProvider))
            {
                if (this.xip == null) this.xip = new XmlIntellisenseProvider(this.model);
                return this.xip;
            }
            else if (service == typeof(HelpProvider))
            {
                return this.helpProvider;
            }
            //else if (service == typeof(WebProxyService))
            //{
            //    if (this._proxyService == null)
            //        this._proxyService = new WebProxyService((IServiceProvider)this);
            //    return this._proxyService;
            //}
            else if (service == typeof(DelayedActions))
            {
                return this.delayedActions;
            }
            return null;
        }

        private void OnComboBoxAddress_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var path = this.ComboBoxAddress.Text;
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        LoadFile(path);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error Parsing Filename", MessageBoxButton.OK, MessageBoxImage.Error);
                        ComboBoxAddress.Focus();
                    }
                }
            }
        }

        private void LoadFile(string path)
        {
            var model = (XmlCache)GetService(typeof(XmlCache));
            model.Load(path);
        }
        private void OnRecentFileSelected(object sender, MostRecentlyUsedEventArgs args)
        {
            if (args.Selection != null)
            {
                this.LoadFile(args.Selection);
            }
        }

    }
}
