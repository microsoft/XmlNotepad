//------------------------------------------------------------------------------
// <copyright file="Diffgram.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.Xml;
using System.Diagnostics;

namespace Microsoft.XmlDiffPatch
{
//////////////////////////////////////////////////////////////////
// Diffgram
//
internal class Diffgram : DiffgramParentOperation
{
// Fields
    XmlDiff _xmlDiff;
    
    OperationDescriptor _descriptors;

// Constructor
    internal Diffgram( XmlDiff xmlDiff ) : base( 0 )
    {
        _xmlDiff = xmlDiff;
    }

// Properties
    internal override XmlDiffOperation Operation {
        get {
            return XmlDiffOperation.Undefined;
        }
    }


// Methods
    internal void AddDescriptor( OperationDescriptor desc )
    {
        desc._nextDescriptor = _descriptors;
        _descriptors = desc;
    }

    internal override void WriteTo( XmlWriter xmlWriter, XmlDiff xmlDiff ) {
        _xmlDiff = xmlDiff;
        WriteTo( xmlWriter );
    }

    internal void WriteTo( XmlWriter xmlWriter )
    {
        Debug.Assert( _xmlDiff._fragments != TriStateBool.DontKnown );

        xmlWriter.WriteStartDocument();

        xmlWriter.WriteStartElement( XmlDiff.Prefix, "xmldiff", XmlDiff.NamespaceUri );
        xmlWriter.WriteAttributeString( "version", "1.0" );
        xmlWriter.WriteAttributeString( "srcDocHash", _xmlDiff._sourceDoc.HashValue.ToString() );
        xmlWriter.WriteAttributeString( "options", _xmlDiff.GetXmlDiffOptionsString() );
        xmlWriter.WriteAttributeString( "fragments", ( _xmlDiff._fragments == TriStateBool.Yes ) ? "yes" : "no" ) ;

        WriteChildrenTo( xmlWriter, _xmlDiff );

        OperationDescriptor curOD = _descriptors;
        while ( curOD != null )
        {
            curOD.WriteTo( xmlWriter );
            curOD = curOD._nextDescriptor;
        }

        xmlWriter.WriteEndElement(); 
        xmlWriter.WriteEndDocument();
    }
}

}
