//------------------------------------------------------------------------------
// <copyright file="DiffgramOperation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.Collections;

namespace Microsoft.XmlDiffPatch
{
//////////////////////////////////////////////////////////////////
// DiffgramOperation
//
internal abstract class DiffgramOperation
{
// Fields
    internal DiffgramOperation _nextSiblingOp;
    protected ulong _operationID;
    internal DiffgramOperation _parent;

	internal DiffgramOperation( ulong operationID )
	{
        _nextSiblingOp = null;
        _operationID = operationID;
	}

    internal abstract XmlDiffOperation Operation { get; }

// Methods
    internal abstract void WriteTo( XmlWriter xmlWriter, XmlDiff xmlDiff );

    internal static string GetRelativeAddressOfNodeset( XmlDiffNode firstNode, XmlDiffNode lastNode )
    {
		Debug.Assert( !( firstNode is XmlDiffAttributeOrNamespace ) &&
			          !( lastNode is XmlDiffAttributeOrNamespace ) );

        int prevPosition = -1;
        bool bInterval = false;
        StringBuilder sb = new StringBuilder();
        XmlDiffNode curNode = firstNode;
        for (;;)
        {
			Debug.Assert( curNode.Position > 0 );

            if ( curNode.Position != prevPosition + 1 ) {
                if ( bInterval ) {
                    sb.Append( prevPosition );
                    bInterval = false;
                    sb.Append( '|' );
                }
                sb.Append( curNode.Position );
                if ( curNode != lastNode ) {
                    if ( curNode._nextSibling.Position == curNode.Position + 1 ) {
                        sb.Append( "-" );
                        bInterval = true;
                    }
                    else
                        sb.Append( '|' );
                }
            }

            if ( curNode == lastNode )
                break;

            prevPosition = curNode.Position;
            curNode = curNode._nextSibling;
        }
        
        if ( bInterval )
            sb.Append( lastNode.Position );

        return sb.ToString();
    }

	internal static void GetAddressOfAttributeInterval( AttributeInterval interval, XmlWriter xmlWriter )
	{
		Debug.Assert( interval != null );
		if ( interval._next == null )
		{
			if ( interval._firstAttr == interval._lastAttr )
			{
				xmlWriter.WriteAttributeString( "match", interval._firstAttr.GetRelativeAddress() );
				return;
			}

            if ( interval._firstAttr._parent._firstChildNode == interval._firstAttr && 
				 ( interval._lastAttr._nextSibling == null || 
				   interval._lastAttr._nextSibling.NodeType != XmlDiffNodeType.Attribute ) )
			{
				xmlWriter.WriteAttributeString( "match", "@*" );
				return;
			}
		}
			
		string match = string.Empty;
		
		for(;;)
		{
			XmlDiffAttribute attr = (XmlDiffAttribute) interval._firstAttr;
			for (;;)
			{
				match += attr.GetRelativeAddress();
		
				if ( attr == interval._lastAttr )
					break;
				match += "|";
				attr = (XmlDiffAttribute) attr._nextSibling;
				Debug.Assert( attr != null );
			}

			interval = interval._next;
			if ( interval == null )
			{
				xmlWriter.WriteAttributeString( "match", match );
				return;
			}
			
			match += "|";
		}
	}

	internal static void WriteAbsoluteMatchAttribute( XmlDiffNode node, XmlWriter xmlWriter )
    {
        XmlDiffAttribute attr = node as XmlDiffAttribute;
        
        if ( attr != null  && attr.NamespaceURI != string.Empty )
            WriteNamespaceDefinition( attr, xmlWriter );

        xmlWriter.WriteAttributeString( "match", node.GetAbsoluteAddress() );
    }

    private static void WriteNamespaceDefinition( XmlDiffAttribute attr, XmlWriter xmlWriter )
    {
        Debug.Assert( attr.NamespaceURI != string.Empty );

        if ( attr.Prefix != string.Empty )
            xmlWriter.WriteAttributeString( "xmlns", attr.Prefix, XmlDiff.XmlnsNamespaceUri, attr.NamespaceURI );
        else
            xmlWriter.WriteAttributeString( string.Empty, "xmlns", XmlDiff.XmlnsNamespaceUri, attr.NamespaceURI );
    }
}

//////////////////////////////////////////////////////////////////
// DiffgramParentOperation
//
internal abstract class DiffgramParentOperation : DiffgramOperation
{
// Fields
    internal DiffgramOperation _firstChildOp;
    internal DiffgramOperation _lastChildOp;

// Constructor
	internal DiffgramParentOperation( ulong operationID ) : base ( operationID )
	{
        _firstChildOp = null;
        _lastChildOp = null;
	}

// Methods
    internal void InsertAtBeginning( DiffgramOperation newOp )
    {
        newOp._nextSiblingOp = _firstChildOp;
        _firstChildOp = newOp;
        newOp._parent = this;

        if ( newOp._nextSiblingOp == null )
            _lastChildOp = newOp;
    }

    internal void InsertAtEnd( DiffgramOperation newOp )
    {
        newOp._nextSiblingOp = null;
        if ( _lastChildOp == null ) 
        {
            Debug.Assert( _firstChildOp == null );
            _firstChildOp = _lastChildOp = newOp;
        }
        else
        {
            _lastChildOp._nextSiblingOp = newOp;
            _lastChildOp = newOp;
        }
        newOp._parent = this;
    }

    internal void InsertAfter( DiffgramOperation newOp, DiffgramOperation refOp )
    {
        Debug.Assert( newOp._nextSiblingOp == null );
        if ( refOp == null )
        {
        }
        else
        {
            newOp._nextSiblingOp = refOp._nextSiblingOp;
            refOp._nextSiblingOp = newOp;
        }
        newOp._parent = this;
    }

    internal void InsertOperationAtBeginning( DiffgramOperation op )
    {
        Debug.Assert( op._nextSiblingOp == null );
        op._nextSiblingOp = _firstChildOp;
        _firstChildOp = op;
        op._parent = this;
    }

    internal void WriteChildrenTo( XmlWriter xmlWriter, XmlDiff xmlDiff )
    {
        DiffgramOperation curOp = _firstChildOp;
        while ( curOp != null )
        {
            curOp.WriteTo( xmlWriter, xmlDiff );
            curOp = curOp._nextSiblingOp;
        }
    }

	internal bool MergeRemoveSubtreeAtBeginning( XmlDiffNode subtreeRoot )
	{
		Debug.Assert( !( subtreeRoot is XmlDiffAttributeOrNamespace ) );

		DiffgramRemoveSubtrees remSubtrees = _firstChildOp as DiffgramRemoveSubtrees;

		return ( remSubtrees != null &&
			     remSubtrees.SetNewFirstNode( subtreeRoot ) );
	}

	internal bool MergeRemoveSubtreeAtEnd( XmlDiffNode subtreeRoot )
	{
		Debug.Assert( !( subtreeRoot is XmlDiffAttributeOrNamespace ) );

		DiffgramRemoveSubtrees remSubtrees = _lastChildOp as DiffgramRemoveSubtrees;

		return ( remSubtrees != null &&
			     remSubtrees.SetNewLastNode( subtreeRoot ) );
	}

	internal bool MergeRemoveAttributeAtBeginning( XmlDiffNode subtreeRoot )
	{
		if ( subtreeRoot.NodeType != XmlDiffNodeType.Attribute )
            return false;

		DiffgramRemoveAttributes remAttrs = _firstChildOp as DiffgramRemoveAttributes;

		return ( remAttrs != null &&
			     remAttrs.AddAttribute( (XmlDiffAttribute)subtreeRoot ) );
	}

    internal bool MergeAddSubtreeAtBeginning( XmlDiffNode subtreeRoot )
    {
        Debug.Assert( subtreeRoot.NodeType != XmlDiffNodeType.Attribute );

        DiffgramAddSubtrees addSubtrees = _firstChildOp as DiffgramAddSubtrees;

        return ( addSubtrees != null &&
                 addSubtrees.SetNewFirstNode( subtreeRoot ) );
    }

    internal bool MergeAddSubtreeAtEnd( XmlDiffNode subtreeRoot )
    {
        Debug.Assert( subtreeRoot.NodeType != XmlDiffNodeType.Attribute );

        DiffgramAddSubtrees addSubtrees = _lastChildOp as DiffgramAddSubtrees;

        return ( addSubtrees != null &&
                 addSubtrees.SetNewLastNode( subtreeRoot ) );
    }
}

//////////////////////////////////////////////////////////////////
// DiffgramAddNode
//
internal class DiffgramAddNode : DiffgramParentOperation
{
// Fields
    XmlDiffNode _targetNode;

// Constructor
    internal DiffgramAddNode( XmlDiffNode targetNode, ulong operationID ) : base ( operationID )
    {
        Debug.Assert( targetNode != null );
        Debug.Assert( targetNode.NodeType == XmlDiffNodeType.Element ||
                      targetNode.NodeType == XmlDiffNodeType.Attribute ||
                      targetNode.NodeType == XmlDiffNodeType.Namespace ||
                      targetNode.NodeType == XmlDiffNodeType.XmlDeclaration || 
                      targetNode.NodeType == XmlDiffNodeType.DocumentType || 
                      targetNode.NodeType == XmlDiffNodeType.EntityReference );

        _targetNode = targetNode;
    }

// Properties
    internal override XmlDiffOperation Operation { 
        get { 
            return XmlDiffOperation.Add; 
        }
    }

// Methods
    internal override void WriteTo( XmlWriter xmlWriter, XmlDiff xmlDiff )
    {
        xmlWriter.WriteStartElement( XmlDiff.Prefix, "add", XmlDiff.NamespaceUri );

        switch ( _targetNode.NodeType )
        {
            case XmlDiffNodeType.Element:
            {
                xmlWriter.WriteAttributeString( "type", ((int)XmlNodeType.Element).ToString() );

                XmlDiffElement el = _targetNode as XmlDiffElement;
                xmlWriter.WriteAttributeString( "name", el.LocalName );
                if ( el.NamespaceURI != string.Empty )
                    xmlWriter.WriteAttributeString( "ns", el.NamespaceURI );
                if ( el.Prefix != string.Empty )
                    xmlWriter.WriteAttributeString( "prefix", el.Prefix );
                if ( _operationID != 0 ) 
                    xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );

                WriteChildrenTo( xmlWriter, xmlDiff );
                break;
            }
            case XmlDiffNodeType.Attribute:
            {
                xmlWriter.WriteAttributeString( "type", ((int)XmlNodeType.Attribute).ToString() );

                XmlDiffAttribute at = _targetNode as XmlDiffAttribute;
                xmlWriter.WriteAttributeString( "name", at.LocalName );
                if ( at.NamespaceURI != string.Empty )
                    xmlWriter.WriteAttributeString( "ns", at.NamespaceURI );
                if ( at.Prefix != string.Empty )
                    xmlWriter.WriteAttributeString( "prefix", at.Prefix );
                if ( _operationID != 0 ) 
                    xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );
                xmlWriter.WriteString( at.Value );
                break;
            }
            case XmlDiffNodeType.Namespace:
            {
                xmlWriter.WriteAttributeString( "type", ((int)XmlNodeType.Attribute).ToString() );

                XmlDiffNamespace ns = _targetNode as XmlDiffNamespace;
                if ( ns.Prefix != string.Empty )
                {
                    xmlWriter.WriteAttributeString( "prefix", "xmlns" );
                    xmlWriter.WriteAttributeString( "name", ns.Prefix );
                }
                else
                    xmlWriter.WriteAttributeString( "name", "xmlns" );
                if ( _operationID != 0 ) 
                    xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );

                xmlWriter.WriteString( ns.NamespaceURI );
                break;
            }
            case XmlDiffNodeType.CDATA:
            {
                Debug.Assert( false, "CDATA nodes should be added with DiffgramAddSubtrees class." );

                xmlWriter.WriteAttributeString( "type", ((int)_targetNode.NodeType).ToString() );

                if ( _operationID != 0 ) 
                    xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );
                xmlWriter.WriteCData( (_targetNode as XmlDiffCharData).Value );
                break;
            }
            case XmlDiffNodeType.Comment:
            {
                Debug.Assert( false, "Comment nodes should be added with DiffgramAddSubtrees class." );

                xmlWriter.WriteAttributeString( "type", ((int)_targetNode.NodeType).ToString() );

                if ( _operationID != 0 ) 
                    xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );
                xmlWriter.WriteComment( (_targetNode as XmlDiffCharData).Value );
                break;
            }
            case XmlDiffNodeType.Text:
            {
                Debug.Assert( false, "Text nodes should be added with DiffgramAddSubtrees class." );

                xmlWriter.WriteAttributeString( "type", ((int)_targetNode.NodeType).ToString() );

                if ( _operationID != 0 ) 
                    xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );
                xmlWriter.WriteString( (_targetNode as XmlDiffCharData).Value );
                break;
            }
            case XmlDiffNodeType.ProcessingInstruction:
            {
                Debug.Assert( false, "Processing instruction nodes should be added with DiffgramAddSubtrees class." );

                xmlWriter.WriteAttributeString( "type", ((int)_targetNode.NodeType).ToString() );

                if ( _operationID != 0 ) 
                    xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );
                XmlDiffPI pi = _targetNode as XmlDiffPI;
                xmlWriter.WriteProcessingInstruction( pi.Name, pi.Value );
                break;
            }
            case XmlDiffNodeType.EntityReference:
            {
                xmlWriter.WriteAttributeString( "type", ((int)XmlNodeType.EntityReference).ToString() );

                if ( _operationID != 0 ) 
                    xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );

                xmlWriter.WriteAttributeString( "name", ((XmlDiffER)_targetNode).Name );
                break;
            }
            case XmlDiffNodeType.SignificantWhitespace:
            {
                Debug.Assert( false, "Significant whitespace nodes should be added with DiffgramAddSubtrees class." );

                xmlWriter.WriteAttributeString( "type", ((int)_targetNode.NodeType).ToString() );

                if ( _operationID != 0 ) 
                    xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );
                xmlWriter.WriteString( ((XmlDiffCharData)_targetNode).Value );
                break;
            }
            case XmlDiffNodeType.XmlDeclaration:
            {
                xmlWriter.WriteAttributeString( "type", ((int)XmlNodeType.XmlDeclaration).ToString() );

                if ( _operationID != 0 )
                    xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );
                xmlWriter.WriteString( ((XmlDiffXmlDeclaration)_targetNode).Value ); 
                break;
            }
            case XmlDiffNodeType.DocumentType:
            {
                xmlWriter.WriteAttributeString( "type", ((int)XmlNodeType.DocumentType).ToString() );

                XmlDiffDocumentType docType = (XmlDiffDocumentType)_targetNode;

                if ( _operationID != 0 )
                    xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );
                xmlWriter.WriteAttributeString( "name", docType.Name );
                if ( docType.PublicId != string.Empty )
                    xmlWriter.WriteAttributeString( "publicId", docType.PublicId );
                if ( docType.SystemId != string.Empty )
                    xmlWriter.WriteAttributeString( "systemId", docType.SystemId );
                if ( docType.Subset != string.Empty )
                    xmlWriter.WriteCData( docType.Subset );
                break;
            }
            default:
                Debug.Assert( false );
                break;
        }

        xmlWriter.WriteEndElement();
    }
}

//////////////////////////////////////////////////////////////////
// DiffgramAddSubtrees
//
internal class DiffgramAddSubtrees : DiffgramOperation
{
// Fields
    internal XmlDiffNode _firstTargetNode;
    internal XmlDiffNode _lastTargetNode;
    private bool _bSorted;
    private bool _bNeedNamespaces;

// Constructor
    internal DiffgramAddSubtrees( XmlDiffNode subtreeRoot, ulong operationID, bool bSorted ) : base ( operationID )
    {
        Debug.Assert( subtreeRoot != null );
        _firstTargetNode = subtreeRoot;
        _lastTargetNode = subtreeRoot;
        _bSorted = bSorted;
        _bNeedNamespaces = subtreeRoot.NodeType == XmlDiffNodeType.Element;
    }

// Properties
    internal override XmlDiffOperation Operation {
        get {
            return XmlDiffOperation.Add;
        }
    }

// Methods
    internal override void WriteTo( XmlWriter xmlWriter, XmlDiff xmlDiff )
    {
        if ( !_bSorted )
            Sort();

        xmlWriter.WriteStartElement( XmlDiff.Prefix, "add", XmlDiff.NamespaceUri );
        if ( _operationID != 0 ) 
            xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );

        // namespaces
        if ( _bNeedNamespaces ) {
            Hashtable definedPrefixes = new Hashtable(); 
            XmlDiffParentNode parent = _firstTargetNode._parent;
            while ( parent != null ) {
                if ( parent._bDefinesNamespaces ) {
                    XmlDiffElement el = (XmlDiffElement)parent;
                    XmlDiffAttributeOrNamespace curNs = el._attributes;
                    while ( curNs != null && curNs.NodeType == XmlDiffNodeType.Namespace ) {
                        if ( definedPrefixes[curNs.Prefix] == null ) {
                            if ( curNs.Prefix == string.Empty )
                                xmlWriter.WriteAttributeString( "xmlns", XmlDiff.XmlnsNamespaceUri, curNs.NamespaceURI );
                            else
                                xmlWriter.WriteAttributeString( "xmlns", curNs.Prefix, XmlDiff.XmlnsNamespaceUri, curNs.NamespaceURI );
                            definedPrefixes[curNs.Prefix] = curNs.Prefix;
                        }
                        curNs = (XmlDiffAttributeOrNamespace)curNs._nextSibling;
                    }
                }
                parent = parent._parent;
            }
        }
        
        // output nodes
        XmlDiffNode node = _firstTargetNode;
        for (;;)
        {
            node.WriteTo( xmlWriter );
            if ( node == _lastTargetNode )
                break;
            node = node._nextSibling;
        }
        xmlWriter.WriteEndElement();
    }

    private void Sort() {
        XmlDiffNode prevSibling = null;
        XmlDiff.SortNodesByPosition( ref _firstTargetNode, ref _lastTargetNode, ref prevSibling );
        _bSorted = true;
    }

	internal bool SetNewFirstNode( XmlDiffNode targetNode )
	{
		if ( _operationID != 0 ||
			 targetNode._nextSibling != _firstTargetNode ||
             ( targetNode.NodeType == XmlDiffNodeType.Text && _firstTargetNode.NodeType == XmlDiffNodeType.Text ) ||
		     !targetNode.CanMerge ||
		     !_firstTargetNode.CanMerge )
			return false;

		_firstTargetNode = targetNode;

        if ( targetNode.NodeType == XmlDiffNodeType.Element )
            _bNeedNamespaces = true;
		return true;
	}

	internal bool SetNewLastNode( XmlDiffNode targetNode )
	{
		if ( _operationID != 0 ||
			 _lastTargetNode._nextSibling != targetNode ||
             ( targetNode.NodeType == XmlDiffNodeType.Text && _lastTargetNode.NodeType == XmlDiffNodeType.Text ) ||
		     !targetNode.CanMerge ||
		     !_lastTargetNode.CanMerge )
			return false;

		_lastTargetNode = targetNode;

        if ( targetNode.NodeType == XmlDiffNodeType.Element )
            _bNeedNamespaces = true;
		return true;
	}
}

//////////////////////////////////////////////////////////////////
// DiffgramCopy
//
internal class DiffgramCopy : DiffgramParentOperation
{
// Fields
    XmlDiffNode _sourceNode;
    bool _bSubtree;

// Constructor
    internal DiffgramCopy( XmlDiffNode sourceNode, bool bSubtree, ulong operationID ) : base ( operationID )
    {
        Debug.Assert( sourceNode != null );
        _sourceNode = sourceNode;
        _bSubtree = bSubtree;
    }

// Properties
    internal override XmlDiffOperation Operation {
        get {
            return XmlDiffOperation.Add;
        }
    }

// Methods
    internal override void WriteTo( XmlWriter xmlWriter, XmlDiff xmlDiff )
    {
        xmlWriter.WriteStartElement( XmlDiff.Prefix, "add", XmlDiff.NamespaceUri );
        WriteAbsoluteMatchAttribute( _sourceNode, xmlWriter );
        if ( !_bSubtree )
            xmlWriter.WriteAttributeString( "subtree", "no" );
        if ( _operationID != 0 ) 
            xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );

        WriteChildrenTo( xmlWriter, xmlDiff );

        xmlWriter.WriteEndElement();
    }
}

//////////////////////////////////////////////////////////////////
// DiffgramRemoveNode
//
internal class DiffgramRemoveNode : DiffgramParentOperation
{
// Fields
    internal XmlDiffNode _sourceNode;
    bool _bSubtree;

// Costructor
    internal DiffgramRemoveNode( XmlDiffNode sourceNode, bool bSubtree, ulong operationID ) : base ( operationID )
    {
        Debug.Assert( sourceNode != null );
        _sourceNode = sourceNode;
        _bSubtree = bSubtree;
    }

// Properties
    internal override XmlDiffOperation Operation {
        get {
            return XmlDiffOperation.Remove;
        }
    }

// Methods
    internal override void WriteTo( XmlWriter xmlWriter, XmlDiff xmlDiff )
    {
        xmlWriter.WriteStartElement( XmlDiff.Prefix, "remove", XmlDiff.NamespaceUri );
        xmlWriter.WriteAttributeString( "match", _sourceNode.GetRelativeAddress() );
        if ( !_bSubtree )
            xmlWriter.WriteAttributeString( "subtree", "no" );
        if ( _operationID != 0 ) 
            xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );

        WriteChildrenTo( xmlWriter, xmlDiff );

        xmlWriter.WriteEndElement();
    }
}

//////////////////////////////////////////////////////////////////
// DiffgramRemoveSubtrees
//
internal class DiffgramRemoveSubtrees : DiffgramOperation
{
// Fields
    XmlDiffNode _firstSourceNode;
	XmlDiffNode _lastSourceNode;
    bool _bSorted;

// Constructor
    internal DiffgramRemoveSubtrees( XmlDiffNode sourceNode, ulong operationID, bool bSorted ) : base ( operationID )
    {
        Debug.Assert( sourceNode != null );
        _firstSourceNode = sourceNode;
		_lastSourceNode = sourceNode;
        _bSorted = bSorted;
    }

// Properties
    internal override XmlDiffOperation Operation {
        get {
            return XmlDiffOperation.Remove;
        }
    }

// Methods
    internal override void WriteTo( XmlWriter xmlWriter, XmlDiff xmlDiff )
    {
        if ( !_bSorted ) 
            Sort();

        xmlWriter.WriteStartElement( XmlDiff.Prefix, "remove", XmlDiff.NamespaceUri );
		if ( _firstSourceNode == _lastSourceNode ) 
			xmlWriter.WriteAttributeString( "match", _firstSourceNode.GetRelativeAddress() );
		else
			xmlWriter.WriteAttributeString( "match", GetRelativeAddressOfNodeset( _firstSourceNode, _lastSourceNode ) );
        if ( _operationID != 0 ) 
            xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );
        xmlWriter.WriteEndElement();
    }

    private void Sort() {
        XmlDiffNode prevSibling = null;
        XmlDiff.SortNodesByPosition( ref _firstSourceNode, ref _lastSourceNode, ref prevSibling );
        _bSorted = true;
    }

	internal bool SetNewFirstNode( XmlDiffNode srcNode )
	{
		if ( _operationID != 0 ||
			 srcNode._nextSibling != _firstSourceNode ||
			 !srcNode.CanMerge ||
			 !_firstSourceNode.CanMerge )
			return false;

		_firstSourceNode = srcNode;
		return true;
	}

	internal bool SetNewLastNode( XmlDiffNode srcNode )
	{
		if ( _operationID != 0 ||
			 _lastSourceNode._nextSibling != srcNode ||
			 !srcNode.CanMerge ||
             !_firstSourceNode.CanMerge )
			return false;

		_lastSourceNode = srcNode;
		return true;
	}
}

//////////////////////////////////////////////////////////////////
// DiffgramRemoveAttributes
//
internal class DiffgramRemoveAttributes : DiffgramOperation
{
// Fields
    AttributeInterval _attributes;

// Constructor
    internal DiffgramRemoveAttributes( XmlDiffAttribute sourceAttr ) : base ( 0 )
    {
        Debug.Assert( sourceAttr != null );
		_attributes = new AttributeInterval( sourceAttr, null );
    }

// Properties
    internal override XmlDiffOperation Operation {
        get {
            return XmlDiffOperation.Remove;
        }
    }

// Methods
    internal override void WriteTo( XmlWriter xmlWriter, XmlDiff xmlDiff )
    {
        xmlWriter.WriteStartElement( XmlDiff.Prefix, "remove", XmlDiff.NamespaceUri );
        GetAddressOfAttributeInterval( _attributes, xmlWriter );
        Debug.Assert( _operationID == 0 );
        xmlWriter.WriteEndElement();
    }

	internal bool AddAttribute( XmlDiffAttribute srcAttr )
	{
		if ( _operationID != 0 ||
			 srcAttr._parent != _attributes._firstAttr._parent )
			return false;

		if ( srcAttr._nextSibling == _attributes._firstAttr )
			_attributes._firstAttr = srcAttr;
		else
			_attributes = new AttributeInterval( srcAttr, _attributes );

		return true;
	}
}

//////////////////////////////////////////////////////////////////
// DiffgramChangeNode
//
internal class DiffgramChangeNode : DiffgramParentOperation
{
// Fields
    internal XmlDiffNode _sourceNode;
    internal XmlDiffNode _targetNode;
    internal XmlDiffOperation _op;

// Constructor
    internal DiffgramChangeNode( XmlDiffNode sourceNode, XmlDiffNode targetNode, XmlDiffOperation op, ulong operationID ) 
        : base ( operationID )
    {
        Debug.Assert( sourceNode != null );
        Debug.Assert( targetNode != null );
        Debug.Assert( XmlDiff.IsChangeOperation( op ) || op == XmlDiffOperation.ChangeAttr);

        _sourceNode = sourceNode;
        _targetNode = targetNode;
        _op = op;
    }

// Properties
    internal override XmlDiffOperation Operation {
        get {
            return _op;
        }
    }

// Methods
    internal override void WriteTo( XmlWriter xmlWriter, XmlDiff xmlDiff )
    {
        xmlWriter.WriteStartElement( XmlDiff.Prefix, "change", XmlDiff.NamespaceUri );
        xmlWriter.WriteAttributeString( "match", _sourceNode.GetRelativeAddress() );
        if ( _operationID != 0 ) 
            xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );

        switch ( _op )
        {
            case XmlDiffOperation.ChangeAttr:
            {
                XmlDiffAttribute sourceAttr = (XmlDiffAttribute) _sourceNode;
                XmlDiffAttribute targetAttr = (XmlDiffAttribute) _targetNode;

                if ( sourceAttr.Prefix != targetAttr.Prefix && !xmlDiff.IgnorePrefixes && !xmlDiff.IgnoreNamespaces )
                    xmlWriter.WriteAttributeString( "prefix", targetAttr.Prefix );
                if ( sourceAttr.NamespaceURI != targetAttr.NamespaceURI && !xmlDiff.IgnoreNamespaces )
                    xmlWriter.WriteAttributeString( "ns", targetAttr.NamespaceURI );
                xmlWriter.WriteString( targetAttr.Value );
                break;
            }
	        case XmlDiffOperation.ChangeElementName:
            {
                XmlDiffElement sourceEl = (XmlDiffElement) _sourceNode;
                XmlDiffElement targetEl = (XmlDiffElement) _targetNode;

                if ( sourceEl.LocalName != targetEl.LocalName )
                    xmlWriter.WriteAttributeString( "name", targetEl.LocalName );
                if ( sourceEl.Prefix != targetEl.Prefix && !xmlDiff.IgnorePrefixes && !xmlDiff.IgnoreNamespaces )
                    xmlWriter.WriteAttributeString( "prefix", targetEl.Prefix );
                if ( sourceEl.NamespaceURI != targetEl.NamespaceURI && !xmlDiff.IgnoreNamespaces )
                    xmlWriter.WriteAttributeString( "ns", targetEl.NamespaceURI );

                WriteChildrenTo( xmlWriter, xmlDiff );

                break;
            }
	        case XmlDiffOperation.ChangePI:
            {
				XmlDiffPI sourcePi = (XmlDiffPI)_sourceNode;
                XmlDiffPI targetPi = (XmlDiffPI)_targetNode;

                if ( sourcePi.Value == targetPi.Value ) {
                    Debug.Assert( sourcePi.Name != targetPi.Name );
	    		    xmlWriter.WriteAttributeString( "name", targetPi.Name );
                }
                else {
					xmlWriter.WriteProcessingInstruction( targetPi.Name, targetPi.Value );
                }
                break;
            }
            case XmlDiffOperation.ChangeCharacterData:
            {
                XmlDiffCharData chd = (XmlDiffCharData)_targetNode;
                switch ( _targetNode.NodeType ) {
                    case XmlDiffNodeType.Text:
                    case XmlDiffNodeType.SignificantWhitespace:
                        xmlWriter.WriteString( chd.Value );
                        break;
                    case XmlDiffNodeType.Comment:
                        xmlWriter.WriteComment( chd.Value );
                        break;
                    case XmlDiffNodeType.CDATA:
                        xmlWriter.WriteCData( chd.Value );
                        break;
                    default:
                        Debug.Assert( false );
                        break;
                }
                break;
            }
            case XmlDiffOperation.ChangeER:
            {
                xmlWriter.WriteAttributeString( "name", ((XmlDiffER)_targetNode).Name );
                break;
            }
            case XmlDiffOperation.ChangeXmlDeclaration:
            {
                xmlWriter.WriteString( ((XmlDiffXmlDeclaration)_targetNode).Value );
                break;
            }
            case XmlDiffOperation.ChangeDTD:
            {
				XmlDiffDocumentType sourceDtd = (XmlDiffDocumentType)_sourceNode;
                XmlDiffDocumentType targetDtd = (XmlDiffDocumentType)_targetNode;

				if ( sourceDtd.Name != targetDtd.Name )
					xmlWriter.WriteAttributeString( "name", targetDtd.Name );
				if ( sourceDtd.SystemId != targetDtd.SystemId )
					xmlWriter.WriteAttributeString( "systemId", targetDtd.SystemId );
				if ( sourceDtd.PublicId != targetDtd.PublicId )
					xmlWriter.WriteAttributeString( "publicId", targetDtd.PublicId );
				if ( sourceDtd.Subset != targetDtd.Subset )
					xmlWriter.WriteCData( targetDtd.Subset );
                break;
            }
            default:
                Debug.Assert( false );
                break;
        }

        xmlWriter.WriteEndElement();
    }
}

//////////////////////////////////////////////////////////////////
// DiffgramRemoveNode
//
internal class DiffgramPosition : DiffgramParentOperation
{
// Fields
    internal XmlDiffNode _sourceNode;
    internal XmlDiffNode _targetNode;

// Costructor
    internal DiffgramPosition( XmlDiffNode sourceNode, XmlDiffNode targetNode ) 
        : this (  sourceNode ) {
        _targetNode = targetNode;
    }

    internal DiffgramPosition( XmlDiffNode sourceNode ) : base( 0 )
    {
        Debug.Assert( !( sourceNode is XmlDiffAttributeOrNamespace ) );

        if ( sourceNode is XmlDiffShrankNode ) 
        {
    		Debug.Assert( sourceNode != null );
			sourceNode = ((XmlDiffShrankNode)sourceNode)._lastNode;
        }

        _sourceNode = sourceNode;
    }

// Properties
    internal override XmlDiffOperation Operation {
        get {
            return XmlDiffOperation.Match;
        }
    }

// Methods
    internal override void WriteTo( XmlWriter xmlWriter, XmlDiff xmlDiff )
    {
        xmlWriter.WriteStartElement( XmlDiff.Prefix, "node", XmlDiff.NamespaceUri );
        xmlWriter.WriteAttributeString( "match", _sourceNode.GetRelativeAddress() );

        WriteChildrenTo( xmlWriter, xmlDiff );

        xmlWriter.WriteEndElement();
    }
}

//////////////////////////////////////////////////////////////////
// AttributeInterval
//
internal class AttributeInterval
{
	internal XmlDiffAttribute _firstAttr;
	internal XmlDiffAttribute _lastAttr;
	internal AttributeInterval _next;

	internal AttributeInterval( XmlDiffAttribute attr, AttributeInterval next )
	{
		_firstAttr = attr;
		_lastAttr = attr;
		_next = next;
	}
}

}