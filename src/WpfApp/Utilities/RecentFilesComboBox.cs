using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace XmlNotepad.Utilities
{
    class RecentFilesComboBox
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
            this._location.SelectionChanged += OnSelectedIndexChanged;
            this._location.KeyDown += OnLocationKeyDown;
        }

        public bool SelectFirstItemByDefault { get; set; }

        private void OnSelectedIndexChanged(object sender, SelectionChangedEventArgs e)
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

        private void OnLocationKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // user has entered something new?
                e.Handled = true;
                //e.SuppressKeyPress = true;
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
                    catch (Exception ex)
                    {
                        var msg = ex.Message;
                        MessageBox.Show(msg, "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else if (this._location.SelectedItem is ComboFileItem item && item != null)
                {
                    this._files.OnRecentFileSelected(item.Location);
                }
            }
            else if (e.Key == Key.Escape)
            {
                this._location.IsDropDownOpen = false;
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
}
