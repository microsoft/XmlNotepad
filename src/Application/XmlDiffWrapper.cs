using Microsoft.XmlDiffPatch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using SR = XmlNotepad.StringResources;

namespace XmlNotepad
{
    internal class XmlDiffWrapper
    {
        private string localStyles;
        private readonly System.CodeDom.Compiler.TempFileCollection _tempFiles = new System.CodeDom.Compiler.TempFileCollection();

        public XmlDiffWrapper() 
        { 

        }

        public string GetOrCreateLocalStyles()
        {
            if (!string.IsNullOrEmpty(localStyles) && File.Exists(localStyles) && !string.IsNullOrWhiteSpace(File.ReadAllText(localStyles)))
            {
                return localStyles;
            }
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(local, "Microsoft\\Xml Notepad");
            try
            {
                System.IO.Directory.CreateDirectory(path);
            }
            catch (Exception)
            {
                // hmmm, now what?
                path = System.IO.Path.GetTempPath();
            }

            localStyles = Path.Combine(path, "XmlDiffStyles.css");
            if (File.Exists(localStyles) && !string.IsNullOrWhiteSpace(File.ReadAllText(localStyles)))
            {
                return localStyles;
            }

            File.WriteAllText(localStyles, GetEmbeddedString("XmlNotepad.Resources.XmlDiffStyles.css"));
            return localStyles;
        }

        private string LoadStyleSheet()
        {
            var path = this.GetOrCreateLocalStyles();
            return File.ReadAllText(path);
        }

        public void DoCompare(Form owner, XmlCache model, string otherXmlFile, XmlDiffOptions options, bool omitIdentical)
        {
            CleanupTempFiles();

            // todo: add UI for setting XmlDiffOptions.

            string filename = model.FileName;

            // load file from disk, as saved doc can be slightly different
            // (e.g. it might now have an XML declaration).  This ensures we
            // are diffing the exact same doc as we see loaded below on the
            // diffView.Load call.
            XmlDocument original = new XmlDocument();
            XmlReaderSettings settings = model.GetReaderSettings();
            using (XmlReader reader = XmlReader.Create(filename, settings))
            {
                original.Load(reader);
            }

            XmlDocument doc = new XmlDocument();
            settings = model.GetReaderSettings();
            using (XmlReader reader = XmlReader.Create(otherXmlFile, settings))
            {
                doc.Load(reader);
            }

            //output diff file.
            string diffFile = Path.Combine(Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".xml");
            this._tempFiles.AddFile(diffFile, false);

            bool isEqual = false;
            XmlTextWriter diffWriter = new XmlTextWriter(diffFile, Encoding.UTF8);
            diffWriter.Formatting = Formatting.Indented;
            using (diffWriter)
            {
                XmlDiff diff = new XmlDiff(options);
                isEqual = diff.Compare(original, doc, diffWriter);
            }

            if (isEqual)
            {
                //This means the files were identical for given options.
                MessageBox.Show(owner, SR.FilesAreIdenticalPrompt, SR.FilesAreIdenticalCaption,
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            string tempFile = Path.Combine(Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".htm");
            _tempFiles.AddFile(tempFile, false);

            using (XmlReader diffGram = XmlReader.Create(diffFile, settings))
            {
                XmlDiffView diffView = new XmlDiffView();
                using (var reader = new XmlTextReader(filename))
                {
                    diffView.Load(reader, diffGram);
                    using (TextWriter htmlWriter = new StreamWriter(tempFile, false, Encoding.UTF8))
                    {
                        SideBySideXmlNotepadHeader(model.FileName, otherXmlFile, htmlWriter);
                        diffView.GetHtml(htmlWriter, omitIdentical);
                        htmlWriter.WriteLine("</body></html>");
                    }
                }
            }

            WebBrowser.OpenUrl(owner.Handle, tempFile);
        }

        internal void CleanupTempFiles()
        {
            try
            {
                this._tempFiles.Delete();
            }
            catch
            {
            }
        }

        /// <summary>
        /// The html header used by XmlNotepad.
        /// </summary>
        /// <param name="sourceXmlFile">name of baseline xml data</param>
        /// <param name="changedXmlFile">name of file being compared</param>
        /// <param name="resultHtml">Output file</param>
        public void SideBySideXmlNotepadHeader(
            string sourceXmlFile,
            string changedXmlFile,
            TextWriter resultHtml)
        {

            // this initializes the html
            resultHtml.WriteLine("<html><head>");
            resultHtml.WriteLine("<style TYPE='text/css'>");
            resultHtml.WriteLine(LoadStyleSheet());
            resultHtml.WriteLine("</style>");
            resultHtml.WriteLine("</head>");
            resultHtml.WriteLine(GetEmbeddedString("XmlNotepad.Resources.XmlDiffHeader.html"));

            resultHtml.WriteLine(string.Format(SR.XmlDiffBody,
                    System.IO.Path.GetDirectoryName(sourceXmlFile),
                    sourceXmlFile,
                    System.IO.Path.GetDirectoryName(changedXmlFile),
                    changedXmlFile
            ));
        }

        string GetEmbeddedString(string name)
        {
            using (Stream stream = typeof(XmlNotepad.FormMain).Assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                {
                    throw new Exception(string.Format("You have a build problem: resource '{0} not found", name));
                }
                using (StreamReader sr = new StreamReader(stream))
                {
                    return sr.ReadToEnd();
                }
            }
        }

    }
}
