using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace XmlNotepad {

    // For some unexplainable reason the TabIndexes defined in the Form 
    // are not being honored by the default TAB navigation provided by the base Form so
    // we are implementing it ourselves here.
    class TabNavigator {
        Control owner;

        public TabNavigator(Control c) {
            owner = c;
        }

        internal class TabStopControl : IComparable {
            Control c;
            public TabStopControl(Control c) {
                this.c = c;
            }
            public int CompareTo(object obj) {
                TabStopControl t = (TabStopControl)obj;
                if (t != null) {
                    return this.c.TabIndex - t.c.TabIndex;
                }
                return 0;
            }
            public Control Control { get { return c; } }
        }

        List<TabStopControl> tabStops;


        void FindTabStops(Control.ControlCollection children, List<TabStopControl> list) {
            foreach (Control c in children) {
                if (c.TabStop) {
                    list.Add(new TabStopControl(c));
                }
                if (c.Controls != null) {
                    FindTabStops(c.Controls, list);
                }
            }
        }

        public void HandleTab(KeyEventArgs e) {
            // Build list of TabStop controls in the form.
            if (tabStops == null) {
                tabStops = new List<TabStopControl>();
                FindTabStops(owner.Controls, tabStops);
                tabStops.Sort();
            }

            // Find the current focussed control.
            int i = 0;
            for (int len = tabStops.Count - 1; i < len; i++) {
                TabStopControl t = tabStops[i];
                if (t.Control.Focused) {
                    break;
                }
            }

            // Find the next in the specified direction, skipping currently invisible controls
            bool forward = (e.Modifiers & Keys.Shift) == 0;
            int dir = forward ? 1 : -1;
            i += dir;
            Control next = null;
            bool wrapped = false;
            while (next == null) {
                if (i < 0) {
                    if (wrapped) break;
                    i = tabStops.Count - 1;
                    wrapped = true;
                }
                if (i >= tabStops.Count) {
                    if (wrapped) break;
                    i = 0;
                    wrapped = true;
                }
                TabStopControl t = tabStops[i];
                if (t.Control.Visible) {
                    next = t.Control;
                    break;
                }
                i += dir;
            }

            // Now focus the next control.
            if (next != null) {
                next.Focus();
                e.Handled = true;
            }
        }
    }
}
