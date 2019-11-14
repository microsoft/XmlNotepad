using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace XmlNotepadBuildTasks
{
    public class SyncWixVersion : Task
    {
        [Required]
        public string VersionFile { get; set; }

        [Required]
        public string WixFile { get; set; }

        [Required]
        public string HelpFile { get; set; }

        [Required]
        public string UpdatesFile { get; set; }

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

                bool result = UpdateWixVersion(v);
                result &= UpdateHelpFile(v);
                result &= CheckUpdatesFile(v);
                return result;
            }
        }

        private bool UpdateWixVersion(Version v)
        {
            if (!System.IO.File.Exists(this.WixFile))
            {
                Log.LogError("WIX file not found: " + this.WixFile);
                return false;
            }

            try
            {
                XDocument doc = XDocument.Load(this.WixFile);
                XNamespace ns = doc.Root.Name.Namespace;
                XElement product = doc.Root.Element(ns + "Product");
                if (v.ToString() != (string)product.Attribute("Version"))
                {
                    product.SetAttributeValue("Version", v.ToString());
                    doc.Save(this.WixFile);
                    Log.LogMessage(MessageImportance.High, "Updated version number in : " + this.WixFile + " to match Version.cs version " + v.ToString());
                }
            }
            catch (Exception ex)
            {
                Log.LogError("WIX file edit failed: " + ex.Message);
                return false;
            }
            return true;
        }

        private bool UpdateHelpFile(Version v)
        {
            if (!System.IO.File.Exists(this.HelpFile))
            {
                Log.LogError("Help file not found: " + this.HelpFile);
                return false;
            }

            try
            {
                string[] lines = System.IO.File.ReadAllLines(this.HelpFile);
                using (StreamWriter writer = new StreamWriter(this.HelpFile))
                {
                    foreach (var line in lines)
                    {
                        string outline = line;
                        //             Version 2.8.0.7
                        if (line.Trim().StartsWith("Version"))
                        {
                            int i = line.IndexOf("Version") + 8;
                            outline = line.Substring(0, i) + v.ToString();
                            Log.LogMessage(MessageImportance.High, "Updated version number in : " + this.HelpFile + " to match Version.cs version " + v.ToString());
                        }
                        writer.WriteLine(outline);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError("HelpFile file edit failed: " + ex.Message);
                return false;
            }
            return true;
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
    }

}
