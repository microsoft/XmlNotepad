using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using SR = XmlNotepad.StringResources;

namespace XmlNotepad
{
    public class RecentFileEventArgs : EventArgs
    {
        public Uri FileName;

        public RecentFileEventArgs(Uri fname)
        {
            this.FileName = fname;
        }
    }

    public delegate void RecentFileHandler(object sender, RecentFileEventArgs args);

    public class RecentFilesMenu
    {
        private List<Uri> _recentFiles = new List<Uri>();
        private const int _maxRecentMenuFiles = 20;
        private const int _maxRecentFiles = 1000;
        private ToolStripMenuItem _parent;
        private ComboBox _location;

        private class ComboFileItem
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

        public RecentFilesMenu(ToolStripMenuItem parent, ComboBox location)
        {
            this._parent = parent;
            this._location = location;
            this._location.SelectedIndexChanged += new EventHandler(OnSelectedIndexChanged);
            this._location.KeyDown += new KeyEventHandler(OnLocationKeyDown);
            this._location.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            this._location.AutoCompleteSource = AutoCompleteSource.CustomSource;
        }

        public Uri[] ToArray()
        {
            return _recentFiles.ToArray();
        }

        public void Clear()
        {
            _recentFiles.Clear();
        }

        public void SetFiles(Uri[] files)
        {
            Clear();
            foreach (Uri fileName in files)
            {
                AddRecentFileName(fileName);
            }
            SyncRecentFilesUI();
        }

        bool addingFile;

        void AddRecentFileName(Uri fileName)
        {
            try
            {
                if (this._recentFiles.Contains(fileName))
                {
                    this._recentFiles.Remove(fileName);
                }
                if (fileName.IsFile && !File.Exists(fileName.LocalPath))
                {
                    return; // ignore deleted files.
                }
                this._recentFiles.Add(fileName);

                if (this._recentFiles.Count > _maxRecentFiles)
                {
                    this._recentFiles.RemoveAt(0);
                }

            }
            catch (System.UriFormatException)
            {
                // ignore bad file names
            }
            catch (System.IO.IOException)
            {
                // ignore bad files
            }
        }

        public void RemoveRecentFile(Uri fileName)
        {
            if (this._recentFiles.Contains(fileName))
            {
                this._recentFiles.Remove(fileName);
                SyncRecentFilesUI();
            }
        }

        public void AddRecentFile(Uri fileName)
        {
            try
            {
                Uri trimmed = new Uri(RemoveQuotes(fileName.OriginalString), UriKind.RelativeOrAbsolute);
                AddRecentFileName(fileName);
                SyncRecentFilesUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Ignoring bad recent file: {0}: {1}", fileName, ex.Message));
            }
        }
        void SyncRecentFilesUI()
        {
            // Synchronize menu items.
            this._parent.Enabled = true;
            this.addingFile = true;

            try
            {
                ToolStripItemCollection ic = this._parent.DropDownItems;
                ic.Clear();

                // Add most recent files first.
                for (int i = this._recentFiles.Count - 1, j = 0; i >= 0 && j < _maxRecentMenuFiles; i--, j++)
                {
                    Uri uri = this._recentFiles[i];
                    ToolStripItem item = new ToolStripMenuItem();
                    item.Click += new EventHandler(OnRecentFile);
                    ic.Add(item);
                    item.Text = uri.IsFile ? uri.LocalPath : uri.AbsoluteUri;
                    item.Tag = uri;
                }

                // Synchronize combo-box items
                this._location.Items.Clear();
                for (int i = this._recentFiles.Count - 1; i >= 0; i--)
                {
                    Uri uri = this._recentFiles[i];
                    this._location.Items.Add(new ComboFileItem(uri));
                }

                if (this._location.Items.Count > 0)
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

        void OnRecentFile(object sender, EventArgs e)
        {
            if (this.RecentFileSelected != null)
            {
                ToolStripItem ts = (ToolStripItem)sender;
                Uri location = ts.Tag as Uri;
                if (location != null)
                {
                    this.RecentFileSelected(sender, new RecentFileEventArgs(location));
                }
            }
        }

        void OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (!this.addingFile && this.RecentFileSelected != null)
            {
                ComboFileItem item = this._location.SelectedItem as ComboFileItem;
                if (item != null)
                {
                    this.RecentFileSelected(sender, new RecentFileEventArgs(item.Location));
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
                        uri = new Uri(filename);
                        this.RecentFileSelected(sender, new RecentFileEventArgs(uri));
                    }
                    catch
                    {
                        var msg = string.Format(SR.InvalidFileName, this._location.Text);
                        MessageBox.Show(msg, SR.LoadErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (this._location.SelectedItem is ComboFileItem item && item != null)
                {
                    this.RecentFileSelected(sender, new RecentFileEventArgs(item.Location));
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this._location.DroppedDown = false;
            }
        }

        string RemoveQuotes(string s)
        {
            return s.Trim().Trim('"');
        }

        public bool Contains(Uri uri)
        {
            return _recentFiles.Contains(uri);
        }
    }
}
