using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace XmlNotepad
{
    public class MostRecentlyUsedEventArgs : EventArgs
    {
        public string Selection;

        public MostRecentlyUsedEventArgs(string selection)
        {
            this.Selection = selection;
        }
    }

    public class MostRecentlyUsed
    {
        private const int _maxRecentValues = 100;
        private List<string> _recentValues = new List<string>();

        public event EventHandler<MostRecentlyUsedEventArgs> RecentItemSelected;
        public event EventHandler RecentItemsChanged;

        public string[] GetLatestValues()
        {
            return _recentValues.ToArray();
        }

        public void Clear()
        {
            _recentValues.Clear();
        }

        public bool Contains(string s)
        {
            return _recentValues.Contains(s);
        }

        public void SetValues(string[] items)
        {
            Clear();
            if (items != null)
            {
                _recentValues = new List<string>(items);
            }
            FireRecentItemsChanged();
        }

        public void RemoveItem(string value)
        {
            if (this._recentValues.Contains(value))
            {
                this._recentValues.Remove(value);
                FireRecentItemsChanged();
            }
        }

        public void AddItem(string value)
        {
            try
            {
                // reposition the item at the top of the list.
                if (this._recentValues.Contains(value))
                {
                    this._recentValues.Remove(value);
                }

                this._recentValues.Add(value);

                if (this._recentValues.Count > _maxRecentValues)
                {
                    this._recentValues.RemoveAt(0);
                }

                FireRecentItemsChanged();
            }
            catch (Exception ex)
            {
            }
        }

        protected virtual void FireRecentItemsChanged()
        {
            if (RecentItemsChanged != null)
            {
                RecentItemsChanged(this, EventArgs.Empty);
            }
        }

        public void OnRecentFileSelected(string selected)
        {
            if (this.RecentItemSelected != null && selected != null)
            {
                this.RecentItemSelected(this, new MostRecentlyUsedEventArgs(selected));
            }
        }
    }

}
