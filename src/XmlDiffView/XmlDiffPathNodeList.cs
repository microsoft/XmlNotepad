//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffPathNodeList.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Abstract methods to navigate the nodes in a list.
// </summary>
// <history>
//      [barryw] 03MAR15 Created
// </history>
//  ---------------------------------------------------------------------------

namespace Microsoft.XmlDiffPatch
{
    #region Using directives

    using System;

    #endregion

    /// <summary>
    /// Abstract class to provides methods to navigate nodes.  
    /// </summary>
    internal abstract class XmlDiffPathNodeList
    {
        #region Properties section

        /// <summary>
        ///  Gets the current node.
        /// </summary>
        public abstract XmlDiffViewNode Current
        {
            get;
        }

        /// <summary>
        /// Gets the number of nodes in the list
        /// </summary>
        public abstract int Count
        {
            get;
        }

        #endregion
        
        #region Methods section
        
        /// <summary>
        /// Add a node to the current list of data.
        /// </summary>
        /// <param name="node">Node object to add</param>
        public abstract void AddNode(XmlDiffViewNode node);
        
        /// <summary>
        /// Reset the position in the list of nodes.  
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// Move to the next list of nodes 
        /// </summary>
        /// <returns>Moved to the next list of nodes</returns>
        public abstract bool MoveNext();

        #endregion

    }
}
