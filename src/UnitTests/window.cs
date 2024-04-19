using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;
using WindowsInput;

namespace UnitTests
{
    static class AutomationExtensions
    {
        public static IEnumerable<AutomationElement> ToList(this AutomationElementCollection collection)
        {
            foreach (var item in collection)
            {
                if (item is AutomationElement e)
                {
                    yield return e;
                }
            }
        }
    }

    public class Window : IDisposable
    {
        private readonly Process _process;
        private readonly IntPtr _handle;
        private bool _closed;
        private TestBase _test;
        private readonly AutomationWrapper _acc;
        private Dictionary<string, AutomationWrapper> _menuItems;
        private readonly Window _parent;
        private bool _disposed;
        private Dictionary<string, List<string>> _menuHeirarchy;
        private const int MenuDelay = 100;

        public InputSimulator sim;

        public Window(Window parent, IntPtr handle, InputSimulator sim)
        {
            this._parent = parent;
            this._handle = handle;
            this.sim = sim;
            this._acc = AutomationWrapper.AccessibleObjectForWindow(handle);
        }

        public Window(Process p, InputSimulator sim, string className, string rootElementName)
        {
            this.sim = sim;
            this._process = p;
            IntPtr h = p.Handle;
            while (h == IntPtr.Zero || !p.Responding)
            {
                Sleep(1000);
                p.WaitForInputIdle();
                h = p.Handle;
                if (p.HasExited)
                {
                    throw new InvalidOperationException(string.Format("Process '{0}' has exited!", p.StartInfo.FileName));
                }
            }
            p.WaitForInputIdle();
            p.Exited += new EventHandler(OnExited);
            int id = p.Id;
            if (this._acc == null)
            {
                // p.MainWindowHandle always returns 0 for some unknown reason...
                int retries = 20;
                while (retries-- > 0 && this._acc == null)
                {
                    try
                    {
                        this._acc = FindWindowForProcessId(id, className, rootElementName);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error finding window for process id : " + ex.Message);
                    }
                    Sleep(1000);
                }
                if (this._acc == null)
                {
                    throw new Exception("Process as no window handle");
                }

                this._handle = this._acc.Hwnd;
            }
        }

        void OnExited(object sender, EventArgs e)
        {
            if (!_disposed && !_closed)
            {
                throw new Exception("Unexpected process exit.");
            }
        }

        public static AutomationWrapper FindWindowForProcessId(int id, string windowClassName, string name)
        {
            // Hmmm, try and find window for this process then.
            IntPtr hwnd = GetWindow(GetDesktopWindow(), GetWindowOptions.Child);
            while (hwnd != IntPtr.Zero)
            {
                GetWindowThreadProcessId(hwnd, out int procid);
                if (procid == id)
                {
                    AutomationWrapper acc = AutomationWrapper.AccessibleObjectForWindow(hwnd);
                    if (acc != null && (windowClassName == null || acc.ClassName == windowClassName) &&
                        (name == null || acc.Name == name))
                    {
                        if (IsWindowVisible(hwnd))
                        {
                            return acc;
                        }
                    }
                }
                hwnd = GetWindow(hwnd, GetWindowOptions.Next);
            }
            return null;
        }

        public AutomationWrapper FindDescendant(string name)
        {
            return this._acc.FindDescendant(name);
        }

        public AutomationWrapper FindDescendant(string name, ControlType controlType)
        {
            AutomationElement e = this._acc.AutomationElement.FindFirst(TreeScope.Descendants,
                new AndCondition(new PropertyCondition(AutomationElement.NameProperty, name),
                                 new PropertyCondition(AutomationElement.ControlTypeProperty, controlType)));
            if (e == null)
            {
                throw new Exception(string.Format("Control of type {0} named '{1}' not found", controlType.LocalizedControlType, name));
            }
            return new AutomationWrapper(e);
        }

        public AutomationWrapper FindDescendant(ControlType controlType)
        {
            AutomationElement e = this._acc.AutomationElement.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));
            if (e == null)
            {
                throw new Exception(string.Format("Control of type {0} not found", controlType.LocalizedControlType));
            }
            return new AutomationWrapper(e);
        }

        public AutomationWrapper GetTitleBar()
        {
            AutomationElement e = this._acc.AutomationElement.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "TitleBar"));
            if (e == null)
            {
                throw new Exception("Window has no 'TitleBar'");
            }
            return new AutomationWrapper(e);
        }

        public FileDialogWrapper WaitForFileDialog()
        {
            var window = WaitForPopup();
            return new FileDialogWrapper(window);
        }

        public Window WaitForPopup()
        {
            return WaitForPopup(IntPtr.Zero);
        }

        public Window TryWaitForMessageBox(string name, int timeout = 50, int retries = 5)
        {
            Sleep(100);
            Window found = null;
            IntPtr h = this.Handle;
            for (int retry = 0; retry < retries; retry++)
            {
                try
                {
                    IntPtr popup = GetLastActivePopup(h);
                    if (popup != h && popup != IntPtr.Zero)
                    {
                        found = new Window(this, popup, sim);
                    }
                    else
                    {
                        IntPtr hwnd = GetForegroundWindow();
                        if (hwnd != h && hwnd != this.Handle)
                        {
                            found = new Window(this, hwnd, sim);
                        }
                    }
                    if (found != null)
                    {
                        Sleep(100);
                        var bounds = found.GetWindowBounds();
                        if (bounds.Width == 0 && bounds.Height == 0)
                        {
                            // can't be it!
                            found = null;
                        }
                        else if (found.GetWindowText() != name)
                        {
                            found = null;
                        }
                    }
                }
                catch
                {
                    // unrecognized window, perhaps a temp window...
                }
                if (found == null)
                {
                    Sleep(timeout);
                }
            }
            if (found != null)
            {
                found.WaitForInteractive();
                return found;
            }
            return null;
        }

        public Window WaitForPopup(IntPtr excludingThisWindow)
        {
            Sleep(100);
            Window found = null;
            IntPtr h = this.Handle;
            int retries = 10;
            while (retries-- > 0 && found == null)
            {
                try
                {
                    IntPtr popup = GetLastActivePopup(h);
                    if (popup != h && popup != excludingThisWindow && popup != IntPtr.Zero)
                    {
                        found = new Window(this, popup, sim);
                    }
                    else
                    {
                        IntPtr hwnd = GetForegroundWindow();
                        if (hwnd != h && hwnd != excludingThisWindow)
                        {
                            found = new Window(this, hwnd, sim);
                        }
                    }
                    if (found != null)
                    {
                        Sleep(100);
                        var bounds = found.GetWindowBounds();
                        if (bounds.Width == 0 && bounds.Height == 0)
                        {
                            // can't be it!
                            found = null;
                        }
                    }
                }
                catch
                {
                    // unrecognized window, perhaps a temp window...
                }
                if (found == null)
                {
                    Sleep(1000);
                }
            }
            if (found != null)
            {
                found.WaitForInteractive();
                Trace.WriteLine("WaitForPopup found: '" + found.GetWindowText() + "', at:" + found.GetWindowBounds().ToString());
                return found;
            }
            throw new ApplicationException("Window is not appearing!");
        }

        public static IntPtr GetForegroundWindowHandle()
        {
            return GetForegroundWindow();
        }

        public static string GetForegroundWindowText()
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return "";
            return GetWindowText(hwnd);
        }

        public string GetWindowText()
        {
            return GetWindowText(this._handle);
        }

        public static string GetWindowText(IntPtr hwnd)
        {
            int len = GetWindowTextLength(hwnd);
            if (len <= 0) return "";
            len++; // include space for the null terminator.
            IntPtr buffer = Marshal.AllocCoTaskMem(len * 2);
            GetWindowText(hwnd, buffer, len);
            string s = Marshal.PtrToStringUni(buffer, len - 1);
            Marshal.FreeCoTaskMem(buffer);
            return s;
        }

        /// <summary>
        /// Dismiss the popup that is on top of this window and wait for this window to be reactivated.
        /// </summary>
        /// <param name="keys"></param>
        public void DismissPopUp(string keys)
        {
            Sleep(1000);
            this.Activate();

            IntPtr h = this._handle;
            IntPtr popup = GetLastActivePopup(h);
            string current = GetWindowText(popup);
            Trace.WriteLine("Dismissing: " + GetForegroundWindowText());
            SendKeystrokes(keys);
            Sleep(1000);
            int retries = 10;
            while (retries-- > 0)
            {
                IntPtr hwnd = GetLastActivePopup(h);
                if (popup != h && h == hwnd)
                {
                    Trace.WriteLine("Popup is disappeared:" + current);
                    Sleep(500); // give it time to get keystrokes!
                    return;

                }
                Trace.WriteLine("ForegroundWindow=" + GetForegroundWindowText());
                hwnd = GetForegroundWindow();
                if (hwnd != h)
                {
                    Trace.WriteLine("WindowChanged:" + GetForegroundWindowText());
                    Sleep(500); // give it time to get keystrokes!
                    return;
                }
                Sleep(200);
            }
            throw new ApplicationException("Popup is not dismissing!");
        }

        public Window ExpectingPopup(string name)
        {
            Application.DoEvents();
            Window popup = null;
            string text = "";
            int retries = 3;
            while (text.ToLowerInvariant() != name.ToLowerInvariant() && retries-- > 0)
            {
                popup = this.WaitForPopup();
                if (popup != null)
                {
                    text = popup.GetWindowText();
                }
            }
            if (text.ToLowerInvariant() != name.ToLowerInvariant())
            {
                if (popup != null)
                {
                    popup.Close();
                }
                throw new ApplicationException(string.Format("Expecting popup '{0}'", name));
            }
            return popup;
        }


        public AutomationWrapper AccessibleObject
        {
            get
            {
                return this._acc;
            }
        }

        public AutomationWrapper FindMenuItem(string name)
        {
            ReloadMenuItems(name);
            return _menuItems[name];
        }

        void LoadMenuItems(AutomationElement menuItem, List<string> names)
        {
            // On .NET 4.8 menus have to be expanded before you can find their children.
            ExpandCollapsePattern pattern = null;
            if (menuItem.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out object patternObject))
            {
                pattern = (ExpandCollapsePattern)patternObject;
                pattern.Expand();
            }
            foreach (AutomationElement subMenuItem in menuItem.FindAll(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem)))
            {
                var childId = subMenuItem.Current.Name;
                names.Add(childId);
                _menuItems[childId] = new AutomationWrapper(subMenuItem);
                LoadMenuItems(subMenuItem, names);
            }
            if (pattern != null) { 
                pattern.Collapse();
            }
        }

        void ReloadMenuItems(string name)
        {
            if (_menuItems == null)
            {
                _menuItems = new Dictionary<string, AutomationWrapper>();
            }
            if (_menuHeirarchy == null)
            {
                _menuHeirarchy = new Dictionary<string, List<string>>();
            }

            if (_menuItems.Count == 0 || !_menuItems.ContainsKey(name))
            {
                var menuStrip = this._acc.AutomationElement.FindFirst(TreeScope.Children,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuBar));

                // enumerate the menu bar start filling out the _menuHeirarchy.
                // Note: starting in .NET 4.8 one has to literally expand the menu on screen to
                // find the menu items inside!
                foreach (AutomationElement menuItem in menuStrip.FindAll(TreeScope.Children,
                       new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.MenuItem)))
                {
                    var id = menuItem.Current.Name;
                    if (!_menuHeirarchy.ContainsKey(id))
                    {
                        List<string> names = new List<string>();
                        LoadMenuItems(menuItem, names);
                        _menuHeirarchy[id] = names;
                        if (names.Contains(name))
                        {
                            // found it!
                            break;
                        }
                    }
                }

                var toolStrip = this._acc.AutomationElement.FindFirst(TreeScope.Children,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ToolBar));

                // enumerate the toolbar bar and fill out the _menuHeirarchy.
                // Note: starting in .NET 4.8 one has to literally expand the menu on screen to
                // find the menu items inside!
                if (toolStrip != null && !_menuHeirarchy.ContainsKey(toolStrip.Current.Name))
                {
                    List<string> names = new List<string>();
                    _menuHeirarchy[toolStrip.Current.Name] = names;
                    foreach (AutomationElement menuItem in toolStrip.FindAll(TreeScope.Children,
                            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)))
                    {
                        var childId = menuItem.Current.Name;
                        names.Add(childId);
                        _menuItems[childId] = new AutomationWrapper(menuItem);
                    }
                }
            }

            if (!_menuItems.ContainsKey(name))
            {
                throw new Exception($"Could not find menu item {name}");
            }
        }

        public IntPtr Handle
        {
            get
            {
                return _handle;
            }
        }

        public TestBase TestBase
        {
            get { return _test; }
            set { _test = value; }
        }

        public void SetWindowSize(int cx, int cy)
        {
            var wb = GetWindowBounds();
            Screen s = Screen.FromHandle(this.Handle);
            int x = wb.Left;
            int y = wb.Top;
            if (wb.Left + cx > s.WorkingArea.Right)
            {
                // move window left so it stays on screen.
                x = s.WorkingArea.Right - cx;
            }
            if (wb.Top + cy > s.WorkingArea.Bottom)
            {
                // move window left so it stays on screen.
                y = s.WorkingArea.Bottom - cy;
            }
            SetWindowPos(this._handle, IntPtr.Zero, x, y, cx, cy, 0);
        }

        public void SetWindowPosition(int x, int y)
        {
            SetWindowPos(this._handle, IntPtr.Zero, x, y, 0, 0, (uint)SetWindowPosFlags.SWP_NOSIZE);
        }

        public void WaitForIdle(int timeout)
        {
            if (_parent != null)
                _parent.WaitForIdle(timeout);
            else if (_process != null && !_process.HasExited)
                _process.WaitForInputIdle(timeout);
        }

        public void WaitForInteractive(int retries = 5, int delay = 50)
        {
            WaitForIdle(200);
            WindowPattern wp = (WindowPattern)_acc.AutomationElement.GetCurrentPattern(WindowPattern.Pattern);
            WindowInteractionState state = WindowInteractionState.NotResponding;
            for (; retries > 0; retries--)
            {
                state = wp.Current.WindowInteractionState;
                if (state == WindowInteractionState.ReadyForUserInteraction)
                {
                    return;
                }
                try
                {
                    Thread.Sleep(delay);
                    this._acc.SetFocus();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Cannot set focus: " + ex.Message);
                }
            }
            throw new Exception("Timeout waiting for window to be ready for user interaction, it is stuck in state: " + state);
        }

        public bool Closed
        {
            get { return _closed; }
            set { _closed = value; }
        }

        public void Sleep(int ms)
        {
            Thread.Sleep(ms);
        }

        public void Close()
        {
            if (!_closed)
            {
                PostMessage(this._handle, (uint)WindowsMessages.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                Sleep(MenuDelay);
            }
            _closed = true;
        }

        public Window OpenDialog(string menuCommand, string formName)
        {
            InvokeAsyncMenuItem(menuCommand);
            return this.WaitForPopup();
        }

        public void SendKeystrokes(string keys)
        {
            if (Window.GetForegroundWindow() != this._handle &&
                Window.GetLastActivePopup(Window.GetForegroundWindow()) != this._handle)
            {
                Activate();
                Sleep(1000);
            }
            Debug.WriteLine("Sending keys: " + keys);
            System.Windows.Forms.SendKeys.SendWait(keys);
            this.WaitForIdle(100); // wait for it to behandled by the app
        }


        public void Activate()
        {
            Debug.WriteLine("Activating window");
            ShowWindow(this._handle, (int)ShowWindowFlags.SW_SHOW);
            SetActiveWindow(this._handle);
            SetForegroundWindow(this._handle);
            ShowWindow(this._handle, (int)ShowWindowFlags.SW_SHOW);
        }

        // const uint OBJID_CLIENT = 0xFFFFFFFC;

        public void InvokeMenuItem(string menuItemName)
        {
            this.ReloadMenuItems(menuItemName);
            Sleep(30);
            this.WaitForIdle(2000);
            if (!this._menuItems.TryGetValue(menuItemName, out AutomationWrapper item))
            {
                throw new Exception(string.Format("Menu item '{0}' not found", menuItemName));
            }
            Trace.WriteLine("InvokeMenuItem(" + menuItemName + ")");
            item.Invoke();
            this.WaitForIdle(1000);
        }

        public void InvokeAsyncMenuItem(string menuItemName)
        {
            this.ReloadMenuItems(menuItemName);
            Sleep(MenuDelay);
            this.WaitForIdle(2000);

            if (!this._menuItems.TryGetValue(menuItemName, out AutomationWrapper item))
            {
                throw new Exception(string.Format("Menu item '{0}' not found", menuItemName));
            }
            Trace.WriteLine("InvokeAsyncMenuItem(" + menuItemName + ")");

            // this is NOT async with things like SaveAs where a dialog pops up!
            //item.DoDefaultAction();

            this.WaitForIdle(1000);
            Sleep(1000);
            // BUFBUG: This blocks on .NET 4.8 but works fine on .NET 4.7.2
            // See https://github.com/dotnet/winforms/issues/10244.
            item.Invoke();
            this.WaitForIdle(1000);
        }

        //Point Center(Rectangle bounds)
        //{
        //    return new Point(bounds.Left + (bounds.Width / 2),
        //        bounds.Top + (bounds.Height / 2));
        //}


        void TypeShortcut(AccessibleObject item)
        {

            Sleep(1000);


            AccessibleObject parent = item.Parent;
            if (parent.Role == AccessibleRole.MenuItem)
            {
                TypeShortcut(parent);
            }
            string shortcut = item.KeyboardShortcut;
            if (!string.IsNullOrEmpty(shortcut))
            {
                SendShortcut(shortcut);
            }
            else
            {
                throw new NotImplementedException("InvokeAsyncMenuItem can't work without menu item shortcuts");
            }

            if (item.Role == AccessibleRole.MenuItem)
            {
                // this isn't working for some reason.
                // WaitForPopupMenu();
                Sleep(200);
            }
        }

        public void SendShortcut(string shortcut)
        {
            string keys = shortcut.Replace("Alt+", "%").Replace("Ctrl+", "^").Replace("Shift+", "+");
            SendKeystrokes(keys);
        }

        public Rectangle GetScreenBounds()
        {
            WINDOWINFO wi = new WINDOWINFO();
            wi.cbSize = Marshal.SizeOf(wi);
            if (GetWindowInfo(this._handle, ref wi))
            {
                RECT r = wi.rcWindow;
                return new Rectangle(r.left, r.top, r.right - r.left, r.bottom - r.top);
            }
            return Rectangle.Empty;
        }

        public Rectangle GetClientBounds()
        {
            WINDOWINFO wi = new WINDOWINFO();
            wi.cbSize = Marshal.SizeOf(wi);
            if (GetWindowInfo(this._handle, ref wi))
            {
                RECT r = wi.rcClient;
                return new Rectangle(r.left, r.top, r.right - r.left, r.bottom - r.top);
            }
            return Rectangle.Empty;
        }

        public Rectangle GetWindowBounds()
        {
            GetWindowRect(this._handle, out RECT r);
            return new Rectangle(r.left, r.top, r.right - r.left, r.bottom - r.top);
        }

        public void Dispose(bool terminating)
        {
            _disposed = true;
            if (_process != null && !_process.HasExited)
            {
                for (int i = 0; i < 5; i++)
                {
                    this.Close();
                    Sleep(500);
                    if (_process.HasExited)
                    {
                        break;
                    }
                }
                if (!_process.HasExited)
                {
                    if (terminating)
                    {
                        _process.Kill();
                    }
                    else
                    {
                        throw new Exception("Application is not terminating!");
                    }
                }
            }
        }


        #region IDisposable Members

        public void Dispose()
        {
            Dispose(false);
        }

        #endregion

        enum GetWindowOptions
        {
            First = 0,
            Last = 1,
            Next = 2,
            Previous = 3,
            Owner = 5,
            Child = 5
        }

        [DllImport("User32")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("User32")]
        public static extern IntPtr GetTopWindow(IntPtr hwnd);

        [DllImport("User32")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("User32")]
        public static extern IntPtr GetLastActivePopup(IntPtr hWnd);

        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern int GetWindowTextLength(IntPtr hwnd);

        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern int GetWindowText(IntPtr hWnd, IntPtr lpString, int nMaxCount);

        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern int GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern IntPtr GetWindow(IntPtr hWnd, GetWindowOptions uCmd);

        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShows);

        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern bool SetActiveWindow(IntPtr hWnd);



        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern void SetForegroundWindow(IntPtr hWnd);

        enum WindowsMessages
        {
            WM_CLOSE = 0x0010
        }

        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);


        [DllImport("User32", CharSet = CharSet.Unicode)]
        static extern int PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);


        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWINFO
        {
            public int cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public int dwStyle;
            public int dwExStyle;
            public int dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public IntPtr atomWindowType;
            public int wCreatorVersion;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        };


        enum SetWindowPosFlags
        {
            SWP_NOSIZE = 0x0001,
            SWP_NOMOVE = 0x0002,
            SWP_NOZORDER = 0x0004,
            SWP_NOREDRAW = 0x0008,
            SWP_NOACTIVATE = 0x0010,
            SWP_FRAMECHANGED = 0x0020,  /* The frame changed: send WM_NCCALCSIZE */
            SWP_SHOWWINDOW = 0x0040,
            SWP_HIDEWINDOW = 0x0080,
            SWP_NOCOPYBITS = 0x0100,
            SWP_NOOWNERZORDER = 0x0200, /* Don't do owner Z ordering */
            SWP_NOSENDCHANGING = 0x0400,  /* Don't send WM_WINDOWPOSCHANGING */
            SWP_DRAWFRAME = SWP_FRAMECHANGED,
            SWP_NOREPOSITION = SWP_NOOWNERZORDER
        }

        enum ShowWindowFlags
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11,
            SW_MAX = 11
        }


        internal AutomationWrapper FindPopup(string name)
        {
            var w = TryFindPopup(name, 200, 5);
            if (w == null)
            {
                throw new Exception(string.Format("Popup '{0}' not found", name));
            }
            return w;
        }

        internal AutomationWrapper TryFindPopup(string name, int timeout = 200, int retries = 2)
        {
            AutomationElement desktop = AutomationElement.FromHandle(GetDesktopWindow());
            for (int r = 0; r < retries; r++)
            {
                AutomationElement e = desktop.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, name, PropertyConditionFlags.IgnoreCase));
                if (e != null)
                {
                    return new AutomationWrapper(e);
                }
                Sleep(timeout);
            }

            return null;
        }
    }
}
