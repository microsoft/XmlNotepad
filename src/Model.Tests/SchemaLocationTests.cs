using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using XmlNotepad;

// Settings.Instance and the delayed-action dispatcher are process-global.
[assembly: DoNotParallelize]

namespace XmlNotepad.Model.Tests
{
    [TestClass]
    public sealed class SchemaLocationTests : IServiceProvider
    {
        private const string NetworkSchemaLocation = @"\\mock-schema.invalid\schemas\schema.xsd";
        private MockSchemaSource _source = null!;
        private Settings _settings = null!;
        private XmlCache _model = null!;
        private string _documentFile = null!;

        [TestInitialize]
        public void Initialize()
        {
            _source = new MockSchemaSource();
            _settings = new Settings
            {
                StartupPath = AppContext.BaseDirectory,
                Resolver = _source
            };
            _settings.SetDefaults();
            var actions = new DelayedActions(action => action());
            _settings.DelayedActions = actions;
            var cache = new SchemaCache(this, _source);
            _model = new XmlCache(this, cache, actions);
            _documentFile = Path.Combine(AppContext.BaseDirectory, Guid.NewGuid().ToString("N") + ".xml");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _model?.Dispose();
            _settings?.Dispose();
        }

        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(Settings) ? _settings : null;
        }

        [TestMethod]
        [DataRow("noNamespaceSchemaLocation")]
        [DataRow("schemaLocation")]
        public void OpeningDocumentDoesNotRequestNetworkSchema(string schemaAttribute)
        {
            LoadXml(CreateXml(schemaAttribute, NetworkSchemaLocation));

            Assert.IsEmpty(_source.Requests,
                "Opening XML must not implicitly request a schema from a network share. " +
                "Mock source requests: " + string.Join(", ", _source.Requests));
        }

        [TestMethod]
        [DataRow("noNamespaceSchemaLocation")]
        [DataRow("schemaLocation")]
        public void ValidatingDocumentDoesNotRequestNetworkSchema(string schemaAttribute)
        {
            var document = new XmlDocument { XmlResolver = _source };
            document.LoadXml(CreateXml(schemaAttribute, NetworkSchemaLocation));
            _model.Load(document);

            _model.ValidateModel(new ValidationErrorHandler());

            Assert.IsEmpty(_source.Requests,
                "Validation must not implicitly request a schema from a network share. " +
                "Mock source requests: " + string.Join(", ", _source.Requests));
        }

        [TestMethod]
        public void OpeningDocumentLoadsLocalSchemaFromMockSource()
        {
            LoadXml(CreateXml("noNamespaceSchemaLocation", "schema.xsd"));

            Assert.HasCount(1, _source.Requests);
            Assert.AreEqual(new Uri(new Uri(_documentFile), "schema.xsd"), _source.Requests[0]);
            Assert.AreEqual(1, _model.Document.Schemas.Count);
        }

        [TestMethod]
        public void OpeningDocumentWithoutSchemaLocationDoesNotRequestSchema()
        {
            LoadXml("<root />");

            Assert.IsEmpty(_source.Requests);
        }

        private void LoadXml(string xml)
        {
            using (var text = new StringReader(xml))
            using (var reader = XmlReader.Create(text, _model.GetReaderSettings(), new Uri(_documentFile).AbsoluteUri))
            {
                _model.Load(reader, _documentFile);
            }
            Assert.AreEqual("root", _model.Document.DocumentElement?.LocalName);
        }

        private static string CreateXml(string schemaAttribute, string schemaLocation)
        {
            string namespaceAttribute = "";
            if (schemaAttribute == "schemaLocation")
            {
                namespaceAttribute = " xmlns=\"urn:mock-schema\"";
                schemaLocation = "urn:mock-schema " + schemaLocation;
            }
            return "<root" + namespaceAttribute +
                " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"" +
                " xsi:" + schemaAttribute + "=\"" + schemaLocation + "\" />";
        }

        private sealed class MockSchemaSource : XmlResolver
        {
            public List<Uri> Requests { get; } = new List<Uri>();

            public override ICredentials? Credentials
            {
                set => throw new NotSupportedException("The mock source never uses credentials.");
            }

            public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
            {
                Requests.Add(absoluteUri);
                if (absoluteUri.IsUnc || !absoluteUri.IsFile)
                {
                    throw new IOException("The mock source recorded the network request without performing I/O.");
                }
                if (ofObjectToReturn != null && ofObjectToReturn != typeof(Stream))
                {
                    throw new NotSupportedException("The mock source only provides streams.");
                }

                const string schema = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">" +
                    "<xs:element name=\"root\" type=\"xs:string\" /></xs:schema>";
                return new MemoryStream(Encoding.UTF8.GetBytes(schema));
            }
        }

        private sealed class ValidationErrorHandler : ErrorHandler
        {
            public override void HandleError(Severity sev, string reason, string filename, int line, int col, object data)
            {
                Trace.WriteLine(sev + ": " + reason);
            }
        }
    }
}
