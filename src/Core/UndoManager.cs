using System;
using System.Collections.Generic;

namespace XmlNotepad
{
    public class CommandEventArgs : EventArgs
    {
        private Command _cmd;

        public CommandEventArgs(Command cmd)
        {
            this._cmd = cmd;
        }
        public Command Command { get { return this._cmd; } }
    }

    public class UndoManager
    {
        private List<Command> _stack;
        private int _pos;
        private int _max;
        private Command _exec = null;
        private CompoundCommand _compound;

        public event EventHandler StateChanged;
        public event EventHandler<CommandEventArgs> CommandDone;
        public event EventHandler<CommandEventArgs> CommandUndone;
        public event EventHandler<CommandEventArgs> CommandRedone;

        public UndoManager(int maxHistory)
        {
            this._stack = new List<Command>();
            this._max = maxHistory;
        }

        public void Clear()
        {
            if (_stack.Count > 0)
            {
                _pos = 0;
                _stack.Clear();
                if (StateChanged != null)
                {
                    StateChanged(this, EventArgs.Empty);
                }
            }
        }

        public Command Executing
        {
            get { return this._exec; }
        }

        public bool CanUndo
        {
            get
            {
                return _pos > 0;
            }
        }

        public bool CanRedo
        {
            get
            {
                return _pos < _stack.Count;
            }
        }

        public Command Current
        {
            get
            {
                if (_pos >= 0 && _pos < _stack.Count)
                {
                    return _stack[_pos];
                }
                return null;
            }
        }

        void Add(Command cmd)
        {
            if (_pos < _stack.Count)
            {
                _stack.RemoveRange(_pos, _stack.Count - _pos);
            }
            System.Diagnostics.Trace.WriteLine(cmd.Name);
            _stack.Add(cmd);
            if (_stack.Count > this._max)
            {
                _stack.RemoveAt(0);
            }
            else
            {
                _pos++;
            }
        }

        public void Push(Command cmd)
        {
            if (cmd.IsNoop) return; // do nothing!

            if (this._compound != null)
            {
                this._compound.Add(cmd);
            }
            else
            {
                Add(cmd);
            }

            // Must do command after adding it to the command stack!
            Command saved = this._exec;
            try
            {
                this._exec = cmd;
                cmd.Do();

            }
            finally
            {
                this._exec = saved;
            }

            if (StateChanged != null)
            {
                StateChanged(this, EventArgs.Empty);
            }
            if (CommandDone != null)
            {
                CommandDone(this, new CommandEventArgs(cmd));
            }
        }

        public Command Undo()
        {
            if (_pos > 0)
            {
                _pos--;
                Command cmd = this.Current;
                if (cmd != null && !cmd.IsNoop)
                {
                    Command saved = this._exec;
                    try
                    {
                        this._exec = cmd;
                        cmd.Undo();
                    }
                    finally
                    {
                        this._exec = saved;
                    }
                    if (StateChanged != null)
                    {
                        StateChanged(this, EventArgs.Empty);
                    }
                    if (CommandUndone != null)
                    {
                        CommandUndone(this, new CommandEventArgs(cmd));
                    }
                }
                return cmd;
            }
            return null;
        }

        public Command Redo()
        {
            if (_pos < _stack.Count)
            {
                Command cmd = this.Current;
                if (cmd != null && !cmd.IsNoop)
                {
                    Command saved = this._exec;
                    try
                    {
                        this._exec = cmd;
                        cmd.Redo();
                    }
                    finally
                    {
                        this._exec = saved;
                    }
                    if (StateChanged != null)
                    {
                        StateChanged(this, new CommandEventArgs(cmd));
                    }
                    if (CommandRedone != null)
                    {
                        CommandRedone(this, new CommandEventArgs(cmd));
                    }
                }
                _pos++;
                return cmd;
            }
            return null;
        }

        public Command Peek()
        {
            if (_pos > 0 && _pos - 1 < _stack.Count)
            {
                return _stack[_pos - 1];
            }
            return null;
        }

        public Command Pop()
        {
            // remove command without undoing it.
            if (_pos > 0)
            {
                _pos--;
                if (_pos < _stack.Count)
                {
                    _stack.RemoveAt(_pos);
                }
                if (StateChanged != null)
                {
                    StateChanged(this, EventArgs.Empty);
                }
            }
            return this.Current;
        }

        public void Merge(CompoundCommand cmd)
        {
            // Replace the current command without upsetting rest of the redo stack
            // and insert the current command into this compound command.
            Command current = Peek();
            if (current != null)
            {
                _stack.Insert(_pos - 1, cmd);
                _stack.RemoveAt(_pos);
                cmd.Do();
                cmd.Insert(current);
            }
            else
            {
                Push(cmd);
            }
        }

        public CompoundCommand OpenCompoundAction(string name)
        {
            this._compound = new CompoundCommand(name);
            Add(this._compound);
            return this._compound;
        }

        public void CloseCompoundAction()
        {
            this._compound = null;
        }
    }

    public abstract class Command
    {
        public abstract string Name { get; }
        public abstract void Do();
        public abstract void Undo();
        public abstract void Redo();
        public abstract bool IsNoop { get; }
    }


    public class CompoundCommand : Command
    {
        List<Command> commands = new List<Command>();
        string name;

        public CompoundCommand(string name)
        {
            this.name = name;
        }

        public override string Name
        {
            get { return name; }
        }

        public void Add(Command cmd)
        {
            commands.Add(cmd);
        }

        public int Count { get { return commands.Count; } }

        public override void Do()
        {
            if (this.IsNoop) return;
            foreach (Command cmd in this.commands)
            {
                cmd.Do();
            }
        }
        public override void Undo()
        {
            // Must undo in reverse order!
            for (int i = this.commands.Count - 1; i >= 0; i--)
            {
                Command cmd = this.commands[i];
                cmd.Undo();
            }
        }
        public override void Redo()
        {
            foreach (Command cmd in this.commands)
            {
                cmd.Redo();
            }
        }
        public override bool IsNoop
        {
            get
            {
                foreach (Command cmd in this.commands)
                {
                    if (!cmd.IsNoop) return false;
                }
                return true;
            }
        }

        public void Insert(Command cmd)
        {
            commands.Insert(0, cmd);
        }
    }
}
