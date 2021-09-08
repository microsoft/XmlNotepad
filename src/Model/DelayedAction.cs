using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace XmlNotepad
{
    public delegate void DispatchHandler(Action a);

    /// <summary>
    /// This class provides a delayed action that has a name.  If the same named action is
    /// started multiple times before the delay if fires the action only once.
    /// </summary>
    public class DelayedActions
    {
        private bool _closed;
        private DispatchHandler _handler;
        private Dictionary<string, DelayedAction> _pending = new Dictionary<string, DelayedAction>();

        /// <summary>
        /// Construct a new DelayedActions providing the action handler that knows how to switch
        /// the execution of the action to the correct thread (e.g. sometimes you want actions
        /// to run on the UI thread).
        /// </summary>
        /// <param name="handler"></param>
        public DelayedActions(DispatchHandler handler)
        {
            this._handler = handler;
        }

        public void StartDelayedAction(string name, Action action, TimeSpan delay)
        {
            if (this._closed)
            {
                Debug.WriteLine(string.Format("Ignoring delayed action '{0}' because object is closed", name));
                return;
            }

            DelayedAction da;
            if (!_pending.TryGetValue(name, out da))
            {
                da = new DelayedAction(this._handler, name);
                _pending[name] = da;
            }

            da.StartDelayTimer(action, delay);
        }

        public void CancelDelayedAction(string name)
        {
            DelayedAction action;
            if (_pending.TryGetValue(name, out action))
            {
                action.StopDelayTimer();
                _pending.Remove(name);
            }
        }

        public void Close()
        {
            this._closed = true;
            foreach (var pair in _pending)
            {
                pair.Value.StopDelayTimer();
            }
            _pending.Clear();
        }


        class DelayedAction
        {
            System.Threading.Timer delayTimer;
            Action delayedAction;
            int startTime;
            DispatchHandler handler;
            string name;

            public DelayedAction(DispatchHandler handler, string name)
            {
                this.handler = handler;
                this.name = name;
            }

            /// <summary>
            /// Start a count down with the given delay, and fire the given action when it reaches zero.
            /// But if this method is called again before the timeout it resets the timeout and starts again.
            /// </summary>
            /// <param name="action">The action to perform when the delay is reached</param>
            /// <param name="delay">The timeout before calling the action</param>
            public void StartDelayTimer(Action action, TimeSpan delay)
            {
                startTime = Environment.TickCount;

                // stop any previous timer and start over.
                StopDelayTimer();

                this.delayedAction = action;

                if (delay.TotalMilliseconds == 0)
                {
                    // immediate!
                    this.OnDelayTimerTick(null);
                }
                else
                {
                    this.delayTimer = new System.Threading.Timer(OnDelayTimerTick, null, (int)delay.TotalMilliseconds, System.Threading.Timeout.Infinite);
                }
            }

            public void StopDelayTimer()
            {
                System.Threading.Timer timer = this.delayTimer;
                System.Threading.Interlocked.CompareExchange(ref this.delayTimer, null, timer);
                if (timer != null)
                {
                    // give up on this old one and start over.
                    timer.Dispose();
                    timer = null;
                }
                delayedAction = null;
            }

            internal void OnDelayTimerTick(object state)
            {
                int endTime = Environment.TickCount;
                int diff = startTime - endTime;

                Action a = this.delayedAction;

                StopDelayTimer();

                if (a != null)
                {
                    this.handler(() =>
                    {
                        try
                        {
                            Debug.WriteLine("invoking delayed action: " + this.name);
                            a();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("OnDelayTimerTick caught unhandled exception: " + ex.ToString());
                        }
                    });
                }
            }
        }
    }
}
