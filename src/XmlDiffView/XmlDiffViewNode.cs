//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffViewNode.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Generate output data for the nodes
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
    /// Class to generate output data for the nodes
    /// </summary>
    internal abstract class XmlDiffViewNode 
    {
    
        #region Member variables section

        /// <summary>
        /// a place to store change information 
        /// (only if Operation == XmlDiffViewOperation.Change)
        /// </summary>
        protected ChangeInfo changeInfo = null;

        /// <summary>
        /// stores the node type value
        /// </summary>
        private XmlNodeType nodeTypeStore;

        /// <summary>
        /// Pointer to the next sibiling
        /// </summary>
        private XmlDiffViewNode nextSibilingStore = null;

        /// <summary>
        /// Pointer to the parent node
        /// </summary>
        private XmlDiffViewNode parentStore = null;

        /// <summary>
        /// The type of difference
        /// </summary>
        private XmlDiffViewOperation operation = XmlDiffViewOperation.Match;
        
        /// <summary>
        /// Identification number for the difference
        /// </summary>
        private int operationId = 0; // operation id
    
        #endregion
        
        #region  Constructors section

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodeType">type of node</param>
        internal XmlDiffViewNode(XmlNodeType nodeType) 
        {
            this.NodeType = nodeType;
        }

        #endregion

        #region Properties section

        /// <summary>
        /// Abstract property to get the outer xml data
        /// </summary>
        public abstract string OuterXml
        { 
            get; 
        }
        
        /// <summary>
        /// Gets or sets an object contained changed data.
        /// </summary>
        public ChangeInfo ChangeInformation
        {
            get
            {
                return this.changeInfo;
            }
            set
            {
                this.changeInfo = value;
            }
        }

        /// <summary>
        /// Gets or sets the next sibling
        /// </summary>
        public XmlDiffViewNode NextSibbling
        {
            get
            {
                return this.nextSibilingStore;
            }
            set
            {
                this.nextSibilingStore = value;
            }
        }

        /// <summary>
        /// Gets or sets the parent node
        /// </summary>
        public XmlDiffViewNode Parent
        {
            get
            {
                return this.parentStore;
            }
            set
            {
                this.parentStore = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the type of node
        /// </summary>
        public XmlNodeType NodeType
        {
            get
            {
                return this.nodeTypeStore;
            }
            set
            {
                this.nodeTypeStore = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the type of difference.
        /// </summary>
        public XmlDiffViewOperation Operation
        {
            get
            {
                return this.operation;
            }
            set
            {
                this.operation = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the difference identifier number
        /// </summary>
        public int OperationId
        {
            get
            {
                return this.operationId;
            }
            set
            {
                this.operationId  = value;
            }
        }
        
        /// <summary>
        /// Gets a null value
        /// </summary>
        internal virtual XmlDiffViewNode FirstChildNode 
        {
            get 
            {
                return null; 
            } 
        }

        #endregion
        
        #region Methods section

        /// <summary>
        /// Generate output text for a difference due to a change 
        /// to the baseline data.
        /// </summary>
        /// <param name="writer">output data stream</param>
        /// <param name="indent">size of the indent</param>
        public void DrawTextNoChange(
            TextWriter writer, 
            int indent) 
        {
            Debug.Assert(this.NodeType != XmlNodeType.Element && this.NodeType != XmlNodeType.Attribute);
            Debug.Assert(this.Operation != XmlDiffViewOperation.Change);
            //bool closeElement = OutputNavigationHtml(writer);
            string xmlText = this.OuterXml;
            writer.Write(XmlDiffView.IndentText(indent) + 
                xmlText);
        }
    
        /// <summary>
        /// Abstract method to get a copy of a node
        /// </summary>
        /// <param name="deep">has children</param>
        /// <returns>a node object</returns>
        internal abstract XmlDiffViewNode Clone(bool deep);
        
        /// <summary>
        /// Abstract method to generate html output data
        /// </summary>
        /// <param name="writer">data stream</param>
        /// <param name="indent">size of indentation</param>
        internal abstract void DrawHtml(XmlWriter writer, int indent);

        /// <summary>
        /// Abstract method to generate text output data
        /// </summary>
        /// <param name="writer">data stream</param>
        /// <param name="indent">size of indentation</param>
        internal abstract void DrawText(TextWriter writer, int indent);

        /// <summary>
        /// Generates output data in html form when the node has not changed
        /// </summary>
        /// <param name="writer">output data stream</param>
        /// <param name="indent">size of indentation</param>
        internal void DrawHtmlNoChange(XmlWriter writer, int indent) 
        {
            Debug.Assert(this.NodeType != XmlNodeType.Element && this.NodeType != XmlNodeType.Attribute);
            Debug.Assert(this.Operation != XmlDiffViewOperation.Change);
            XmlDiffView.HtmlStartRow(writer);
            this.DrawLinkNode(writer);

            for (int i = 0; i < 2; i++) 
            {
                XmlDiffView.HtmlStartCell(writer, indent);
                if (XmlDiffView.HtmlWriteToPane[(int)this.Operation, i]) 
                {
                    bool closeElement = this.OutputNavigationHtml(writer);
                    XmlDiffView.HtmlWriteString(
                        writer, 
                        this.Operation, 
                        this.OuterXml);
                    if (closeElement) 
                    {
                        writer.WriteEndElement();
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

        internal void DrawLinkNode(XmlWriter writer) {
            writer.WriteStartElement("td");
            if (this.operationId != XmlDiffView.LastVisitedOpId) {
                XmlDiffView.LastVisitedOpId = this.operationId;
                bool prev = false;
                if (this.operationId != 0) {
                    writer.WriteStartElement("a");
                    writer.WriteAttributeString("name", "id" + operationId);
                }
                if (this.operationId > 1) {
                    writer.WriteStartElement("a");
                    writer.WriteAttributeString("href", "#id" + (operationId - 1));
                    writer.WriteString("prev");
                    writer.WriteEndElement();
                    prev = true;
                }
                if (this.operationId > 0 && this.operationId+1 < XmlDiffView.LastOperationId) {
                    if (prev) {
                        writer.WriteStartElement("br");
                        writer.WriteEndElement();
                    }
                    writer.WriteStartElement("a");
                    writer.WriteAttributeString("href", "#id" + (operationId + 1));
                    writer.WriteString("next");
                    writer.WriteEndElement();
                    writer.WriteStartElement("br");
                    writer.WriteEndElement();
                }
                if (this.operationId != 0) {
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();
        }

        /// <summary>
        /// Adds bookmarks and links to navigate from and to the moved items
        /// </summary>
        /// <param name="writer">The output writer object</param>
        /// <returns>A closing 'A' element is needed</returns>
        /// <remarks>When this returns 'true' use the 'WriteEndElement' 
        /// method later in the calling funtions to write the closing 'A' tag.</remarks>
        protected bool OutputNavigationHtml(XmlWriter writer) 
        {
            if (this.Parent == null || this.Parent.Operation != this.Operation) 
            {
                switch (this.Operation) 
                {
                    case XmlDiffViewOperation.MoveFrom:
                        writer.WriteStartElement("a");
                        writer.WriteAttributeString("name", "move_from_" + this.OperationId);
                        writer.WriteEndElement();
                        writer.WriteStartElement("a");
                        writer.WriteAttributeString("href", "#move_to_" + this.OperationId);
                        return true;
                    case XmlDiffViewOperation.MoveTo:
                        writer.WriteStartElement("a");
                        writer.WriteAttributeString("name", "move_to_" + this.OperationId);
                        writer.WriteEndElement();
                        writer.WriteStartElement("a");
                        writer.WriteAttributeString("href", "#move_from_" + this.OperationId);
                        return true;
                }
            }
            return false;
        }

        #endregion
        
        /// <summary>
        /// Strips the provided text of tabs and newline characters  
        /// </summary>
        /// <param name="innerText">text from which to remove 
        /// the special characters</param>
        /// <returns>the statement after the special characters
        /// have been removed</returns>
        protected string RemoveTabsAndNewlines(string innerText)
        {
            const string tab = "\t";
            // Remove tabs
            innerText = innerText.Replace(tab, String.Empty);
            // remove newlines
            innerText = innerText.Replace(Environment.NewLine, String.Empty);
            // trim off leading and trailing spaces
            innerText = innerText.Trim();

            return innerText;
        }

        #region Subclasses section

        /// <summary>
        /// Class to hold values of the changed data.
        /// </summary>
        internal class ChangeInfo 
        {
            #region Member variables section
            
            /// <summary>
            /// Name of the node without the prefix
            /// </summary>
            private string localName;
            /// <summary>
            /// Prefix for the node except for a DocumentType
            /// node this holds the "attribute" value for the 'PUBLIC' "attribute".
            /// </summary>
            private string prefix;
            /// <summary>
            /// URI for the node except for a DocumentType
            /// node this holds the "attribute" value for the 'SYSTEM' "attribute".
            /// </summary>
            private string namespaceUri;
            /// <summary>
            /// internal subset for a DocumentType node
            /// </summary>
            private string subset;
            
            #endregion

            #region Properties section

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
            public string Subset
            {
                get
                {
                    return this.subset;
                }
                set
                {
                    this.subset = value;
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
            
            #endregion
        }
        #endregion
    }
}