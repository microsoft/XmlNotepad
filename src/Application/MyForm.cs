using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace XmlNotepad {

    /// <summary>
    /// Example subclass of the XmlNotepad main Form.
    /// </summary>
    public class MyForm : FormMain
    {

        public override void SaveConfig()
        {
            base.SaveConfig();
        }

        protected override void SetDefaultSettings()
        {
            base.SetDefaultSettings();

        }

        public override void LoadConfig()
        {
            base.LoadConfig();
        }
    }
}