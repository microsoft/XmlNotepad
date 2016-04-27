//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffViewElement.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Summary description for this file
// </summary>           
// <history>
//      [barryw] 03MAR05 Created
// </history>
//  ---------------------------------------------------------------------------

namespace Microsoft.XmlDiffPatch
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Diagnostics;

    /// <summary>
    /// Class to generate output for xml elements.
    /// </summary>
    internal class XmlDiffViewElement : XmlDiffViewParentNode
    {
        #region Member variables section

        // name
        private string localName;
        private string prefix;
        private string namespaceUri;
        private string name;

        // attributes
        private XmlDiffViewAttribute attributes;

        private bool ignorePrefixes;

        #endregion
        
        #region  Constructors section

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="localName">Element node name</param>
        /// <param name="prefix">xml node prefix</param>
        /// <param name="namespaceUri">Uniform Resource Identifier (URI)
        /// </param>
        /// <param name="ignorePrefix">Ignore differences in the prefix</param>
        public XmlDiffViewElement(
            string localName, 
            string prefix, 
            string namespaceUri, 
            bool ignorePrefix) : 
            base(XmlNodeType.Element)
        {
            this.LocalName = localName;
            this.Prefix = prefix;
            this.NamespaceUri = namespaceUri;

            if (this.Prefix != string.Empty)
            {
                this.Name = this.Prefix + ":" + this.LocalName;
            }
            else
            {
                    this.Name = this.LocalName;
            }
            this.ignorePrefixes = ignorePrefix;
        }

        #endregion

        #region Properties section

        /// <summary>
        /// Gets or sets an attributes object.
        /// </summary>
        public XmlDiffViewAttribute Attributes
        {
            get
            {
                return this.attributes;
            }
            set
            {
                this.attributes = value;
            }
        }

        /// <summary>
        /// Returns and exception("OuterXml is not 
        /// supported on XmlDiffViewElement.
        /// </summary>
        public override string OuterXml
        {
            get
            {
                string message = "OuterXml is not supported" +
                    " on XmlDiffViewElement.";
                throw new Exception(message);
            }
        }

        /// <summary>
        /// Gets or sets the name of the element without the prefix.
        /// </summary>
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
        /// Gets or sets the prefix.
        /// </summary>
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
        /// Gets or sets the namespace Uniform Resource Identifier (URI)
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
        /// Gets or set the name of the element with the prefix.
        /// </summary>
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

        #endregion
        
        #region Methods section
        
        /// <summary>
        /// Gets an attribute object for the specified attribute name.
        /// </summary>
        /// <param name="name">attribute name</param>
        /// <returns>an attribute object</returns>
        public XmlDiffViewAttribute GetAttribute(string name)
        {
            XmlDiffViewAttribute curAttr = this.Attributes;
            while (curAttr != null)
            {
                if (curAttr.Name == name && 
                    curAttr.Operation == XmlDiffViewOperation.Match)
                {
                    return curAttr;
                }
                curAttr = (XmlDiffViewAttribute)curAttr.NextSibbling;
            }
            return null;
        }

        /// <summary>
        /// Inserts an attribute object after the specified attribute object.
        /// </summary>
        /// <param name="newAttr">the attribute object to insert</param>
        /// <param name="refAttr">attribute object to insert after</param>
        public void InsertAttributeAfter(
            XmlDiffViewAttribute newAttr, 
            XmlDiffViewAttribute refAttr)
        {
            Debug.Assert(newAttr != null);
            if (refAttr == null)
            {
                newAttr.NextSibbling = this.Attributes;
                this.Attributes = newAttr;
            }
            else
            {
                newAttr.NextSibbling = refAttr.NextSibbling;
                refAttr.NextSibbling = newAttr;
            }
            newAttr.Parent = this;
        }

        /// <summary>
        /// Creates a complete copy of the current node.
        /// </summary>
        /// <param name="deep">has child nodes</param>
        /// <returns>A copy of the current node</returns>
        internal override XmlDiffViewNode Clone(bool deep)
        {
            XmlDiffViewElement newElement = new XmlDiffViewElement(
                this.LocalName, 
                this.Prefix, 
                this.NamespaceUri, 
                this.ignorePrefixes);

            // attributes
            {
                XmlDiffViewAttribute curAttr = this.Attributes;
                XmlDiffViewAttribute lastNewAtt = null;
                while (curAttr != null)
                {
                    XmlDiffViewAttribute newAttr = 
                        (XmlDiffViewAttribute)curAttr.Clone(true);
                    newElement.InsertAttributeAfter(newAttr, lastNewAtt);
                    lastNewAtt = newAttr;

                    curAttr = (XmlDiffViewAttribute)curAttr.NextSibbling;
                }
            }

            if (!deep)
            {
                return newElement;
            }
            // child nodes
            {
                XmlDiffViewNode curChild = ChildNodes;
                XmlDiffViewNode lastNewChild = null;
                while (curChild != null)
                {
                    XmlDiffViewNode newChild = curChild.Clone(true);
                    newElement.InsertChildAfter(newChild, lastNewChild, false);
                    lastNewChild = newChild;

                    curChild = curChild.NextSibbling;
                }
            }

            return newElement;
        }

        /// <summary>
        /// Generates  output data in html form
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        internal override void DrawHtml(XmlWriter writer, int indent)
        {
            XmlDiffViewOperation typeOfDifference = Operation;
            bool closeElement = false;
            XmlDiffView.HtmlStartRow(writer);
            this.DrawLinkNode(writer);
            
            for (int i = 0; i < 2; i++)
            {
                XmlDiffView.HtmlStartCell(writer, indent);
                if (XmlDiffView.HtmlWriteToPane[(int)Operation, i])
                {
                    closeElement = OutputNavigationHtml(writer);

                    if (Operation == XmlDiffViewOperation.Change)
                    {
                        typeOfDifference = XmlDiffViewOperation.Match;
                        XmlDiffView.HtmlWriteString(
                            writer, 
                            typeOfDifference, 
                            Tags.XmlOpenBegin);
                        if (i == 0)
                        {
                            this.DrawHtmlNameChange(
                                writer, 
                                this.LocalName, 
                                this.Prefix);
                        }
                        else
                        {
                            this.DrawHtmlNameChange(
                                writer, 
                                ChangeInformation.LocalName, 
                                ChangeInformation.Prefix);
                        }
                    }
                    else
                    {
                        this.DrawHtmlName(
                            writer, 
                            typeOfDifference, 
                            Tags.XmlOpenBegin, 
                            string.Empty);
                    }
    
                    if (closeElement)
                    {
                        // write the closing '</A>' tag.
                        writer.WriteEndElement();
                        closeElement = false;
                    }

                    // attributes
                    this.DrawHtmlAttributes(writer, i);

                    // close start tag
                    if (ChildNodes != null)
                    {
                        XmlDiffView.HtmlWriteString(
                            writer, 
                            typeOfDifference, 
                            Tags.XmlOpenEnd);
                    }
                    else
                    {
                        XmlDiffView.HtmlWriteString(
                            writer, 
                            typeOfDifference, 
                            Tags.XmlOpenEndTerse);
                    }
                }
                else
                {
                    XmlDiffView.HtmlWriteEmptyString(writer);
                }
                XmlDiffView.HtmlEndCell(writer);
            }
            XmlDiffView.HtmlEndRow(writer);

            // child nodes
            if (ChildNodes != null)
            {
                HtmlDrawChildNodes(writer, indent + XmlDiffView.DeltaIndent);

                // end element
                XmlDiffView.HtmlStartRow(writer);
                this.DrawLinkNode(writer);

                for (int i = 0; i < 2; i++)
                {
                    XmlDiffView.HtmlStartCell(writer, indent);
                    if (XmlDiffView.HtmlWriteToPane[(int)Operation, i])
                    {
                        if (Operation == XmlDiffViewOperation.Change)
                        {
                            Debug.Assert(typeOfDifference == 
                                XmlDiffViewOperation.Match);
                            XmlDiffView.HtmlWriteString(
                                writer, 
                                typeOfDifference, 
                                Tags.XmlCloseBegin);
                            if (i == 0)
                            {
                                this.DrawHtmlNameChange(
                                    writer, 
                                    this.LocalName, 
                                    this.Prefix);
                            }
                            else
                            {
                                this.DrawHtmlNameChange(
                                    writer, 
                                    ChangeInformation.LocalName, 
                                    ChangeInformation.Prefix);
                            }
                            XmlDiffView.HtmlWriteString(
                                writer, 
                                typeOfDifference, 
                                Tags.XmlOpenEnd);
                        }
                        else
                        {
                            this.DrawHtmlName(
                                writer, 
                                typeOfDifference, 
                                Tags.XmlCloseBegin,
                                Tags.XmlCloseEnd);
                        }
                    }
                    else
                    {
                        XmlDiffView.HtmlWriteEmptyString(writer);
                    }
                    XmlDiffView.HtmlEndCell(writer);
                }
                XmlDiffView.HtmlEndRow(writer);
            }
        }

        /// <summary>
        /// Generates  output data in text form
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        internal override void DrawText(
            TextWriter writer,
            int indent)
        {
            Debug.Assert(XmlNodeType.Element == NodeType);
            switch (Operation)
            {
                case XmlDiffViewOperation.Add:
                    this.DrawTextNameAdd(
                        writer,
                        indent,
                        Tags.XmlOpenBegin,
                        string.Empty);
                    break;
                case XmlDiffViewOperation.Change:
                    this.DrawTextNameChange(
                        writer,
                        indent,
                        Tags.XmlOpenBegin,
                        string.Empty);
                    break;
                case XmlDiffViewOperation.Ignore:
                case XmlDiffViewOperation.Match:
                    // No change or ignored
                    /* write out the element but leave the 
                     * tag open in case there are attributes.
                     */
                    this.DrawTextName(
                        writer,
                        indent,
                        Tags.XmlOpenBegin,
                        string.Empty);
                    break;
                case XmlDiffViewOperation.MoveFrom:
                    this.DrawTextNameMoveFrom(
                        writer,
                        indent,
                        Tags.XmlOpenBegin,
                        string.Empty);
                    break;
                case XmlDiffViewOperation.MoveTo:
                    this.DrawTextNameMoveTo(
                        writer,
                        indent,
                        Tags.XmlOpenBegin,
                        string.Empty);
                    break;
                case XmlDiffViewOperation.Remove:
                    this.DrawTextNameDelete(
                        writer,
                        indent,
                        Tags.XmlOpenBegin,
                        string.Empty);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

            // attributes
            this.DrawTextAttributes(writer, indent);

            // if there is children
            if (ChildNodes != null)
            {
                // close start tag
                writer.Write(Tags.XmlOpenEnd +
                    writer.NewLine);
                // process child nodes
                TextDrawChildNodes(writer, indent);
                // end element
                this.DrawTextName(
                    writer,
                    indent,
                    Tags.XmlCloseBegin,
                    Tags.XmlCloseEnd + writer.NewLine);
            }
            else
            {
                // avoid the need for closing node name tag
                writer.Write(Tags.XmlOpenEndTerse +
                    writer.NewLine);
            }
        }

        /// <summary>
        /// Generates attributes' output data in html form
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="paneNo">Pane number, indicating
        ///  the left (baseline) or right side (actual) of the 
        ///  display</param>
        private void DrawHtmlAttributes(
            XmlWriter writer, 
            int paneNo)
        {
            if (this.Attributes == null)
            {
                return;
            }
            string attrIndent = string.Empty;
            if (this.Attributes.NextSibbling != null)
            {
                attrIndent = XmlDiffView.GetIndent(this.Name.Length + 2);
            }
            XmlDiffViewAttribute curAttr = this.Attributes;
            while (curAttr != null)
            {
                if (XmlDiffView.HtmlWriteToPane[(int)curAttr.Operation, paneNo])
                {
                    if (curAttr == this.Attributes)
                    {
                        writer.WriteString(" ");
                    }
                    else
                    {
                        writer.WriteRaw(attrIndent);
                    }
                    if (curAttr.Operation == XmlDiffViewOperation.Change)
                    {
                        if (paneNo == 0)
                        {
                            this.DrawHtmlAttributeChange(
                                writer, 
                                curAttr, 
                                curAttr.LocalName, 
                                curAttr.Prefix, 
                                curAttr.AttributeValue);
                        }
                        else
                        {
                            this.DrawHtmlAttributeChange(
                                writer, 
                                curAttr, 
                                curAttr.ChangeInformation.LocalName, 
                                curAttr.ChangeInformation.Prefix,
                                curAttr.ChangeInformation.Subset);
                        }
                    }
                    else
                    {
                        this.DrawHtmlAttribute(
                            writer, 
                            curAttr, 
                            curAttr.Operation);
                    }
                }
                else
                {
                    XmlDiffView.HtmlWriteEmptyString(writer);
                }
                curAttr = (XmlDiffViewAttribute)curAttr.NextSibbling;
                if (curAttr != null)
                {
                    XmlDiffView.HtmlBr(writer);
                }
            }
        }

        /// <summary>
        /// Generates atrributes' output data in text form
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        private void DrawTextAttributes(
            TextWriter writer,
            int indent)
        {
            if (this.Attributes != null)
            {
                indent += Indent.IncrementSize;
                XmlDiffViewAttribute curAttr = this.Attributes;
                while (curAttr != null)
                {
                    if (curAttr == this.Attributes)
                    {
                        writer.Write(" ");
                    }
                    else
                    {  // put subsequent attributes on their own line
                        writer.Write(
                            writer.NewLine + XmlDiffView.IndentText(indent));
                    }

                    // The attributes could have their own 
                    // 'add'/'remove'/'move from'/ 'move to'/match/ignore 
                    // attribute operations so we check for that now 
                    switch (curAttr.Operation)
                    {
                        case XmlDiffViewOperation.Change:
                            this.DrawTextAttributeChange(writer, curAttr);
                            break;
                        case XmlDiffViewOperation.Add:
                        case XmlDiffViewOperation.Ignore:
                        case XmlDiffViewOperation.MoveFrom:
                        case XmlDiffViewOperation.MoveTo:
                        case XmlDiffViewOperation.Remove:
                        case XmlDiffViewOperation.Match:
                            // for 'add'/'remove'/'move from'/'move to'/match
                            // operations write out the baseline attributes 
                            // data.
                            this.DrawTextAttribute(writer, curAttr);
                            break;
                        default:
                            // raise exception for new type of 
                            // difference created in XmlDiff object.
                            throw new ArgumentException(
                                "Unrecognised type of difference", 
                                "Operation");
                    }
                    curAttr = (XmlDiffViewAttribute)curAttr.NextSibbling;
                }
            }
        }

        /// <summary>
        /// Generates output data in html form for a difference due to
        /// a change in element name.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="localName">name of the 
        /// element (without the prefix)</param>
        /// <param name="prefix">prefix</param>
        private void DrawHtmlNameChange(
            XmlWriter writer, 
            string localName, 
            string prefix)
        {
            if (prefix != string.Empty)
            {
                XmlDiffView.HtmlWriteString(
                    writer, 
                    this.ignorePrefixes ? XmlDiffViewOperation.Ignore : (prefix == ChangeInformation.Prefix) ? XmlDiffViewOperation.Match : XmlDiffViewOperation.Change,
                    prefix + ":");
            }

            XmlDiffView.HtmlWriteString(
                writer,
                (localName == ChangeInformation.LocalName) ? XmlDiffViewOperation.Match : XmlDiffViewOperation.Change,
                localName);
        }

        /// <summary>
        /// Generates output data in text form for a difference due
        /// to adding data.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        /// <param name="tagStart">xml tags at start of statement</param>
        /// <param name="tagEnd">xml tags at end of statement</param>
        private void DrawTextNameAdd(
            TextWriter writer,
            int indent,
            string tagStart,
            string tagEnd)
        {
            writer.Write(XmlDiffView.IndentText(indent) +
                tagStart);
            if (this.Prefix != string.Empty)
            {
                writer.Write(this.Prefix + ":");
            }
            writer.Write(Difference.Tag + 
                Difference.NodeAdded + this.LocalName);
            writer.Write(tagEnd);
        }

        /// <summary>
        /// Generates output data in text form for a difference due
        /// to deleting data.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        /// <param name="tagStart">xml tags at start of statement</param>
        /// <param name="tagEnd">xml tags at end of statement</param>
        private void DrawTextNameDelete(
            TextWriter writer,
            int indent,
            string tagStart,
            string tagEnd)
        {
            writer.Write(XmlDiffView.IndentText(indent) +
                tagStart);
            if (this.Prefix != string.Empty)
            {
                writer.Write(this.Prefix + ":");
            }
            writer.Write(this.LocalName + " " + Difference.Tag +
                Difference.NodeDeleted);
            writer.Write(tagEnd);
        }

        /// <summary>
        /// Generates output data in text form for a difference due
        /// to changing existing data.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        /// <param name="tagStart">xml tags at start of statement</param>
        /// <param name="tagEnd">xml tags at end of statement</param>
        private void DrawTextNameChange(
            TextWriter writer,
            int indent,
            string tagStart,
            string tagEnd)
        {
            writer.Write(XmlDiffView.IndentText(indent) +
                tagStart);
            if (this.Prefix != string.Empty)
            {
                writer.Write(this.Prefix + ":");
            }
            writer.Write(Difference.Tag + 
                Difference.ChangeBegin + 
                this.LocalName +
                Difference.ChangeTo + 
                ChangeInformation.LocalName + 
                Difference.ChangeEnd);
            writer.Write(tagEnd);
        }

        /// <summary>
        /// Generates output data in text form for a difference due
        /// to moving data from a location.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        /// <param name="tagStart">xml tags at start of statement</param>
        /// <param name="tagEnd">xml tags at end of statement</param>
        private void DrawTextNameMoveFrom(
            TextWriter writer,
            int indent,
            string tagStart,
            string tagEnd)
        {
            writer.Write(XmlDiffView.IndentText(indent) +
                tagStart);
            if (this.Prefix != string.Empty)
            {
                writer.Write(this.Prefix + ":");
            }
            writer.Write(this.LocalName + " " +
                Difference.Tag +
                Difference.NodeMovedFromBegin + 
                OperationId + 
                Difference.NodeMovedFromEnd);
            writer.Write(tagEnd);
        }

        /// <summary>
        /// Generates output data in text form for a difference due
        /// to moving data to a new location.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        /// <param name="tagStart">xml tags at start of statement</param>
        /// <param name="tagEnd">xml tags at end of statement</param>
        private void DrawTextNameMoveTo(
            TextWriter writer,
            int indent,
            string tagStart,
            string tagEnd)
        {
            writer.Write(XmlDiffView.IndentText(indent) +
                tagStart);
            if (this.Prefix == string.Empty)
            {
                writer.Write(this.Prefix + ":");
            }
            writer.Write(this.LocalName + " " +
                Difference.Tag +
                Difference.NodeMovedToBegin +
                OperationId +
                Difference.NodeMovedToEnd);
            writer.Write(tagEnd);
        }

        /// <summary>
        /// Generates output data in text form for a difference due
        /// to adding data.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="typeOfDifference">type of difference</param>
        /// <param name="tagStart">xml tags at start of statement</param>
        /// <param name="tagEnd">xml tags at end of statement</param>
        private void DrawHtmlName(
            XmlWriter writer, 
            XmlDiffViewOperation typeOfDifference, 
            string tagStart, 
            string tagEnd)
        {
            if (this.Prefix != string.Empty && this.ignorePrefixes)
            {
                XmlDiffView.HtmlWriteString(
                    writer, 
                    typeOfDifference, 
                    tagStart);
                XmlDiffView.HtmlWriteString(
                    writer, 
                    XmlDiffViewOperation.Ignore, 
                    this.Prefix + ":");
                XmlDiffView.HtmlWriteString(
                    writer, 
                    typeOfDifference, 
                    this.LocalName + tagEnd);
            }
            else
            {
                XmlDiffView.HtmlWriteString(
                    writer, 
                    typeOfDifference, 
                    tagStart + this.Name + tagEnd);
            }
        }

        /// <summary>
        /// Generates output data in text form for the name of the element.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        /// <param name="tagStart">xml tags at start of statement</param>
        /// <param name="tagEnd">xml tags at end of statement</param>
        private void DrawTextName(
            TextWriter writer,
            int indent,
            string tagStart,
            string tagEnd)
        {
            if (this.Prefix != string.Empty && this.ignorePrefixes)
            {
                writer.Write(
                    XmlDiffView.IndentText(indent) +
                    tagStart +
                    this.Prefix + ":" +
                    this.LocalName + tagEnd);
            }
            else
            {
                writer.Write(XmlDiffView.IndentText(indent) +
                    tagStart + this.Name + tagEnd);
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
        private void DrawHtmlAttributeChange(
            XmlWriter writer, 
            XmlDiffViewAttribute attr, 
            string localName, 
            string prefix, 
            string attributeValue)
        {
            if (prefix != string.Empty)
            {
                XmlDiffView.HtmlWriteString(
                    writer,
                    this.ignorePrefixes ? XmlDiffViewOperation.Ignore : (attr.Prefix == attr.ChangeInformation.Prefix) ? XmlDiffViewOperation.Match : XmlDiffViewOperation.Change,
                    prefix + ":");
            }

            XmlDiffView.HtmlWriteString(
                writer,
                (attr.LocalName == attr.ChangeInformation.LocalName) ? XmlDiffViewOperation.Match : XmlDiffViewOperation.Change,
                this.localName);

            if (attr.AttributeValue != attr.ChangeInformation.Subset)
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
        }

        /// <summary>
        /// Generate text output data for a differences 
        /// due to a change, which may or may not have been 
        /// a change in the attribute.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="attr">Attribute object</param>
        private void DrawTextAttributeChange(
            TextWriter writer,
            XmlDiffViewAttribute attr)
        {
            Debug.Assert(null != attr);

            if (this.Prefix != string.Empty)
            {
                //if the prefix changed then show the change
                XmlDiffViewOperation op = this.ignorePrefixes ? XmlDiffViewOperation.Ignore :
                    (attr.Prefix == attr.ChangeInformation.Prefix) ? XmlDiffViewOperation.Match : XmlDiffViewOperation.Change;

                switch (op)
                {
                    case XmlDiffViewOperation.Ignore:
                    case XmlDiffViewOperation.Match:
                        writer.Write(attr.Prefix + ":");
                        break;
                    case XmlDiffViewOperation.Change:
                        // show the new prefix
                        writer.Write(
                            Difference.Tag + "=" + Difference.ChangeBegin +
                            attr.Prefix + Difference.ChangeTo +
                            attr.ChangeInformation.Prefix + 
                            Difference.ChangeEnd);
                        writer.Write(attr.ChangeInformation.Prefix + ":");
                        break;
                    default:
                        Trace.WriteLine("Unexpected type of difference");
                        throw new ArgumentException(
                            "Unexpected type of difference", 
                            "Operation");
                }
            }

            if (System.Diagnostics.Debugger.IsAttached)
            {
                string debugMessage = "It is not appropriate to call this function" +
                "when the ChangeInformation object is null.";

                Debug.Assert(
                    null != attr.ChangeInformation,
                    debugMessage);
            }
            // something changed
            if (attr.LocalName != attr.ChangeInformation.LocalName)
            {
                // show the change in the name
                writer.Write(" " + attr.LocalName + "=\"" +
                    Difference.Tag + "RenamedNode" +
                    Difference.ChangeTo +
                    attr.ChangeInformation.LocalName +
                    Difference.ChangeEnd + "=");
            }
            else
            {
                writer.Write(" " + attr.LocalName + "=\"");
            }
            // determine if the attribute value has changed
            if (attr.AttributeValue != attr.ChangeInformation.Subset)
            {
                // attribute value changed
                //Note: "xd_ChangeFrom('original value')To('new value')"
                string attributeValueChange =
                    Difference.Tag + Difference.ChangeBegin +
                    attr.AttributeValue +
                    Difference.ChangeTo +
                    RemoveTabsAndNewlines(attr.ChangeInformation.Subset) +
                    Difference.ChangeEnd;
                writer.Write(attributeValueChange + "\"");
            }
            else
            {
                // attribute value is same
                writer.Write(
                    RemoveTabsAndNewlines(attr.AttributeValue) + "\"");
            }
        }

        /// <summary>
        /// Generate html output data for a differences 
        /// due to a change in an attribute.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="attr">Attribute object</param>
        /// <param name="typeOfDifference">type of difference</param>
        private void DrawHtmlAttribute(
            XmlWriter writer, 
            XmlDiffViewAttribute attr, 
            XmlDiffViewOperation typeOfDifference)
        {
            if (this.ignorePrefixes)
            {
                if (attr.Prefix == "xmlns" || (attr.LocalName == "xmlns" && 
                    attr.Prefix == string.Empty))
                {
                    XmlDiffView.HtmlWriteString(
                        writer, 
                        XmlDiffViewOperation.Ignore, 
                        attr.Name);
                    XmlDiffView.HtmlWriteString(
                        writer, 
                        typeOfDifference, 
                        "=\"" + attr.AttributeValue + "\"");
                    return;
                }
                else if (attr.Prefix != string.Empty)
                {
                    XmlDiffView.HtmlWriteString(
                        writer, 
                        XmlDiffViewOperation.Ignore, 
                        attr.Prefix + ":");
                    XmlDiffView.HtmlWriteString(
                        writer, 
                        typeOfDifference, 
                        attr.LocalName + "=\"" + attr.AttributeValue + "\"");
                    return;
                }
            }

            XmlDiffView.HtmlWriteString(
                writer, 
                typeOfDifference, 
                attr.Name + "=\"" + attr.AttributeValue + "\"");
        }
        
        /// <summary>
        /// Generate text output data for an unchanged attribute.
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="attr">attribute object</param>
        private void DrawTextAttribute(
            TextWriter writer,
            XmlDiffViewAttribute attr)
        {
            if (this.ignorePrefixes)
            {
                if (attr.Prefix == "xmlns" || (attr.LocalName == "xmlns" && 
                    attr.Prefix == string.Empty))
                {
                    writer.Write(attr.Name);
                    writer.Write("='" + 
                        RemoveTabsAndNewlines(attr.AttributeValue) + "'");
                    return;
                }
                else if (attr.Prefix != string.Empty)
                {
                    writer.Write(attr.Prefix + ":");
                    writer.Write(attr.LocalName + "=\"" + 
                        RemoveTabsAndNewlines(attr.AttributeValue) + "\"");
                    return;
                }
            }
            writer.Write(attr.Name + "=\"" + 
                RemoveTabsAndNewlines(attr.AttributeValue) + "\"");
        }
        
        #endregion
        
   }

}