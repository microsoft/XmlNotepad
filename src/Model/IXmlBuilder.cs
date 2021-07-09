using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Schema;

namespace XmlNotepad
{
    /// <summary>
    /// Represents the application window object.
    /// </summary>
    public interface IHostWindow
    {
    }

    /// <summary>
    /// Represents UI editor object
    /// </summary>
    public interface IEditorControl
    {
    }

    /// <summary>
    /// This interface is used to provide extensible popup modal dialog for editing a particular
    /// type of value in the XML document.  (e.g. color picker).
    /// </summary>
    public interface IXmlBuilder
    {

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
        bool EditValue(IHostWindow owner, XmlSchemaType type, string input, out string output);
    }

    /// <summary>
    /// This interface is used to provide other types of editors besides the default TextBox for
    /// inline editing of particular types of values in the XML document.  For example, DateTimePicker.
    /// </summary>
    public interface IXmlEditor
    {

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
        IEditorControl Editor { get; }

        /// <summary>
        /// The setter is called just before editing to pass in the current value from the 
        /// XmlDocument.  At the end of editing, the getter is called to pull the new value
        /// back out of the editor for storing in the XmlDocument.
        /// </summary>
        string XmlValue { get; set; }
    }

}
