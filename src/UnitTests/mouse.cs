using System.Runtime.InteropServices;
using System.Threading;

namespace UnitTests
{
    // Why the heck does .NET provide SendKeys but not mouse simulation???
    // Another interesting tid-bit.  Reading the cursor position doesn't work over
    // terminal server!
    public static class Mouse
    {
        [DllImport("user32.dll")]
        public static extern int GetDoubleClickTime();

        internal static void AvoidDoubleClick()
        {
            int sleep = GetDoubleClickTime();
            Thread.Sleep(sleep * 2);
        }
    }

}
