using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace XmlNotepad {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            if (Environment.GetEnvironmentVariable("XML_NOTEPAD_DISABLE_HIGH_DPI") == "1") {
                var section = ConfigurationManager.GetSection("System.Windows.Forms.ApplicationConfigurationSection") as NameValueCollection;
                section.Set("DpiAwareness", "false");
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            FormMain form = new FormMain
            {
                AllowAnalytics = Environment.GetEnvironmentVariable("XML_NOTEPAD_DISABLE_ANALYTICS") != "1"
            };
            Application.DoEvents();
            foreach(string arg in args){
                if (!string.IsNullOrEmpty(arg)) {
                    char c = arg[0];
                    if (c == '-' || c == '/') {
                        switch (arg.Substring(1).ToLowerInvariant()) {
                            case "offset":
                                form.Location = new System.Drawing.Point(form.Location.X + 20, form.Location.Y + 20);
                                break;
                        }
                    } else {
                        form.Open(arg);
                    }
                }
            }
            form.Show();
            Application.Run(form);
        }
    }

}