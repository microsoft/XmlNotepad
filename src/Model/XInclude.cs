using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;

namespace XmlNotepad
{

    /// <summary>
    /// XmlIncludeReader automatically expands XInclude elements and returns
    /// the expanded nodes making the XInclude 
    /// </summary>
    public class XmlIncludeReader : XmlReader, IXmlLineInfo
    {
        private XmlReaderSettings _settings;
        private XmlReader _reader; // current reader 
        private Stack<XmlReader> _stack = new Stack<XmlReader>();
        private Stack<Uri> _baseUris = new Stack<Uri>();
        private Uri _baseUri;
        private bool _cancelled;
        private long _position;
        private long _size;

        public const string XIncludeNamespaceUri = "http://www.w3.org/2001/XInclude";

        public static XmlIncludeReader CreateIncludeReader(string url, XmlReaderSettings settings)
        {
            XmlIncludeReader r = new XmlIncludeReader();
            r._reader = XmlReader.Create(url, settings);
            r._settings = settings;
            return r;
        }

        public void Cancel()
        {
            this._cancelled = true;
        }

        public long Position => _position;

        public long Size => _size;

        // [cjl] dead code removal
        //public static XmlIncludeReader CreateIncludeReader(Stream stream, XmlReaderSettings settings, string baseUri) {
        //    XmlIncludeReader r = new XmlIncludeReader();
        //    r.reader = XmlReader.Create(stream, settings, baseUri);
        //    r.settings = settings;
        //    r.baseUri = new Uri(baseUri);
        //    return r;
        //}
        //public static XmlIncludeReader CreateIncludeReader(TextReader reader, XmlReaderSettings settings, string baseUri) {
        //    XmlIncludeReader r = new XmlIncludeReader();
        //    r.reader = XmlReader.Create(reader, settings, baseUri);
        //    r.settings = settings;
        //    r.baseUri = new Uri(baseUri);
        //    return r;
        //}
        //public static XmlIncludeReader CreateIncludeReader(TextReader reader, XmlReaderSettings settings, XmlParserContext context) {
        //    XmlIncludeReader r = new XmlIncludeReader();
        //    r.reader = XmlReader.Create(reader, settings, context);
        //    r.settings = settings;
        //    return r;
        //}

        public static XmlIncludeReader CreateIncludeReader(XmlDocument doc, XmlReaderSettings settings, string baseUri)
        {
            XmlIncludeReader r = new XmlIncludeReader();
            r._reader = new XmlNodeReader(doc);
            r._settings = settings;
            r._baseUri = new Uri(baseUri);
            r._size = CountNodes(doc);
            r._position = 0;
            return r;
        }

        static long CountNodes(XmlNode node)
        {
            long count = 1;            
            for (var child = node.FirstChild; child != null; child = child.NextSibling)
            {
                count++;
                if (child.HasChildNodes)
                {
                    count += CountNodes(child);
                }
            }
            return count;
        }

        public override XmlNodeType NodeType
        {
            get { return _reader.NodeType; }
        }

        public override string LocalName
        {
            get { return _reader.LocalName; }
        }
        public override string NamespaceURI
        {
            get { return _reader.NamespaceURI; }
        }
        public override string Prefix
        {
            get { return _reader.Prefix; }
        }
        public override bool HasValue
        {
            get { return _reader.HasValue; }
        }
        public override string Value
        {
            get { return _reader.Value; }
        }
        public override string BaseURI
        {
            get
            {
                Uri uri = GetBaseUri();
                if (uri.IsFile) return uri.LocalPath;
                return uri.AbsoluteUri;
            }
        }
        public Uri GetBaseUri()
        {
            string s = _reader.BaseURI;
            if (string.IsNullOrEmpty(s))
            {
                if (_baseUris.Count > 0)
                {
                    Uri curi = _baseUris.Peek();
                    if (curi != null) return curi;
                }
                return this._baseUri;
            }
            else
            {
                return new Uri(s);
            }
        }

        public override bool IsEmptyElement
        {
            get { return _reader.IsEmptyElement; }
        }
        public override int AttributeCount
        {
            get { return _reader.AttributeCount; }
        }
        public override string GetAttribute(int i)
        {
            return _reader.GetAttribute(i);
        }
        public override string GetAttribute(string name)
        {
            return _reader.GetAttribute(name);
        }
        public override string GetAttribute(string name, string namespaceURI)
        {
            return _reader.GetAttribute(name, namespaceURI);
        }
        public override bool MoveToAttribute(string name)
        {
            return _reader.MoveToAttribute(name);
        }
        public override bool MoveToAttribute(string name, string ns)
        {
            return _reader.MoveToAttribute(name, ns);
        }
        public override bool MoveToFirstAttribute()
        {
            return _reader.MoveToFirstAttribute();
        }
        public override bool MoveToNextAttribute()
        {
            return _reader.MoveToNextAttribute();
        }
        public override bool ReadAttributeValue()
        {
            return _reader.ReadAttributeValue();
        }
        public override bool EOF
        {
            get { return _reader.EOF; }
        }
        public override void Close()
        {
            _reader.Close();
            while (_stack.Count > 0)
            {
                _reader = _stack.Pop();
                _reader.Close();
            }
        }
        public override ReadState ReadState
        {
            get { return _reader.ReadState; }
        }
        public override XmlNameTable NameTable
        {
            get { return _reader.NameTable; }
        }
        public override string LookupNamespace(string prefix)
        {
            return _reader.LookupNamespace(prefix);
        }
        public override void ResolveEntity()
        {
            _reader.ResolveEntity();
        }
        public override int Depth
        {
            get { return _reader.Depth; }
        }
        public override bool MoveToElement()
        {
            return _reader.MoveToElement();
        }

        /// <summary>
        /// This is the real meat of this class where we automatically expand the 
        /// XInclude elements following their href attributes, returning the expanded
        /// nodes.
        /// </summary>
        /// <returns>Returns false when all includes have been expanded and 
        /// we have reached the end of the top level document.</returns>
        public override bool Read()
        {
            if (_cancelled)
            {
                throw new System.Threading.Tasks.TaskCanceledException();
            }
            _position++;

            bool rc = _reader.Read();
        pop:
            while (!rc && _stack.Count > 0)
            {
                _reader.Close();
                _reader = _stack.Pop();
                rc = _reader.Read();
            }
            if (!rc) return rc; // we're done!

            // Now check if we're on an XInclude element and expand it if we are.
            while (_reader.NamespaceURI == XIncludeNamespaceUri)
            {
                if (_reader.LocalName == "fallback" && _reader.NodeType == XmlNodeType.EndElement)
                {
                    rc = _reader.Read();
                }
                else if (_reader.LocalName == "include")
                {
                    rc = ExpandInclude();
                }
                else
                {
                    rc = _reader.Read();
                }
                if (!rc) goto pop;
            }
            return rc;
        }

        bool ExpandInclude()
        {
            string href = _reader.GetAttribute("href");
            string parse = _reader.GetAttribute("parse");
            string xpointer = _reader.GetAttribute("xpointer");
            string encoding = _reader.GetAttribute("encoding");
            string accept = _reader.GetAttribute("accept");
            string acceptLanguage = _reader.GetAttribute("accept-language");

            // todo: support for parse, xpointer, etc.
            if (string.IsNullOrEmpty(href))
            {
                throw new ApplicationException(Strings.IncludeHRefRequired);
            }

            XmlElement fallback = ReadFallback();

            try
            {
                Uri baseUri = this.GetBaseUri();
                Uri resolved = new Uri(baseUri, href);
                // HTTP has a limit of 2 requests per client on a given server, so we
                // have to cache the entire include to avoid a deadlock.
                using (XmlReader ir = XmlReader.Create(resolved.AbsoluteUri, _settings))
                {
                    XmlDocument include = new XmlDocument(_reader.NameTable);
                    include.Load(ir);
                    if (include.FirstChild.NodeType == XmlNodeType.XmlDeclaration)
                    {
                        // strip XML declarations.
                        include.RemoveChild(include.FirstChild);
                    }
                    _stack.Push(_reader);
                    _baseUris.Push(resolved);
                    _reader = new XmlNodeReader(include);
                    _size += CountNodes(include);
                    return _reader.Read(); // initialize reader to first node in document.
                }
            }
            catch (Exception)
            {
                // return fall back element.
                if (fallback != null)
                {
                    _baseUris.Push(this.GetBaseUri());
                    _stack.Push(_reader);
                    _reader = new XmlNodeReader(fallback);
                    _reader.Read(); // initialize reader
                    _size += CountNodes(fallback);
                    return _reader.Read(); // consume fallback start tag.
                }
                else
                {
                    throw;
                }
            }


        }

        XmlElement ReadFallback()
        {
            XmlDocument fallback = new XmlDocument(_reader.NameTable);
            if (_reader.IsEmptyElement)
            {
                return null;
            }
            else
            {
                _reader.Read();
            }
            while (_reader.NodeType != XmlNodeType.EndElement)
            {
                if (_reader.NamespaceURI == XIncludeNamespaceUri &&
                    _reader.LocalName == "fallback")
                {
                    fallback.AppendChild(fallback.ReadNode(_reader));
                }
                else
                {
                    _reader.Skip();
                }
            }
            return fallback.DocumentElement;
        }

        #region IXmlLineInfo Members

        public bool HasLineInfo()
        {
            IXmlLineInfo xi = _reader as IXmlLineInfo;
            return xi != null ? xi.HasLineInfo() : false;
        }

        public int LineNumber
        {
            get
            {
                IXmlLineInfo xi = _reader as IXmlLineInfo;
                if (xi != null) return xi.LineNumber;
                return 0;
            }
        }

        public int LinePosition
        {
            get
            {
                IXmlLineInfo xi = _reader as IXmlLineInfo;
                if (xi != null) return xi.LinePosition;
                return 0;
            }
        }

        #endregion
    }
}
