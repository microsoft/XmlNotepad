using Microsoft.XmlDiffPatch;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XmlNotepad
{
    public static class SettingExtensions
    {
        public static Font GetFont(this Settings s)
        {
            var name = (string)s["FontFamily"];
            var size = Math.Max(5, (double)s["FontSize"]);
            var style = (string)s["FontStyle"];
            var weight = (string)s["FontWeight"];
            FontStyle fs = FontStyle.Regular;
            switch (style)
            {
                case "Normal":
                    fs = FontStyle.Regular;
                    break;
                case "Italic":
                    fs = fs = FontStyle.Italic;
                    break;
                case "Bold":
                    fs = fs = FontStyle.Bold;
                    break;
            }
            switch (weight)
            {
                case "Bold":
                    fs = FontStyle.Bold;
                    break;
                default:
                    break;
            }
            return new Font(name, (float)size, fs);
        }

        public static void SetFont(this Settings s, Font f) 
        {
            s["FontFamily"] = f.FontFamily.Name;
            s["FontSize"] = (double)f.SizeInPoints;
            switch (f.Style)
            {
                case FontStyle.Regular:
                    s["FontStyle"] = "Normal";
                    s["FontWeight"] = "Normal";
                    break;
                case FontStyle.Bold:
                    s["FontStyle"] = "Normal";
                    s["FontWeight"] = "Bold";
                    break;
                case FontStyle.Italic:
                    s["FontStyle"] = "Italic";
                    s["FontWeight"] = "Normal";
                    break;
                case FontStyle.Underline:
                    break;
                case FontStyle.Strikeout:
                    break;
                default:
                    break;
            }
        }

        public static XmlDiffOptions GetXmlDiffOptions(this Settings s) {

            XmlDiffOptions options = XmlDiffOptions.None;
            if ((bool)s["XmlDiffIgnoreChildOrder"])
            {
                options |= XmlDiffOptions.IgnoreChildOrder;
            }
            if ((bool)s["XmlDiffIgnoreComments"])
            {
                options |= XmlDiffOptions.IgnoreComments;
            }
            if ((bool)s["XmlDiffIgnorePI"])
            {
                options |= XmlDiffOptions.IgnorePI;
            }
            if ((bool)s["XmlDiffIgnoreWhitespace"])
            {
                options |= XmlDiffOptions.IgnoreWhitespace;
            }
            if ((bool)s["XmlDiffIgnoreNamespaces"])
            {
                options |= XmlDiffOptions.IgnoreNamespaces;
            }
            if ((bool)s["XmlDiffIgnorePrefixes"])
            {
                options |= XmlDiffOptions.IgnorePrefixes;
            }
            if ((bool)s["XmlDiffIgnoreXmlDecl"])
            {
                options |= XmlDiffOptions.IgnoreXmlDecl;
            }
            if ((bool)s["XmlDiffIgnoreDtd"])
            {
                options |= XmlDiffOptions.IgnoreDtd;
            }
            return options;
        }

        public static Point CenterPosition(this Form w, Rectangle bounds)
        {
            Size s = w.ClientSize;
            Point center = new Point(bounds.Left + (bounds.Width / 2) - (s.Width / 2),
                bounds.Top + (bounds.Height / 2) - (s.Height / 2));

            if (center.X < 0) center.X = 0;
            if (center.Y < 0) center.Y = 0;

            return center;
        }

        public static bool IsOnScreen(this Form w, Point topLeft)
        {
            // Check if a window location is off in never never land which can
            // happen if a user changes the monitor configuration or copies the settings from
            // a different machine that has different monitor setup.
            Point bottomRight = topLeft + w.ClientSize;

            foreach (Screen s in Screen.AllScreens)
            {
                Rectangle sb = s.WorkingArea;
                if (sb.Contains(topLeft) && sb.Contains(bottomRight))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
