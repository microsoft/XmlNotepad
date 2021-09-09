using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

namespace XmlNotepad
{

    public class PaneResizer : Control
    {
        // panes could be horizontally aligned, or vertically aligned, 
        // it will do the right thing.
        private Control _pane1;
        private Control _pane2;
        private Point _pos;
        private bool _vertical = true;
        private int _width = 5;
        private bool _down;
        private Rectangle _start;
        private Border3DStyle _style = Border3DStyle.Raised;
        private HatchControl _feedback;

        public int PaneWidth
        {
            get { return this._width; }
            set { this._width = value; }
        }

        public Control Pane1
        {
            get { return this._pane1; }
            set { this._pane1 = value; }
        }
        public Control Pane2
        {
            get { return this._pane2; }
            set { this._pane2 = value; }
        }

        public Border3DStyle Border3DStyle
        {
            get { return this._style; }
            set { this._style = value; }
        }

        public bool Vertical
        {
            get { return this._vertical; }
            set { this._vertical = value; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Border3DSide sides = this.Vertical ?
                (Border3DSide.Left | Border3DSide.Right) : (Border3DSide.Top | Border3DSide.Bottom);
            ControlPaint.DrawBorder3D(e.Graphics, 0, 0, this.Width, this.Height, this._style, sides);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            this.Cursor = this.Vertical ? Cursors.VSplit : Cursors.HSplit;
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            if (!this._down) this.Cursor = Cursors.Default;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (this._feedback == null)
            {
                this._feedback = new HatchControl();
                this.Parent.Controls.Add(this._feedback);
                int index = 0;
                foreach (Control c in this.Parent.Controls)
                {
                    if (c == this)
                        break;
                    index++;
                }
                this.Parent.Controls.SetChildIndex(this._feedback, index);
            }
            this._start = this.Bounds;
            this._feedback.Bounds = this.Bounds;
            this._pos = new Point(e.X, e.Y);
            this._down = true;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            this._down = false;
            HandleMove(e.X, e.Y);
            this.Parent.Controls.Remove(this._feedback);
            this.Cursor = Cursors.Default;
            this._feedback = null;
        }
        void HandleMove(int x, int y)
        {
            if (this.Vertical)
            {
                x = this.Left + (x - this._pos.X);
                if (x < this._pane1.Left + 10) x = this._pane1.Left + 10;
                if (x > this._pane2.Right - 10) x = this._pane2.Right - 10;
                y = this.Top;
            }
            else
            {
                x = this.Left;
                y = this.Top + (y - this._pos.Y);
                if (y < this._pane1.Top + 20) y = this._pane1.Top + 20;
                if (y > this._pane2.Bottom - 20) y = this._pane2.Bottom - 20;
            }
            Rectangle newBounds = new Rectangle(x, y, this.Width, this.Height);
            if (this.Bounds != newBounds)
            {
                if (!this._down)
                {
                    this.Bounds = newBounds;
                    if (this.Vertical)
                    {
                        int diff = (this._start.Left - newBounds.Left);
                        if (this._pane1 != null)
                        {
                            this._pane1.Width -= diff;
                        }
                        if (this._pane2 != null)
                        {
                            this._pane2.Width += diff;
                        }
                    }
                    else
                    {
                        int diff = (this._start.Top - newBounds.Top);
                        if (this._pane1 != null)
                        {
                            this._pane1.Height -= diff;
                        }
                        if (this._pane2 != null)
                        {
                            this._pane2.Height += diff;
                        }
                    }
                    this.Parent.PerformLayout();
                }
                else
                {
                    this._feedback.Bounds = newBounds;
                }
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (this._down)
            {
                HandleMove(e.X, e.Y);
            }
        }

        internal class HatchControl : Control
        {
            private Pen _pen;
            private Brush _brush;

            public HatchControl()
            {
                this._brush = new HatchBrush(HatchStyle.Percent50, Color.Black, Color.White);
                this._pen = new Pen(_brush);
            }
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    _pen.Dispose();
                    _brush.Dispose();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                if (this.Width > this.Height)
                {
                    _pen.Width = this.Height;
                    int y = this.Height / 2;
                    g.DrawLine(this._pen, 0, y, this.Right, y);
                }
                else
                {
                    _pen.Width = this.Width;
                    int x = this.Width / 2;
                    g.DrawLine(this._pen, x, 0, x, this.Bottom);
                }
            }

        }

    }
}
