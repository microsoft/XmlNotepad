using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace XmlNotepad {
    public enum TipRequestType { Default, Hover };

    public class IntelliTipEventArgs : EventArgs {
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
    public class IntelliTip {

        private Control owner;
        private ToolTip tip = new ToolTip();
        int tipTime;
        List<Control> watch = new List<Control>();
        bool tipVisible;
        TipRequestType type;
        Timer popupDelay;
        bool resetpending = true;
        Rectangle lastHover;
        Control showing;
        int hoverWidth;
        int hoverHeight;
        bool tracking;

        public event IntelliTipEventHandler ShowToolTip;

        public IntelliTip(Control owner) {
            this.owner = owner;
            this.tip.Popup += new PopupEventHandler(OnTipPopup);
            
            this.popupDelay = new Timer();
            this.popupDelay.Tick += new EventHandler(popupDelay_Tick);
            this.popupDelay.Interval = GetMouseHoverTime();
            this.hoverWidth = GetMouseHoverWidth();
            this.hoverHeight = GetMouseHoverHeight();

            this.tip.AutoPopDelay = 0;
            this.tip.AutomaticDelay = 0;
            this.tip.UseAnimation = false;
            this.tip.UseFading = false;
        }

        internal void Close()
        {
            Stop();
            foreach (Control c in watch)
            {
                c.MouseMove -= new MouseEventHandler(OnWatchMouseMove);
                c.KeyDown -= new KeyEventHandler(OnWatchKeyDown);
                c.MouseLeave -= new EventHandler(OnWatchMouseLeave);
            }
            watch.Clear();
        }

        public int PopupDelay {
            get { return this.popupDelay.Interval; }
            set { this.popupDelay.Interval = value; }
        }

        public void AddWatch(Control c) {
            c.MouseMove += new MouseEventHandler(OnWatchMouseMove);
            c.KeyDown += new KeyEventHandler(OnWatchKeyDown);
            watch.Add(c);
        }

        public bool Visible {
            get { return this.tipVisible; }
        }

        public void Hide() {
            if (showing != null) {
                this.tip.Hide(showing);
            }
            showing = null;
            this.tip.RemoveAll();                    
            this.tipVisible = false;
        }

        //=============================== Private methods ===============================
        void popupDelay_Tick(object sender, EventArgs e)
        {
            this.type = TipRequestType.Hover;
            popupDelay.Stop();
            this.owner.Invoke(new EventHandler(OnPopupDelay), new object[] { this, EventArgs.Empty });
        }

        void OnPopupDelay(object sender, EventArgs e) {
            this.OnShowToolTip();
        }

        void OnTipPopup(object sender, PopupEventArgs e) {
            this.tipVisible = true;
        }

        void OnWatchKeyDown(object sender, KeyEventArgs e) {
            Hide();  
        }

        void Start() {
            this.popupDelay.Stop();
            this.popupDelay.Start();
        }
        void Stop()
        {
            this.popupDelay.Stop();
        }

        Control GetFocus() {
            foreach (Control c in this.watch) {
                if (c.Focused) return c;
            }
            return this.owner;
        }

        internal void OnShowToolTip() {

            if (ShowToolTip != null && !owner.Capture) {
                Control c = GetFocus();
                Point local = c.PointToClient(Cursor.Position);
                IntelliTipEventArgs args = new IntelliTipEventArgs();
                args.Type = this.type;
                args.Focus = c;
                args.Location = local;
                ShowToolTip(this, args);
                string toolTip = args.ToolTip;
                if (!string.IsNullOrEmpty(toolTip)) {
                    this.tip.ShowAlways = true;
                    this.tip.Active = true;
                    Point p = args.Location;
                    if (p.X == local.X && p.Y == local.Y) {
                        p.Y += 10;
                        p.Y += 10;
                    }
                    this.tipTime = Environment.TickCount;
                    showing = c;
                    this.tip.Show(WordWrap(toolTip), (IWin32Window)c, p);
                    return;
                }
            }
            this.tip.Hide(owner);
            this.type = TipRequestType.Default;
        }

        private void OnWatchMouseLeave(object sender, EventArgs e)
        {
            Control c = (Control)sender;
            Stop();
            Hide();
            resetpending = true;
            tracking = false;
            c.MouseLeave -= new EventHandler(OnWatchMouseLeave);
        }

        void OnWatchMouseMove(object sender, MouseEventArgs e) {

            Control c = (Control)sender;
            if (!tracking)
            {
                if (!watch.Contains(c))
                {
                    Debug.WriteLine("????");
                }
                tracking = true;
                TrackMouseLeave(c.Handle);

                c.MouseLeave += new EventHandler(OnWatchMouseLeave);
            }
            bool outside = !lastHover.Contains(Cursor.Position);
            if (outside)
            {
                // if mouse moves outside the hover rect then we are not hovering.
                Stop();
                Hide();
                resetpending = true;
            }

            if (!this.tipVisible)
            {
                if (resetpending)
                {
                    resetpending = false;
                    lastHover = new Rectangle(Cursor.Position, Size.Empty);
                    lastHover.Inflate(this.hoverWidth / 2, this.hoverHeight / 2);
                    // start the timer, if we remain inside this hover rect until timer
                    // fires, then show tooltip.  
                    Start(); 
                }                
            }

        }

        string WordWrap(string tip) {
            Screen screen = Screen.FromControl(owner);
            int width = screen.Bounds.Width / 2;
            StringBuilder sb = new StringBuilder();
            using (Graphics g = owner.CreateGraphics()) {
                Font f = owner.Font;
                int wrap = 0;
                foreach (string word in tip.Split(' ', '\t', '\r', '\n')) {
                    if (string.IsNullOrEmpty(word)) continue;
                    SizeF size = g.MeasureString(word + " ", f);
                    wrap += (int)size.Width;
                    sb.Append(word);
                    if (wrap > width) {
                        sb.Append('\n');
                        wrap = 0;
                    } else {
                        sb.Append(' ');
                    }
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
