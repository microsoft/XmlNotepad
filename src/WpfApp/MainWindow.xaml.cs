using ModernWpf;
using ModernWpf.Controls;
using System;
using System.ComponentModel;
using System.Windows;

namespace XmlNotepad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        UndoManager undoManager;
        Settings settings;
        Analytics analytics;
        Updater updater;
        DelayedActions delayedActions;

        public MainWindow()
        {
            this.undoManager = new UndoManager(1000);
            this.settings = new Settings();
            this.settings.StartupPath = System.IO.Path.GetDirectoryName(Application.Current.StartupUri.LocalPath);
            this.settings.ExecutablePath = Application.Current.StartupUri.LocalPath;

            delayedActions = new DelayedActions((action) =>
            {
                this.Dispatcher.Invoke(action);
            });

            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.updater = new Updater(this.settings, this.delayedActions);
            this.updater.Title = this.Title;
            this.updater.UpdateRequired += new EventHandler<bool>(OnUpdateRequired);
        }

        private void OnUpdateRequired(object sender, bool e)
        {
            // show UI
        }

        private void OnDarkTheme(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
        }

        private void OnLightTheme(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        private void OnExit(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.Close();
        }
    }
}
