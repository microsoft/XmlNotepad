//------------------------------------------------------------------------------
// <copyright file="HashAlgorithm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Microsoft.XmlDiffPatch
{
//////////////////////////////////////////////////////////////////
// HashAlgorithm
//
internal class HashAlgorithm
{
// Fields
    ulong _hash;

// Constructor
	internal HashAlgorithm()
	{
	}

// Properties
    internal ulong Hash { get { return _hash; } } 

// Methods
    static internal ulong GetHash( string data )
    {
        return GetHash( data, 0 );
    }

    internal void AddString( string data )
    {
        _hash = GetHash( data, _hash );
    }

    internal void AddInt( int i )
    {
        _hash += ( _hash << 11 ) + (ulong)i;
    }

    internal void AddULong( ulong u )
    {
        _hash += ( _hash << 11 ) + u;
    }

    static private ulong GetHash( string data, ulong hash )
    {
        hash += ( hash << 13 ) + (ulong)data.Length;
        for ( int i = 0; i < data.Length; i++ )
            hash += ( hash << 17 ) + data[i];
        return hash;
    }

}

}
