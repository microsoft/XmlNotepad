//------------------------------------------------------------------------------
// <copyright file="OperationDescriptor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Xml;

namespace Microsoft.XmlDiffPatch
{

//////////////////////////////////////////////////////////////////
// OperationDescriptor
//
internal abstract class OperationDescriptor
{
// Fields
    protected ulong _operationID;
    internal OperationDescriptor _nextDescriptor;

// Constructor
	internal OperationDescriptor( ulong opid )
	{
        Debug.Assert( opid > 0 );
        _operationID = opid;
	}

// Properties
    internal abstract string Type { get; }

// Methods
    internal virtual void WriteTo( XmlWriter xmlWriter )
    {
        xmlWriter.WriteStartElement( XmlDiff.Prefix, "descriptor", XmlDiff.NamespaceUri );
        xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );
        xmlWriter.WriteAttributeString( "type", Type );
        xmlWriter.WriteEndElement();
    }
}

//////////////////////////////////////////////////////////////////
// OperationDescMove
//
internal class OperationDescrMove : OperationDescriptor
{
// Constructor
	internal OperationDescrMove( ulong opid ) : base ( opid )
	{
	}

// Properties
    internal override string Type { get { return "move"; } }
}

//////////////////////////////////////////////////////////////////
// OperationDescNamespaceChange
//
internal class OperationDescrNamespaceChange : OperationDescriptor
{
// Fields
    DiffgramGenerator.NamespaceChange _nsChange;
    
// Constructor
	internal OperationDescrNamespaceChange( DiffgramGenerator.NamespaceChange nsChange ) 
        : base ( nsChange._opid )
	{
        _nsChange = nsChange;
	}

// Properties
    internal override string Type { get { return "namespace change"; } }

// Methods
    internal override void WriteTo( XmlWriter xmlWriter )
    {
        xmlWriter.WriteStartElement( XmlDiff.Prefix, "descriptor", XmlDiff.NamespaceUri );
        xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );
        xmlWriter.WriteAttributeString( "type", Type );

        xmlWriter.WriteAttributeString( "prefix", _nsChange._prefix );
        xmlWriter.WriteAttributeString( "oldNs", _nsChange._oldNS );
        xmlWriter.WriteAttributeString( "newNs", _nsChange._newNS );

        xmlWriter.WriteEndElement();
    }
}

//////////////////////////////////////////////////////////////////
// OperationDescPrefixChange
//
internal class OperationDescrPrefixChange : OperationDescriptor
{
// Fields
    DiffgramGenerator.PrefixChange _prefixChange;
    
// Constructor
	internal OperationDescrPrefixChange( DiffgramGenerator.PrefixChange prefixChange ) 
        : base ( prefixChange._opid )
	{
        _prefixChange = prefixChange;
	}

// Properties
    internal override string Type { get { return "prefix change"; } }

// Methods
    internal override void WriteTo( XmlWriter xmlWriter )
    {
        xmlWriter.WriteStartElement( XmlDiff.Prefix, "descriptor", XmlDiff.NamespaceUri );
        xmlWriter.WriteAttributeString( "opid", _operationID.ToString() );
        xmlWriter.WriteAttributeString( "type", Type );

        xmlWriter.WriteAttributeString( "ns", _prefixChange._NS );
        xmlWriter.WriteAttributeString( "oldPrefix", _prefixChange._oldPrefix );
        xmlWriter.WriteAttributeString( "newPrefix", _prefixChange._newPrefix );

        xmlWriter.WriteEndElement();
    }
}
}
