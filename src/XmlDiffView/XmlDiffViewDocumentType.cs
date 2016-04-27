//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffViewDocumentType.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Generate output data for differences in 
///    the DOCTYPE declaration (DTD) nodes. This
///    does not drill down into the components of
///    the DTD's internal subset.
// </summary>
// <history>
//      [barryw] 03MAR05 Created
// </history>
//  ---------------------------------------------------------------------------

namespace Microsoft.XmlDiffPatch
{
    #region Using directives

    using System;
    using System.Xml;
    using System.IO;
    using System.Diagnostics;
    
    #endregion

    /// <summary>
    /// Class to generate output data for differences in 
    /// the DOCTYPE declaration (DTD) nodes.
    /// </summary>
    /// <remarks>Programmer notes for future code 
    ///  enhancements: The PublicLiteral 
    ///  and the SystemLiteral are considered attributes. 
    ///  The attribute names are PUBLIC and SYSTEM. 
    ///  To retrieve the content of the attribute, use 
    ///  GetAttribute or another attribute accessing 
    ///  method.</remarks>
    internal class XmlDiffViewDocumentType : XmlDiffViewNode
    {
        #region Member variables section

        /// <summary>
        /// Name given to the DOCTYPE declaration (DTD).
        /// </summary>
        private string nameStore;

        private string systemIdStore;
        private string publicIdStore;
        private string internalDtdSubset;

        #endregion
        
        #region  Constructors section

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">declaration name</param>
        /// <param name="publicId">value of the PUBLIC  declaration 'attribute'</param>
        /// <param name="systemId">value of the SYSTEM declaration 'attribute'</param>
        /// <param name="documentTypeSubset">The inner declaration data</param>
        public XmlDiffViewDocumentType(string name, string publicId, string systemId, string documentTypeSubset) : base(XmlNodeType.DocumentType) 
        {
            this.Name = name;
            this.PublicId = (publicId == null) ? string.Empty : publicId;
            this.SystemId = (systemId == null) ? string.Empty : systemId;
            this.Subset = documentTypeSubset;
        }

        #endregion

        #region Properties section
      
        /// <summary>
        /// Gets and sets the value of the SYSTEM declaration 'attribute'
        /// </summary>
        public string SystemId
        {
            get
            {
                return this.systemIdStore;
            }
            set
            {
                this.systemIdStore = value;
            }
        }

        /// <summary>
        /// Gets and sets the declaration name
        /// </summary>
        public string Name
        {
            get
            {
                return this.nameStore;
            }
            set
            {
                this.nameStore = value;
            }
        }

        /// <summary>
        /// Gets and sets the value of the PUBLIC declaration 'attribute'
        /// </summary>
        public string PublicId
        {
            get
            {
                return this.publicIdStore;
            }
            set
            {
                this.publicIdStore = value;
            }
        }

        /// <summary>
        /// Gets and sets the inner data for the declaration
        /// </summary>
        public string Subset
        {
            get
            {
                return this.internalDtdSubset;
            }
            set
            {
                this.internalDtdSubset = value;
            }
        }

        /// <summary>
        /// Returns the complete declaration 
        /// </summary>
        public override string OuterXml
        {
            get
            {
                string dtd = "<!DOCTYPE " + this.Name + " ";
                if (this.PublicId != string.Empty)
                {
                    dtd += Tags.DtdPublic + "\"" + this.PublicId + "\" ";
                }
                else if (this.SystemId != string.Empty)
                {
                    dtd += Tags.DtdSystem + "\"" + this.SystemId + "\" ";
                }

                if (this.Subset != string.Empty)
                {
                    dtd += "[" + this.Subset + "]";
                }
                dtd += ">";
                return dtd;
            }
        }

        #endregion
        
        #region Methods section

        /// <summary>
        /// Creates a complete copy of the current node.
        /// </summary>
        /// <param name="deep">deprecated</param>
        /// <returns>Exception: Clone method should 
        /// never be called on a document type 
        /// node.</returns>
        [Obsolete("Clone method should never be called on document type node.", true)]
        internal override XmlDiffViewNode Clone(bool deep)
        {
            Debug.Assert(false, "Clone method should never be called on document type node.");
            return null;
        }

        /// <summary>
        /// Generates  output data in html form
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        internal override void DrawHtml(XmlWriter writer, int indent) 
        {
            if (Operation == XmlDiffViewOperation.Change) 
            {
                XmlDiffView.HtmlStartRow(writer);
                this.DrawLinkNode(writer);

                for (int i = 0; i < 2; i++) 
                {
                    XmlDiffView.HtmlStartCell(writer, indent);
                    // name
                    XmlDiffView.HtmlWriteString(
                        writer, 
                        XmlDiffViewOperation.Match, 
                        Tags.XmlDocumentTypeBegin);
                    if (i == 0)
                    {
                        XmlDiffView.HtmlWriteString(
                            writer, 
                            (this.Name == ChangeInformation.LocalName) ? XmlDiffViewOperation.Match : XmlDiffViewOperation.Change, 
                            this.Name);
                    }
                    else 
                    {
                        XmlDiffView.HtmlWriteString(
                            writer, 
                            (this.Name == ChangeInformation.LocalName) ? XmlDiffViewOperation.Match : XmlDiffViewOperation.Change, 
                            ChangeInformation.LocalName);
                    }
                    XmlDiffView.HtmlWriteString(
                        writer, 
                        XmlDiffViewOperation.Match, 
                        " ");

                    string systemString = "SYSTEM ";
                    // public id
                    if (this.PublicId == ChangeInformation.Prefix) 
                    {
                        // match
                        if (this.PublicId != string.Empty) 
                        {
                            XmlDiffView.HtmlWriteString(
                                writer, 
                                XmlDiffViewOperation.Match, 
                                Tags.DtdPublic + "\"" + this.PublicId + "\" ");
                            systemString = string.Empty;
                        }
                    }
                    else 
                    {
                        // add
                        if (this.PublicId == string.Empty) 
                        {
                            if (i == 1) 
                            {
                                XmlDiffView.HtmlWriteString(
                                    writer, 
                                    XmlDiffViewOperation.Add,
                                    Tags.DtdPublic + "\"" + ChangeInformation.Prefix + "\" ");
                                systemString = string.Empty;
                            }
                        }
                            // remove
                        else if (ChangeInformation.Prefix == string.Empty) 
                        {
                            if (i == 0) 
                            {
                                XmlDiffView.HtmlWriteString(
                                    writer, 
                                    XmlDiffViewOperation.Remove,
                                    Tags.DtdPublic + "\"" + this.PublicId + "\" ");
                                systemString = string.Empty;
                            }
                        }
                            // change
                        else 
                        {
                            XmlDiffView.HtmlWriteString(
                                writer, 
                                XmlDiffViewOperation.Change,
                                Tags.DtdPublic + "\"" + ((i == 0) ? this.PublicId : ChangeInformation.Prefix) + "\"");
                            systemString = string.Empty;
                        }
                    }

                    // system id
                    if (this.SystemId == ChangeInformation.NamespaceUri) 
                    {
                        if (this.SystemId != string.Empty) 
                        {
                            XmlDiffView.HtmlWriteString(
                                writer, 
                                XmlDiffViewOperation.Match, 
                                systemString  + "\"" + this.SystemId + "\" ");
                        }
                    }
                    else 
                    { 
                        // add 
                        if (this.SystemId == string.Empty) 
                        {
                            if (i == 1) 
                            {
                                XmlDiffView.HtmlWriteString(
                                    writer, 
                                    XmlDiffViewOperation.Add, 
                                    systemString  + "\"" + ChangeInformation.NamespaceUri + "\" ");
                            }                
                        }
                            // remove
                        else if (ChangeInformation.Prefix == string.Empty) 
                        {
                            if (i == 0) 
                            {
                                XmlDiffView.HtmlWriteString(
                                    writer, 
                                    XmlDiffViewOperation.Remove, 
                                    systemString  + "\"" + this.SystemId + "\"");
                            }
                        }
                            // change
                        else 
                        {
                            XmlDiffView.HtmlWriteString(
                                writer, 
                                XmlDiffViewOperation.Change, 
                                systemString  + "\"" + ((i == 0) ? this.SystemId : ChangeInformation.NamespaceUri) + "\" ");
                        }
                    }

                    // internal subset
                    if (this.Subset == ChangeInformation.Subset) 
                    {
                        if (this.Subset != string.Empty) 
                        {
                            XmlDiffView.HtmlWriteString(
                                writer, 
                                XmlDiffViewOperation.Match, 
                                "[" + this.Subset + "]");
                        }
                    }
                    else 
                    {
                        // add 
                        if (this.Subset == string.Empty) 
                        {
                            if (i == 1) 
                            {
                                XmlDiffView.HtmlWriteString(
                                    writer, 
                                    XmlDiffViewOperation.Add, 
                                    "[" + ChangeInformation.Subset + "]");
                            }                
                        }
                            // remove
                        else if (ChangeInformation.Subset == string.Empty) 
                        {
                            if (i == 0) 
                            {
                                XmlDiffView.HtmlWriteString(
                                    writer, 
                                    XmlDiffViewOperation.Remove, 
                                    "[" + this.Subset + "]");
                            }
                        }
                            // change
                        else 
                        {
                            XmlDiffView.HtmlWriteString(
                                writer, 
                                XmlDiffViewOperation.Change, 
                                "[" + ((i == 0) ? this.Subset : ChangeInformation.Subset) + "]");
                        }
                    }

                    // close start tag
                    XmlDiffView.HtmlWriteString(
                        writer, 
                        XmlDiffViewOperation.Match, 
                        Tags.XmlDocumentTypeEnd);
                    XmlDiffView.HtmlEndCell(writer);
                }
                XmlDiffView.HtmlEndRow(writer);
            }
            else 
            {
                DrawHtmlNoChange(writer, indent);
            }
        }

        /// <summary>
        /// Add the DocumentType data to the output. 
        /// </summary>
        /// <param name="writer">Output data stream</param>
        /// <param name="indent">current size of text indentation</param>
        /// <remarks>If the DOCTYPE declaration includes
        /// declarations that are to be combined with 
        /// external files or the external subset, it 
        /// uses the following syntax.
        ///   DOCTYPE rootElement SYSTEM "URIreference"
        ///     [declarations]
        ///   or 
        /// DOCTYPE rootElement PUBLIC "PublicIdentifier" "URIreference"
        /// [declarations]
        /// </remarks>
        internal override void DrawText(TextWriter writer, int indent)
        {
            Debug.Assert(NodeType == XmlNodeType.DocumentType);
            // indent the text.
            writer.Write(XmlDiffView.IndentText(indent));
            switch (Operation)
            {
                case XmlDiffViewOperation.Add:
                    this.DrawTextAdd(writer, indent);
                    break;
                case XmlDiffViewOperation.Change:
                    this.DrawTextChange(writer, indent);
                    break;
                case XmlDiffViewOperation.Ignore:
                case XmlDiffViewOperation.MoveFrom:
                    this.DrawTextMoveFrom(writer, indent);
                    break;
                case XmlDiffViewOperation.MoveTo:
                    this.DrawTextMoveTo(writer, indent);
                    break;
                case XmlDiffViewOperation.Match:
                    // for 'Ignore' and match operations
                    // write out the baseline data
                    this.DrawTextNoChange(writer, indent);
                    break;
                case XmlDiffViewOperation.Remove:
                    this.DrawTextRemove(writer, indent);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
            writer.Write(writer.NewLine);

        }

        /// <summary>
        /// Generates the original document type sudo-attribute
        /// </summary>
        /// <returns>document type sudo-attribute</returns>
        private string DocumentTypeSudoAttributes()
        {
            string systemString = "SYSTEM ";
            const string publicString = "PUBLIC ";
            string attributes = " ";
            switch (Operation)
            {
                case XmlDiffViewOperation.Add:
                case XmlDiffViewOperation.Remove:
                case XmlDiffViewOperation.MoveFrom:
                case XmlDiffViewOperation.Match:
                    // for 'add'/'remove'/'move from' differences and
                    // match the values are in the regular properties, 
                    // not the changed information object 
                    if ((null != this.PublicId) &&
                        (this.PublicId != string.Empty))
                    {
                        attributes += publicString + "\"" + this.PublicId + "\" ";
                    }
                    else if ((null != this.SystemId) &&
                        (this.SystemId != string.Empty))
                    {
                        attributes += systemString + "\"" + this.SystemId + "\" ";
                    }
                    else if (null != ChangeInformation)
                    {
                        if ((null != ChangeInformation.Prefix) &&
                        (ChangeInformation.Prefix != string.Empty))
                        {
                            Debug.Assert(
                                false,
                                "Unexpected value ",
                                publicString + "\"" + ChangeInformation.Prefix + "\" ");
                        }
                        else if ((null != ChangeInformation.NamespaceUri) &&
                        (ChangeInformation.NamespaceUri != string.Empty))
                        {
                            Debug.Assert(
                                false,
                                "Unexpected value ",
                                systemString + "\"" + ChangeInformation.NamespaceUri + "\" ");
                        }
                    }
                    break;
                case XmlDiffViewOperation.MoveTo:
                    // for ''move to' differences the values
                    // are in the changed information object
                    if ((null != ChangeInformation.Prefix) &&
                        (ChangeInformation.Prefix != string.Empty))
                    {
                        attributes += publicString + "\"" + ChangeInformation.Prefix + "\" ";
                    }
                    else if ((null != ChangeInformation.NamespaceUri) &&
                        (ChangeInformation.NamespaceUri != string.Empty))
                    {
                        attributes += systemString + "\"" + ChangeInformation.NamespaceUri + "\" ";
                    }
                    else if ((null != this.PublicId) &&
                        (this.PublicId != string.Empty))
                    {
                        Debug.Assert(
                            false,
                            "Unexpected value ",
                            publicString + "\"" + this.PublicId + "\" ");
                    }
                    else if ((null != this.SystemId) &&
                   (this.SystemId != string.Empty))
                    {
                        Debug.Assert(
                            false,
                            "Unexpected value ",
                            systemString + "\"" + this.SystemId + "\" ");
                    }
                    break;
                case XmlDiffViewOperation.Ignore:
                    attributes = string.Empty;
                    break;
                case XmlDiffViewOperation.Change:
                    attributes = " ";
                    // check for changes in the public "attribute" value
                    if (((null != this.PublicId) ||
                        (null != ChangeInformation.Prefix)) &&
                        (this.PublicId == ChangeInformation.Prefix))
                    {
                        // match
                        if (string.Empty != this.PublicId)
                        {
                            attributes += Tags.DtdPublic + "\"" + this.PublicId + "\" ";
                            systemString = string.Empty;
                        }
                    }
                    else
                    {
                        if ((string.Empty == this.PublicId) &&
                            (string.Empty != ChangeInformation.Prefix))
                        {
                            // add
                            attributes += Difference.Tag + Difference.DocumentTypeAdded;
                            attributes += " " + Tags.DtdPublic + "\"" + ChangeInformation.Prefix + "\" ";
                            systemString = string.Empty;
                        }
                        else if ((string.Empty == ChangeInformation.Prefix) &&
                            (string.Empty != this.PublicId))
                        {
                            // remove
                            attributes += Difference.Tag + Difference.DocumentTypeDeleted;
                            attributes += Tags.DtdPublic + "\"" + this.PublicId + "\" ";
                            systemString = string.Empty;
                        }
                        else
                        {
                            // if both have values, they must be different
                            if ((string.Empty != ChangeInformation.Prefix) &&
                                (string.Empty != this.PublicId))
                            {
                                // change
                                attributes += Difference.Tag + "=" +
                                    Difference.ChangeBegin +
                                    Tags.DtdPublic + "\"" + this.PublicId + "\" " +
                                    Difference.ChangeTo +
                                    Tags.DtdPublic + "\"" + ChangeInformation.Prefix +
                                    "\" " +
                                    Difference.ChangeEnd;
                                systemString = string.Empty;
                            }
                        }
                    }

                    // system id
                    if (((null != this.SystemId) ||
                        (null != ChangeInformation.NamespaceUri)) &&
                        (this.SystemId == ChangeInformation.NamespaceUri))
                    {
                        // match
                        if (this.SystemId != string.Empty)
                        {
                            attributes += systemString + "\"" + this.SystemId + "\" ";
                        }
                    }
                    else
                    {
                        if ((string.Empty == this.SystemId) &&
                            (string.Empty != ChangeInformation.NamespaceUri))
                        {
                            // add 
                            attributes += Difference.Tag + Difference.DocumentTypeAdded;
                            attributes += systemString + "\"" +
                                ChangeInformation.NamespaceUri + "\" ";
                        }
                        // remove
                        else if ((ChangeInformation.Prefix == string.Empty) &&
                            (string.Empty != this.SystemId))
                        {
                            attributes += Difference.Tag + Difference.DocumentTypeDeleted;
                            attributes += systemString + "\"" + this.SystemId + "\" ";
                        }
                        // change
                        else
                        {
                            // if both have values, they must be different
                            if ((string.Empty != ChangeInformation.NamespaceUri) &&
                                (string.Empty != this.SystemId))
                            {
                                // change
                                attributes += Difference.Tag + "=" +
                                    Difference.ChangeBegin +
                                    systemString + "\"" + this.SystemId + "\" " +
                                    Difference.ChangeTo +
                                    systemString + "\"" + ChangeInformation.NamespaceUri +
                                    "\" " +
                                    Difference.ChangeEnd;
                            }
                        }
                    }
                    break;
                default:
                    Trace.WriteLine("This differencing operation is not recognized");
                    throw new ArgumentOutOfRangeException(
                        "Operation",
                        Operation,
                        "This differencing operation is not recognized");
            }
            return attributes;
        }

        /// <summary>
        /// Generates output data in text form for differences
        /// due to adding data
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        private void DrawTextAdd(
            TextWriter writer,
            int indent)
        {
            writer.Write(Tags.XmlDocumentTypeBegin +
                this.Name + this.DocumentTypeSudoAttributes() +
                "[" + writer.NewLine +
                XmlDiffView.IndentText(indent + indent) +
                Tags.XmlCommentOldStyleBegin + " " + Difference.Tag + 
                Difference.DocumentTypeAdded +
                " " + Tags.XmlCommentOldStyleEnd + writer.NewLine +
                this.internalDtdSubset +
                writer.NewLine + "]" +
                Tags.XmlDocumentTypeEnd);
        }

        /// <summary>
        /// Generates output data in text form for differences
        /// due to moving data from a location
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        private void DrawTextMoveFrom(
            TextWriter writer,
            int indent)
        {
            // generate the dtd name and sudo-attributes
            writer.Write(Tags.XmlDocumentTypeBegin +
                this.Name + this.DocumentTypeSudoAttributes() +
                "[" + writer.NewLine);
            // generate the main body of the dtd.
            writer.Write(XmlDiffView.IndentText(indent + indent) +
                writer.NewLine);
            // include a comment about the difference.
            writer.Write(Tags.XmlCommentOldStyleBegin + " " + Difference.Tag +
                Difference.DocumentTypeMovedFromBegin + OperationId +
                Difference.DocumentTypeMovedFromEnd +
                " " + Tags.XmlCommentOldStyleEnd + writer.NewLine);
            // include main body and closing tags
            writer.Write(XmlDiffView.IndentText(indent + indent) +
                this.internalDtdSubset +
                writer.NewLine + "]" +
                Tags.XmlDocumentTypeEnd);
        }

        /// <summary>
        /// Generates output data in text form for differences
        /// due to moving data to a new location
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        private void DrawTextMoveTo(
            TextWriter writer,
            int indent)
        {
            // generate the dtd name and sudo-attributes
            writer.Write(Tags.XmlDocumentTypeBegin +
                this.Name + this.DocumentTypeSudoAttributes() +
                "[" + writer.NewLine);
            // generate the main body of the dtd.
            writer.Write(XmlDiffView.IndentText(indent + indent) +
                writer.NewLine);
            // include a comment about the difference.
            writer.Write(Tags.XmlCommentOldStyleBegin + " " + Difference.Tag +
                Difference.DocumentTypeMovedToBegin + OperationId +
                Difference.DocumentTypeMovedToEnd +
                " " + Tags.XmlCommentOldStyleEnd + writer.NewLine);
            // include main body and closing tags
            writer.Write(XmlDiffView.IndentText(indent + indent) +
                this.internalDtdSubset +
                writer.NewLine + "]" +
                Tags.XmlDocumentTypeEnd);
        }

        /// <summary>
        /// Generates output data in text form for differences
        /// due to removing data
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        private void DrawTextRemove(
            TextWriter writer,
            int indent)
        {
            // generate the dtd name and sudo-attributes
            writer.Write(Tags.XmlDocumentTypeBegin +
                this.Name + this.DocumentTypeSudoAttributes() +
                "[" + writer.NewLine);
            // generate the main body of the dtd.
            writer.Write(XmlDiffView.IndentText(indent + indent));
            // include a comment about the difference.
            writer.Write(Tags.XmlCommentOldStyleBegin + " " + Difference.Tag +
                Difference.DocumentTypeDeleted +
                " " + Tags.XmlCommentOldStyleEnd + writer.NewLine);
            // include main body and closing tags
            writer.Write(XmlDiffView.IndentText(indent + indent) +
                this.internalDtdSubset +
                writer.NewLine +
                XmlDiffView.IndentText(indent + indent) +
                "]" +
                Tags.XmlDocumentTypeEnd);
        }

        /// <summary>
        /// Generates output data in text form for differences
        /// due to changing data
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        private void DrawTextChange(
            TextWriter writer,
            int indent)
        {
            // generate the dtd name and sudo-attributes
            writer.Write(Tags.XmlDocumentTypeBegin +
                this.Name + this.DocumentTypeSudoAttributes() +
                "[" + writer.NewLine);
            // generate the main body of the dtd.
            writer.Write(XmlDiffView.IndentText(indent + indent) +
                writer.NewLine);
            // include a comment about the difference showing the old subset data.
            writer.Write(Tags.XmlCommentOldStyleBegin + writer.NewLine +
                " " + Difference.Tag +
                Difference.ChangeBegin + this.internalDtdSubset + Difference.ChangeEnd +
                writer.NewLine +
                " " + Tags.XmlCommentOldStyleEnd + 
                writer.NewLine);
            // include main body and closing tags
            writer.Write(XmlDiffView.IndentText(indent + indent) +
                changeInfo.Subset +
                writer.NewLine + "]" +
                Tags.XmlDocumentTypeEnd);
        }

        /// <summary>
        /// Generates output data in text form for differences
        /// due to changing existing data
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        private new void DrawTextNoChange(
            TextWriter writer,
            int indent)
        {
            string dtd = Tags.XmlDocumentTypeBegin +
                this.Name + this.DocumentTypeSudoAttributes() +
                this.internalDtdSubset +
                Tags.XmlDocumentTypeEnd;
        }

        #endregion
        
    }
}