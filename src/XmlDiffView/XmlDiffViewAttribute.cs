//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffViewAttribute.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Manages the output for viewing changes in xml attributes.
// </summary>
// <history>
//      [barryw] 03MAR05 Created
// </history>
//  ---------------------------------------------------------------------------

namespace Microsoft.XmlDiffPatch
{
    #region Using directives

    using System;
    using System.IO;
    using System.Xml;
    // Diagnostics is used for trace and debug.
    using System.Diagnostics;

    #endregion

    /// <summary>
    /// Class the manage the output for viewing xml attributes.
    /// </summary>
    internal class XmlDiffViewAttribute : XmlDiffViewNode
    {
        #region Member variables section

        /// <summary>
        /// Declares a variation to store the
        /// xml prefix value (without the colon).
        /// </summary>
        private string prefix;

        /// <summary>
        /// Declares a variation to store the node's localName
        /// (the name without the prefix).
        /// </summary>
        /// <remarks>prefix + ":" + localName = name;</remarks>
        private string localName;

        /// <summary>
        /// Declares a variation to store the node's full name.
        /// </summary>
        /// <remarks>the localName with the prefix and colon separator</remarks>
        private string name;

        /// <summary>
        /// Declares a variation to store the
        /// xml namespaceUri value.
        /// </summary>
        private string namespaceUri;
        /// <summary>
        /// Declares a variation to store the
        /// xml attribute value.
        /// </summary>
        private string attributeValue;

        #endregion
        
        #region  Constructors section

        /// <summary>
        /// Alternative constructor
        /// </summary>
        /// <param name="localName">Attribute name without prefix</param>
        /// <param name="prefix">Attribute prefix</param>
        /// <param name="ns">Namespace URI</param>
        /// <param name="name">Attribute name including prefix</param>
        /// <param name="attributeValue">the attribute's value</param>
        public XmlDiffViewAttribute(
            string localName,
            string prefix,
            string ns,
            string name,
            string attributeValue) : base(XmlNodeType.Attribute)
        {
            this.LocalName = localName;
            this.Prefix = prefix;
            this.NamespaceUri = ns;
            this.AttributeValue = attributeValue;
            this.Name = name;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="localName">Attribute name without prefix</param>
        /// <param name="prefix">Attribute prefix</param>
        /// <param name="ns">Namespace URI</param>
        /// <param name="attributeValue">the attribute's value</param>
        internal XmlDiffViewAttribute(
            string localName,
            string prefix,
            string ns,
            string attributeValue) 
            : base(XmlNodeType.Attribute)
        {
            this.LocalName = localName;
            this.Prefix = prefix;
            this.NamespaceUri = ns;
            this.AttributeValue = attributeValue;

            if (prefix == string.Empty)
            {
                this.Name = this.LocalName;
            }
            else
            {
                this.Name = prefix + ":" + this.LocalName;
            }
        }

        #endregion
        
        #region Properties section

        /// <summary>
        /// Gets or sets the attribute's value.
        /// </summary>
        public string AttributeValue
        {
            get
            {
                return this.attributeValue;
            }
            set
            {
                this.attributeValue = value;
            }
        }

        /// <summary>
        /// Gets or sets the xml node prefix (without the semi-colon) 
        /// </summary>
        /// <example>xls</example>
        public string Prefix
        {
            get
            {
                return this.prefix;
            }
            set
            {
                this.prefix = value;
            }
        }

        /// <summary>
        /// Gets or sets the node name with, if present,
        /// a prefix
        /// </summary>
        /// <example>xls:mynode</example>
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
            }
        }

        /// <summary>
        /// Gets or sets the node name without a prefix.
        /// </summary>
        /// <example>mynode</example>
        public string LocalName
        {
            get
            {
                return this.localName;
            }
            set
            {
                this.localName = value;
            }
        }

        /// <summary>
        /// Gets or sets the namespace Uniform 
        /// Resource Identifier (URI).
        /// </summary>
        public string NamespaceUri
        {
            get
            {
                return this.namespaceUri;
            }
            set
            {
                this.namespaceUri = value;
            }
        }

        /// <summary>
        /// Returns the string representing the attribute name
        /// and its value.
        /// </summary>
        public override string OuterXml
        {
            get
            {
                string outerXml = string.Empty;
                if (this.Prefix != string.Empty)
                {
                    outerXml = this.Prefix + ":";
                }
                outerXml += this.LocalName + "=\"" +
                    RemoveTabsAndNewlines(this.attributeValue) + "\"";
                return outerXml;
            }
        }

        #endregion
        
        #region Methods section

        /// <summary>
        /// Gets a complete copy of the current attribute.
        /// </summary>
        /// <param name="deep">deprecated</param>
        /// <returns>an attribute object</returns>
        internal override XmlDiffViewNode Clone(bool deep)
        {
            //barryw 3/14/2005 Corrected parameter order to fix 
            //                 empty Name bug.
            return new XmlDiffViewAttribute(
                this.LocalName, 
                this.Prefix, 
                this.NamespaceUri, 
                this.Name, 
                this.AttributeValue);
        }

        /// <summary>
        /// Override for the method to add this node's data 
        /// to the output stream. This method should never 
        /// be called from this object.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">size of indent</param>
        [Obsolete("This method should never be called", true)]
        internal override void DrawHtml(XmlWriter writer, int indent)
        {
            throw new Exception("This method should never be called.");
        }

        /// <summary>
        /// Override for the method to add this node's data 
        /// in text form to the output stream. This method 
        /// should never be called from this object.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">size of indent</param>
        [Obsolete("This method should never be called", true)]
        internal override void DrawText(TextWriter writer, int indent)
        {
            throw new Exception("This method should never be called.");
        }

        /// <summary>
        /// Generate html output data for a differences 
        /// due to a change in an attribute.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="attr">Attribute object</param>
        /// <param name="typeOfDifference">type of difference</param>
        public void DrawHtmlAttribute(
            XmlWriter writer,
            bool ignorePrefixes,
            XmlDiffViewOperation typeOfDifference)
        {
            bool opid = false;
            if (this.OperationId != XmlDiffView.LastVisitedOpId)
            {
                XmlDiffView.LastVisitedOpId = this.OperationId;
                // only write this anchor if the parent elemnt was not also changed.
                if (this.OperationId != 0 && this.Parent.Operation != this.Operation &&
                    (this.PreviousSibling == null || this.PreviousSibling.Operation != this.Operation))
                {
                    writer.WriteStartElement("a");
                    writer.WriteAttributeString("name", "id" + this.OperationId);
                    opid = true;
                }
            }
            if (ignorePrefixes)
            {
                if (this.Prefix == "xmlns" || (this.LocalName == "xmlns" &&
                    this.Prefix == string.Empty))
                {
                    XmlDiffView.HtmlWriteString(
                        writer,
                        XmlDiffViewOperation.Ignore,
                        this.Name);
                    XmlDiffView.HtmlWriteString(
                        writer,
                        typeOfDifference,
                        "=\"" + this.AttributeValue + "\"");
                    return;
                }
                else if (this.Prefix != string.Empty)
                {
                    XmlDiffView.HtmlWriteString(
                        writer,
                        XmlDiffViewOperation.Ignore,
                        this.Prefix + ":");
                    XmlDiffView.HtmlWriteString(
                        writer,
                        typeOfDifference,
                        this.LocalName + "=\"" + this.AttributeValue + "\"");
                    return;
                }
            }

            XmlDiffView.HtmlWriteString(
                writer,
                typeOfDifference,
                this.Name + "=\"" + this.AttributeValue + "\"");

            if (opid)
            {
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Generates output data in html for a difference due
        /// to changing attribute data.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="attr">Attribute object</param>
        /// <param name="localName">name of attribute 
        /// (without the prefix)</param>
        /// <param name="prefix">xml attribute prefix</param>
        /// <param name="attributeValue">The value for the attribute.</param>
        public void DrawHtmlAttributeChange(
            XmlWriter writer,
            string localName,
            string prefix,
            string attributeValue,
            bool ignorePrefixes)
        {
            bool opid = false;

            if (this.OperationId != XmlDiffView.LastVisitedOpId)
            {
                // only write this anchor if the parent elemnt was not also changed.
                if (this.OperationId != 0 && this.Parent.Operation == XmlDiffViewOperation.Match &&
                    (this.PreviousSibling == null || this.PreviousSibling.Operation == XmlDiffViewOperation.Match))
                {
                    XmlDiffView.LastVisitedOpId = this.OperationId;
                    writer.WriteStartElement("a");
                    writer.WriteAttributeString("name", "id" + this.OperationId);
                    opid = true;
                }
            }
            if (prefix != string.Empty)
            {
                XmlDiffView.HtmlWriteString(
                    writer,
                    ignorePrefixes ? XmlDiffViewOperation.Ignore : (this.Prefix == this.ChangeInformation.Prefix) ? XmlDiffViewOperation.Match : XmlDiffViewOperation.Change,
                    prefix + ":");
            }

            XmlDiffView.HtmlWriteString(
                writer,
                (this.LocalName == this.ChangeInformation.LocalName) ? XmlDiffViewOperation.Match : XmlDiffViewOperation.Change,
                this.localName);

            if (this.AttributeValue != this.ChangeInformation.Subset)
            {
                XmlDiffView.HtmlWriteString(writer, "=\"");
                XmlDiffView.HtmlWriteString(
                    writer,
                    XmlDiffViewOperation.Change,
                    attributeValue);
                XmlDiffView.HtmlWriteString(writer, "\"");
            }
            else
            {
                XmlDiffView.HtmlWriteString(
                    writer,
                    "=\"" + attributeValue + "\"");
            }
            if (opid)
            {
                writer.WriteEndElement();
            }
        }

        #endregion

    }

}