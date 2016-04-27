//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="Tags.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Constants for a variety of opening and closing tags for 
//     xml nodes, and other xml data types.
// </summary>
// <history>
//      [barryw] 24JAN05 Created
// </history>
//  ---------------------------------------------------------------------------

namespace Microsoft.XmlDiffPatch
{
    #region Using directives

    using System;

    #endregion

    #region Xml Tags Class

    /// -----------------------------------------------------------------------------
    /// <summary>
    /// Class to contain constants for all text Xml-like and other
    /// tags used for formatting the output as text.
    /// </summary>
    /// <history>
    /// 	[barryw] 2/24/2005 Created
    /// </history>
    /// -----------------------------------------------------------------------------
    internal class Tags
    {
        #region Constants section

        public const string XmlOpenBegin = "<";

        public const string XmlOpenEnd = ">";

        public const string XmlOpenEndTerse = "/>";

        public const string XmlCloseBegin = "</";

        public const string XmlCloseEnd = ">";

        public const string XmlDeclarationBegin = "<?xml ";

        public const string XmlDeclarationEnd = " ?>";

        public const string XmlDocumentTypeBegin = "<!DOCTYPE ";

        public const string XmlDocumentTypeEnd = ">";

        public const string XmlCharacterDataBegin = "<![CDATA[";

        public const string XmlCharacterDataEnd = "]]>";

        public const string XmlCommentBegin = "<comment>";

        public const string XmlCommentEnd = "</comment>";

        public const string XmlCommentOldStyleBegin = "<!--";

        public const string XmlCommentOldStyleEnd = "-->";

        public const string XmlErrorHandlingBegin = "<?";

        public const string XmlErrorHandlingEnd = " ?>";

        public const string PrefixColon = ":";

        public const string DtdPublic = "PUBLIC ";

        public const string DtdSystem = "SYSTEM ";

        #endregion
        #region  Constructors section

        private Tags()
        {
        }

        #endregion
    }
    #endregion
}
