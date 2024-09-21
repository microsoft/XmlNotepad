using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XmlNotepad
{
    class MenuItemInfo
    {
        public ToolStripMenuItem MenuItem { get; set; }
        public bool RunAsync { get; set; }
    }

    /// <summary>
    /// This class is a workarund for bug 
    /// https://github.com/dotnet/winforms/issues/10244
    /// </summary>
    public partial class HiddenMenuItems : UserControl
    {
        AccessibleHiddenMenuItems acc;
        List<MenuItemInfo> items = new List<MenuItemInfo>();

        public HiddenMenuItems()
        {
            InitializeComponent();
            // offscreen, but has to be visible to show up in accessible objects
            this.Width = 1;
            this.Height = 1;
            this.Left = -100;
        }

        public void Add(ToolStripMenuItem item, bool runAsync = false)
        {
            if (string.IsNullOrEmpty(item.AccessibleName))
            {
                throw new Exception("This should not be an automation object");
            }
            items.Add(new MenuItemInfo() {  MenuItem = item, RunAsync = runAsync });
        }

        protected override AccessibleObject CreateAccessibilityInstance()
        {
            if (this.acc == null) 
                this.acc = new AccessibleHiddenMenuItems(this, items);
            return this.acc;
        }
    }

    internal class AccessibleHiddenMenuItems : Control.ControlAccessibleObject
    {
        DelayedActions actions = new DelayedActions();
        HiddenMenuItems _owner;
        List<MenuItemInfo> _items;
        List<AccessibleMenuItem> _accs;

        public AccessibleHiddenMenuItems(HiddenMenuItems owner, List<MenuItemInfo> items) : base(owner)
        {
            this._owner = owner;
            this._items = items;
            this._accs = new List<AccessibleMenuItem>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                this._accs.Add(null);
            }
        }

        public override int GetChildCount()
        {
            return _items.Count;
        }

        public override AccessibleObject GetChild(int index)
        {
            if (index >= 0 && index <= GetChildCount())
            {
                var result = _accs[index];
                if (result == null)
                {
                    result = new AccessibleMenuItem(this, _items[index]);
                    _accs[index] = result;
                }
                return result;
            }
            return null;
        }

        public override AccessibleRole Role => AccessibleRole.MenuItem;

        public override AccessibleObject Parent => _owner.AccessibilityObject;

        public override AccessibleStates State => AccessibleStates.Default;


        internal void AsyncInvoke(ToolStripMenuItem item)
        {
            this.actions.StartDelayedAction("invoke", item.PerformClick, TimeSpan.FromMilliseconds(1));
        }
    }

    internal class AccessibleMenuItem : AccessibleObject
    {
        AccessibleHiddenMenuItems _parent;
        MenuItemInfo _item;
        string _name;

        public AccessibleMenuItem(AccessibleHiddenMenuItems parent, MenuItemInfo item)
        {
            this._parent = parent;
            this._item = item;
            this._name = item.MenuItem.AccessibleName;
        }

        public override string Name { get => _name; set => _name = value; }

        public override AccessibleRole Role => AccessibleRole.MenuItem;

        public override AccessibleObject Parent => _parent;

        public override AccessibleStates State => AccessibleStates.Default;

        public override void DoDefaultAction()
        {
            if (this._item.RunAsync)
            {
                this._parent.AsyncInvoke(this._item.MenuItem);
                System.Threading.Thread.Sleep(100);
            }
            else
            {
                this._item.MenuItem.PerformClick();
            }
        }
    }

}
