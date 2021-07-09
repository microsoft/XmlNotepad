using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace XmlNotepad
{
    class Commands
    {
        public readonly static RoutedUICommand Exit;
        static Commands()
        {

            // FILE
            Exit = new RoutedUICommand("Exit", "Exit", typeof(Commands));
        }

    }
}
