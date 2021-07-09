using System;
using System.Collections;

namespace XmlNotepad
{
    public class CommandEventArgs : EventArgs {
        Command cmd;
        public CommandEventArgs(Command cmd) {
            this.cmd = cmd;
        }
        public Command Command { get { return this.cmd; } }
    }

	public class UndoManager
	{        
        ArrayList stack = new ArrayList();
        int pos;
        int max;
        Command exec = null;
        CompoundCommand compound;

        public event EventHandler StateChanged;
        public event EventHandler<CommandEventArgs> CommandDone;
        public event EventHandler<CommandEventArgs> CommandUndone;
        public event EventHandler<CommandEventArgs> CommandRedone;

		public UndoManager(int maxHistory)
		{
            this.stack = new ArrayList();            
            this.max = maxHistory;
        }

        public void Clear(){
            if (stack.Count>0){
                pos = 0;
                stack.Clear();
                if (StateChanged != null){
                    StateChanged(this, EventArgs.Empty);
                }
            }
        }

        public Command Executing {
            get { return this.exec; }
        }

        public bool CanUndo{
            get {
                return pos>0;
            }
        }

        public bool CanRedo{
            get {
                return pos < stack.Count;
            }
        }

        public Command Current {
            get { 
                if (pos>=0 && pos < stack.Count){
                    return (Command)stack[pos];
                }
                return null;
            }
        }

        void Add(Command cmd) {
            if (pos < stack.Count) {
                stack.RemoveRange(pos, stack.Count - pos);
            }
            System.Diagnostics.Trace.WriteLine(cmd.Name);
            stack.Add(cmd);
            if (stack.Count > this.max) {
                stack.RemoveAt(0);
            } else {
                pos++;
            }
        }

        public void Push(Command cmd) {
            if (cmd.IsNoop) return; // do nothing!

            if (this.compound != null) {
                this.compound.Add(cmd);
            } else {
                Add(cmd);
            }

            // Must do command after adding it to the command stack!
            Command saved = this.exec;
            try {
                this.exec = cmd;
                cmd.Do();
                
            } finally {
                this.exec = saved;
            }

            if (StateChanged != null) {
                StateChanged(this, EventArgs.Empty);
            }
            if (CommandDone != null) {
                CommandDone(this, new CommandEventArgs(cmd));
            }
        }

        public Command Undo(){
            if (pos>0){
                pos--;                
                Command cmd = this.Current;
                if (cmd != null && !cmd.IsNoop){
                    Command saved = this.exec;
                    try {
                        this.exec = cmd;
                        cmd.Undo();
                    } finally {
                        this.exec = saved;
                    }
                    if (StateChanged != null) {
                        StateChanged(this, EventArgs.Empty);
                    }
                    if (CommandUndone != null) {
                        CommandUndone(this, new CommandEventArgs(cmd));
                    }
                }
                return cmd;
            }
            return null;
        }

        public Command Redo(){
            if (pos < stack.Count){
                Command cmd = this.Current;
                if (cmd != null && !cmd.IsNoop){
                    Command saved = this.exec;
                    try {
                        this.exec = cmd;
                        cmd.Redo();
                    } finally {
                        this.exec = saved;
                    }
                    if (StateChanged != null) {
                        StateChanged(this, new CommandEventArgs(cmd));
                    }
                    if (CommandRedone != null) {
                        CommandRedone(this, new CommandEventArgs(cmd));
                    }
                }
                pos++;
                return cmd;
            }
            return null;
        }

        public Command Peek(){
            if (pos>0 && pos-1 < stack.Count){
                return (Command)stack[pos-1];
            }
            return null;
        }

        public Command Pop(){
            // remove command without undoing it.
            if (pos > 0) {
                pos--;
                if (pos < stack.Count) {
                    stack.RemoveAt(pos);
                }
                if (StateChanged != null) {
                    StateChanged(this, EventArgs.Empty);
                }
            }
            return this.Current;
        }

        public void Merge(CompoundCommand cmd) {
            // Replace the current command without upsetting rest of the redo stack
            // and insert the current command into this compound command.
            Command current = Peek();
            if (current != null) {
                stack.Insert(pos-1, cmd);
                stack.RemoveAt(pos);
                cmd.Do();
                cmd.Insert(current);
            } else {
                Push(cmd);
            }
        }

        public CompoundCommand OpenCompoundAction(string name) {
            this.compound = new CompoundCommand(name);
            Add(this.compound);
            return this.compound;
        }

        public void CloseCompoundAction() {
            this.compound = null;
        }
    }

    public abstract class Command {
        public abstract string Name { get; }
        public abstract void Do();
        public abstract void Undo();
        public abstract void Redo();
        public abstract bool IsNoop { get; }
    }


    public class CompoundCommand : Command {
        ArrayList commands = new ArrayList();
        string name;

        public CompoundCommand(string name) {
            this.name = name;
        }

        public override string Name {
            get { return name; }
        }

        public void Add(Command cmd) {
            commands.Add(cmd);
        }

        public int Count { get { return commands.Count; } }

        public override void Do() {
            if (this.IsNoop) return;
            foreach (Command cmd in this.commands) {
                cmd.Do();
            }
        }
        public override void Undo() {
            // Must undo in reverse order!
            for (int i = this.commands.Count - 1; i >= 0; i--) {
                Command cmd = (Command)this.commands[i];
                cmd.Undo();
            }
        }
        public override void Redo() {
            foreach (Command cmd in this.commands) {
                cmd.Redo();
            }
        }
        public override bool IsNoop {
            get {
                foreach (Command cmd in this.commands) {
                    if (!cmd.IsNoop) return false;
                }
                return true;
            }
        }

        public void Insert(Command cmd) {
            commands.Insert(0, cmd);
        }
    }
}
