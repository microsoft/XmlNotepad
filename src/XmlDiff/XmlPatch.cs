//------------------------------------------------------------------------------
// <copyright file="XmlPatch.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Diagnostics;
using Microsoft.XmlDiffPatch;

namespace Microsoft.XmlDiffPatch
{

//////////////////////////////////////////////////////////////////
// XmlPatch
//
/// <include file='doc\XmlPatch.uex' path='docs/doc[@for="XmlPatch"]/*' />
/// <summary>
///    XML Patch modifies XML documents or nodes according to the XDL diffgram created by XML Diff.  
/// </summary>
public class XmlPatch
{
// Fields
    XmlNode _sourceRootNode;
    bool   _ignoreChildOrder;

// Constructor
	public XmlPatch()
	{
	}

// Methods
    /// <include file='doc\XmlPatch.uex' path='docs/doc[@for="XmlPatch.Patch1"]/*' />
    /// <summary>
    ///    Reads the XDL diffgram from the diffgramFileName and modifies the original XML document
    ///    sourceDoc according to the changes described in the diffgram. 
    /// </summary>
    /// <param name="sourceDoc">The original xml document</param>
    /// <param name="diffgramFileName">XmlReader for the XDL diffgram.</param>
	public void Patch( XmlDocument sourceDoc, XmlReader diffgram ) 
	{
        if ( sourceDoc == null )
            throw new ArgumentNullException( "sourceDoc" );
        if ( diffgram == null )
            throw new ArgumentNullException( "diffgram" );

        XmlNode sourceNode = sourceDoc;
		Patch( ref sourceNode, diffgram );
        Debug.Assert( sourceNode == sourceDoc );
	}
    
    /// <include file='doc\XmlPatch.uex' path='docs/doc[@for="XmlPatch.Patch3"]/*' />
    /// <summary>
    ///    Reads the XDL diffgram from the diffgramFileName and modifies the original XML document
    ///    sourceDoc according to the changes described in the diffgram. 
    /// </summary>
    /// <param name="sourceDoc">The original xml document</param>
    /// <param name="diffgramFileName">XmlReader for the XDL diffgram.</param>
	public void Patch( string sourceFile, Stream outputStream, XmlReader diffgram ) 
	{
        if ( sourceFile == null )
            throw new ArgumentNullException( "sourceFile" );
        if ( outputStream == null )
            throw new ArgumentNullException( "outputStream" );
        if ( diffgram == null ) 
            throw new ArgumentException( "diffgram" );

        XmlDocument diffDoc = new XmlDocument();
        diffDoc.Load( diffgram );

        // patch fragment
        if ( diffDoc.DocumentElement.GetAttribute( "fragments" ) == "yes" ) {
            NameTable nt = new NameTable();
            XmlTextReader tr = new XmlTextReader( new FileStream( sourceFile, FileMode.Open, FileAccess.Read ),
                                                  XmlNodeType.Element,
                                                  new XmlParserContext( nt, new XmlNamespaceManager( nt ),
                                                                        string.Empty, XmlSpace.Default ) );
            Patch( tr, outputStream, diffDoc ); 
        }
        // patch document
        else {
            Patch ( new XmlTextReader( sourceFile ), outputStream, diffDoc );
        }
	}

    /// <include file='doc\XmlPatch.uex' path='docs/doc[@for="XmlPatch.Patch3"]/*' />
    /// <summary>
    ///    Reads the XDL diffgram from the diffgramFileName and modifies the original XML document
    ///    sourceDoc according to the changes described in the diffgram. 
    /// </summary>
    /// <param name="sourceDoc">The original xml document</param>
    /// <param name="diffgramFileName">XmlReader for the XDL diffgram.</param>
    public void Patch( XmlReader sourceReader, Stream outputStream, XmlReader diffgram ) {
        if ( sourceReader == null )
            throw new ArgumentNullException( "sourceReader" );
        if ( outputStream == null )
            throw new ArgumentNullException( "outputStream" );
        if ( diffgram == null ) 
            throw new ArgumentException( "diffgram" );

        XmlDocument diffDoc = new XmlDocument();
        diffDoc.Load( diffgram );

        Patch( sourceReader, outputStream, diffDoc );
    }

    private void Patch( XmlReader sourceReader, Stream outputStream, XmlDocument diffDoc ) {
        bool bFragments = diffDoc.DocumentElement.GetAttribute( "fragments" ) == "yes"; 
        Encoding enc = null;

        if ( bFragments ) {
            // load fragment
            XmlDocument tmpDoc = new XmlDocument();
            XmlDocumentFragment frag = tmpDoc.CreateDocumentFragment();

            XmlNode node;
            while ( ( node = tmpDoc.ReadNode( sourceReader ) ) != null ) {
                switch ( node.NodeType ) {
                    case XmlNodeType.Whitespace:
                        break;
                    case XmlNodeType.XmlDeclaration:
                        frag.InnerXml = node.OuterXml;
                        break;
                    default:
                        frag.AppendChild( node );
                        break;
                }

                if ( enc == null ) {
                    if ( sourceReader is XmlTextReader ) {
                        enc = ((XmlTextReader)sourceReader).Encoding;
                    }
                    else if ( sourceReader is XmlValidatingReader ) {
                        enc = ((XmlValidatingReader)sourceReader).Encoding;
                    }
                    else {
                        enc = Encoding.Unicode;
                    }
                }
            }

            // patch
            XmlNode sourceNode = frag;
            Patch( ref sourceNode, diffDoc );
            Debug.Assert( sourceNode == frag );

            // save
            if ( frag.FirstChild != null && frag.FirstChild.NodeType == XmlNodeType.XmlDeclaration ) {
                enc = Encoding.GetEncoding( ((XmlDeclaration)sourceNode.FirstChild).Encoding );
            }
            XmlTextWriter tw = new XmlTextWriter( outputStream, enc );
            frag.WriteTo( tw );
            tw.Flush();
        }
        else {
            // load document
            XmlDocument sourceDoc = new XmlDocument();
            sourceDoc.XmlResolver = null;
            sourceDoc.Load( sourceReader );

            // patch
            XmlNode sourceNode = sourceDoc;
    	    Patch( ref sourceNode, diffDoc );
            Debug.Assert( sourceNode == sourceDoc );

            // save
            sourceDoc.Save( outputStream );
        }
    }


    /// <include file='doc\XmlPatch.uex' path='docs/doc[@for="XmlPatch.Patch2"]/*' />
    /// <summary>
    ///    Reads the XDL diffgram from the diffgramFileName and modifies the original XML document
    ///    sourceDoc according to the changes described in the diffgram. 
    /// </summary>
    /// <param name="sourceDoc">The original xml node</param>
    /// <param name="diffgramFileName">XmlReader for the XDL diffgram.</param>
    public void Patch( ref XmlNode sourceNode, XmlReader diffgram )
    {
        if ( sourceNode == null )
            throw new ArgumentNullException( "sourceNode" );
        if ( diffgram == null )
            throw new ArgumentNullException( "diffgram" );

        XmlDocument diffDoc = new XmlDocument();
        diffDoc.Load( diffgram );

        Patch( ref sourceNode, diffDoc );
    }

    private void Patch( ref XmlNode sourceNode, XmlDocument diffDoc ) {
        XmlElement diffgramEl = diffDoc.DocumentElement;
        if ( diffgramEl.LocalName != "xmldiff" || diffgramEl.NamespaceURI != XmlDiff.NamespaceUri )
            XmlPatchError.Error( XmlPatchError.ExpectingDiffgramElement );

        XmlNamedNodeMap diffgramAttributes = diffgramEl.Attributes;
        XmlAttribute srcDocAttr = (XmlAttribute)diffgramAttributes.GetNamedItem( "srcDocHash" );
        if ( srcDocAttr == null )
            XmlPatchError.Error( XmlPatchError.MissingSrcDocAttribute );

        ulong hashValue = 0;
        try { 
            hashValue = ulong.Parse( srcDocAttr.Value );
        }
        catch {
            XmlPatchError.Error( XmlPatchError.InvalidSrcDocAttribute );
        }

        XmlAttribute optionsAttr = (XmlAttribute) diffgramAttributes.GetNamedItem( "options" );
        if ( optionsAttr == null )
            XmlPatchError.Error( XmlPatchError.MissingOptionsAttribute );
            
        // parse options
        XmlDiffOptions xmlDiffOptions = XmlDiffOptions.None;
        try {
            xmlDiffOptions = XmlDiff.ParseOptions( optionsAttr.Value );
        }
        catch {
            XmlPatchError.Error( XmlPatchError.InvalidOptionsAttribute );
        }
        
        _ignoreChildOrder = ( (int)xmlDiffOptions & (int)XmlDiffOptions.IgnoreChildOrder ) != 0;

        // Calculate the hash value of source document and check if it agrees with
        // of srcDocHash attribute value.
        if ( !XmlDiff.VerifySource( sourceNode, hashValue, xmlDiffOptions ) )
            XmlPatchError.Error( XmlPatchError.SrcDocMismatch );

        // Translate diffgram & Apply patch
        if ( sourceNode.NodeType == XmlNodeType.Document ) 
        {
            Patch patch = CreatePatch( sourceNode, diffgramEl );

    		// create temporary root element and move all document children under it
            XmlDocument sourceDoc = (XmlDocument)sourceNode;
    		XmlElement tempRoot = sourceDoc.CreateElement( "tempRoot" );
	    	XmlNode child = sourceDoc.FirstChild;
    		while ( child != null )
	    	{
		    	XmlNode tmpChild = child.NextSibling;

			    if ( child.NodeType != XmlNodeType.XmlDeclaration &&
				    child.NodeType != XmlNodeType.DocumentType )
    			{
	    			sourceDoc.RemoveChild( child );
		    		tempRoot.AppendChild( child );
    			}

	    		child = tmpChild;
    		}
	    	sourceDoc.AppendChild( tempRoot );

            // Apply patch
            XmlNode temp = null;
            patch.Apply( tempRoot, ref temp );

    		// remove the temporary root element
            if ( sourceNode.NodeType == XmlNodeType.Document ) {
    		    sourceDoc.RemoveChild( tempRoot );
    	    	Debug.Assert( tempRoot.Attributes.Count == 0 );
	    	    while ( ( child = tempRoot.FirstChild ) != null )
    	    	{
	    	    	tempRoot.RemoveChild( child );
    		    	sourceDoc.AppendChild( child );
	    	    }
            }
        }
        else if ( sourceNode.NodeType == XmlNodeType.DocumentFragment ) {
            Patch patch = CreatePatch( sourceNode, diffgramEl );
            XmlNode temp = null;
            patch.Apply( sourceNode, ref temp );
        }
        else {
            // create fragment with sourceNode as its only child
            XmlDocumentFragment fragment = sourceNode.OwnerDocument.CreateDocumentFragment();
            XmlNode previousSourceParent = sourceNode.ParentNode;
            XmlNode previousSourceSibbling = sourceNode.PreviousSibling;

            if ( previousSourceParent != null ) {
                previousSourceParent.RemoveChild( sourceNode );
            }
            if ( sourceNode.NodeType != XmlNodeType.XmlDeclaration ) {
                fragment.AppendChild( sourceNode );
            }
            else {
                fragment.InnerXml = sourceNode.OuterXml;
            }

            Patch patch = CreatePatch( fragment, diffgramEl );
            XmlNode temp = null;
            patch.Apply( fragment, ref temp );

            XmlNodeList childNodes = fragment.ChildNodes;
            if ( childNodes.Count != 1 ) {
                XmlPatchError.Error( XmlPatchError.InternalErrorMoreThanOneNodeLeft, childNodes.Count.ToString() );
            }

            sourceNode = childNodes.Item(0);
            fragment.RemoveAll();
            if ( previousSourceParent != null ) {
                previousSourceParent.InsertAfter( sourceNode, previousSourceSibbling );
            }
        }
    }

    private Patch CreatePatch( XmlNode sourceNode, XmlElement diffgramElement )
	{
        Debug.Assert( sourceNode.NodeType == XmlNodeType.Document ||
                      sourceNode.NodeType == XmlNodeType.DocumentFragment );

        Patch patch = new Patch( sourceNode );

        _sourceRootNode = sourceNode;

        // create patch for <xmldiff> node children
        CreatePatchForChildren( sourceNode, 
                                diffgramElement,
                                patch );
        return patch;
    }

    private void CreatePatchForChildren( XmlNode sourceParent, 
                                         XmlElement diffgramParent, 
                                         XmlPatchParentOperation patchParent )
    {
        Debug.Assert( sourceParent != null );
        Debug.Assert( diffgramParent != null );
        Debug.Assert( patchParent != null );

        XmlPatchOperation lastPatchOp = null;

        XmlNode node = diffgramParent.FirstChild;
        while ( node != null )
        {
            if ( node.NodeType != XmlNodeType.Element ) {
                node = node.NextSibling;
                continue;
            }

            XmlElement diffOp = (XmlElement)node;
            XmlNodeList matchNodes = null;
            string matchAttr = diffOp.GetAttribute( "match" );

            if ( matchAttr != string.Empty )
            {
                matchNodes = PathDescriptorParser.SelectNodes( _sourceRootNode, sourceParent, matchAttr );
                
                if ( matchNodes.Count == 0 )
                    XmlPatchError.Error( XmlPatchError.NoMatchingNode, matchAttr );
            }

            XmlPatchOperation patchOp = null;

            switch ( diffOp.LocalName )
            {
                case "node":
                {
                    Debug.Assert( matchAttr != string.Empty );

                    if ( matchNodes.Count != 1 )
                        XmlPatchError.Error( XmlPatchError.MoreThanOneNodeMatched, matchAttr );

                    XmlNode matchNode = matchNodes.Item( 0 );

                    if ( _sourceRootNode.NodeType != XmlNodeType.Document ||
                         ( matchNode.NodeType != XmlNodeType.XmlDeclaration && matchNode.NodeType != XmlNodeType.DocumentType ) ) {
                        patchOp = new PatchSetPosition( matchNode );
                        CreatePatchForChildren( matchNode, diffOp, (XmlPatchParentOperation) patchOp );
                    }
                    break;
                }
                case "add":
                {
                    // copy node/subtree
                    if ( matchAttr != string.Empty )
                    {
                        bool bSubtree = diffOp.GetAttribute( "subtree" ) != "no";
                        patchOp = new PatchCopy( matchNodes, bSubtree );
                        if ( !bSubtree )
                            CreatePatchForChildren( sourceParent, diffOp, (XmlPatchParentOperation) patchOp );
                    }
                    else
                    {
                        string type = diffOp.GetAttribute( "type" );
                        // add single node
                        if ( type != string.Empty )
                        {
                            XmlNodeType nodeType = (XmlNodeType) int.Parse( type );
                            bool bElement = (nodeType == XmlNodeType.Element);

                            if ( nodeType != XmlNodeType.DocumentType ) {
                                patchOp = new PatchAddNode( nodeType,  
                                                            diffOp.GetAttribute( "name" ), 
                                                            diffOp.GetAttribute( "ns" ), 
                                                            diffOp.GetAttribute( "prefix" ), 
                                                            bElement ? string.Empty : diffOp.InnerText,
                                                            _ignoreChildOrder );
                                if ( bElement )
                                    CreatePatchForChildren( sourceParent, diffOp, (XmlPatchParentOperation) patchOp );
                            }
                            else {
                                patchOp = new PatchAddNode( nodeType,  
                                                            diffOp.GetAttribute( "name" ), 
                                                            diffOp.GetAttribute( "systemId" ), 
                                                            diffOp.GetAttribute( "publicId" ), 
                                                            diffOp.InnerText,
                                                            _ignoreChildOrder );
                            }
                        }
                        // add blob
                        else
                        {
                            Debug.Assert( diffOp.ChildNodes.Count > 0 );
                            patchOp = new PatchAddXmlFragment( diffOp.ChildNodes );
                        }
                    }

                    break;
                }
                case "remove":
                {
                    Debug.Assert( matchAttr != string.Empty );

                    bool bSubtree = diffOp.GetAttribute( "subtree" ) != "no";
                    patchOp = new PatchRemove( matchNodes, bSubtree );
                    if ( !bSubtree )
                    {
                        Debug.Assert( matchNodes.Count == 1 );
                        CreatePatchForChildren( matchNodes.Item(0), diffOp, (XmlPatchParentOperation) patchOp );
                    }

                    break;
                }
                case "change":
                {
                    Debug.Assert( matchAttr != string.Empty );
                    if ( matchNodes.Count != 1 )
                        XmlPatchError.Error( XmlPatchError.MoreThanOneNodeMatched, matchAttr );

                    XmlNode matchNode = matchNodes.Item( 0 );
                    if ( matchNode.NodeType != XmlNodeType.DocumentType ) {
                        patchOp = new PatchChange( matchNode, 
                                                diffOp.HasAttribute( "name" ) ? diffOp.GetAttribute( "name" ) : null,
                                                diffOp.HasAttribute( "ns" ) ? diffOp.GetAttribute( "ns" ) : null, 
                                                diffOp.HasAttribute( "prefix" ) ? diffOp.GetAttribute( "prefix" ) : null, 
                                                (matchNode.NodeType == XmlNodeType.Element) ? null : diffOp );
                    }
                    else {
                        patchOp = new PatchChange( matchNode,
                                                   diffOp.HasAttribute( "name" ) ? diffOp.GetAttribute( "name" ) : null, 
                                                   diffOp.HasAttribute( "systemId" ) ? diffOp.GetAttribute( "systemId" ) : null,
                                                   diffOp.HasAttribute( "publicId" ) ? diffOp.GetAttribute( "publicId" ) : null,
                                                   diffOp.IsEmpty ? null : diffOp );
                    }

                    if ( matchNode.NodeType == XmlNodeType.Element )
                        CreatePatchForChildren( matchNode, diffOp, (XmlPatchParentOperation) patchOp );
                    break;
                }
                case "descriptor":
                    return;

                default:
                    Debug.Assert( false, "Invalid element in the XDL diffgram ." );
                    break;
            }

            if ( patchOp != null ) {
                patchParent.InsertChildAfter( lastPatchOp, patchOp );
                lastPatchOp = patchOp;
            }
            node = node.NextSibling;
        }
    }
}

}
