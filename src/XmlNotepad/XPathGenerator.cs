using System;
using System.Collections;
using System.Text;
using System.Xml;

namespace Microsoft.Xml {

    /// <summary>
    /// The XPathGenerator takes an XmlNode object, and an XmlNamespaceManager as follows:
    /// <code>
    /// <line><span style='color:teal'>XmlDocument</span> doc = <span style='color:blue'>new</span>&#160;
    /// <span style='color:teal'>XmlDocument</span>();</line>
    /// 
    /// <line>doc.Load(<span style='color:maroon'>&quot;test.xml&quot;</span>);</line>
    /// 
    /// <line><span style='color:teal'>XmlNode</span> node = doc.DocumentElement.FirstChild.LastChild;</line>
    /// 
    /// <line>&#160;</line>
    /// 
    /// <line><span style='color:teal'>XmlNamespaceManager</span><span> nsmgr = <span
    /// style='color:blue'>new</span>&#160;<span style='color:teal'>XmlNamespaceManager</span>(doc.NameTable);</span></line>
    /// 
    /// <line><span style='color:teal'>XPathGenerator</span>
    ///  gen = <span style='color:blue'>new</span>&#160;<span style='color:teal'>XPathGenerator</span>();</line>
    /// 
    /// <line><span style='color:blue'>string</span><span >
    /// xpath = gen.GetXPath(node, nsmgr);</span></line>
    /// </code>
    /// <line>
    /// The resulting string can then be used in SelectNodes or SelectSingleNodes 
    /// using the same XmlNamespaceManager and it will find the same node as follows: 
    /// </line>    
    /// <code>
    /// <line><span style='color:teal'>XmlNode</span> found = doc.SelectSingleNode(xpath, nsmgr); </line>
    /// <line><span style='color:teal'>Debug</span>.Assert(found == node);</line>
    /// </code>
    /// </summary>
    public class XPathGenerator {

        int nextPrefix;
        bool useIndices;

        /// <summary>
        /// Construct new XPathGenerator.  
        /// </summary>        
        public XPathGenerator() {
        }

        /// <summary>
        /// Construct new XPathGenerator with a flag to tell it to always specify 
        /// child index positions in the resulting xpath expression.  By default
        /// the XPathGenerator only generates child index positions if the child
        /// is not uniquely named in the collection of children. For example, you
        /// might get the xpath expression:
        /// <code>
        /// <line>/html/body/p[5]/span</line>
        /// </code>
        /// By passing true in this constructor you will get the following instead:
        /// <code>
        /// <line>/html[1]/body[1]/p[5]/span[1]</line>
        /// </code>
        /// </summary>
        /// <param name="useIndices">Specify whether you want the XPathGenerator
        /// to always include child index positions.  Default is false.
        /// </param>
        public XPathGenerator(bool useIndices) {
            this.useIndices = useIndices;
        }

        /// <summary>
        /// Return an XPath that will locate the given node within it's
        /// document and populate an XmlNamespaceManager with the namespace
        /// prefixes used in that XPath query.  You can then
        /// use this XmlNamespaceManager in the call 
        /// the SelectNodes or SelectSingleNode methods.
        /// </summary>
        /// <param name="node">The node to locate via XPath</param>
        /// <param name="nsmgr">A namespace manager, it may be empty
        /// or it may be pre-populated with the prefixes you want to use.  
        /// Either way if a namespace prefix is needed that is not defined or 
        /// if it conflicts with another definition
        /// then a new prefix will be generated automatically.</param>
        /// <returns>The XPath expression needed to locate the given node
        /// or null if the node is not locatable by XPath because of it's
        /// NodeType.</returns>
        public string GetXPath(XmlNode node, XmlNamespaceManager nsmgr) {
            if (node == null) {
                throw new System.ArgumentNullException("node");
            }
            if (nsmgr == null) {
                throw new System.ArgumentNullException("nsmgr");
            }
            nextPrefix = 0;
            StringBuilder sb = new StringBuilder();
            switch (node.NodeType) {
                // these node types are not accessible to XPath
                case XmlNodeType.Document:
                case XmlNodeType.DocumentType:
                case XmlNodeType.EntityReference:
                case XmlNodeType.DocumentFragment:
                case XmlNodeType.EndElement:
                case XmlNodeType.EndEntity:
                case XmlNodeType.Entity:
                case XmlNodeType.None:
                case XmlNodeType.Notation:
                case XmlNodeType.XmlDeclaration:
                    return null;
            }
            
            NodeToXPath(node, sb, nsmgr);
            return sb.ToString();
        }

        void NodeToXPath(XmlNode node, StringBuilder sb, XmlNamespaceManager nsmgr) {
            if (node != null) {
                XmlNode parent = node.ParentNode;
                if (parent == null) {
                    // ParentNode doesn't work on Attributes!
                    parent = node.SelectSingleNode("..");
                }
                NodeToXPath(parent, sb, nsmgr);
                string path = GetPathInParent(node, nsmgr);
                if (path != null) {
                    sb.Append("/");
                    sb.Append(path);
                }
            }
        }

        string GetPathInParent(XmlNode node, XmlNamespaceManager nsmgr) {
            XmlNodeType nt = node.NodeType;
            if (nt == XmlNodeType.Attribute) {
                if (node.NamespaceURI == "http://www.w3.org/2000/xmlns/") {
                    if (string.IsNullOrEmpty(node.Prefix) &&
                        node.LocalName == "xmlns") {
                        return "namespace::*[local-name()='']";// and .='" + node.Value + "']";
                    } else {
                        return "namespace::" + node.LocalName;
                    }
                }
                // attributes are unique by definition, so no indices are
                // required.
                return string.Format("@{0}", GetQualifiedPath(node, nsmgr)); 
            }
                            
            XmlNode parent = node.ParentNode;
            if (parent != null) {
                int count = 0;
                int index = 0;
                bool wasText = false;
                XmlNode child = parent.FirstChild;
                while (child != null) {
                    if (child == node) {
                        index = count;
                    }
                    XmlNodeType ct = child.NodeType;
                    if (IsTextNode(ct)) {
                        if (IsTextNode(nt)) {      
                            // Adjacent text nodes are merged in XPath model.
                            if (!wasText) count++;                             
                        }
                        wasText = true;
                    } else {
                        wasText = false;
                        if (ct == nt && child.Name == node.Name) {
                            count++;
                        }
                    }
                    child = child.NextSibling;
                }

                string selector = null;
                switch (node.NodeType) {
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        selector = "text()";
                        break;
                    case XmlNodeType.Comment:
                        selector = "comment()";
                        break;
                    case XmlNodeType.Element:
                        selector = GetQualifiedPath(node, nsmgr);
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        selector = "processing-instruction('" + node.Name + "')";
                        break;
                }

                if (!this.useIndices && count < 2) { // it's unique already, so no need for indices.
                    return selector;
                }
                index++; // XPath indices are 1-based
                return string.Format("{0}[{1}]", selector, index.ToString());
            } else {
                return null;
            }
        }

        static bool IsTextNode(XmlNodeType nt) {
            return nt == XmlNodeType.SignificantWhitespace ||
                  nt == XmlNodeType.Whitespace ||
                  nt == XmlNodeType.Text ||
                  nt == XmlNodeType.CDATA ||
                  nt == XmlNodeType.EntityReference;
        }

        string GetQualifiedPath(XmlNode node, XmlNamespaceManager nsmgr) {
            string nsuri = node.NamespaceURI;
            string prefix = node.Prefix;
            string localName = node.LocalName;

            if (!string.IsNullOrEmpty(nsuri)) {
                string p = nsmgr.LookupPrefix(nsuri);
                if (!string.IsNullOrEmpty(p)) {
                    // Use previously defined prefix for this namespace.
                    prefix = p;
                } else {
                    string found = nsmgr.LookupNamespace(prefix);
                    if (found == null && !string.IsNullOrEmpty(prefix)) {
                        nsmgr.AddNamespace(prefix, nsuri);
                    } else if (found != node.NamespaceURI) {
                        // we have a prefix conflict, so need to invent a new 
                        // prefix for this part of the query.
                        int i = nextPrefix++;
                        int number = (i / 26);
                        char letter = Convert.ToChar('a' + i - (26 * number));
                        if (number == 0) {
                            prefix = letter.ToString();
                        } else {
                            prefix = string.Format("{0}{1}", letter, number);
                        }
                        nsmgr.AddNamespace(prefix, nsuri);
                    }
                }
                return string.Format("{0}:{1}", prefix, localName);
            }
            return localName;
        }

    }
}