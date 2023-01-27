using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using WindowsInput.Native;

namespace WindowsInput
{
    /// <summary>
    /// A record of an actual mouse event and where it showed up in the client app.
    /// </summary>
    public class MouseCalibration
    {
        /// <summary>
        /// The mouse position sent.
        /// </summary>
        public Point Expected { get; set; }
        /// <summary>
        /// The mouse position received.
        /// </summary>
        public Point Actual { get; set; }
    }

    /// <summary>
    /// Implements the <see cref="IMouseSimulator"/> interface by calling the an <see cref="IInputMessageDispatcher"/> to simulate Mouse gestures.
    /// </summary>
    public class MouseSimulator : IMouseSimulator
    {
        private const int MouseWheelClickSize = 120;
        private MouseButton mouseDown;
        private readonly IInputSimulator _inputSimulator;
        private List<MouseCalibration> calibration;

        /// <summary>
        /// The instance of the <see cref="IInputMessageDispatcher"/> to use for dispatching <see cref="INPUT"/> messages.
        /// </summary>
        private readonly IInputMessageDispatcher _messageDispatcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseSimulator"/> class using an instance of a <see cref="WindowsInputMessageDispatcher"/> for dispatching <see cref="INPUT"/> messages.
        /// </summary>
        /// <param name="inputSimulator">The <see cref="IInputSimulator"/> that owns this instance.</param>
        public MouseSimulator(IInputSimulator inputSimulator)
        {
            if (inputSimulator == null) throw new ArgumentNullException("inputSimulator");

            _inputSimulator = inputSimulator;
            _messageDispatcher = new WindowsInputMessageDispatcher();
        }

        /// <summary>
        /// Implement the calibrate method.
        /// </summary>
        /// <param name="points"></param>
        public void Calibrate(List<MouseCalibration> points)
        {
            this.calibration = points;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseSimulator"/> class using the specified <see cref="IInputMessageDispatcher"/> for dispatching <see cref="INPUT"/> messages.
        /// </summary>
        /// <param name="inputSimulator">The <see cref="IInputSimulator"/> that owns this instance.</param>
        /// <param name="messageDispatcher">The <see cref="IInputMessageDispatcher"/> to use for dispatching <see cref="INPUT"/> messages.</param>
        /// <exception cref="InvalidOperationException">If null is passed as the <paramref name="messageDispatcher"/>.</exception>
        internal MouseSimulator(IInputSimulator inputSimulator, IInputMessageDispatcher messageDispatcher)
        {
            if (inputSimulator == null)
                throw new ArgumentNullException("inputSimulator");

            if (messageDispatcher == null)
                throw new InvalidOperationException(
                    string.Format("The {0} cannot operate with a null {1}. Please provide a valid {1} instance to use for dispatching {2} messages.",
                    typeof(MouseSimulator).Name, typeof(IInputMessageDispatcher).Name, typeof(INPUT).Name));

            _inputSimulator = inputSimulator;
            _messageDispatcher = messageDispatcher;
        }

        /// <summary>
        /// Gets the <see cref="IKeyboardSimulator"/> instance for simulating Keyboard input.
        /// </summary>
        /// <value>The <see cref="IKeyboardSimulator"/> instance.</value>
        public IKeyboardSimulator Keyboard { get { return _inputSimulator.Keyboard; } }

        /// <summary>
        /// Sends the list of <see cref="INPUT"/> messages using the <see cref="IInputMessageDispatcher"/> instance.
        /// </summary>
        /// <param name="inputList">The <see cref="System.Array"/> of <see cref="INPUT"/> messages to send.</param>
        private void SendSimulatedInput(INPUT[] inputList)
        {
            _messageDispatcher.DispatchInput(inputList);
        }

        /// <summary>
        /// Simulates mouse movement by the specified distance measured as a delta from the current mouse location in pixels.
        /// </summary>
        /// <param name="pixelDeltaX">The distance in pixels to move the mouse horizontally.</param>
        /// <param name="pixelDeltaY">The distance in pixels to move the mouse vertically.</param>
        public IMouseSimulator MoveMouseBy(int pixelDeltaX, int pixelDeltaY)
        {
            var inputList = new InputBuilder(calibration).AddRelativeMouseMovement(pixelDeltaX, pixelDeltaY, this.mouseDown).ToArray();
            SendSimulatedInput(inputList);
            return this;
        }

        /// <summary>
        /// Simulates mouse movement to the specified location on the primary display device.
        /// </summary>
        /// <param name="absoluteX">The destination's absolute X-coordinate on the primary display device where 0 is the extreme left hand side of the display device and 65535 is the extreme right hand side of the display device.</param>
        /// <param name="absoluteY">The destination's absolute Y-coordinate on the primary display device where 0 is the top of the display device and 65535 is the bottom of the display device.</param>
        public IMouseSimulator MoveMouseTo(double absoluteX, double absoluteY)
        {
            var inputList = new InputBuilder(calibration).AddAbsoluteMouseMovement((int)Math.Truncate(absoluteX), (int)Math.Truncate(absoluteY), this.mouseDown).ToArray();
            SendSimulatedInput(inputList);
            return this;
        }

        /// <summary>
        /// Simulates mouse movement to the specified location on the Virtual Desktop which includes all active displays.
        /// </summary>
        /// <param name="absoluteX">The destination's absolute X-coordinate on the virtual desktop where 0 is the left hand side of the virtual desktop and 65535 is the extreme right hand side of the virtual desktop.</param>
        /// <param name="absoluteY">The destination's absolute Y-coordinate on the virtual desktop where 0 is the top of the virtual desktop and 65535 is the bottom of the virtual desktop.</param>
        public IMouseSimulator MoveMouseToPositionOnVirtualDesktop(double absoluteX, double absoluteY)
        {
            var inputList = new InputBuilder(calibration).AddAbsoluteMouseMovementOnVirtualDesktop((int)Math.Truncate(absoluteX), (int)Math.Truncate(absoluteY), this.mouseDown).ToArray();
            SendSimulatedInput(inputList);
            return this;
        }

        /// <summary>
        /// Simulates a mouse left button down gesture.
        /// </summary>
        public IMouseSimulator LeftButtonDown()
        {
            var inputList = new InputBuilder(calibration).AddMouseButtonDown(MouseButton.LeftButton).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.LeftButton;
            return this;
        }

        /// <summary>
        /// Simulates a mouse left button up gesture.
        /// </summary>
        public IMouseSimulator LeftButtonUp()
        {
            var inputList = new InputBuilder(calibration).AddMouseButtonUp(MouseButton.LeftButton).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.None;
            return this;
        }

        /// <summary>
        /// Simulates a mouse left-click gesture.
        /// </summary>
        public IMouseSimulator LeftButtonClick()
        {
            var inputList = new InputBuilder(calibration).AddMouseButtonClick(MouseButton.LeftButton).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.None;
            return this;
        }

        /// <summary>
        /// Simulates a mouse left button double-click gesture.
        /// </summary>
        public IMouseSimulator LeftButtonDoubleClick()
        {
            var inputList = new InputBuilder(calibration).AddMouseButtonDoubleClick(MouseButton.LeftButton).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.None;
            return this;
        }


        /// <summary>
        /// Simulates a mouse middle button down gesture.
        /// </summary>
        public IMouseSimulator MiddleButtonDown()
        {
            var inputList = new InputBuilder(calibration).AddMouseButtonDown(MouseButton.MiddleButton).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.MiddleButton;
            return this;
        }

        /// <summary>
        /// Simulates a mouse middle button up gesture.
        /// </summary>
        public IMouseSimulator MiddleButtonUp()
        {
            var inputList = new InputBuilder(calibration).AddMouseButtonUp(MouseButton.MiddleButton).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.None;
            return this;
        }

        /// <summary>
        /// Simulates a mouse middle-click gesture.
        /// </summary>
        public IMouseSimulator MiddleButtonClick()
        {
            var inputList = new InputBuilder(calibration).AddMouseButtonClick(MouseButton.MiddleButton).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.None;
            return this;
        }

        /// <summary>
        /// Simulates a mouse middle button double-click gesture.
        /// </summary>
        public IMouseSimulator MiddleButtonDoubleClick()
        {
            var inputList = new InputBuilder(calibration).AddMouseButtonDoubleClick(MouseButton.MiddleButton).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.None;
            return this;
        }

        /// <summary>
        /// Simulates a mouse right button down gesture.
        /// </summary>
        public IMouseSimulator RightButtonDown()
        {
            var inputList = new InputBuilder(calibration).AddMouseButtonDown(MouseButton.RightButton).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.RightButton;
            return this;
        }

        /// <summary>
        /// Simulates a mouse right button up gesture.
        /// </summary>
        public IMouseSimulator RightButtonUp()
        {
            var inputList = new InputBuilder(calibration).AddMouseButtonUp(MouseButton.RightButton).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.None;
            return this;
        }

        /// <summary>
        /// Simulates a mouse right button click gesture.
        /// </summary>
        public IMouseSimulator RightButtonClick()
        {
            var inputList = new InputBuilder(calibration).AddMouseButtonClick(MouseButton.RightButton).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.None;
            return this;
        }

        /// <summary>
        /// Simulates a mouse right button double-click gesture.
        /// </summary>
        public IMouseSimulator RightButtonDoubleClick()
        {
            var inputList = new InputBuilder(calibration).AddMouseButtonDoubleClick(MouseButton.RightButton).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.None;
            return this;
        }

        /// <summary>
        /// Simulates a mouse X button down gesture.
        /// </summary>
        /// <param name="buttonId">The button id.</param>
        public IMouseSimulator XButtonDown(int buttonId)
        {
            var inputList = new InputBuilder(calibration).AddMouseXButtonDown(buttonId).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.None;
            return this;
        }

        /// <summary>
        /// Simulates a mouse X button up gesture.
        /// </summary>
        /// <param name="buttonId">The button id.</param>
        public IMouseSimulator XButtonUp(int buttonId)
        {
            var inputList = new InputBuilder(calibration).AddMouseXButtonUp(buttonId).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.None;
            return this;
        }

        /// <summary>
        /// Simulates a mouse X button click gesture.
        /// </summary>
        /// <param name="buttonId">The button id.</param>
        public IMouseSimulator XButtonClick(int buttonId)
        {
            var inputList = new InputBuilder(calibration).AddMouseXButtonClick(buttonId).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.None;
            return this;
        }

        /// <summary>
        /// Simulates a mouse X button double-click gesture.
        /// </summary>
        /// <param name="buttonId">The button id.</param>
        public IMouseSimulator XButtonDoubleClick(int buttonId)
        {
            var inputList = new InputBuilder(calibration).AddMouseXButtonDoubleClick(buttonId).ToArray();
            SendSimulatedInput(inputList);
            this.mouseDown = MouseButton.None;
            return this;
        }

        /// <summary>
        /// Simulates mouse vertical wheel scroll gesture.
        /// </summary>
        /// <param name="scrollAmountInClicks">The amount to scroll in clicks. A positive value indicates that the wheel was rotated forward, away from the user; a negative value indicates that the wheel was rotated backward, toward the user.</param>
        public IMouseSimulator VerticalScroll(int scrollAmountInClicks)
        {
            var inputList = new InputBuilder(calibration).AddMouseVerticalWheelScroll(scrollAmountInClicks * MouseWheelClickSize).ToArray();
            SendSimulatedInput(inputList);
            return this;
        }

        /// <summary>
        /// Simulates a mouse horizontal wheel scroll gesture. Supported by Windows Vista and later.
        /// </summary>
        /// <param name="scrollAmountInClicks">The amount to scroll in clicks. A positive value indicates that the wheel was rotated to the right; a negative value indicates that the wheel was rotated to the left.</param>
        public IMouseSimulator HorizontalScroll(int scrollAmountInClicks)
        {
            var inputList = new InputBuilder(calibration).AddMouseHorizontalWheelScroll(scrollAmountInClicks * MouseWheelClickSize).ToArray();
            SendSimulatedInput(inputList);
            return this;
        }

        /// <summary>
        /// Sleeps the executing thread to create a pause between simulated inputs.
        /// </summary>
        /// <param name="millsecondsTimeout">The number of milliseconds to wait.</param>
        public IMouseSimulator Sleep(int millsecondsTimeout)
        {
            Thread.Sleep(millsecondsTimeout);
            return this;
        }

        /// <summary>
        /// Sleeps the executing thread to create a pause between simulated inputs.
        /// </summary>
        /// <param name="timeout">The time to wait.</param>
        public IMouseSimulator Sleep(TimeSpan timeout)
        {
            Thread.Sleep(timeout);
            return this;
        }

        /// <summary>
        /// Perform left click drag drop operation from start to end points moving the 
        /// mouse by the given delta each time with delay between each movement.
        /// </summary>
        /// <param name="sx"></param>
        /// <param name="sy"></param>
        /// <param name="ex"></param>
        /// <param name="ey"></param>
        /// <param name="delta"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public IMouseSimulator LeftButtonDragDrop(int sx, int sy, int ex, int ey, int delta, int delay)
        {
            this.MoveMouseTo(sx, sy);
            this.LeftButtonDown();

            // Interpolate and move mouse smoothly over to given location.                
            double dx = ex - sx;
            double dy = ey - sy;
            if (delta < 1)
            {
                delta = 1;
            }
            int length = (int)Math.Sqrt((dx * dx) + (dy * dy));
            if (length == 0)
            {
                length = 1;
            }
            
            for (int i = 0; i < length; i += delta)
            {
                int tx = (int)(sx + ((dx * i) / length));
                int ty = (int)(sy + ((dy * i) / length));
                this.MoveMouseTo(tx, ty);
                Thread.Sleep(delay);
            }

            // nail the landing.
            this.MoveMouseTo(ex, ey)
                .Sleep(delay)
                .LeftButtonUp(); // release the mouse!
            return this;
        }
    }
}