//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="Difference.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Constants used for marking the nature of differences detected in the xml data.
// </summary>
// <history>
//      [barryw] 24JAN05 Created
// </history>
//  ---------------------------------------------------------------------------

namespace Microsoft.XmlDiffPatch
{
    using System;

    #region Difference Class

    /// -----------------------------------------------------------------------------
    /// <summary>
    /// Class to contain constants for all text relating to 
    /// differences in the xml and used for formatting the 
    /// output as text.
    /// </summary>
    /// <history>
    /// 	[barryw] 2/24/2005 Created
    /// </history>
    /// -----------------------------------------------------------------------------
    internal class Difference
    {
        /// <summary>
        /// Constant for the name of the root node for the output
        /// in text format. When using, prefix
        /// with the 'Tag' constant.
        /// </summary>
        public const string NodeDifferences = "DIFFERENCES";

        /// <summary>
        /// Constant for the prefix which indicates xml
        /// differences.
        /// </summary>
        /// <remarks>The underscore character is used
        ///  as a separator because it is an allowed 
        ///  character in a node name.
        ///  </remarks>
        public const string Tag = "xd_";

        /// <summary>
        /// Constant to indicate the start of a changed 
        /// value statement. When using, prefix
        /// with the 'Tag' constant.
        /// </summary>
        public const string ChangeBegin = "ChangeFrom('";

        /// <summary>
        /// Constant to indicate the middle ('To' portion)
        /// of a changed value statement.
        /// </summary>
        public const string ChangeTo = "')To('";

        /// <summary>
        /// Constant to indicate the end
        /// of a changed value statement.
        /// </summary>
        public const string ChangeEnd = "')";

        /// <summary>
        /// Constant to indicate the start of the name of 
        /// an added attribute. When using, prefix
        /// with the 'Tag' constant.
        /// </summary>
        public const string AttributeAddedBegin = "AttributeAdded('";

        /// <summary>
        /// Constant to indicate the end of the name of 
        /// an added attribute.
        /// </summary>
        public const string AttributeAddedEnd = "')";

        /// <summary>
        /// Constant to indicate the start of the name of 
        /// a deleted attribute. When using, prefix
        /// with the 'Tag' constant.
        /// </summary>
        public const string AttributeDeletedBegin = "AttributeDeleted('";

        /// <summary>
        /// Constant to indicate the end of the name of 
        /// a deleted attribute.
        /// </summary>
        public const string AttributeDeletedEnd = "')";

        /// <summary>
        /// Constant to indicate 
        /// an additional node (and its attributes and 
        /// children). When using, prefix with the 'Tag'
        /// constant.
        /// </summary>
        public const string NodeAdded = "='Add(node)'";

        /// <summary>
        /// Constant to indicate 
        /// a deleted node (and its attributes and 
        /// children). When using, prefix with the 'Tag'
        /// constant.
        /// </summary>
        public const string NodeDeleted = "='Delete(node)'";

        public const string NodeMovedFromBegin = "=Move(node)From('";
        public const string NodeMovedFromEnd = "')";

        public const string NodeMovedToBegin = "=Move(node)To('";
        public const string NodeMovedToEnd = "')";

        public const string NodeRenamedBegin = "=Rename(node)From('";
        public const string NodeRenamedEnd = "')";

        /// <summary>
        /// Constant to indicate the start of the added text, e.g., 
        /// the value of an added node value, or comments. When 
        /// using, prefix with the 'Tag' constant.
        /// </summary>
        public const string TextAddedBegin = "Add('";

        /// <summary>
        /// Constant to indicate the end of the added text, e.g., value of 
        /// an added node value, or comments.
        /// </summary>
        public const string TextAddedEnd = "')";

        /// <summary>
        /// Constant to indicate the start of the value of 
        /// a deleted node value. When using, prefix
        /// with the 'Tag' constant.
        /// </summary>
        public const string TextDeletedBegin = "Delete('";

        /// <summary>
        /// Constant to indicate the end of the value of 
        /// a deleted node value.
        /// </summary>
        public const string TextDeletedEnd = "')";

        /// <summary>
        /// CharData, Comment, or Text moved from this position,
        /// when using append the OperationId number.
        /// </summary>
        public const string TextMovedFromBegin = "=Move(text)From('";
        public const string TextMovedFromEnd = "')";

        /// <summary>
        /// CharData, Comment, or Text moved to this position,
        /// when using append the OperationId number.
        /// </summary>
        public const string TextMovedToBegin = "=Move(text)To('";
        public const string TextMovedToEnd = "')";

        // Node Type: Processing instruction section

        public const string PIAdded = "='Add(component)' ";

        public const string PIDeleted = "='Delete(component)' ";

        public const string PIRenamedBegin = "=Rename(component)From('";
        public const string PIRenamedEnd = "')";

        public const string PIMovedFromBegin = "=Move(component)From('";
        public const string PIMovedFromEnd = "')";

        public const string PIMovedToBegin = "=Move(component)To('";
        public const string PIMovedToEnd = "')";

        public const string DeclarationAdded = "='Add(declaration)' ";

        public const string DeclarationDeleted = "='Delete(declaration)' ";

        public const string DeclarationMovedFromBegin = "=Move(declaration)From('";
        public const string DeclarationMovedFromEnd = "')";

        public const string DeclarationMovedToBegin = "=Move(declaration)To('";
        public const string DeclarationMovedToEnd = "')";

        public const string DocumentTypeAdded = "='Add(doctype)' ";

        public const string DocumentTypeDeleted = "='Delete(doctype)' ";

        public const string DocumentTypeMovedFromBegin = "=Move(doctype)From('";
        public const string DocumentTypeMovedFromEnd = "')";

        public const string DocumentTypeMovedToBegin = "=Move(doctype)To('";
        public const string DocumentTypeMovedToEnd = "')";

    }
   #endregion
}