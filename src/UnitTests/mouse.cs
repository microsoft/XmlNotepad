using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace UnitTests
{

    // Why the heck does .NET provide SendKeys but not mouse simulation???
    // Another interesting tid-bit.  Reading the cursor position doesn't work over
    // terminal server!
    public class Mouse
    {
        static int Timeout = 100;

        private Mouse() { }

        private static MouseFlags AddMouseDownFlags(MouseFlags flags, MouseButtons buttons)
        {
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
            return flags;
        }

        private static MouseFlags AddMouseUpFlags(MouseFlags flags, MouseButtons buttons)
        {
            if ((buttons & MouseButtons.Left) != 0)
            {
                flags |= MouseFlags.MOUSEEVENTF_LEFTUP;
            }
            if ((buttons & MouseButtons.Right) != 0)
            {
                flags |= MouseFlags.MOUSEEVENTF_RIGHTUP;
            }
            if ((buttons & MouseButtons.Middle) != 0)
            {
                flags |= MouseFlags.MOUSEEVENTF_MIDDLEUP;
            }
            if ((buttons & MouseButtons.XButton1) != 0)
            {
                flags |= MouseFlags.MOUSEEVENTF_XUP;
            }
            return flags;
        }

        public static void MouseDown(Point pt, MouseButtons buttons)
        {
            MouseMoveTo(pt.X, pt.Y, MouseButtons.None, true);
            MouseInput input = GetAbsoluteMouseInput(pt.X, pt.Y);
            MouseFlags flags = (MouseFlags)input.dwFlags;
            flags = AddMouseDownFlags(flags, buttons);
            input.dwFlags = (int)flags;
            SendInput(input);
            Application.DoEvents();
        }

        public static void MouseUp(Point pt, MouseButtons buttons)
        {
            MouseMoveTo(pt.X, pt.Y, MouseButtons.None, false);
            MouseInput input = GetAbsoluteMouseInput(pt.X, pt.Y);
            MouseFlags flags = (MouseFlags)input.dwFlags;
            flags = AddMouseUpFlags(flags, buttons);
            input.dwFlags = (int)flags;
            SendInput(input);
            Application.DoEvents();
        }

        public static void MouseClick(Point pt, MouseButtons buttons)
        {
            MouseDown(pt, buttons);
            MouseUp(pt, buttons);
        }

        public static void MouseDoubleClick(Point pt, MouseButtons buttons)
        {
            MouseClick(pt, buttons);
            Thread.Sleep(1);
            MouseClick(pt, buttons);
        }

        private static MouseInput GetAbsoluteMouseInput(int x, int y)
        {
            MouseInput input = new MouseInput();
            input.type = (int)(InputType.INPUT_MOUSE);
            input.dx = x;
            input.dy = y;
            input.dwFlags = (int)(MouseFlags.MOUSEEVENTF_MOVE_NOCOALESCE | MouseFlags.MOUSEEVENTF_ABSOLUTE);
            input.mouseData = 0;
            input.time = 0;
            return input;
        }

        private static MouseInput GetVirtualMouseInput(int x, int y)
        {
            MouseInput input = new MouseInput();
            input.type = (int)(InputType.INPUT_MOUSE);
            input.dx = (x * 65536) / GetSystemMetrics(SM_CXSCREEN);
            input.dy = (y * 65536) / GetSystemMetrics(SM_CYSCREEN);
            input.dwFlags = (int)(MouseFlags.MOUSEEVENTF_MOVE_NOCOALESCE | MouseFlags.MOUSEEVENTF_ABSOLUTE | MouseFlags.MOUSEEVENTF_VIRTUALDESK);
            input.mouseData = 0;
            input.time = 0;
            return input;
        }

        public static void MouseMoveTo(int x, int y, MouseButtons button, bool down)
        {
            MouseInput input = GetAbsoluteMouseInput(x, y);
            MouseFlags flags = (MouseFlags)input.dwFlags;
            if (button != MouseButtons.None)
            {
                if (down)
                {
                    flags = AddMouseDownFlags(flags, button);
                }
                else
                {
                    flags = AddMouseUpFlags(flags, button);
                }
                flags |= MouseFlags.MOUSEEVENTF_MOVE;
            }
            input.dwFlags = (int)flags;
            SendInput(input);
            Application.DoEvents();
        }

        public static void MouseMoveTo(Point start, Point end, int step)
        {
            MouseDragTo(start, end, step, MouseButtons.None);
        }

        public static void MouseDragTo(Point start, Point end, int step, MouseButtons button)
        {
            // Interpolate and move mouse smoothly over to given location.                
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            int length = (int)Math.Sqrt((dx * dx) + (dy * dy));
            step = Math.Abs(step);
            for (int i = 0; i < length; i += step)
            {
                int tx = start.X + (int)((dx * i) / length);
                int ty = start.Y + (int)((dy * i) / length);
                MouseMoveTo(tx, ty, button, true);
                Thread.Sleep(1);
                Application.DoEvents();
            }

            MouseMoveTo(end.X, end.Y, button, true);
        }

        public static void MouseWheel(AutomationWrapper w, int clicks)
        {
            var c = Cursor.Position;
            if (w != null)
            {
                c = w.PhysicalToLogicalPoint(c);
            }
            MouseInput input = GetAbsoluteMouseInput(c.X, c.Y);
            input.mouseData = clicks;
            input.dwFlags |= (int)MouseFlags.MOUSEEVENTF_WHEEL;
            SendInput(input);
        }

        static void SendInput(MouseInput input)
        {
            //Trace.WriteLine("SendInput:" + input.dx + "," + input.dy + " cursor is at " + Cursor.Position.X + "," + Cursor.Position.Y);
            if ((input.dwFlags & (int)MouseFlags.MOUSEEVENTF_ABSOLUTE) != 0 &&
                (input.dwFlags & (int)MouseFlags.MOUSEEVENTF_VIRTUALDESK) == 0)
            {
                Cursor.Position = new Point(input.dx, input.dy);
            }
            Debug.WriteLine("SendInput x={0}, y={1}, flags={2:x}", input.dx, input.dy, input.dwFlags);
            input.time = Environment.TickCount & Int32.MaxValue;
            int cb = Marshal.SizeOf(input);
            cb = 40; // C++ sizeof(INPUT) seems to pad the MOUSEINPUT from 28 up to 32, , not sure why 
            IntPtr ptr = Marshal.AllocCoTaskMem(cb);
            try
            {
                Marshal.StructureToPtr(input, ptr, false);
                uint rc = SendInput(1, ptr, cb);
                if (rc != 1)
                {
                    int hr = GetLastError();
                    if (hr != 0)
                    {
                        throw new ApplicationException("SendInput error " + hr);
                    }
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [DllImport("Kernel32.dll")]
        static extern int GetLastError();

        // Simluate MouseEvents

        enum InputType { INPUT_MOUSE = 0, INPUT_KEYBOARD = 1, INPUT_HARDWARE = 2 };

        enum MouseFlags
        {
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
            MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000, /* The WM_MOUSEMOVE messages will not be coalesced. The default behavior is to coalesce WM_MOUSEMOVE messages. */
            MOUSEEVENTF_VIRTUALDESK = 0x4000, /* map to entire virtual desktop */
            MOUSEEVENTF_ABSOLUTE = 0x8000, /* absolute move */
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MouseInput
        {
            public int type;
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        };

        [StructLayout(LayoutKind.Sequential)]
        struct MouseMovePoint
        {
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
        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;

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
