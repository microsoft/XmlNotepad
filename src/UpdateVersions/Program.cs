using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

namespace UpdateVersions
{
    public class LogWriter
    {
        public void LogError(string message)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = saved;
        }

        public void LogMessage(string message)
        {
            Console.WriteLine(message);
        }
    }

    public class SyncVersions
    {
        public LogWriter Log { get; set; }
        public string MasterVersionFile { get; set; }
        public string CSharpVersionFile { get; set; }
        public string WixFile { get; set; }
        public string ApplicationProjectFile { get; set; }
        public string AppManifestFile { get; set; }
        public string DropDir { get; set; }
        public string UpdatesFile { get; set; }
        public string WebView2Version { get; set; }

        public bool Execute()
        {
            if (!System.IO.File.Exists(this.MasterVersionFile))
            {
                Log.LogError("Cannot find master version file: " + this.MasterVersionFile);
                return false;
            }
            if (!System.IO.File.Exists(this.CSharpVersionFile))
            {
                Log.LogError("Cannot find C# version file: " + this.CSharpVersionFile);
                return false;
            }

            var doc = XDocument.Load(this.MasterVersionFile);
            var ns = doc.Root.Name.Namespace;
            var e = doc.Root.Element(ns + "PropertyGroup").Element(ns + "ApplicationVersion");
            string version = e.Value;

            Version v;
            if (string.IsNullOrEmpty(version) || !Version.TryParse(version, out v))
            {
                Log.LogError("Could not find valid valid version number in : " + this.MasterVersionFile);
                return false;
            }

            Log.LogMessage("SyncVersions to " + v.ToString());

            bool result = UpdateWebView2Version(doc);
            result &= UpdateCSharpVersion(v);
            result &= UpdateWixDoc(v);
            result &= UpdatePackageManifest(v);
            result &= UpdateApplicationProjectFile(v);
            result &= CheckUpdatesFile(v);
            return result;
        }

        private bool UpdateWebView2Version(XDocument props)
        {
            var dir = Path.GetDirectoryName(this.ApplicationProjectFile);
            var xmlnotepadProject = Path.Combine(dir, "..", "XmlNotepad", "XmlNotepad.csproj");
            var doc = XDocument.Load(xmlnotepadProject);
            var ns = doc.Root.Name.Namespace;
            var prefix = "Microsoft.Web.WebView2.Core";
            string version = null;
            foreach (var e in doc.Descendants(ns + "Reference"))
            {
                string include = (string)e.Attribute("Include");
                if (include.StartsWith(prefix))
                {
                    var name = new AssemblyName(include);
                    version = name.Version.ToString();
                    break;
                }
            }
            if (!string.IsNullOrEmpty(version))
            {
                WebView2Version = version;
                ns = props.Root.Name.Namespace;
                var wve = props.Root.Descendants(ns + "WebView2Version").FirstOrDefault();
                if (wve != null)
                {
                    var v = wve.Value;
                    if (v != version)
                    {
                        wve.Value = version;
                        props.Save(this.MasterVersionFile);
                        Log.LogMessage("Updating WebView2Version to " + version);
                    }
                    return true;
                }
                else
                {
                    Log.LogError("Missing <WebView2Version> property in " + this.MasterVersionFile);
                }
            }

            Log.LogError("Could not find Microsoft.Web.WebView2.Core version in XmlNotepad.csproj");
            return false;
        }

        private bool UpdateCSharpVersion(Version v)
        {
            // Fix these assembly attributes:
            // [assembly: AssemblyVersion("2.8.0.29")]
            // [assembly: AssemblyFileVersion("2.8.0.29")]
            bool changed = true;
            string[] prefixes = new string[] { "[assembly: AssemblyVersion", "[assembly: AssemblyFileVersion" };
            string[] lines = File.ReadAllLines(this.CSharpVersionFile);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                foreach (var prefix in prefixes)
                {
                    if (line.StartsWith(prefix))
                    {
                        string expected = string.Format("{0}(\"{1}\")]", prefix, v);
                        if (line != expected)
                        {
                            lines[i] = expected;
                            changed = true;
                        }
                    }
                }
            }
            if (changed)
            {
                try
                {
                    File.WriteAllLines(this.CSharpVersionFile, lines);
                }
                catch (Exception ex)
                {
                    Log.LogError("file '" + CSharpVersionFile + "' edit failed: " + ex.Message);
                }
            }
            // return that there is no error.
            return true;
        }

        private bool UpdateApplicationProjectFile(Version v)
        {
            if (!System.IO.File.Exists(this.ApplicationProjectFile))
            {
                Log.LogError("ApplicationProjectFile file not found: " + this.ApplicationProjectFile);
                return false;
            }
            try
            {
                bool changed = false;
                XDocument doc = XDocument.Load(this.ApplicationProjectFile);
                var ns = doc.Root.Name.Namespace;

                var prefix = "Microsoft.Web.WebView2";
                var webView2Version = GetReferenceVersion(prefix,
                    Path.Combine(Path.GetDirectoryName(this.ApplicationProjectFile), "..", "XmlNotepad", "XmlNotepad.csproj"));

                // ClickOnce is wrongly editing the project file to "add" these items, when in reality
                // they need to be inherited from version.props.
                List<XElement> toRemove = new List<XElement>();
                foreach (var e in doc.Root.Elements(ns + "PropertyGroup"))
                {
                    foreach (var f in e.Elements(ns + "ApplicationRevision"))
                    {
                        toRemove.Add(f);
                    }
                    foreach (var f in e.Elements(ns + "ApplicationVersion"))
                    {
                        toRemove.Add(f);
                    }
                }

                if (!string.IsNullOrEmpty(webView2Version))
                {
                    var expected = prefix + "." + webView2Version;
                    foreach (var e in doc.Descendants(ns + "Content"))
                    {
                        string src = (string)e.Attribute("Include");
                        if (src.Contains(prefix) && !src.Contains(expected))
                        {
                            // might need to need to fix the version
                            e.SetAttributeValue("Include", FixeIncludeVersion(src, prefix, expected));
                            changed = true;
                        }
                    }
                }

                foreach (var e in toRemove)
                {
                    e.Remove();
                    changed = true;
                }

                if (changed)
                {
                    Log.LogMessage("SyncVersions updating " + this.ApplicationProjectFile);
                    doc.Save(this.ApplicationProjectFile);
                }
            }
            catch (Exception ex)
            {
                Log.LogError("file '" + this.AppManifestFile + "' edit failed: " + ex.Message);
                return false;
            }

            // return that there is no error.
            return true;
        }

        private string FixeIncludeVersion(string src, string prefix, string expected)
        {
            string[] parts = src.Split('\\');
            for (int i = 0, n = parts.Length; i < n; i++)
            {
                var s = parts[i];
                if (s.StartsWith(prefix))
                {
                    parts[i] = expected;
                }
            }
            return string.Join("\\", parts);
        }

        private string GetReferenceVersion(string prefix, string projectFile)
        {
            XDocument doc = XDocument.Load(projectFile);
            var ns = doc.Root.Name.Namespace;
            foreach (var e in doc.Descendants(ns + "Reference"))
            {
                string include = (string)e.Attribute("Include");
                if (include.Contains(prefix))
                {
                    string[] parts = include.Split(',');
                    if (parts.Length > 1)
                    {
                        var version = parts[1].Trim().Split('=');
                        if (version.Length > 0)
                        {
                            return version[1].Trim();
                        }
                    }
                }
            }
            return null;
        }

        private bool UpdatePackageManifest(Version v)
        {
            if (!System.IO.File.Exists(this.AppManifestFile))
            {
                Log.LogError("AppManifest file not found: " + this.AppManifestFile);
                return false;
            }

            try
            {
                string newVersion = v.ToString();
                bool changed = false;
                XDocument doc = XDocument.Load(this.AppManifestFile);
                var ns = doc.Root.Name.Namespace;
                foreach (var e in doc.Root.Elements(ns + "Identity"))
                {
                    var s = (string)e.Attribute("Version");
                    if (s != newVersion)
                    {
                        changed = true;
                        e.SetAttributeValue("Version", newVersion);
                    }
                }

                if (changed)
                {
                    Log.LogMessage("SyncVersions updating " + this.AppManifestFile);
                    doc.Save(this.AppManifestFile);
                }
            }
            catch (Exception ex)
            {
                Log.LogError("file '" + this.AppManifestFile + "' edit failed: " + ex.Message);
                return false;
            }
            // return that there is no error.
            return true;
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
                    Log.LogMessage("SyncVersions updating " + this.WixFile);
                    doc.Save(this.WixFile);
                }
            }
            catch (Exception ex)
            {
                Log.LogError("file '" + this.WixFile + "' edit failed: " + ex.Message);
                return false;
            }
            // return that there is no error.
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
            foreach (var cref in feature.Elements(ns + "ComponentRef"))
            {
                existing.Add((string)cref.Attribute("Id"));
            }

            HashSet<string> hashFound = new HashSet<string>(components);
            HashSet<string> hashExisting = new HashSet<string>(existing);
            if (!hashFound.SetEquals(hashExisting))
            {
                Debug.WriteLine("Rewriting component references...");
                foreach (var child in feature.Nodes().ToArray())
                {
                    child.Remove();
                }

                foreach (var id in components)
                {
                    feature.Add(new XElement(ns + "ComponentRef",
                        new XAttribute("Id", id)));
                }
            }

            // return that there is no error.
            return true;
        }

        private bool UpdateWixVersion(XDocument wixdoc, Version v)
        {
            XNamespace ns = wixdoc.Root.Name.Namespace;
            XElement product = wixdoc.Root.Element(ns + "Product");
            if (v.ToString() != (string)product.Attribute("Version"))
            {
                product.SetAttributeValue("Version", v.ToString());
                return true;
            }
            return false;
        }

        private bool UpdateDropFiles(XDocument wixdoc, Version v)
        {
            if (!System.IO.Directory.Exists(this.DropDir))
            {
                // PublishDrop.cmd hasn't been run yet, so skip it.
                return false;
            }

            bool result = true;
            try
            {
                XNamespace ns = wixdoc.Root.Name.Namespace;
                XElement product = wixdoc.Root.Element(ns + "Product");

                foreach (string subdir in new string[] { "Help", "samples" })
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
            if (!System.IO.Directory.Exists(this.DropDir))
            {
                // PublishDrop.cmd hasn't been run yet, so skip it.
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
                var reluri = new Uri(this.DropDir + "/").MakeRelativeUri(new Uri(dirname));
                var relpath = Uri.UnescapeDataString(reluri.ToString());
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
            foreach (var pair in existing)
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
                    Log.LogMessage("Please remember to add new version section to : " + this.UpdatesFile);
                }
            }
            catch (Exception ex)
            {
                Log.LogError("WIX file edit failed: " + ex.Message);
                return false;
            }
            // return that there is no error.
            return true;
        }
    }

    internal class Program
    {
        static int Main(string[] args)
        {
            SyncVersions sync = new SyncVersions();
            sync.Log = new LogWriter();
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var solutionPath = Path.GetDirectoryName(exePath);
            while (!string.IsNullOrEmpty(solutionPath))
            {
                var sln = Path.Combine(solutionPath, "XmlNotepad.sln");
                if (File.Exists(sln))
                {
                    break;
                }
                var parent = Path.GetDirectoryName(solutionPath);
                if (parent == sln)
                {
                    break;
                }
                solutionPath = parent;
            }

            if (!File.Exists(Path.Combine(solutionPath, "XmlNotepad.sln")))
            {
                Console.WriteLine("Error: Cannot find XmlNotepad.sln");
                return 1;
            }

            sync.MasterVersionFile = Path.Combine(solutionPath, "Version", "Version.props");
            sync.CSharpVersionFile = Path.Combine(solutionPath, "Version", "Version.cs");
            sync.WixFile = Path.Combine(solutionPath, "XmlNotepadSetup", "Product.wxs");
            sync.ApplicationProjectFile = Path.Combine(solutionPath, "Application", "Application.csproj");
            sync.AppManifestFile = Path.Combine(solutionPath, "XmlNotepadPackage", "Package.appxmanifest");
            sync.DropDir = Path.Combine(solutionPath, "drop");
            sync.UpdatesFile = Path.Combine(solutionPath, "Updates", "Updates.xml");
            bool rc = sync.Execute();
            return rc ? 0 : 1;
        }
    }
}