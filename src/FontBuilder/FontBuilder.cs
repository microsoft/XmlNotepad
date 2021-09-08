using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System.Xml.Schema;
using XmlNotepad;

namespace Microsoft
{
    /// <summary>
    /// This is a custom builder for editing font values using the FontDialog.
    /// You can specify this builder using the following annotation in your schema:
    ///     vs:builder="Microsoft.FontBuilder"
    ///     vs:assembly="FontBuilder"
    /// where xmlns:vs="http://schemas.microsoft.com/Visual-Studio-Intellisense"
    /// </summary>
    class FontBuilder : IXmlBuilder
    {
        private FontDialog _fd = new FontDialog();
        private ISite _site;
        private IIntellisenseProvider _owner;

        public IIntellisenseProvider Owner
        {
            get { return this._owner; }
            set { this._owner = value; }
        }

        public ISite Site
        {
            get { return this._site; }
            set { this._site = value; }
        }

        public string Caption { get { return "&Font picker..."; } }

        public bool EditValue(IHostWindow owner, XmlSchemaType type, string input, out string output)
        {
            output = input;
            FontConverter fc = new FontConverter();
            Font f = null;
            try
            {
                f = (Font)fc.ConvertFromString(input);
                _fd.Font = f;
            }
            catch
            {
            }

            if (_fd.ShowDialog(owner as IWin32Window) == DialogResult.OK)
            {
                output = fc.ConvertToString(_fd.Font);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
