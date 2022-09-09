using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace XmlNotepad
{
    public enum TipRequestType { Default, Hover };

    public class IntelliTipEventArgs : EventArgs
    {
        public TipRequestType Type;
        public string ToolTip;
        public Point Location;
        public Control Focus;
    }
    public delegate void IntelliTipEventHandler(object sender, IntelliTipEventArgs args);

    /// <summary>
    /// This class provides a ToolTip at the cursor location based on mouse hover events
    /// on the watched child views.  It is wraps the WinForms ToolTip class and provides
    /// some added benefits, like being able to monitor multiple child views, and being
    /// able to Start() the tip operation based on some other event, (like list box
    /// selection changed) and word wrapping of the tooltip text string.
    /// </summary>
    public class IntelliTip
    {
        private Control _owner;
        private ToolTip _tip = new ToolTip();
        private List<Control> _watch = new List<Control>();
        private bool _tipVisible;
        private TipRequestType _type;
        private Timer _popupDelay;
        private bool _resetpending = true;
        private Rectangle _lastHover;
        private Control _showing;
        private int _hoverWidth;
        private int _hoverHeight;
        private bool _tracking;

        public event IntelliTipEventHandler ShowToolTip;

        public IntelliTip(Control owner)
        {
            this._owner = owner;
            this._tip.Popup += new PopupEventHandler(OnTipPopup);

            this._popupDelay = new Timer();
            this._popupDelay.Tick += new EventHandler(popupDelay_Tick);
            this._popupDelay.Interval = GetMouseHoverTime();
            this._hoverWidth = GetMouseHoverWidth();
            this._hoverHeight = GetMouseHoverHeight();

            this._tip.AutoPopDelay = 0;
            this._tip.AutomaticDelay = 0;
            this._tip.UseAnimation = false;
            this._tip.UseFading = false;
        }

        internal void Close()
        {
            Stop();
            foreach (Control c in _watch)
            {
                c.MouseMove -= new MouseEventHandler(OnWatchMouseMove);
                c.KeyDown -= new KeyEventHandler(OnWatchKeyDown);
                c.MouseLeave -= new EventHandler(OnWatchMouseLeave);
            }
            _watch.Clear();
        }

        public int PopupDelay
        {
            get { return this._popupDelay.Interval; }
            set { this._popupDelay.Interval = value; }
        }

        public void AddWatch(Control c)
        {
            c.MouseMove += new MouseEventHandler(OnWatchMouseMove);
            c.KeyDown += new KeyEventHandler(OnWatchKeyDown);
            _watch.Add(c);
        }

        public bool Visible
        {
            get { return this._tipVisible; }
        }

        public void Hide()
        {
            if (_showing != null)
            {
                this._tip.Hide(_showing);
            }
            _showing = null;
            this._tip.RemoveAll();
            this._tipVisible = false;
        }

        //=============================== Private methods ===============================
        void popupDelay_Tick(object sender, EventArgs e)
        {
            this._type = TipRequestType.Hover;
            _popupDelay.Stop();
            this._owner.Invoke(new EventHandler(OnPopupDelay), new object[] { this, EventArgs.Empty });
        }

        void OnPopupDelay(object sender, EventArgs e)
        {
            this.OnShowToolTip();
        }

        void OnTipPopup(object sender, PopupEventArgs e)
        {
            this._tipVisible = true;
        }

        void OnWatchKeyDown(object sender, KeyEventArgs e)
        {
            Hide();
        }

        void Start()
        {
            this._popupDelay.Stop();
            this._popupDelay.Start();
        }
        void Stop()
        {
            this._popupDelay.Stop();
        }

        Control GetFocus()
        {
            foreach (Control c in this._watch)
            {
                if (c.Focused) return c;
            }
            return this._owner;
        }

        internal void OnShowToolTip()
        {

            if (ShowToolTip != null && !_owner.Capture)
            {
                Control c = GetFocus();
                Point local = c.PointToClient(Cursor.Position);
                IntelliTipEventArgs args = new IntelliTipEventArgs();
                args.Type = this._type;
                args.Focus = c;
                args.Location = local;
                ShowToolTip(this, args);
                string toolTip = args.ToolTip;
                if (!string.IsNullOrEmpty(toolTip))
                {
                    this._tip.ShowAlways = true;
                    this._tip.Active = true;
                    Point p = args.Location;
                    if (p.X == local.X && p.Y == local.Y)
                    {
                        p.Y += 10;
                        p.Y += 10;
                    }

                    _showing = c;
                    this._tip.Show(WordWrap(toolTip), (IWin32Window)c, p);
                    return;
                }
            }
            this._tip.Hide(_owner);
            this._type = TipRequestType.Default;
        }

        private void OnWatchMouseLeave(object sender, EventArgs e)
        {
            Control c = (Control)sender;
            Stop();
            Hide();
            _resetpending = true;
            _tracking = false;
            c.MouseLeave -= new EventHandler(OnWatchMouseLeave);
        }

        void OnWatchMouseMove(object sender, MouseEventArgs e)
        {

            Control c = (Control)sender;
            if (!_tracking)
            {
                if (!_watch.Contains(c))
                {
                    Debug.WriteLine("????");
                }
                _tracking = true;
                TrackMouseLeave(c.Handle);

                c.MouseLeave += new EventHandler(OnWatchMouseLeave);
            }
            Point local = c.PointToClient(Cursor.Position);
            bool outside = !_lastHover.Contains(local);
            if (outside)
            {
                // if mouse moves outside the hover rect then we are not hovering.
                Stop();
                Hide();
                _resetpending = true;
            }

            if (!this._tipVisible)
            {
                if (_resetpending)
                {
                    _resetpending = false;
                    _lastHover = new Rectangle(local, Size.Empty);
                    _lastHover.Inflate(this._hoverWidth / 2, this._hoverHeight / 2);
                    // start the timer, if we remain inside this hover rect until timer
                    // fires, then show tooltip.  
                    Start();
                }
            }

        }

        string WordWrap(string tip)
        {
            Screen screen = Screen.FromControl(_owner);
            int width = screen.Bounds.Width / 2;
            StringBuilder sb = new StringBuilder();
            using (Graphics g = _owner.CreateGraphics())
            {
                Font f = _owner.Font;
                int wrap = 0;
                int start = 0;
                int n = tip.Length;
                for (int i = 0; i < n; i++)
                {
                    char c = tip[i];
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                    {
                        // hit a whitespace, so time to insert the word if we have one.
                        if (i - start > 0)
                        {
                            var word = tip.Substring(start, i - start);
                            SizeF size = g.MeasureString(word + " ", f);
                            wrap += (int)size.Width;
                            bool wrapped = false;
                            if (wrap > width)
                            {
                                sb.AppendLine();
                                wrapped = true;
                                wrap = 0;
                            }
                            sb.Append(word);
                            if (!wrapped && (c == '\r' || c == '\n'))
                            {
                                sb.AppendLine();
                                wrap = 0;
                            }
                            else
                            {
                                sb.Append(' ');
                            }
                            if (c == '\r' && i + 1 < n && tip[i + 1] == '\n')
                            {
                                i++; // skip \r\n pair.
                            }
                        }
                        start = i + 1;
                    }
                    else
                    {
                        // scanning over a word.
                    }
                }
                if (n - start > 0)
                {
                    var word = tip.Substring(start, n - start);
                    SizeF size = g.MeasureString(word + " ", f);
                    wrap += (int)size.Width;
                    if (wrap > width)
                    {
                        sb.AppendLine();
                    }
                    sb.Append(word);
                }
            }
            return sb.ToString();
        }


        #region Native Helpers

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fwinIni);

        const uint SPI_GETMOUSEHOVERTIME = 0x0066;
        const uint SPI_GETMOUSEHOVERHEIGHT = 0x0064;
        const uint SPI_GETMOUSEHOVERWIDTH = 0x0062;

        internal int GetSystemParameter(uint id)
        {
            int result = 300;
            int bytes = Marshal.SizeOf(typeof(uint));
            IntPtr ptr = Marshal.AllocCoTaskMem(bytes);
            if (SystemParametersInfo(id, 0, ptr, 0))
            {
                result = (int)Marshal.ReadInt32(ptr);
            }
            Marshal.FreeCoTaskMem(ptr);
            return result;
        }

        internal int GetMouseHoverTime()
        {
            return GetSystemParameter(SPI_GETMOUSEHOVERTIME);
        }

        internal int GetMouseHoverWidth()
        {
            return GetSystemParameter(SPI_GETMOUSEHOVERWIDTH);
        }
        internal int GetMouseHoverHeight()
        {
            return GetSystemParameter(SPI_GETMOUSEHOVERHEIGHT);
        }
        #endregion

        #region HoverTracking

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool TrackMouseEvent(TRACKMOUSEEVENT tme);

        [StructLayout(LayoutKind.Sequential)]
        public class TRACKMOUSEEVENT
        {
            public int cbSize = Marshal.SizeOf(typeof(TRACKMOUSEEVENT));
            public int dwFlags;
            public IntPtr hwndTrack;
            public int dwHoverTime;
        }

        TRACKMOUSEEVENT trackMouseEvent;
        const int TME_LEAVE = 0x00000002;

        internal void TrackMouseLeave(IntPtr handle)
        {
            if (trackMouseEvent == null)
            {
                trackMouseEvent = new TRACKMOUSEEVENT();
                trackMouseEvent.dwFlags = TME_LEAVE;
                trackMouseEvent.hwndTrack = handle;
                trackMouseEvent.dwHoverTime = 0;
            }
            if (!TrackMouseEvent(trackMouseEvent))
            {
                Debug.WriteLine("TrackMouseEvent failed, rc=" + Marshal.GetHRForLastWin32Error());
            }
        }

        #endregion

    }
}
