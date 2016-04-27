//------------------------------------------------------------------------------
// <copyright file="XmlPatchNodeList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.Xml;
using System.Diagnostics;
using System.Collections;

namespace Microsoft.XmlDiffPatch
{

//////////////////////////////////////////////////////////////////
// XmlPatchNodeList
//
internal abstract class XmlPatchNodeList : XmlNodeList 
{
    internal abstract void AddNode( XmlNode node );
}

//////////////////////////////////////////////////////////////////
// XmlPatchNodeList
//
internal class MultiNodeList : XmlPatchNodeList
{
    internal class ListChunk 
    {
        internal const int ChunkSize = 10;

        internal XmlNode[] _nodes = new XmlNode[ ChunkSize ];
        internal int _count = 0;
        internal ListChunk _next = null;

        internal XmlNode this[int i] { get { return _nodes[i]; } }

        internal void AddNode( XmlNode node )
        {
            Debug.Assert( _count < ChunkSize );
            _nodes[ _count++ ] = node;
        }
    }

    private class Enumerator : IEnumerator
    {
        private MultiNodeList _nodeList;
        private ListChunk _currentChunk;
        private int _currentChunkIndex = 0;

        internal Enumerator( MultiNodeList nodeList )
        {
            _nodeList = nodeList;
            _currentChunk = nodeList._chunks;
        }

        public object Current
        {
            get 
            { 
                if ( _currentChunk == null )
                    return null;
                else
                    return _currentChunk[ _currentChunkIndex ];
            }
        }

        public bool MoveNext()
        {
            if ( _currentChunk == null )
                return false;

            if ( _currentChunkIndex >= _currentChunk._count - 1 )
            {
                if ( _currentChunk._next == null )
                    return false;
                else
                {
                    _currentChunk = _currentChunk._next;
                    _currentChunkIndex = 0;
                    Debug.Assert( _currentChunk._count > 0 );
                    return true;
                }
            }
            else
            {
                _currentChunkIndex++;
                return true;
            }
        }

        public void Reset()
        {
            _currentChunk = _nodeList._chunks;
            _currentChunkIndex = -1;
        }
    }

// Fields
    int _count = 0;
    internal ListChunk _chunks = null;
    ListChunk _lastChunk = null;

// Constructor
	internal MultiNodeList()
	{
	}

// Overriden methods
    public override XmlNode Item(int index)
    {
        if ( _chunks == null )
            return null;

        if ( index < ListChunk.ChunkSize )
            return _chunks[ index ];

        int chunkNo = index / ListChunk.ChunkSize;
        ListChunk curChunk = _chunks;
        while ( chunkNo > 0 )
        {
            curChunk = curChunk._next;
            chunkNo--;

            Debug.Assert( curChunk != null );
        }

        return curChunk[ index % ListChunk.ChunkSize ]; 
    }

    public override int Count 
    { 
        get { return _count; } 
    }

    public override IEnumerator GetEnumerator()
    {
        return new Enumerator( this );
    }

// Methods
    internal override void AddNode( XmlNode node )
    {
        if ( _lastChunk == null )
        {
            _chunks = new ListChunk();
            _lastChunk = _chunks;
        }
        else if ( _lastChunk._count == ListChunk.ChunkSize )
        {
            _lastChunk._next = new ListChunk();
            _lastChunk = _lastChunk._next;
        }

        _lastChunk.AddNode( node );
        _count++;
    }
}

//////////////////////////////////////////////////////////////////
// SingleNodeList
//
internal class SingleNodeList : XmlPatchNodeList
{
    private class Enumerator : IEnumerator
    {
        enum State 
        { 
            BeforeNode = 0,
            OnNode = 1,
            AfterNode = 2
        }

        private XmlNode _node;
        private State _state = State.BeforeNode;

        internal Enumerator( XmlNode node )
        {
            _node = node;
        }

        public object Current 
        {
            get { return ( _state == State.OnNode ) ? _node : null; } 
        }

        public bool MoveNext()
        {
            switch ( _state ) 
            {
                case State.BeforeNode:
                    _state = State.OnNode;
                    return true;
                case State.OnNode:
                    _state = State.AfterNode;
                    return false;
                case State.AfterNode:
                    return false;
                default:
                    return false;
            }
        }

        public void Reset()
        {
            _state = State.BeforeNode;
        }
    }

// Fields
    XmlNode _node;

// Constructor
	internal SingleNodeList ()
	{
	}

// Overriden methods
    public override XmlNode Item(int index)
    {
        if ( index == 0 )
            return _node;
        else
            return null;
    }

    public override int Count 
    { 
        get { return 1; } 
    }

    public override IEnumerator GetEnumerator()
    {
        return new Enumerator( _node );
    }

    internal override void AddNode( XmlNode node )
    {
        Debug.Assert( _node == null );
        if ( _node != null )
            XmlPatchError.Error( XmlPatchError.InternalErrorMoreThanOneNodeInList );
        _node = node;
    }
}
}

