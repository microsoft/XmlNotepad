using System;
using System.Collections;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace XmlNotepad
{
    public class NoBorderTabControlEventArgs
    {
        private NoBorderTabPage _page;

        public NoBorderTabControlEventArgs(NoBorderTabPage page)
        {
            this._page = page;
        }
        public NoBorderTabPage TabPage
        {
            get { return this._page; }
        }
    }

    public delegate void NoBorderTabControlEventHandler(object sender, NoBorderTabControlEventArgs args);

    public class NoBorderTabControl : UserControl
    {
        private TabControl _tabs;
        private TabPageCollection _pages;
        public delegate void PageEventHandler(object sender, PageEventArgs args);
        public event NoBorderTabControlEventHandler Selected;

        public NoBorderTabControl()
        {
            _pages = new TabPageCollection();
            _tabs = new TabControl();

            this.Controls.Add(_tabs);
            _pages.PageAdded += new PageEventHandler(OnPageAdded);
            _pages.PageRemoved += new PageEventHandler(OnPageRemoved);
            _tabs.SelectedIndexChanged += new EventHandler(OnTabsSelectedIndexChanged);
        }

        public TabControl Tabs => _tabs;

        public int SelectedIndex
        {
            get { return _tabs.SelectedIndex; }
            set { _tabs.SelectedIndex = value; }
        }

        public NoBorderTabPage SelectedTab
        {
            get
            {
                return (NoBorderTabPage)_pages[_tabs.SelectedIndex];
            }
            set
            {
                foreach (NoBorderTabPage p in _pages)
                {
                    if (p == value)
                    {
                        this._tabs.SelectedTab = p.Page;
                        break;
                    }
                }
            }
        }

        void OnTabsSelectedIndexChanged(object sender, EventArgs e)
        {
            TabPage page = _tabs.SelectedTab;
            foreach (NoBorderTabPage p in _pages)
            {
                if (p.Page == page)
                {
                    if (Selected != null)
                    {
                        Selected(this, new NoBorderTabControlEventArgs(p));
                    }
                    this.Controls.SetChildIndex(p, 0); // put it on top!
                    break;
                }
            }
        }

        void OnPageRemoved(object sender, PageEventArgs e)
        {
            NoBorderTabPage page = e.Page;
            _tabs.TabPages.Remove(page.Page);
            if (this.Controls.Contains(page))
            {
                this.Controls.Remove(page);
            }
        }

        void OnPageAdded(object sender, PageEventArgs e)
        {
            NoBorderTabPage page = e.Page;
            if (e.Index >= _tabs.TabPages.Count)
            {
                _tabs.TabPages.Add(page.Page);
            }
            else
            {
                _tabs.TabPages.Insert(e.Index, page.Page);
            }
            if (!this.Controls.Contains(page))
            {
                this.Controls.Add(page);
                this.Controls.SetChildIndex(page, this.TabPages.IndexOf(page));
            }
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            NoBorderTabPage page = e.Control as NoBorderTabPage;
            if (page != null && !_tabs.TabPages.Contains(page.Page))
            {
                _pages.Add(page);
            }
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);
            NoBorderTabPage page = e.Control as NoBorderTabPage;
            if (page != null && _tabs.TabPages.Contains(page.Page))
            {
                _pages.Remove(page);
            }
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            _tabs.MinimumSize = new Size(10, 10);
            Size s = _tabs.GetPreferredSize(new Size(this.Width, 20));
            int height = _tabs.ItemSize.Height + _tabs.Padding.Y;
            _tabs.Bounds = new Rectangle(0, 0, this.Width, height);

            foreach (NoBorderTabPage p in this.TabPages)
            {
                p.Bounds = new Rectangle(0, height, this.Width, this.Height - height);
            }
        }

        public TabPageCollection TabPages
        {
            get
            {
                return _pages;
            }
        }

        public class PageEventArgs : EventArgs
        {
            int index;
            NoBorderTabPage page;

            public PageEventArgs(NoBorderTabPage page, int index)
            {
                this.page = page;
                this.index = index;
            }

            public NoBorderTabPage Page
            {
                get
                {
                    return this.page;
                }
                set
                {
                    this.page = value;
                }
            }

            public int Index
            {
                get
                {
                    return this.index;
                }
                set
                {
                    this.index = value;
                }
            }
        }

        public class TabPageCollection : IList
        {
            ArrayList list = new ArrayList();

            public event PageEventHandler PageAdded;
            public event PageEventHandler PageRemoved;

            void OnPageAdded(NoBorderTabPage page, int index)
            {
                PageAdded(this, new PageEventArgs(page, index));
            }

            void OnPageRemoved(NoBorderTabPage page)
            {
                PageRemoved(this, new PageEventArgs(page, 0));
            }

            #region IList Members

            public int Add(object value)
            {
                int index = list.Count;
                list.Add(value);
                OnPageAdded((NoBorderTabPage)value, index);
                return index;
            }

            public void Clear()
            {
                foreach (NoBorderTabPage page in list)
                {
                    OnPageRemoved(page);
                }
                list.Clear();
            }

            public bool Contains(object value)
            {
                return list.Contains(value);
            }

            public int IndexOf(object value)
            {
                return list.IndexOf(value);
            }

            public void Insert(int index, object value)
            {
                list.Insert(index, value);
                OnPageAdded((NoBorderTabPage)value, index);
            }

            public bool IsFixedSize
            {
                get { return false; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public void Remove(object value)
            {
                OnPageRemoved((NoBorderTabPage)value);
                list.Remove(value);
            }

            public void RemoveAt(int index)
            {
                if (index >= 0 && index < list.Count)
                {
                    OnPageRemoved((NoBorderTabPage)list[index]);
                    list.RemoveAt(index);
                }
            }

            public object this[int index]
            {
                get
                {
                    return list[index];
                }
                set
                {
                    RemoveAt(index);
                    if (value != null)
                    {
                        Insert(index, value);
                    }
                }
            }

            #endregion

            #region ICollection Members

            public void CopyTo(Array array, int index)
            {
                list.CopyTo(array, index);
            }

            public int Count
            {
                get { return list.Count; }
            }

            public bool IsSynchronized
            {
                get { return list.IsSynchronized; }
            }

            public object SyncRoot
            {
                get { return list.SyncRoot; }
            }

            #endregion

            #region IEnumerable Members

            public IEnumerator GetEnumerator()
            {
                return list.GetEnumerator();
            }

            #endregion
        }
    }

    public class NoBorderTabPage : Panel
    {
        private TabPage _page = new TabPage();

        public NoBorderTabPage()
        {
        }

        [System.ComponentModel.Browsable(false)]
        internal TabPage Page
        {
            get { return this._page; }
        }

        public override string Text
        {
            get
            {
                return this._page.Text;
            }
            set
            {
                this._page.Text = value;
            }
        }

    }
}
