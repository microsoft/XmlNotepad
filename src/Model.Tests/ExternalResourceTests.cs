using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Xsl;
using XmlNotepad;

// Settings.Instance and the delayed-action dispatcher are process-global.
[assembly: DoNotParallelize]

namespace XmlNotepad.Model.Tests
{
    [TestClass]
    public sealed class ExternalResourceTests : IServiceProvider
    {
        private const string NetworkSchemaLocation = @"\\mock-schema.invalid\schemas\schema.xsd";
        private const string NetworkFileUri = "file://mock-schema.invalid/schemas/resource.xml";
        private const string LocalSchema = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">" +
            "<xs:element name=\"root\" type=\"xs:string\" /></xs:schema>";
        private MockSchemaSource _source = null!;
        private TestProxyResolver _resolver = null!;
        private Settings _settings = null!;
        private SchemaCache _cache = null!;
        private XmlCache _model = null!;
        private string _documentFile = null!;

        [TestInitialize]
        public void Initialize()
        {
            _source = new MockSchemaSource();
            _resolver = new TestProxyResolver(this, _source);
            _settings = new Settings
            {
                StartupPath = AppContext.BaseDirectory,
                Resolver = _resolver
            };
            _settings.SetDefaults();
            var actions = new DelayedActions(action => action());
            _settings.DelayedActions = actions;
            _cache = new SchemaCache(this, _source);
            _model = new XmlCache(this, _cache, actions);
            _documentFile = Path.GetTempFileName();
            File.Delete(_documentFile); // The in-memory input must not enable file watching.
        }

        [TestCleanup]
        public void Cleanup()
        {
            _model?.Dispose();
            if (_documentFile != null)
            {
                File.Delete(_documentFile);
            }
            _settings?.Dispose();
        }

        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(Settings) ? _settings : null;
        }

        [TestMethod]
        [DataRow("noNamespaceSchemaLocation", NetworkSchemaLocation)]
        [DataRow("schemaLocation", NetworkSchemaLocation)]
        [DataRow("noNamespaceSchemaLocation", NetworkFileUri)]
        [DataRow("schemaLocation", NetworkFileUri)]
        [DataRow("noNamespaceSchemaLocation", "file:///%5C%5Cmock-schema.invalid/schemas/schema.xsd")]
        [DataRow("schemaLocation", "file:///%5C%5Cmock-schema.invalid/schemas/schema.xsd")]
        public void OpeningDocumentDoesNotRequestNetworkSchema(string schemaAttribute, string schemaLocation)
        {
            LoadXml(CreateXml(schemaAttribute, schemaLocation));

            Assert.IsEmpty(_source.Requests,
                "Opening XML must not implicitly request a schema from a network share. " +
                "Mock source requests: " + string.Join(", ", _source.Requests));
        }

        [TestMethod]
        [DataRow("noNamespaceSchemaLocation", NetworkSchemaLocation)]
        [DataRow("schemaLocation", NetworkSchemaLocation)]
        [DataRow("noNamespaceSchemaLocation", NetworkFileUri)]
        [DataRow("schemaLocation", NetworkFileUri)]
        public void ValidatingDocumentDoesNotRequestNetworkSchema(string schemaAttribute, string schemaLocation)
        {
            var document = new XmlDocument { XmlResolver = _resolver };
            document.LoadXml(CreateXml(schemaAttribute, schemaLocation));
            _model.Load(document);

            _model.ValidateModel(new ValidationErrorHandler());

            Assert.IsEmpty(_source.Requests,
                "Validation must not implicitly request a schema from a network share. " +
                "Mock source requests: " + string.Join(", ", _source.Requests));
        }

        [TestMethod]
        public void OpeningDocumentLoadsLocalSchemaFromMockSource()
        {
            var schemaUri = new Uri(new Uri(_documentFile), "schema.xsd");
            _source.Documents.Add(schemaUri, LocalSchema);
            LoadXml(CreateXml("noNamespaceSchemaLocation", "schema.xsd"));

            Assert.HasCount(1, _source.Requests);
            Assert.AreEqual(schemaUri, _source.Requests[0]);
            Assert.AreEqual(1, _model.Document.Schemas.Count);
        }

        [TestMethod]
        [DataRow("http://mock-schema.invalid/schema.xsd")]
        [DataRow("https://mock-schema.invalid/schema.xsd")]
        public void OpeningDocumentLoadsHttpSchemaFromMockSource(string location)
        {
            var schemaUri = new Uri(location);
            _source.Documents.Add(schemaUri, LocalSchema);

            LoadXml(CreateXml("noNamespaceSchemaLocation", location));

            Assert.HasCount(1, _source.Requests);
            Assert.AreEqual(schemaUri, _source.Requests[0]);
            Assert.AreEqual(1, _model.Document.Schemas.Count);
        }

        [TestMethod]
        [DataRow("include")]
        [DataRow("import")]
        public void OpeningDocumentDoesNotRequestNetworkSchemaDependency(string directive)
        {
            var schemaUri = new Uri(new Uri(_documentFile), "schema.xsd");
            string targetNamespace = directive == "import" ? " namespace=\"urn:external\"" : "";
            _source.Documents.Add(schemaUri,
                "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">" +
                "<xs:" + directive + targetNamespace + " schemaLocation=\"" + NetworkFileUri + "\" />" +
                "<xs:element name=\"root\" type=\"xs:string\" /></xs:schema>");

            LoadXml(CreateXml("noNamespaceSchemaLocation", "schema.xsd"));

            Assert.HasCount(1, _source.Requests,
                "Only the local parent schema may reach the source. Requests: " +
                string.Join(", ", _source.Requests));
            Assert.AreEqual(schemaUri, _source.Requests[0]);
        }

        [TestMethod]
        public void OpeningDocumentWithoutSchemaLocationDoesNotRequestSchema()
        {
            LoadXml("<root />");

            Assert.IsEmpty(_source.Requests);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ParsingDtdDoesNotRequestNetworkResource(bool externalSubset)
        {
            _settings["IgnoreDTD"] = false;
            string declaration = externalSubset
                ? "<!DOCTYPE root SYSTEM \"" + NetworkFileUri + "\">"
                : "<!DOCTYPE root [<!ENTITY external SYSTEM \"" + NetworkFileUri + "\">]>";
            string xml = declaration + (externalSubset ? "<root />" : "<root>&external;</root>");
            using (var text = new StringReader(xml))
            using (var reader = XmlHelpers.ReadXml(text, _resolver))
            {
                var document = new XmlDocument();
                Assert.Throws<XmlException>(() => document.Load(reader));
            }

            Assert.IsEmpty(_source.Requests);
        }

        [TestMethod]
        public void ParsingDtdLoadsLocalEntityFromMockSource()
        {
            _settings["IgnoreDTD"] = false;
            var entityUri = new Uri(new Uri(_documentFile), "entity.txt");
            _source.Documents.Add(entityUri, "allowed");
            string xml = "<!DOCTYPE root [<!ENTITY external SYSTEM \"" +
                entityUri.AbsoluteUri + "\">]><root>&external;</root>";
            using (var text = new StringReader(xml))
            using (var reader = XmlHelpers.ReadXml(text, _resolver))
            {
                var document = new XmlDocument();
                document.Load(reader);
                Assert.AreEqual("allowed", document.InnerText);
            }

            Assert.HasCount(1, _source.Requests);
        }

        [TestMethod]
        public void ExpandingIncludesDoesNotRequestNetworkResource()
        {
            LoadXml("<root xmlns:xi=\"http://www.w3.org/2001/XInclude\">" +
                "<xi:include href=\"" + NetworkFileUri + "\" /></root>");

            Assert.Throws<XmlException>(() => _model.ExpandIncludes());

            Assert.IsEmpty(_source.Requests);
        }

        [TestMethod]
        public void ExpandingIncludesLoadsLocalResourceFromMockSource()
        {
            var includeUri = new Uri(new Uri(_documentFile), "include.xml");
            _source.Documents.Add(includeUri, "<included>allowed</included>");
            LoadXml("<root xmlns:xi=\"http://www.w3.org/2001/XInclude\">" +
                "<xi:include href=\"" + includeUri.AbsoluteUri + "\" /></root>");

            _model.ExpandIncludes();

            Assert.AreEqual("allowed", _model.Document.DocumentElement?.InnerText);
            Assert.HasCount(1, _source.Requests);
        }

        [TestMethod]
        [DataRow("include")]
        [DataRow("import")]
        public void LoadingStylesheetDoesNotRequestNetworkResource(string directive)
        {
            string stylesheet = "<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\">" +
                "<xsl:" + directive + " href=\"" + NetworkFileUri + "\" /></xsl:stylesheet>";
            using (var text = new StringReader(stylesheet))
            using (var reader = XmlReader.Create(text))
            {
                var transform = new XslCompiledTransform();
                Assert.Throws<XsltException>(() => transform.Load(reader, XsltSettings.Default, _resolver));
            }

            Assert.IsEmpty(_source.Requests);
        }

        [TestMethod]
        public void TransformingDoesNotRequestNetworkDocument()
        {
            var transform = CreateDocumentTransform(NetworkFileUri);
            using (var text = new StringReader("<root />"))
            using (var reader = XmlReader.Create(text))
            using (var output = new StringWriter())
            using (var writer = XmlWriter.Create(output, transform.OutputSettings))
            {
                Assert.Throws<XsltException>(() => transform.Transform(reader, null, writer, _resolver));
            }

            Assert.IsEmpty(_source.Requests);
        }

        [TestMethod]
        public void TransformingLoadsLocalDocumentFromMockSource()
        {
            var documentUri = new Uri(new Uri(_documentFile), "external.xml");
            _source.Documents.Add(documentUri, "<value>allowed</value>");
            var transform = CreateDocumentTransform(documentUri.AbsoluteUri);
            using (var text = new StringReader("<root />"))
            using (var reader = XmlReader.Create(text))
            using (var output = new StringWriter())
            {
                using (var writer = XmlWriter.Create(output, transform.OutputSettings))
                {
                    transform.Transform(reader, null, writer, _resolver);
                }
                StringAssert.Contains(output.ToString(), "allowed");
            }

            Assert.HasCount(1, _source.Requests);
        }

        [TestMethod]
        public async Task ResolvingNetworkResourceAsyncDoesNotReachSource()
        {
            await Assert.ThrowsAsync<XmlException>(() =>
                _resolver.GetEntityAsync(new Uri(NetworkFileUri), null, typeof(Stream)));

            Assert.IsEmpty(_source.Requests);
        }

        [TestMethod]
        [DataRow("file:////mock-schema.invalid/schemas/resource.xml")]
        [DataRow("file:///%5C%5C%3F%5CUNC%5Cmock-schema.invalid%5Cschemas%5Cresource.xml")]
        public void ResolvingNetworkFileAliasDoesNotReachSource(string location)
        {
            Assert.Throws<XmlException>(() =>
                _resolver.GetEntity(new Uri(location), null, typeof(Stream)));

            Assert.IsEmpty(_source.Requests);
        }

        [TestMethod]
        [DataRow(DriveType.Network)]
        [DataRow(DriveType.Unknown)]
        [DataRow(DriveType.NoRootDirectory)]
        public void ResolvingRestrictedDriveDoesNotReachSource(DriveType driveType)
        {
            var uri = new Uri(_documentFile);
            _source.Documents.Add(uri, "<root />");
            var resolver = new ClassifiedDriveResolver(_source, driveType);

            Assert.Throws<XmlException>(() => resolver.GetEntity(uri, null, typeof(Stream)));

            Assert.IsEmpty(_source.Requests);
        }

        [TestMethod]
        public void RestrictedDriveIsClassifiedBeforeNormalizingShortNameTail()
        {
            var uri = new Uri(new Uri(_documentFile), "SHORT~1|invalid/schema.xsd");
            var resolver = new ClassifiedDriveResolver(_source, DriveType.Network);

            Assert.Throws<XmlException>(() => resolver.GetEntity(uri, null, typeof(Stream)));

            Assert.HasCount(1, resolver.Roots);
            Assert.AreEqual(Path.GetPathRoot(_documentFile), resolver.Roots[0]);
            Assert.IsEmpty(_source.Requests);
        }

        [TestMethod]
        public void RootSuggestionsResolveDeferredImportsThroughSchemaResolver()
        {
            var childUri = new Uri("mock://schemas/child.xsd");
            const string parentSchema =
                "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"urn:parent\" " +
                "xmlns:child=\"urn:child\"><xs:import namespace=\"urn:child\" " +
                "schemaLocation=\"mock://schemas/child.xsd\" />" +
                "<xs:element name=\"root\" type=\"child:valueType\" /></xs:schema>";
            XmlSchema parent;
            using (var text = new StringReader(parentSchema))
            using (var reader = XmlReader.Create(text, new XmlReaderSettings(),
                "https://mock-schema.invalid/parent.xsd"))
            {
                parent = XmlSchema.Read(reader, null)!;
            }
            parent.SourceUri = "https://mock-schema.invalid/parent.xsd";
            _cache.Add(parent);
            _source.Documents.Add(childUri,
                "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"urn:child\">" +
                "<xs:simpleType name=\"valueType\"><xs:restriction base=\"xs:string\" />" +
                "</xs:simpleType></xs:schema>");
            _source.Requests.Clear();

            var roots = new List<XmlSchemaElement>(_cache.GetPossibleTopLevelElements());

            Assert.HasCount(1, roots);
            Assert.AreEqual(new XmlQualifiedName("root", "urn:parent"), roots[0].QualifiedName);
            Assert.HasCount(1, _source.Requests);
            Assert.AreEqual(childUri, _source.Requests[0]);
        }

        [TestMethod]
        public void LoadingResourceDocumentBlocksNetworkSource()
        {
            var uri = new Uri(NetworkFileUri);
            _source.Documents.Add(uri, "<stylesheet />");

            Assert.Throws<XmlException>(() => XmlHelpers.LoadXmlResource(uri, _resolver));

            Assert.IsEmpty(_source.Requests);
        }

        [TestMethod]
        [DataRow("stylesheet.xsl")]
        [DataRow("https://mock-schema.invalid/stylesheet.xsl")]
        public void LoadingResourceDocumentUsesAllowedMockSource(string location)
        {
            var uri = new Uri(new Uri(_documentFile), location);
            _source.Documents.Add(uri, "<stylesheet method=\"html\" />");

            var document = XmlHelpers.LoadXmlResource(uri, _resolver);

            Assert.AreEqual("html", document.DocumentElement?.GetAttribute("method"));
            Assert.HasCount(1, _source.Requests);
        }

        [TestMethod]
        [DataRow(NetworkSchemaLocation)]
        [DataRow(NetworkFileUri)]
        public void ReadingExplicitlySelectedNetworkDocumentDoesNotResolveItsRoot(string filename)
        {
            Uri? opened = null;
            var input = new MemoryStream(Encoding.UTF8.GetBytes("<root>selected</root>"));
            using (var reader = XmlHelpers.ReadDocument(filename, _model.GetReaderSettings(), location =>
            {
                opened = location;
                return input;
            }))
            {
                var document = new XmlDocument();
                document.Load(reader);
                Assert.AreEqual("selected", document.InnerText);
            }

            Assert.AreEqual(new Uri(filename), opened);
            Assert.IsFalse(input.CanRead);
            Assert.IsEmpty(_source.Requests);
        }

        [TestMethod]
        public void ReadingSelectedNetworkDocumentStillBlocksItsExternalEntities()
        {
            _settings["IgnoreDTD"] = false;
            const string xml = "<!DOCTYPE root [<!ENTITY external SYSTEM " +
                "\"file://other-source.invalid/entity.txt\">]><root>&external;</root>";
            using (var reader = XmlHelpers.ReadDocument(NetworkFileUri, _model.GetReaderSettings(),
                location => new MemoryStream(Encoding.UTF8.GetBytes(xml))))
            {
                var document = new XmlDocument();
                Assert.Throws<XmlException>(() => document.Load(reader));
            }

            Assert.IsEmpty(_source.Requests);
        }

        [TestMethod]
        public async Task ResolvingMappedDriveAsyncDoesNotReachSource()
        {
            var uri = new Uri(_documentFile);
            _source.Documents.Add(uri, "<root />");
            var resolver = new ClassifiedDriveResolver(_source, DriveType.Network);

            await Assert.ThrowsAsync<XmlException>(() => resolver.GetEntityAsync(uri, null, typeof(Stream)));

            Assert.IsEmpty(_source.Requests);
        }

        [TestMethod]
        public async Task ResolvingLocalResourceAsyncUsesMockSource()
        {
            var uri = new Uri(_documentFile);
            _source.Documents.Add(uri, "allowed");
            var resolver = new ClassifiedDriveResolver(_source, DriveType.Fixed);
            using (var stream = (Stream)await resolver.GetEntityAsync(uri, null, typeof(Stream)))
            using (var reader = new StreamReader(stream))
            {
                Assert.AreEqual("allowed", await reader.ReadToEndAsync());
            }

            Assert.HasCount(1, _source.Requests);
        }

        [TestMethod]
        public void ReaderGuardsCustomResolverBeforeReadingExternalEntity()
        {
            _settings["IgnoreDTD"] = false;
            string xml = "<!DOCTYPE root [<!ENTITY external SYSTEM \"" +
                NetworkFileUri + "\">]><root>&external;</root>";
            using (var text = new StringReader(xml))
            using (var reader = XmlHelpers.ReadXml(text, _source))
            {
                var document = new XmlDocument();
                Assert.Throws<XmlException>(() => document.Load(reader));
            }

            Assert.IsEmpty(_source.Requests);
        }

        [TestMethod]
        public void LoadingSelectedLocalFilePreservesRelativeEntityBaseUri()
        {
            _settings["IgnoreDTD"] = false;
            string filename = Path.GetTempFileName();
            var entityUri = new Uri(new Uri(filename), "entity.txt");
            _source.Documents.Add(entityUri, "allowed");
            try
            {
                File.WriteAllText(filename,
                    "<!DOCTYPE root [<!ENTITY external SYSTEM \"entity.txt\">]><root>&external;</root>");

                _model.Load(filename);

                Assert.AreEqual("allowed", _model.Document.DocumentElement?.InnerText);
                Assert.HasCount(1, _source.Requests);
                Assert.AreEqual(entityUri, _source.Requests[0]);
            }
            finally
            {
                _model.Clear();
                File.Delete(filename);
            }
        }

        private XslCompiledTransform CreateDocumentTransform(string location)
        {
            string stylesheet = "<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\">" +
                "<xsl:template match=\"/\"><result><xsl:value-of select=\"document('" +
                location + "')/value\" /></result></xsl:template></xsl:stylesheet>";
            var transform = new XslCompiledTransform();
            using (var text = new StringReader(stylesheet))
            using (var reader = XmlReader.Create(text))
            {
                transform.Load(reader, new XsltSettings(true, false), _resolver);
            }
            return transform;
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
            public Dictionary<Uri, string> Documents { get; } = new Dictionary<Uri, string>();

            public override ICredentials? Credentials
            {
                set => throw new NotSupportedException("The mock source never uses credentials.");
            }

            public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
            {
                Requests.Add(absoluteUri);
                if (ofObjectToReturn != null && ofObjectToReturn != typeof(Stream))
                {
                    throw new NotSupportedException("The mock source only provides streams.");
                }

                if (!Documents.TryGetValue(absoluteUri, out string? document))
                {
                    throw new XmlException("The mock source recorded the request without performing I/O.");
                }
                return new MemoryStream(Encoding.UTF8.GetBytes(document));
            }

            public override Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn)
            {
                return Task.FromResult(GetEntity(absoluteUri, role, ofObjectToReturn));
            }
        }

        private sealed class TestProxyResolver : XmlProxyResolver
        {
            public TestProxyResolver(IServiceProvider site, XmlResolver source) : base(site, source)
            {
            }
        }

        private sealed class ClassifiedDriveResolver : XmlResourceResolver
        {
            private readonly DriveType _driveType;
            public List<string> Roots { get; } = new List<string>();

            public ClassifiedDriveResolver(XmlResolver source, DriveType driveType) : base(source)
            {
                _driveType = driveType;
            }

            protected override DriveType GetDriveType(string root)
            {
                Roots.Add(root);
                return _driveType;
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
