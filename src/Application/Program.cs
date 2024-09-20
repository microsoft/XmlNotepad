using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Windows.Forms;
using SR = XmlNotepad.StringResources;

namespace XmlNotepad
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("XML_NOTEPAD_DISABLE_HIGH_DPI") == "1")
            {
                var section = ConfigurationManager.GetSection("System.Windows.Forms.ApplicationConfigurationSection") as NameValueCollection;
                section.Set("DpiAwareness", "false");
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SettingsLocation loc = SettingsLocation.Auto;
            bool showMousePosition = false;
            string filename = null;
            foreach (string arg in args)
            {
                if (!string.IsNullOrEmpty(arg))
                {
                    char c = arg[0];
                    if (c == '-' || c == '/')
                    {
                        switch (arg.TrimStart('-').ToLowerInvariant())
                        {
                            case "test":
                                loc = SettingsLocation.Test;
                                break;
                            case "template":
                                loc = SettingsLocation.PortableTemplate;
                                break;
                            case "debugmouse":
                                showMousePosition = true;
                                break;
                        }
                    }
                    else if (filename == null)
                    {
                        filename = arg;
                    }
                }
            }

            FormMain form = new FormMain(loc);
            if (showMousePosition) form.ShowMousePosition();
            form.AllowAnalytics = Environment.GetEnvironmentVariable("XML_NOTEPAD_DISABLE_ANALYTICS") != "1";
            form.Show();
            Application.DoEvents();
            if (!string.IsNullOrEmpty(filename))
            {
                _ = form.Open(filename);
            }
            Application.Run(form);
        }

        public static void Launch(string args)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = ApplicationPath;
            info.Arguments = $"\"{args}\"";
            Process p = new Process();
            p.StartInfo = info;
            if (!p.Start())
            {
                MessageBox.Show(string.Format(SR.ErrorCreatingProcessPrompt, info.FileName), SR.LaunchErrorPrompt, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static string ApplicationPath
        {
            get
            {
                string path = Application.ExecutablePath;
                if (path.EndsWith("vstesthost.exe", StringComparison.CurrentCultureIgnoreCase))
                {
                    // must be running UnitTests
                    Uri baseUri = new Uri(typeof(Program).Assembly.Location);
                    Uri resolved = new Uri(baseUri, @"..\..\..\Application\bin\debug\XmlNotepad.exe");
                    path = resolved.LocalPath;
                }
                return path;
            }
        }

    }

}