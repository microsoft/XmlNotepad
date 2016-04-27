using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

namespace XmlNotepad {

    /// <summary>
    /// XmlIncludeReader automatically expands XInclude elements and returns
    /// the expanded nodes making the XInclude 
    /// </summary>
    public class XmlIncludeReader : XmlReader, IXmlLineInfo {

        XmlReaderSettings settings;
        XmlReader reader; // current reader 
        Stack<XmlReader> stack = new Stack<XmlReader>();
        Stack<Uri> baseUris = new Stack<Uri>();
        Uri baseUri;

        public const string XIncludeNamespaceUri = "http://www.w3.org/2001/XInclude";

        public static XmlIncludeReader CreateIncludeReader(string url, XmlReaderSettings settings) {
            XmlIncludeReader r = new XmlIncludeReader();
            r.reader = XmlReader.Create(url, settings);
            r.settings = settings;
            return r;
        }

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

        public static XmlIncludeReader CreateIncludeReader(XmlDocument doc, XmlReaderSettings settings, string baseUri) {
            XmlIncludeReader r = new XmlIncludeReader();
            r.reader = new XmlNodeReader(doc);
            r.settings = settings;
            r.baseUri = new Uri(baseUri);
            return r;
        }

        public override XmlNodeType NodeType {
            get { return reader.NodeType;  }
        }

        public override string LocalName {
            get { return reader.LocalName; }
        }
        public override string NamespaceURI {
            get { return reader.NamespaceURI; }
        }
        public override string Prefix {
            get { return reader.Prefix; }
        }
        public override bool HasValue {
            get { return reader.HasValue; }
        }
        public override string Value {
            get { return reader.Value; }
        }
        public override string BaseURI {
            get {
                Uri uri = GetBaseUri();
                if (uri.IsFile) return uri.LocalPath;
                return uri.AbsoluteUri;
            }
        }
        public Uri GetBaseUri() {
            string s = reader.BaseURI;
            if (string.IsNullOrEmpty(s)) {
                if (baseUris.Count > 0) {
                    Uri curi = baseUris.Peek();
                    if (curi != null) return curi;
                }
                return this.baseUri;
            } else {
                return new Uri(s);
            }
        }

        public override bool IsEmptyElement {
            get { return reader.IsEmptyElement; }
        }
        public override int AttributeCount {
            get { return reader.AttributeCount; }
        }
        public override string GetAttribute(int i) {
            return reader.GetAttribute(i);
        }
        public override string GetAttribute(string name) {
            return reader.GetAttribute(name);
        }
        public override string GetAttribute(string name, string namespaceURI) {
            return reader.GetAttribute(name, namespaceURI);
        }
        public override bool MoveToAttribute(string name) {
            return reader.MoveToAttribute(name);
        }
        public override bool MoveToAttribute(string name, string ns) {
            return reader.MoveToAttribute(name, ns);
        }
        public override bool MoveToFirstAttribute() {
            return reader.MoveToFirstAttribute();
        }
        public override bool MoveToNextAttribute() {
            return reader.MoveToNextAttribute();
        }
        public override bool ReadAttributeValue() {
            return reader.ReadAttributeValue();
        }
        public override bool EOF {
            get { return reader.EOF; }
        }
        public override void Close() {
            reader.Close();
            while (stack.Count > 0) {
                reader = stack.Pop();
                reader.Close();
            }
        }
        public override ReadState ReadState {
            get { return reader.ReadState; }
        }
        public override XmlNameTable NameTable {
            get { return reader.NameTable; }
        }
        public override string LookupNamespace(string prefix) {
            return reader.LookupNamespace(prefix);
        }
        public override void ResolveEntity() {
            reader.ResolveEntity();
        }
        public override int Depth {
            get { return reader.Depth; }
        }
        public override bool MoveToElement() {
            return reader.MoveToElement();
        }

        /// <summary>
        /// This is the real meat of this class where we automatically expand the 
        /// XInclude elements following their href attributes, returning the expanded
        /// nodes.
        /// </summary>
        /// <returns>Returns false when all includes have been expanded and 
        /// we have reached the end of the top level document.</returns>
        public override bool Read() {
            bool rc = reader.Read();
        pop:
            while (!rc && stack.Count > 0) {
                reader.Close();
                reader = stack.Pop();
                rc = reader.Read();
            }
            if (!rc) return rc; // we're done!

            // Now check if we're on an XInclude element and expand it if we are.
            while (reader.NamespaceURI == XIncludeNamespaceUri) {
                if (reader.LocalName == "fallback" && reader.NodeType == XmlNodeType.EndElement) {
                    rc = reader.Read();
                } else if (reader.LocalName == "include") {
                    rc = ExpandInclude();
                } else {
                    rc = reader.Read();
                }
                if (!rc) goto pop;
            }
            return rc;
        }

        bool ExpandInclude() {
            string href = reader.GetAttribute("href");
            string parse = reader.GetAttribute("parse");
            string xpointer = reader.GetAttribute("xpointer");
            string encoding = reader.GetAttribute("encoding");
            string accept = reader.GetAttribute("accept");
            string acceptLanguage = reader.GetAttribute("accept-language");

            // todo: support for parse, xpointer, etc.
            if (string.IsNullOrEmpty(href)) {
                throw new ApplicationException(SR.IncludeHRefRequired);
            }

            XmlElement fallback = ReadFallback();

            try {
                Uri baseUri = this.GetBaseUri();
                Uri resolved = new Uri(baseUri, href);
                // HTTP has a limit of 2 requests per client on a given server, so we
                // have to cache the entire include to avoid a deadlock.
                using (XmlReader ir = XmlReader.Create(resolved.AbsoluteUri, settings)) {
                    XmlDocument include = new XmlDocument(reader.NameTable);
                    include.Load(ir);
                    if (include.FirstChild.NodeType == XmlNodeType.XmlDeclaration) {
                        // strip XML declarations.
                        include.RemoveChild(include.FirstChild);
                    }
                    stack.Push(reader);
                    baseUris.Push(resolved);
                    reader = new XmlNodeReader(include);
                    return reader.Read(); // initialize reader to first node in document.
                }
            } catch (Exception) {
                // return fall back element.
                if (fallback != null) {
                    baseUris.Push(this.GetBaseUri());
                    stack.Push(reader);
                    reader = new XmlNodeReader(fallback);
                    reader.Read(); // initialize reader
                    return reader.Read(); // consume fallback start tag.
                } else {
                    throw;
                }
            }


        }

        XmlElement ReadFallback() {
            XmlDocument fallback = new XmlDocument(reader.NameTable);
            if (reader.IsEmptyElement) {
                return null;
            } else {
                reader.Read();
            }
            while (reader.NodeType != XmlNodeType.EndElement) {
                if (reader.NamespaceURI == XIncludeNamespaceUri &&
                    reader.LocalName == "fallback") {
                    fallback.AppendChild(fallback.ReadNode(reader));
                } else {
                    reader.Skip();
                }
            }
            return fallback.DocumentElement;
        }

        #region IXmlLineInfo Members

        public bool HasLineInfo() {
            IXmlLineInfo xi = reader as IXmlLineInfo;
            return xi != null ? xi.HasLineInfo() : false;
        }

        public int LineNumber {
            get {
                IXmlLineInfo xi = reader as IXmlLineInfo;
                if (xi != null) return xi.LineNumber;
                return 0;
            }
        }

        public int LinePosition {
            get {
                IXmlLineInfo xi = reader as IXmlLineInfo;
                if (xi != null) return xi.LinePosition;
                return 0;
            }
        }

        #endregion
    }
}
