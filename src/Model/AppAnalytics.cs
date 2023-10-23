using GoogleAnalytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlNotepad
{
    public partial class AppAnalytics
    {
        private const string HostName = "microsoft.github.io";
        private const string TrackingId = "G-130J0SE94H";
        private string _clientId;
        private bool _formOptions; // did they use the options dialog during this session?
        private bool _formSchemas;
        private bool _formSearch;
        private bool _csvImport;
        private bool _xsltView;
        private bool _enabled;

        public AppAnalytics(string clientId, bool enabled)
        {
            this._clientId = clientId;
            this._enabled = enabled;
        }

        private async void SendMeasurement(string path, string title)
        {
            try
            {
                var a = new Analytics()
                {
                    ApiSecret = ApiKey,
                    MeasurementId = TrackingId,
                    ClientId = _clientId
                };
                a.Events.Add(new PageMeasurement()
                {
                    Path = "https://" + HostName + path,
                    Title = title
                });
                await HttpProtocol.PostMeasurements(a);
            }
            catch
            {
                // Ignore.
            }
        }

        public void RecordAppLaunched()
        {
            if (this._enabled)
            {
                SendMeasurement("/App/Launch", "Launch");
            }
        }

        public void RecordFormOptions()
        {
            if (this._enabled && !_formOptions)
            {
                _formOptions = true;
                SendMeasurement("/App/FormOptions", "Options");
            }
        }

        public void RecordFormSchemas()
        {
            if (this._enabled && !_formSchemas)
            {
                _formSchemas = true;
                SendMeasurement("/App/FormSchemas", "Schemas");
            }
        }

        public void RecordFormSearch()
        {
            if (this._enabled && !_formSearch)
            {
                _formSearch = true;
                SendMeasurement("/App/FormSearch", "Search");
            }
        }

        public void RecordCsvImport()
        {
            if (this._enabled && !_csvImport)
            {
                _csvImport = true;
                SendMeasurement("/App/CsvImport", "CsvImport");
            }
        }

        public void RecordXsltView()
        {
            if (this._enabled && !_xsltView)
            {
                _xsltView = true;
                SendMeasurement("/App/XsltView", "XsltView");
            }
        }
    }
}
