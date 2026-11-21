using System;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace XmlNotepad
{
    public sealed class UpdateManifest
    {
        public Version LatestVersion { get; private set; }
        public Uri FeedLocation { get; private set; }
        public Uri Installer { get; private set; }
        public Uri History { get; private set; }
        public TimeSpan? MinimumInterval { get; private set; }

        public static UpdateManifest Read(Stream stream, Uri source)
        {
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 2 * 1024 * 1024
            }))
            {
                var root = XDocument.Load(reader).Root;
                if (root == null || root.Name != "updates")
                {
                    throw new XmlException("The update feed must contain an updates element.");
                }

                Version latest = null;
                foreach (var item in root.Elements("version"))
                {
                    if (Version.TryParse((string)item.Attribute("number"), out var parsed))
                    {
                        var version = new Version(parsed.Major, parsed.Minor, Math.Max(0, parsed.Build), Math.Max(0, parsed.Revision));
                        if (latest == null || version > latest)
                        {
                            latest = version;
                        }
                    }
                }

                if (latest == null)
                {
                    throw new XmlException("The update feed did not contain a valid release version.");
                }

                var application = root.Element("application");
                var minimumInterval = (string)application?.Element("frequency");
                return new UpdateManifest
                {
                    LatestVersion = latest,
                    FeedLocation = ReadLink(application, "location", source),
                    Installer = ReadLink(application, "installer", source) ?? ReadLink(application, "download", source),
                    History = ReadLink(application, "history", source),
                    MinimumInterval = TimeSpan.TryParse(minimumInterval, CultureInfo.InvariantCulture, out var interval) &&
                        interval > TimeSpan.Zero && interval != TimeSpan.MaxValue ? interval : (TimeSpan?)null
                };
            }
        }

        private static Uri ReadLink(XElement application, string name, Uri source)
        {
            var value = ((string)application?.Element(name))?.Trim();
            if (string.IsNullOrEmpty(value) || !Uri.TryCreate(source, value, out var uri))
            {
                return null;
            }
            return (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
                uri.UserInfo.Length == 0 ? uri : null;
        }
    }
}
