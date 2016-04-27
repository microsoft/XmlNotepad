//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffPathMultiNodeList.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Methods to handle nodes which have child nodes.
// </summary>
// <history>
//      [barryw] 03MAR15 Created
// </history>
//  ---------------------------------------------------------------------------

namespace Microsoft.XmlDiffPatch
{
    #region Using directives

    using System;
    using System.Diagnostics;

    #endregion

    /// <summary>
    /// Class which provides methods 
    /// to navigate sibling nodes. 
    /// </summary>
    internal class XmlDiffPathMultiNodeList : XmlDiffPathNodeList
    {
        #region Member variables section

        /// <summary>
        /// Number of sibling nodes in the list of data.
        /// </summary>
        private int count = 0;

        /// <summary>
        /// Collection of sibling nodes
        /// </summary>
        private ListChunk chunks = null;
        
        /// <summary>
        /// Reference to the previous collection of sibling nodes
        /// </summary>
        private ListChunk lastChunk = null;
        
        /// <summary>
        /// Reference to collection of sibling nodes
        /// </summary>
        private ListChunk currentChunk = null;
        
        /// <summary>
        /// The index to the current collection of
        /// sibling nodes
        /// </summary>
        private int currentChunkIndex = -1;
        
        #endregion

        #region  Constructors section

        /// <summary>
        /// Constructor
        /// </summary>
        public XmlDiffPathMultiNodeList()
        {
        }
 
        #endregion

        #region Properties section

        /// <summary>
        /// Gets the current sibling node.
        /// </summary>
        public override XmlDiffViewNode Current
        {
            get
            {
                if (this.currentChunk == null || this.currentChunkIndex < 0)
                {
                    return null;
                }
                else
                {
                    return this.currentChunk[this.currentChunkIndex];
                }
            }
        }

        /// <summary>
        /// Gets the number of sibling nodes
        /// </summary>
        public override int Count
        {
            get
            {
                return this.count;
            }
        }

        #endregion
        
        #region Indexers section

        #endregion
        
        #region Methods section

        /// <summary>
        /// Move to the next list (branch) of nodes
        /// </summary>
        /// <returns>Moved to the next list of nodes</returns>
        public override bool MoveNext()
        {
            if (this.currentChunk == null)
            {
                return false;
            }
            
            if (this.currentChunkIndex >= this.currentChunk.Count - 1)
            {
                if (this.currentChunk.Next == null)
                {
                    return false;
                }
                else
                {
                    this.currentChunk = this.currentChunk.Next;
                    this.currentChunkIndex = 0;
                    Debug.Assert(this.currentChunk.Count > 0);
                    return true;
                }
            }
            else
            {
                this.currentChunkIndex++;
                return true;
            }
        }

        /// <summary>
        /// Reset the position in the lists of nodes.
        /// </summary>
        public override void Reset()
        {
            this.currentChunk = this.chunks;
            this.currentChunkIndex = -1;
        }

        /// <summary>
        /// Adds a node to the current list of data.
        /// </summary>
        /// <param name="node">Node object to add</param>
        public override void AddNode(XmlDiffViewNode node)
        {
            if (this.lastChunk == null)
            {
                this.chunks = new ListChunk();
                this.lastChunk = this.chunks;
                this.currentChunk = this.chunks;
            }
            else if (this.lastChunk.Count == ListChunk.ChunkSize)
            {
                this.lastChunk.Next = new ListChunk();
                this.lastChunk = this.lastChunk.Next;
            }

            this.lastChunk.AddNode(node);
            this.count++;
        }

        #endregion

        #region Subclasses section

        /// <summary>
        /// Class to provide an array in which
        /// to hold nodes and the ability to navigate
        /// the array.
        /// </summary>
        private class ListChunk
        {
            #region Constants section

            /// <summary>
            /// Maximum number of nodes in the list.
            /// </summary>
            public const int ChunkSize = 10;

            #endregion

            #region Member variables section

            /// <summary>
            /// Create the list of nodes object. 
            /// </summary>
            private XmlDiffViewNode[] nodes = new XmlDiffViewNode[ChunkSize];
            
            /// <summary>
            /// Number of nodes in the list.
            /// </summary>
            private int count = 0;
            
            /// <summary>
            /// Initialize the next chuck of data.
            /// </summary>
            private ListChunk next = null;

            #endregion

            #region Properties section

            /// <summary>
            /// Gets the number of nodes (chunks of
            /// data) in the current list (branch) of data. Read only.
            /// </summary>
            /// <value></value>
            public int Count
            {
                get
                {
                    return this.count;
                }
            }

            /// <summary>
            /// Gets or sets the next chuck of data 
            /// </summary>
            /// <value>Reference to the next list of nodes</value>
            public ListChunk Next
            {
                get
                {
                    return this.next;
                }
                
                set
                {
                    this.next = value;
                }
            }

            #endregion
        
            #region Indexers section

            /// <summary>
            /// Gets a node based on its index in the collection.
            /// </summary>
            /// <param name="index">index to node</param>
            public XmlDiffViewNode this[int index]
            {
                get
                {
                    return this.nodes[index];
                }
            }
            
            #endregion
        
            #region Methods section

            /// <summary>
            /// Adds a node object to the collection.
            /// </summary>
            /// <param name="node">node object to add</param>
            public void AddNode(XmlDiffViewNode node)
            {
                Debug.Assert(this.count < ChunkSize);
                this.nodes[this.count++] = node;
            }

            #endregion
        }

        #endregion
    }
}
