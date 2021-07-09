using Microsoft.Win32;
using System.Windows.Forms;

namespace XmlNotepad
{
    public class FileAssociation
    {
        public static void AddXmlProgids()
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\.xml\OpenWithProgids", true))
            {
                key.SetValue("XmlNotepad.xmlfile", "");
            }

            using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\XmlNotepad.xmlfile\shell\open\command"))
            {
                var cmd = Application.ExecutablePath + " \"%1\"";
                key.SetValue("", cmd);
            }
        }
    }
}
