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
                return System.IO.Path.Combine(path, "XmlNotepad.template.settings");
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

        public void LoadSettings(Settings settings, bool testing)
        {
            settings.SetDefaults();
            if (testing)
            {
                // always start with no settings.                
                settings.Load(this.TestConfigFile);
                settings["SettingsLocation"] = (int)SettingsLocation.Roaming;
            }
            else
            {
                // allow user to have a local settings file (xcopy deployable).
                SettingsLocation location = SettingsLocation.Portable;
                var path = PortableConfigFile;
                if (!File.Exists(path))
                {
                    path = LocalConfigFile;
                    location = SettingsLocation.Local;
                }
                if (!File.Exists(path))
                {
                    path = RoamingConfigFile;
                    location = SettingsLocation.Roaming;
                }

                if (File.Exists(path))
                {
                    Debug.WriteLine(path);
                    settings.Load(path);
                    settings["SettingsLocation"] = (int)location;
                    _settingsLocation = location;
                }
                else if (File.Exists(PortableTemplateFile))
                {
                    // brand new user, so load the template
                    settings.Load(PortableTemplateFile);
                    settings.FileName = path; // but store it in RoamingConfigFile.
                }

                if (string.IsNullOrEmpty(settings.FileName))
                {
                    settings.FileName = path;
                }
            }
        }

        public void MoveSettings(Settings settings)
        {
            SettingsLocation location = (SettingsLocation)settings["SettingsLocation"];
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
                    }
                    settings.FileName = newLocation;
                    this._settingsLocation = location;
                }
                finally
                {
                    // revert the change
                    settings["SettingsLocation"] = (int)this._settingsLocation;
                }
            }
        }

    }
}
