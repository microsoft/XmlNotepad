using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace XmlNotepad {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MyForm form = new MyForm();
            form.Show();
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
            Application.Run(form);
        }
    }

}