//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffView.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Public entry point for the library.
// </summary>
// <history>
//      [barryw] 01MAR05 Adapted from sample file.
// </history>
//  ---------------------------------------------------------------------------

namespace Microsoft.XmlDiffPatch
{
    #region Using directives

    using System;
    using System.Xml;
    using System.IO;
    using System.Diagnostics;
    using System.Collections;
    using Microsoft.XmlDiffPatch;

    #endregion
    
    #region Library Enums section
       
   
    /// <summary>
    /// Enumerator values for types of differences
    /// (Match indicates no difference) 
    /// </summary>
    internal enum XmlDiffViewOperation
    {
        /// <summary>
        /// Data matches
        /// </summary>
        Match = 0,

        /// <summary>
        /// Data differences will be ignored
        /// </summary>
        Ignore = 1,

        /// <summary>
        /// Data was added
        /// </summary>
        Add = 2,

        /// <summary>
        /// Data was moved to here
        /// </summary>
        MoveTo = 3,

        /// <summary>
        /// Data was removed
        /// </summary>
        Remove = 4,

        /// <summary>
        /// Data was moved from here
        /// </summary>
        MoveFrom = 5,

        /// <summary>
        /// Data was changed
        /// </summary>
        Change = 6,
    }

    #endregion

    /// <summary>
    /// Class which provides the external methods
    /// and properties to use this library.
    /// </summary>
    public sealed class XmlDiffView
    {
        #region Member variables section

        // Static methods and data for drawing

        /// <summary>
        /// Size of the incremental indentation
        /// </summary>
        internal static readonly int DeltaIndent = 15;

        /// <summary>
        /// Operation settings to control writing to 
        /// the baseline and actual "panes" of the html.
        /// </summary>
        internal static readonly bool[,] HtmlWriteToPane = 
        {
            // Match    = 0
            {
                true,  true
            },
            
            // Ignore   = 1
            {
                true,  true
            },
            
            // Add      = 2
            {
                false,  true
            },

            // MoveTo   = 3,
            {
                false,  true
            },  

            // Remove   = 4,
            {
                true, false
            },  

            // MoveFrom = 5,
            {
                true, false
            },  

            // Change   = 6,
            {
                true,  true
            },  
        };

        /// <summary>
        /// String used to set a required number html space characters.
        /// </summary>
        private static readonly string Nbsp = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" +
            "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" +
            "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";

        /// <summary>
        /// Declares a view document to hold the merged data.
        /// </summary>
        private XmlDiffViewDocument viewDocument = null;
        
        /// <summary>
        /// Creates a hash table of change short descriptions.
        /// </summary>
        private Hashtable descriptors = new Hashtable();
        
        /// <summary>
        /// Declares a memory stream to hold the diffgram.
        /// </summary>
        private MemoryStream diffgram = null;
        
        /// <summary>
        /// Declares a TextWriter object to hold the output data.
        /// </summary>
        private TextWriter outputData = null;

        /// <summary>
        /// Stores the final output.
        /// </summary>
        private XmlDiffViewResults finalOutput = null;

        // options

        /// <summary>
        /// Initializes the child order option
        /// </summary>
        private bool ignoreChildOrder = false;
        
        /// <summary>
        /// Initializes the comments option
        /// </summary>
        private bool ignoreComments = false;
        
        /// <summary>
        /// Initializes the programming instructions node type option
        /// </summary>
        private bool ignorePI = false;
        
        /// <summary>
        /// Initializes the white space option
        /// </summary>
        private bool ignoreWhitespace = false;
        
        /// <summary>
        /// Initializes the namespaces node type option
        /// </summary>
        private bool ignoreNamespaces = false;
        
        /// <summary>
        /// Initializes the xml prefixes option
        /// </summary>
        private bool ignorePrefixes = false;
        
        /// <summary>
        /// Initializes the declaration node type option
        /// </summary>
        private bool ignoreXmlDecl = false;
        
        /// <summary>
        /// Initializes the DTD node type option
        /// </summary>
        private bool ignoreDtd = false;

        /// <summary>
        /// Declares an object with references to the last
        /// node and attribute.
        /// </summary>
        private LoadState loadState;

        private static int nextOperationId = 1;

        #endregion

        #region  Constructors section

        /// <summary>
        /// Constructor
        /// </summary>
        public XmlDiffView()
        {
        }

        #endregion

        #region Destructors section

        /// <summary>
        /// Destructor
        /// </summary>
        ~XmlDiffView()
        {
        }

        #endregion

        #region Methods section

        #region Public static methods section

        #endregion

        #region Public non-static methods section

        public static int NextOperationId {
            get { return nextOperationId++; }
        }

        public static int LastOperationId {
            get { return nextOperationId; }
        }

        /// <summary>
        /// Loads the diffgram to an XmlDocument object, and drives the 
        /// process of loading the baseline xml file to a 
        /// XmlDiffViewDocument object, tagging the nodes and attributes
        /// according to the options selected, and finally merges the
        /// diffgram data into the XmlDiffViewDocument object. 
        /// </summary>
        /// <param name="sourceXml">baseline data</param>
        /// <param name="diffgram">diffgram data stream</param>
        public void Load(XmlReader sourceXml, XmlReader diffGram)
        {
            nextOperationId = 1;

            if (null == sourceXml)
            {
                throw new ArgumentNullException("sourceXml");
            }

            if (null == diffGram)
            {
                throw new ArgumentNullException("diffgram");
            }

            // load diffgram to DOM
            XmlDocument diffgramDoc = new XmlDocument();
            diffgramDoc.Load(diffGram);

            // process operation descriptors
            this.PreprocessDiffgram(diffgramDoc);

            // load document
            this.viewDocument = new XmlDiffViewDocument();
            this.LoadSourceChildNodes(this.viewDocument, sourceXml, false);

            // apply diffgram
            this.ApplyDiffgram(diffgramDoc.DocumentElement, this.viewDocument);
        }

        /// <summary>
        /// Write the differences in the Xml data 
        /// to the output file as formatted xml-like text.
        /// </summary>
        /// <param name="sourceXmlFile">baseline file</param>
        /// <param name="changedXmlFile">actual file</param>
        /// <param name="outputTextPath">Output data file for the data in text form</param>
        /// <param name="appendToOutputFile">Append output to the file</param>
        /// <param name="fragment">This is an xml data frament</param>
        /// <param name="options">Comparison options</param>
        /// <returns>data is identical</returns>
        public bool DifferencesAsFormattedText(
            string sourceXmlFile,
            string changedXmlFile,
            string outputTextPath,
            bool appendToOutputFile,
            bool fragment,
            XmlDiffOptions options)
        {
            // Append to the specified output file.
            FileMode mode;
            if (appendToOutputFile)
            {
                mode = FileMode.Append;
            }
            else
            {
                mode = FileMode.Create;
            }
            this.outputData = new StreamWriter(
                new FileStream(
                outputTextPath,
                mode,
                FileAccess.Write));

            bool identicalData;
            identicalData = this.GetDifferencesAsFormattedText(
                sourceXmlFile,
                changedXmlFile,
                fragment,
                options);

            // close the output stream to release the file.
            this.outputData.Close();

            return identicalData;
        }

        /// <summary>
        /// Append the differences in the Xml data 
        /// to the output file as formatted xml-like text.
        /// </summary>
        /// <param name="sourceXmlFile">baseline file</param>
        /// <param name="changedXmlFile">actual file</param>
        /// <param name="outputTextPath">output file for the data in text form</param>
        /// <param name="fragment">This is an xml data frament</param>
        /// <param name="options">Comparison options</param>
        /// <returns>Differences were not found.</returns>
        public bool DifferencesAsFormattedText(
            string sourceXmlFile,
            string changedXmlFile,
            string outputTextPath,
            bool fragment,
            XmlDiffOptions options)
        {
            bool identicalData = this.DifferencesAsFormattedText(
                sourceXmlFile,
                changedXmlFile,
                outputTextPath,
                true,
                fragment,
                options);

            return identicalData;
        }

        /// <summary>
        /// Write the differences in the Xml data 
        /// as formatted xml-like text return in
        /// a memory based TextReader object.
        /// </summary>
        /// <param name="sourceXmlFile">baseline file</param>
        /// <param name="changedXmlFile">actual file</param>
        /// <param name="fragment">This is an xml data frament</param>
        /// <param name="options">Comparison options</param>
        /// <param name="reader">A reference to return readable 
        /// formatted xml-like text.</param>
        /// <returns>data is identical</returns>
        public XmlDiffViewResults DifferencesAsFormattedText(
            string sourceXmlFile,
            string changedXmlFile,
            bool fragment,
            XmlDiffOptions options)
        {
            MemoryStream data = new MemoryStream();
            this.outputData = new StreamWriter(
                data,
                System.Text.Encoding.Unicode);

            bool identicalData;
            identicalData = this.GetDifferencesAsFormattedText(
                sourceXmlFile,
                changedXmlFile,
                fragment,
                options);

            // Move the data to the memory stream
            this.outputData.Flush();

            this.finalOutput = new XmlDiffViewResults(data, identicalData);

            // return result of comparison
            return this.finalOutput;
        }

        /// <summary>
        /// Create WinDiff like static comparison in Html.
        /// </summary>
        /// <param name="sourceXmlFile">the baseline file</param>
        /// <param name="changedXmlFile">the actual (or target) file</param>
        /// <param name="resultHtmlViewFile">the html output file</param>
        /// <param name="fragment">the file is only an Xml fragment</param>
        /// <param name="options">comparison filtering options</param>
        /// <returns>Differences were not found after filtering.</returns>
        public bool DifferencesSideBySideAsHtml(
            string sourceXmlFile,
            string changedXmlFile,
            string resultHtmlViewFile,
            bool fragment,
            XmlDiffOptions options)
        {
            bool identicalData = this.DifferencesSideBySideAsHtml(
                sourceXmlFile,
                changedXmlFile,
                resultHtmlViewFile,
                fragment,
                true,
                options);

            return identicalData;
        }

        /// <summary>
        /// Create WinDiff like static comparison in Html.
        /// </summary>
        /// <param name="sourceXmlFile">the baseline file</param>
        /// <param name="changedXmlFile">the actual (or target) file</param>
        /// <param name="resultHtmlViewFile">the html output file</param>
        /// <param name="fragment">the file is only an Xml fragment</param>
        /// <param name="appendToOutputFile">Append to existing output file</param>
        /// <param name="options">comparison filtering options</param>
        /// <returns>Differences were not found after filtering.</returns>
        public bool DifferencesSideBySideAsHtml(
            string sourceXmlFile,
            string changedXmlFile,
            string resultHtmlViewFile,
            bool fragment,
            bool appendToOutputFile,
            XmlDiffOptions options)
        {
            // Append to the specified output file.
            FileMode mode;
            if (appendToOutputFile)
            {
                mode = FileMode.Append;
            }
            else
            {
                mode = FileMode.Create;
            }
            this.outputData = new StreamWriter(
                new FileStream(
                resultHtmlViewFile,
                mode,
                FileAccess.Write));

            bool identicalData;
            try
            {
                identicalData = this.DifferencesSideBySideAsHtml(
                    sourceXmlFile,
                    changedXmlFile,
                    fragment,
                    options,
                    this.outputData);
            }
            finally
            {
                this.outputData.Close();
            }

            return identicalData;
        }

        /// <summary>
        /// Create WinDiff like static comparison in Html.
        /// </summary>
        /// <param name="sourceXmlFile">the baseline file</param>
        /// <param name="changedXmlFile">the actual (or target) file</param>
        /// <param name="fragment">the file is only an Xml fragment</param>
        /// <param name="options">comparison filtering options</param>
        /// <param name="reader">Readable output data stream</param>
        /// <returns>Differences were not found after filtering.</returns>
        public XmlDiffViewResults DifferencesSideBySideAsHtml(
            string sourceXmlFile,
            string changedXmlFile,
            bool fragment,
            XmlDiffOptions options)
        {
            MemoryStream data = new MemoryStream();
            try
            {
                this.outputData = new StreamWriter(
                    data,
                    System.Text.Encoding.Unicode);

                bool identicalData = this.DifferencesSideBySideAsHtml(
                    sourceXmlFile,
                    changedXmlFile,
                    fragment,
                    options,
                    this.outputData);

                // Move the data to the memory stream
                this.outputData.Flush();

                // Generate the final output using the returned values
                // from the differences comparison.
                this.finalOutput = new XmlDiffViewResults(data, identicalData);
            }
            finally
            {
                if (null != data)
                {
                    data.Close();
                }
            }            // return result of comparison
            return this.finalOutput;
        }

        internal static int LastVisitedOpId = 0;

        

        /// <summary>
        /// Converts a copy of the xml data in the 
        /// XmlDiffViewDocument object
        /// to html and writes it out to the 
        /// TextWriter object (which may be a file).
        /// </summary>
        /// <param name="htmlOutput">Data stream for output</param>
        public void GetHtml(TextWriter htmlOutput)
        {
            LastVisitedOpId = 0;

            XmlTextWriter writer = new XmlTextWriter(htmlOutput);
            if (XmlDiffView.LastOperationId > 0) {
                writer.WriteStartElement("tr");
                    writer.WriteStartElement("td");
                        writer.WriteStartElement("a");
                        writer.WriteAttributeString("href", "#id1");
                        writer.WriteString("first");
                        writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteStartElement("td");
                    writer.WriteEndElement();
                    writer.WriteStartElement("td");
                    writer.WriteEndElement();
                writer.WriteEndElement();
            }
            this.viewDocument.DrawHtml(writer, 10);

            if (XmlDiffView.LastOperationId > 0) {
                writer.WriteStartElement("tr");
                writer.WriteStartElement("td");
                writer.WriteStartElement("a");
                writer.WriteAttributeString("href", "#id" + (XmlDiffView.LastOperationId-1));
                writer.WriteString("last");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("td");
                writer.WriteEndElement();
                writer.WriteStartElement("td");
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }

        #endregion

        #region Internal static methods section

        /// <summary>
        /// Writes to the specified section
        /// </summary>
        /// <param name="pane">baseline/actual data presentation sections</param>
        /// <param name="str">Statement to be written</param>
        internal static void HtmlWriteString(
            XmlWriter pane,
            string str)
        {
            pane.WriteString(str);
        }

        /// <summary>
        /// Writes to the specified section using
        /// highlighting based on hte type of data change
        /// </summary>
        /// <param name="pane">baseline/actual data presentation sections</param>
        /// <param name="op">Type of data change</param>
        /// <param name="str">Statement to be written</param>
        internal static void HtmlWriteString(
            XmlWriter pane,
            XmlDiffViewOperation op,
            string str)
        {
            HtmlSetColor(pane, op);
            pane.WriteString(str);
            HtmlResetColor(pane);
        }

        /// <summary>
        /// Writes an 'empty' space character.
        /// </summary>
        /// <param name="pane">baseline/actual data presentation sections</param>
        internal static void HtmlWriteEmptyString(XmlWriter pane)
        {
            pane.WriteRaw("&nbsp;");
        }

        /// <summary>
        /// Writes the start of a table cell
        /// </summary>
        /// <param name="writer">output stream</param>
        /// <param name="indent">Text indentation characters</param>
        internal static void HtmlStartCell(XmlWriter writer, int indent)
        {
            writer.WriteStartElement("td");
            writer.WriteAttributeString("style", "padding-left: " + indent.ToString() + "pt;");
        }

        /// <summary>
        /// Writes the closing tags
        /// </summary>
        /// <param name="writer">output stream</param>
        internal static void HtmlEndCell(XmlWriter writer)
        {
            writer.WriteFullEndElement();
        }

        /// <summary>
        /// Inserts an html break-line and writes the tags to 
        /// close the html element
        /// </summary>
        /// <param name="writer">Output data stream</param>
        internal static void HtmlBr(XmlWriter writer)
        {
            writer.WriteStartElement("br");
            writer.WriteEndElement();
        }

        /// <summary>
        /// Starts a new row in the table
        /// </summary>
        /// <param name="writer">output data stream</param>
        internal static void HtmlStartRow(XmlWriter writer)
        {
            writer.WriteStartElement("tr");
        }

        /// <summary>
        /// Ends a row in the table
        /// </summary>
        /// <param name="writer">output data stream</param>
        internal static void HtmlEndRow(XmlWriter writer)
        {
            writer.WriteFullEndElement();
        }

        /// <summary>
        /// Gets 'empty' string characters to use as an 
        /// indentation for formatting the output.
        /// </summary>
        /// <param name="charCount">Number of indentations</param>
        /// <returns>string to use as an indentation</returns>
        internal static string GetIndent(int charCount)
        {
            int nbspCount = charCount * 6;
            if (nbspCount <= Nbsp.Length)
            {
                return Nbsp.Substring(0, nbspCount);
            }
            else
            {
                string indent = string.Empty;
                while (nbspCount > Nbsp.Length)
                {
                    indent += Nbsp;
                    nbspCount -= Nbsp.Length;
                }
                indent += Nbsp.Substring(0, nbspCount);
                return indent;
            }
        }

        /// <summary>
        /// This functions allows the size of the indentation to be
        /// set, and returns one indentation.
        /// </summary>
        /// <param name="charCount">Size of the indent (spaces)</param>
        /// <returns>A string of spaces</returns>
        internal static string IndentText(int charCount)
        {
            const string oneSpace = " ";
            string indent = string.Empty;
            while (charCount > 0)
            {
                // increment the spaces
                indent += oneSpace;

                // decrement the count
                charCount -= 1;
            }
            return indent;
        }

        /// <summary>
        /// Adjusts the format of the data to account for the defined
        /// whitespace characters.
        /// </summary>
        /// <param name="text">data to be formatted</param>
        /// <returns>formatted data</returns>
        internal static string NormalizeText(string text)
        {
            char[] chars = text.ToCharArray();
            int i = 0;
            int j = 0;

            for (;;)
            {
                while (j < chars.Length && IsWhitespace(text[j]))
                {
                    j++;
                }

                while (j < chars.Length && !IsWhitespace(text[j]))
                {
                    chars[i++] = chars[j++];
                }

                if (j < chars.Length)
                {
                    chars[i++] = ' ';
                    j++;
                }
                else
                {
                    if (j == 0)
                    {
                        return string.Empty;
                    }

                    if (IsWhitespace(chars[j - 1]))
                    {
                        i--;
                    }
                    return new string(chars, 0, i);
                }
            }
        }

        /// <summary>
        /// Determines if the specifed character is a 
        /// defined whitespace character.
        /// </summary>
        /// <param name="c">character to test</param>
        /// <returns>specifed character is a 
        /// defined whitespace character</returns>
        internal static bool IsWhitespace(char c)
        {
            return (c == ' ' ||
                c == '\t' ||
                c == '\n' ||
                c == '\r');
        }

        #endregion

        #region Private static methods section

        /// <summary>
        /// Sets the output style based on the type of change in the data. 
        /// </summary>
        /// <param name="pane">baseline/actual data presentation sections</param>
        /// <param name="op">Type of data change</param>
        private static void HtmlSetColor(
            XmlWriter pane,
            XmlDiffViewOperation op)
        {
            pane.WriteStartElement("span");
            pane.WriteAttributeString("class", op.ToString().ToLowerInvariant());
        }

        /// <summary>
        /// Closes the tags after setting the color
        /// </summary>
        /// <param name="pane">baseline/actual data presentation sections</param>
        private static void HtmlResetColor(XmlWriter pane)
        {
            pane.WriteFullEndElement();
        }

        /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.ParseOptions"]/*' />
        /// <summary>
        ///    Translates string representation of XmlDiff options into XmlDiffOptions enum.
        /// </summary>
        /// <param name="options">Value of the 'options' attribute of the 'xd:xmldiff' element in diffgram.</param>
        /// <returns>An object containing the parsing options.</returns>
        private static XmlDiffOptions ParseOptions(string options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            if (options == XmlDiffOptions.None.ToString())
                return XmlDiffOptions.None;
            else
            {
                XmlDiffOptions optionsEnum = XmlDiffOptions.None;

                int j = 0, i = 0;
                while (i < options.Length)
                {
                    j = options.IndexOf(' ', i);
                    if (j == -1)
                        j = options.Length;

                    string opt = options.Substring(i, j - i);

                    switch (opt)
                    {
                        case "IgnoreChildOrder": optionsEnum |= XmlDiffOptions.IgnoreChildOrder; 
                            break;
                        case "IgnoreComments": optionsEnum |= XmlDiffOptions.IgnoreComments; 
                            break;
                        case "IgnoreNamespaces": optionsEnum |= XmlDiffOptions.IgnoreNamespaces; 
                            break;
                        case "IgnorePI": optionsEnum |= XmlDiffOptions.IgnorePI; 
                            break;
                        case "IgnorePrefixes": optionsEnum |= XmlDiffOptions.IgnorePrefixes; 
                            break;
                        case "IgnoreWhitespace": optionsEnum |= XmlDiffOptions.IgnoreWhitespace; 
                            break;
                        case "IgnoreXmlDecl": optionsEnum |= XmlDiffOptions.IgnoreXmlDecl; 
                            break;
                        case "IgnoreDtd": optionsEnum |= XmlDiffOptions.IgnoreDtd; 
                            break;
                        default:
                            throw new ArgumentException("options");
                    }

                    i = j + 1;
                }

                return optionsEnum;
            }
        }

        #endregion

        #region Private non-static methods section

        /// <summary>
        /// Determines if the data changed
        /// </summary>
        /// <param name="sourceXmlFile">baseline file</param>
        /// <param name="changedXmlFile">actual file</param>
        /// <param name="fragment">xml data fragment</param>
        /// <param name="options">Comparison options</param>
        /// <returns>data is identical</returns>
        private bool GetDifferencesAsFormattedText(
            string sourceXmlFile,
            string changedXmlFile,
            bool fragment,
            XmlDiffOptions options)
        {
            bool identicalData = this.MarkupBaselineWithChanges(
                sourceXmlFile,
                changedXmlFile,
                fragment,
                options);

            // only generate the output if there are differences. 
            if (!identicalData)
            {
                // Populate the output
                this.GetText(sourceXmlFile, changedXmlFile);
            }
            return identicalData;
        }

        /// <summary>
        /// Determines if the data changed
        /// </summary>
        /// <param name="sourceXmlFile">baseline file</param>
        /// <param name="changedXmlFile">actual file</param>
        /// <param name="fragment">xml data fragment</param>
        /// <param name="options">Comparison options</param>
        /// <param name="resultHtml">output data</param>
        /// <returns>data is identical</returns>
        private bool DifferencesSideBySideAsHtml(
            string sourceXmlFile,
            string changedXmlFile,
            bool fragment,
            XmlDiffOptions options,
            TextWriter resultHtml)
        {
            bool identicalData = this.MarkupBaselineWithChanges(
                sourceXmlFile,
                changedXmlFile,
                fragment,
                options);

            
                this.SideBySideHtmlHeader(
                    sourceXmlFile,
                    changedXmlFile,
                    identicalData,
                    resultHtml);
            
            this.GetHtml(resultHtml);

            this.SideBySideHtmlFooter(resultHtml);

            return identicalData;
        }

        /// <summary>
        /// Markup the baseline data with changes
        /// </summary>
        /// <param name="sourceXmlFile">baseline xml data</param>
        /// <param name="changedXmlFile">xml data to which to compare</param>
        /// <param name="fragment">xml data fragment</param>
        /// <param name="options">comparison options</param>
        /// <returns>data is identical</returns>
        private bool MarkupBaselineWithChanges(
            string sourceXmlFile,
            string changedXmlFile,
            bool fragment,
            XmlDiffOptions options)
        {
            // generate the diffgram 
            bool identicalData = this.GenerateDiffGram(
                sourceXmlFile,
                changedXmlFile,
                fragment,
                options);

            this.MergeDiffgramAndBaseline(
                sourceXmlFile,
                fragment);

            return identicalData;
        }

        private int ParseOpId(string value) {
            int opid = 0;
            if (string.IsNullOrEmpty(value)) {
                opid = NextOperationId;
            } else {
                opid = int.Parse(value);
            }
            if (XmlDiffView.nextOperationId <= opid) {
                XmlDiffView.nextOperationId = opid + 1;
            }
            return opid;
        }

        /// <summary>
        /// Adjust the diffgram data for the comparison options. 
        /// </summary>
        /// <param name="diffgramDoc">diffgram data</param>
        private void PreprocessDiffgram(XmlDocument diffgramDoc)
        {
            // read xmldiff options
            XmlAttribute attr = (XmlAttribute)
                diffgramDoc.DocumentElement.Attributes.GetNamedItem("options");
            if (attr == null)
            {
                throw new NullReferenceException(
                    "Missing 'options' attribute in the diffgram.");
            }
            string optionsAttr = attr.Value;
            XmlDiffOptions options = ParseOptions(optionsAttr);
            this.ignoreChildOrder = (((int)options & (int)
                (XmlDiffOptions.IgnoreChildOrder)) > 0);
            this.ignoreComments = (((int)options &
                (int)(XmlDiffOptions.IgnoreComments)) > 0);
            this.ignorePI = (((int)options &
                (int)(XmlDiffOptions.IgnorePI)) > 0);
            this.ignoreWhitespace = (((int)options &
                (int)(XmlDiffOptions.IgnoreWhitespace)) > 0);
            this.ignoreNamespaces = (((int)options &
                (int)(XmlDiffOptions.IgnoreNamespaces)) > 0);
            this.ignorePrefixes = (((int)options &
                (int)(XmlDiffOptions.IgnorePrefixes)) > 0);
            this.ignoreDtd = (((int)options &
                (int)(XmlDiffOptions.IgnoreDtd)) > 0);

            if (this.ignoreNamespaces)
            {
                this.ignorePrefixes = true;
            }

            // read descriptors
            XmlNodeList children =
                diffgramDoc.DocumentElement.ChildNodes;
            IEnumerator e = children.GetEnumerator();
            while (e.MoveNext())
            {
                XmlElement desc = e.Current as XmlElement;
                if (desc != null && desc.LocalName == "descriptor")
                {
                    int opid = ParseOpId(desc.GetAttribute("opid"));
                    OperationDescriptor.Type type;
                    switch (desc.GetAttribute("type"))
                    {
                        case "move":
                            type =
                                OperationDescriptor.Type.Move;
                            break;
                        case "prefix change":
                            type = 
                                OperationDescriptor.Type.PrefixChange;
                            break;
                        case "namespace change":
                            type = OperationDescriptor.Type.NamespaceChange;
                            break;
                        default:
                            throw new ArgumentException(
                                "Invalid descriptor type.");
                    }
                    OperationDescriptor od = new OperationDescriptor(
                        opid,
                        type);

                    // save this change operation in the hashtable.
                    this.descriptors[opid] = od;
                }
            }
        }

        /// <summary>
        /// Recurses through the baseline document loading the
        /// contents to the XmlDiffViewDocument object and tagging
        /// the pieces to be ignored later when the data is output.
        /// </summary>
        /// <param name="parent">Parent node</param>
        /// <param name="reader">The xml data</param>
        /// <param name="emptyElement">Node has no children</param>
        private void LoadSourceChildNodes(
            XmlDiffViewParentNode parent,
            XmlReader reader,
            bool emptyElement)
        {
            LoadState savedLoadState = this.loadState;
            this.loadState.Reset();

            // load attributes
            while (reader.MoveToNextAttribute())
            {
                XmlDiffViewAttribute attr;
                if (reader.Prefix == "xmlns" ||
                    (reader.Prefix == string.Empty && 
                    reader.LocalName == "xmlns"))
                {
                    // create new DiffView attribute
                    attr = new XmlDiffViewAttribute(
                        reader.LocalName,
                        reader.Prefix,
                        reader.NamespaceURI,
                        reader.Value);
                    if (this.ignoreNamespaces)
                    {
                        // set the output operation to be performed  
                        attr.Operation = XmlDiffViewOperation.Ignore;
                    }
                }
                else
                {
                    string attrValue = this.ignoreWhitespace ? NormalizeText(reader.Value) : reader.Value;
                    attr = new XmlDiffViewAttribute(
                        reader.LocalName,
                        reader.Prefix,
                        reader.NamespaceURI,
                        attrValue);
                }
                ((XmlDiffViewElement)parent).InsertAttributeAfter(
                    attr,
                    this.loadState.LastAttribute);
                this.loadState.LastAttribute = attr;
            }

            // empty element -> return, do not load chilren
            if (emptyElement)
            {
                goto End;
            }
            
            // load children
            while (reader.Read())
            {
                // ignore whitespaces between nodes
                if (reader.NodeType == XmlNodeType.Whitespace)
                {
                    continue;
                }
                XmlDiffViewNode child = null;
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        bool emptyElementNode = reader.IsEmptyElement;
                        XmlDiffViewElement elem = new XmlDiffViewElement(
                            reader.LocalName,
                            reader.Prefix,
                            reader.NamespaceURI,
                            this.ignorePrefixes);
                        this.LoadSourceChildNodes(elem, reader, emptyElementNode);
                        child = elem;
                        break;
                    case XmlNodeType.Attribute:
                        string reason = "We should never get to this point, " +
                            "attributes should be read at the beginning of this method.";
                        Debug.Assert(false, reason);
                        break;
                    case XmlNodeType.Text:
                        child = new XmlDiffViewCharData((this.ignoreWhitespace) ? NormalizeText(reader.Value) : reader.Value, XmlNodeType.Text);
                        break;
                    case XmlNodeType.CDATA:
                        child = new XmlDiffViewCharData(reader.Value, XmlNodeType.CDATA);
                        break;
                    case XmlNodeType.EntityReference:
                        Debug.Assert(false, "XmlDiffViewER was thought to be dead code");
                        
                        // child = new XmlDiffViewER(reader.Name);
                        break;
                    case XmlNodeType.Comment:
                        child = new XmlDiffViewCharData(reader.Value, XmlNodeType.Comment);
                        if (this.ignoreComments)
                        {
                            child.Operation = XmlDiffViewOperation.Ignore;
                        }
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        child = new XmlDiffViewPI(reader.Name, reader.Value);
                        if (this.ignorePI)
                        {
                            child.Operation = XmlDiffViewOperation.Ignore;
                        }
                        break;
                    case XmlNodeType.SignificantWhitespace:
                        if (reader.XmlSpace == XmlSpace.Preserve)
                        {
                            child = new XmlDiffViewCharData(reader.Value, XmlNodeType.SignificantWhitespace);
                            if (this.ignoreWhitespace)
                            {
                                child.Operation = XmlDiffViewOperation.Ignore;
                            }
                        }
                        break;
                    case XmlNodeType.XmlDeclaration:
                        child = new XmlDiffViewXmlDeclaration(NormalizeText(reader.Value));
                        if (this.ignoreXmlDecl)
                        {
                            child.Operation = XmlDiffViewOperation.Ignore;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        goto End;

                    case XmlNodeType.DocumentType:
                        child = new XmlDiffViewDocumentType(
                            reader.Name,
                            reader.GetAttribute("PUBLIC"),
                            reader.GetAttribute("SYSTEM"),
                            reader.Value);
                        if (this.ignoreDtd)
                        {
                            child.Operation = XmlDiffViewOperation.Ignore;
                        }
                        break;

                    default:
                        Debug.Assert(false, "Invalid node type");
                        break;
                }
                parent.InsertChildAfter(child, this.loadState.LastChild, true);
                this.loadState.LastChild = child;
            }

        End:
            this.loadState = savedLoadState;
        }

        /// <summary>
        /// Loops through the child nodes of the diffgram and
        /// annotates the nodes with the type of operation, e.g., 
        /// add, change, remove, etc.
        /// </summary>
        /// <param name="diffgramParent">node in diffgram data</param>
        /// <param name="sourceParent">node in baseline data</param>
        private void ApplyDiffgram(
            XmlNode diffgramParent,
            XmlDiffViewParentNode sourceParent)
        {
            sourceParent.CreateSourceNodesIndex();
            XmlDiffViewNode currentPosition = null;

            IEnumerator diffgramChildren = 
                diffgramParent.ChildNodes.GetEnumerator();
            while (diffgramChildren.MoveNext())
            {
                XmlNode diffgramNode = (XmlNode)diffgramChildren.Current;
                if (diffgramNode.NodeType == XmlNodeType.Comment)
                {
                    continue;
                }
                XmlElement diffgramElement = 
                    diffgramChildren.Current as XmlElement;
                if (diffgramElement == null)
                {
                    Trace.WriteLine("Invalid node in diffgram.");
                    throw new InvalidOperationException(
                        "Invalid node in diffgram.");
                }

                if (diffgramElement.NamespaceURI != XmlDiff.NamespaceUri)
                {
                    Trace.WriteLine("Invalid element in diffgram.");
                    throw new InvalidOperationException(
                        "Invalid element in diffgram.");
                }

                string matchAttr = diffgramElement.GetAttribute("match");
                XmlDiffPathNodeList matchNodes = null;
                if (matchAttr != string.Empty)
                {
                    matchNodes = XmlDiffPath.SelectNodes(
                    this.viewDocument,
                    sourceParent,
                    matchAttr);
                }

                switch (diffgramElement.LocalName)
                {
                    case "node":
                        if (matchNodes.Count != 1)
                        {
                            string message = "The 'match' attribute of " +
                                "'node' element must select a single node.";
                            throw new InvalidOperationException(message);
                        }

                        matchNodes.MoveNext();
                        if (diffgramElement.ChildNodes.Count > 0)
                        {
                            this.ApplyDiffgram(
                                diffgramElement,
                                (XmlDiffViewParentNode)matchNodes.Current);
                        }

                        currentPosition = matchNodes.Current;
                        break;
                    case "add":
                        if (matchAttr != string.Empty)
                        {
                            this.OnAddMatch(
                                diffgramElement,
                                matchNodes,
                                sourceParent,
                                ref currentPosition);
                        }
                        else
                        {
                            string typeAttr = diffgramElement.GetAttribute(
                                "type");
                            if (typeAttr != string.Empty)
                            {
                                this.OnAddNode(
                                    diffgramElement,
                                    typeAttr,
                                    sourceParent,
                                    ref currentPosition);
                            }
                            else
                            {
                                this.OnAddFragment(
                                    diffgramElement,
                                    sourceParent,
                                    ref currentPosition);
                            }
                        }
                        break;
                    case "remove":
                        this.OnRemove(
                            diffgramElement,
                            matchNodes,
                            sourceParent,
                            ref currentPosition);
                        break;
                    case "change":
                        this.OnChange(
                            diffgramElement,
                            matchNodes,
                            sourceParent,
                            ref currentPosition);
                        break;
                }
            }
        }

        /// <summary>
        /// Tag the relocated data 
        /// </summary>
        /// <param name="diffgramElement">node in diffgram</param>
        /// <param name="matchNodes">the path to the baseline node</param>
        /// <param name="sourceParent">the baseline parent node</param>
        /// <param name="currentPosition">the resulting node</param>
        private void OnRemove(
            XmlElement diffgramElement,
            XmlDiffPathNodeList matchNodes,
            XmlDiffViewParentNode sourceParent,
            ref XmlDiffViewNode currentPosition)
        {
            // opid & descriptor
            XmlDiffViewOperation operation = XmlDiffViewOperation.Remove;
            int operationId = 0;
            OperationDescriptor operationDesc = null;

            string opidAttr = diffgramElement.GetAttribute("opid");
            if (opidAttr != string.Empty) {
                operationId = int.Parse(opidAttr);
                operationDesc = this.GetDescriptor(operationId);
                if (operationDesc.OperationType == OperationDescriptor.Type.Move) {
                    operation = XmlDiffViewOperation.MoveFrom;
                }
            } else {
                operationId = NextOperationId;
            }

            // subtree
            string subtreeAttr = diffgramElement.GetAttribute("subtree");
            bool subtree = (subtreeAttr != "no");
            if (!subtree)
            {
                if (matchNodes.Count != 1)
                {
                    throw new Exception("The 'match' attribute of 'remove' " +
                        "element must select a single node when the 'subtree' " +
                        "attribute is specified.");
                }

                // annotate node
                matchNodes.MoveNext();
                XmlDiffViewNode node = matchNodes.Current;
                this.AnnotateNode(node, operation, operationId, false);
                if (operationId != 0 && operationDesc != null)
                {
                    operationDesc.NodeList.AddNode(node);
                }
                
                // recurse
                this.ApplyDiffgram(diffgramElement, (XmlDiffViewParentNode)node);
            }
            else
            {
                // annotate nodes
                matchNodes.Reset();
                while (matchNodes.MoveNext())
                {
                    if (operationId != 0 && operationDesc != null)
                    {
                        operationDesc.NodeList.AddNode(matchNodes.Current);
                    }
                    this.AnnotateNode(matchNodes.Current, operation, operationId, true);
                }
            }
        }

        /// <summary>
        /// Relocate matched data. 
        /// </summary>
        /// <param name="diffgramElement">node in diffgram</param>
        /// <param name="matchNodes">the path to the baseline node</param>
        /// <param name="sourceParent">the baseline parent node</param>
        /// <param name="currentPosition">the resulting node</param>
        private void OnAddMatch(
            XmlElement diffgramElement,
            XmlDiffPathNodeList matchNodes,
            XmlDiffViewParentNode sourceParent,
            ref XmlDiffViewNode currentPosition)
        {
            string opidAttr = diffgramElement.GetAttribute("opid");
            if (opidAttr == string.Empty)
            {
                throw new Exception("Missing opid attribute.");
            }
            
            // opid & descriptor
            int opid = ParseOpId(opidAttr);
            OperationDescriptor operationDesc = this.GetDescriptor(opid);

            string subtreeAttr = diffgramElement.GetAttribute("subtree");
            bool subtree = (subtreeAttr != "no");
            
            // move single node without subtree
            if (!subtree)
            {
                if (matchNodes.Count != 1)
                {
                    throw new Exception("The 'match' attribute of 'add' " +
                        "element must select a single node when the 'subtree' " +
                        "attribute is specified.");
                }
                
                // clone node
                matchNodes.MoveNext();
                XmlDiffViewNode newNode = matchNodes.Current.Clone(false);
                this.AnnotateNode(
                    newNode,
                    XmlDiffViewOperation.MoveTo,
                    opid,
                    true);

                operationDesc.NodeList.AddNode(newNode);

                // insert in tree
                sourceParent.InsertChildAfter(newNode, currentPosition, false);
                currentPosition = newNode;

                // recurse
                this.ApplyDiffgram(
                    diffgramElement,
                    (XmlDiffViewParentNode)newNode);
            }
            else
            {
                // move subtree
                matchNodes.Reset();
                while (matchNodes.MoveNext())
                {
                    XmlDiffViewNode newNode = matchNodes.Current.Clone(true);
                    this.AnnotateNode(
                        newNode,
                        XmlDiffViewOperation.MoveTo,
                        opid,
                        true);

                    operationDesc.NodeList.AddNode(newNode);

                    sourceParent.InsertChildAfter(newNode, currentPosition, false);
                    currentPosition = newNode;
                }
            }
        }

        /// <summary>
        /// Add the new node or attribute 
        /// </summary>
        /// <param name="diffgramElement">node in diffgram</param>
        /// <param name="nodeTypeAttr">Whether this is an Attribute</param>
        /// <param name="sourceParent">the baseline parent node</param>
        /// <param name="currentPosition">the resulting node</param>
        private void OnAddNode(
            XmlElement diffgramElement,
            string nodeTypeAttr,
            XmlDiffViewParentNode sourceParent,
            ref XmlDiffViewNode currentPosition)
        {
            XmlNodeType nodeType = (XmlNodeType)
                int.Parse(nodeTypeAttr);
            string name = diffgramElement.GetAttribute("name");
            string prefix = diffgramElement.GetAttribute("prefix");
            string ns = diffgramElement.GetAttribute("ns");
            string opidAttr = diffgramElement.GetAttribute("opid");
            int opid = ParseOpId(opidAttr);

            if (nodeType == XmlNodeType.Attribute)
            {
                Debug.Assert(name != string.Empty);
                XmlDiffViewAttribute newAttr = new XmlDiffViewAttribute(
                    name,
                    prefix,
                    ns,
                    diffgramElement.InnerText);
                newAttr.Operation = XmlDiffViewOperation.Add;
                newAttr.OperationId = opid;
                ((XmlDiffViewElement)
                    sourceParent).InsertAttributeAfter(newAttr, null);
            }
            else
            {
                XmlDiffViewNode newNode = null;

                switch (nodeType)
                {
                    case XmlNodeType.Element:
                        Debug.Assert(name != string.Empty);
                        newNode = new XmlDiffViewElement(
                            name,
                            prefix,
                            ns,
                            this.ignorePrefixes);
                        this.ApplyDiffgram(
                            diffgramElement,
                            (XmlDiffViewParentNode)newNode);
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Comment:
                        Debug.Assert(diffgramElement.InnerText != string.Empty);
                        newNode = new XmlDiffViewCharData(
                            diffgramElement.InnerText,
                            nodeType);
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        Debug.Assert(diffgramElement.InnerText != string.Empty);
                        Debug.Assert(name != string.Empty);
                        newNode = new XmlDiffViewPI(
                            name,
                            diffgramElement.InnerText);
                        break;
                    case XmlNodeType.EntityReference:
                        Debug.Assert(name != string.Empty);
                        Debug.Assert(false, "XmlDiffViewER was thought to be dead code");
                        //// newNode = new XmlDiffViewER(name);
                        break;
                    case XmlNodeType.XmlDeclaration:
                        Debug.Assert(diffgramElement.InnerText != string.Empty);
                        newNode = new XmlDiffViewXmlDeclaration(
                            diffgramElement.InnerText);
                        break;
                    case XmlNodeType.DocumentType:
                        newNode = new XmlDiffViewDocumentType(
                            diffgramElement.GetAttribute("name"),
                            diffgramElement.GetAttribute("publicId"),
                            diffgramElement.GetAttribute("systemId"),
                            diffgramElement.InnerText);
                        break;
                    default:
                        Debug.Assert(false, "Invalid node type.");
                        break;
                }
                Debug.Assert(newNode != null);
                newNode.Operation = XmlDiffViewOperation.Add;
                newNode.OperationId = opid;
                sourceParent.InsertChildAfter(newNode, currentPosition, false);
                currentPosition = newNode;
            }
        }

        /// <summary>
        /// Add the new fragment 
        /// </summary>
        /// <param name="diffgramElement">node in diffgram</param>
        /// <param name="sourceParent">the baseline parent node</param>
        /// <param name="currentPosition">the resulting node</param>
        private void OnAddFragment(
            XmlElement diffgramElement,
            XmlDiffViewParentNode sourceParent,
            ref XmlDiffViewNode currentPosition)
        {
            int opid = NextOperationId;
            IEnumerator childNodes = 
                diffgramElement.ChildNodes.GetEnumerator();
            while (childNodes.MoveNext())
            {
                XmlDiffViewNode newChildNode = this.ImportNode(
                    (XmlNode)childNodes.Current);
                sourceParent.InsertChildAfter(
                    newChildNode,
                    currentPosition,
                    false);
                currentPosition = newChildNode;

                this.AnnotateNode(
                    newChildNode,
                    XmlDiffViewOperation.Add,
                    opid,
                    true);
            }
        }

        /// <summary>
        /// Generate a new node
        /// </summary>
        /// <param name="node">node to clone</param>
        /// <returns>the new node</returns>
        private XmlDiffViewNode ImportNode(XmlNode node)
        {
            XmlDiffViewNode newNode = null;
            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    XmlElement el = (XmlElement)node;
                    XmlDiffViewElement newElement = new XmlDiffViewElement(
                        el.LocalName,
                        el.Prefix,
                        el.NamespaceURI,
                        this.ignorePrefixes);
                    
                    // attributes
                    IEnumerator attributes = node.Attributes.GetEnumerator();
                    XmlDiffViewAttribute lastNewAttr = null;
                    while (attributes.MoveNext())
                    {
                        XmlAttribute at = (XmlAttribute)attributes.Current;
                        XmlDiffViewAttribute newAttr = new XmlDiffViewAttribute(
                            at.LocalName,
                            at.Prefix,
                            at.NamespaceURI,
                            at.Value);
                        newElement.InsertAttributeAfter(newAttr, lastNewAttr);
                        lastNewAttr = newAttr;
                    }

                    // children
                    IEnumerator childNodes = node.ChildNodes.GetEnumerator();
                    XmlDiffViewNode lastNewChildNode = null;
                    while (childNodes.MoveNext())
                    {
                        XmlDiffViewNode newChildNode = this.ImportNode(
                            (XmlNode)childNodes.Current);
                        newElement.InsertChildAfter(
                            newChildNode,
                            lastNewChildNode,
                            false);
                        lastNewChildNode = newChildNode;
                    }
                    newNode = newElement;
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.Comment:
                    newNode = new XmlDiffViewCharData(
                        node.Value,
                        node.NodeType);
                    break;
                case XmlNodeType.ProcessingInstruction:
                    newNode = new XmlDiffViewPI(node.Name, node.Value);
                    break;
                case XmlNodeType.EntityReference:
                    Debug.Assert(false, "XmlDiffViewER was thought to be dead code");
                    //// newNode = new XmlDiffViewER(node.Name);
                    break;
                default:
                    Debug.Assert(false, "Invalid node type.");
                    break;
            }
            Debug.Assert(newNode != null);
            return newNode;
        }

        /// <summary>
        /// Store changes in the ChangeInfo object of the marked-up-baseline node
        /// </summary>
        /// <param name="diffgramElement">current element in the diffgram</param>
        /// <param name="matchNodes">Object containing the list of baseline nodes
        ///  which match the position in the diffgram</param>
        /// <param name="sourceParent">parent node in the baseline data</param>
        /// <param name="currentPosition">current position</param>
        private void OnChange(
            XmlElement diffgramElement,
            XmlDiffPathNodeList matchNodes,
            XmlDiffViewParentNode sourceParent,
            ref XmlDiffViewNode currentPosition)
        {
            Debug.Assert(matchNodes.Count == 1);
            matchNodes.Reset();
            matchNodes.MoveNext();
            XmlDiffViewNode node = matchNodes.Current;

            if (node.NodeType != XmlNodeType.Attribute)
            {
                currentPosition = node;
            }
            XmlDiffViewNode.ChangeInfo changeInfo = new XmlDiffViewNode.ChangeInfo();
            string name = diffgramElement.HasAttribute("name") ? diffgramElement.GetAttribute("name") : null;
            string prefix = diffgramElement.HasAttribute("prefix") ? diffgramElement.GetAttribute("prefix") : null;
            string ns = diffgramElement.HasAttribute("ns") ? diffgramElement.GetAttribute("ns") : null;

            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    changeInfo.LocalName = (name == null) ? ((XmlDiffViewElement)node).LocalName : name;
                    changeInfo.Prefix = (prefix == null) ? ((XmlDiffViewElement)node).Prefix : prefix;
                    changeInfo.NamespaceUri = (ns == null) ? ((XmlDiffViewElement)node).NamespaceUri : ns;
                    break;
                case XmlNodeType.Attribute:
                    string value = diffgramElement.InnerText;
                    if (name == string.Empty && prefix == string.Empty && value == string.Empty)
                    {
                        return;
                    }
                    changeInfo.LocalName = (name == null) ? ((XmlDiffViewAttribute)node).LocalName : name;
                    changeInfo.Prefix = (prefix == null) ? ((XmlDiffViewAttribute)node).Prefix : prefix;
                    changeInfo.NamespaceUri = (ns == null) ? ((XmlDiffViewAttribute)node).NamespaceUri : ns;
                    changeInfo.Subset = diffgramElement.InnerText;
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    Debug.Assert(diffgramElement.FirstChild != null);
                    changeInfo.Subset = diffgramElement.InnerText;
                    break;
                case XmlNodeType.Comment:
                    Debug.Assert(diffgramElement.FirstChild != null);
                    Debug.Assert(diffgramElement.FirstChild.NodeType == XmlNodeType.Comment);
                    changeInfo.Subset = diffgramElement.FirstChild.Value;
                    break;
                case XmlNodeType.ProcessingInstruction:
                    if (name == null)
                    {
                        Debug.Assert(diffgramElement.FirstChild != null);
                        Debug.Assert(diffgramElement.FirstChild.NodeType == XmlNodeType.ProcessingInstruction);
                        changeInfo.LocalName = diffgramElement.FirstChild.Name;
                        changeInfo.Subset = diffgramElement.FirstChild.Value;
                    }
                    else
                    {
                        changeInfo.LocalName = name;
                        changeInfo.Subset = ((XmlDiffViewPI)node).InnerText;
                    }
                    break;
                case XmlNodeType.EntityReference:
                    Debug.Assert(name != null);
                    changeInfo.LocalName = name;
                    break;
                case XmlNodeType.XmlDeclaration:
                    Debug.Assert(diffgramElement.FirstChild != null);
                    changeInfo.Subset = diffgramElement.InnerText;
                    break;
                case XmlNodeType.DocumentType:
                    changeInfo.LocalName = (name == null) ? ((XmlDiffViewDocumentType)node).Name : name;

                    if (diffgramElement.HasAttribute("publicId"))
                    {
                        changeInfo.Prefix = diffgramElement.GetAttribute("publicId");
                    }
                    else
                    {
                        changeInfo.Prefix = ((XmlDiffViewDocumentType)node).PublicId;
                    }

                    if (diffgramElement.HasAttribute("systemId"))
                    {
                        changeInfo.NamespaceUri = diffgramElement.GetAttribute("systemId");
                    }
                    else
                    {
                        changeInfo.NamespaceUri = ((XmlDiffViewDocumentType)node).SystemId;
                    }

                    if (diffgramElement.FirstChild != null)
                    {
                        changeInfo.Subset = diffgramElement.InnerText;
                    }
                    else
                    {
                        changeInfo.Subset = ((XmlDiffViewDocumentType)node).Subset;
                    }
                    break;
                default:
                    Debug.Assert(false, "Invalid node type.");
                    break;
            }
            node.ChangeInformation = changeInfo;
            node.Operation = XmlDiffViewOperation.Change;

            string opidAttr = diffgramElement.GetAttribute("opid");
            if (opidAttr != string.Empty) {
                node.OperationId = int.Parse(opidAttr);
            } else {
                node.OperationId = NextOperationId;
            }

            if (node.NodeType == XmlNodeType.Element &&
                diffgramElement.FirstChild != null)
            {
                this.ApplyDiffgram(diffgramElement, (XmlDiffViewParentNode)node);
            }
        }

        /// <summary>
        /// Gets a reference to the operation description object
        /// for the operation identification number provided.
        /// </summary>
        /// <param name="opid">operation identification number</param>
        /// <returns>A reference to the operation description object</returns>
        private OperationDescriptor GetDescriptor(int opid)
        {
            OperationDescriptor operationDesc = (OperationDescriptor)this.descriptors[opid];
            if (operationDesc == null)
            {
                throw new Exception("Invalid operation id.");
            }
            return operationDesc;
        }

        /// <summary>
        /// Mark the nodes (and attributes) with the type of data change
        /// </summary>
        /// <param name="node">the node to annotate</param>
        /// <param name="op">the type of data change</param>
        /// <param name="opid">the operation identification number</param>
        /// <param name="subtree">the node's subtree</param>
        private void AnnotateNode(
            XmlDiffViewNode node,
            XmlDiffViewOperation op,
            int opid,
            bool subtree)
        {
            node.Operation = op;
            node.OperationId = opid;

            if (node.NodeType == XmlNodeType.Element)
            {
                XmlDiffViewAttribute attr = (
                    (XmlDiffViewElement)node).Attributes;
                while (attr != null)
                {
                    attr.Operation = op;
                    attr.OperationId = opid;
                    attr = (XmlDiffViewAttribute)attr.NextSibbling;
                }
            }

            if (subtree)
            {
                XmlDiffViewNode childNode = node.FirstChildNode;
                while (childNode != null)
                {
                    this.AnnotateNode(childNode, op, opid, true);
                    childNode = childNode.NextSibbling;
                }
            }
        }

        /// <summary>
        /// initialise the output with differences node and
        /// start the process of formatting the output
        /// </summary>
        /// <param name="baselineFile">baseline file name</param>
        /// <param name="actualFile">actual file name</param>
        private void GetText(
            string baselineFile,
            string actualFile)
        {
            // initialise output with differences node
            this.outputData.Write(Tags.XmlOpenBegin +
                Difference.Tag + Difference.NodeDifferences +
                " fromFile='" + baselineFile + "' toFile='" +
                actualFile + "'" + Tags.XmlOpenEnd + this.outputData.NewLine);
            
            // flag the output object is open for cleanup later.
            this.viewDocument.DrawText(this.outputData, Indent.InitialSize);
            
            // end differences node
            this.outputData.Write(Tags.XmlCloseBegin +
                Difference.Tag + Difference.NodeDifferences +
                Tags.XmlCloseEnd + this.outputData.NewLine);
        }

        /// <summary>
        /// Compare the xml data files
        /// </summary>
        /// <param name="sourceXmlFile">baseline file</param>
        /// <param name="changedXmlFile">actual file</param>
        /// <param name="fragment">xml data fragment</param>
        /// <param name="options">comparison options</param>
        /// <returns>data is identical</returns>
        private bool GenerateDiffGram(
            string sourceXmlFile,
            string changedXmlFile,
            bool fragment,
            XmlDiffOptions options)
        {
            // set class scope variables
            // MemoryStream diffgram
            bool identicalData;
            this.diffgram = new MemoryStream();
            XmlTextWriter diffgramWriter = new XmlTextWriter(
                new StreamWriter(this.diffgram));

            Trace.WriteLine("Comparing " + sourceXmlFile +
                " & " + changedXmlFile);
            XmlDiffOptions xmlDiffOptions = (XmlDiffOptions)options;
            XmlDiff xmlDiff = new XmlDiff(xmlDiffOptions);

            try
            {
                identicalData = xmlDiff.Compare(
                    sourceXmlFile,
                    changedXmlFile,
                    fragment,
                    diffgramWriter);
            }
            catch (XmlException format)
            {
                Trace.WriteLine(format.Message);
                throw;
            }
            Trace.WriteLine("Files compared " +
                (identicalData ? "identical." : "different."));
            
            return identicalData;
        }

        /// <summary>
        /// The generic html header.
        /// </summary>
        /// <param name="sourceXmlFile">baseline xml data</param>
        /// <param name="changedXmlFile">xml data to which to compare</param>
        /// <param name="identicalData">Data is identical</param>
        /// <param name="resultHtml">Output file</param>
        public void SideBySideHtmlHeader(
            string sourceXmlFile,
            string changedXmlFile,
            bool identicalData,
            TextWriter resultHtml)
        {
            // this initializes the html
            resultHtml.WriteLine("<html><head>");
            resultHtml.WriteLine(@"<html><head>
                <style TYPE='text/css' MEDIA='screen'>
                <!-- td { font-family: Courier New; font-size:14; } 
                th { font-family: Arial; } 
                p { font-family: Arial; } 
                .match { }
                .ignore { color:#AAAAAA; }
                .add { background-color:yellow; }
                .moveto { background-color:cyan; color:navy; }
                .remove { background-color:red; }
                .movefrom {  background-color:cyan; color:navy; }
                .change {  background-color:lightgreen;  }
                -->
            </style></head>
            <body>
                <table border='0' style='table-layout:fixed;' width='100%'>
                    <col width='20'><col width='50%'><col width='50%'>
                    <tr><td><table border='0' width='100%'>
                    <tr><td colspan='3' align='center'>
                    <b>Legend:</b> <span class='add'>added</span>&nbsp;&nbsp;
                        <span class='remove'>removed</span>&nbsp;&nbsp;
                        <span class='change'>changed</span>&nbsp;&nbsp;
                        <span class='movefrom'>moved from</span>&nbsp;&nbsp;
                        <span class='moveto'>moved to</span>&nbsp;&nbsp;
                        <span class='ignore'>ignored</span><br/><br/>
                    </td></tr>");
            resultHtml.WriteLine("<tr><td><table border='0'>");
            resultHtml.WriteLine("<tr><th>" + sourceXmlFile + "</th><th>" +
                changedXmlFile + "</th></tr>" +
                "<tr><td colspan='3'><hr size=1></td></tr>");
            if (identicalData)
            {
                resultHtml.WriteLine("<tr><td colspan='3' align='middle'>Files are identical.</td></tr>");
            }
            else
            {
                resultHtml.WriteLine("<tr><td colspan='3' align='middle'>" +
                    "Files are different.</td></tr>");
            }
        }

        /// <summary>
        /// Merge the diffgram and the baseline file 
        /// into a new XmlDiffViewDocument object.
        /// </summary>
        /// <param name="sourceXmlFile">the baseline file</param>
        /// <param name="fragment">the file is an Xml fragment</param>
        private void MergeDiffgramAndBaseline(
            string sourceXmlFile,
            bool fragment)
        {
            Debug.Assert(null != this.diffgram);

            // Populate an xml reader with the baseline data.
            this.diffgram.Seek(0, SeekOrigin.Begin);
            XmlTextReader sourceReader;
            if (fragment)
            {
                NameTable nt = new NameTable();
                ////Todo: break up the following overly
                ////      complex statement to avoid StyleCop
                ////      complaints.
                sourceReader = new XmlTextReader(
                    new FileStream(
                        sourceXmlFile,
                        FileMode.Open,
                        FileAccess.Read),
                    XmlNodeType.Element,
                    new XmlParserContext(
                        nt,
                        new XmlNamespaceManager(nt),
                        string.Empty,
                        XmlSpace.Default));
            }
            else
            {
                sourceReader = new XmlTextReader(sourceXmlFile);
            }

            sourceReader.XmlResolver = null;
            this.Load(
                sourceReader,
                new XmlTextReader(this.diffgram));
        }

        /// <summary>
        /// html footer
        /// </summary>
        /// <param name="resultHtml">output stream</param>
        private void SideBySideHtmlFooter(TextWriter resultHtml)
        {
            resultHtml.WriteLine("</table></table></body></html>");
        }

        #endregion

        #endregion

        #region Structs section

        /// <summary>
        /// An object used when loading to provide 
        /// references to the last node and attribute.
        /// </summary>
        private struct LoadState
        {
            #region Member variables section

            /// <summary>
            /// Declares a reference to the last child node processed
            /// </summary>
            private XmlDiffViewNode lastChild;
            
            /// <summary>
            /// Declares a reference to the last attribute processed
            /// </summary>
            private XmlDiffViewAttribute lastAttribute;

            #endregion

            #region Properties section

            /// <summary>
            /// Gets or sets a reference to the last child node processed
            /// </summary>
            public XmlDiffViewNode LastChild
            {
                get
                {
                    return this.lastChild;
                }
                
                set
                {
                    this.lastChild = value;
                }
            }

            /// <summary>
            /// Gets or sets a reference to the last attribute processed
            /// </summary>
            public XmlDiffViewAttribute LastAttribute
            {
                get
                {
                    return this.lastAttribute;
                }

                set
                {
                    this.lastAttribute = value;
                }
            }
            #endregion

            #region Methods section

            /// <summary>
            /// Clear the references the last 
            /// child node and attribute processed.
            /// </summary>
            public void Reset()
            {
                this.lastChild = null;
                this.lastAttribute = null;
            }

            #endregion
        }
        #endregion
    }
}