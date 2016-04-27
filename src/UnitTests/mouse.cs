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
    public class Mouse {
        static int Timeout = 100;

        private Mouse() { }

        public static void MouseDown(Point pt, MouseButtons buttons) {
            MouseInput input = new MouseInput();
            input.type = (int)InputType.INPUT_MOUSE;
            input.dx = pt.X;
            input.dy = pt.Y;
            input.dwFlags = (int)GetMouseDownFlags(buttons);
            input.dwFlags |= (int)MouseFlags.MOUSEEVENTF_ABSOLUTE;
            SendInput(input);
        }

        private static MouseFlags GetMouseDownFlags(MouseButtons buttons) {
            MouseFlags flags = 0;
            if ((buttons & MouseButtons.Left) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_LEFTDOWN;
            }
            if ((buttons & MouseButtons.Right) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_RIGHTDOWN;
            }
            if ((buttons & MouseButtons.Middle) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_MIDDLEDOWN;
            }
            if ((buttons & MouseButtons.XButton1) != 0) {
                flags |= MouseFlags.MOUSEEVENTF_XDOWN;
            }
            return flags;
        }

        public static void MouseUp(Point pt, MouseButtons buttons) {
            MouseInput input = new MouseInput();
            input.type = (int)InputType.INPUT_MOUSE;
            input.dx = pt.X;
            input.dy = pt.Y;
            MouseFlags flags = MouseFlags.MOUSEEVENTF_ABSOLUTE;
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
            MouseClick(pt, buttons);
        }

        public static void MouseMoveBy(int dx, int dy, MouseButtons buttons) {
            MouseInput input = new MouseInput();
            input.type = (int)(InputType.INPUT_MOUSE);
            input.dx = dx;
            input.dy = dy;
            input.dwFlags = (int)MouseFlags.MOUSEEVENTF_MOVE;
            SendInput(input);
            Application.DoEvents();
        }

        public static void MouseDragDrop(Point start, Point end, int step, MouseButtons buttons) {
            int s = Timeout;
            Timeout = 10;
            MouseDown(start, buttons);
            Application.DoEvents();
            Thread.Sleep(200);
            MouseDragTo(start, end, step, buttons);
            Thread.Sleep(200);
            MouseUp(end, buttons);
            Application.DoEvents();
            Thread.Sleep(200);
            Timeout = s;
        }

        public static void MouseMoveTo(Point start, Point end, int step) {
            MouseDragTo(start, end, step, MouseButtons.None);
        }

        public static void MouseDragTo(Point start, Point end, int step, MouseButtons buttons) {
            // Interpolate and move mouse smoothly over to given location.                
            int dx = end.X - start.X;
            int dy = end.Y - start.Y;
            int length = (int)Math.Sqrt((dx * dx) + (dy * dy));
            step = Math.Abs(step);
            int s = Timeout;
            Timeout = 10;
            Application.DoEvents();
            int lastx = start.X;
            int lasty = start.Y;
            Point pos;
            for (int i = 0; i < length; i += step) {
                int tx = start.X + (dx * i) / length;
                int ty = start.Y + (dy * i) / length;
                int mx = tx - lastx;
                int my = ty - lasty;
                if (mx != 0 || my != 0) {
                    MouseMoveBy(mx, my, buttons);
                }

                // Now calibrate movement based on current mouse position.
                Application.DoEvents();
                pos = Control.MousePosition;
                lastx = pos.X;
                lasty = pos.Y;
            }
            pos = Control.MousePosition;
            dx = pos.X - end.X;
            dy = pos.Y - end.Y;
            MouseMoveBy(dx, dy, buttons);
            Application.DoEvents();

            Timeout = s;
        }

        public static void MouseWheel(int clicks) {
            MouseInput input = new MouseInput();
            input.type = (int)InputType.INPUT_MOUSE;
            input.mouseData = clicks;
            MouseFlags flags = MouseFlags.MOUSEEVENTF_WHEEL;
            input.dwFlags = (int)flags;
            Point c = Cursor.Position;
            input.dx = c.X;
            input.dy = c.Y;
            SendInput(input);
        }

        static void SendInput(MouseInput input) {
            //Trace.WriteLine("SendInput:" + input.dx + "," + input.dy + " cursor is at " + Cursor.Position.X + "," + Cursor.Position.Y);
            if ((input.dwFlags & (int)MouseFlags.MOUSEEVENTF_ABSOLUTE) != 0) {
                Cursor.Position = new Point(input.dx, input.dy);
            }
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

    }

}
