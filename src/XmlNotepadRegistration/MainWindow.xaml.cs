using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace XmlNotepadRegistration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Registration associations;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            MessageBox.Show("Debugme", "Debug", MessageBoxButton.OK);
#endif
            string appPath = null;
            string appName = null;
            string appDescription = null;

            string[] args = ((App)App.Current).Arguments;
            if (args.Length > 2)
            {
                appPath = args[0];
                appName = args[1];
                appDescription = args[2];
                associations = new Registration(appPath);
                string cmd = string.Format("\"{0}\" %1", appPath);
                associations.RegisterApplication(appName, cmd, cmd, appName, appDescription);
            }

            this.Close();
        }


    }
}
