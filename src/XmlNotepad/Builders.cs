using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using SR = XmlNotepad.StringResources;

namespace XmlNotepad
{
    /// <summary>
    /// This is a custom builder for editing color values using the ColorDialog.
    /// You can specify this builder using the following annotation in your schema:
    ///     vs:builder="XmlNotepad.ColorBuilder"
    /// where xmlns:vs="http://schemas.microsoft.com/Visual-Studio-Intellisense"
    /// </summary>
    public class ColorBuilder : IXmlBuilder
    {
        private ColorDialog _cd = new ColorDialog();
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

        public string Caption { get { return SR.ColorPickerLabel; } }

        public bool EditValue(IHostWindow owner, XmlSchemaType type, string input, out string output)
        {
            output = input;
            ColorConverter cc = new ColorConverter();
            Color c = Color.Black;
            try
            {
                c = (Color)cc.ConvertFromString(input);
            }
            catch
            {
            }
            _cd.Color = c;
            _cd.AnyColor = true;
            if (_cd.ShowDialog(owner as IWin32Window) == DialogResult.OK)
            {
                output = cc.ConvertToString(_cd.Color);
                return true;
            }
            else
            {
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
    public class UriBuilder : IXmlBuilder
    {
        private OpenFileDialog _fd = new OpenFileDialog();
        private ISite _site; 
        private IIntellisenseProvider _owner;

        public UriBuilder()
        {
            _fd.Filter = "All files (*.*)|*.*";
        }


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

        public string Caption { get { return SR.UriBrowseLabel; } }

        public bool EditValue(IHostWindow owner, XmlSchemaType type, string input, out string output)
        {
            output = input;

            if (!string.IsNullOrEmpty(input))
            {
                _fd.FileName = GetAbsolute(input);
            }
            if (_fd.ShowDialog(owner as IWin32Window) == DialogResult.OK)
            {
                output = GetRelative(_fd.FileName);
                return true;
            }
            else
            {
                return false;
            }
        }

        string GetRelative(string s)
        {
            Uri baseUri = this._owner.BaseUri;
            if (baseUri != null)
            {
                try
                {
                    Uri uri = new Uri(s, UriKind.RelativeOrAbsolute);
                    Uri rel = baseUri.MakeRelativeUri(uri);
                    return rel.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped);
                }
                catch (UriFormatException)
                {
                    return s;
                }
            }
            return s;
        }

        string GetAbsolute(string s)
        {
            Uri baseUri = this._owner.BaseUri;
            if (baseUri != null)
            {
                try
                {
                    Uri uri = new Uri(s, UriKind.RelativeOrAbsolute);
                    Uri resolved = new Uri(baseUri, uri);
                    if (resolved.IsFile) return resolved.LocalPath;
                    return resolved.AbsoluteUri;
                }
                catch (UriFormatException)
                {
                    return s;
                }
            }
            return s;
        }
    }

    public class CustomDateTimePicker : DateTimePicker, IEditorControl
    { 
    }

    /// <summary>
    /// This is a custom editor for editing date/time values using the DateTimePicker.
    /// This editor is provided by default when you use xs:time, xs:dateTime or xs:date
    /// simple types in your schema, or you can specify this editor using the following 
    /// annotation in your schema: vs:editor="XmlNotepad.DateTimeEditor"
    /// where xmlns:vs="http://schemas.microsoft.com/Visual-Studio-Intellisense"
    /// </summary>
    public class DateTimeEditor : IXmlEditor, IDisposable
    {
        private XmlSchemaType _type;
        private CustomDateTimePicker _picker = new CustomDateTimePicker();
        private string _format = "yyyy-MM-dd";
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

        /// <summary>
        /// This property provides the XmlSchemaType for the editor
        /// </summary>
        public XmlSchemaType SchemaType
        {
            get { return this._type; }
            set
            {
                this._type = value;
                if (_type != null)
                {
                    DateTimeFormatInfo dtInfo = DateTimeFormatInfo.CurrentInfo;
                    switch (_type.TypeCode)
                    {
                        case XmlTypeCode.DateTime:
                            _format = SR.DateTimeFormat;
                            _picker.Format = DateTimePickerFormat.Custom;
                            _picker.CustomFormat = dtInfo.ShortDatePattern + " " + dtInfo.LongTimePattern;
                            _picker.ShowUpDown = false;
                            break;
                        case XmlTypeCode.Time:
                            _picker.Format = DateTimePickerFormat.Time;
                            _format = SR.TimeFormat;
                            _picker.ShowUpDown = true;
                            break;
                        default:
                            _picker.Format = DateTimePickerFormat.Short;
                            _format = SR.DateFormat;
                            _picker.ShowUpDown = false;
                            break;

                    }
                    // Todo: set picker.MinDate and MaxDate based on the XmlSchemaFacet information, if any.
                }
            }
        }

        public IEditorControl Editor
        {
            get { return _picker; }
        }

        public string XmlValue
        {
            get
            {
                return XmlConvert.ToString(_picker.Value, _format);
            }
            set
            {
                try
                {
                    _picker.Text = value;
                }
                catch
                {
                    // ignore exceptions.
                }
            }
        }

        ~DateTimeEditor()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_picker != null)
            {
                _picker.Dispose();
                _picker = null;
            }
        }

    }

}
