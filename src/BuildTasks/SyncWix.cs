using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Diagnostics;

namespace XmlNotepadBuildTasks
{
    public class SyncWix : Task
    {
        [Required]
        public string VersionFile { get; set; }

        [Required]
        public string WixFile { get; set; }

        [Required]
        public string DropDir { get; set; }

        [Required]
        public string UpdatesFile { get; set; }

        [Required]
        public string ReadmeFile { get; set; }

        public override bool Execute()
        {
            if (!System.IO.File.Exists(this.VersionFile))
            {
                Log.LogError("Cannot find version file: " + this.VersionFile);
                return false;
            }
            using (var reader = new StreamReader(this.VersionFile))
            {
                string line = null;
                string version = null;
                do
                {
                    line = reader.ReadLine();
                    if (line == null) break;
                    //[assembly: AssemblyVersion("2.8.0.1")]
                    if (line.Contains("AssemblyVersion"))
                    {
                        int i = line.IndexOf('"');
                        if (i > 0)
                        {
                            i++;
                            int j = line.IndexOf('"', i);
                            version = line.Substring(i, j - i);
                            break;
                        }
                    }
                } while (line != null);

                Version v;
                if (string.IsNullOrEmpty(version) || !Version.TryParse(version, out v))
                {
                    Log.LogError("Could not find valid quoted version number in : " + this.VersionFile);
                    return false;
                }

                bool result = UpdateWixDoc(v);
                result &= CheckUpdatesFile(v);
                result &= UpdateReadmeFile(v);
                return result;
            }
        }

        private bool UpdateWixDoc(Version v)
        {
            if (!System.IO.File.Exists(this.WixFile))
            {
                Log.LogError("WIX file not found: " + this.WixFile);
                return false;
            }

            try
            {
                XDocument doc = XDocument.Load(this.WixFile);
                bool result = UpdateWixVersion(doc, v);
                result &= UpdateDropFiles(doc, v);
                result &= UpdateFeature(doc);
                if (result) 
                {
                    doc.Save(this.WixFile);
                }
            }
            catch (Exception ex)
            {
                Log.LogError("WIX file edit failed: " + ex.Message);
                return false;
            }
            return true;
        }

        private bool UpdateFeature(XDocument wixdoc)
        {
            XNamespace ns = wixdoc.Root.Name.Namespace;
            XElement product = wixdoc.Root.Element(ns + "Product");
            XElement feature = product.Element(ns + "Feature");

            List<string> components = new List<string>();
            foreach (var dirref in product.Elements(ns + "DirectoryRef"))
            {
                foreach (var comp in dirref.Elements(ns + "Component"))
                {
                    components.Add((string)comp.Attribute("Id"));
                }
            }

            List<string> existing = new List<string>();
            foreach(var cref in feature.Elements(ns + "ComponentRef"))
            {
                existing.Add((string)cref.Attribute("Id"));
            }

            HashSet<string> hashFound = new HashSet<string>(components);
            HashSet<string> hashExisting = new HashSet<string>(existing);
            if (!hashFound.SetEquals(hashExisting))
            {
                Debug.WriteLine("Rewriting component references...");
                foreach(var child in feature.Nodes().ToArray())
                {
                    child.Remove();
                }

                foreach(var id in components)
                {
                    feature.Add(new XElement(ns + "ComponentRef",
                        new XAttribute("Id", id)));
                }
            }

            return true;
        }

        private bool UpdateWixVersion(XDocument wixdoc, Version v)
        {
            XNamespace ns = wixdoc.Root.Name.Namespace;
            XElement product = wixdoc.Root.Element(ns + "Product");
            if (v.ToString() != (string)product.Attribute("Version"))
            {
                product.SetAttributeValue("Version", v.ToString());                
                Log.LogMessage(MessageImportance.High, "Updated version number in : " + this.WixFile + " to match Version.cs version " + v.ToString());
            }
            return true;
        }

        private bool UpdateDropFiles(XDocument wixdoc, Version v)
        {
            if (!System.IO.Directory.Exists(this.DropDir))
            {
                Log.LogError("Drop dir does not exist: " + this.DropDir);
                return false;
            }

            bool result = true;
            try
            {
                XNamespace ns = wixdoc.Root.Name.Namespace;
                XElement product = wixdoc.Root.Element(ns + "Product");

                foreach (string subdir in new string[] { "Help", "samples"})
                {
                    GetOrCreateDirRef(wixdoc, Path.Combine(this.DropDir, subdir));
                }
            }
            catch (Exception ex)
            {
                Log.LogError("UpdateDropFiles failed: " + ex.Message);
                result = false;
            }

            return result;
        }

        private bool UpdateDropDirectory(XDocument wixdoc, XElement parent, string dir)
        {
            if (!System.IO.Directory.Exists(dir))
            {
                Log.LogError("Drop dir does not exist: " + dir);
                return false;
            }

            bool result = true;
            try
            {
                // add DirectoryRef element for dir.
                GetOrCreateFiles(parent, dir);

                GetOrCreateDirs(parent, dir);

                // traverse down.
                foreach (string dirname in Directory.GetDirectories(dir))
                {
                    // get or create the DirectoryRef
                    GetOrCreateDirRef(wixdoc, dirname);
                }
            }
            catch (Exception ex)
            {
                Log.LogError("UpdateDropFiles failed: " + ex.Message);
                result = false;
            }

            return result;
        }

        private void GetOrCreateDirRef(XDocument wixdoc, string dirname)
        {
            string refid = Path.GetFileName(dirname);
            XNamespace ns = wixdoc.Root.Name.Namespace;
            XElement product = wixdoc.Root.Element(ns + "Product");
            XElement theDir = (from e in product.Elements(ns + "DirectoryRef") where refid == (string)e.Attribute("Id") select e).FirstOrDefault();
            if (theDir == null)
            {
                var relpath = new Uri(this.DropDir + "/").MakeRelative(new Uri(dirname));
                Debug.WriteLine("Adding new directory: " + dirname);
                theDir = new XElement(ns + "DirectoryRef",
                    new XAttribute("Id", refid),
                    new XAttribute("FileSource", @"$(var.SolutionDir)\drop\" + relpath.Replace("/", "\\")));
                product.Add(theDir);
            }

            UpdateDropDirectory(wixdoc, theDir, dirname);
        }

        private void GetOrCreateDirs(XElement parent, string dir)
        {
            XNamespace ns = parent.Name.Namespace;
            Dictionary<string, XElement> existing = new Dictionary<string, XElement>();
            foreach (var c in parent.Elements(ns + "Directory").ToArray())
            {
                string id = (string)c.Attribute("Id");
                existing[id] = c;
            }

            HashSet<string> found = new HashSet<string>();
            foreach (string dirname in Directory.GetDirectories(dir))
            {
                // add this directory
                string filename = Path.GetFileName(dirname);
                found.Add(filename);
                if (!existing.ContainsKey(filename))
                {
                    Debug.WriteLine("Adding new directory: " + dirname);
                    parent.Add(new XElement(ns + "Directory",
                    new XAttribute("Id", filename),
                    new XAttribute("Name", filename)));
                }
            }

            // remove deleted directories
            foreach (var pair in existing)
            {
                if (!found.Contains(pair.Key))
                {
                    Debug.WriteLine("Removing old directory: " + pair.Key);
                    pair.Value.Remove();
                }
            }
        }

        private void GetOrCreateFiles(XElement parent, string dir)
        {
            XNamespace ns = parent.Name.Namespace;
            Dictionary<string, XElement> existing = new Dictionary<string, XElement>();
            foreach (var c in parent.Elements(ns + "Component").ToArray())
            {
                string id = (string)c.Attribute("Id");
                existing[id] = c;
            }

            // add new files
            HashSet<string> found = new HashSet<string>();
            foreach (string fullpath in Directory.GetFiles(dir))
            {
                // add Component for this file
                string filename = Path.GetFileName(fullpath);
                found.Add(filename);
                if (!existing.ContainsKey(filename))
                {
                    Debug.WriteLine("Adding new file: " + fullpath);
                    parent.Add(new XElement(ns + "Component",
                        new XAttribute("Id", filename),
                        new XAttribute("Guid", Guid.NewGuid().ToString()),
                        new XElement(ns + "File",
                            new XAttribute("Id", filename),
                            new XAttribute("KeyPath", "yes"))));
                }
            }

            // remove deleted files
            foreach(var pair in existing)
            {
                if (!found.Contains(pair.Key))
                {
                    Debug.WriteLine("Removing old file: " + pair.Key);
                    pair.Value.Remove();
                }
            }
        }

        private bool CheckUpdatesFile(Version v)
        {
            if (!System.IO.File.Exists(this.UpdatesFile))
            {
                Log.LogError("Updates.xml file not found: " + this.UpdatesFile);
                return false;
            }

            try
            {
                XDocument doc = XDocument.Load(this.UpdatesFile);
                XNamespace ns = doc.Root.Name.Namespace;
                XElement firstVersion = doc.Root.Element("version");
                if (v.ToString() != (string)firstVersion.Attribute("number"))
                {
                    Log.LogMessage(MessageImportance.High, "Please remember to add new version section to : " + this.UpdatesFile);
                }
            }
            catch (Exception ex)
            {
                Log.LogError("WIX file edit failed: " + ex.Message);
                return false;
            }
            return true;
        }

        private bool UpdateReadmeFile(Version v)
        {
            if (!System.IO.File.Exists(this.ReadmeFile))
            {
                Log.LogError("ReadmeFile not found: " + this.ReadmeFile);
                return false;
            }

            try
            {
                XDocument doc = XDocument.Load(this.ReadmeFile);
                XNamespace ns = XNamespace.Get("http://www.w3.org/1999/xhtml");
                foreach (var span in doc.Root.Descendants(ns + "span"))
                {
                    if ((string)span.Attribute("class") == "version")
                    {
                        string newValue = "Version " + v.ToString();
                        if (newValue != (string)span.Value)
                        {
                            span.Value = newValue;
                            doc.Save(this.ReadmeFile);
                            Log.LogMessage(MessageImportance.High, "Updated version number in : " + this.ReadmeFile + " to match Version.cs version " + v.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError("ReadmeFile file edit failed: " + ex.Message);
                return false;
            }
            return true;
        }
    }

    class Program
    {
        public static void Main()
        {
            SyncWix wix = new SyncWix()
            {
                DropDir = @"d:\git\lovettchris\XmlNotepad\src\drop",
                VersionFile = @"d:\git\lovettchris\XmlNotepad\src\Version\Version.cs",
                WixFile = @"d:\git\lovettchris\XmlNotepad\src\XmlNotepadSetup\Product.wxs",
                UpdatesFile = @"d:\git\lovettchris\XmlNotepad\src\Updates\Updates.xml",
                ReadmeFile = @"d:\git\lovettchris\XmlNotepad\src\Updates\Readme.htm"
            };

            wix.Execute();
        }
    }
}
