using Microsoft.Xml;
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

        public const string XmlnsUri = "http://www.w3.org/2000/xmlns/";
        public const string XmlUri = "http://www.w3.org/XML/1998/namespace";

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
                    result.NamespaceUri = XmlUri;
                }
                else if (prefix == "xmlns")
                {
                    result.NamespaceUri = XmlnsUri;
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
                    result.NamespaceUri = XmlnsUri;
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
                    result.NamespaceUri = XmlUri;
                }
                else if (prefix == "xmlns")
                {
                    result.NamespaceUri = XmlnsUri;
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
                    result.NamespaceUri = XmlnsUri;
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

        public static bool IsNamespaceInScope(XmlNode context, string nsuri)
        {
            var scope = GetNamespaceScope(context);
            return scope.HasNamespace(nsuri);
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
                            if (a.NamespaceURI == XmlnsUri)
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
            XmlAttribute xmlns = context.OwnerDocument.CreateAttribute("xmlns", name.Prefix, XmlHelpers.XmlnsUri);
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
}
