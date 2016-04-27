using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace XmlNotepad {
    public class RecentFileEventArgs : EventArgs {
        public string FileName;
        public RecentFileEventArgs(string fname) {
            this.FileName = fname;
        }
    }

    public delegate void RecentFileHandler(object sender, RecentFileEventArgs args);

    public class RecentFilesMenu {
        List<Uri> recentFiles = new List<Uri>();
        const int maxRecentFiles = 10;
        ToolStripMenuItem parent;
        ComboBox location;

        public event RecentFileHandler RecentFileSelected;

        public RecentFilesMenu(ToolStripMenuItem parent, ComboBox location) {
            this.parent = parent;
            this.location = location;
            this.location.SelectedIndexChanged += new EventHandler(OnSelectedIndexChanged);
            this.location.KeyDown += new KeyEventHandler(OnLocationKeyDown);
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
            SyncRecentFilesMenu();
        }

        bool addingFile;

        void AddRecentFileName(Uri fileName) {
            try {
                addingFile = true;
                if (this.recentFiles.Contains(fileName)) {
                    this.recentFiles.Remove(fileName);
                }
                string fname = fileName.IsFile ? fileName.LocalPath : fileName.AbsoluteUri;
                if (this.location.Items.Contains(fname)) {
                    this.location.Items.Remove(fname);
                }
                if (fileName.IsFile && !File.Exists(fileName.LocalPath)) {
                    return; // ignore deleted files.
                }
                this.recentFiles.Add(fileName);
                this.location.Items.Insert(0, fname);
                if (this.recentFiles.Count > maxRecentFiles) {
                    this.recentFiles.RemoveAt(0);
                }
                if (this.location.Items.Count > maxRecentFiles) {
                    this.location.Items.RemoveAt(this.location.Items.Count - 1);
                }
                this.location.SelectedIndex = 0;

            } catch (System.UriFormatException) {
                // ignore bad file names
            } catch (System.IO.IOException) {
                // ignore bad files
            } finally {
                addingFile = false;
            }
        }

        public void AddRecentFile(Uri fileName) {
            AddRecentFileName(fileName);
            SyncRecentFilesMenu();            
        }

        void SyncRecentFilesMenu() {
            // Synchronize menu items.
            if (this.recentFiles.Count == 0) {
                return;
            }
            this.parent.Enabled = true;
            
            ToolStripItemCollection ic = this.parent.DropDownItems;
            // Add most recent files first.
            for (int i = this.recentFiles.Count-1, j = 0; i >= 0; i--, j++) {
                ToolStripItem item = null;
                if (ic.Count > j) {
                    item = ic[j];
                } else {
                    item = new ToolStripMenuItem();
                    item.Click += new EventHandler(OnRecentFile);
                    ic.Add(item);
                }
                Uri uri = this.recentFiles[i];
                item.Text = uri.IsFile ? uri.LocalPath : uri.AbsoluteUri;  
            }

            // Remove any extra menu items.
            for (int i = ic.Count - 1, n = this.recentFiles.Count; i > n; i--) {
                ic.RemoveAt(i);
            }
        }

        void OnRecentFile(object sender, EventArgs e) {
            if (this.RecentFileSelected != null) {
                ToolStripItem ts = (ToolStripItem)sender;
                this.RecentFileSelected(sender, new RecentFileEventArgs(RemoveQuotes(ts.Text)));
            }
        }

        void OnSelectedIndexChanged(object sender, EventArgs e) {
            if (!addingFile && this.RecentFileSelected != null) {
                this.RecentFileSelected(sender, new RecentFileEventArgs(RemoveQuotes(this.location.Text)));
            }
        }

        void OnLocationKeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                // user has entered something new?
                e.Handled = true;
                e.SuppressKeyPress = true;
                this.RecentFileSelected(sender, new RecentFileEventArgs(RemoveQuotes(this.location.Text)));
            }
        }

        string RemoveQuotes(string s)
        {
            return s.Trim().Trim('"');
        }

    }
}
