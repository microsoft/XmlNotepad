using System;
using System.Xml;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace XmlNotepad
{
    public sealed class Utilities
    {
        private Utilities() { }

        public static void InitializeWriterSettings(XmlWriterSettings settings, IServiceProvider sp) {
            settings.CheckCharacters = false;
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineChars = "\r\n";
            settings.NewLineHandling = NewLineHandling.Replace;

            if (sp != null) {
                Settings s = (Settings)sp.GetService(typeof(Settings));
                if (s != null)
                {
                    settings.Indent = (bool)s["AutoFormatOnSave"];
                    IndentChar indentChar = (IndentChar)s["IndentChar"];
                    int indentLevel = (int)s["IndentLevel"];
                    char ch = (indentChar == IndentChar.Space) ? ' ' : '\t';
                    settings.IndentChars = new string(ch, indentLevel);
                    settings.NewLineChars = UserSettings.Unescape((string)s["NewLineChars"]);
                }
            }
        }
        
        // Lighten up the given baseColor so it is easy to read on the system Highlight color background.
        public static Brush HighlightTextBrush(Color baseColor) {
            SolidBrush ht = SystemBrushes.Highlight as SolidBrush;
            Color selectedColor = ht != null ? ht.Color : Color.FromArgb(49, 106, 197);
            HLSColor cls = new HLSColor(baseColor);
            HLSColor hls = new HLSColor(selectedColor);
            int luminosity = (hls.Luminosity > 120) ? 20 : 220;
            return new SolidBrush(HLSColor.ColorFromHLS(cls.Hue, luminosity, cls.Saturation));
        }


        public static void OpenUrl(IntPtr hwnd, string url) {
            const int SW_SHOWNORMAL = 1;
            ShellExecute(hwnd, "open", url, null, Application.StartupPath, SW_SHOWNORMAL);
        }

        [DllImport("Shell32.dll", EntryPoint = "ShellExecuteA",
             SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true,
             CallingConvention = CallingConvention.StdCall)]
        static extern int ShellExecute(IntPtr handle, string verb, string file,
            string args, string dir, int show);
    }

    public static class CurrentEvent {
        public static EventArgs Event;
    }


    [ClassInterface(ClassInterfaceType.None)]
    [ComImport]
    [Guid("1968106d-f3b5-44cf-890e-116fcb9ecef1")]
    [TypeLibType(TypeLibTypeFlags.FCanCreate)]
    internal sealed class ApplicationAssociationRegistrationUI : IApplicationAssociationRegistrationUI
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void LaunchAdvancedAssociationUI(string appRegistryName);
    }

    [CoClass(typeof(ApplicationAssociationRegistrationUI))]
    [ComImport]
    [Guid("1f76a169-f994-40ac-8fc8-0959e8874710")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [TypeLibImportClass(typeof(ApplicationAssociationRegistrationUI))]
    internal interface IApplicationAssociationRegistrationUI
    {
        void LaunchAdvancedAssociationUI([MarshalAs(UnmanagedType.LPWStr)] string appRegistryName);
    }

}
