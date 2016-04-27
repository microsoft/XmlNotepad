//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffViewER.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Provides an interface the EntityReference nodes.
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
    /// Class to generate outout data for the EntityReference type nodes 
    /// </summary>
    [Obsolete("This appears to be dead code",true)]
    internal class XmlDiffViewER : XmlDiffViewNode
    {
        #region Member variables section
        
        private string nameStore;

        #endregion

        #region  Constructors section

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">name of the node</param>
        internal XmlDiffViewER(string name) : base(XmlNodeType.EntityReference)
        {
            this.nameStore = name;
        }

        #endregion

        #region Properties section

        /// <summary>
        /// Gets the name of the ER node with special characters
        /// </summary>
        /// <value>{this value is not used}</value>
        public override string OuterXml
        {
            get
            {
                return "&" + this.nameStore + ";";
            }
        }

        #endregion

        #region Methods section

        /// <summary>
        /// Creates a complete copy of the current node.
        /// </summary>
        /// <param name="deep">{deprecated}</param>
        /// <returns>a node object</returns>
        internal override XmlDiffViewNode Clone(bool deep)
        {
            return new XmlDiffViewER(this.nameStore);
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
                Debug.Assert(this.nameStore != ChangeInformation.LocalName);

                XmlDiffView.HtmlStartRow(writer);
                this.DrawLinkNode(writer);
                XmlDiffView.HtmlStartCell(writer, indent);

                XmlDiffView.HtmlWriteString(writer, "&");
                XmlDiffView.HtmlWriteString(
                    writer, 
                    XmlDiffViewOperation.Change, 
                    this.nameStore);
                XmlDiffView.HtmlWriteString(writer, ";");

                XmlDiffView.HtmlEndCell(writer);
                XmlDiffView.HtmlStartCell(writer, indent);

                XmlDiffView.HtmlWriteString(writer, "&");
                XmlDiffView.HtmlWriteString(
                    writer, 
                    XmlDiffViewOperation.Change, 
                    ChangeInformation.LocalName);
                XmlDiffView.HtmlWriteString(writer, ";");

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
            if (Operation == XmlDiffViewOperation.Change)
            {
                Debug.Assert(this.nameStore != ChangeInformation.LocalName);

                // Tests have not seen this method called.
                throw new NotImplementedException();
            }
            else
            {
                DrawTextNoChange(writer, indent);
            }
        }

        #endregion

    }
}