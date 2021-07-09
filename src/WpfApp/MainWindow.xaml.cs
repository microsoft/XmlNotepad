using ModernWpf;
using ModernWpf.Controls;
using System.Windows;

namespace XmlNotepad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnDarkTheme(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
        }

        private void OnLightTheme(object sender, RoutedEventArgs e)
        {
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;

        }
    }
}
