using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Windows.Forms;

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
            if (IsHighDpiDisabled())
            {
                DisableHighDpiAwareness();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Added new features
            SettingsLocation loc = GetSettingsLocation(args);
            bool showMousePosition = IsDebugMouseEnabled(args);
            string filename = GetFilename(args);

            FormMain form = new FormMain(loc);
            if (showMousePosition)
            {
                form.ShowMousePosition();
            }
            form.AllowAnalytics = !IsAnalyticsDisabled();
            form.Show();
            Application.DoEvents();
            if (!string.IsNullOrEmpty(filename))
            {
                _ = form.Open(filename);
            }
            Application.Run(form);
        }

        static bool IsHighDpiDisabled()
        {
            return Environment.GetEnvironmentVariable("XML_NOTEPAD_DISABLE_HIGH_DPI") == "1";
        }

        static void DisableHighDpiAwareness()
        {
            var section = ConfigurationManager.GetSection("System.Windows.Forms.ApplicationConfigurationSection") as NameValueCollection;
            section?.Set("DpiAwareness", "false");
        }

        // New feature: Get the settings location from command-line arguments
        static SettingsLocation GetSettingsLocation(string[] args)
        {
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
                                return SettingsLocation.Test;
                            case "template":
                                return SettingsLocation.PortableTemplate;
                        }
                    }
                }
            }
            return SettingsLocation.Auto;
        }

        // New feature: Check if the debug mouse option is enabled from command-line arguments
        static bool IsDebugMouseEnabled(string[] args)
        {
            foreach (string arg in args)
            {
                if (!string.IsNullOrEmpty(arg) && arg.TrimStart('-').ToLowerInvariant() == "debugmouse")
                {
                    return true;
                }
            }
            return false;
        }

        // New feature: Get the filename from command-line arguments
        static string GetFilename(string[] args)
        {
            foreach (string arg in args)
            {
                if (!string.IsNullOrEmpty(arg) && arg[0] != '-' && arg[0] != '/')
                {
                    return arg;
                }
            }
            return null;
        }

        static bool IsAnalyticsDisabled()
        {
            return Environment.GetEnvironmentVariable("XML_NOTEPAD_DISABLE_ANALYTICS") == "1";
        }
    }
}
