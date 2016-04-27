//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffViewResults.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Provides access the results.
// </summary>
// <history>
//      [barryw] 31MAR05 Created
// </history>
//  ---------------------------------------------------------------------------

namespace Microsoft.XmlDiffPatch
{
    #region Using directives

    using System;
    using System.IO;

    #endregion

    #region Results Class

    /// -----------------------------------------------------------------------------
    /// <summary>
    /// Class to provide access the results.
    /// </summary>
    /// <history>
    /// 	[barryw] 31MAR05 Created again.
    /// </history>
    /// -----------------------------------------------------------------------------
    public class XmlDiffViewResults
    {
        #region  Member variables section

        /// <summary>
        /// Holds the comparison data locally.
        /// </summary>
        private TextReader reader = null;
        /// <summary>
        /// Holds the value which indicates the 
        /// baseline and actual data are identical.
        /// </summary>
        private bool identicalData = false;

        #endregion

        #region  Constructors section

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data">detailed results of the comparison</param>
        /// <param name="identical">summary result if the 
        /// comparison, value is 'true' if there were no 
        /// differences found.</param>
        internal XmlDiffViewResults(
            MemoryStream data,
            bool identical)
        {
            this.reader = this.PopulateReader(ref data);
            this.identicalData = identical;
        }

        #endregion

        #region  Properties section

        /// <summary>
        /// Gets the value which indicates whether differences were found.
        /// </summary>
        /// <value>the xml data are identical.</value>
        public bool Identical
        {
            get
            {
                return this.identicalData;
            }
        }
        
        #endregion

        #region  Methods section

        /// <summary>Reads the next character without changing the state of the reader or the character source. Returns the next available character without actually reading it from the input stream.</summary>
        /// <returns>The next character to be read, or -1 if no more characters are available or the stream does not support seeking.</returns>
        public int Peek()
        {
            return this.reader.Peek();
        }

        /// <summary>Reads a maximum of count characters from the current
        /// stream and writes the data to buffer, beginning at index.
        /// </summary>
        /// <returns>The number of characters that have been read. The number
        /// will be less than or equal to count, depending on whether the 
        /// data is available within the stream. This method returns zero if 
        /// called when no more characters are left to read.</returns>
        /// <param name="count">The maximum number of characters to read. If 
        /// the end of the stream is reached before count of characters is 
        /// read into buffer, the current method returns. </param>
        /// <param name="buffer">When this method returns, contains the 
        /// specified character array with the values between index and 
        /// (index + count - 1) replaced by the characters read from the 
        /// current source. </param>
        /// <param name="index">The place in buffer at which to begin writing.
        /// </param>
        public int Read(char[] buffer, int index, int count)
        {
            return this.reader.Read(buffer, index, count);
        }

        /// <summary>Reads the next character from the input stream and 
        /// advances the character position by one character.</summary>
        /// <returns>The next character from the input stream, or -1 if no 
        /// more characters are available. The default implementation returns
        /// -1.</returns>
        public int Read()
        {
            return this.reader.Read();
        }

        /// <summary>Reads a maximum of count characters from the current 
        /// stream and writes the data to buffer, beginning at index.</summary>
        /// <returns>The number of characters that have been read. The number 
        /// will be less than or equal to count, depending on whether all 
        /// input characters have been read.</returns>
        /// <param name="count">The maximum number of characters to read. 
        /// </param>
        /// <param name="buffer">When this method returns, this parameter 
        /// contains the specified character array with the values between 
        /// index and (index + count -1) replaced by the characters read from 
        /// the current source. </param>
        /// <param name="index">The place in buffer at which to begin writing.
        /// </param>
        public int ReadBlock(char[] buffer, int index, int count)
        {
            return this.reader.ReadBlock(buffer, index, count);
        }

        /// <summary>Reads a line of characters from the current stream and 
        /// returns the data as a string.</summary>
        /// <returns>The next line from the input stream, or null if all 
        /// characters have been read.</returns>
        public string ReadLine()
        {
            return this.reader.ReadLine();
        }

        /// <summary>Reads all characters from the current position to the 
        /// end of the TextReader and returns them as one string.</summary>
        /// <returns>A string containing all characters from the current 
        /// position to the end of the TextReader.</returns>
        public string ReadToEnd()
        {
            return this.reader.ReadToEnd();
        }

        /// <summary>
        /// Returns the underlying populated basestream of a 
        /// TextWriter objectdata as a TextReader object 
        /// re-positioned to the beginning of the data stream.
        /// </summary>
        /// <param name="data">reference to the data in memory</param>
        /// <returns>the Textreader object</returns>
        private TextReader PopulateReader(ref MemoryStream data)
        {
            StreamReader sr = null;
            sr = new StreamReader(
                data,
                System.Text.Encoding.Unicode);
            // Set the StreamReader file pointer to the beginning.
            sr.BaseStream.Seek(0, SeekOrigin.Begin);
            return sr;
        }
        #endregion
    }
    #endregion
}
