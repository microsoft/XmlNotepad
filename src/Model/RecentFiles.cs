using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

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
            if (files != null)
            {
                foreach (Uri fileName in files)
                {
                    AddRecentFileName(fileName);
                }
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

}
