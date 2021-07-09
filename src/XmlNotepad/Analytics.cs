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
        const string HostName = "microsoft.github.io";
        const string TrackingId = "UA-89203408-1";
        string clientId;
        bool formOptions; // did they use the options dialog during this session?
        bool formSchemas;
        bool formSearch;
        bool csvImport;
        bool xsltView;
        bool enabled;

        public Analytics(string clientId, bool enabled)
        {
            this.clientId = clientId;
            this.enabled = enabled;
        }

        private async void SendMeasurement(string path, string title)
        {
            await HttpProtocol.PostMeasurements(new PageMeasurement()
            {
                TrackingId = TrackingId,
                ClientId = this.clientId,
                HostName = HostName,
                Path = path,
                Title = title
            });
        }

        public void RecordAppLaunched()
        {
            if (this.enabled)
            {
                SendMeasurement("/App/Launch", "Launch");
            }
        }

        public void RecordFormOptions()
        {
            if (this.enabled && !formOptions)
            {
                formOptions = true;
                SendMeasurement("/App/FormOptions", "Options");
            }
        }

        public void RecordFormSchemas()
        {
            if (this.enabled && !formSchemas)
            {
                formSchemas = true;
                SendMeasurement("/App/FormSchemas", "Schemas");
            }
        }

        public void RecordFormSearch()
        {
            if (this.enabled && !formSearch)
            {
                formSearch = true;
                SendMeasurement("/App/FormSearch", "Search");
            }
        }

        public void RecordCsvImport()
        {
            if (this.enabled && !csvImport)
            {
                csvImport = true;
                SendMeasurement("/App/CsvImport", "CsvImport");
            }
        }

        public void RecordXsltView()
        {
            if (this.enabled && !xsltView)
            {
                xsltView = true;
                SendMeasurement("/App/XsltView", "XsltView");
            }
        }
    }
}
