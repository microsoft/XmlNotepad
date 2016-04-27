using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

namespace XmlNotepad {

    public class PaneResizer : Control {

        // panes could be horizontally aligned, or vertically aligned, 
        // it will do the right thing.
        Control pane1; 
        Control pane2;
        Point pos;
        bool vertical = true;
        int width = 5;
        bool down;
        Rectangle start;
        Border3DStyle style = Border3DStyle.Raised;
        HatchControl feedback;
        
        public int PaneWidth { 
            get { return this.width; }
            set { this.width = value; }
        }

        public Control Pane1 { 
            get { return this.pane1; }
            set { this.pane1 = value; }
        }
        public Control Pane2 { 
            get { return this.pane2; }
            set { this.pane2 = value; }
        }    
        
        public Border3DStyle Border3DStyle {
            get { return this.style; }
            set { this.style = value; }
        }

        public bool Vertical {
            get {return this.vertical;}
            set { this.vertical = value; }
        }

        protected override void OnPaint(PaintEventArgs e){
            base.OnPaint(e);
            Border3DSide sides = this.Vertical ?  
                (Border3DSide.Left | Border3DSide.Right) : (Border3DSide.Top | Border3DSide.Bottom);
            ControlPaint.DrawBorder3D(e.Graphics, 0, 0, this.Width, this.Height, this.style, sides);
        }
        
        protected override void OnMouseEnter(EventArgs e){
            this.Cursor = this.Vertical ? Cursors.VSplit : Cursors.HSplit;
        }
        protected override void OnMouseLeave(EventArgs e){
            if (!this.down) this.Cursor = Cursors.Default;            
        }
        protected override void OnMouseDown(MouseEventArgs e){
            if (this.feedback == null){
                this.feedback = new HatchControl();
                this.Parent.Controls.Add(this.feedback);
                int index = 0;
                foreach (Control c in this.Parent.Controls){
                    if (c == this)
                        break;
                    index++;
                }
                this.Parent.Controls.SetChildIndex(this.feedback, index);
            }
            this.start = this.Bounds;
            this.feedback.Bounds = this.Bounds;
            this.pos = new Point(e.X, e.Y);
            this.down = true;
        }
        protected override void OnMouseUp(MouseEventArgs e){
            this.down = false;  
            HandleMove(e.X, e.Y);
            this.Parent.Controls.Remove(this.feedback);
            this.Cursor = Cursors.Default;
            this.feedback = null;
        }
        void HandleMove(int x, int y) {
            if (this.Vertical){
                x = this.Left + (x - this.pos.X);
                if (x < this.pane1.Left+10) x= this.pane1.Left+10;
                if (x > this.pane2.Right-10) x = this.pane2.Right-10;
                y = this.Top;
            } else {
                x = this.Left;
                y = this.Top + (y - this.pos.Y);
                if (y < this.pane1.Top + 20) y = this.pane1.Top + 20;
                if (y > this.pane2.Bottom - 20) y = this.pane2.Bottom - 20;
            }
            Rectangle newBounds = new Rectangle(x,y, this.Width, this.Height);
            if (this.Bounds != newBounds ){
                if (!this.down){
                    this.Bounds = newBounds;
                    if (this.Vertical) {
                        int diff = (this.start.Left - newBounds.Left);
                        if (this.pane1 != null) {
                            this.pane1.Width -= diff;
                        }
                        if (this.pane2 != null) {
                            this.pane2.Width += diff;
                        }
                    } else {
                        int diff = (this.start.Top - newBounds.Top);
                        if (this.pane1 != null) {
                            this.pane1.Height -= diff;
                        }
                        if (this.pane2 != null) {
                            this.pane2.Height += diff;
                        }
                    }
                    this.Parent.PerformLayout();
                } else {
                    this.feedback.Bounds = newBounds;
                }
            }
        }
        protected override void OnMouseMove(MouseEventArgs e){
            if (this.down){
                HandleMove(e.X, e.Y);
            }
        }

        internal class HatchControl : Control {
            Pen pen;
            Brush brush;

            public HatchControl() {
                this.brush = new HatchBrush(HatchStyle.Percent50, Color.Black, Color.White);
                this.pen = new Pen(brush);
            }
            protected override void Dispose(bool disposing) {
                base.Dispose (disposing);
                if (disposing){
                    pen.Dispose();
                    brush.Dispose();
                }
            }

            protected override void OnPaint(PaintEventArgs e) {
                base.OnPaint (e);
                Graphics g = e.Graphics;
                if (this.Width>this.Height){
                    pen.Width = this.Height;
                    int y = this.Height/2;
                    g.DrawLine(this.pen, 0, y, this.Right, y);
                } else {
                    pen.Width = this.Width;
                    int x = this.Width/2;
                    g.DrawLine(this.pen, x, 0, x, this.Bottom);
                }
            }

        }

    }
}
