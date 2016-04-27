//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffViewParentNode.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Provides access to the parent node object and its child node objects.
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
    /// Class to access the parent node object and its child node objects.
    /// </summary>
    internal abstract class XmlDiffViewParentNode : XmlDiffViewNode
    {
        #region Member variables section

        // child nodes
        private XmlDiffViewNode childNodes;
        // number of source child nodes
        private int sourceChildNodesCount;
        // source nodes indexed by their relative position
        private XmlDiffViewNode[] sourceChildNodesIndex;
        
        #endregion
        
        #region  Constructors section

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nodeType">Type of xml node</param>
        internal XmlDiffViewParentNode(
            XmlNodeType nodeType) : base(nodeType) 
        {
        }
        
        #endregion

        #region Properties section

        /// <summary>
        /// Gets or sets a reference to the childnodes
        /// </summary>
        public XmlDiffViewNode ChildNodes
        {
            get
            {
                return this.childNodes;
            }
            set
            {
                this.childNodes = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of child nodes
        /// </summary>
        public int SourceChildNodesCount
        {
            get
            {
                return this.sourceChildNodesCount;
            }
            set
            {
                this.sourceChildNodesCount = value;
            }
        }

        /// <summary>
        /// Gets the first child node
        /// </summary>
        internal override XmlDiffViewNode FirstChildNode
        { 
            get 
            { 
                return this.ChildNodes; 
            } 
        }

        /// <summary>
        /// Gets or sets the collection of baseline child nodes
        /// </summary>
        private XmlDiffViewNode[] SourceChildNodesIndex
        {
            get
            {
                return this.sourceChildNodesIndex;
            }
            set
            {
                this.sourceChildNodesIndex = value;
            }
        }

        #endregion
        
        #region Methods section

        /// <summary>
        /// Gets a particular child node based on its index.
        /// </summary>
        /// <param name="index">index of the child node</param>
        /// <returns>child node</returns>
        /// <exception cref="ArgumentException">Thrown when the
        /// index value is out of bounds (Has the CreateSourceNodesIndex
        ///  method been called?)</exception>
        internal XmlDiffViewNode GetSourceChildNode(int index) 
        { 
            if (index < 0 || 
                index >= this.SourceChildNodesCount || 
                this.SourceChildNodesCount == 0)
            {
                throw new ArgumentException("index");
            }
            if (this.SourceChildNodesCount == 0)
            {
                    return null;
            }
            if (this.SourceChildNodesIndex == null)
            {
                    this.CreateSourceNodesIndex();
            }
            return this.SourceChildNodesIndex[index];
        }

        /// <summary>
        /// Creates an indexed collection of child nodes.
        /// </summary>
        internal void CreateSourceNodesIndex()
        {
            if (this.SourceChildNodesIndex != null || 
                this.SourceChildNodesCount == 0)
            {
                    return;
            }
            this.SourceChildNodesIndex = new 
                XmlDiffViewNode[this.SourceChildNodesCount];
        
            XmlDiffViewNode curChild = this.ChildNodes;
            for (int i = 0; i < this.SourceChildNodesCount; i++, curChild = curChild.NextSibbling) 
            {
                Debug.Assert(curChild != null);
                this.SourceChildNodesIndex[i] = curChild;
            }
            Debug.Assert(curChild == null);
        }

        /// <summary>
        /// Inserts a node after the specified node
        /// </summary>
        /// <param name="newChild">node to insert</param>
        /// <param name="referenceChild">node to insert after</param>
        /// <param name="sourceNode">This is a baseline node</param>
        internal void InsertChildAfter(
            XmlDiffViewNode newChild, 
            XmlDiffViewNode referenceChild, 
            bool sourceNode) 
        {
            Debug.Assert(newChild != null);
            if (referenceChild == null) 
            {
                newChild.NextSibbling = this.ChildNodes;
                this.ChildNodes = newChild;
            }
            else 
            {
                newChild.NextSibbling = referenceChild.NextSibbling;
                referenceChild.NextSibbling = newChild;
            }
            if (sourceNode)
            {
                this.SourceChildNodesCount++;
            }
            newChild.Parent = this;
        }

        /// <summary>
        /// Generates  output data in html form
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        internal void HtmlDrawChildNodes(XmlWriter writer, int indent) 
        {
            XmlDiffViewNode curChild = this.ChildNodes;
            while (curChild != null) 
            {
                curChild.DrawHtml(writer, indent);
                curChild = curChild.NextSibbling;
            }
        }

        /// <summary>
        /// Generates output data in text form
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">number of indentations</param>
        internal void TextDrawChildNodes(
            TextWriter writer, 
            int indent)
        {
            indent += Indent.IncrementSize;
            XmlDiffViewNode curChild = this.ChildNodes;
            while (curChild != null) 
            {
                curChild.DrawText(writer, indent);
                curChild = curChild.NextSibbling;
            }
        }

        #endregion
        
    }
}
