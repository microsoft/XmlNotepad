//------------------------------------------------------------------------------
// <copyright file="XmlHash.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.Xml;
using System.Diagnostics;

namespace Microsoft.XmlDiffPatch
{

internal class XmlHash
{
// Fields
    bool _bIgnoreChildOrder = false;
    bool _bIgnoreComments = false;
    bool _bIgnorePI = false;
    bool _bIgnoreWhitespace = false;
    bool _bIgnoreNamespaces = false;
    bool _bIgnorePrefixes = false;
    bool _bIgnoreXmlDecl = false;
    bool _bIgnoreDtd = false;

    const string Delimiter = "\0x01";

// Constructor

	internal XmlHash( XmlDiff xmlDiff )
	{
        // set flags
        _bIgnoreChildOrder  = xmlDiff.IgnoreChildOrder;
        _bIgnoreComments    = xmlDiff.IgnoreComments;
        _bIgnorePI          = xmlDiff.IgnorePI;
        _bIgnoreWhitespace  = xmlDiff.IgnoreWhitespace;
        _bIgnoreNamespaces  = xmlDiff.IgnoreNamespaces;
        _bIgnorePrefixes    = xmlDiff.IgnorePrefixes;
        _bIgnoreXmlDecl     = xmlDiff.IgnoreXmlDecl;
        _bIgnoreDtd         = xmlDiff.IgnoreDtd;
	}

	internal XmlHash()
	{
	}

// Methods

    private void ClearFlags()
    {
        _bIgnoreChildOrder  = false;
        _bIgnoreComments    = false;
        _bIgnorePI          = false;
        _bIgnoreWhitespace  = false;
        _bIgnoreNamespaces  = false;
        _bIgnorePrefixes    = false;
        _bIgnoreXmlDecl     = false;
        _bIgnoreDtd         = false;
     }

    internal ulong ComputeHash( XmlNode node, XmlDiffOptions options )
    {
        _bIgnoreChildOrder = ( ( (int)options & (int)(XmlDiffOptions.IgnoreChildOrder) ) > 0 ) ;
        _bIgnoreComments   = ( ( (int)options & (int)(XmlDiffOptions.IgnoreComments)   ) > 0 ) ;
        _bIgnorePI         = ( ( (int)options & (int)(XmlDiffOptions.IgnorePI)         ) > 0 ) ;
        _bIgnoreWhitespace = ( ( (int)options & (int)(XmlDiffOptions.IgnoreWhitespace) ) > 0 ) ;
        _bIgnoreNamespaces = ( ( (int)options & (int)(XmlDiffOptions.IgnoreNamespaces) ) > 0 ) ;
        _bIgnorePrefixes   = ( ( (int)options & (int)(XmlDiffOptions.IgnorePrefixes)   ) > 0 ) ;
        _bIgnoreXmlDecl    = ( ( (int)options & (int)(XmlDiffOptions.IgnoreXmlDecl)    ) > 0 ) ;
        _bIgnoreDtd        = ( ( (int)options & (int)(XmlDiffOptions.IgnoreDtd)        ) > 0 ) ;

        return ComputeHash( node );
    }

    internal ulong ComputeHash( XmlNode node)
    {
        switch ( node.NodeType )
        {
            case XmlNodeType.Document:
                return ComputeHashXmlDocument( (XmlDocument)node );
            case XmlNodeType.DocumentFragment:
                return ComputeHashXmlFragment( (XmlDocumentFragment)node );
            default:
                return ComputeHashXmlNode( node );
        }
    }

    private ulong ComputeHashXmlDocument( XmlDocument doc )
    {
        HashAlgorithm ha = new HashAlgorithm();
        HashDocument( ha );
        ComputeHashXmlChildren( ha, doc );
        return ha.Hash;
    }

    private ulong ComputeHashXmlFragment( XmlDocumentFragment frag )
    {
        HashAlgorithm ha = new HashAlgorithm();
        ComputeHashXmlChildren( ha, frag );
        return ha.Hash;
    }

    internal ulong ComputeHashXmlDiffDocument( XmlDiffDocument doc )
    {
        HashAlgorithm ha = new HashAlgorithm();
        HashDocument( ha );
        ComputeHashXmlDiffChildren( ha, doc );
        return ha.Hash;
    }

    internal ulong ComputeHashXmlDiffElement( XmlDiffElement el )
    {
        HashAlgorithm ha = new HashAlgorithm();
        HashElement( ha, el.LocalName, el.Prefix, el.NamespaceURI );
        ComputeHashXmlDiffAttributes( ha, el );
        ComputeHashXmlDiffChildren( ha, el );
        return ha.Hash;
    }

    private void ComputeHashXmlDiffAttributes( HashAlgorithm ha, XmlDiffElement el )
    {
        int attrCount = 0;
        ulong attrHashAll = 0;
        XmlDiffAttributeOrNamespace curAttrOrNs = el._attributes;
        while ( curAttrOrNs != null )
        {
            attrHashAll += curAttrOrNs.HashValue;
            attrCount++;
            curAttrOrNs = (XmlDiffAttributeOrNamespace)curAttrOrNs._nextSibling;
        }

        if ( attrCount > 0 ) 
        {
            ha.AddULong( attrHashAll );
            ha.AddInt( attrCount );
        }
    }

    private void ComputeHashXmlDiffChildren( HashAlgorithm ha, XmlDiffParentNode parent )
    {
        int childrenCount = 0;
		if ( _bIgnoreChildOrder )
		{
			ulong totalHash = 0;
            XmlDiffNode curChild = parent.FirstChildNode;
			while ( curChild != null )
			{
                Debug.Assert( !( curChild is XmlDiffAttributeOrNamespace ) );
				Debug.Assert ( curChild.HashValue != 0 );

				totalHash += curChild.HashValue;
				childrenCount++;
				curChild = curChild._nextSibling;
			}
			ha.AddULong( totalHash );
		}
		else
		{
            XmlDiffNode curChild = parent.FirstChildNode;
			while ( curChild != null )
			{
                Debug.Assert( !( curChild is XmlDiffAttributeOrNamespace ) );
				Debug.Assert ( curChild.HashValue != 0 );

				ha.AddULong( curChild.HashValue );
				childrenCount++;
				curChild = curChild._nextSibling;
			}
		}

        if ( childrenCount != 0 )
            ha.AddInt( childrenCount );
    }

    private void ComputeHashXmlChildren( HashAlgorithm ha, XmlNode parent )
    {
        XmlElement el = parent as XmlElement;
        if ( el != null )
        {
            ulong attrHashSum = 0;
            int attrsCount = 0;
            XmlAttributeCollection attrs = ((XmlElement)parent).Attributes;
            for ( int i = 0; i < attrs.Count; i++ )
            {
                XmlAttribute attr = (XmlAttribute)attrs.Item(i);

                ulong hashValue = 0;

                // default namespace def
                if ( attr.LocalName == "xmlns" && attr.Prefix == string.Empty ) {
                    if ( _bIgnoreNamespaces ) {
                        continue;
                    }
                    hashValue = HashNamespace( string.Empty, attr.Value );
                }
                // namespace def
                else if ( attr.Prefix == "xmlns" ) {
                    if ( _bIgnoreNamespaces ) {
                        continue;
                    }
                    hashValue = HashNamespace( attr.LocalName, attr.Value );
                }
                // attribute
                else {
                    if ( _bIgnoreWhitespace )
                        hashValue = HashAttribute( attr.LocalName, attr.Prefix, attr.NamespaceURI, XmlDiff.NormalizeText( attr.Value ) );
                    else
                        hashValue = HashAttribute( attr.LocalName, attr.Prefix, attr.NamespaceURI, attr.Value );
                }

                Debug.Assert( hashValue != 0 );

                attrsCount++;
                attrHashSum += hashValue;
            }

            if ( attrsCount != 0 )
            {
                ha.AddULong( attrHashSum );
                ha.AddInt( attrsCount );
            }
        }

		int childrenCount = 0;
		if ( _bIgnoreChildOrder )
		{
			ulong totalHashSum = 0;
			XmlNode curChild = parent.FirstChild;
			while ( curChild != null )
			{
				ulong hashValue = ComputeHashXmlNode( curChild );
				if ( hashValue != 0 )
				{
					totalHashSum += hashValue;
					childrenCount++;
				}
				curChild = curChild.NextSibling;
			}
			ha.AddULong( totalHashSum );
		}
		else
		{
			XmlNode curChild = parent.FirstChild;
			while ( curChild != null )
			{
				ulong hashValue = ComputeHashXmlNode( curChild );
				if ( hashValue != 0 )
				{
					ha.AddULong( hashValue );
					childrenCount++;
				}
				curChild = curChild.NextSibling;
			}
		}
        if ( childrenCount != 0 )
            ha.AddInt( childrenCount );
    }

    private ulong ComputeHashXmlNode( XmlNode node )
    {
        switch ( node.NodeType )
        {
            case XmlNodeType.Element:
            {
                XmlElement el = (XmlElement)node;
                HashAlgorithm ha = new HashAlgorithm();

                HashElement( ha, el.LocalName, el.Prefix, el.NamespaceURI );
                ComputeHashXmlChildren( ha, el );

                return ha.Hash;
            }
            case XmlNodeType.Attribute:
                // attributes are hashed in ComputeHashXmlChildren;
                Debug.Assert( false );
                return 0;

            case XmlNodeType.Whitespace:
                return 0;

            case XmlNodeType.SignificantWhitespace:
                if ( !_bIgnoreWhitespace )
                    goto case XmlNodeType.Text;
                return 0;
            case XmlNodeType.Comment:
                if ( !_bIgnoreComments )
                    return HashCharacterNode( XmlNodeType.Comment, ((XmlCharacterData)node).Value );
                return 0;
            case XmlNodeType.Text:
            {
                XmlCharacterData cd = (XmlCharacterData)node;
                if ( _bIgnoreWhitespace )
                    return HashCharacterNode( cd.NodeType, XmlDiff.NormalizeText( cd.Value ) );
                else
                    return HashCharacterNode( cd.NodeType, cd.Value );
            }
            case XmlNodeType.CDATA:
            {
                XmlCharacterData cd = (XmlCharacterData)node;
                return HashCharacterNode( cd.NodeType, cd.Value );
            }
            case XmlNodeType.ProcessingInstruction:
            {
                if ( _bIgnorePI )
                    return 0;

                XmlProcessingInstruction pi = (XmlProcessingInstruction)node;
                return HashPI( pi.Target, pi.Value );
            }
            case XmlNodeType.EntityReference:
            {
                XmlEntityReference er = (XmlEntityReference)node;
                return HashER( er.Name );
            }
            case XmlNodeType.XmlDeclaration:
            {
                if ( _bIgnoreXmlDecl )
                    return 0;
                XmlDeclaration decl = (XmlDeclaration)node;
                return HashXmlDeclaration( XmlDiff.NormalizeXmlDeclaration( decl.Value ) );
            }
            case XmlNodeType.DocumentType:
            {
                if ( _bIgnoreDtd )
                    return 0;
                XmlDocumentType docType = (XmlDocumentType)node;
                return HashDocumentType( docType.Name, docType.PublicId, docType.SystemId, docType.InternalSubset );
            }
            case XmlNodeType.DocumentFragment:
                return 0;
            default:
                Debug.Assert( false );
                return 0;
        }
    }

    private void HashDocument( HashAlgorithm ha )
    {
        // Intentionally empty
    }

    internal void HashElement( HashAlgorithm ha, string localName, string prefix, string ns )
    {
        ha.AddString( (int)(XmlNodeType.Element) + 
                      Delimiter +
                      ( ( _bIgnoreNamespaces || _bIgnorePrefixes ) ? string.Empty : prefix ) +
                      Delimiter +
                      ( _bIgnoreNamespaces ? string.Empty : ns ) +
                      Delimiter +
                      localName );
    }

    internal ulong HashAttribute( string localName, string prefix, string ns, string value )
    {
        return HashAlgorithm.GetHash( (int)XmlNodeType.Attribute + 
                                      Delimiter +
                                      ( ( _bIgnoreNamespaces || _bIgnorePrefixes ) ? string.Empty : prefix ) +
                                      Delimiter +
                                      ( _bIgnoreNamespaces ? string.Empty : ns ) +
                                      Delimiter +
                                      localName +
                                      Delimiter +
                                      value );
    }

    internal ulong HashNamespace( string prefix, string ns )
    {
        Debug.Assert( !_bIgnoreNamespaces );

        return HashAlgorithm.GetHash( (int)XmlDiffNodeType.Namespace +
                                      Delimiter +
                                      ( _bIgnorePrefixes ? string.Empty : prefix ) +
                                      Delimiter +
                                      ns );
    }

    internal ulong HashCharacterNode( XmlNodeType nodeType, string value )
    {
        return HashAlgorithm.GetHash( ((int)nodeType).ToString() + 
                                      Delimiter +
                                      value );
    }

    internal ulong HashPI( string target, string value )
    {
        return HashAlgorithm.GetHash( ((int)XmlNodeType.ProcessingInstruction).ToString() + 
                                      Delimiter +
                                      target +
                                      Delimiter +
                                      value );
    }

    internal ulong HashER( string name )
    {
        return HashAlgorithm.GetHash( ((int)XmlNodeType.EntityReference).ToString() + 
                                      Delimiter +
                                       name );
    }

    internal ulong HashXmlDeclaration( string value )
    {
        return HashAlgorithm.GetHash( ((int)XmlNodeType.XmlDeclaration).ToString() +
                                      Delimiter +
                                       value );
    }

    internal ulong HashDocumentType( string name, string publicId, string systemId, string subset )
    {
        return HashAlgorithm.GetHash( ((int)XmlNodeType.DocumentType).ToString() +
                                      Delimiter +
                                      name +
                                      Delimiter +
                                      publicId + 
                                      Delimiter + 
                                      systemId + 
                                      Delimiter +
                                      subset );
    }
}
}
