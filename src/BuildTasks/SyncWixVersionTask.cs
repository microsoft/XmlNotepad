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

                return UpdateWixVersion(v);
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
                    Log.LogMessage("Updated version number in : " + this.WixFile + " to match Version.cs version " + v.ToString());
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
