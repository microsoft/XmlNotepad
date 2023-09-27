using System;
using System.Collections.Generic;
using System.Text;

namespace XmlNotepad
{
    public sealed class HelpService
    {
        public static string HelpBaseUri
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
                return HelpBaseUri + "index.html";                
            }
        }

        public string XsltHelp
        {
            get
            {
                return HelpBaseUri + "help/xslt";
            }
        }

        public string OptionsHelp
        {
            get
            {
                return HelpBaseUri + "help/options";
            }
        }

        public string SchemaHelp
        {
            get
            {
                return HelpBaseUri + "help/schemas";
            }
        }


        public string FindHelp
        {
            get
            {
                return HelpBaseUri + "help/find";
            }
        }

        public bool DynamicHelpEnabled { get; set; }
    }

}
