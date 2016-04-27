//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffViewXmlDeclaration.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Generate differences data for xml declaration nodes
// </summary>
// <history>
//      [barryw] 03MAR05 Created
// </history>
//  ---------------------------------------------------------------------------

namespace Microsoft.XmlDiffPatch
{
    using System;
    using System.Xml;
    using System.IO;
    using System.Diagnostics;

    /// <summary>
    /// Class to generate differences data for xml declaration nodes. 
    /// </summary>
    internal class XmlDiffViewXmlDeclaration : XmlDiffViewNode
    {
        #region Member variables section

        private string declarationValue;

        #endregion

        #region  Constructors section

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="value">the declaration's value</param>
        internal XmlDiffViewXmlDeclaration(string value) : base(XmlNodeType.XmlDeclaration)
        {
            this.declarationValue = value;
        }

        #endregion

        #region Properties section

        /// <summary>
        /// Gets the declaration value enclosed within xml declaration tags.
        /// </summary>
        public override string OuterXml
        {
            get
            {
                return Tags.XmlDeclarationBegin + this.declarationValue + Tags.XmlDeclarationEnd;
            }
        }

        #endregion

        #region Methods section

        /// <summary>
        /// Creates a complete copy of the current node.
        /// </summary>
        /// <param name="deep">deprecated</param>
        /// <returns>a declaration node</returns>
        internal override XmlDiffViewNode Clone(bool deep)
        {
            return new XmlDiffViewXmlDeclaration(this.declarationValue);
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
                Debug.Assert(this.declarationValue != ChangeInformation.Subset);

                XmlDiffView.HtmlStartRow(writer);
                this.DrawLinkNode(writer);
                XmlDiffView.HtmlStartCell(writer, indent);
                XmlDiffView.HtmlWriteString(writer, Tags.XmlDeclarationBegin);
                XmlDiffView.HtmlWriteString(writer, XmlDiffViewOperation.Change, this.declarationValue);
                XmlDiffView.HtmlWriteString(writer, Tags.XmlDeclarationEnd);

                XmlDiffView.HtmlEndCell(writer);
                XmlDiffView.HtmlStartCell(writer, indent);

                XmlDiffView.HtmlWriteString(writer, Tags.XmlDeclarationBegin);
                XmlDiffView.HtmlWriteString(writer, XmlDiffViewOperation.Change, ChangeInformation.Subset);
                XmlDiffView.HtmlWriteString(writer, Tags.XmlDeclarationEnd);

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
        internal override void DrawText(
            TextWriter writer,
            int indent)
        {
            Debug.Assert(XmlNodeType.XmlDeclaration == NodeType);
            writer.Write(XmlDiffView.IndentText(indent));
            switch (Operation)
            {
                case XmlDiffViewOperation.Add:
                    //Note: Can only have one valid declaration
                    //      in an Xml document and it must be on
                    //      the first line if it is present
                    // component name is the new
                    writer.Write(Tags.XmlDeclarationBegin);

                    // add difference attribute
                    writer.Write(Difference.Tag + Difference.DeclarationAdded);

                    // process sudo-attributes
                    //DrawAttributes(writer, indent);
                    writer.Write(this.declarationValue);

                    // close tag
                    writer.Write(Tags.XmlDeclarationEnd);
                    break;
                case XmlDiffViewOperation.Change:
                    Debug.Assert(this.declarationValue != ChangeInformation.Subset);
                    // component name is the same
                    writer.Write(Tags.XmlDeclarationBegin);

                    // process attributes
                    //DrawAttributes(writer, indent);
                    //Note: the following breaks xml validation but
                    //      could not design a better alternative.
                    writer.Write(Difference.Tag +
                        Difference.ChangeBegin + this.declarationValue +
                        Difference.ChangeTo + ChangeInformation.Subset +
                        Difference.ChangeEnd);

                    // close tag
                    writer.Write(Tags.XmlDeclarationEnd);
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
                    // component removed
                    writer.Write(Tags.XmlDeclarationBegin);

                    // add difference attribute
                    writer.Write(Difference.Tag + Difference.DeclarationDeleted);

                    // process sudo-attributes
                    //DrawAttributes(writer, indent);
                    writer.Write(this.declarationValue);

                    // close tag
                    writer.Write(Tags.XmlDeclarationEnd);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
            writer.Write(writer.NewLine);
        }

        #endregion

    }
}