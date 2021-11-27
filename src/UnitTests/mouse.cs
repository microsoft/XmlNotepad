using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace UnitTests {

    // Why the heck does .NET provide SendKeys but not mouse simulation???
    // Another interesting tid-bit.  Reading the cursor position doesn't work over
    // terminal server!
    public class Mouse 
    {
        static int Timeout = 100;

        private Mouse() { }

        public static void MouseDown(Point pt, MouseButtons buttons) {
            MouseInput input = GetVirtualMouseInput(pt.X, pt.Y);
            MouseFlags flags = (MouseFlags)input.dwFlags;
            if ((buttons & MouseButtons.Left) != 0)
            {
                flags |= MouseFlags.MOUSEEVENTF_LEFTDOWN;
            }
            if ((buttons & MouseButtons.Right) != 0)
            {
                flags |= MouseFlags.MOUSEEVENTF_RIGHTDOWN;
            }
            if ((buttons & MouseButtons.Middle) != 0)
            {
                flags |= MouseFlags.MOUSEEVENTF_MIDDLEDOWN;
            }
            if ((buttons & MouseButtons.XButton1) != 0)
            {
                flags |= MouseFlags.MOUSEEVENTF_XDOWN;
            }
            input.dwFlags = (int)(flags | MouseFlags.MOUSEEVENTF_MOVE);
            SendInput(input);
        }

        public static void MouseUp(Point pt, MouseButtons buttons) {
            MouseInput input = GetVirtualMouseInput(pt.X, pt.Y);
            MouseFlags flags = (MouseFlags)input.dwFlags;
            if ((buttons & MouseButtons.Left) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_LEFTUP;
            }
            if ((buttons & MouseButtons.Right) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_RIGHTUP;
            }
            if ((buttons & MouseButtons.Middle) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_MIDDLEUP;
            }
            if ((buttons & MouseButtons.XButton1) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_XUP;
            }
            input.dwFlags = (int)flags;
            SendInput(input);
        }

        public static void MouseClick(Point pt, MouseButtons buttons) {            
            MouseDown(pt, buttons);
            MouseUp(pt, buttons);
        }

        public static void MouseDoubleClick(Point pt, MouseButtons buttons) {
            MouseClick(pt, buttons);
            Thread.Sleep(1);
            MouseClick(pt, buttons);
        }

        private static MouseInput GetVirtualMouseInput(int x, int y)
        {
            MouseInput input = new MouseInput();
            input.type = (int)(InputType.INPUT_MOUSE);
            long screenX = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            long screenY = GetSystemMetrics(SM_CYVIRTUALSCREEN);
            long scale = 65535;
            input.dx = (int)(((long)x * scale) / screenX);
            input.dy = (int)(((long)y * scale) / screenY);
            input.dwFlags = (int)MouseFlags.MOUSEEVENTF_VIRTUALDESK + (int)MouseFlags.MOUSEEVENTF_ABSOLUTE;
            return input;
        }

        public static void MouseMoveTo(int x, int y, MouseButtons buttons) {
            MouseInput input = GetVirtualMouseInput(x, y);
            input.dwFlags += (int)MouseFlags.MOUSEEVENTF_MOVE + (int)MouseFlags.MOUSEEVENTF_ABSOLUTE;
            SendInput(input);
            Application.DoEvents();
        }

        const int DragDelayDrop = 200;

        public static void MouseDragDrop(Point start, Point end, int step, MouseButtons buttons) {
            int s = Timeout;
            Timeout = 10;
            MouseDown(start, buttons);
            Application.DoEvents();
            Thread.Sleep(DragDelayDrop);
            MouseDragTo(start, end, step, buttons);
            Thread.Sleep(DragDelayDrop);
            
            MouseUp(end, buttons);
            Application.DoEvents();
            Thread.Sleep(DragDelayDrop);
            Timeout = s;
        }

        public static void MouseMoveTo(Point start, Point end, int step) {
            MouseDragTo(start, end, step, MouseButtons.None);
        }

        public static void MouseDragTo(Point start, Point end, int step, MouseButtons buttons) {
            const int DelayDragDrop = 100;
            // Interpolate and move mouse smoothly over to given location.                
            int dx = end.X - start.X;
            int dy = end.Y - start.Y;
            int length = (int)Math.Sqrt((double)((dx * dx) + (dy * dy)));
            step = Math.Abs(step);
            int s = Timeout;
            Timeout = 10;
            Application.DoEvents();
            for (int i = 0; i < length; i += step) {
                int tx = start.X + (dx * i) / length;
                int ty = start.Y + (dy * i) / length;
                MouseMoveTo(tx, ty, buttons);

                // Now calibrate movement based on current mouse position.
                Application.DoEvents();
                Thread.Sleep(DelayDragDrop);
            }

            MouseMoveTo(end.X, end.Y, buttons);
            Application.DoEvents();

            Timeout = s;
        }

        public static void MouseWheel(AutomationWrapper w, int clicks) {
            var c = Cursor.Position;
            if (w != null)
            {
                c = w.PhysicalToLogicalPoint(c);
            }
            MouseInput input = GetVirtualMouseInput(c.X, c.Y);
            input.mouseData = clicks;
            input.dwFlags += (int)MouseFlags.MOUSEEVENTF_WHEEL;
            SendInput(input);
        }

        static void SendInput(MouseInput input) {
            //Trace.WriteLine("SendInput:" + input.dx + "," + input.dy + " cursor is at " + Cursor.Position.X + "," + Cursor.Position.Y);
            if ((input.dwFlags & (int)MouseFlags.MOUSEEVENTF_ABSOLUTE) != 0) {
                Cursor.Position = new Point(input.dx, input.dy);
            }
            Debug.WriteLine("SendInput x={0}, y={1}, flags={2}", input.dx, input.dy, input.dwFlags);
            input.time = Environment.TickCount;
            int cb = Marshal.SizeOf(input);
            Debug.Assert(cb == 28); // must match what C++ returns for the INPUT union.
            IntPtr ptr = Marshal.AllocCoTaskMem(cb);
            try {
                Marshal.StructureToPtr(input, ptr, false);
                uint rc = SendInput(1, ptr, cb);
                if (rc != 1) {
                    int hr = GetLastError();
                    throw new ApplicationException("SendInput error " + hr);
                }                
            } finally {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [DllImport("Kernel32.dll")]
        static extern int GetLastError();

        // Simluate MouseEvents

        enum InputType { INPUT_MOUSE = 0, INPUT_KEYBOARD = 1, INPUT_HARDWARE = 2 };

        enum MouseFlags {
            MOUSEEVENTF_MOVE = 0x0001, /* mouse move */
            MOUSEEVENTF_LEFTDOWN = 0x0002, /* left button down */
            MOUSEEVENTF_LEFTUP = 0x0004, /* left button up */
            MOUSEEVENTF_RIGHTDOWN = 0x0008, /* right button down */
            MOUSEEVENTF_RIGHTUP = 0x0010, /* right button up */
            MOUSEEVENTF_MIDDLEDOWN = 0x0020, /* middle button down */
            MOUSEEVENTF_MIDDLEUP = 0x0040, /* middle button up */
            MOUSEEVENTF_XDOWN = 0x0080, /* x button down */
            MOUSEEVENTF_XUP = 0x0100, /* x button down */
            MOUSEEVENTF_WHEEL = 0x0800, /* wheel button rolled */
            MOUSEEVENTF_VIRTUALDESK = 0x4000, /* map to entire virtual desktop */
            MOUSEEVENTF_ABSOLUTE = 0x8000, /* absolute move */
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MouseInput {
            public int type;
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        };

        [StructLayout(LayoutKind.Sequential)]
        struct MouseMovePoint {
            public int x;
            public int y;
            public int time;
            public IntPtr dwExtraInfo;
        };

        [DllImport("User32", EntryPoint = "SendInput")]
        static extern uint SendInput(uint nInputs, IntPtr pInputs, int cbSize);

        // GetSystemMetrics
        public const int SM_CXMAXTRACK = 59;
        public const int SM_CYMAXTRACK = 60;
        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;
        public const int SM_SWAPBUTTON = 23;

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int metric);

        [DllImport("user32.dll")]
        public static extern int GetDoubleClickTime();

        internal static void AvoidDoubleClick()
        {
            int sleep = GetDoubleClickTime();
            Thread.Sleep(sleep * 2);
        }
    }

}
