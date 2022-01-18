using System;
using System.Collections.Generic;
using System.Text;

namespace XmlNotepad
{
    public sealed class HelpService
    {
        public string HelpBaseUri
        {
            get
            {
                return "https://microsoft.github.io/XmlNotepad/";
            }
        }

        public string DefaultHelp
        {
            get
            {
                if (OnlineHelpAvailable)
                {
                    return HelpBaseUri + "index.html";
                }
                else
                {
                    return FileHelpers.ValidPath(Settings.Instance.StartupPath + "\\Help\\index.html");
                }
            }
        }

        public string OptionsHelp
        {
            get
            {
                if (OnlineHelpAvailable)
                {
                    return HelpBaseUri + "help/options";
                }
                else
                {
                    return FileHelpers.ValidPath(Settings.Instance.StartupPath + "\\Help\\help\\options.htm");
                }
            }
        }

        public string SchemaHelp
        {
            get
            {
                if (OnlineHelpAvailable)
                {
                    return HelpBaseUri + "help/schemas";
                }
                else
                {
                    return FileHelpers.ValidPath(Settings.Instance.StartupPath + "\\Help\\help\\schemas.htm");
                }
            }
        }


        public string FindHelp
        {
            get
            {
                if (OnlineHelpAvailable)
                {
                    return HelpBaseUri + "help/find";
                }
                else
                {
                    return FileHelpers.ValidPath(Settings.Instance.StartupPath + "\\Help\\help\\find.htm");
                }
            }
        }

        public bool OnlineHelpAvailable { get; set; }

        public bool DynamicHelpEnabled { get; set; }
    }

}
