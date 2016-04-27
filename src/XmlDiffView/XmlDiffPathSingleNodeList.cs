//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffPathSingleNodeList.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//    A collection containing a single node  
// </summary>
// <history>
//      [barryw] 03/07/2005 Created
// </history>
//  ---------------------------------------------------------------------------
namespace Microsoft.XmlDiffPatch
{
    #region Using directives

    using System;

    #endregion

    /// <summary>
    /// Class to create a collection containing only a single node.
    /// </summary>
    internal class XmlDiffPathSingleNodeList : XmlDiffPathNodeList
    {
        #region Constants section

        #endregion

        #region Member variables section
        
        /// <summary>
        /// Declares a node object.
        /// </summary>
        private XmlDiffViewNode node;
        /// <summary>
        /// Initialize the nodes state. 
        /// </summary>
        private State state = State.BeforeNode;

        #endregion
        
        #region  Constructors section

        /// <summary>
        /// Constructor
        /// </summary>
        public XmlDiffPathSingleNodeList()
        {
        }

        #endregion

        #region Destructors section

        #endregion

        #region Delegates section

        #endregion
       
        #region Events section

        #endregion
        
        #region Enums section

        /// <summary>
        /// Node states
        /// </summary>
        public enum State
        {
            /// <summary>
            /// Positioned before the node
            /// </summary>
            BeforeNode = 0,
            /// <summary>
            /// Positioned on the node
            /// </summary>
            OnNode = 1,
            /// <summary>
            /// Positioned after the node
            /// </summary>
            AfterNode = 2
        }

        #endregion
        
        #region Interfaces section

        #endregion
        
        #region Properties section

        /// <summary>
        /// Gets the count of the nodes in the list.
        /// </summary>
        public override int Count
        {
            get
            {
                // fixed at a single node
                return 1;
            }
        }

        /// <summary>
        /// Gets a reference to the current node.  Returns 
        /// null if not positioned on the node. 
        /// </summary>
        public override XmlDiffViewNode Current
        {
            get
            {
                return (this.state == State.OnNode) ? this.node : null;
            }
        }

        /// <summary>
        /// Gets a reference to the node regardless of its state. 
        /// </summary>
        public XmlDiffViewNode Node
        {
            get
            {
                return this.node;
            }
        }

        /// <summary>
        /// Sets the state of the node.
        /// </summary>
        public State NodePostion
        {
            set
            {
                this.state = value;
            }
        }
        #endregion
        
        #region Indexers section

        #endregion
        
        #region Methods section

        /// <summary>
        /// Change the state of the node's position.
        /// </summary>
        /// <returns>Changed the node's position state</returns>
        public override bool MoveNext()
        {
            switch (this.state)
            {
                case State.BeforeNode:
                    this.state = State.OnNode;
                    return true;
                case State.OnNode:
                    this.state = State.AfterNode;
                    return false;
                case State.AfterNode:
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Reset the nodes position
        /// </summary>
        public override void Reset()
        {
            this.state = State.BeforeNode;
        }

        /// <summary>
        /// Add a node to the list.  This method should only be 
        /// called once otherwise an exception will be raised.
        /// </summary>
        /// <param name="node">The node to add</param>
        public override void AddNode(XmlDiffViewNode node)
        {
            if (this.node != null)
            {
                throw new Exception(
                    "XmlDiffPathSingleNodeList can contain one node only.");
            }
            this.node = node;
        }

        #endregion
        
        #region Structs section

        #endregion
        
        #region Subclasses section

        #endregion
    }
}
