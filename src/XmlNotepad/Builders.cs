using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Schema;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Design;
using System.Xml;
using System.Globalization;
using System.ComponentModel;

namespace XmlNotepad {
    /// <summary>
    /// This interface is used to provide extensible popup modal dialog for editing a particular
    /// type of value in the XML document.  (e.g. color picker).
    /// </summary>
    public interface IXmlBuilder {
        
        /// <summary>
        /// Return a caption for the button that launches your dialog.
        /// </summary>
        string Caption { get; }

        /// <summary>
        /// Provides the ISite objects which is how you get services from the hosting application.
        /// </summary>
        /// <param name="site"></param>
        ISite Site { get; set; }

        /// <summary>
        /// Provides the IntellisenseProvider that created this object.
        /// </summary>
        IIntellisenseProvider Owner { get; set; }
                
        /// <summary>
        /// This method launches a custom builder (e.g. color picker, etc)
        /// with an initial value and produces a resulting value.  
        /// </summary>
        /// <param name="owner">The parent window that is calling us</param>
        /// <param name="type">The type associated with the value being edited</param>
        /// <param name="input">The current value being edited</param>
        /// <param name="output">The result of the builder</param>
        /// <returns>Returns false if the user cancelled the operation</returns>
        bool EditValue(IWin32Window owner, XmlSchemaType type, string input, out string output);
    }

    /// <summary>
    /// This interface is used to provide other types of editors besides the default TextBox for
    /// inline editing of particular types of values in the XML document.  For example, DateTimePicker.
    /// </summary>
    public interface IXmlEditor {

        /// <summary>
        /// Provides the ISite objects which is how you get services from the hosting application.
        /// </summary>
        /// <param name="site"></param>
        ISite Site { get; set; }

        /// <summary>
        /// Provides the IntellisenseProvider that created this object.
        /// </summary>
        IIntellisenseProvider Owner { get; set; }

        /// <summary>
        /// This property provides the XmlSchemaType for the editor
        /// </summary>
        XmlSchemaType SchemaType { get; set; }

        /// <summary>
        /// Return the editor you want to use to edit your values.
        /// </summary>
        Control Editor { get; }

        /// <summary>
        /// The setter is called just before editing to pass in the current value from the 
        /// XmlDocument.  At the end of editing, the getter is called to pull the new value
        /// back out of the editor for storing in the XmlDocument.
        /// </summary>
        string XmlValue { get; set;}

    }

    /// <summary>
    /// This is a custom builder for editing color values using the ColorDialog.
    /// You can specify this builder using the following annotation in your schema:
    ///     vs:builder="XmlNotepad.ColorBuilder"
    /// where xmlns:vs="http://schemas.microsoft.com/Visual-Studio-Intellisense"
    /// </summary>
    class ColorBuilder : IXmlBuilder {
        ColorDialog cd = new ColorDialog();
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

        public string Caption { get { return SR.ColorPickerLabel; } }
       
        public bool EditValue(IWin32Window owner, XmlSchemaType type, string input, out string output) {
            output = input;
            ColorConverter cc = new ColorConverter();
            Color c = Color.Black;
            try { 
                c = (Color)cc.ConvertFromString(input);
            } catch {
            }
            cd.Color = c;
            cd.AnyColor = true;
            if (cd.ShowDialog(owner) == DialogResult.OK) {
                output = cc.ConvertToString(cd.Color);
                return true;
            } else {
                return false;
            }
        }
    }

    /// <summary>
    /// This is a custom builder for editing anyUri types via Open File Dialog.
    /// You can specify this builder using the following annotation in your schema:
    ///     vs:builder="XmlNotepad.UriBuilder"
    /// where xmlns:vs="http://schemas.microsoft.com/Visual-Studio-Intellisense"
    /// </summary>
    class UriBuilder : IXmlBuilder {
        OpenFileDialog fd = new OpenFileDialog();
        ISite site;
        
        public UriBuilder() {
            fd.Filter = "All files (*.*)|*.*";
        }

        IIntellisenseProvider owner;

        public IIntellisenseProvider Owner {
            get { return this.owner; }
            set { this.owner = value; }
        }
        
        public ISite Site {
            get { return this.site; }
            set { this.site = value; }
        }

        public string Caption { get { return SR.UriBrowseLabel; } }

        public bool EditValue(IWin32Window owner, XmlSchemaType type, string input, out string output) {
            output = input;

            if (!string.IsNullOrEmpty(input)) {
                fd.FileName = GetAbsolute(input);
            }            
            if (fd.ShowDialog(owner) == DialogResult.OK) {
                output = GetRelative(fd.FileName);
                return true;
            } else {
                return false;
            }
        }

        string GetRelative(string s) {
            Uri baseUri = this.owner.BaseUri;
            if (baseUri != null) {
                try {
                    Uri uri = new Uri(s, UriKind.RelativeOrAbsolute);
                    return baseUri.MakeRelative(uri);
                } catch (UriFormatException) {
                    return s;
                }
            }
            return s;
        }

        string GetAbsolute(string s) {
            Uri baseUri = this.owner.BaseUri;
            if (baseUri != null) {
                try {
                    Uri uri = new Uri(s, UriKind.RelativeOrAbsolute);
                    Uri resolved = new Uri(baseUri, uri);
                    if (resolved.IsFile) return resolved.LocalPath;
                    return resolved.AbsoluteUri;
                } catch (UriFormatException) {
                    return s;
                }
            }
            return s;
        }
    }

    /// <summary>
    /// This is a custom editor for editing date/time values using the DateTimePicker.
    /// This editor is provided by default when you use xs:time, xs:dateTime or xs:date
    /// simple types in your schema, or you can specify this editor using the following 
    /// annotation in your schema: vs:editor="XmlNotepad.DateTimeEditor"
    /// where xmlns:vs="http://schemas.microsoft.com/Visual-Studio-Intellisense"
    /// </summary>
    public class DateTimeEditor : IXmlEditor, IDisposable {
        XmlSchemaType type;
        DateTimePicker picker = new DateTimePicker();
        string format = "yyyy-MM-dd";
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

        /// <summary>
        /// This property provides the XmlSchemaType for the editor
        /// </summary>
        public XmlSchemaType SchemaType { 
            get { return this.type; }
            set {
                this.type = value;
                if (type != null) {
                    DateTimeFormatInfo dtInfo = DateTimeFormatInfo.CurrentInfo;
                    switch (type.TypeCode){
                        case XmlTypeCode.DateTime:
                            format = SR.DateTimeFormat;
                            picker.Format =  DateTimePickerFormat.Custom;
                            picker.CustomFormat = dtInfo.ShortDatePattern + " " + dtInfo.LongTimePattern;
                            picker.ShowUpDown = false;
                            break;
                        case XmlTypeCode.Time:
                            picker.Format = DateTimePickerFormat.Time;
                            format = SR.TimeFormat;
                            picker.ShowUpDown = true;
                            break;
                        default:
                            picker.Format = DateTimePickerFormat.Short;
                            format = SR.DateFormat;
                            picker.ShowUpDown = false;
                            break;
                        
                    }
                    // Todo: set picker.MinDate and MaxDate based on the XmlSchemaFacet information, if any.
                }
            }
        }

        public Control Editor {
            get { return picker; }
        }

        public string XmlValue {
            get {
                return XmlConvert.ToString(picker.Value, format);
            }
            set {
                try {
                    picker.Text = value;
                } catch {
                    // ignore exceptions.
                }
            }
        }

        ~DateTimeEditor() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing){
            if (picker != null) {
                picker.Dispose();
                picker = null;
            }
        }

    }

}
