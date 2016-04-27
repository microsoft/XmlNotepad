//------------------------------------------------------------------------------
// <copyright file="XmlDiffDocument.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Collections;

namespace Microsoft.XmlDiffPatch
{
//////////////////////////////////////////////////////////////////
// XmlDiffDocument
//
internal class XmlDiffDocument : XmlDiffParentNode
{
// Fields
    protected XmlDiff _XmlDiff;
	bool _bLoaded;
    
    // Used at loading only
    XmlDiffNode _curLastChild;
    XmlHash _xmlHash;

// Constructor
	internal XmlDiffDocument( XmlDiff xmlDiff ) : base( 0 )
	{
        _bLoaded = false;
        _XmlDiff = xmlDiff;
    }

// Properties
	internal override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.Document; } }

    internal bool IsFragment {
        get {
            XmlDiffNode node = _firstChildNode;
            while ( node != null && node.NodeType != XmlDiffNodeType.Element ) {
                node = node._nextSibling;
            }
            if ( node == null  ) {
                return true;
            }
            node = node._nextSibling;
            while ( node != null && node.NodeType != XmlDiffNodeType.Element ) {
                node = node._nextSibling;
            }
            return ( node != null );
        }
    }

// Methods
    // computes the hash value of the node and saves it into the _hashValue field
    internal override void ComputeHashValue( XmlHash xmlHash )
    {
        Debug.Assert( _hashValue == 0 );
        _hashValue = xmlHash.ComputeHashXmlDiffDocument( this );
    }

    // compares the node to another one and returns the xmldiff operation for changing this node to the other
	internal override XmlDiffOperation GetDiffOperation( XmlDiffNode changedNode, XmlDiff xmlDiff )
	{
        if ( changedNode.NodeType != XmlDiffNodeType.Document )
            return XmlDiffOperation.Undefined;
        else 
			return XmlDiffOperation.Match;
	}

    // Loads the document from XmlReader
    internal virtual void Load( XmlReader reader, XmlHash xmlHash ) 
    {
        if ( _bLoaded ) 
            throw new InvalidOperationException( "The document already contains data and should not be used again." );

        try 
        {
            _curLastChild = null;
            _xmlHash = xmlHash;

            LoadChildNodes( this, reader, false );

            ComputeHashValue( _xmlHash );
            _bLoaded = true;

    #if DEBUG
            if ( XmlDiff.T_LoadedDoc.Enabled )
            {
                Trace.Write( "\nLoaded document " + reader.BaseURI + ": \n" );
                Dump();
            }
    #endif
        }
        finally
        {
            _xmlHash = null;
        }
    }

    // Loads child nodes of the 'parent' node
    internal void LoadChildNodes ( XmlDiffParentNode parent, XmlReader reader, bool bEmptyElement ) 
    {
        XmlDiffNode savedLastChild = _curLastChild;
        _curLastChild = null;

        // load attributes & namespace nodes
        while ( reader.MoveToNextAttribute() )
        {
            if ( reader.Prefix == "xmlns" )
            {
                if ( !_XmlDiff.IgnoreNamespaces ) 
                {
                    XmlDiffNamespace nsNode = new XmlDiffNamespace( reader.LocalName, reader.Value );
                    nsNode.ComputeHashValue( _xmlHash );
                    InsertAttributeOrNamespace( (XmlDiffElement)parent, nsNode );
                }
            }
            else if ( reader.Prefix == string.Empty  && reader.LocalName == "xmlns" )
            {
                if ( !_XmlDiff.IgnoreNamespaces ) 
                {
                    XmlDiffNamespace nsNode = new XmlDiffNamespace( string.Empty, reader.Value );
                    nsNode.ComputeHashValue( _xmlHash );
                    InsertAttributeOrNamespace( (XmlDiffElement)parent, nsNode );
                }
            }
            else
            {
                string attrValue = _XmlDiff.IgnoreWhitespace ? XmlDiff.NormalizeText( reader.Value ) : reader.Value;
                XmlDiffAttribute attr = new XmlDiffAttribute( reader.LocalName, reader.Prefix, reader.NamespaceURI, attrValue );
                attr.ComputeHashValue( _xmlHash );
                InsertAttributeOrNamespace( (XmlDiffElement)parent, attr );
            }
        }

        // empty element -> return, do not load chilren
        if ( bEmptyElement ) 
            goto End;

        int childPosition = 0;

        // load children
        if ( !reader.Read()) 
            goto End;

        do {
            // ignore whitespaces between nodes
            if ( reader.NodeType == XmlNodeType.Whitespace )
                continue;

            switch ( reader.NodeType ) 
            {
                case XmlNodeType.Element:
                {
                    bool bEmptyEl = reader.IsEmptyElement;
                    XmlDiffElement elem = new XmlDiffElement( ++childPosition, reader.LocalName, reader.Prefix, reader.NamespaceURI );

                    LoadChildNodes( elem, reader, bEmptyEl );

                    elem.ComputeHashValue( _xmlHash );
                    InsertChild( parent, elem );
                    break;
                }
                case XmlNodeType.Attribute:
                {
                    Debug.Assert( false, "We should never get to this point, attributes should be read at the beginning of thid method." );
                    break;
                }
                case XmlNodeType.Text:
                {
                    string textValue = ( _XmlDiff.IgnoreWhitespace ) ? XmlDiff.NormalizeText( reader.Value ) : reader.Value;
                    XmlDiffCharData charDataNode = new XmlDiffCharData( ++childPosition, textValue, XmlDiffNodeType.Text );
                    charDataNode.ComputeHashValue( _xmlHash );
                    InsertChild( parent, charDataNode );
                    break;
                }
                case XmlNodeType.CDATA:
                {
                    XmlDiffCharData charDataNode = new XmlDiffCharData( ++childPosition, reader.Value, XmlDiffNodeType.CDATA );
                    charDataNode.ComputeHashValue( _xmlHash );
                    InsertChild( parent, charDataNode );
                    break;
                }
                case XmlNodeType.EntityReference:
                {
                    XmlDiffER er = new XmlDiffER( ++childPosition, reader.Name );
                    er.ComputeHashValue( _xmlHash );
                    InsertChild( parent, er );
                    break;
                }
                case XmlNodeType.Comment:
                {
                    ++childPosition;
                    if ( !_XmlDiff.IgnoreComments ) 
                    {
                        XmlDiffCharData charDataNode = new XmlDiffCharData( childPosition, reader.Value, XmlDiffNodeType.Comment );
                        charDataNode.ComputeHashValue( _xmlHash );
                        InsertChild( parent, charDataNode );
                    }
                    break;
                }
                case XmlNodeType.ProcessingInstruction:
                {
                    ++childPosition;
                    if ( !_XmlDiff.IgnorePI )
                    {
                        XmlDiffPI pi = new XmlDiffPI( childPosition, reader.Name, reader.Value );
                        pi.ComputeHashValue( _xmlHash );
                        InsertChild( parent, pi );
                    }
                    break;
                }
                case XmlNodeType.SignificantWhitespace:
                {
                    if( reader.XmlSpace == XmlSpace.Preserve )
                    {
                        ++childPosition;
                        if (!_XmlDiff.IgnoreWhitespace ) 
                        {
                            XmlDiffCharData charDataNode = new XmlDiffCharData( childPosition, reader.Value, XmlDiffNodeType.SignificantWhitespace );
                            charDataNode.ComputeHashValue( _xmlHash );
                            InsertChild( parent, charDataNode );
                        }
                    }
                    break;
                }
                case XmlNodeType.XmlDeclaration:
                {
                    ++childPosition;
                    if ( !_XmlDiff.IgnoreXmlDecl ) 
                    {
                        XmlDiffXmlDeclaration xmlDecl = new XmlDiffXmlDeclaration( childPosition, XmlDiff.NormalizeXmlDeclaration( reader.Value ) );
                        xmlDecl.ComputeHashValue( _xmlHash );
						InsertChild( parent, xmlDecl );
                    }
                    break;
                }
                case XmlNodeType.EndElement:
                    goto End;

                case XmlNodeType.DocumentType:
                    childPosition++;
                    if ( !_XmlDiff.IgnoreDtd ) {
                        
                        XmlDiffDocumentType docType = new XmlDiffDocumentType( childPosition, 
                                                                               reader.Name,
                                                                               reader.GetAttribute("PUBLIC"),
                                                                               reader.GetAttribute("SYSTEM"),
                                                                               reader.Value );
                        docType.ComputeHashValue( _xmlHash );
                        InsertChild( parent, docType );
                    }
                    break;

                default:
                    Debug.Assert( false );
                    break;
            }
        } while ( reader.Read() );

    End:
        _curLastChild = savedLastChild;
    }

    // Loads the document from XmlNode
    internal virtual void Load( XmlNode node, XmlHash xmlHash ) 
    {
        if ( _bLoaded ) 
            throw new InvalidOperationException( "The document already contains data and should not be used again." );

        if ( node.NodeType == XmlNodeType.Attribute ||
             node.NodeType == XmlNodeType.Entity || 
             node.NodeType == XmlNodeType.Notation || 
             node.NodeType == XmlNodeType.Whitespace ) {
            throw new ArgumentException( "Invalid node type." );
        }

        try 
        {
            _curLastChild = null;
            _xmlHash = xmlHash;

            if ( node.NodeType == XmlNodeType.Document || node.NodeType == XmlNodeType.DocumentFragment ) {
                LoadChildNodes( this, node );
                ComputeHashValue( _xmlHash );
            }
            else {
                int childPos = 0;
                XmlDiffNode rootNode = LoadNode( node, ref childPos );
                if ( rootNode != null ) {
                    InsertChildNodeAfter( null, rootNode );
                    _hashValue = rootNode.HashValue;
                }
            }
            _bLoaded = true;

    #if DEBUG
            if ( XmlDiff.T_LoadedDoc.Enabled )
            {
                Trace.Write( "\nLoaded document " + node.BaseURI + ": \n" );
                Dump();
            }
    #endif
        }
        finally
        {
            _xmlHash = null;
        }
    }

    internal XmlDiffNode LoadNode( XmlNode node, ref int childPosition ) {
        switch ( node.NodeType ) 
        {
            case XmlNodeType.Element:
                XmlDiffElement elem = new XmlDiffElement( ++childPosition, node.LocalName, node.Prefix, node.NamespaceURI );
                LoadChildNodes( elem, node );
                elem.ComputeHashValue( _xmlHash );
                return elem;

            case XmlNodeType.Attribute:
                Debug.Assert( false, "Attributes cannot be loaded by this method." );
                return null;

            case XmlNodeType.Text:
                string textValue = ( _XmlDiff.IgnoreWhitespace ) ? XmlDiff.NormalizeText( node.Value ) : node.Value;
                XmlDiffCharData text = new XmlDiffCharData( ++childPosition, textValue, XmlDiffNodeType.Text );
                text.ComputeHashValue( _xmlHash );
                return text;

            case XmlNodeType.CDATA:
                XmlDiffCharData cdata = new XmlDiffCharData( ++childPosition, node.Value, XmlDiffNodeType.CDATA );
                cdata.ComputeHashValue( _xmlHash );
                return cdata;

            case XmlNodeType.EntityReference:
                XmlDiffER er = new XmlDiffER( ++childPosition, node.Name );
                er.ComputeHashValue( _xmlHash );
                return er;

            case XmlNodeType.Comment:
                ++childPosition;
                if ( _XmlDiff.IgnoreComments ) 
                    return null;

                XmlDiffCharData comment = new XmlDiffCharData( childPosition, node.Value, XmlDiffNodeType.Comment );
                comment.ComputeHashValue( _xmlHash );
                return comment;

            case XmlNodeType.ProcessingInstruction:
                ++childPosition;
                if ( _XmlDiff.IgnorePI )
                    return null;

                XmlDiffPI pi = new XmlDiffPI( childPosition, node.Name, node.Value );
                pi.ComputeHashValue( _xmlHash );
                return pi;

            case XmlNodeType.SignificantWhitespace:
                ++childPosition;
                if ( _XmlDiff.IgnoreWhitespace ) 
                    return null;
                XmlDiffCharData ws = new XmlDiffCharData( childPosition, node.Value, XmlDiffNodeType.SignificantWhitespace );
                ws.ComputeHashValue( _xmlHash );
                return ws;

            case XmlNodeType.XmlDeclaration:
                ++childPosition;
                if ( _XmlDiff.IgnoreXmlDecl ) 
                    return null;
                XmlDiffXmlDeclaration xmlDecl = new XmlDiffXmlDeclaration( childPosition, XmlDiff.NormalizeXmlDeclaration( node.Value ) );
                xmlDecl.ComputeHashValue( _xmlHash );
				return xmlDecl;

            case XmlNodeType.EndElement:
                return null;

            case XmlNodeType.DocumentType:
                childPosition++;
                if ( _XmlDiff.IgnoreDtd )
                    return null;

                XmlDocumentType docType = (XmlDocumentType)node;
                XmlDiffDocumentType diffDocType = new XmlDiffDocumentType( childPosition, docType.Name, docType.PublicId, docType.SystemId, docType.InternalSubset );
                diffDocType.ComputeHashValue( _xmlHash );
                return diffDocType;

            default:
                Debug.Assert( false );
                return null;
        }
    }

    // Loads child nodes of the 'parent' node
    internal void LoadChildNodes( XmlDiffParentNode parent, XmlNode parentDomNode ) 
    {
        XmlDiffNode savedLastChild = _curLastChild;
        _curLastChild = null;

        // load attributes & namespace nodes
        XmlNamedNodeMap attribs = parentDomNode.Attributes;
        if ( attribs != null && attribs.Count > 0 ) 
        {
            IEnumerator attrEnum = attribs.GetEnumerator();
            while ( attrEnum.MoveNext() ) 
            {
                XmlAttribute attr = (XmlAttribute)attrEnum.Current;
                if ( attr.Prefix == "xmlns" )
                {
                    if ( !_XmlDiff.IgnoreNamespaces ) 
                    {
                        XmlDiffNamespace nsNode = new XmlDiffNamespace( attr.LocalName, attr.Value );
                        nsNode.ComputeHashValue( _xmlHash );
                        InsertAttributeOrNamespace( (XmlDiffElement)parent, nsNode );
                    }
                }
                else if ( attr.Prefix == string.Empty && attr.LocalName == "xmlns" )
                {
                    if ( !_XmlDiff.IgnoreNamespaces ) 
                    {
                        XmlDiffNamespace nsNode = new XmlDiffNamespace( string.Empty, attr.Value );
                        nsNode.ComputeHashValue( _xmlHash );
                        InsertAttributeOrNamespace( (XmlDiffElement)parent, nsNode );
                    }
                }
                else
                {
                    string attrValue = _XmlDiff.IgnoreWhitespace ? XmlDiff.NormalizeText( attr.Value ) : attr.Value;
                    XmlDiffAttribute newAttr = new XmlDiffAttribute( attr.LocalName, attr.Prefix, attr.NamespaceURI, attrValue );
                    newAttr.ComputeHashValue( _xmlHash );
                    InsertAttributeOrNamespace( (XmlDiffElement)parent, newAttr );
                }
            }
        }

        // load children
        XmlNodeList children = parentDomNode.ChildNodes;
        if ( children.Count == 0 )
            goto End;

        int childPosition = 0;
        IEnumerator childEnum = children.GetEnumerator();
        while ( childEnum.MoveNext() )
        {
            XmlNode node = (XmlNode)childEnum.Current;

            // ignore whitespaces between nodes
            if ( node.NodeType == XmlNodeType.Whitespace )
                continue;

            XmlDiffNode newDiffNode = LoadNode( (XmlNode)childEnum.Current, ref childPosition );
            if ( newDiffNode != null )
                InsertChild( parent, newDiffNode );
        }

        End:
        _curLastChild = savedLastChild;
    }
    
    // Inserts the 'newChild' node. If child order is significant, the new child is
    // inserted at the end of all child nodes. If the child order is not signoficant,
    // the new node is sorted into the other child nodes.
    private void InsertChild( XmlDiffParentNode parent, XmlDiffNode newChild ) 
    {
        if ( _XmlDiff.IgnoreChildOrder ) 
        {
            XmlDiffNode curChild = parent.FirstChildNode;
			XmlDiffNode prevChild = null;

            while ( curChild != null && ( OrderChildren( curChild, newChild ) <= 0 ) ) 
            {
                prevChild = curChild;
                curChild = curChild._nextSibling;
            }
            parent.InsertChildNodeAfter( prevChild, newChild );
        }
        else
        {
            parent.InsertChildNodeAfter( _curLastChild, newChild );
            _curLastChild = newChild;
        }
    }

    // Inserts an attribute or namespace node. The new node is sorted into the other attributes/namespace nodes.
    private void InsertAttributeOrNamespace( XmlDiffElement element, XmlDiffAttributeOrNamespace newAttrOrNs ) 
    {
        element.InsertAttributeOrNamespace( newAttrOrNs );
    }

    // Compares the two nodes. Used for sorting of the child nodes when the child order is not significant.
    static internal int OrderChildren( XmlDiffNode node1, XmlDiffNode node2 )
    {
        Debug.Assert( node1 != null && node2 != null );

        int nt1 = (int) node1.NodeType;
        int nt2 = (int) node2.NodeType;

        if ( nt1 < nt2) 
            return -1;
        
        if ( nt2 < nt1 ) 
            return 1;

        // now nt1 == nt2
        switch ( nt1 )
        {
            case (int) XmlDiffNodeType.Element:
                return OrderElements( node1 as XmlDiffElement, node2 as XmlDiffElement );
            case (int) XmlDiffNodeType.Attribute:
            case (int) XmlDiffNodeType.Namespace:
                Debug.Assert( false, "We should never get to this point" );
                return 0;
            case (int) XmlDiffNodeType.EntityReference:
                return OrderERs( node1 as XmlDiffER, node2 as XmlDiffER );
            case (int) XmlDiffNodeType.ProcessingInstruction:
                return OrderPIs( node1 as XmlDiffPI, node2 as XmlDiffPI );
            case (int) XmlDiffNodeType.ShrankNode:
                if ( ((XmlDiffShrankNode)node1).MatchingShrankNode == ((XmlDiffShrankNode)node2).MatchingShrankNode ) {
                    return 0;
                }
                else {
                    return ( ((XmlDiffShrankNode)node1).HashValue < ((XmlDiffShrankNode)node2).HashValue ) ? -1 : 1;
                }
            default:
                Debug.Assert ( node1 is XmlDiffCharData );
                return OrderCharacterData( node1 as XmlDiffCharData, node2 as XmlDiffCharData );
        }
    }

    static internal int OrderElements( XmlDiffElement elem1, XmlDiffElement elem2 ) 
    {
        Debug.Assert( elem1 != null && elem2 != null );

        int nCompare;
        if ( ( nCompare = OrderStrings( elem1.LocalName, elem2.LocalName ) ) == 0  &&
             ( nCompare = OrderStrings( elem1.NamespaceURI, elem2.NamespaceURI ) ) == 0 )
        {
            return OrderSubTrees( elem1, elem2 );
        }
        return nCompare;
    }


   static internal int OrderAttributesOrNamespaces( XmlDiffAttributeOrNamespace node1, 
                                                    XmlDiffAttributeOrNamespace node2 ) 
   {
        Debug.Assert( node1 != null && node2 != null );

	    if ( node1.NodeType != node2.NodeType )
		{
			if ( node1.NodeType == XmlDiffNodeType.Namespace )
				return -1;
			else
				return 1;
		}

        int nCompare;
        if ( ( nCompare = OrderStrings( node1.LocalName, node2.LocalName ) ) == 0  &&
			 ( nCompare = OrderStrings( node1.Prefix, node2.Prefix ) ) == 0  && 
             ( nCompare = OrderStrings( node1.NamespaceURI, node2.NamespaceURI ) ) == 0 &&
             ( nCompare = OrderStrings( node1.Value, node2.Value ) ) == 0 )
        {
            return 0;
        }
        return nCompare;
    }

    static internal int OrderERs( XmlDiffER er1, XmlDiffER er2 ) 
    {
        Debug.Assert( er1 != null && er2 != null );
        return OrderStrings( er1.Name, er2.Name );
    }

    static internal int OrderPIs( XmlDiffPI pi1, XmlDiffPI pi2 ) 
    {
        Debug.Assert( pi1 != null && pi2 != null );

        int nCompare = 0;
        if ( ( nCompare = OrderStrings( pi1.Name, pi2.Name ) ) == 0  && 
             ( nCompare = OrderStrings( pi1.Value, pi2.Value ) ) == 0 ) 
        {
            return 0;
        }
        return nCompare;
    }

    static internal int OrderCharacterData( XmlDiffCharData t1, XmlDiffCharData t2 ) 
    {
        Debug.Assert( t1 != null && t2 != null );
        return OrderStrings( t1.Value, t2.Value );
    }

    // returns 0 if the same string; 1 if s1 > s2 and -1 if s1 < s2
    static internal int OrderStrings( string s1, string s2 ) 
    {
        int len = ( s1.Length < s2.Length ) ? s1.Length : s2.Length;
        int i = 0;

        while ( i < len  &&  s1[i] == s2[i] ) i++;
        if ( i < len ) 
            return ( s1[i] < s2[i]) ? -1 : 1;
        else 
            if ( s1.Length == s2.Length )
                return 0;
            else
                return ( s2.Length > s1.Length) ? -1 : 1;
    }

    static internal int OrderSubTrees( XmlDiffElement elem1, XmlDiffElement elem2 ) 
    {
        Debug.Assert( elem1 != null && elem2 != null );

        int nCompare = 0; 

        // attributes - ignore namespace nodes
        XmlDiffAttributeOrNamespace curAttr1 = elem1._attributes;
        XmlDiffAttributeOrNamespace curAttr2 = elem2._attributes;

        while ( curAttr1 != null && curAttr1.NodeType == XmlDiffNodeType.Namespace )
            curAttr1 = (XmlDiffAttributeOrNamespace)curAttr1._nextSibling;
        while ( curAttr2 != null && curAttr2.NodeType == XmlDiffNodeType.Namespace )
            curAttr2 = (XmlDiffAttributeOrNamespace)curAttr2._nextSibling;
        
        while ( curAttr1 != null && curAttr2 != null ) {
            if ( ( nCompare = OrderAttributesOrNamespaces( curAttr1, curAttr2 ) ) != 0 )
                return nCompare;
            curAttr1 = (XmlDiffAttributeOrNamespace)curAttr1._nextSibling;
            curAttr2 = (XmlDiffAttributeOrNamespace)curAttr2._nextSibling;
        }

        // children
        if ( curAttr1 == curAttr2 ) {
            XmlDiffNode curChild1 = elem1.FirstChildNode;
            XmlDiffNode curChild2 = elem2.FirstChildNode;

            while ( curChild1 != null && curChild2 != null ) 
            {
                if ( ( nCompare = OrderChildren( curChild1, curChild2 ) ) != 0 )
                    return nCompare;

                curChild1 = curChild1._nextSibling;
                curChild2 = curChild2._nextSibling;
            }

            if ( curChild1 == curChild2 ) 
                return 0;
            else if ( curChild1 ==  null ) 
                return 1; //elem2 > elem1;
            else 
                return -1; //elem1 > elem1;
        }
        else if ( curAttr1 == null )
            return 1; //elem2 > elem1;
        else {
            return -1; //elem1 > elem1;
        }
    }

    internal override void WriteTo( XmlWriter w ) 
    {
        WriteContentTo( w );
    }

    internal override void WriteContentTo( XmlWriter w ) 
    {
        XmlDiffNode child = FirstChildNode;
        while ( child != null ) 
        {
            child.WriteTo( w );
            child = child._nextSibling;
        }
    }

#if DEBUG
    private void Dump( )
    {
        Dump( "- " );
    }

    internal override void Dump( string indent )
    {
        DumpChildren( indent );
    }
#endif
}

}
