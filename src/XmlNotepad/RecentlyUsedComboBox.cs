using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XmlNotepad
{
    public class RecentlyUsedComboBox
    {
        private const int _maxRecentMenuFiles = 20;
        bool addingFile;
        private ComboBox _box;
        private MostRecentlyUsed _values;

        public RecentlyUsedComboBox(MostRecentlyUsed items, ComboBox combo)
        {
            this._values = items;
            items.RecentItemsChanged += OnRecentItemsChanged;
            this._box = combo;
            this._box.SelectedIndexChanged += new EventHandler(OnSelectedIndexChanged);
            this._box.KeyDown += new KeyEventHandler(OnComboKeyDown);
            this._box.LostFocus += new EventHandler(OnComboLostFocus);
            this._box.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            this._box.AutoCompleteSource = AutoCompleteSource.CustomSource;
            this._values.RecentItemSelected += OnRecentSelected;
        }

        private void OnRecentSelected(object sender, MostRecentlyUsedEventArgs e)
        {
            this._box.Text = e.Selection;
        }

        public bool SelectFirstItemByDefault { get; set; }

        void OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (!this.addingFile)
            {
                ComboRecentItem item = this._box.SelectedItem as ComboRecentItem;
                if (item != null)
                {
                    this._values.OnRecentFileSelected(item.Value);
                }
            }
        }

        void OnComboKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                CommitTextValue();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                // user has entered something new?
                CommitTextValue();
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (!string.IsNullOrEmpty(this._box.Text))
                {
                    string value = null;
                    value = this._box.Text;
                    this._values.OnRecentFileSelected(value);
                }
                else if (this._box.SelectedItem is ComboRecentItem item && item != null)
                {
                    this._values.OnRecentFileSelected(item.Value);
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this._box.DroppedDown = false;
            }
        }

        void OnComboLostFocus(object sender, EventArgs e)
        {
            CommitTextValue();
        }

        private void CommitTextValue()
        {
            var text = this._box.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                this._values.AddItem(text);
                var item = FindItem(text);
                this._box.SelectedItem = item;
            }
        }

        private ComboRecentItem FindItem(string text)
        {
            foreach(ComboRecentItem item in this._box.Items)
            {
                if (item.Value == text)
                {
                    return item;
                }
            }
            return null;
        }


        private void OnRecentItemsChanged(object sender, EventArgs e)
        {
            // Synchronize menu items.
            this.addingFile = true;

            try
            {
                // Add most recent files first.
                string[] recentValues = this._values.GetLatestValues();

                // Synchronize combo-box items
                this._box.Items.Clear();
                for (int i = recentValues.Length - 1; i >= 0; i--)
                {
                    string item = recentValues[i];
                    this._box.Items.Add(new ComboRecentItem(item));
                }

                if (this._box.Items.Count > 0 && this.SelectFirstItemByDefault)
                {
                    this._box.SelectedIndex = 0;
                }

                // sync autocompletion list
                this._box.AutoCompleteCustomSource.Clear();
                for (int i = 0; i < this._box.Items.Count; i++)
                {
                    ComboRecentItem item = (ComboRecentItem)this._box.Items[i];
                    this._box.AutoCompleteCustomSource.Add(item.ToString());
                }
            }
            finally
            {
                this.addingFile = false;
            }
        }

        private class ComboRecentItem
        {
            public string Value;

            public ComboRecentItem(string value)
            {
                Value = value;
            }

            public override string ToString()
            {
                return Value;
            }
        }

    }
}
