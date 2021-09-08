#region Using directives

using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
#endregion

namespace XmlNotepad
{
    partial class FormAbout : Form
    {
        public FormAbout()
        {
            InitializeComponent();

            this.labelVersion.Text = string.Format(this.labelVersion.Text, GetVersion());
        }

        string GetVersion()
        {
            string name = GetType().Assembly.FullName;
            string[] parts = name.Split(',');
            if (parts.Length > 1)
            {
                string version = parts[1].Trim();
                parts = version.Split('=');
                if (parts.Length > 1)
                {
                    return parts[1];
                }
            }
            return "1.0";
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (LinearGradientBrush brush = new LinearGradientBrush(new Point(0, 0), new Point(0, this.Height),
                Color.White, Color.FromArgb(0xc0, 0xc0, 0xdd)))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }
        }

        private void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        {
            string url = labelURL.Text;
            Utilities.OpenUrl(this.Handle, url);
        }

    }
}