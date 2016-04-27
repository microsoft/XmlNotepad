//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffViewCharData.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Generate output data for character 
//     based data, e.g., text, CharData, and comments.
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
    using System.Diagnostics;
    
    #endregion

    /// <summary>
    /// Class to generate output data for character 
    /// based data, e.g., text, CharData, and comments.
    /// </summary>
    internal class XmlDiffViewCharData : XmlDiffViewNode
    {
        #region Member variables section

        private string openString;
        private string closeString;

        /// <summary>
        /// Hold the value of the innerText for the node.
        /// </summary>
        private string innerText;

        #endregion

        #region  Constructors section

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">innerText for this node</param>
        /// <param name="nodeType">type of node</param>
        internal XmlDiffViewCharData(
            string value, 
            XmlNodeType nodeType) : base(nodeType)
        {
            this.InnerText = value;
        }
        
        #endregion

        #region Properties section

        /// <summary>
        /// Gets or sets the inner text value.  Text returned 
        /// is free of tabs and newline characters.
        /// </summary>
        public string InnerText
        {
            get
            {
                return RemoveTabsAndNewlines(this.innerText);
            }
            set
            {
                this.innerText = value;
            }
        }

        /// <summary>
        /// Returns innerText of current node.
        /// </summary>
        /// <value>innerText stripped of tabs and newline characters.</value>
        public override string OuterXml
        {
            get
            {
                switch (NodeType)
                {
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                        return this.InnerText;
                    case XmlNodeType.Comment:
                        return Tags.XmlCommentOldStyleBegin + 
                            this.InnerText + Tags.XmlCommentOldStyleEnd;
                    case XmlNodeType.CDATA:
                        return Tags.XmlCharacterDataBegin + this.InnerText + 
                            Tags.XmlCharacterDataEnd;
                    default:
                        Debug.Assert(false, "Invalid node type.");
                        return string.Empty;
                }
            }
        }

        #endregion

        #region Methods section

        /// <summary>
        /// Creates a complete copy of the current node.
        /// </summary>
        /// <param name="deep">this parameter is deprecated</param>
        /// <returns>a node object of type XmlDiffViewCharData</returns>
        internal override XmlDiffViewNode Clone(bool deep)
        {
            return new XmlDiffViewCharData(this.InnerText, NodeType);
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
                string openString = string.Empty;
                string closeString = string.Empty;
                //Note: OuterXml function is not used here 
                //      in order that the tags
                //      can correctly wrap the data.
                if (NodeType == XmlNodeType.CDATA)
                {
                    openString = Tags.XmlCharacterDataBegin;
                    closeString = Tags.XmlCharacterDataEnd;
                }
                else if (NodeType == XmlNodeType.Comment)
                {
                    openString = Tags.XmlCommentOldStyleBegin;
                    closeString = Tags.XmlCommentOldStyleEnd;
                }

                XmlDiffView.HtmlStartRow(writer);
                this.DrawLinkNode(writer);
                XmlDiffView.HtmlStartCell(writer, indent);
                if (openString != string.Empty)
                {
                    XmlDiffView.HtmlWriteString(writer, openString);
                    XmlDiffView.HtmlWriteString(
                        writer, 
                        XmlDiffViewOperation.Change, 
                        this.InnerText);
                    XmlDiffView.HtmlWriteString(writer, closeString);
                }
                else
                {
                    XmlDiffView.HtmlWriteString(
                        writer, 
                        XmlDiffViewOperation.Change, 
                        this.InnerText);
                }
                XmlDiffView.HtmlEndCell(writer);                
                XmlDiffView.HtmlStartCell(writer, indent);

                if (openString != string.Empty)
                {
                    XmlDiffView.HtmlWriteString(writer, openString);
                    XmlDiffView.HtmlWriteString(
                        writer, 
                        XmlDiffViewOperation.Change, 
                        ChangeInformation.Subset);
                    XmlDiffView.HtmlWriteString(writer, closeString);
                }
                else
                {
                    XmlDiffView.HtmlWriteString(
                        writer, 
                        XmlDiffViewOperation.Change, 
                        ChangeInformation.Subset);
                }
                XmlDiffView.HtmlEndCell(writer);
                XmlDiffView.HtmlEndRow(writer);
            }
            else
            {
                DrawHtmlNoChange(writer, indent);
            }
        }
        
        /// <summary>
        /// Generates  output data in text form
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        internal override void DrawText(TextWriter writer, int indent)
        {
            Debug.Assert(XmlNodeType.Comment == NodeType ||
                XmlNodeType.CDATA == NodeType ||
                XmlNodeType.Text == NodeType);
            
            // indent the text.
            writer.Write(XmlDiffView.IndentText(indent));

            switch (Operation)
            {
                case XmlDiffViewOperation.Add:
                    this.DrawTextAdd(writer);
                    break;
                case XmlDiffViewOperation.Change:
                    this.DrawTextChange(writer);
                    break;
                case XmlDiffViewOperation.Ignore:
                    // suppress the output                    
                    break;
                case XmlDiffViewOperation.Match:
                    DrawTextNoChange(writer, indent);
                    break;
                case XmlDiffViewOperation.MoveFrom:
                    Debug.Assert(false);
                    break;
                case XmlDiffViewOperation.MoveTo:
                    Debug.Assert(false);
                    break;
                case XmlDiffViewOperation.Remove:
                    this.DrawTextDelete(writer);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }

        /// <summary>
        /// Set xml tags based on the type of object.
        /// </summary>
        /// <param name="nodeType">The type of xml node.</param>
        private void SetOpeningAndClosingTags(XmlNodeType nodeType)
        {
            switch (nodeType)
            {
                case XmlNodeType.CDATA:
                    this.openString = Tags.XmlCharacterDataBegin;
                    this.closeString = Tags.XmlCharacterDataEnd;
                    break;
                case XmlNodeType.Comment:
                    this.openString = Tags.XmlCommentOldStyleBegin;
                    this.closeString = Tags.XmlCommentOldStyleEnd;
                    break;
                case XmlNodeType.Attribute:
                    Debug.Assert(false);
                    break;
                case XmlNodeType.Document:
                    Debug.Assert(false);
                    break;
                case XmlNodeType.DocumentFragment:
                    Debug.Assert(false);
                    break;
                case XmlNodeType.DocumentType:
                    Debug.Assert(false);
                    break;
                case XmlNodeType.Element:
                    Debug.Assert(false);
                    break;
                case XmlNodeType.EndElement:
                    Debug.Assert(false);
                    break;
                case XmlNodeType.EndEntity:
                    Debug.Assert(false);
                    break;
                case XmlNodeType.Entity:
                    Debug.Assert(false);
                    break;
                case XmlNodeType.EntityReference:
                    Debug.Assert(false);
                    break;
                case XmlNodeType.None:
                    Debug.Assert(false);
                    break;
                case XmlNodeType.Notation:
                    Debug.Assert(false);
                    break;
                case XmlNodeType.ProcessingInstruction:
                    Debug.Assert(false);
                    break;
                case XmlNodeType.SignificantWhitespace:
                    Debug.Assert(false);
                    break;
                case XmlNodeType.Text:
                    this.openString = string.Empty;
                    this.closeString = string.Empty;
                    break;
                case XmlNodeType.Whitespace:
                    Debug.Assert(false);
                    break;
                case XmlNodeType.XmlDeclaration:
                    Debug.Assert(false);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }

        /// <summary>
        /// Generates output text when the difference is due to adding data
        /// </summary>
        /// <param name="writer">output stream</param>
        private void DrawTextAdd(TextWriter writer)
        {
            Debug.Assert(XmlDiffViewOperation.Add == Operation);
            // Remove tabs and newlines from values
            this.InnerText = this.InnerText;

            this.SetOpeningAndClosingTags(NodeType);
            if (this.openString != string.Empty)
            {
                writer.Write(this.openString);
                writer.Write(Difference.Tag + 
                    Difference.TextAddedBegin + 
                    this.InnerText + 
                    Difference.TextAddedEnd);
                writer.Write(this.closeString);
            }
            else
            {
                writer.Write(Difference.Tag + 
                    Difference.TextAddedBegin +
                    this.InnerText +
                    Difference.TextAddedEnd);
            }
            writer.Write(writer.NewLine);
        }

        /// <summary>
        /// Generates output text when the difference is due to deleting data
        /// </summary>
        /// <param name="writer">output stream</param>
        private void DrawTextDelete(TextWriter writer)
        {
            Debug.Assert(XmlDiffViewOperation.Remove == Operation);
            // Remove tabs and newlines from values
            this.InnerText = this.InnerText;

            this.SetOpeningAndClosingTags(NodeType);
            if (this.openString != string.Empty)
            {
                writer.Write(this.openString);
                writer.Write(Difference.Tag + 
                    Difference.TextDeletedBegin +
                    this.InnerText +
                    Difference.TextDeletedEnd);
                writer.Write(this.closeString);
            }
            else
            {
                writer.Write(Difference.Tag + 
                    Difference.TextDeletedBegin +
                    this.InnerText +
                    Difference.TextDeletedEnd);
            }
            writer.Write(writer.NewLine);
        }

        /// <summary>
        /// Generates output text when the difference
        ///  is due to changing existing data
        /// </summary>
        /// <param name="writer">output stream</param>
        private void DrawTextChange(TextWriter writer)
        {
            Debug.Assert(XmlDiffViewOperation.Change == Operation);
            // Remove tabs and newlines from values
            this.InnerText = this.InnerText;
            ChangeInformation.Subset = RemoveTabsAndNewlines(
                ChangeInformation.Subset);

            this.SetOpeningAndClosingTags(NodeType);
            if (this.openString != string.Empty)
            {
                writer.Write(this.openString);
                writer.Write(Difference.Tag + 
                    Difference.ChangeBegin + 
                    this.InnerText +
                    Difference.ChangeTo + 
                    ChangeInformation.Subset + 
                    Difference.ChangeEnd);
                writer.Write(this.closeString);
            }
            else
            {
                writer.Write(Difference.Tag +
                    Difference.ChangeBegin +
                    this.InnerText +
                    Difference.ChangeTo +
                    ChangeInformation.Subset +
                    Difference.ChangeEnd);
            }
            writer.Write(writer.NewLine);
        }

        /// <summary>
        /// Generates output text when the difference is 
        /// due to moving data from a location.
        /// </summary>
        /// <param name="writer">output stream</param>
        private void DrawTextMoveFrom(TextWriter writer)
        {
            Debug.Assert(XmlDiffViewOperation.MoveFrom == Operation);
            // Remove tabs and newlines from values
            this.InnerText = this.InnerText;

            this.SetOpeningAndClosingTags(NodeType);
            if (this.openString != string.Empty)
            {
                writer.Write(this.openString);
                writer.Write(Difference.Tag + 
                    Difference.TextMovedFromBegin + 
                    this.InnerText +
                    Difference.TextMovedFromEnd);
                writer.Write(this.closeString);
            }
            else
            {
                writer.Write(Difference.Tag +
                    Difference.TextMovedFromBegin +
                    this.InnerText +
                    Difference.TextMovedFromEnd);
            }
            writer.Write(writer.NewLine);
        }

        /// <summary>
        /// Generates output text when the difference is 
        /// due to moving data to a location.
        /// </summary>
        /// <param name="writer">output stream</param>
        private void DrawTextMoveTo(TextWriter writer)
        {
            Debug.Assert(XmlDiffViewOperation.MoveTo == Operation);
            // Remove tabs and newlines from values
            this.InnerText = this.InnerText;

            this.SetOpeningAndClosingTags(NodeType);
            if (this.openString != string.Empty)
            {
                writer.Write(this.openString);
                writer.Write(Difference.Tag +
                    Difference.TextMovedToBegin +
                    this.InnerText +
                    Difference.TextMovedToEnd);
                writer.Write(this.closeString);
            }
            else
            {
                writer.Write(Difference.Tag +
                    Difference.TextMovedToBegin +
                    this.InnerText +
                    Difference.TextMovedToEnd);
            }
            writer.Write(writer.NewLine);
        }
        #endregion

     }
}