using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Xml.Xsl;

namespace XmlNotepad.XsltScriptHost
{
    internal static class Program
    {
        private static int Main(string[] arguments)
        {
            if (arguments.Length != 5)
            {
                Console.Error.WriteLine("Expected a stylesheet, XML snapshot, original document URI, output path, and metadata path.");
                return 2;
            }

            try
            {
                var resolver = new LocalFileResolver();
                var stylesheet = new Uri(arguments[0], UriKind.Absolute);
                var documentUri = new Uri(arguments[2], UriKind.Absolute);
                var readerSettings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = resolver
                };
                var transform = new XslCompiledTransform();
                using (var reader = XmlReader.Create(stylesheet.AbsoluteUri, readerSettings))
                {
                    transform.Load(reader, new XsltSettings(enableDocumentFunction: true, enableScript: true), resolver);
                }
                var inputSettings = readerSettings.Clone();
                inputSettings.DtdProcessing = DtdProcessing.Ignore;
                using (var source = File.OpenRead(arguments[1]))
                using (var input = XmlReader.Create(source, inputSettings, documentUri.AbsoluteUri))
                using (var output = XmlWriter.Create(arguments[3], transform.OutputSettings))
                {
                    transform.Transform(input, null, output, resolver);
                }
                File.WriteAllLines(arguments[4], new[]
                {
                    transform.OutputSettings.OutputMethod.ToString(),
                    transform.OutputSettings.Encoding.WebName
                });
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        }

        private sealed class LocalFileResolver : XmlResolver
        {
            public override ICredentials Credentials { set { } }

            public override Uri ResolveUri(Uri baseUri, string relativeUri)
            {
                var location = base.ResolveUri(baseUri, relativeUri);
                GetLocalPath(location);
                return location;
            }

            public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn) =>
                File.OpenRead(GetLocalPath(absoluteUri));

            private static string GetLocalPath(Uri location)
            {
                if (!location.IsFile || location.IsUnc || location.LocalPath.StartsWith(@"\\", StringComparison.Ordinal))
                {
                    throw new XmlException("Scripted stylesheets must use local file references.");
                }
                var path = location.LocalPath;
                var drive = new DriveInfo(Path.GetPathRoot(path));
                if (drive.DriveType == DriveType.Network || drive.DriveType == DriveType.Unknown ||
                    drive.DriveType == DriveType.NoRootDirectory)
                {
                    throw new XmlException("Scripted stylesheets must use local file references.");
                }
                return path;
            }
        }
    }
}
