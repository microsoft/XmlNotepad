//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffViewPI.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Generate output data for programming instruction nodes.
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
    /// Class to generate output data for programming instruction nodes.
    /// </summary>
    internal class XmlDiffViewPI : XmlDiffViewCharData
    {
        #region Member variables section

        /// <summary>
        /// Name of the programing instruction.
        /// </summary>
        private string name;

        #endregion

        #region  Constructors section

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">name of programming instruction</param>
        /// <param name="value">value of programming instruction</param>
        internal XmlDiffViewPI(
            string name, 
            string value) : 
            base(value, XmlNodeType.ProcessingInstruction)
        {
            this.Name = name;
        }

        #endregion

        #region Properties section

        /// <summary>
        /// Name of the programming instruction.
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

        /// <summary>
        /// Gets or sets the programming instruction statement
        /// </summary>
        public override string OuterXml
        {
            get
            {
                return Tags.XmlErrorHandlingBegin +
                    this.Name + " " +
                    RemoveTabsAndNewlines(InnerText) +
                    Tags.XmlErrorHandlingEnd;
            }
        }

        #endregion

        #region Methods section

        /// <summary>
        /// Creates a complete copy of the current node.
        /// </summary>
        /// <param name="deep">deprecated</param>
        /// <returns>a programming instruction node</returns>
        internal override XmlDiffViewNode Clone(bool deep)
        {
            return new XmlDiffViewPI(this.Name, this.InnerText);
        }

        /// <summary>
        /// Generates output data in html form.
        /// </summary>
        /// <param name="writer">output data stream</param>
        /// <param name="indent">size of indentation</param>
        internal override void DrawHtml(XmlWriter writer, int indent)
        {
            if (Operation == XmlDiffViewOperation.Change)
            {
                XmlDiffViewOperation nameOp = (this.Name == ChangeInformation.LocalName) ? XmlDiffViewOperation.Match : XmlDiffViewOperation.Change;
                XmlDiffViewOperation valueOp = (this.InnerText == ChangeInformation.Subset) ? XmlDiffViewOperation.Match : XmlDiffViewOperation.Change;

                XmlDiffView.HtmlStartRow(writer);
                this.DrawLinkNode(writer);

                XmlDiffView.HtmlStartCell(writer, indent);

                XmlDiffView.HtmlWriteString(writer, Tags.XmlErrorHandlingBegin);
                XmlDiffView.HtmlWriteString(writer, nameOp, this.Name);
                XmlDiffView.HtmlWriteString(writer, " ");
                XmlDiffView.HtmlWriteString(
                    writer,
                    valueOp,
                    RemoveTabsAndNewlines(InnerText));
                XmlDiffView.HtmlWriteString(writer, Tags.XmlErrorHandlingEnd);

                XmlDiffView.HtmlEndCell(writer);
                XmlDiffView.HtmlStartCell(writer, indent);

                XmlDiffView.HtmlWriteString(writer, Tags.XmlErrorHandlingBegin);
                XmlDiffView.HtmlWriteString(
                    writer,
                    nameOp,
                    ChangeInformation.LocalName);
                XmlDiffView.HtmlWriteString(writer, " ");
                XmlDiffView.HtmlWriteString(
                    writer,
                    valueOp,
                    RemoveTabsAndNewlines(ChangeInformation.Subset));
                XmlDiffView.HtmlWriteString(writer, Tags.XmlErrorHandlingEnd);

                XmlDiffView.HtmlEndCell(writer);
                XmlDiffView.HtmlEndRow(writer);
            }
            else
            {
                DrawHtmlNoChange(writer, indent);
            }
        }

        /// <summary>
        /// Generates output data in text form.
        /// </summary>
        /// <param name="writer">output data stream</param>
        /// <param name="indent">size of indentation</param>
        internal override void DrawText(TextWriter writer, int indent)
        {
            Debug.Assert(XmlNodeType.ProcessingInstruction == NodeType);
            writer.Write(XmlDiffView.IndentText(indent));
            switch (Operation)
            {
                case XmlDiffViewOperation.Add:
                    // component name is the new
                    writer.Write(Tags.XmlErrorHandlingBegin +
                        this.Name +
                        " ");

                    // add difference attribute
                    writer.Write(Difference.Tag + Difference.PIAdded);

                    // process other attributes
                    this.DrawAttributes(writer, indent);

                    // close tag
                    writer.Write(Tags.XmlErrorHandlingEnd);
                    break;
                case XmlDiffViewOperation.Change:
                {
                    // Determine nature of changes
                    if (this.Name == ChangeInformation.LocalName)
                    {
                        // component name is the same
                        writer.Write(Tags.XmlErrorHandlingBegin +
                            this.Name +
                            " ");

                        // process attributes
                        this.DrawAttributes(writer, indent);

                        // close tag
                        writer.Write(Tags.XmlErrorHandlingEnd);
                    }
                    else
                    {
                        // component name changed
                        //Note: <?{new name of component} xd_="Rename(component)From('original name')" [, {attributes, values} ?>
                        writer.Write(Tags.XmlErrorHandlingBegin +
                            ChangeInformation.LocalName +
                            " ");

                        writer.Write(Difference.Tag + "=" +
                            Difference.PIRenamedBegin +
                            this.Name + Difference.PIRenamedEnd);

                        // process attributes
                        this.DrawAttributes(writer, indent);

                        // close tag
                        writer.Write(Tags.XmlErrorHandlingEnd);
                    }
                    writer.Write(writer.NewLine);
                }
                    break;
                case XmlDiffViewOperation.Ignore:
                    Debug.Assert(false);
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
                    writer.Write(Tags.XmlErrorHandlingBegin +
                        this.Name +
                        " ");

                    // add difference attribute
                    writer.Write(Difference.Tag + Difference.PIDeleted);

                    // process other attributes
                    this.DrawAttributes(writer, indent);

                    // close tag
                    writer.Write(Tags.XmlErrorHandlingEnd);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
            writer.Write(writer.NewLine);
        }

        /// <summary>
        /// Generates output data for attributes in text form.
        /// </summary>
        /// <param name="writer">output data stream</param>
        /// <param name="indent">size of indentation</param>
        private void DrawAttributes(
            TextWriter writer,
            int indent)
        {
            if (Operation == XmlDiffViewOperation.Change)
            {
                // determine if there was a sudo-attributes change
                if (InnerText != ChangeInformation.Subset)
                {
                    writer.Write(Tags.XmlCharacterDataBegin +
                        Difference.Tag + Difference.ChangeBegin +
                        RemoveTabsAndNewlines(ChangeInformation.Subset) +
                        Difference.ChangeTo +
                        RemoveTabsAndNewlines(InnerText) +
                        Difference.ChangeEnd +
                        Tags.XmlCharacterDataEnd);
                    return;
                }
            }
            // no change in sudo-attributes
            writer.Write(RemoveTabsAndNewlines(InnerText));
        }

        #endregion

    }
}