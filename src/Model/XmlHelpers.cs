using Microsoft.Xml;
using System.Collections.Generic;
using System.Xml;

namespace XmlNotepad
{
    public class XmlName
    {
        private string _prefix;
        private string _localName;
        private string _namespaceUri;

        public string Prefix
        {
            get { return _prefix; }
            set { _prefix = value; }
        }

        public string LocalName
        {
            get { return _localName; }
            set { _localName = value; }
        }

        public string NamespaceUri
        {
            get { return _namespaceUri; }
            set { _namespaceUri = value; }
        }
    }

    public sealed class XmlHelpers
    {
        private XmlHelpers() { }


        public static XmlName ParseName(XmlNode context, string name, XmlNodeType nt)
        {
            XmlName result = new XmlName();
            XmlConvert.VerifyName(name);
            int i = name.IndexOf(':');
            if (i > 0)
            {
                string prefix = result.Prefix = name.Substring(0, i);
                result.LocalName = name.Substring(i + 1);
                if (prefix == "xml")
                {
                    result.NamespaceUri = XmlStandardUris.XmlUri;
                }
                else if (prefix == "xmlns")
                {
                    result.NamespaceUri = XmlStandardUris.XmlnsUri;
                }
                else
                {
                    result.NamespaceUri = context.GetNamespaceOfPrefix(prefix);
                }
            }
            else
            {
                result.Prefix = "";
                result.LocalName = name;
                if (name == "xmlns")
                {
                    result.NamespaceUri = XmlStandardUris.XmlnsUri;
                }
                else if (nt == XmlNodeType.Attribute)
                {
                    result.NamespaceUri = ""; // non-prefixed attributes are empty namespace by definition
                }
                else
                {
                    result.NamespaceUri = context.GetNamespaceOfPrefix("");
                }
            }
            return result;
        }

        public static XmlName ParseName(XmlNamespaceManager nsmgr, string name, XmlNodeType nt)
        {
            XmlName result = new XmlName();
            XmlConvert.VerifyName(name);
            int i = name.IndexOf(':');
            if (i > 0)
            {
                string prefix = result.Prefix = name.Substring(0, i);
                result.LocalName = name.Substring(i + 1);
                if (prefix == "xml")
                {
                    result.NamespaceUri = XmlStandardUris.XmlUri;
                }
                else if (prefix == "xmlns")
                {
                    result.NamespaceUri = XmlStandardUris.XmlnsUri;
                }
                else
                {
                    result.NamespaceUri = nsmgr.LookupNamespace(prefix);
                }
            }
            else
            {
                result.LocalName = name;
                if (name == "xmlns")
                {
                    result.NamespaceUri = XmlStandardUris.XmlnsUri;
                }
                else if (nt == XmlNodeType.Attribute)
                {
                    result.NamespaceUri = ""; // non-prefixed attributes are empty namespace by definition
                }
                else
                {
                    result.NamespaceUri = nsmgr.LookupNamespace("");
                }
            }
            return result;
        }

        public static bool IsDefaultNamespaceInScope(XmlNode context, string nsuri)
        {
            var scope = GetNamespaceScope(context);
            return scope.LookupNamespace("") == nsuri;
        }

        public static bool IsPrefixInScope(XmlNode context, string prefix)
        {
            var scope = GetNamespaceScope(context);
            return scope.HasNamespace(prefix);
        }

        public static XmlNamespaceManager GetNamespaceScope(XmlNode context)
        {
            XmlDocument owner;
            if (context is XmlDocument xd)
            {
                owner = xd;
            }
            else
            {
                owner = context.OwnerDocument;
            }
            XmlNameTable nt = owner.NameTable;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(nt);
            XmlNode parent = context;

            while (parent != null)
            {
                if (parent is XmlElement)
                {
                    if (parent.Attributes != null)
                    {
                        foreach (XmlAttribute a in parent.Attributes)
                        {
                            if (a.NamespaceURI == XmlStandardUris.XmlnsUri)
                            {
                                string prefix = nt.Add(a.LocalName);
                                if (prefix == "xmlns") prefix = "";
                                if (!nsmgr.HasNamespace(prefix))
                                {
                                    nsmgr.AddNamespace(prefix, nt.Add(a.Value));
                                }
                            }
                        }
                    }
                }
                if (parent.NodeType == XmlNodeType.Attribute)
                {
                    parent = ((XmlAttribute)parent).OwnerElement;
                }
                else
                {
                    parent = parent.ParentNode;
                }
            }
            return nsmgr;
        }

        public static string GetXPathLocation(XmlNode context, XmlNamespaceManager scope)
        {
            XPathGenerator gen = new XPathGenerator();
            return gen.GetXPath(context, scope);
        }

        public static bool MissingNamespace(XmlName name)
        {
            return !string.IsNullOrEmpty(name.Prefix) && string.IsNullOrEmpty(name.NamespaceUri) &&
                name.Prefix != "xmlns" && name.LocalName != "xmlns" && name.Prefix != "xml";
        }

        public static XmlAttribute GenerateNamespaceDeclaration(XmlElement context, XmlName name)
        {
            int count = 1;
            while (!string.IsNullOrEmpty(context.GetPrefixOfNamespace("uri:" + count)))
            {
                count++;
            }
            name.NamespaceUri = "uri:" + count;
            XmlAttribute xmlns = context.OwnerDocument.CreateAttribute("xmlns", name.Prefix, XmlStandardUris.XmlnsUri);
            if (context.HasAttribute(xmlns.Name))
            {
                // already have an attribute with this name! This is a tricky case where
                // user is deleting a namespace declaration.  We don't want to reinsert it
                // automatically in that case!
                return null;
            }
            xmlns.Value = name.NamespaceUri;
            return xmlns;
        }

        public static bool IsXmlnsNode(XmlNode node)
        {
            if (node == null) return false;
            return node.NodeType == XmlNodeType.Attribute &&
                (node.LocalName == "xmlns" || node.Prefix == "xmlns");
        }

        public static bool IsXsiAttribute(XmlNode node)
        {
            if (node == null) return false;
            return node.NodeType == XmlNodeType.Attribute &&
                (node.LocalName == "type" && node.NamespaceURI == "http://www.w3.org/2001/XMLSchema-instance");
        }
    }


    public class MyXmlNamespaceResolver : System.Xml.IXmlNamespaceResolver
    {
        private System.Xml.XmlNameTable _nameTable;
        private XmlNode _context;
        private string _emptyAtom;

        public MyXmlNamespaceResolver(System.Xml.XmlNameTable nameTable)
        {
            this._nameTable = nameTable;
            this._emptyAtom = nameTable.Add(string.Empty);
        }

        public XmlNode Context
        {
            get
            {
                return this._context;
            }
            set
            {
                this._context = value;
            }
        }

        public System.Xml.XmlNameTable NameTable
        {
            get
            {
                return this._nameTable;
            }
        }

        private string Atomized(string s)
        {
            if (s == null) return null;
            if (s.Length == 0) return this._emptyAtom;
            return this._nameTable.Add(s);
        }

        public string LookupPrefix(string namespaceName, bool atomizedName)
        {
            string result = null;
            if (_context != null)
            {
                result = _context.GetPrefixOfNamespace(namespaceName);
            }
            return Atomized(result);
        }

        public string LookupPrefix(string namespaceName)
        {
            string result = null;
            if (_context != null)
            {
                result = _context.GetPrefixOfNamespace(namespaceName);
            }
            return Atomized(result);
        }

        public string LookupNamespace(string prefix, bool atomizedName)
        {
            return LookupNamespace(prefix);
        }

        public string LookupNamespace(string prefix)
        {
            string result = null;
            if (_context != null)
            {
                result = _context.GetNamespaceOfPrefix(prefix);
            }
            return Atomized(result);
        }

        public IDictionary<string, string> GetNamespacesInScope(System.Xml.XmlNamespaceScope scope)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            if (this._context != null)
            {
                foreach (XmlAttribute a in this._context.SelectNodes("namespace::*"))
                {
                    string nspace = a.InnerText;
                    string prefix = a.Prefix;
                    if (prefix == "xmlns")
                    {
                        prefix = "";
                    }
                    dict[prefix] = nspace;
                }
            }
            return dict;
        }

    }

}
