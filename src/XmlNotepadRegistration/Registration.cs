using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace XmlNotepadRegistration
{
    public class Registration
    {
        string appPath;

        public Registration(string appPath)
        {
            this.appPath = appPath;
        }

        public void Register(RegistryKey key, XElement tree)
        {
            string name = (string)tree.Attribute("name");
            RegistryKey child = key.CreateSubKey(name, RegistryKeyPermissionCheck.ReadWriteSubTree);
            using (child)
            {
                string value = (string)tree.Attribute("value");
                if (!string.IsNullOrEmpty(value))
                {
                    child.SetValue("", value);
                }
                foreach (XElement e in tree.Elements())
                {
                    name = (string)e.Attribute("name");
                    value = "" + (string)e.Attribute("value");

                    switch (e.Name.LocalName)
                    {
                        case "Key":
                            Register(child, e);
                            break;
                        case "String":
                            child.SetValue(name, value);
                            break;
                        case "DWord":
                            {
                                int i = 0;
                                int.TryParse(value, out i);
                                child.SetValue(name, i, RegistryValueKind.DWord);
                            }
                            break;
                        case "Binary":
                            {
                                byte[] data = Convert.FromBase64String(value);
                                child.SetValue(name, data, RegistryValueKind.Binary);
                            }
                            break;
                        case "None":
                            {
                                child.SetValue(name, new byte[0], RegistryValueKind.None);
                            }
                            break;
                    }
                }
            }
        }

        public bool IsRegistered(string name)
        {            
            var key = Registry.ClassesRoot.OpenSubKey(string.Format(@"Applications\{0}\shell\open\command", name), true);
            using (key)
            {
                return key != null;
            }
        }

        public void RegisterApplication(string progId, string editCommand, string openCommand, string appName, string appDescription)
        {
            var key = Registry.ClassesRoot.OpenSubKey("Applications", true);
            using (key)
            {
                key.DeleteSubKeyTree(progId, false);

                Register(key, XDocument.Parse(string.Format(@"<Key name='{0}'>
    <Key name='shell'>
        <Key name='edit'>
            <Key name='command' value='{1}'/>
        </Key>
        <Key name='open'>
            <Key name='command' value='{2}'/>
        </Key>
    </Key>
</Key>", progId, editCommand, openCommand)).Root);
            }


            Registry.ClassesRoot.DeleteSubKeyTree(progId, false);
            key = Registry.ClassesRoot.CreateSubKey(progId, RegistryKeyPermissionCheck.ReadWriteSubTree);
            using (key)
            {
                Register(key, XDocument.Parse(string.Format(@"
    <Key name='shell'>
        <Key name='edit'>
            <Key name='command' value='{0}'/>
        </Key>
        <Key name='open'>
            <Key name='command' value='{1}'/>
        </Key>
    </Key>", editCommand, openCommand)).Root);
            }


            key = Registry.LocalMachine.OpenSubKey(@"Software", true);
            using (key)
            {
                Register(key, new XElement("Key", new XAttribute("name", "RegisteredApplications"),
                    new XElement("String", new XAttribute("name", appName), 
                                           new XAttribute("value", string.Format(@"Software\Microsoft\{0}\Capabilities", appName)))));
            }

            key = Registry.LocalMachine.CreateSubKey(@"Software\Microsoft", RegistryKeyPermissionCheck.ReadWriteSubTree);
            using (key)
            {
                key.DeleteSubKeyTree(appName, false);

                Register(key, XDocument.Parse(string.Format(@"<Key name='{0}'>
  <Key name='Capabilities'>
    <String name='ApplicationName' value='{0}'/>        
    <String name='ApplicationDescription' value='{1}'/>        
    <Key name='FileAssociations'>
        <String name='.xml' value='{2}'/>
        <String name='.xslt' value='{2}'/>
        <String name='.xsd' value='{2}'/>
        <String name='.xsl' value='{2}'/>
        <String name='.xdr' value='{2}'/>
    </Key>
  </Key>
</Key>", appName, appDescription, progId)).Root);
            }

        }

    }
}
