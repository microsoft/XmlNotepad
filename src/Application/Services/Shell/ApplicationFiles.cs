using System.Runtime.InteropServices;
using System.Text;

namespace XmlNotepad.Services;

internal static class ApplicationFiles
{
    public static string ContentDirectory { get; } = FindContentDirectory();

    private static string FindContentDirectory()
    {
        var executableDirectory = AppContext.BaseDirectory;
        if (File.Exists(Path.Combine(executableDirectory, "Assets", "XmlNotepad.ico")))
        {
            return executableDirectory;
        }

        const int insufficientBuffer = 122;
        uint length = 0;
        if (GetCurrentPackagePath(ref length, null) == insufficientBuffer)
        {
            var path = new StringBuilder((int)length);
            if (GetCurrentPackagePath(ref length, path) == 0)
            {
                var packageDirectory = path.ToString();
                if (File.Exists(Path.Combine(packageDirectory, "Assets", "XmlNotepad.ico")))
                {
                    return packageDirectory;
                }
            }
        }
        return executableDirectory;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int GetCurrentPackagePath(ref uint pathLength, StringBuilder? path);
}
