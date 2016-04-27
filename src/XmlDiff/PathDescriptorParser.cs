//------------------------------------------------------------------------------
// <copyright file="PathDescriptorParser.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.Xml;
using System.Diagnostics;
using System.Collections;

namespace Microsoft.XmlDiffPatch
{
	internal class PathDescriptorParser
	{
        static char[] Delimiters = new char[] {'|','-','/'};
        static char[] MultiNodesDelimiters = new char[] {'|','-'};

        internal static XmlNodeList SelectNodes( XmlNode rootNode, XmlNode currentParentNode, string pathDescriptor )
        {
            switch ( pathDescriptor[0] )
            {
                case '/':
                    return SelectAbsoluteNodes( rootNode, pathDescriptor );
                case '@':
                    if ( pathDescriptor.Length < 2 )
                        OnInvalidExpression( pathDescriptor );
                    if ( pathDescriptor[1] == '*' )
                        return SelectAllAttributes( currentParentNode );
                    else
                        return SelectAttributes( currentParentNode, pathDescriptor );
                case '*':
                    if ( pathDescriptor.Length == 1 )
                        return SelectAllChildren( currentParentNode );
                    else 
                    {
                        OnInvalidExpression( pathDescriptor );
                        return null;
                    }
                default:
                    return SelectChildNodes( currentParentNode, pathDescriptor, 0 );
            }
        }

        private static XmlNodeList SelectAbsoluteNodes( XmlNode rootNode, string path )
        {
            Debug.Assert( path[0] == '/' );
            
            int pos = 1;
            XmlNode node = rootNode;

            for (;;)
            {
                int startPos = pos;
                XmlNodeList childNodes = node.ChildNodes;

                int nodePos = ReadPosition( path, ref pos );
                
                if ( pos == path.Length || path[pos] == '/' ) {
                    if ( childNodes.Count == 0 || nodePos < 0 || nodePos > childNodes.Count )
                        OnNoMatchingNode( path );

                    node = childNodes.Item( nodePos - 1 );

                    if ( pos == path.Length ) {
                        XmlPatchNodeList list = new SingleNodeList();
                        list.AddNode( node );
                        return list;
                    }
                    pos++;
                }
                else {
                    if ( path[pos] == '-' || path[pos] == '|' ) {
                        return SelectChildNodes( node, path, startPos );
                    }
                    OnInvalidExpression( path );
                }
            }
        }

        private static XmlNodeList SelectAllAttributes( XmlNode parentNode )
        {
            XmlAttributeCollection attributes = parentNode.Attributes;

            if ( attributes.Count == 0 ) 
            {
                OnNoMatchingNode( "@*" );
                return null;
            }
            else if ( attributes.Count == 1 ) 
            {
                XmlPatchNodeList nodeList = new SingleNodeList();
                nodeList.AddNode( attributes.Item( 0 ) );
                return nodeList;
            }
            else 
            {
                IEnumerator enumerator = attributes.GetEnumerator();
                XmlPatchNodeList nodeList = new MultiNodeList();
                while ( enumerator.MoveNext() )
                    nodeList.AddNode( (XmlNode) enumerator.Current );
                return nodeList;
            }
        }

        private static XmlNodeList SelectAttributes( XmlNode parentNode, string path )
        {
            Debug.Assert( path[0] == '@' );

            int pos = 1;
            XmlAttributeCollection attributes = parentNode.Attributes;
            XmlPatchNodeList nodeList = null;
            for (;;) 
            {
                string name = ReadAttrName( path, ref pos );

                if ( nodeList == null ) 
                {
                    if ( pos == path.Length ) 
                        nodeList = new SingleNodeList();
                    else
                        nodeList = new MultiNodeList();
                }

                XmlNode attr = attributes.GetNamedItem( name );
                if ( attr == null )
                    OnNoMatchingNode( path );

                nodeList.AddNode( attr );

                if ( pos == path.Length )
                    break;
                else if ( path[pos] == '|' ) {
                    pos++;
                    if ( path[pos] != '@' )
                        OnInvalidExpression( path );
                    pos++;
                }
                else
                    OnInvalidExpression( path );
            }

            return nodeList;
        }

        private static XmlNodeList SelectAllChildren( XmlNode parentNode )
        {
            XmlNodeList children = parentNode.ChildNodes;

            if ( children.Count == 0 ) 
            {
                OnNoMatchingNode( "*" );
                return null;
            }
            else if ( children.Count == 1 ) 
            {
                XmlPatchNodeList nodeList = new SingleNodeList();
                nodeList.AddNode( children.Item( 0 ) );
                return nodeList;
            }
            else 
            {
                IEnumerator enumerator = children.GetEnumerator();
                XmlPatchNodeList nodeList = new MultiNodeList();
                while ( enumerator.MoveNext() )
                    nodeList.AddNode( (XmlNode) enumerator.Current );
                return nodeList;
            }   
        }

        private static XmlNodeList SelectChildNodes( XmlNode parentNode, string path, int startPos )
        {
            int pos = startPos;
            XmlPatchNodeList nodeList = null;
            XmlNodeList children = parentNode.ChildNodes;

            int nodePos = ReadPosition( path, ref pos );

            if ( pos == path.Length ) 
                nodeList = new SingleNodeList();
            else
                nodeList = new MultiNodeList();

            for (;;)
            {
                if ( nodePos <= 0 || nodePos > children.Count )
                    OnNoMatchingNode( path );

                XmlNode node = children.Item( nodePos - 1 );
                nodeList.AddNode( node );

                if ( pos == path.Length )
                    break;
                else if ( path[pos] == '|' )
                    pos++;
                else if ( path[pos] == '-' )
                {
                    pos++;
                    int endNodePos = ReadPosition( path, ref pos );
                    if ( endNodePos <= 0 || endNodePos > children.Count )
                        OnNoMatchingNode( path );

                    while ( nodePos < endNodePos )
                    {
                        nodePos++;
                        node = node.NextSibling;
                        nodeList.AddNode( node );
                    }

                    Debug.Assert( (object)node == (object)children.Item( endNodePos - 1 ) );

                    if ( pos == path.Length )
                        break;
                    else if ( path[pos] == '|' )
                        pos++;
                    else 
                        OnInvalidExpression( path );
                }

                nodePos = ReadPosition( path, ref pos );
            }
            return nodeList;
        }

        private static int ReadPosition( string str, ref int pos ) 
        {
            int end = str.IndexOfAny( Delimiters, pos );
            if ( end < 0 )
                end = str.Length;
            
            // TODO: better error handling if this should be shipped
            int nodePos = int.Parse( str.Substring( pos, end - pos ) );

            pos = end;
            return nodePos;
        }

        private static string ReadAttrName( string str, ref int pos ) 
        {
            int end = str.IndexOf( '|', pos );
            if ( end < 0 )
                end = str.Length;
            
            // TODO: better error handling if this should be shipped
            string name = str.Substring( pos, end - pos );

            pos = end;
            return name;
        }

        private static void OnInvalidExpression( string path )
        {
            XmlPatchError.Error( XmlPatchError.InvalidPathDescriptor, path );
        }

        private static void OnNoMatchingNode( string path )
        {
            XmlPatchError.Error( XmlPatchError.NoMatchingNode, path );
        }
	}
}
