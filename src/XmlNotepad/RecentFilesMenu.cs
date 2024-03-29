using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using SR = XmlNotepad.StringResources;

namespace XmlNotepad
{
    public class RecentFilesComboBox
    {
        private const int _maxRecentMenuFiles = 20;
        bool addingFile;
        private ComboBox _location;
        private RecentFiles _files;

        public RecentFilesComboBox(RecentFiles files, ComboBox location)
        {
            this._files = files;
            files.RecentFilesChanged += OnRecentFilesChanged;
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
                ComboFileItem item = this._location.SelectedItem as ComboFileItem;
                if (item != null)
                {
                    this._files.OnRecentFileSelected(item.Location);
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
                    Uri uri = null;
                    try
                    {
                        var filename = this._location.Text.Trim('\"');
                        this._location.Text = filename;
                        uri = new Uri(filename, UriKind.RelativeOrAbsolute);
                        this._files.OnRecentFileSelected(uri);
                    }
                    catch
                    {
                        var msg = string.Format(SR.InvalidFileName, this._location.Text);
                        MessageBox.Show(msg, SR.LoadErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (this._location.SelectedItem is ComboFileItem item && item != null)
                {
                    this._files.OnRecentFileSelected(item.Location);
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this._location.DroppedDown = false;
            }
        }

        private void OnRecentFilesChanged(object sender, EventArgs e)
        {
            // Synchronize menu items.
            this.addingFile = true;

            try
            {
                // Add most recent files first.
                Uri[] recentFiles = this._files.GetRelativeUris();

                // Synchronize combo-box items
                this._location.Items.Clear();
                for (int i = recentFiles.Length - 1; i >= 0; i--)
                {
                    Uri uri = recentFiles[i];
                    this._location.Items.Add(new ComboFileItem(uri));
                }

                if (this._location.Items.Count > 0 && this.SelectFirstItemByDefault)
                {
                    this._location.SelectedIndex = 0;
                }

                // sync autocompletion list
                this._location.AutoCompleteCustomSource.Clear();
                for (int i = 0; i < this._location.Items.Count; i++)
                {
                    ComboFileItem item = (ComboFileItem)this._location.Items[i];
                    this._location.AutoCompleteCustomSource.Add(item.ToString());
                }
            }
            finally
            {
                this.addingFile = false;
            }
        }

        private class ComboFileItem
        {
            public Uri Location;

            public ComboFileItem(Uri uri)
            {
                Location = uri;
            }

            public override string ToString()
            {
                if (!Location.IsAbsoluteUri)
                {
                    return Location.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped).Replace('/', '\\');
                }
                return Location.IsFile ? Location.LocalPath : Location.AbsoluteUri;
            }
        }

    }

    public class RecentFilesMenu
    {
        private const int _maxRecentMenuFiles = 20;
        private ToolStripMenuItem _parent;
        private RecentFiles _files;

        public RecentFilesMenu(RecentFiles files, ToolStripMenuItem parent)
        {
            this._parent = parent;
            this._files = files;
            files.RecentFilesChanged += OnRecentFilesChanged;
        }

        private void OnRecentFilesChanged(object sender, EventArgs e)
        {
            // Synchronize menu items.
            this._parent.Enabled = true;

            ToolStripItemCollection ic = this._parent.DropDownItems;
            ic.Clear();

            // Add most recent files first.
            Uri[] recentFiles = this._files.GetRelativeUris();
            for (int i = recentFiles.Length - 1, j = 0; i >= 0 && j < _maxRecentMenuFiles; i--, j++)
            {
                Uri uri = recentFiles[i];
                ToolStripItem item = new ToolStripMenuItem();
                item.Click += new EventHandler(OnRecentFile);
                ic.Add(item);
                item.Text = uri.IsFile ? uri.LocalPath : uri.AbsoluteUri;
                item.Tag = uri;
            }
        }

        void OnRecentFile(object sender, EventArgs e)
        {
            ToolStripItem ts = (ToolStripItem)sender;
            Uri location = ts.Tag as Uri;
            if (location != null)
            {
                this._files.OnRecentFileSelected(location);
            }
        }

    }
}
