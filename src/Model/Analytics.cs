using GoogleAnalytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlNotepad
{
    public class Analytics
    {
        private const string HostName = "microsoft.github.io";
        private const string TrackingId = "UA-89203408-1";
        private string _clientId;
        private bool _formOptions; // did they use the options dialog during this session?
        private bool _formSchemas;
        private bool _formSearch;
        private bool _csvImport;
        private bool _xsltView;
        private bool _enabled;

        public Analytics(string clientId, bool enabled)
        {
            this._clientId = clientId;
            this._enabled = enabled;
        }

        private async void SendMeasurement(string path, string title)
        {
            await HttpProtocol.PostMeasurements(new PageMeasurement()
            {
                TrackingId = TrackingId,
                ClientId = this._clientId,
                HostName = HostName,
                Path = path,
                Title = title
            });
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
