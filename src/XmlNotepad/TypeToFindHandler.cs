using System;
using System.IO;
using System.Windows.Forms;

namespace XmlNotepad
{

    public delegate void TypeToFindEventHandler(object sender, string toFind);

    public class TypeToFindHandler : IDisposable
    {
        private uint _start;
        private readonly Control _control;
        private string _typedSoFar;
        private readonly int _resetDelay;
        private TypeToFindEventHandler _handler;
        private bool _started;
        private Cursor _cursor;

        public TypeToFindEventHandler FindString
        {
            get { return this._handler; }
            set { this._handler = value; }
        }

        public TypeToFindHandler(Control c, int resetDelayInMilliseconds)
        {
            this._control = c;
            this._resetDelay = resetDelayInMilliseconds;
            this._control.KeyPress += new KeyPressEventHandler(OnControlKeyPress);
            this._control.KeyDown += new KeyEventHandler(OnControlKeyDown);
        }

        ~TypeToFindHandler()
        {
            Dispose(false);
        }

        public bool Started
        {
            get
            {
                if (Cursor.Current != this._cursor) _started = false;
                return _started;
            }
        }

        void OnControlKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.I:
                    if ((e.Modifiers & Keys.Control) != 0)
                    {
                        StartIncrementalSearch();
                    }
                    break;
                case Keys.Escape:
                    if (_started)
                    {
                        StopIncrementalSearch();
                        e.Handled = true;
                    }
                    break;
                case Keys.Enter:
                case Keys.Home:
                case Keys.End:
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    StopIncrementalSearch();
                    break;
                default:
                    if (_started && !e.Control && !e.Alt)
                        e.Handled = true;
                    break;
            }
        }


        public Cursor Cursor
        {
            get
            {
                if (_cursor == null)
                {
                    using (Stream stream = this.GetType().Assembly.GetManifestResourceStream("XmlNotepad.Resources.isearch.cur"))
                    {
                        this._cursor = new Cursor(stream);
                    }
                }
                return this._cursor;
            }
        }

        public void StartIncrementalSearch()
        {
            Cursor.Current = this.Cursor;
            _started = true;
        }

        public void StopIncrementalSearch()
        {
            Cursor.Current = Cursors.Arrow;
            _started = false;
            _typedSoFar = "";
        }

        void OnControlKeyPress(object sender, KeyPressEventArgs e)
        {
            if (_started)
            {
                char ch = e.KeyChar;
                if (ch < 0x20) return; // don't process control characters
                uint tick = PerformanceInfo.TickCount;
                if (tick < _start || tick < this._resetDelay || _start < tick - this._resetDelay)
                {
                    _typedSoFar = ch.ToString();
                }
                else
                {
                    _typedSoFar += ch.ToString();
                }
                _start = tick;
                if (FindString != null) FindString(this, _typedSoFar);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (_cursor != null)
            {
                _cursor.Dispose();
                _cursor = null;
            }
        }
    }
}
