//  ---------------------------------------------------------------------------
// <copyright company="Microsoft Corporation" file="XmlDiffPath.cs">
//     Copyright (c) Microsoft Corporation 2005
// </copyright>
// <project>
//     XmlDiffView
// </project>
// <summary>
//     Provide lists of nodes and attributes
//     based on proprietary alphanumeric path statements.
// </summary>
// <history>
//      [barryw] 03MAR15 Created
// </history>
//  ---------------------------------------------------------------------------

namespace Microsoft.XmlDiffPatch {
    #region Using directives

    using System;
    using System.Diagnostics;

    #endregion

    /// <summary>
    /// Class to provide lists of nodes and attributes
    /// based on proprietary alphanumeric path statements
    /// </summary>
    internal class XmlDiffPath {

        #region Member variables section

        private static char[] delimites = new
            char[] {
                       '|', '-', '/'
                   };

        #endregion

        #region Methods section

        /// <summary>
        /// Gets the list of nodes or attributes below the position indicated
        /// </summary>
        /// <param name="rootNode">The root node for the xml data</param>
        /// <param name="currentParentNode">The current node</param>
        /// <param name="xmlDiffPathExpr">Proprietary path statement separator or character</param>
        /// <returns>List of nodes or attributes</returns>
        public static XmlDiffPathNodeList SelectNodes(
            XmlDiffViewParentNode rootNode,
            XmlDiffViewParentNode currentParentNode,
            string xmlDiffPathExpr) {
            switch (xmlDiffPathExpr[0]) {
                case '/':
                    return SelectAbsoluteNodes(rootNode, xmlDiffPathExpr);
                case '@':
                    if (xmlDiffPathExpr.Length < 2) {
                        OnInvalidExpression(xmlDiffPathExpr);
                    }
                    if (xmlDiffPathExpr[1] == '*') {
                        return SelectAllAttributes(
                            (XmlDiffViewElement)currentParentNode);
                    } else {
                        return SelectAttributes(
                            (XmlDiffViewElement)currentParentNode, xmlDiffPathExpr);
                    }
                case '*':
                    if (xmlDiffPathExpr.Length == 1) {
                        return SelectAllChildren(currentParentNode);
                    } else {
                        OnInvalidExpression(xmlDiffPathExpr);
                        return null;
                    }
                default:
                    int startPosition = 0;
                    return SelectChildNodes(
                        currentParentNode,
                        xmlDiffPathExpr,
                        startPosition);
            }
        }

        /// <summary>
        /// Gets a list of node objects corresponding to
        /// the proprietary path reference provided. 
        /// </summary>
        /// <param name="rootNode">The starting node</param>
        /// <param name="path">Absolute path reference to node of interest</param>
        /// <returns>list of node objects</returns>
        private static XmlDiffPathNodeList SelectAbsoluteNodes(
            XmlDiffViewParentNode rootNode,
            string path) {
            Debug.Assert(path[0] == '/');

            int pos = 1;
            XmlDiffViewNode node = rootNode;

            for (; ; ) {
                int startPos = pos;
                int nodePos = ReadPosition(path, ref pos);

                if (pos == path.Length || path[pos] == '/') {
                    if (node.FirstChildNode == null) {
                        OnNoMatchingNode(path);
                    }

                    XmlDiffViewParentNode parentNode = (XmlDiffViewParentNode)node;
                    if (nodePos <= 0 || nodePos > parentNode.
                        SourceChildNodesCount) {
                        OnNoMatchingNode(path);
                    }

                    node = parentNode.GetSourceChildNode(nodePos - 1);

                    if (pos == path.Length) {
                        XmlDiffPathNodeList list = new
                            XmlDiffPathSingleNodeList();
                        list.AddNode(node);
                        return list;
                    }

                    pos++;
                } else {
                    if (path[pos] == '-' || path[pos] == '|') {
                        if (node.FirstChildNode == null) {
                            OnNoMatchingNode(path);
                        }
                        return SelectChildNodes(
                            ((XmlDiffViewParentNode)node),
                            path,
                            startPos);
                    }

                    OnInvalidExpression(path);
                }
            }
        }

        /// <summary>
        /// Gets a list of the attributes for the specifed node
        /// and if applicable, its children.
        /// </summary>
        /// <param name="parentElement">The node which 
        /// contains the attributes</param>
        /// <returns>List of attributes</returns>
        private static XmlDiffPathNodeList SelectAllAttributes(
            XmlDiffViewElement parentElement) {
            if (parentElement.Attributes == null) {
                OnNoMatchingNode("@*");
                return null;
            } else if (parentElement.Attributes.NextSibbling == null) {
                XmlDiffPathNodeList nodeList = new XmlDiffPathSingleNodeList();
                nodeList.AddNode(parentElement.Attributes);
                return nodeList;
            } else {
                XmlDiffPathNodeList nodeList = new XmlDiffPathMultiNodeList();
                XmlDiffViewAttribute curAttr = parentElement.Attributes;
                while (curAttr != null) {
                    nodeList.AddNode(curAttr);
                }
                return nodeList;
            }
        }

        /// <summary>
        /// Gets a list of attribute objects based on the location
        /// specified.
        /// </summary>
        /// <param name="parentElement">Node at which to start the path search</param>
        /// <param name="path">Proprietary alphanumeric path statement</param>
        /// <returns>list of attribute objects</returns>
        private static XmlDiffPathNodeList SelectAttributes(
            XmlDiffViewElement parentElement,
            string path) {
            Debug.Assert(path[0] == '@');

            int pos = 1;
            XmlDiffPathNodeList nodeList = null;
            for (; ; ) {
                string name = ReadAttrName(path, ref pos);

                if (nodeList == null) {
                    if (pos == path.Length) {
                        nodeList = new XmlDiffPathSingleNodeList();
                    } else {
                        nodeList = new XmlDiffPathMultiNodeList();
                    }
                }

                XmlDiffViewAttribute attr = parentElement.GetAttribute(name);
                if (attr == null) {
                    OnNoMatchingNode(path);
                }
                nodeList.AddNode(attr);

                if (pos == path.Length) {
                    break;
                } else if (path[pos] == '|') {
                    pos++;
                    if (path[pos] != '@') {
                        OnInvalidExpression(path);
                    }
                    pos++;
                } else {
                    OnInvalidExpression(path);
                }
            }
            return nodeList;
        }

        /// <summary>
        /// Gets a list of all node objects at and below the location
        /// specified.
        /// </summary>
        /// <param name="parentNode">Node at which to start</param>
        /// <returns>list of node objects</returns>
        private static XmlDiffPathNodeList SelectAllChildren(
            XmlDiffViewParentNode parentNode) {
            if (parentNode.ChildNodes == null) {
                OnNoMatchingNode("*");
                return null;
            } else if (parentNode.ChildNodes.NextSibbling == null) {
                XmlDiffPathNodeList nodeList = new XmlDiffPathSingleNodeList();
                nodeList.AddNode(parentNode.ChildNodes);
                return nodeList;
            } else {
                XmlDiffPathNodeList nodeList = new XmlDiffPathMultiNodeList();
                XmlDiffViewNode childNode = parentNode.ChildNodes;
                while (childNode != null) {
                    nodeList.AddNode(childNode);
                    childNode = childNode.NextSibbling;
                }
                return nodeList;
            }
        }

        /// <summary>
        /// Gets the list of child nodes below the position indicated
        /// </summary>
        /// <param name="parentNode">The current node</param>
        /// <param name="path">Proprietary path statement</param>
        /// <param name="startPos">Position in the path statement 
        /// at which to start collecting node objects.</param>
        /// <returns>list of child nodes</returns>
        /// <returns>List of nodes or attributes</returns>
        private static XmlDiffPathNodeList SelectChildNodes(
            XmlDiffViewParentNode parentNode,
            string path,
            int startPos) {
            int pos = startPos;
            XmlDiffPathNodeList nodeList = null;

            for (; ; ) {
                int nodePos = ReadPosition(path, ref pos);

                if (pos == path.Length) {
                    nodeList = new XmlDiffPathSingleNodeList();
                } else {
                    nodeList = new XmlDiffPathMultiNodeList();
                }
                if (nodePos <= 0 || nodePos > parentNode.SourceChildNodesCount) {
                    OnNoMatchingNode(path);
                }
                nodeList.AddNode(parentNode.GetSourceChildNode(nodePos - 1));

                if (pos == path.Length) {
                    break;
                } else if (path[pos] == '|') {
                    pos++;
                } else if (path[pos] == '-') {
                    pos++;
                    int endNodePos = ReadPosition(path, ref pos);
                    if (endNodePos <= 0 || endNodePos > parentNode.SourceChildNodesCount) {
                        OnNoMatchingNode(path);
                    }
                    while (nodePos < endNodePos) {
                        nodePos++;
                        nodeList.AddNode(parentNode.GetSourceChildNode(nodePos - 1));
                    }

                    if (pos == path.Length) {
                        break;
                    } else if (path[pos] == '|') {
                        pos++;
                    } else {
                        OnInvalidExpression(path);
                    }
                }
            }
            return nodeList;
        }

        /// <summary>
        /// Gets the numeric value at the specified
        /// position in the statement
        /// </summary>
        /// <param name="str">Statement to search</param>
        /// <param name="pos">Position at which to start the search</param>
        /// <returns>Representation of the position in the absolute path to the node</returns>
        private static int ReadPosition(string str, ref int pos) {
            Debug.Assert(pos <= str.Length);

            int end = str.IndexOfAny(delimites, pos);
            if (end < 0) {
                end = str.Length;
            }
            // TODO: better error handling if this should be shipped
            int nodePos = int.Parse(str.Substring(pos, end - pos));

            pos = end;
            return nodePos;
        }

        /// <summary>
        /// Returns the sub-string representing the attribute name which
        /// starts at the specified position in the provided statement
        /// and ends just before a vertical bar character or the end 
        /// of the specifed statement.
        /// </summary>
        /// <param name="str">Statement to search</param>
        /// <param name="pos">Position at which to start the search</param>
        /// <returns>attribute name</returns>
        private static string ReadAttrName(string str, ref int pos) {
            Debug.Assert(pos <= str.Length);

            int end = str.IndexOf('|', pos);
            if (end < 0) {
                end = str.Length;
            }
            // TODO: better error handling if this should be shipped
            string name = str.Substring(pos, end - pos);

            pos = end;
            return name;
        }

        /// <summary>
        /// Throws an 'invalid XmlDiffPath expression' exception.
        /// </summary>
        /// <param name="path">Proprietary alphanumeric path statement</param>
        private static void OnInvalidExpression(string path) {
            throw new Exception("Invalid XmlDiffPath expression: " + path);
        }

        /// <summary>
        /// Throws an 'no matching node' exception.
        /// </summary>
        /// <param name="path">Proprietary alphanumeric path statement</param>
        private static void OnNoMatchingNode(string path) {
            throw new Exception("No matching node:" + path);
        }

        #endregion

    }
}