//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace XmlNotepad.Properties {

    /// <summary>
    /// Provides access to the application settings.
    /// </summary>
    public static class AppSettings
    {
        private static readonly Settings settings = new Settings();

        /// <summary>
        /// Gets the default instance of the application settings.
        /// </summary>
        public static Settings Default
        {
            get { return settings; }
        }

        /// <summary>
        /// Saves the application settings.
        /// </summary>
        public static void Save()
        {
            settings.Save();
        }
    }

    /// <summary>
    /// Represents the application settings.
    /// </summary>
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase
    {
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));

        /// <summary>
        /// Gets the default instance of the application settings.
        /// </summary>
        public static Settings Default
        {
            get { return defaultInstance; }
        }
    }
}
