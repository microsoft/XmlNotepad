using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Windows.Forms;
using System.IO;

namespace XmlNotepad
{
    public partial class WebBrowserForm : Form
    {
        #region ctors
        public WebBrowserForm(Uri uri, string formName)
        {
            InitializeComponent();

            this.Text = formName;
            this.webBrowser1.Url = uri;
        }
        #endregion

        
    }
}