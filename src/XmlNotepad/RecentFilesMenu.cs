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

    public class RecentFiles
    {
        private const int _maxRecentFiles = 1000;
        private List<Uri> _recentFiles = new List<Uri>();

        public event RecentFileHandler RecentFileSelected;
        public event EventHandler RecentFilesChanged;

        private Uri _baseUri;

        /// <summary>
        /// If provided, the items rendered will be relativized to this base uri.
        /// </summary>
        public Uri BaseUri
        {
            get => _baseUri;
            set
            {
                _baseUri = value;
                SyncRecentFilesUI();
            }
        }

        public Uri[] GetRelativeUris()
        {
            List<Uri> rel = new List<Uri>();
            foreach (var item in _recentFiles)
            {
                rel.Add(MakeRelative(item));
            }
            return rel.ToArray();
        }

        public Uri[] ToArray()
        {
            return _recentFiles.ToArray();
        }

        public void Clear()
        {
            _recentFiles.Clear();
        }

        public bool Contains(Uri uri)
        {
            return _recentFiles.Contains(uri);
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

        private void AddRecentFileName(Uri fileName)
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

        protected virtual void SyncRecentFilesUI()
        {
            if (RecentFilesChanged != null)
            {
                RecentFilesChanged(this, EventArgs.Empty);
            }
        }

        public void OnRecentFileSelected(Uri selected)
        {
            if (this.RecentFileSelected != null && selected != null)
            {
                this.RecentFileSelected(this, new RecentFileEventArgs(selected));
            }
        }

        string RemoveQuotes(string s)
        {
            return s.Trim().Trim('"');
        }

        private Uri MakeRelative(Uri uri)
        {
            if (!uri.IsAbsoluteUri || this.BaseUri == null)
            {
                return uri;
            }
            var relative = this.BaseUri.MakeRelativeUri(uri);
            if (relative.IsAbsoluteUri)
            {
                return relative;
            }
            string original = uri.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped).Replace('/', '\\');
            string result = relative.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped).Replace('/', '\\');
            if (result.Length > original.Length)
            {
                // keep the full path then, it's shorter!
                return uri;
            }
            return relative;
        }
    }

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
                        uri = new Uri(filename);
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
