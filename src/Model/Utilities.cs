using System;
using System.Xml;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Drawing;

namespace XmlNotepad
{
    public class FileHelpers
    {
        public static string ValidPath(string path)
        {
            Uri uri = new Uri(path);
            if (uri.Scheme == "file")
            {
                if (!File.Exists(uri.LocalPath))
                {
                    // help file not installed?
                    return "";
                }
            }
            return path;
        }
    }

    public class FileEntity
    {
        Encoding encoding = Encoding.UTF8;
        Stream stream;
        Uri uri;
        string mimeType;
        string localPath;

        public string MimeType => mimeType;
        public Encoding Encoding => encoding;
        public Stream Stream => stream;
        public Uri Uri => uri;
        public string LocalPath => localPath;

        private async Task OpenStreamAsync()
        {
            this.encoding = null;
            var uri = this.uri;
            if (uri.Scheme != "file")
            {
                using (HttpClient client = new HttpClient())
                {
                    using (var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
                    {
                        var contentType = response.Content.Headers.ContentType;
                        // todo: contentType.CharSet is also interesting for getting the right Encoding!
                        this.mimeType = contentType.MediaType;
                        if (this.mimeType == "text/plain")
                        {
                            SetMimeType(this.GetFileExtension());
                        }
                        MemoryStream ms = new MemoryStream();
                        using (var s = await response.Content.ReadAsStreamAsync())
                        {
                            s.CopyTo(ms);
                            ms.Seek(0, SeekOrigin.Begin);
                        }
                        this.stream = ms;
                        if (!string.IsNullOrEmpty(contentType.CharSet))
                        {
                            try
                            {
                                this.encoding = System.Text.Encoding.GetEncoding(contentType.CharSet);
                            }
                            catch { }
                        }
                    }
                }

                string filename = uri.Segments.Length > 1 ? uri.Segments[uri.Segments.Length - 1] : "index";
                if (uri.OriginalString.EndsWith("/"))
                {
                    filename = "index" + this.GetFileExtension();
                }
                filename = System.IO.Path.GetFileNameWithoutExtension(filename);
                filename += this.GetFileExtension();
                this.localPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), filename);
            }
            else
            {
                var filename = uri.LocalPath;
                string ext = System.IO.Path.GetExtension(filename).ToLowerInvariant();
                SetMimeType(ext);
                this.stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                this.localPath = filename;
            }
            if (this.encoding == null)
            {
                this.encoding = EncodingHelpers.SniffEncoding(this.stream);
            }
        }

        private void SetMimeType(string ext)
        {
            switch (ext)
            {
                case ".csv":
                    this.mimeType = "text/csv";
                    break;
                case ".json":
                    this.mimeType = "text/json";
                    break;
                case ".htm":
                case ".html":
                    this.mimeType = "text/html";
                    break;
                default:
                    this.mimeType = "text/xml";
                    break;
            }
        }

        public static async Task<FileEntity> Fetch(string url)
        {
            // if it is http then we have to sniff the url to get the content MimeType since
            // a url like "https://en.wikipedia.org/" has no file extension.
            Uri baseUri = new Uri("file:///" + Directory.GetCurrentDirectory().Replace('\\', '/') + "\\");
            Uri resolved = new Uri(baseUri, url);
            FileEntity e = new FileEntity() { uri = resolved };
            await e.OpenStreamAsync();
            return e;
        }

        public void Close()
        {
            using (var s = this.stream)
            {
                this.stream = null;
            }
        }

        public async Task<string> ReadText()
        {
            if (this.stream == null)
            {
                await OpenStreamAsync();
            }
            using (var reader = new StreamReader(this.stream, this.encoding))
            {
                return reader.ReadToEnd();
            }
        }

        private string GetFileExtension()
        {
            if (this.mimeType == "text/plain")
            {
                // Hmmm, server didn't specify correctly, so check the file extension.
                var extension = System.IO.Path.GetExtension(this.uri.OriginalString);
                switch (extension)
                {
                    case ".csv":
                        this.mimeType = "text/csv";
                        break;
                    case ".json":
                        this.mimeType = "text/json";
                        break;
                    case ".htm":
                    case ".html":
                        this.mimeType = "text/html";
                        break;
                }

            }
            switch (this.mimeType)
            {
                case "text/csv":
                    return ".csv";
                case "text/json":
                    return ".json";
                case "text/html":
                    return ".htm";
                default:
                    return ".xml";
            }
        }
    }

    public class EncodingHelpers
    {
        public static void InitializeWriterSettings(XmlWriterSettings settings, IServiceProvider sp)
        {
            settings.CheckCharacters = false;
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineChars = "\r\n";
            settings.NewLineHandling = NewLineHandling.Replace;

            if (sp != null)
            {
                Settings s = (Settings)sp.GetService(typeof(Settings));
                if (s != null)
                {
                    settings.Indent = (bool)s["AutoFormatOnSave"];
                    IndentChar indentChar = (IndentChar)s["IndentChar"];
                    int indentLevel = (int)s["IndentLevel"];
                    char ch = (indentChar == IndentChar.Space) ? ' ' : '\t';
                    settings.IndentChars = new string(ch, indentLevel);
                    settings.NewLineChars = Settings.UnescapeNewLines(s.GetString("NewLineChars", "\r\n"));
                }
            }
        }

        public static Encoding SniffEncoding(Stream stm)
        {
            byte[] bytes = new byte[16000];
            int len = stm.Read(bytes, 0, bytes.Length);
            stm.Seek(0, SeekOrigin.Begin);
            string xmlDeclPrefix = "<?xml";
            if (len > xmlDeclPrefix.Length && Encoding.UTF8.GetString(bytes, 0, xmlDeclPrefix.Length) == xmlDeclPrefix)
            {
                try
                {
                    using (var reader = XmlReader.Create(stm))
                    {
                        if (reader.Read() && reader.NodeType == XmlNodeType.XmlDeclaration)
                        {
                            var value = reader.GetAttribute("encoding");
                            return Encoding.GetEncoding(value);
                        }
                    }
                }
                catch
                {
                    return Encoding.UTF8;
                }
            }

            Encoding result = SniffByteOrderMark(bytes, len);
            if (result == null)
            {
                result = Encoding.UTF8;
            }
            return result;
        }

        public static void WriteFileWithoutBOM(MemoryStream ms, string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new byte[16000];
                int len = ms.Read(bytes, 0, bytes.Length);

                int start = 0;
                Encoding sniff = SniffByteOrderMark(bytes, len);
                if (sniff != null)
                {
                    if (sniff == Encoding.UTF8)
                    {
                        start = 3;
                    }
                    else if (sniff == Encoding.GetEncoding(12001) || sniff == Encoding.UTF32)  // UTF-32.
                    {
                        start = 4;
                    }
                    else if (sniff == Encoding.Unicode || sniff == Encoding.BigEndianUnicode)  // UTF-16.
                    {
                        start = 2;
                    }
                }

                while (len > 0)
                {
                    fs.Write(bytes, start, len - start);
                    len = ms.Read(bytes, 0, bytes.Length);
                    start = 0;
                }
            }
        }

        internal static Encoding SniffByteOrderMark(byte[] bytes, int len)
        {
            if (len >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf)
            {
                return Encoding.UTF8;
            }
            else if (len >= 4 && ((bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xfe && bytes[3] == 0xff) || (bytes[0] == 0xfe && bytes[1] == 0xff && bytes[2] == 0xfe && bytes[3] == 0xff)))
            {
                return Encoding.GetEncoding(12001); // big endian UTF-32.
            }
            else if (len >= 4 && ((bytes[0] == 0xff && bytes[1] == 0xfe && bytes[2] == 0x00 && bytes[3] == 0x00) || (bytes[0] == 0xff && bytes[1] == 0xfe && bytes[2] == 0xff && bytes[3] == 0xfe)))
            {
                return Encoding.UTF32; // skip UTF-32 little endian BOM
            }
            else if (len >= 2 && bytes[0] == 0xff && bytes[1] == 0xfe)
            {
                return Encoding.Unicode; // skip UTF-16 little endian BOM
            }
            else if (len >= 2 && bytes[0] == 0xf2 && bytes[1] == 0xff)
            {
                return Encoding.BigEndianUnicode; // skip UTF-16 big endian BOM
            }
            return null;
        }
    }

    public static class WebBrowser
    { 
        public static void OpenUrl(IntPtr hwnd, string url)
        {
            const int SW_SHOWNORMAL = 1;
            ShellExecute(hwnd, "open", url, null, Settings.Instance.StartupPath, SW_SHOWNORMAL);
        }

        [DllImport("Shell32.dll", EntryPoint = "ShellExecuteA",
             SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true,
             CallingConvention = CallingConvention.StdCall)]
        static extern int ShellExecute(IntPtr handle, string verb, string file,
            string args, string dir, int show);
    }

    public static class CurrentEvent
    {
        public static EventArgs Event;
    }

    public class PerformanceInfo : EventArgs
    {
        public long XsltMilliseconds;
        public long BrowserMilliseconds;

        public string BrowserName { get; set; }

        public static uint TickCount
        {
            get
            {
                return (uint)Environment.TickCount;
            }
        }
    }

}
