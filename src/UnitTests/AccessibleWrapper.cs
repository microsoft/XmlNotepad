using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;
using WindowsInput;

namespace UnitTests
{

    public class FileComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            // The operating system may or may not have visible "file extensions" so we do an extensionless compare
            return string.Compare(Path.GetFileNameWithoutExtension(a), Path.GetFileNameWithoutExtension(b), StringComparison.CurrentCultureIgnoreCase);
        }
    }

    public class StringComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            return String.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    public class AutomationWrapper
    {
        AutomationElement e;

        public AutomationWrapper(AutomationElement e)
        {
            this.e = e;
        }

        internal AutomationElement AutomationElement { get { return e; } }

        public static AutomationWrapper AccessibleObjectForWindow(IntPtr hwnd)
        {
            AutomationElement e = AutomationElement.FromHandle(hwnd);
            if (e == null)
            {
                throw new Exception("Automation element not found for this window");
            }
            return new AutomationWrapper(e);
        }

        internal static AutomationWrapper AccessibleObjectAt(Point center)
        {
            // The given point must have already been converted to physical screen
            // coordinates.
            AutomationElement e = AutomationElement.FromPoint(new System.Windows.Point(center.X, center.Y));
            if (e == null)
            {
                throw new Exception("Automation element not found at this location: " + center.ToString());
            }
            return new AutomationWrapper(e);
        }

        public string Name
        {
            get
            {
                try
                {
                    return e.Current.Name;
                }
                catch
                {
                    return "";
                }
            }
        }

        public ControlType ControlType
        {
            get
            {
                try
                {
                    return e.Current.ControlType;
                }
                catch
                {
                    return ControlType.Custom;
                }
            }
        }

        public string ClassName
        {
            get
            {
                try
                {
                    return e.Current.ClassName;
                }
                catch
                {
                    return "";
                }
            }
        }


        public AutomationWrapper Parent
        {
            get
            {
                AutomationElement parent = null;
                try
                {
                    parent = TreeWalker.RawViewWalker.GetParent(e);
                }
                catch
                {
                }
                if (parent != null)
                {
                    return new AutomationWrapper(parent);
                }
                throw new Exception("Element has no parent");
            }
        }

        public string KeyboardShortcut { get { return e.Current.AcceleratorKey; } }

        public string Help { get { return e.Current.HelpText; } }

        public string Role { get { return e.Current.ControlType.LocalizedControlType; } }

        public string Status { get { return e.Current.ItemStatus; } }


        public Rectangle Bounds
        {
            get { return e.Current.BoundingRectangle.ToRectangle(); }
        }

        public int GetChildCount()
        {
            var children = GetChildren();
            return (children == null) ? 0 : children.Length;
        }

        public bool IsVisible { get { return !e.Current.IsOffscreen; } }

        public AutomationWrapper FindChild(string name)
        {
            AutomationElement child = e.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, name, PropertyConditionFlags.IgnoreCase));
            if (child != null)
            {
                return new AutomationWrapper(child);
            }
            throw new Exception(string.Format("Child '{0}' not found", name));
        }

        public void Invoke()
        {
            InvokePattern ip = e.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
            if (ip != null)
            {
                ip.Invoke();
                return;
            }
            throw new Exception("Element '" + e.Current.Name + "' does not support InvokePattern");
        }

        public string Value
        {
            get
            {
                if (e.TryGetCurrentPattern(ValuePattern.Pattern, out object o))
                {
                    ValuePattern vp = (ValuePattern)o;
                    return vp.Current.Value;
                }
                else
                {
                    // must not be a leaf node then, unfortunately the mapping from IAccessible to AutomationElement
                    // don't add ValuePattern on ListItems that have children, so we have to get the value the hard way.
                    Thread.Sleep(100);
                    SendKeys.SendWait("{ENTER}");
                    Thread.Sleep(300);
                    SendKeys.SendWait("^c");
                    string text = Clipboard.GetText();
                    Thread.Sleep(300);
                    SendKeys.SendWait("{ENTER}");
                    Thread.Sleep(300);
                    return text;
                }
            }
            set
            {
                if (e.TryGetCurrentPattern(ValuePattern.Pattern, out object o))
                {
                    ValuePattern vp = (ValuePattern)o;
                    vp.SetValue(value);
                }
                else
                {
                    // must not be a leaf node then, unfortunately the mapping from IAccessible to AutomationElement
                    // don't add ValuePattern on ListItems that have children, so we have to get the value the hard way.
                    Thread.Sleep(100);
                    SendKeys.SendWait("{ENTER}");
                    Thread.Sleep(300);
                    Clipboard.SetText(value);
                    SendKeys.SendWait("^v");
                    Thread.Sleep(300);
                    SendKeys.SendWait("{ENTER}");
                    Thread.Sleep(300);
                }
            }
        }

        #region Manage Children

        public AutomationWrapper FirstChild
        {
            get
            {
                AutomationElement child = TreeWalker.RawViewWalker.GetFirstChild(e);
                if (child != null)
                {
                    return new AutomationWrapper(child);
                }
                throw new Exception("FirstChild not found");
            }
        }

        public AutomationWrapper LastChild
        {
            get
            {
                AutomationElement child = TreeWalker.RawViewWalker.GetLastChild(e);
                if (child != null)
                {
                    return new AutomationWrapper(child);
                }
                throw new Exception("LastChild not found");
            }
        }

        public AutomationWrapper NextSibling
        {
            get
            {
                AutomationElement next = TreeWalker.RawViewWalker.GetNextSibling(e);
                if (next != null)
                {
                    return new AutomationWrapper(next);
                }
                throw new Exception("There is no next sibling");
            }
        }

        public AutomationWrapper PreviousSibling
        {
            get
            {
                AutomationElement next = TreeWalker.RawViewWalker.GetPreviousSibling(e);
                if (next != null)
                {
                    return new AutomationWrapper(next);
                }
                throw new Exception("There is no previous sibling");
            }
        }


        public ScrollAutomationWrapper FindScroller()
        {
            var parent = this;

            while (true)
            {
                foreach (var child in parent.GetChildren())
                {
                    var name = child.AutomationElement.Current.ClassName;
                    if (name.StartsWith("WindowsForms10.SCROLLBAR."))
                    {
                        // we have a legacy scrollbar.
                        return new ScrollAutomationWrapper(child, null);
                    }
                }

                AutomationElement scrollbar = parent.AutomationElement.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ScrollBar));
                if (scrollbar != null)
                {
                    return new ScrollAutomationWrapper(null, new AutomationWrapper(scrollbar));
                }

                var next = TreeWalker.RawViewWalker.GetParent(parent.AutomationElement);
                if (next == null)
                {
                    throw new Exception("Scroller not found");
                }
                parent = new AutomationWrapper(next);
            }

        }

        public AutomationWrapper FindDescendant(string name)
        {
            AutomationElement child = this.e.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, name));
            if (child != null)
            {
                return new AutomationWrapper(child);
            }
            throw new Exception(string.Format("Descendant named '{0}' not found", name));
        }

        public AutomationWrapper[] GetChildren()
        {
            List<AutomationWrapper> list = new List<AutomationWrapper>();
            foreach (AutomationElement child in this.e.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.IsOffscreenProperty, false)))
            {
                list.Add(new AutomationWrapper(child));
            }
            return list.ToArray();
        }

        internal AutomationWrapper GetChild(int index)
        {
            AutomationWrapper[] children = GetChildren();
            if (index >= 0 && index < children.Length)
            {
                return children[index];
            }
            throw new Exception(string.Format("Child at index {0} not found, there are only {1} children", index, children.Length));
        }

        internal AutomationWrapper HitTest(int x, int y)
        {
            var node = AutomationWrapper.AccessibleObjectAt(new Point(x, y));
            return node;
        }

        #endregion

        #region Selection 

        public void Select()
        {
            SelectionItemPattern vp = e.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
            if (vp != null)
            {
                vp.Select();
                return;
            }
            throw new Exception("Element does not support SelectionItemPattern");
        }

        public void AddToSelection()
        {
            SelectionItemPattern vp = e.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
            if (vp != null)
            {
                vp.AddToSelection();
                return;
            }
            throw new Exception("Element does not support SelectionItemPattern");
        }

        public void RemoveFromSelection()
        {
            SelectionItemPattern vp = e.GetCurrentPattern(SelectionItemPattern.Pattern) as SelectionItemPattern;
            if (vp != null)
            {
                vp.RemoveFromSelection();
                return;
            }
            throw new Exception("Element does not support SelectionItemPattern");
        }

        internal AutomationWrapper GetSelectedChild()
        {
            SelectionPattern sp = e.GetCurrentPattern(SelectionPattern.Pattern) as SelectionPattern;
            if (sp != null)
            {
                foreach (AutomationElement selected in sp.Current.GetSelection())
                {
                    return new AutomationWrapper(selected);
                }
            }
            throw new Exception("No child is selected");
        }



        #endregion 

        #region Focus

        public static AutomationWrapper Focus
        {
            get { return new AutomationWrapper(AutomationElement.FocusedElement); }
        }

        public void SetFocus()
        {
            e.SetFocus();
        }

        public bool IsFocused
        {
            get
            {
                return this.e.Current.HasKeyboardFocus;
            }
        }
        #endregion 

        #region Toggle Items

        public bool IsChecked
        {
            get
            {
                TogglePattern ep = this.e.GetCurrentPattern(TogglePattern.Pattern) as TogglePattern;
                if (ep != null)
                {
                    return ep.Current.ToggleState == ToggleState.On;
                }
                throw new Exception("Element does not support TogglePattern");
            }
            set
            {
                TogglePattern ep = this.e.GetCurrentPattern(TogglePattern.Pattern) as TogglePattern;
                if (ep != null)
                {
                    ToggleState target = ToggleState.Off;
                    if (value)
                    {
                        target = ToggleState.On;
                    }

                    var start = ep.Current.ToggleState;
                    while (ep.Current.ToggleState != target)
                    {
                        ep.Toggle();
                        if (ep.Current.ToggleState == start)
                        {
                            throw new Exception("Element is not toggling, enabled state is " + e.Current.IsEnabled);
                        }
                    }
                    return;
                }
                throw new Exception("Element does not support TogglePattern");
            }
        }
        #endregion 

        #region expandible

        public bool IsExpanded
        {
            get
            {
                // we cheat in the TreeView implementation and return the expanded state in the Help field.
                // The reason for this is the mapping from IAccessible to AutomationElement only allows one control type.
                // We want ListItem and OutlineItem, but we can't do both.  We need the SelectionItemPattern of ListItem
                // so we go with that, and cheat on expandable state.
                string help = this.Help;
                if (help == "expanded")
                {
                    return true;
                }
                return false;
            }
            set
            {
                bool expanded = IsExpanded;
                if (value)
                {
                    if (!expanded)
                    {
                        Invoke();
                    }
                }
                else
                {
                    if (expanded)
                    {
                        Invoke();
                    }
                }
            }
        }
        #endregion 

        public IntPtr Hwnd { get { return new IntPtr(this.e.Current.NativeWindowHandle); } }

        #region DPI scaling

        public Point LogicalToPhysicalPoint(Point logical)
        {
            NativeMethods.POINT pt = new NativeMethods.POINT()
            {
                x = logical.X,
                y = logical.Y
            };
            NativeMethods.LogicalToPhysicalPointForPerMonitorDPI(this.Hwnd, ref pt);
            return new Point(pt.x, pt.y);
        }

        public Point PhysicalToLogicalPoint(Point physical)
        {
            NativeMethods.POINT pt = new NativeMethods.POINT()
            {
                x = physical.X,
                y = physical.Y
            };
            NativeMethods.PhysicalToLogicalPointForPerMonitorDPI(this.Hwnd, ref pt);
            return new Point(pt.x, pt.y);
        }

        static class NativeMethods
        {
            public struct POINT
            {
                public int x;
                public int y;
            }

            [DllImport("User32", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool LogicalToPhysicalPointForPerMonitorDPI(
              IntPtr hWnd,
              ref POINT lpPoint
            );


            [DllImport("User32", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool PhysicalToLogicalPointForPerMonitorDPI(
              IntPtr hWnd,
              ref POINT lpPoint
            );

            [DllImport("User32", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool LogicalToPhysicalPoint(
              IntPtr hWnd,
              ref POINT lpPoint
            );


            [DllImport("User32", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool PhysicalToLogicalPoint(
              IntPtr hWnd,
              ref POINT lpPoint
            );

        }

        #endregion
    }


    public static class WinFormsExtensions
    {
        public static Rectangle ToRectangle(this System.Windows.Rect r)
        {
            return new Rectangle((int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height);
        }

        public static Point Center(this Rectangle bounds)
        {
            return new Point(bounds.Left + (bounds.Width / 2),
                bounds.Top + (bounds.Height / 2));
        }
    }

    public class FileDialogWrapper
    {
        Window dialog;

        public FileDialogWrapper(Window window)
        {
            this.dialog = window;
        }

        public Window Window => this.dialog;

        public AutomationWrapper GetFileItem(string fileName)
        {
            AutomationWrapper items = dialog.AccessibleObject.FindDescendant("Items View");
            if (items != null)
            {
                AutomationElement item = items.AutomationElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, fileName, PropertyConditionFlags.IgnoreCase));

                while (item == null)
                {
                    if (ScrollList(items, ScrollAmount.LargeIncrement) >= 100)
                    {
                        break;
                    }
                    item = items.AutomationElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NameProperty, fileName, PropertyConditionFlags.IgnoreCase));
                }
                if (item != null)
                {
                    return new AutomationWrapper(item);
                }
            }


            throw new Exception("File '" + fileName + "'not found");
        }

        private double ScrollList(AutomationWrapper items, ScrollAmount amount)
        {
            ScrollPattern sp = items.AutomationElement.GetCurrentPattern(ScrollPattern.Pattern) as ScrollPattern;
            if (sp.Current.VerticallyScrollable)
            {
                sp.ScrollVertical(amount);
            }
            double percent = sp.Current.VerticalScrollPercent;
            return percent;
        }

        internal void DismissPopUp(string keyStrokes)
        {
            this.dialog.DismissPopUp(keyStrokes);
        }

        internal void SendKeystrokes(string keyStrokes)
        {
            this.dialog.SendKeystrokes(keyStrokes);
        }

        internal void WaitForInteractive()
        {
            this.dialog.WaitForInteractive();
        }

        internal void Cancel()
        {
            this.DismissPopUp("%{F4}");
        }

        public string FileName
        {
            get
            {
                AutomationWrapper wrapper = this.dialog.FindDescendant("File name:", ControlType.Edit);
                return wrapper.Value;
            }
            set
            {
                AutomationWrapper wrapper = this.dialog.FindDescendant("File name:", ControlType.Edit);
                wrapper.Value = value;
            }
        }
    }

    public class FindDialog
    {
        Window w;

        public FindDialog(Window w)
        {
            this.w = w;
        }

        public Window Window { get { return this.w; } }

        public bool MatchCase
        {
            get
            {
                return GetCheckedState("checkBoxMatchCase");

            }
            set
            {
                SetCheckedState("checkBoxMatchCase", value);
            }
        }

        public bool UseWholeWord
        {
            get
            {
                return GetCheckedState("checkBoxWholeWord");

            }
            set
            {
                SetCheckedState("checkBoxWholeWord", value);
            }
        }

        public bool UseRegex
        {
            get
            {
                return GetCheckedState("checkBoxRegex");

            }
            set
            {
                SetCheckedState("checkBoxRegex", value);
            }
        }

        public bool UseXPath
        {
            get
            {
                return GetCheckedState("checkBoxXPath");

            }
            set
            {
                SetCheckedState("checkBoxXPath", value);
            }
        }

        public string FindString
        {
            get
            {
                AutomationWrapper c = w.FindDescendant("comboBoxFind");
                return c.Value;
            }
            set
            {
                AutomationWrapper c = w.FindDescendant("comboBoxFind");
                c.Value = value;
            }
        }


        internal void ClearFindCheckBoxes()
        {
            MatchCase = false;
            UseWholeWord = false;
            UseRegex = false;
            UseXPath = false;
            FindString = "";
        }

        private bool GetCheckedState(string name)
        {
            AutomationWrapper s = w.FindDescendant(name);
            foreach (var p in s.AutomationElement.GetSupportedProperties())
            {
                if (p.ProgrammaticName == "TogglePatternIdentifiers.ToggleStateProperty")
                {
                    var value = s.AutomationElement.GetCurrentPropertyValue(p);
                    if (value != null && value.ToString() == "On")
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void SetCheckedState(string name, bool state)
        {
            if (this.GetCheckedState(name) == state)
            {
                return;
            }
            // seems to be a bug in the AutomationElement mapping, checkbox is not supporting the TogglePattern!
            AutomationWrapper s = w.FindDescendant(name);
            Rectangle r = s.Bounds;
            Mouse.MouseClick(r.Center(), MouseButtons.Left);
            Thread.Sleep(200);
        }

        internal void FocusFindString()
        {
            AutomationWrapper findCombo = this.w.FindDescendant("comboBoxFind");
            Mouse.MouseClick(findCombo.Bounds.Center(), MouseButtons.Left);
        }
    }

    public class ScrollAutomationWrapper
    {
        AutomationWrapper legacyScrollbar;
        AutomationWrapper properScroller;

        public ScrollAutomationWrapper(AutomationWrapper legacyScrollbar, AutomationWrapper properScroller)
        {
            this.legacyScrollbar = legacyScrollbar;
            this.properScroller = properScroller;
        }

        public InputSimulator Simulator { get; internal set; }

        public void PageDown()
        {
            if (this.legacyScrollbar != null)
            {
                // Unfortunately UIAutomationClient is missing support for ILegacyAutomation interfaces, so the scrollbar
                // is returns as one opaque "pane".
                var bounds = legacyScrollbar.Bounds;
                // hit the down arrow is the best we can do.
                Simulator.Mouse.MoveMouseTo(bounds.Left + 10, bounds.Bottom - 10).LeftButtonClick();
            }
            else
            {
                ScrollPattern sp = (ScrollPattern)properScroller.AutomationElement.GetCurrentPattern(ScrollPattern.Pattern);
                sp.ScrollVertical(ScrollAmount.LargeDecrement);
            }
        }

        public void PageUp()
        {
            if (this.legacyScrollbar != null)
            {
                // Unfortunately UIAutomationClient is missing support for ILegacyAutomation interfaces, so the scrollbar
                // is returns as one opaque "pane".
                var bounds = legacyScrollbar.Bounds;
                // hit the up arrow is the best we can do.
                Simulator.Mouse.MoveMouseTo(bounds.Left + 10, bounds.Top + 10).LeftButtonClick();
            }
            else
            {
                ScrollPattern sp = (ScrollPattern)properScroller.AutomationElement.GetCurrentPattern(ScrollPattern.Pattern);
                sp.ScrollVertical(ScrollAmount.LargeIncrement);
            }
        }

        public void ScrollIntoView(AutomationWrapper element, AutomationWrapper container)
        {
            do
            {
                var r = element.Bounds;
                var tb = container.Bounds;
                if (r.Y < tb.Y)
                {
                    Debug.WriteLine("Paging up because {0} < {1}", r.Y, tb.Y);
                    this.PageUp();
                    Thread.Sleep(500);
                    var s = element.Bounds;
                    if (s == r)
                    {
                        // item didn't move, so the page keys are not working, try up arrow.
                        Debug.WriteLine("???");
                    }
                }
                else if (r.Bottom > tb.Bottom)
                {
                    Debug.WriteLine("Paging down because {0} > {1}", r.Y, tb.Y);
                    this.PageDown();
                    Thread.Sleep(500);
                    var s = element.Bounds;
                    if (s == r)
                    {
                        // item didn't move, so the page keys are not working, try down arrow.
                        Debug.WriteLine("???");
                    }
                }
                else
                {
                    return;
                }
            } while (true);
        }

    }

    class MainWindowWrapper
    {
        Window window;
        AutomationWrapper xslOutputTab;
        AutomationWrapper xmlTreeViewTab;
        AutomationWrapper xsltViewer;

        public MainWindowWrapper(Window w)
        {
            this.window = w;
        }

        public void LoadXmlAddress(string url, string expectedXmlPrefix)
        {
            Trace.WriteLine("Click in the combo box location field");
            AutomationWrapper comboBoxLocation = this.window.FindDescendant("comboBoxLocation");
            Rectangle bounds = comboBoxLocation.Bounds;
            Mouse.MouseClick(bounds.Center(), MouseButtons.Left);

            Trace.WriteLine("Loading: " + url);
            this.window.SendKeystrokes("{END}+{HOME}" + url + "{ENTER}");

            Trace.WriteLine("Wait for rss to be loaded");
            WaitForText("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        }

        void WaitForText(string value)
        {
            int retries = 20;
            string clip = null;
            while (retries-- > 0)
            {
                this.window.SendKeystrokes("^c");
                clip = Clipboard.GetText();
                Trace.WriteLine("clip=" + clip);
                if (clip == value)
                    return;
                Sleep(2000);
            }
            throw new Exception("Not finding expected text '" + value + "', instead we got '" + clip + "'");
        }

        public AutomationWrapper GetXsltViewer()
        {
            if (xsltViewer == null)
            {
                xsltViewer = this.window.FindDescendant("xsltViewer");
            }
            return xsltViewer;
        }

        public void EnterXslFilename(string filename)
        {
            AutomationWrapper s = GetXsltViewer().FindDescendant("SourceFileName");
            Rectangle bounds = s.Bounds;
            Mouse.MouseClick(bounds.Center(), MouseButtons.Left);
            Sleep(500);
            this.window.SendKeystrokes("{END}+{HOME}" + filename + "{ENTER}");
        }

        internal void EnterXmlOutputFilename(string filename)
        {
            AutomationWrapper s = GetXsltViewer().FindDescendant("OutputFileName");
            Rectangle bounds = s.Bounds;
            Mouse.MouseClick(bounds.Center(), MouseButtons.Left);
            Sleep(500);
            this.window.SendKeystrokes("{END}+{HOME}" + filename);
        }

        internal string GetXmlOutputFilename()
        {
            AutomationWrapper s = GetXsltViewer().FindDescendant("OutputFileName");
            return s.Value;
        }

        internal void InvokeTransformButton()
        {
            AutomationWrapper s = GetXsltViewer().FindDescendant("TransformButton");
            s.Invoke();
        }

        public string CopyHtml()
        {
            Rectangle bounds = GetXsltViewer().Bounds;
            // click in HTML view
            Mouse.MouseClick(bounds.Center(), MouseButtons.Left);

            // select all the text
            Sleep(1000);
            this.window.SendKeystrokes("^a");

            Sleep(1000);
            this.window.SendKeystrokes("^c");
            return Clipboard.GetText();
        }

        public void Sleep(int ms)
        {
            Thread.Sleep(ms);
        }

        public void ShowXslt()
        {
            if (xslOutputTab == null)
            {
                AutomationWrapper tabControl = this.window.FindDescendant("tabControlViews");
                xslOutputTab = tabControl.FindDescendant("XSL Output");
            }
            var bounds = xslOutputTab.Bounds;
            Trace.WriteLine("Select XSL output tab");
            Mouse.MouseClick(new Point(bounds.Left + (bounds.Right - bounds.Left) / 2, bounds.Top + 5), MouseButtons.Left);
            Sleep(1000);
        }

        internal void ShowXmlTree()
        {
            if (xmlTreeViewTab == null)
            {
                AutomationWrapper tabControl = this.window.FindDescendant("tabControlViews");
                xmlTreeViewTab = tabControl.FindDescendant("Tree View");
            }
            var bounds = xmlTreeViewTab.Bounds;
            Trace.WriteLine("Select XML tree view tab");
            Mouse.MouseClick(new Point(bounds.Left + (bounds.Right - bounds.Left) / 2, bounds.Top + 5), MouseButtons.Left);
            Sleep(100);
        }
    }

}
