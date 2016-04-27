//------------------------------------------------------------------------------
// <copyright file="XmlDiffNodes.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Diagnostics;

namespace Microsoft.XmlDiffPatch {

//////////////////////////////////////////////////////////////////
// XmlDiffNode
//
internal abstract class XmlDiffNode 
{
// Fields
    // tree pointers
    internal XmlDiffParentNode _parent;
	internal XmlDiffNode _nextSibling;

    // original position among the other children
    internal int _position;

    // 'matching identical subtrees' algorithm fields:
    internal ulong _hashValue = 0;
    internal bool _bExpanded;

    // 'tree-to-tree comparison' algorithm fields:
    internal int _leftmostLeafIndex;
    internal bool _bKeyRoot;

    internal bool _bSomeDescendantMatches = false;

#if DEBUG
    internal int _index = 0;
#endif

//    internal bool _nodeOrDescendantMatches = false;

// Constructors
    internal XmlDiffNode( int position ) 
	{
        _parent = null;
        _nextSibling = null;
        _position = position;
        _bExpanded = false;
    }

// Properties
    internal abstract XmlDiffNodeType NodeType { get; }

    internal int Position { get { return _position; } }
    internal bool IsKeyRoot { get { return _bKeyRoot; } }
	internal int Left { get { return _leftmostLeafIndex; } set { _leftmostLeafIndex = value; } } 
    internal virtual XmlDiffNode FirstChildNode { get { return null; } }
    internal virtual bool HasChildNodes { get { return false; } }
    internal virtual int NodesCount { get { return 1; } set { Debug.Assert( value == 1 ); } }
    internal ulong HashValue { get { return _hashValue; } }
   
	internal virtual string OuterXml 
	{
        get {
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter( sw );

            WriteTo( xw );
            xw.Close();

            return sw.ToString();
        }
    }
    
	internal virtual string InnerXml 
	{
        get {
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter( sw );

            WriteContentTo( xw );
            xw.Close();

            return sw.ToString();
        }
    }

    internal virtual bool CanMerge {
        get {
            return true;
        }
    }   


// Methods
    // computes hash value of the node and stores it in the _hashValue variable
    internal abstract void ComputeHashValue( XmlHash xmlHash );

	// compares the node to another one and returns the xmldiff operation for changing this node to the other one
	internal abstract XmlDiffOperation GetDiffOperation( XmlDiffNode changedNode, XmlDiff xmlDiff );

	// compares the node to another one and returns true, if the nodes are identical;
    // on elements this method ignores namespace declarations
	internal virtual bool IsSameAs( XmlDiffNode node, XmlDiff xmlDiff )
    {
        return GetDiffOperation( node, xmlDiff ) == XmlDiffOperation.Match;
    }

    // Abstract methods for outputing
    internal abstract void WriteTo( XmlWriter w );
    internal abstract void WriteContentTo( XmlWriter w );

    // Addressing
    internal virtual string GetRelativeAddress()
    {
        return Position.ToString();
    }

    internal string GetAbsoluteAddress()
    {
        string address = GetRelativeAddress();
        XmlDiffNode ancestor = _parent;

        while ( ancestor.NodeType != XmlDiffNodeType.Document )
        {
            address = ancestor.GetRelativeAddress() + "/" + address;
            ancestor = ancestor._parent;
        }
        
        return "/" + address;
    }

    static internal string GetRelativeAddressOfInterval( XmlDiffNode firstNode, XmlDiffNode lastNode )
    {
		Debug.Assert( firstNode._parent == lastNode._parent );

        if ( firstNode == lastNode )
            return firstNode.GetRelativeAddress();
        else 
        {
            if ( firstNode._parent._firstChildNode == firstNode &&
                 lastNode._nextSibling == null )
                return "*";
            else
                return firstNode.Position.ToString() + "-" + lastNode.Position.ToString();
        }
    }


#if DEBUG
    internal abstract void Dump( string indent );
#endif
}

//////////////////////////////////////////////////////////////////
// XmlDiffParentNode
//
internal abstract class XmlDiffParentNode : XmlDiffNode
{
// Fields
	// first node in the list of attributes AND children 
    internal XmlDiffNode _firstChildNode;

    // number of nodes in the subtree rooted at this node
    private int _nodesCount;

    // number of child nodes - calculated on demand
    private int _childNodesCount = -1;

    // flag if the node contains only element children (this is used by addressing)
    internal bool _elementChildrenOnly;

    internal bool _bDefinesNamespaces;

    internal override bool HasChildNodes { get { return (_firstChildNode != null); } }
	internal override int NodesCount 
	{ 
		get 
		{ 
			return _nodesCount; 
		} 
		set 
		{ 
			Debug.Assert( value > 0 );
			_nodesCount = value; 
		} 
	}

    internal int ChildNodesCount { 
        get {
            if ( _childNodesCount == -1 ) {
                int count = 0;
                for ( XmlDiffNode child = _firstChildNode; child != null; child = child._nextSibling )
                    count++;
                _childNodesCount = count;
            }
            return _childNodesCount;
        }
    }


// Constructor
    internal XmlDiffParentNode( int position ) : base ( position )
    {
        _firstChildNode = null;
        _nodesCount = 1;
        _elementChildrenOnly = true;
        _bDefinesNamespaces = false;
        _hashValue = 0;
    }

// Properties
    internal override XmlDiffNode FirstChildNode { get { return _firstChildNode; } }

// Methods
    internal virtual void InsertChildNodeAfter( XmlDiffNode childNode, XmlDiffNode newChildNode ) 
	{
        Debug.Assert( newChildNode != null );
        Debug.Assert( ! ( newChildNode is XmlDiffAttributeOrNamespace ) );
		Debug.Assert( childNode == null || childNode._parent == this );
#if DEBUG
        if ( newChildNode.NodeType == XmlDiffNodeType.Attribute )
            Debug.Assert( childNode == null || 
                          childNode.NodeType == XmlDiffNodeType.Attribute || 
                          childNode.NodeType == XmlDiffNodeType.Namespace );
#endif

        newChildNode._parent = this;
        if ( childNode == null ) 
        {
            newChildNode._nextSibling = _firstChildNode;
            _firstChildNode = newChildNode;
        }
        else 
        {
            newChildNode._nextSibling = childNode._nextSibling;
            childNode._nextSibling = newChildNode;
        }

        Debug.Assert( newChildNode.NodesCount > 0 );
        _nodesCount += newChildNode.NodesCount;

        if ( newChildNode.NodeType != XmlDiffNodeType.Element &&
             !(newChildNode is XmlDiffAttributeOrNamespace ) )
            _elementChildrenOnly = false;
    }

#if DEBUG
    protected void DumpChildren( string indent )
    {
        XmlDiffNode curChild = _firstChildNode;
        while ( curChild != null )
        {
            curChild.Dump( indent );
            curChild = curChild._nextSibling;
        }
    }
#endif
}
   
//////////////////////////////////////////////////////////////////
// XmlDiffElement
//
internal class XmlDiffElement : XmlDiffParentNode 
{
// Fields
	string _localName;
    string _prefix;
	string _ns;

    // attribute & namespace nodes
    internal XmlDiffAttributeOrNamespace _attributes = null;
    internal ulong _allAttributesHash = 0; // xor combination of hash values of all attributes and namespace nodes
    internal ulong _attributesHashAH = 0;  // xol combination of hash values of attributes and namespace nodes beginning with 'a'-'h'
    internal ulong _attributesHashIQ = 0;  // xol combination of hash values of attributes and namespace nodes beginning with 'i'-'q'
    internal ulong _attributesHashRZ = 0;  // xol combination of hash values of attributes and namespace nodes beginning with 'r'-'z'
    
// Constructors
    internal XmlDiffElement( int position, string localName, string prefix, string ns ) : base( position ) 
	{
        Debug.Assert( localName != null );
        Debug.Assert( prefix != null );
        Debug.Assert( ns != null );

        _localName = localName;
        _prefix = prefix;
        _ns = ns;
    }

// Properties
    internal override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.Element; } }
    internal string LocalName { get { return _localName; } }
    internal string NamespaceURI { get { return _ns; } }
    internal string Prefix { get { return _prefix; } }

	internal string Name 
	{
        get 
		{
            if ( _prefix.Length > 0 ) 
                return _prefix + ":" + _localName;
            else
                return _localName;
        }
    }

// Methods
    // computes hash value of the node and stores it in the _hashValue variable
    internal override void ComputeHashValue( XmlHash xmlHash )
    {
        Debug.Assert( _hashValue == 0 );
        _hashValue = xmlHash.ComputeHashXmlDiffElement( this );
    }

    // compares the node to another one and returns the xmldiff operation for changing this node to the other
	internal override XmlDiffOperation GetDiffOperation( XmlDiffNode changedNode, XmlDiff xmlDiff )
	{
		Debug.Assert( changedNode != null );

        if ( changedNode.NodeType != XmlDiffNodeType.Element )
            return XmlDiffOperation.Undefined;

        XmlDiffElement changedElement = (XmlDiffElement)changedNode;

        // name
        bool bNameMatches = false;
		if (  LocalName == changedElement.LocalName )
        {
            if ( xmlDiff.IgnoreNamespaces )
                bNameMatches = true;
            else
            {
                if ( NamespaceURI == changedElement.NamespaceURI &&
                    ( xmlDiff.IgnorePrefixes || Prefix == changedElement.Prefix ) )
                {
		            bNameMatches = true;
                }
            }
        }

        // attributes
        if ( changedElement._allAttributesHash == _allAttributesHash )
            return bNameMatches ? XmlDiffOperation.Match : XmlDiffOperation.ChangeElementName;
        
        int n = ( changedElement._attributesHashAH == _attributesHashAH ? 0 : 1 ) +
                ( changedElement._attributesHashIQ == _attributesHashIQ ? 0 : 1 ) +
                ( changedElement._attributesHashRZ == _attributesHashRZ ? 0 : 1 );

        Debug.Assert( (int)XmlDiffOperation.ChangeElementName  + 1 == (int)XmlDiffOperation.ChangeElementAttr1 );
        Debug.Assert( (int)XmlDiffOperation.ChangeElementAttr1 + 1 == (int)XmlDiffOperation.ChangeElementAttr2 );
        Debug.Assert( (int)XmlDiffOperation.ChangeElementAttr2 + 1 == (int)XmlDiffOperation.ChangeElementAttr3 );

        Debug.Assert( (int)XmlDiffOperation.ChangeElementAttr3        + 1 == (int)XmlDiffOperation.ChangeElementNameAndAttr1 );
        Debug.Assert( (int)XmlDiffOperation.ChangeElementNameAndAttr1 + 1 == (int)XmlDiffOperation.ChangeElementNameAndAttr2 );
        Debug.Assert( (int)XmlDiffOperation.ChangeElementNameAndAttr2 + 1 == (int)XmlDiffOperation.ChangeElementNameAndAttr3 );

        Debug.Assert( n != 0 );
        if ( bNameMatches ) 
            return (XmlDiffOperation)( ((int)XmlDiffOperation.ChangeElementName) + n );
        else
            return (XmlDiffOperation)( ((int)XmlDiffOperation.ChangeElementAttr3) + n );
    }

	// compares the node to another one and returns true, if the nodes are identical;
    // on elements this method ignores namespace declarations
	internal override bool IsSameAs( XmlDiffNode node, XmlDiff xmlDiff )
    {
        // check node type
		Debug.Assert( node != null );
        if ( node.NodeType != XmlDiffNodeType.Element )
            return false;

        XmlDiffElement element = (XmlDiffElement)node;

        // check element name
        if ( LocalName != element.LocalName )
            return false;
        else if ( !xmlDiff.IgnoreNamespaces )
            if ( NamespaceURI != element.NamespaceURI ) 
                return false;
        else if ( !xmlDiff.IgnorePrefixes )
            if ( Prefix != element.Prefix ) 
                return false;

        // ignore namespace definitions - should be first in the list of attributes
        XmlDiffAttributeOrNamespace attr1 = _attributes;
        while ( attr1 != null && attr1.NodeType == XmlDiffNodeType.Namespace )
            attr1 = (XmlDiffAttributeOrNamespace) attr1._nextSibling;

        XmlDiffAttributeOrNamespace attr2 = _attributes;
        while ( attr2 != null && attr2.NodeType == XmlDiffNodeType.Namespace )
            attr2 = (XmlDiffAttributeOrNamespace) attr2._nextSibling;

        // check attributes
        while ( attr1 != null && attr2 != null )
        {
            if ( !attr1.IsSameAs( attr2, xmlDiff ) )
                return false;
            attr1 = (XmlDiffAttributeOrNamespace) attr1._nextSibling;
            attr2 = (XmlDiffAttributeOrNamespace) attr2._nextSibling;
        }

        return attr1 == null && attr2 == null;
    }

    internal void InsertAttributeOrNamespace( XmlDiffAttributeOrNamespace newAttrOrNs )
    {
        Debug.Assert( newAttrOrNs != null );

        newAttrOrNs._parent = this;
        
        XmlDiffAttributeOrNamespace curAttr = _attributes;
        XmlDiffAttributeOrNamespace prevAttr = null;
        while ( curAttr != null  && 
			    XmlDiffDocument.OrderAttributesOrNamespaces( curAttr, newAttrOrNs ) <= 0 ) 
		{
            prevAttr = curAttr;
            curAttr = (XmlDiffAttributeOrNamespace)curAttr._nextSibling;
        }

        if ( prevAttr == null ) 
        {
            newAttrOrNs._nextSibling = _attributes;
            _attributes = newAttrOrNs;
        }
        else 
        {
            newAttrOrNs._nextSibling = prevAttr._nextSibling;
            prevAttr._nextSibling = newAttrOrNs;
        }

        // hash
        Debug.Assert( newAttrOrNs.HashValue != 0 );
        _allAttributesHash += newAttrOrNs.HashValue;
        
        char firstLetter;
        if ( newAttrOrNs.NodeType == XmlDiffNodeType.Attribute ) 
            firstLetter = ((XmlDiffAttribute)newAttrOrNs).LocalName[0];
        else
        {
            XmlDiffNamespace nsNode = (XmlDiffNamespace)newAttrOrNs;
            firstLetter = ( nsNode.Prefix == string.Empty ) ? 'A' : nsNode.Prefix[0];
        }

        firstLetter = char.ToUpper( firstLetter );

        if ( firstLetter >= 'R' ) {
            _attributesHashRZ += newAttrOrNs.HashValue;
        }
        else if ( firstLetter >= 'I' ) {
            _attributesHashIQ += newAttrOrNs.HashValue;
        }
        else {
            _attributesHashAH += newAttrOrNs.HashValue;
        }

        if ( newAttrOrNs.NodeType == XmlDiffNodeType.Namespace )
            _bDefinesNamespaces = true;
    }

	// Overriden abstract methods for outputting
    internal override void WriteTo( XmlWriter w ) 
	{
        w.WriteStartElement( Prefix, LocalName, NamespaceURI );
        XmlDiffAttributeOrNamespace attr = _attributes;
        while ( attr != null )
        {
            attr.WriteTo( w );
            attr = (XmlDiffAttributeOrNamespace) attr._nextSibling;
        }

        WriteContentTo( w );

        w.WriteEndElement();
    }

    internal override void WriteContentTo( XmlWriter w ) 
	{
        XmlDiffNode child = _firstChildNode;
        while ( child != null ) 
		{
            child.WriteTo( w );
            child = child._nextSibling;
        }
    }

#if DEBUG
    internal override void Dump( string indent )
    {
        Trace.Write( indent + "(" + _index + ") <" + Name );
        XmlDiffAttributeOrNamespace attr = _attributes;
        while ( attr != null ) {
            attr.Dump( indent );
            attr = (XmlDiffAttributeOrNamespace)attr._nextSibling;
        }
        Trace.WriteLine( "> " );
        DumpChildren( indent + "   " );
    }
#endif
}

//////////////////////////////////////////////////////////////////
// XmlDiffAttributeOrNamespace
//
internal abstract class XmlDiffAttributeOrNamespace : XmlDiffNode 
{
// Constructor
    internal XmlDiffAttributeOrNamespace( ) : base( 0 ) 
    {
    }

// Properties
    internal abstract string LocalName { get;  }
    internal abstract string NamespaceURI { get;  }
    internal abstract string Prefix { get; }
    internal abstract string Value { get; }

}

//////////////////////////////////////////////////////////////////
// XmlDiffAttribute
//
internal class XmlDiffAttribute : XmlDiffAttributeOrNamespace 
{
// Fields
	string _localName;
    string _prefix;
	string _ns;
	string _value;

// Constructor
    internal XmlDiffAttribute( string localName, string prefix, string ns, string value ) : base( ) 
    {
        Debug.Assert( localName != null );
        Debug.Assert( prefix != null );
        Debug.Assert( ns != null );
        Debug.Assert( value != null );

        _localName = localName;
        _prefix = prefix;
        _ns = ns;
        _value = value;
    }

// Properties
    internal override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.Attribute; } }
    internal override string LocalName { get { return _localName; } }
    internal override string NamespaceURI { get { return _ns; } }
    internal override string Prefix { get { return _prefix; } }

	internal string Name 	
	{
        get 
		{
            if ( _prefix.Length > 0 )
                return _prefix + ":" + _localName;
            else
                return _localName;
        }
    }

    internal override string Value { 
        get { 
            return _value; 
        } 
    }

    internal override bool CanMerge {
        get {
            return false;
        }
    }

// Methods
    // computes hash value of the node and stores it in the _hashValue variable
    internal override void ComputeHashValue( XmlHash xmlHash )
    {
        Debug.Assert( _hashValue == 0 );
        _hashValue = xmlHash.HashAttribute( _localName, _prefix, _ns, _value );
    }

	// compares the node to another one and returns true, if the nodes are identical;
    // on elements this method ignores namespace declarations
	internal override bool IsSameAs( XmlDiffNode node, XmlDiff xmlDiff )
    {
        Debug.Assert( node.NodeType == XmlDiffNodeType.Attribute );

        XmlDiffAttribute attr = (XmlDiffAttribute) node;
        
        return ( LocalName == attr.LocalName &&
                 ( xmlDiff.IgnoreNamespaces || NamespaceURI == attr.NamespaceURI ) &&
                 ( xmlDiff.IgnorePrefixes || Prefix == attr.Prefix ) &&
                 Value == attr.Value );
    }

    // compares the node to another one and returns the xmldiff operation for changing this node to the other
	internal override XmlDiffOperation GetDiffOperation( XmlDiffNode changedNode, XmlDiff xmlDiff )
	{
		Debug.Assert( false, "This method should be never called." );
        return XmlDiffOperation.Undefined;
	}
	
	// Overriden abstract methods for outputting
    internal override void WriteTo( XmlWriter w ) 
	{
        w.WriteStartAttribute( Prefix, LocalName, NamespaceURI );
        WriteContentTo( w );
        w.WriteEndAttribute();
    }

    internal override void WriteContentTo( XmlWriter w ) 
	{
        w.WriteString( Value );
    }

    // Addressing
    internal override string GetRelativeAddress()
    {
        return "@" + Name;
    }

#if DEBUG
    internal override void Dump( string indent )
    {
        Trace.Write( " " + Name + "=" + Value );
    }
#endif
}

//////////////////////////////////////////////////////////////////
// XmlDiffNamespace
//
internal class XmlDiffNamespace : XmlDiffAttributeOrNamespace 
{
// Fields
	string _prefix;
	string _namespaceURI;

// Constructor
    internal XmlDiffNamespace( string prefix, string namespaceURI ) : base( ) 
    {
        Debug.Assert( prefix != null );
        Debug.Assert( namespaceURI != null );

        _prefix = prefix;
        _namespaceURI = namespaceURI;
    }

// Properties
    internal override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.Namespace; } }
    internal override string Prefix { get { return _prefix; } }
    internal override string NamespaceURI { get { return _namespaceURI; } }

    internal override string LocalName { get { return string.Empty; }  }
    internal override string Value { get { return string.Empty; } }

    internal string Name 
	{
        get 
		{
            if ( _prefix.Length > 0 ) 
                return "xmlns:" + _prefix;
            else
                return "xmlns";
        }
    }


// Methods
    // computes hash value of the node and stores it in the _hashValue variable
    internal override void ComputeHashValue( XmlHash xmlHash )
    {
        Debug.Assert( _hashValue == 0 );
        _hashValue = xmlHash.HashNamespace( _prefix, _namespaceURI );
    }

    // compares the node to another one and returns the xmldiff operation for changing this node to the other
	internal override XmlDiffOperation GetDiffOperation( XmlDiffNode changedNode, XmlDiff xmlDiff )
	{
        Debug.Assert( false, "This method should be never called." );
        return XmlDiffOperation.Undefined;
	}
	
	// Overriden abstract methods for outputting
    internal override void WriteTo( XmlWriter w ) 
	{
        if ( Prefix == string.Empty ) {
            w.WriteAttributeString( string.Empty, "xmlns", XmlDiff.XmlnsNamespaceUri, NamespaceURI );
        }
        else {
            w.WriteAttributeString( "xmlns", Prefix, XmlDiff.XmlnsNamespaceUri, NamespaceURI );
        }
    }

    internal override void WriteContentTo( XmlWriter w ) 
	{
        Debug.Assert( false );
    }

    // Addressing
    internal override string GetRelativeAddress()
    {
        return "@" + Name;
    }

#if DEBUG
    internal override void Dump( string indent )
    {
        Trace.WriteLine( indent + "xmlns:" + Prefix + "=" + NamespaceURI );
    }
#endif
}

//////////////////////////////////////////////////////////////////
// XmlDiffER
//
internal class XmlDiffER : XmlDiffNode 
{
// Fields
    string _name;

// Constructor
    internal XmlDiffER( int position, string name ) : base ( position ) 
	{
        _name = name;
    }

// Properties
    internal override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.EntityReference; } }
    
    internal string Name { 
        get { 
            return _name; 
        } 
    }
    
    internal override bool CanMerge { 
        get { 
            return false;
        }
    }

// Methods
    // computes hash value of the node and stores it in the _hashValue variable
    internal override void ComputeHashValue( XmlHash xmlHash )
    {
        Debug.Assert( _hashValue == 0 );
        _hashValue = xmlHash.HashER( _name );
    }

    // compares the node to another one and returns the xmldiff operation for changing this node to the other
	internal override XmlDiffOperation GetDiffOperation( XmlDiffNode changedNode, XmlDiff xmlDiff )
	{
		Debug.Assert( changedNode != null );
        if ( changedNode.NodeType != XmlDiffNodeType.EntityReference )
            return XmlDiffOperation.Undefined;

		if ( Name == ((XmlDiffER)changedNode).Name )
			return XmlDiffOperation.Match;
		else
			return XmlDiffOperation.ChangeER;
	}
	
	// Overriden abstract methods for outputing
    internal override void WriteTo( XmlWriter w ) 
	{
        w.WriteEntityRef( this._name );
    }

    internal override void WriteContentTo( XmlWriter w ) {}

#if DEBUG
    internal override void Dump( string indent )
    {
        Trace.WriteLine( indent + "(" + _index + ") &" + Name );
    }
#endif
}

//////////////////////////////////////////////////////////////////
// XmlDiffCharData
//
internal class XmlDiffCharData : XmlDiffNode 
{
// Fields
    string _value;
    XmlDiffNodeType _nodeType;

// Constructor
    internal XmlDiffCharData( int position, string value, XmlDiffNodeType nodeType ) : base( position ) 
	{
        _value = value;
        _nodeType = nodeType;
    }

// Properties
    internal override XmlDiffNodeType NodeType { get { return _nodeType; } }
    internal string Value { get { return this._value; } }

// Methods
    // computes hash value of the node and stores it in the _hashValue variable
    internal override void ComputeHashValue( XmlHash xmlHash )
    {
        Debug.Assert( _hashValue == 0 );
        _hashValue = xmlHash.HashCharacterNode( (XmlNodeType)(int)_nodeType, _value );
    }

    // compares the node to another one and returns the xmldiff operation for changing this node to the other
	internal override XmlDiffOperation GetDiffOperation( XmlDiffNode changedNode, XmlDiff xmlDiff )
	{
		Debug.Assert( changedNode != null );

		if ( NodeType != changedNode.NodeType )
			return XmlDiffOperation.Undefined;

        XmlDiffCharData changedCD = changedNode as XmlDiffCharData;

		if ( changedCD == null )
			return XmlDiffOperation.Undefined;

		if ( Value == changedCD.Value )
			return XmlDiffOperation.Match;
		else
			return XmlDiffOperation.ChangeCharacterData;
	}

	// Overriden abstract methods for outputing
    internal override void WriteTo( XmlWriter w ) 
	{
        switch ( _nodeType ) 
		{
            case XmlDiffNodeType.Comment:
                w.WriteComment( Value );
                break;
            case XmlDiffNodeType.CDATA:
                w.WriteCData( Value );
                break;
            case XmlDiffNodeType.SignificantWhitespace:
            case XmlDiffNodeType.Text:
                w.WriteString( Value );
                break;
            default :
                Debug.Assert( false, "Wrong type for text-like node : " + this._nodeType.ToString() );
                break;
        }
    }

    internal override void WriteContentTo( XmlWriter w ) {}

#if DEBUG
    internal override void Dump( string indent )
    {
        switch ( _nodeType )
        {
            case XmlDiffNodeType.SignificantWhitespace:
            case XmlDiffNodeType.Text:
                Trace.WriteLine( indent + "(" + _index + ") '" + Value + "'" );
                break;
            case XmlDiffNodeType.Comment:
                Trace.WriteLine( indent + "(" + _index + ") <!--" + Value + "-->" );
                break;
            case XmlDiffNodeType.CDATA:
                Trace.WriteLine( indent + "(" + _index + ") <![CDATA[" + Value + "]]>" );
                break;
            default:
                Debug.Assert( false );
                break;
        }
    }
#endif
}

//////////////////////////////////////////////////////////////////
// XmlDiffPI
//
internal class XmlDiffPI : XmlDiffCharData 
{
// Fields
    string _name;

// Constructor
    internal XmlDiffPI( int position, string name, string value ) 
        : base( position, value, XmlDiffNodeType.ProcessingInstruction ) 
	{
        _name = name;
    }

// Properties
	internal string Name { get { return _name; } } 

// Methods
    // computes the hash value of the node and saves it into the _hashValue field
    internal override void ComputeHashValue( XmlHash xmlHash )
    {
        Debug.Assert( _hashValue == 0 );
        _hashValue = xmlHash.HashPI( Name, Value );
    }

    // compares the node to another one and returns the xmldiff operation for changing this node to the other
	internal override XmlDiffOperation GetDiffOperation( XmlDiffNode changedNode, XmlDiff xmlDiff )
	{
		Debug.Assert( changedNode != null );

        if ( changedNode.NodeType != XmlDiffNodeType.ProcessingInstruction )
            return XmlDiffOperation.Undefined;

		XmlDiffPI changedPI = (XmlDiffPI)changedNode;

		if ( Name == changedPI.Name )
		{
			if ( Value == changedPI.Value ) 
				return XmlDiffOperation.Match;
			else 
				return XmlDiffOperation.ChangePI;
		}
		else
		{
			if ( Value == changedPI.Value )
				return XmlDiffOperation.ChangePI;
			else
				return XmlDiffOperation.Undefined;
		}
	}

    internal override void WriteTo( XmlWriter w ) 
	{
        w.WriteProcessingInstruction( Name, Value );
    }
    
	internal override void WriteContentTo( XmlWriter w ) {}

#if DEBUG
    internal override void Dump( string indent )
    {
        Trace.WriteLine( indent + "(" + _index + ") <?" + Name + " " + Value + "?>" );
    }
#endif
}

//////////////////////////////////////////////////////////////////
// XmlDiffShrankNode
//

internal class XmlDiffShrankNode : XmlDiffNode
{
// Fields
    // interval of nodes it represents
    internal XmlDiffNode _firstNode;
    internal XmlDiffNode _lastNode;

	// matching nodes in target/source tree
	XmlDiffShrankNode _matchingShrankNode;

    // address
    string _localAddress;

	// 'move' operation id
	ulong _opid; 

// Constructor
    internal XmlDiffShrankNode(  XmlDiffNode firstNode, XmlDiffNode lastNode, ulong hashValue ) : base ( -1 )
    {
        Debug.Assert( firstNode != null );
        Debug.Assert( lastNode != null );
        Debug.Assert( firstNode.Position <= lastNode.Position );
        Debug.Assert( firstNode.NodeType != XmlDiffNodeType.Attribute ||
                      (firstNode.NodeType == XmlDiffNodeType.Attribute && firstNode == lastNode ) );

        _firstNode = firstNode;
        _lastNode = lastNode;
		_matchingShrankNode = null;

        _hashValue = hashValue;
        _localAddress = DiffgramOperation.GetRelativeAddressOfNodeset( _firstNode, _lastNode );
    }

// Properties
    internal override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.ShrankNode; } }

    internal XmlDiffShrankNode MatchingShrankNode 
	{
		get { return _matchingShrankNode; }
		set
		{
			Debug.Assert( value != null );
			Debug.Assert( _matchingShrankNode == null );
			_matchingShrankNode = value;
		}
	}

	internal ulong MoveOperationId 
	{
		get 
        { 
            if ( _opid == 0 )
                _opid = MatchingShrankNode._opid; 
            return _opid;
        }
		set 
		{ 
			Debug.Assert( _opid == 0 );
			_opid = value;
		}
	}

    internal override bool CanMerge {
        get {
            return false;
        }
    }   

// Methods
    // computes the hash value of the node and saves it into the _hashValue field
    internal override void ComputeHashValue( XmlHash xmlHash )
    {
        Debug.Assert( false, "This method should bever be called." );
    }

    // compares the node to another one and returns the xmldiff operation for changing this node to the other
    internal override XmlDiffOperation GetDiffOperation( XmlDiffNode changedNode, XmlDiff xmlDiff )
    {
        Debug.Assert( changedNode != null );

        if ( changedNode.NodeType != XmlDiffNodeType.ShrankNode )
            return XmlDiffOperation.Undefined;

        if ( _hashValue == ((XmlDiffShrankNode)changedNode)._hashValue )
            return XmlDiffOperation.Match;
        else
            return XmlDiffOperation.Undefined;
    }

    internal override void WriteTo( XmlWriter w )
    {
        WriteContentTo( w );
    }

    internal override void WriteContentTo( XmlWriter w )
    {
        XmlDiffNode curNode = _firstNode;
        for (;;)
        {
            curNode.WriteTo( w );
            if ( curNode == _lastNode ) 
                break;
            curNode = curNode._nextSibling;
        }
    }

    // Addressing
    internal override string GetRelativeAddress()
    {
        return _localAddress;
    }

#if DEBUG
    internal override void Dump( string indent )
    {
        Trace.Write( indent + "(" + _index + ") shrank nodes: "  );
        XmlDiffNode curNode = _firstNode;
        for (;;)
        {
            Trace.Write( curNode.OuterXml );
            if ( curNode == _lastNode )
                break;
            curNode = curNode._nextSibling;
        }
        Trace.Write( "\n" );
    }
#endif
}

//////////////////////////////////////////////////////////////////
// XmlDiffXmlDeclaration
//
internal class XmlDiffXmlDeclaration : XmlDiffNode 
{
// Fields
	string _value;
    
// Constructors
    internal XmlDiffXmlDeclaration( int position, string value ) : base( position ) 
	{
        Debug.Assert( value != null );
        _value = value;
    }

// Properties
    internal override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.XmlDeclaration; } }
    internal string Value { get { return _value; } }

// Methods
    // computes the hash value of the node and saves it into the _hashValue field
    internal override void ComputeHashValue( XmlHash xmlHash )
    {
        Debug.Assert( _hashValue == 0 );
        _hashValue = xmlHash.HashXmlDeclaration( _value );
    }

    // compares the node to another one and returns the xmldiff operation for changing this node to the other
	internal override XmlDiffOperation GetDiffOperation( XmlDiffNode changedNode, XmlDiff xmlDiff )
	{
		Debug.Assert( changedNode != null );

        if ( changedNode.NodeType != XmlDiffNodeType.XmlDeclaration )
            return XmlDiffOperation.Undefined;

		if (  Value == ((XmlDiffXmlDeclaration)changedNode).Value )
            return XmlDiffOperation.Match;
        else
		    return XmlDiffOperation.ChangeXmlDeclaration;
    }

	// Overriden abstract methods for outputting
    internal override void WriteTo( XmlWriter w ) 
	{
        w.WriteProcessingInstruction( "xml", _value );
    }

    internal override void WriteContentTo( XmlWriter w )
    {
    }

#if DEBUG
    internal override void Dump( string indent )
    {
        Trace.WriteLine( indent + "(" + _index + ") <?xml " + Value + "?>" );
    }
#endif
}

//////////////////////////////////////////////////////////////////
// XmlDiffDoctypeDeclaration
//
internal class XmlDiffDocumentType : XmlDiffNode 
{
// Fields
    string _name;
    string _publicId;
    string _systemId;
	string _subset;
    
// Constructors
    internal XmlDiffDocumentType( int position, string name, string publicId, string systemId, string subset ) : base( position ) 
	{
        Debug.Assert( name != null );
        _name = name;
        _publicId = publicId;
        _systemId = systemId;
        _subset = subset;
    }

// Properties
    internal override XmlDiffNodeType NodeType { get { return XmlDiffNodeType.DocumentType; } }
    internal string Name { get { return _name; } }
    internal string PublicId { get { return _publicId; } }
    internal string SystemId { get { return _systemId; } }

    internal string Subset { get { return _subset; } }

// Methods
    // computes the hash value of the node and saves it into the _hashValue field
    internal override void ComputeHashValue( XmlHash xmlHash )
    {
        Debug.Assert( _hashValue == 0 );
        _hashValue = xmlHash.HashDocumentType( _name, _publicId, _systemId, _subset );
    }

    // compares the node to another one and returns the xmldiff operation for changing this node to the other
	internal override XmlDiffOperation GetDiffOperation( XmlDiffNode changedNode, XmlDiff xmlDiff )
	{
		Debug.Assert( changedNode != null );

        if ( changedNode.NodeType != XmlDiffNodeType.DocumentType )
            return XmlDiffOperation.Undefined;

        XmlDiffDocumentType changedDocType = (XmlDiffDocumentType)changedNode;
		if ( Name == changedDocType.Name && PublicId == changedDocType.PublicId &&
             SystemId == changedDocType.SystemId && Subset == changedDocType.Subset ) {
            return XmlDiffOperation.Match;
        }
        else {
            return XmlDiffOperation.ChangeDTD;
        }
    }

	// Overriden abstract methods for outputting
    internal override void WriteTo( XmlWriter w ) 
	{
        w.WriteDocType( _name, string.Empty, string.Empty, _subset );
    }

    internal override void WriteContentTo( XmlWriter w )
    {
    }

#if DEBUG
    internal override void Dump( string indent )
    {
        Trace.WriteLine( indent + "(" + _index + ") " + Name + "(SYSTEM '" + SystemId + "', PUBLIC '" + PublicId +
                         "') [ " + Subset + " ]" );
    }
#endif
}

}
