using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace XmlNotepad {
    public class RecentFileEventArgs : EventArgs {
        public Uri FileName;
        public RecentFileEventArgs(Uri fname) {
            this.FileName = fname;
        }
    }

    public delegate void RecentFileHandler(object sender, RecentFileEventArgs args);

    public class RecentFilesMenu {
        List<Uri> recentFiles = new List<Uri>();
        const int maxRecentMenuFiles = 20;
        const int maxRecentFiles = 1000;
        ToolStripMenuItem parent;
        ComboBox location;

        class ComboFileItem
        {
            public Uri Location;

            public ComboFileItem(Uri uri)
            {
                Location = uri;
            }

            public override string ToString()
            {
                return Location.IsFile ? Location.LocalPath : Location.AbsoluteUri;
            }
        }

        public event RecentFileHandler RecentFileSelected;

        public RecentFilesMenu(ToolStripMenuItem parent, ComboBox location) {
            this.parent = parent;
            this.location = location;
            this.location.SelectedIndexChanged += new EventHandler(OnSelectedIndexChanged);
            this.location.KeyDown += new KeyEventHandler(OnLocationKeyDown);
            this.location.DropDownClosed += OnLocationDropDownClosed;
            this.location.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            this.location.AutoCompleteSource = AutoCompleteSource.CustomSource;
        }

        public Uri[] ToArray() {
            return recentFiles.ToArray();
        }

        public void Clear() {
            recentFiles.Clear();
        }

        public void SetFiles(Uri[] files) {
            Clear();
            foreach (Uri fileName in files) {
                AddRecentFileName(fileName);
            }
            SyncRecentFilesUI();
        }

        bool addingFile;

        void AddRecentFileName(Uri fileName) {
            try {
                if (this.recentFiles.Contains(fileName)) {
                    this.recentFiles.Remove(fileName);
                }            
                if (fileName.IsFile && !File.Exists(fileName.LocalPath)) {
                    return; // ignore deleted files.
                }
                this.recentFiles.Add(fileName);
                
                if (this.recentFiles.Count > maxRecentFiles) {
                    this.recentFiles.RemoveAt(0);
                }

            } catch (System.UriFormatException) {
                // ignore bad file names
            } catch (System.IO.IOException) {
                // ignore bad files
            }
        }

        public void AddRecentFile(Uri fileName) {
            try
            {
                Uri trimmed = new Uri(RemoveQuotes(fileName.OriginalString), UriKind.RelativeOrAbsolute);
                AddRecentFileName(fileName);
                SyncRecentFilesUI();
            } 
            catch (Exception)
            {
                // ignore bad filenames.
            }
        }

        void SyncRecentFilesUI() 
        {
            // Synchronize menu items.
            this.parent.Enabled = true;
            this.addingFile = true;

            try
            {
                ToolStripItemCollection ic = this.parent.DropDownItems;
                ic.Clear();

                // Add most recent files first.
                for (int i = this.recentFiles.Count - 1, j = 0; i >= 0 && j < maxRecentMenuFiles; i--, j++)
                {
                    Uri uri = this.recentFiles[i];
                    ToolStripItem item = new ToolStripMenuItem();
                    item.Click += new EventHandler(OnRecentFile);
                    ic.Add(item);
                    item.Text = uri.IsFile ? uri.LocalPath : uri.AbsoluteUri;
                    item.Tag = uri;
                }

                // Synchronize combo-box items
                this.location.Items.Clear();
                for (int i = this.recentFiles.Count - 1; i >= 0; i--)
                {
                    Uri uri = this.recentFiles[i];
                    this.location.Items.Add(new ComboFileItem(uri));
                }

                if (this.location.Items.Count > 0)
                {
                    this.location.SelectedIndex = 0;
                }

                // sync autocompletion list
                this.location.AutoCompleteCustomSource.Clear();
                for (int i = 0; i < this.location.Items.Count; i++)
                {
                    ComboFileItem item = (ComboFileItem)this.location.Items[i];
                    this.location.AutoCompleteCustomSource.Add(item.ToString());
                }
            } 
            finally
            {
                this.addingFile = false;
            }
        }

        void OnRecentFile(object sender, EventArgs e) {
            if (this.RecentFileSelected != null) {
                ToolStripItem ts = (ToolStripItem)sender;
                Uri location = ts.Tag as Uri;
                if (location != null)
                {
                    this.RecentFileSelected(sender, new RecentFileEventArgs(location));
                }
            }
        }

        void OnSelectedIndexChanged(object sender, EventArgs e) {
            if (!this.addingFile && this.RecentFileSelected != null) {
                ComboFileItem item = this.location.SelectedItem as ComboFileItem;
                if (item != null)
                {
                    this.RecentFileSelected(sender, new RecentFileEventArgs(item.Location));
                }
            }
        }

        void OnLocationKeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter)
            {
                // user has entered something new?
                e.Handled = true;
                e.SuppressKeyPress = true;
                ComboFileItem item = this.location.SelectedItem as ComboFileItem;
                if (item != null)
                {
                    this.RecentFileSelected(sender, new RecentFileEventArgs(item.Location));
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.location.DroppedDown = false;
            }
        }

        private void OnLocationDropDownClosed(object sender, EventArgs e)
        {
            // clear the type to find filter and resync dropdown items.
            SyncRecentFilesUI();
        }


        string RemoveQuotes(string s)
        {
            return s.Trim().Trim('"');
        }

    }
}
