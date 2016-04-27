using System;
using System.Collections;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace XmlNotepad {
    public class NoBorderTabControlEventArgs {
        NoBorderTabPage page;
        public NoBorderTabControlEventArgs(NoBorderTabPage page){
            this.page = page;
        }
        public NoBorderTabPage TabPage {
            get { return this.page; }
        }
    }
    public delegate void NoBorderTabControlEventHandler(object sender, NoBorderTabControlEventArgs args);
    public class NoBorderTabControl : UserControl {
        TabControl tabs;
        TabPageCollection pages;
        public delegate void PageEventHandler(object sender, PageEventArgs args);
        public event NoBorderTabControlEventHandler Selected;

        public NoBorderTabControl() {
            pages = new TabPageCollection();
            tabs = new TabControl();
            
            this.Controls.Add(tabs);
            pages.PageAdded += new PageEventHandler(OnPageAdded);
            pages.PageRemoved += new PageEventHandler(OnPageRemoved);
            tabs.SelectedIndexChanged += new EventHandler(OnTabsSelectedIndexChanged);
        }

        public int SelectedIndex {
            get { return tabs.SelectedIndex; }
            set { tabs.SelectedIndex = value; }
        }

        public NoBorderTabPage SelectedTab {
            get { 
                return (NoBorderTabPage)pages[tabs.SelectedIndex];
            }
            set {
                foreach (NoBorderTabPage p in pages) {
                    if (p == value) {
                        this.tabs.SelectedTab = p.Page;
                        break;
                    }
                }
            }
        }

        void OnTabsSelectedIndexChanged(object sender, EventArgs e) {
            TabPage page = tabs.SelectedTab;
            foreach (NoBorderTabPage p in pages) {
                if (p.Page == page) {
                    if (Selected != null) {
                        Selected(this, new NoBorderTabControlEventArgs(p));
                    }
                    this.Controls.SetChildIndex(p, 0); // put it on top!
                    break;
                }
            }
        }

        void OnPageRemoved(object sender, PageEventArgs e) {
            NoBorderTabPage page = e.Page;
            tabs.TabPages.Remove(page.Page);
            if (this.Controls.Contains(page)) {
                this.Controls.Remove(page);
            }
        }

        void OnPageAdded(object sender, PageEventArgs e) {
            NoBorderTabPage page = e.Page;
            if (e.Index >= tabs.TabPages.Count) {
                tabs.TabPages.Add(page.Page);
            } else {
                tabs.TabPages.Insert(e.Index, page.Page);
            }
            if (!this.Controls.Contains(page)) {
                this.Controls.Add(page);
                this.Controls.SetChildIndex(page, this.TabPages.IndexOf(page));
            }
        }

        protected override void OnControlAdded(ControlEventArgs e) {
            base.OnControlAdded(e);
            NoBorderTabPage page = e.Control as NoBorderTabPage;
            if (page != null && !tabs.TabPages.Contains(page.Page)) {
                pages.Add(page);
            }
        }

        protected override void OnControlRemoved(ControlEventArgs e) {
            base.OnControlRemoved(e);
            NoBorderTabPage page = e.Control as NoBorderTabPage;
            if (page != null && tabs.TabPages.Contains(page.Page)) {
                pages.Remove(page);
            }
        }

        protected override void OnLayout(LayoutEventArgs e) {
            tabs.MinimumSize = new Size(10, 10);
            Size s = tabs.GetPreferredSize(new Size(this.Width, 20));
            int height = tabs.ItemSize.Height + tabs.Padding.Y;
            tabs.Bounds = new Rectangle(0, 0, this.Width, height);

            foreach (NoBorderTabPage p in this.TabPages) {
                p.Bounds = new Rectangle(0, height, this.Width, this.Height - height);
            }
        }

        public TabPageCollection TabPages { 
            get {
                return pages;
            }
        }

        public class PageEventArgs : EventArgs {
            int index;
            NoBorderTabPage page;

            public PageEventArgs(NoBorderTabPage page, int index) {
                this.page = page;
                this.index = index;
            }

            public NoBorderTabPage Page {
                get {
                    return this.page;
                }
                set {
                    this.page = value;
                }
            }

            public int Index {
                get {
                    return this.index;
                }
                set {
                    this.index = value;
                }
            }
        }

        public class TabPageCollection : IList {
            ArrayList list = new ArrayList();

            public event PageEventHandler PageAdded;
            public event PageEventHandler PageRemoved;

            void OnPageAdded(NoBorderTabPage page, int index) {
                PageAdded(this, new PageEventArgs(page, index));
            }

            void OnPageRemoved(NoBorderTabPage page) {
                PageRemoved(this, new PageEventArgs(page, 0));
            }

            #region IList Members

            public int Add(object value) {
                int index = list.Count;
                list.Add(value);
                OnPageAdded((NoBorderTabPage)value, index);
                return index;
            }

            public void Clear() {
                foreach (NoBorderTabPage page in list) {
                    OnPageRemoved(page);
                }
                list.Clear();
            }

            public bool Contains(object value) {
                return list.Contains(value);
            }

            public int IndexOf(object value) {
                return list.IndexOf(value);
            }

            public void Insert(int index, object value) {
                list.Insert(index, value);
                OnPageAdded((NoBorderTabPage)value, index);
            }

            public bool IsFixedSize {
                get { return false; }
            }

            public bool IsReadOnly {
                get { return false; }
            }

            public void Remove(object value) {
                OnPageRemoved((NoBorderTabPage)value); 
                list.Remove(value);
            }

            public void RemoveAt(int index) {
                if (index >= 0 && index < list.Count) {
                    OnPageRemoved((NoBorderTabPage)list[index]);
                    list.RemoveAt(index);
                }
            }

            public object this[int index] {
                get {
                    return list[index];
                }
                set {
                    RemoveAt(index);
                    if (value != null) {
                        Insert(index, value);
                    }
                }
            }

            #endregion

            #region ICollection Members

            public void CopyTo(Array array, int index) {
                list.CopyTo(array, index);
            }

            public int Count {
                get { return list.Count;  }
            }

            public bool IsSynchronized {
                get { return list.IsSynchronized; }
            }

            public object SyncRoot {
                get { return list.SyncRoot; }
            }

            #endregion

            #region IEnumerable Members

            public IEnumerator GetEnumerator() {
                return list.GetEnumerator();
            }

            #endregion
        }
    }

    public class NoBorderTabPage : Panel {
        TabPage page = new TabPage();

        public NoBorderTabPage() {            
        }

        [System.ComponentModel.Browsable(false)]
        internal TabPage Page {
            get { return this.page; }
        }

        public override string Text { 
            get {
                return this.page.Text;
            }
            set {
                this.page.Text = value;
            }
        }

    }
}
