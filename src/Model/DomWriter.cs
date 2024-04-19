using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace XmlNotepad
{
    /// <summary>
    /// This class avoids the dreaded namespace mismatch error from XmlWriter when writing
    /// an edited DOM where user has changed the xmlns value, but the parent XmlElement has
    /// not been recreated in that namespace.  Having to recreate the DOM in memory one all
    /// ns changes is too much of a performance hit, so we fix the problem this way.
    /// </summary>
    public class DomWriter
    {
        XmlWriter w;
        public DomWriter(XmlWriter w)
        {
            this.w = w;
        }

        public void Write(XmlDocument doc)
        {
            WriteChildren(doc);
        }

        internal void WriteChildren(XmlNode parent)
        {
            for (XmlNode child = parent.FirstChild; child != null; child = child.NextSibling)
            {
                switch (child.NodeType)
                {
                    case XmlNodeType.Element:
                        WriteElement((XmlElement)child);
                        break;
                    case XmlNodeType.Text:
                        w.WriteString(child.Value);
                        break;
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        w.WriteWhitespace(child.Value);
                        break;
                    case XmlNodeType.CDATA:
                        w.WriteCData(child.Value);
                        break;
                    case XmlNodeType.Comment:
                        w.WriteComment(child.Value);
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        w.WriteProcessingInstruction(child.Name, child.Value);
                        break;
                    case XmlNodeType.DocumentType:
                        w.WriteDocType(child.Name, ((XmlDocumentType)child).PublicId, ((XmlDocumentType)child).SystemId, child.Value);
                        break;
                    case XmlNodeType.XmlDeclaration:
                        w.WriteProcessingInstruction("xml", ((XmlDeclaration)child).Value);
                        break;
                    default:
                        throw new Exception("Unexpected node type " + child.NodeType);
                }
            }
        }

        public void WriteElement(XmlElement e)
        {
            this.w.WriteStartElement(e.Prefix, e.LocalName, FindNamespaceUri(e));
            this.WriteAttributes(e.Attributes);
            this.WriteChildren(e);
            this.w.WriteEndElement();
        }

        private string FindNamespaceUri(XmlElement e)
        {
            foreach (XmlAttribute a in e.Attributes)
            {
                if (a.NamespaceURI == "http://www.w3.org/2000/xmlns/")
                {
                    if (a.Prefix == e.Prefix)
                    {
                        return a.Value;
                    }
                }
            }
            return e.NamespaceURI;
        }

        private void WriteAttributes(XmlAttributeCollection attributes)
        {
            foreach (XmlAttribute a in attributes)
            {
                if (a.NamespaceURI == "http://www.w3.org/2000/xmlns/")
                {
                    if (a.LocalName == "xmlns")
                    {
                        this.w.WriteAttributeString("xmlns", a.Value);
                    }
                    else
                    {
                        this.w.WriteAttributeString("xmlns", a.LocalName, null, a.Value);
                    }
                }
                else
                {
                    this.w.WriteAttributeString(a.Prefix, a.LocalName, a.NamespaceURI, a.Value);
                }
            }
        }
    }
}
