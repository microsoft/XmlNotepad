using System;
using System.Xml;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;

namespace XmlNotepad
{
    public class Updater : IDisposable
    {
        private Settings _settings;
        private DateTime _lastCheck = DateTime.MinValue;
        private TimeSpan _updateFrequency = TimeSpan.MaxValue;
        private Uri _updateUri;
        private DelayedActions _delayedActions;
        private const string RetryAction = "Retry";
        private const int DefaultDelay = 5000;
        private string _download;
        private string _installer;
        private string _history;
        private string _title;
        private string _version;
        private bool _enabled = true;
        private WebRequest _req;
        private bool _disposed;
        private int _retryCount;
        private const int MaxRetries = 10;
        private TimeSpan _minimumUpdateFrequency = TimeSpan.FromSeconds(5);

        public event EventHandler<bool> UpdateRequired;

        public Updater(Settings s, DelayedActions handler)
        {
            this._settings = s;
            this._delayedActions = handler;
            s["LastUpdateCheck"] = _lastCheck;
            s["UpdateFrequency"] = _updateFrequency;
            s["UpdateLocation"] = "";
            s["UpdateEnabled"] = _enabled;
            s.Changed += new SettingsEventHandler(OnSettingChanged);
            StartTimer();
        }

        void StartTimer(int milliseconds = DefaultDelay)
        {
            if (this._enabled && !this._disposed)
            {
                this._delayedActions.StartDelayedAction(RetryAction, OnTimerTick, TimeSpan.FromMilliseconds(milliseconds));
            }
        }

        void StopTimer()
        {
            this._delayedActions.CancelDelayedAction(RetryAction);
        }

        public string DownloadPage { get { return this._download; } }
        public string Title { get { return this._title; } set { this._title = value; } }
        public string Version { get { return this._version; } set { this._version = value; } }
        public Uri UpdateLocation { get { return this._updateUri; } }
        public string InstallerLocation { get { return this._installer; } }
        public string InstallerHistory { get { return this._history; } }

        void OnSettingChanged(object sender, string name)
        {
            switch (name)
            {
                case "LastUpdateCheck":
                    this._lastCheck = (DateTime)_settings["LastUpdateCheck"];
                    break;
                case "UpdateFrequency":
                    SetUpdateFrequency((TimeSpan)_settings["UpdateFrequency"]);
                    break;
                case "UpdateLocation":
                    SetUpdateLocation((string)_settings["UpdateLocation"]);
                    break;
                case "UpdateEnabled":
                    SetEnabled((bool)_settings["UpdateEnabled"]);
                    break;
            }
        }

        void SetEnabled(bool e)
        {
            if (this._enabled != e)
            {
                this._enabled = e;
                if (e && !this._disposed)
                {
                    StartTimer();
                }
                else
                {
                    StopTimer();
                }
            }
        }

        void SetUpdateFrequency(TimeSpan ts)
        {
            if (ts == TimeSpan.MaxValue || ts < _minimumUpdateFrequency)
            {
                ts = _minimumUpdateFrequency;
            }
            this._updateFrequency = ts;
            TimeSpan f = (TimeSpan)_settings["UpdateFrequency"];
            if (f != ts)
            {
                _settings["UpdateFrequency"] = ts;
            }
            StartTimer();
        }

        void SetUpdateLocation(string location)
        {
            if (string.IsNullOrEmpty(location)) return;
            Uri uri = new Uri(location);
            if (uri != this._updateUri)
            {
                this._retryCount = 0;
                this._updateUri = uri;
                if ((string)_settings["UpdateLocation"] != location)
                {
                    _settings["UpdateLocation"] = location;
                }
                // Location has just changed, so we need to download the new update information.
                StartTimer();
            }
        }

        public void OnUserChange(string oldUri)
        {
            if ((string)_settings["UpdateLocation"] != oldUri)
            {
                // then this user changed the location, so we need to ping the new
                // location right away.
                this._retryCount = 0;
                this._lastCheck = DateTime.MinValue;
                StartTimer();
            }
        }

        async void OnTimerTick()
        {
            if (this._updateUri == null)
            {
                Bootstrap();
            }
            else if (this._lastCheck == DateTime.MinValue ||
              this._updateFrequency == TimeSpan.MaxValue ||
              this._lastCheck + this._updateFrequency < DateTime.Now)
            {
                bool update = await CheckForUpdate();
                FireUpdate(update);
            }
        }

        bool busy;

        async Task<bool> CheckForUpdate(bool retry = true)
        {
            if (busy)
            {
                return false;
            }
            bool update = false;
            busy = true;
            if (this._updateUri != null)
            {
                try
                {
                    // assume success in this request so we don't create DOS attacks on the server!
                    this._lastCheck = DateTime.Now;
                    _settings["LastUpdateCheck"] = this._lastCheck;

                    // now check
                    WebRequest wr = WebRequest.Create(this._updateUri);
                    this._req = wr;
                    wr.Credentials = CredentialCache.DefaultCredentials;
                    wr.Proxy = WebRequest.DefaultWebProxy;
                    WebResponse r = await wr.GetResponseAsync();
                    XmlDocument doc = null;
                    using (Stream s = r.GetResponseStream())
                    {
                        doc = new XmlDocument();
                        doc.Load(s);
                    }
                    if (!this._disposed)
                    {
                        update = ProcessUpdate(doc);
                    }

                }
                catch (Exception)
                {
                    // try again in a bit...
                    this._retryCount++;
                    if (retry && this._retryCount < MaxRetries)
                    {
                        StartTimer();
                    }
                }
                finally
                {
                    this._req = null;
                }
            }
            busy = false;
            return update;
        }

        void Bootstrap()
        {
            // See if we can find a local copy of updates.xml so that we can bootstrap the
            // location from there.
            Uri baseUri = new Uri(this.GetType().Assembly.Location);
            Uri resolved = new Uri(baseUri, "updates.xml");
            string file = resolved.LocalPath;
            if (File.Exists(file))
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(file);
                    ProcessUpdate(doc);
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.Message);
                }
            }
        }

        bool ProcessUpdate(XmlDocument doc)
        {
            Version v = GetType().Assembly.GetName().Version;
            string ver = v.ToString();

            XmlElement loc = doc.SelectSingleNode("updates/application/location") as XmlElement;
            if (loc != null)
            {
                try
                {
                    Uri uri = new Uri(loc.InnerText);
                    if (uri != this._updateUri)
                    {
                        string location = uri.IsFile ? uri.LocalPath : uri.AbsoluteUri;
                        SetUpdateLocation(location);
                        return false; // page has been moved - start over!
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.Message);
                }
            }

            XmlElement de = doc.SelectSingleNode("updates/application/download") as XmlElement;
            if (de != null)
            {
                this._download = de.InnerText;
            }

            XmlElement ie = doc.SelectSingleNode("updates/application/installer") as XmlElement;
            if (ie != null)
            {
                this._installer = ie.InnerText;
            }

            XmlElement ih = doc.SelectSingleNode("updates/application/history") as XmlElement;
            if (ih != null)
            {
                this._history = ih.InnerText;
            }

            XmlElement f = doc.SelectSingleNode("updates/application/frequency") as XmlElement;
            if (f != null)
            {
                try
                {
                    SetUpdateFrequency(TimeSpan.Parse(f.InnerText));
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }
            }
            bool newVersion = false;
            foreach (XmlElement e in doc.SelectNodes("updates/version"))
            {
                string n = e.GetAttribute("number");
                if (!string.IsNullOrEmpty(n))
                {
                    Version v2 = new Version(n);
                    if (v2 > v)
                    {
                        // new version is available!
                        this._version = n;
                        newVersion = true;
                        break;
                    }
                }
            }

            return newVersion;
        }

        public void FireUpdate(bool newVersion)
        {
            // Make sure we switch back to the UI thread.
            var handler = this.UpdateRequired;
            if (handler != null)
            {
                this._delayedActions.StartDelayedAction("UpdateRequired", () =>
                {
                    handler(this, newVersion);
                }, TimeSpan.FromMilliseconds(1));
            }
        }

        public void Dispose()
        {
            this._disposed = true;
            StopTimer();
            WebRequest r = this._req;
            if (r != null)
            {
                try
                {
                    r.Abort();
                }
                catch
                {
                }
            }
        }

        public async Task<bool> CheckNow()
        {
            StopTimer();
            if (this._updateUri != null)
            {
                return await CheckForUpdate(false);
            }
            return false;
        }
    }
}
