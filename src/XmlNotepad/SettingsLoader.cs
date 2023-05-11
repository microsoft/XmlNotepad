using System;
using System.Diagnostics;
using System.IO;

namespace XmlNotepad
{
    public class SettingsLoader
    {
        private SettingsLocation _settingsLocation;

        public string LocalConfigFile
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                Debug.Assert(!string.IsNullOrEmpty(path));
                return System.IO.Path.Combine(path, "Microsoft", "Xml Notepad", "XmlNotepad.settings");
            }
        }

        public string PortableTemplateFile
        {
            get
            {
                string path = Path.GetDirectoryName(this.GetType().Assembly.Location);
                Debug.Assert(!string.IsNullOrEmpty(path));
                var settings = System.IO.Path.Combine(path, "XmlNotepad.template.settings");
                if (File.Exists(settings))
                {
                    return settings;
                }
                settings = System.IO.Path.Combine(path, "Resources", "XmlNotepad.template.settings");
                if (File.Exists(settings))
                {
                    return settings;
                }
                return null;
            }
        }

        public string PortableConfigFile
        {
            get
            {
                string path = Path.GetDirectoryName(this.GetType().Assembly.Location);
                Debug.Assert(!string.IsNullOrEmpty(path));
                return System.IO.Path.Combine(path, "XmlNotepad.settings");
            }
        }

        public string RoamingConfigFile
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                Debug.Assert(!string.IsNullOrEmpty(path));
                return System.IO.Path.Combine(path, "Microsoft", "Xml Notepad", "XmlNotepad.settings");
            }
        }

        public virtual string TemporaryConfigFile
        {
            get
            {
                string path = Path.GetTempPath();
                Debug.Assert(!string.IsNullOrEmpty(path));
                return System.IO.Path.Combine(path, "Microsoft", "Xml Notepad", "XmlNotepad.settings");
            }
        }

        public virtual string TestConfigFile
        {
            get
            {
                string path = Path.GetTempPath();
                Debug.Assert(!string.IsNullOrEmpty(path));
                return System.IO.Path.Combine(path, "Microsoft", "Xml Notepad", "XmlNotepad.test.settings");
            }
        }

        public void LoadSettings(Settings settings, SettingsLocation location = SettingsLocation.Auto)
        {
            settings.SetDefaults();

            string fileName = null;

            if (location == SettingsLocation.Auto)
            {
                // but we prefer the existing settings file wherever that was.
                if (!string.IsNullOrEmpty(settings.FileName) && File.Exists(settings.FileName))
                {
                    fileName = settings.FileName;
                    location = settings.GetLocation();
                }
                else
                {
                    // try portable.
                    fileName = PortableConfigFile;
                    location = SettingsLocation.Portable;
                }
                
                if (!File.Exists(fileName))
                {
                    // try local
                    fileName = LocalConfigFile;
                    location = SettingsLocation.Local;
                }

                if (!File.Exists(fileName))
                {
                    // try roaming.
                    fileName = RoamingConfigFile;
                    location = SettingsLocation.Roaming;
                }

                if (File.Exists(fileName))
                {
                    if (fileName != settings.FileName)
                    {
                        settings.Load(fileName);
                    }
                    // use this path
                }
                else if (File.Exists(PortableTemplateFile))
                {
                    // brand new user, so load the template
                    settings.Load(PortableTemplateFile);
                    location = SettingsLocation.Roaming;
                    fileName = RoamingConfigFile; // and store it in RoamingConfigFile.
                }
            }

            if (location == SettingsLocation.Test)
            {
                // always start with no settings.                
                settings.Load(this.TestConfigFile);
                location = SettingsLocation.Roaming;
                fileName = RoamingConfigFile; // and store it in RoamingConfigFile.
            }
            else if (location == SettingsLocation.PortableTemplate)
            {
                // testing the portable template works.
                settings.Load(PortableTemplateFile);
                location = SettingsLocation.Roaming;
                fileName = RoamingConfigFile; // and store it in RoamingConfigFile.
            }

            if (!string.IsNullOrEmpty(fileName))
            {
                Debug.WriteLine(fileName);
                settings.FileName = fileName;
                settings.SetLocation(location);
                _settingsLocation = location;
            }
        }

        public void MoveSettings(Settings settings)
        {
            SettingsLocation location = settings.GetLocation();
            string existingLocation = settings.FileName;
            string newLocation = null;
            switch (location)
            {
                case SettingsLocation.Portable:
                    newLocation = this.PortableConfigFile;
                    break;
                case SettingsLocation.Local:
                    newLocation = this.LocalConfigFile;
                    break;
                case SettingsLocation.Roaming:
                    newLocation = this.RoamingConfigFile;
                    break;
                default:
                    break;
            }

            if (string.Compare(existingLocation, newLocation, StringComparison.OrdinalIgnoreCase) == 0)
            {
                // no change!
            }
            else
            {
                bool moved = false;
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newLocation));
                    if (File.Exists(newLocation))
                    {
                        File.Delete(newLocation);
                    }
                    if (File.Exists(existingLocation))
                    {
                        File.Move(existingLocation, newLocation);
                        moved = true;
                    }
                    settings.FileName = newLocation;
                    this._settingsLocation = location;
                }
                finally
                {
                    if (!moved)
                    {
                        // revert the change
                        settings.SetLocation(this._settingsLocation);
                    }
                }
            }
        }

    }
}
