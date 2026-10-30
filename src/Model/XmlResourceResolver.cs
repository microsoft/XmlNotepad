using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml;

namespace XmlNotepad
{
    public class XmlResourceResolver : XmlResolver
    {
        private readonly XmlResolver _resolver;

        public XmlResourceResolver() : this(new XmlUrlResolver())
        {
        }

        public XmlResourceResolver(XmlResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public override ICredentials Credentials
        {
            set => _resolver.Credentials = value;
        }

        public override Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            return _resolver.ResolveUri(baseUri, relativeUri);
        }

        public override bool SupportsType(Uri absoluteUri, Type type)
        {
            return _resolver.SupportsType(absoluteUri, type);
        }

        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            ValidateUri(absoluteUri, GetDriveType);
            return _resolver.GetEntity(absoluteUri, role, ofObjectToReturn);
        }

        public override Task<object> GetEntityAsync(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            ValidateUri(absoluteUri, GetDriveType);
            return _resolver.GetEntityAsync(absoluteUri, role, ofObjectToReturn);
        }

        public static void ValidateUri(Uri absoluteUri)
        {
            ValidateUri(absoluteUri, GetSystemDriveType);
        }

        protected virtual DriveType GetDriveType(string root)
        {
            return GetSystemDriveType(root);
        }

        internal static bool IsRestrictedFileUri(Uri absoluteUri)
        {
            return IsRestrictedFileUri(absoluteUri, GetSystemDriveType);
        }

        private static void ValidateUri(Uri absoluteUri, Func<string, DriveType> getDriveType)
        {
            if (absoluteUri == null)
            {
                throw new ArgumentNullException(nameof(absoluteUri));
            }
            if (IsRestrictedFileUri(absoluteUri, getDriveType))
            {
                throw new XmlException(string.Format(Strings.ExternalFileResourceBlocked, absoluteUri));
            }
        }

        private static bool IsRestrictedFileUri(Uri absoluteUri, Func<string, DriveType> getDriveType)
        {
            if (!absoluteUri.IsFile)
            {
                return false;
            }

            string path = absoluteUri.LocalPath;
            if (absoluteUri.IsUnc || path.StartsWith(@"\\", StringComparison.Ordinal) ||
                path.StartsWith("//", StringComparison.Ordinal))
            {
                return true;
            }

            string fullPath = Path.GetFullPath(path);
            if (fullPath.StartsWith(@"\\", StringComparison.Ordinal) ||
                fullPath.StartsWith("//", StringComparison.Ordinal))
            {
                return true;
            }
            // A drive-letter URI can still refer to a mapped network share.
            DriveType driveType = getDriveType(Path.GetPathRoot(fullPath));
            return driveType == DriveType.Network || driveType == DriveType.Unknown ||
                driveType == DriveType.NoRootDirectory;
        }

        private static DriveType GetSystemDriveType(string root)
        {
            return new DriveInfo(root).DriveType;
        }
    }
}
