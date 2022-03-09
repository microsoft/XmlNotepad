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
        private ComboBox _location;
        private MostRecentlyUsed _values;

        public RecentlyUsedComboBox(MostRecentlyUsed items, ComboBox location)
        {
            this._values = items;
            items.RecentItemsChanged += OnRecentItemsChanged;
            this._location = location;
            this._location.SelectedIndexChanged += new EventHandler(OnSelectedIndexChanged);
            this._location.KeyDown += new KeyEventHandler(OnLocationKeyDown);
            this._location.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            this._location.AutoCompleteSource = AutoCompleteSource.CustomSource;
        }

        public bool SelectFirstItemByDefault { get; set; }

        void OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (!this.addingFile)
            {
                ComboRecentItem item = this._location.SelectedItem as ComboRecentItem;
                if (item != null)
                {
                    this._values.OnRecentFileSelected(item.Value);
                }
            }
        }

        void OnLocationKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // user has entered something new?
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (!string.IsNullOrEmpty(this._location.Text))
                {
                    string value = null;
                    value = this._location.Text;
                    this._values.OnRecentFileSelected(value);
                }
                else if (this._location.SelectedItem is ComboRecentItem item && item != null)
                {
                    this._values.OnRecentFileSelected(item.Value);
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this._location.DroppedDown = false;
            }
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
                this._location.Items.Clear();
                for (int i = recentValues.Length - 1; i >= 0; i--)
                {
                    string item = recentValues[i];
                    this._location.Items.Add(new ComboRecentItem(item));
                }

                if (this._location.Items.Count > 0 && this.SelectFirstItemByDefault)
                {
                    this._location.SelectedIndex = 0;
                }

                // sync autocompletion list
                this._location.AutoCompleteCustomSource.Clear();
                for (int i = 0; i < this._location.Items.Count; i++)
                {
                    ComboRecentItem item = (ComboRecentItem)this._location.Items[i];
                    this._location.AutoCompleteCustomSource.Add(item.ToString());
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
