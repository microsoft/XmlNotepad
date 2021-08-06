using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq.Expressions;
using System.Windows.Forms;

namespace XmlNotepad
{
    public class AnimationCanvas : UserControl
    {
        List<Shape> shapes = new List<Shape>();
        AnimationClock clock = new AnimationClock();
        System.Windows.Forms.Timer timer;

        public AnimationCanvas()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.ResizeRedraw, true);
        }

        public List<Shape> Shapes => shapes;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            bool complete = true;
            foreach (var shape in Shapes)
            {
                complete &= shape.Step(clock);
                shape.Draw(graphics);                
            }
            if (!complete)
            {
                if (timer == null) {
                    timer = new Timer();
                    timer.Interval = 1;
                    timer.Tick += (sender, evt) => {
                        Invalidate();
                    };
                    timer.Enabled = true;
                    timer.Start();
                }                
            }
            else if (timer != null)
            {
                Debug.WriteLine("Animation complete");
                timer.Stop();
                timer = null;
            }
        }

        public void InitializeBackgroundFromScreen(int x, int y)
        {
            Bitmap bmp = new Bitmap(this.Width, this.Height);
            using (var graph = Graphics.FromImage(bmp))
            {
                Rectangle screenRectangle = this.RectangleToScreen(this.ClientRectangle);
                graph.CopyFromScreen(x, y, 0, 0, bmp.Size);
            }

            this.BackgroundImage = bmp;
        }
    }

    public class AnimationClock
    {
        Stopwatch timer;
        AnimationClock parent;
        double start;

        public AnimationClock()
        {
            timer = new Stopwatch();
            timer.Start();
        }

        public AnimationClock(AnimationClock parent)
        {
            this.parent = parent;
            start = parent.Now();
        }

        public double Now()
        {
            if (parent != null)
            {
                return parent.Now() -start;
            }
            else
            {
                return (double)timer.ElapsedTicks / (double)Stopwatch.Frequency;
            }
        }
    }

    public abstract class Shape
    {
        Animation animation;

        public void BeginAnimation(Animation animation)
        {
            this.animation = animation;
            if (animation != null)
            {
                animation.TargetObject = this;
            }
        }

        public bool Step(AnimationClock clock)
        {
            if (animation != null)
            {
                animation.Step(clock);
                return animation.IsComplete;
            }
            return true;
        }

        public abstract void Draw(Graphics g);
    }

    public class RectangleShape : Shape
    {
        Rectangle bounds;

        public Rectangle Bounds
        {
            get { return bounds; }
            set
            {
                bounds = value;
            }
        }

        public Brush Fill { get; set; }
        public Brush Stroke { get; set; }
        public float StrokeThickness { get; set; }
        public DashStyle DashStyle { get; set; }


        public Brush Foreground { get; set; }
        public string Label { get; set; }
        public Font Font { get; set; }

        public RectangleShape()
        {
            StrokeThickness = 1;
            DashStyle = DashStyle.Solid;
        }

        public override void Draw(Graphics g)
        {
            if (Fill != null)
            {
                g.FillRectangle(Fill, Bounds);
            }
            if (Stroke != null && StrokeThickness > 0)
            {
                using (Pen pen = new Pen(Stroke, StrokeThickness))
                {
                    pen.DashStyle = this.DashStyle;
                    g.DrawRectangle(pen, Bounds);
                }

            }

            if (Label != null && Font != null && Foreground != null)
            {
                var size = g.MeasureString(Label, Font);
                var position = new PointF(
                    (float)Bounds.X + ((float)Bounds.Width - size.Width) / 2, 
                    (float)Bounds.Y + ((float)Bounds.Height - size.Height) / 2);
                g.DrawString(Label, Font, Foreground, position);
            }
        }
    }

    /// <summary>
    ///  Default linear interpolation
    /// </summary>
    public class AnimationFunction
    {
        public Animation Owner;

        /// <summary>
        ///  return position between 0 and 1 
        /// </summary>
        /// <param name="clock"></param>
        /// <returns></returns>
        public virtual double GetPosition(AnimationClock clock)
        {
            var now = clock.Now();
            var total = Owner.Duration.TotalSeconds;
            if (now > total)
            {
                return 1;
            }
            return now / total;
        }
    }

    /// <summary>
    /// start slowly and exponentially speed up towards the target position.
    /// </summary>
    public class AnimationEaseInFunction : AnimationFunction
    {
        public double ExponentialBase { get; set; }
        public double Scale { get; set; }

        public AnimationEaseInFunction()
        {
            ExponentialBase = 7;
            Scale = 1.2;
        }

        /// <summary>
        ///  return position between 0 and 1 
        /// </summary>
        /// <param name="clock"></param>
        /// <returns></returns>
        public override double GetPosition(AnimationClock clock)
        {
            // get linear position in the range [0-1]
            var pos = base.GetPosition(clock);

            // scale range of the exponential function to the interval [0-1].
            double min = Scale;
            double max = Scale * Math.Pow(ExponentialBase, 1);
            double range = max - min;
            double result = ((Scale * Math.Pow(ExponentialBase, pos)) - min) / range;
            return result;
        }
    }

    public abstract class Animation
    {
        private Shape targetObject;
        private string targetProperty;
        private bool completed;

        public TimeSpan StartTime;
        public TimeSpan Duration;
        public AnimationFunction Function = new AnimationFunction();

        public event EventHandler Completed;

        public abstract void Step(AnimationClock clock);
        public bool IsComplete
        {
            get { return completed; }
            set
            {
                completed = value;
                if (value) {
                    var handler = this.Completed;
                    if (handler != null)
                    {
                        handler(this, EventArgs.Empty);
                    }
                }
            }
        }
        public Shape TargetObject
        {
            get { return targetObject; }
            set { targetObject = value; OnTargetChanged(); }
        }
        public string TargetProperty
        {
            get { return targetProperty; }
            set
            {
                targetProperty = value; OnTargetChanged();
            }
        }

        protected abstract void OnTargetChanged();
    }

    public class BoundsAnimation : Animation
    {
        AnimationClock localTime;
        System.Action<Shape, Rectangle> lambda;

        public Rectangle Start;
        public Rectangle End;

        protected override void OnTargetChanged()
        {
            if (TargetObject != null && TargetProperty != null)
            {
                var property = TargetObject.GetType().GetProperty(TargetProperty);
                if (property == null)
                {
                    throw new Exception(string.Format("Property {0} on target type {1} not found",
                        TargetProperty, TargetObject.GetType().FullName));
                }
                if (property.PropertyType != typeof(Rectangle))
                {
                    throw new Exception(string.Format("Expecting TargetProperty {0} on target type {1} to be of type System.Drawing.Rectangle, but found it to be of type {2}",
                        TargetProperty, TargetObject.GetType().FullName, property.PropertyType.FullName));
                }

                var setter = property.GetSetMethod();
                if (setter == null)
                {
                    throw new Exception(string.Format("Expecting TargetProperty {0} on target type {1} to be settable, but it is not.",
                        TargetProperty, TargetObject.GetType().FullName));
                }

                var argument = Expression.Parameter(property.PropertyType, property.Name);
                var instance = Expression.Parameter(typeof(Shape), "this");
                var typed = Expression.Convert(instance, TargetObject.GetType());
                var call = Expression.Call(typed, setter, argument);
                this.lambda = Expression.Lambda<System.Action<Shape, Rectangle>>(call, instance, argument).Compile();
                Debug.WriteLine("Lambda created");
            }
            else
            {
                this.lambda = null;
            }
        }

        public override void Step(AnimationClock clock)
        {
            Function.Owner = this;

            if (localTime == null)
            {
                localTime = new AnimationClock(clock);
            }

            // get position between 0 and 1
            double position = this.Function.GetPosition(clock);

            if (this.lambda != null)
            {
                // do the interpolation.
                Rectangle r = new Rectangle(
                    (int)(Start.X + (End.X - Start.X) * position),
                    (int)(Start.Y + (End.Y - Start.Y) * position),
                    (int)(Start.Width + (End.Width - Start.Width) * position),
                    (int)(Start.Height + (End.Height - Start.Height) * position));

                this.lambda(this.TargetObject, r);
            }

            if (position == 1)
            {
                IsComplete = true;
            }
        }
    }
}
