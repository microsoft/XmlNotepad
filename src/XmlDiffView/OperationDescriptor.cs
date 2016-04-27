//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="OperationDescriptor.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     An object which associates an operation identification number 
///    (which is the key to a hashtable), and the type of
///    the data changes to a list of nodes which identifies 
///    the location of the change.
// </summary>
// <history>
//      [barryw] 03MAR05 Adapted from sample file.
// </history>
//  ---------------------------------------------------------------------------

namespace Microsoft.XmlDiffPatch
{
    using System;

    /// <summary>
    ///     Class to associate an operation identification number 
    ///     (which is the key to a hashtable), and the type of
    ///     the data changes to a list of nodes which identifies 
    ///     the location of the change.
    /// </summary>
    internal class OperationDescriptor
    {
        #region Constants section

        #endregion

        #region Member variables section

        /// <summary>
        /// Change operation identifer, used to indicate the 
        /// from/to changes in the position of data.
        /// </summary>
        private int operationId;

        /// <summary>
        /// The type of change in data.
        /// </summary>
        /// <example>Move, Prefix change, and 
        /// Namespace change</example>
        private Type operationType;

        /// <summary>
        /// Declares a local copy of a list of (baseline data?) nodes.
        /// </summary>
        private XmlDiffPathMultiNodeList nodeList;

        #endregion
        
        #region  Constructors section

        /// <summary>
        ///  Constructor
        /// </summary>
        /// <param name="opid">Operation identification number</param>
        /// <param name="type">Type of change in the data</param>
        public OperationDescriptor(int opid, Type type)
        {
            this.operationId = opid;
            this.operationType = type;
            this.nodeList = new XmlDiffPathMultiNodeList();
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
        /// Enumerator for the types of changes in the data. 
        /// </summary>
        public enum Type 
        {
            /// <summary>
            /// The data was moved
            /// </summary>
            Move,
            /// <summary>
            /// The namespace data was changed
            /// </summary>
            NamespaceChange,
            /// <summary>
            /// The xml prefix was changed
            /// </summary>
            PrefixChange,
        }

        #endregion
        
        #region Interfaces section

        #endregion
        
        #region Properties section

        /// <summary>
        /// Gets the type of change in the data. 
        /// </summary>
        public Type OperationType
        {
            get
            {
                return this.operationType;
            }
        }

        /// <summary>
        /// Gets the nodes which identify the location of the data change.
        /// </summary>
        public XmlDiffPathMultiNodeList NodeList
        {
            get
            {
                return this.nodeList;
            }
        }

        #endregion
        
        #region Indexers section

        #endregion
        
        #region Methods section

        #endregion
        
        #region Structs section

        #endregion
        
        #region Subclasses section

        #endregion
    }
}
