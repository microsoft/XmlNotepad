//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffViewDocument.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Generate output data for the document
//     level nodes and their children.
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
    /// Class the generate output data for the document
    /// level nodes and their children. 
    /// </summary>
    internal class XmlDiffViewDocument : XmlDiffViewParentNode
    {
        #region  Constructors section

        /// <summary>
        /// Constructor.
        /// </summary>
        internal XmlDiffViewDocument() : base(XmlNodeType.Document) 
        {
        }

        #endregion

        #region Properties section

        /// <summary>
        /// Returns an Exception "OuterXml is not supported on 
        /// XmlDiffViewElement."
        /// </summary>
        [Obsolete("OuterXml is not supported on XmlDiffViewElement",true)]
        public override string OuterXml 
        { 
            get 
            { 
                throw new Exception("OuterXml is not supported on XmlDiffViewElement.");
            }
        }

        #endregion
        
        #region Methods section

        /// <summary>
        /// Creates a complete copy of the current node.
        /// </summary>
        /// <param name="deep">deprecated</param>
        /// <returns>Exception: Clone method should never be called on a document node.</returns>
        [Obsolete("Clone method should never be called on a document node", true)]
        internal override XmlDiffViewNode Clone(bool deep)
        {
            throw new Exception("Clone method should never be called on a document node.");
        }

        /// <summary>
        /// Generates  output data in html form
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        internal override void DrawHtml(XmlWriter writer, int indent) 
        {
            HtmlDrawChildNodes(writer, indent);
        }
    
        /// <summary>
        /// Generates  output data in text form
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        internal override void DrawText(TextWriter writer, int indent) 
        {
            TextDrawChildNodes(writer, indent);
        }
    
        #endregion
        
    }
}