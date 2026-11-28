using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;

namespace XmlNotepad
{
    public sealed class XmlCache
    {
        private readonly IServiceProvider _services;
        private DomLoader _loader;

        public XmlCache(IServiceProvider services, SchemaCache schemaCache)
        {
            _services = services;
            SchemaCache = schemaCache;
            _loader = new DomLoader(_services, SchemaCache);
        }

        public string FileName { get; private set; }
        public XmlDocument Document { get; private set; } = new XmlDocument();
        public SchemaCache SchemaCache { get; }
        public XmlResolver SchemaResolver => SchemaCache.Resolver;
        public HashSet<string> AllNamespaces => _loader.AllNamespaces;
        public Dictionary<XmlNode, XmlSchemaInfo> TypeInfoMap { get; set; }

        public LineInfo GetLineInfo(XmlNode node) => _loader.GetLineInfo(node);

        public void ValidateModel(ErrorHandler handler)
        {
            using (var checker = new Checker(handler))
            {
                checker.Validate(this);
            }
        }

        public void Load(XmlDocument document)
        {
            Clear();
            Document = document;
        }

        public void Load(string file)
        {
            using (var reader = XmlHelpers.ReadDocument(file, XmlHelpers.CreateXmlSettings(Settings.Instance.Resolver)))
            {
                Load(reader, file);
            }
        }

        public void Load(XmlReader reader, string fileName)
        {
            Clear();
            var location = new Uri(fileName, UriKind.RelativeOrAbsolute);
            if (!location.IsAbsoluteUri)
            {
                location = new Uri(System.IO.Path.GetFullPath(fileName));
            }
            FileName = location.IsFile ? location.LocalPath : location.AbsoluteUri;
            Document = _loader.Load(reader);
        }

        public XmlReaderSettings GetReaderSettings()
        {
            var settings = XmlHelpers.CreateXmlSettings(Settings.Instance.Resolver);
            settings.CheckCharacters = false;
            return settings;
        }

        public void ExpandIncludes()
        {
            var settings = XmlHelpers.CreateXmlSettings(Settings.Instance.Resolver);
            using (var reader = XmlIncludeReader.CreateIncludeReader(Document, settings, FileName, estimateSize: false))
            {
                Document = _loader.Load(reader);
            }
        }

        public void Clear()
        {
            FileName = null;
            Document = new XmlDocument();
            TypeInfoMap = null;
            _loader = new DomLoader(_services, SchemaCache);
        }
    }
}
