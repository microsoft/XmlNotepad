using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System.Xml.Schema;
using XmlNotepad;

namespace Microsoft {    
    /// <summary>
    /// This is a custom builder for editing font values using the FontDialog.
    /// You can specify this builder using the following annotation in your schema:
    ///     vs:builder="Microsoft.FontBuilder"
    ///     vs:assembly="FontBuilder"
    /// where xmlns:vs="http://schemas.microsoft.com/Visual-Studio-Intellisense"
    /// </summary>
    class FontBuilder : IXmlBuilder {
        FontDialog fd = new FontDialog();
        ISite site;
        IIntellisenseProvider owner;

        public IIntellisenseProvider Owner {
            get { return this.owner; }
            set { this.owner = value; }
        }

        public ISite Site {
            get { return this.site; }
            set { this.site = value; }
        }

        public string Caption { get { return "&Font picker..."; } }

        public bool EditValue(IWin32Window owner, XmlSchemaType type, string input, out string output) {
            output = input;
            FontConverter fc = new FontConverter();
            Font f = null;
            try {
                f = (Font)fc.ConvertFromString(input);
                fd.Font = f;
            } catch {
            }
            
            if (fd.ShowDialog(owner) == DialogResult.OK) {
                output = fc.ConvertToString(fd.Font);
                return true;
            } else {
                return false;
            }
        }
    }
}
