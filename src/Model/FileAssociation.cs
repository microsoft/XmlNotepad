using Microsoft.Win32;

namespace XmlNotepad
{
    public class FileAssociation
    {
        public static void AddXmlProgids(string executablePath)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\.xml\OpenWithProgids", true))
                {
                    key.SetValue("XmlNotepad.xmlfile", "");
                }

                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\XmlNotepad.xmlfile\shell\open\command"))
                {
                    var cmd = "\"" + executablePath + "\" \"%1\"";
                    key.SetValue("", cmd);
                }
            }
            catch
            {
                // todo: tell the user?
            }
        }
    }
}
