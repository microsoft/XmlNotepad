//------------------------------------------------------------------------------
// <copyright file="EditScript.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace Microsoft.XmlDiffPatch
{

//////////////////////////////////////////////////////////////////
// EditScriptOperation enum
//
internal enum EditScriptOperation
{
    None                 = 0,
    Match                = 1,
    Add                  = 2,
    Remove               = 3,
    ChangeNode           = 4,
    EditScriptReference  = 5,
    EditScriptPostponed  = 6,

    OpenedAdd            = 7,
    OpenedRemove         = 8,
    OpenedMatch          = 9,
}

//////////////////////////////////////////////////////////////////
// EditScript
//
internal abstract class EditScript
{
// Fields
    internal EditScript _nextEditScript;

// Constructor
    internal EditScript( EditScript next)
    {
        _nextEditScript = next;
    }

// Properties
    internal abstract EditScriptOperation Operation { get; }
    internal EditScript Next { get { return _nextEditScript; } }

// Methods
    internal virtual EditScript GetClosedScript( int currentSourceIndex, int currentTargetIndex )
    {
        Debug.Assert( this as EditScriptOpened == null );
        return this;
    }

#if DEBUG
    internal abstract void Dump();
#endif
}

//////////////////////////////////////////////////////////////////
// EditScriptEmpty
//

internal class EditScriptEmpty : EditScript
{
// Constructor
    internal EditScriptEmpty() : base( null ) {}

// Properties
    internal override EditScriptOperation Operation { get { return EditScriptOperation.None; } }

// Methods
#if DEBUG
    internal override void Dump()
    {
        Trace.WriteLine( "empty" );
        if ( _nextEditScript != null ) _nextEditScript.Dump();
    }
#endif
}

//////////////////////////////////////////////////////////////////
// EditScriptMatch
//
internal class EditScriptMatch : EditScript
{
// Fields
    internal int _firstSourceIndex; 
    internal int _firstTargetIndex;
    internal int _length;

// Constructor
    internal EditScriptMatch( int startSourceIndex, int startTargetIndex, 
                              int length, EditScript next )
        : base ( next )
    {
        Debug.Assert( length > 0 );
        Debug.Assert( startSourceIndex > 0 );
        Debug.Assert( startTargetIndex > 0 );

        _firstSourceIndex = startSourceIndex;
        _firstTargetIndex = startTargetIndex;
        _length = length;
    }

// Properties
    internal override EditScriptOperation Operation { get { return EditScriptOperation.Match; } }

// Methods
#if DEBUG
    internal override void Dump()
    {
        if ( _length > 1 )
            Trace.WriteLine( "match s" + _firstSourceIndex + "-s" + (_firstSourceIndex + _length - 1).ToString() + 
                               " to t" + _firstTargetIndex + "-t" + (_firstTargetIndex + _length - 1).ToString() );
        else
            Trace.WriteLine( "match s" + _firstSourceIndex + " to t" + _firstTargetIndex );

        if ( _nextEditScript != null ) _nextEditScript.Dump();
    }
#endif
}

//////////////////////////////////////////////////////////////////
// EditScriptAdd
//
internal class EditScriptAdd : EditScript
{
// Fields
    internal int _startTargetIndex;
    internal int _endTargetIndex;

// Constructor
    internal EditScriptAdd( int startTargetIndex, int endTargetIndex, EditScript next )
        : base ( next )
    {
        Debug.Assert( endTargetIndex - startTargetIndex >= 0 );
        Debug.Assert( startTargetIndex > 0 );
        Debug.Assert( endTargetIndex > 0 );

        _startTargetIndex = startTargetIndex;
        _endTargetIndex = endTargetIndex;
    }

// Properties
    internal override EditScriptOperation Operation { get { return EditScriptOperation.Add; } }

// Methods
#if DEBUG
    internal override void Dump()
    {
        if ( _endTargetIndex - _startTargetIndex > 0 )
            Trace.WriteLine( "add t" + _startTargetIndex + "-t" + _endTargetIndex );
        else
            Trace.WriteLine( "add t" + _startTargetIndex );

        if (_nextEditScript != null ) _nextEditScript.Dump();
    }
#endif
}

//////////////////////////////////////////////////////////////////
// EditScriptRemove
//
internal class EditScriptRemove : EditScript
{
// Fields
    internal int _startSourceIndex;
    internal int _endSourceIndex;

// Constructor
    internal EditScriptRemove( int startSourceIndex, int endSourceIndex, EditScript next )
        : base ( next )
    {
        Debug.Assert( endSourceIndex - startSourceIndex >= 0 );
        Debug.Assert( startSourceIndex > 0 );
        Debug.Assert( endSourceIndex > 0 );

        _startSourceIndex = startSourceIndex;
        _endSourceIndex = endSourceIndex;
    }

// Properties
    internal override EditScriptOperation Operation { get { return EditScriptOperation.Remove; } }

// Methods
#if DEBUG
    internal override void Dump()
    {
        if ( _endSourceIndex - _startSourceIndex > 0 )
            Trace.WriteLine( "remove s" + _startSourceIndex + "-s" + _endSourceIndex );
        else
            Trace.WriteLine( "remove s" + _startSourceIndex );

        if (_nextEditScript != null ) _nextEditScript.Dump();
    }
#endif
}

//////////////////////////////////////////////////////////////////
// EditScriptChange
//
internal class EditScriptChange : EditScript
{
// Fields
    internal int _sourceIndex, _targetIndex;
    internal XmlDiffOperation _changeOp;

// Constructor
    internal EditScriptChange( int sourceIndex, int targetIndex, XmlDiffOperation changeOp, EditScript next )
        : base( next )
    {
        Debug.Assert( sourceIndex > 0 );
        Debug.Assert( targetIndex > 0 );
        Debug.Assert( XmlDiff.IsChangeOperation( changeOp) );

        _sourceIndex = sourceIndex;
        _targetIndex = targetIndex;
        _changeOp = changeOp;
    }

// Properties
    internal override EditScriptOperation Operation { get { return EditScriptOperation.ChangeNode; } }

// Methods
#if DEBUG
    internal override void Dump()
    {
        Trace.WriteLine( "change s" + _sourceIndex + " to t" + _targetIndex );
        if ( _nextEditScript != null ) _nextEditScript.Dump();
    }
#endif
}

//////////////////////////////////////////////////////////////////
// EditScriptOpened
//
internal abstract class EditScriptOpened : EditScript
{
// Constructor
    internal EditScriptOpened( EditScript next ) : base ( next ) {}
}

//////////////////////////////////////////////////////////////////
// EditScriptMatchOpened
//
internal class EditScriptMatchOpened : EditScriptOpened
{
// Fields
    internal int _startSourceIndex;
    internal int _startTargetIndex;

// Constructor
    internal EditScriptMatchOpened( int startSourceIndex, int startTargetIndex, EditScript next )
        : base ( next )
    {
        Debug.Assert( startSourceIndex > 0 );
        Debug.Assert( startTargetIndex > 0 );

        _startSourceIndex = startSourceIndex;
        _startTargetIndex = startTargetIndex;
    }

// Properties
    internal override EditScriptOperation Operation { get { return EditScriptOperation.OpenedMatch; } }

// Methods
    internal override EditScript GetClosedScript( int currentSourceIndex, int currentTargetIndex )
    {
        Debug.Assert( _startSourceIndex - currentSourceIndex == _startTargetIndex - currentTargetIndex );
        Debug.Assert( currentSourceIndex - _startSourceIndex >= 0 );
        Debug.Assert( currentTargetIndex - _startTargetIndex >= 0 );

        return new EditScriptMatch( _startSourceIndex, _startTargetIndex,
                                            currentSourceIndex - _startSourceIndex + 1,
                                            _nextEditScript );
    }

#if DEBUG
    internal override void Dump()
    {
        Trace.WriteLine( "opened match: s" + _startSourceIndex + " to t" + _startTargetIndex );
        if ( _nextEditScript != null ) _nextEditScript.Dump();
    }
#endif
}

//////////////////////////////////////////////////////////////////
// EditScriptAddOpened
//
internal class EditScriptAddOpened : EditScriptOpened
{
// Fields
    internal int _startTargetIndex;

// Constructor
    internal EditScriptAddOpened( int startTargetIndex, EditScript next )
        : base ( next )
    {
        Debug.Assert( startTargetIndex > 0 );
        _startTargetIndex = startTargetIndex;
    }

// Properties
    internal override EditScriptOperation Operation { get { return EditScriptOperation.OpenedAdd; } }

// Methods
    internal override EditScript GetClosedScript( int currentSourceIndex, int currentTargetIndex )
    {
        Debug.Assert( currentTargetIndex - _startTargetIndex >= 0 );
        return new EditScriptAdd( _startTargetIndex, currentTargetIndex, _nextEditScript );
    }

#if DEBUG
    internal override void Dump()
    {
        Trace.WriteLine( "opened add: t" + _startTargetIndex );
        if ( _nextEditScript != null ) _nextEditScript.Dump();
    }
#endif
}

//////////////////////////////////////////////////////////////////
// EditScriptRemoveOpened
//
internal class EditScriptRemoveOpened : EditScriptOpened
{
// Fields
    internal int _startSourceIndex;

// Constructor
    internal EditScriptRemoveOpened( int startSourceIndex, EditScript next )
        : base ( next )
    {
        Debug.Assert( startSourceIndex > 0 );
        _startSourceIndex = startSourceIndex;
    }

// Properties
    internal override EditScriptOperation Operation { get { return EditScriptOperation.OpenedRemove; } }

// Methods
    internal override EditScript GetClosedScript( int currentSourceIndex, int currentTargetIndex )
    {
        Debug.Assert( currentSourceIndex - _startSourceIndex >= 0 );
        return new EditScriptRemove( _startSourceIndex, currentSourceIndex, _nextEditScript );
    }

#if DEBUG
    internal override void Dump()
    {
        Trace.WriteLine( "opened remove: s" + _startSourceIndex );
        if ( _nextEditScript != null ) _nextEditScript.Dump();
    }
#endif
}

//////////////////////////////////////////////////////////////////
// EditScriptReference
//
internal class EditScriptReference : EditScript
{
// Fields
    internal EditScript _editScriptReference;

// Constructor
    internal EditScriptReference( EditScript editScriptReference, EditScript next ) : base( next )
    {
        Debug.Assert( editScriptReference != null );
        Debug.Assert( next != null );

        _editScriptReference = editScriptReference;
    }

// Properties
    internal override EditScriptOperation Operation { get { return EditScriptOperation.EditScriptReference; } }   

// Methods
#if DEBUG
    internal override void Dump()
    {
        Trace.WriteLine( "REFERENCE EDIT SCRIPT - start" );
        _editScriptReference.Dump();
        Trace.WriteLine( "REFERENCE EDIT SCRIPT - end" );
        if ( _nextEditScript != null ) _nextEditScript.Dump();
    }
#endif
}    

//////////////////////////////////////////////////////////////////
// EditScriptReference
//
internal class EditScriptPostponed : EditScript
{
// Fields
    internal DiffgramOperation _diffOperation;
    internal int _startSourceIndex;
    internal int _endSourceIndex;

// Constructor
    internal EditScriptPostponed( DiffgramOperation diffOperation, int startSourceIndex, int endSourceIndex ) : base( null )
    {
        Debug.Assert( diffOperation != null );
        Debug.Assert( startSourceIndex > 0 );
        Debug.Assert( endSourceIndex > 0 );

        _diffOperation = diffOperation;
        _startSourceIndex = startSourceIndex;
        _endSourceIndex = endSourceIndex;
    }

// Properties
    internal override EditScriptOperation Operation { get { return EditScriptOperation.EditScriptPostponed; } }   

// Methods
#if DEBUG
    internal override void Dump()
    {
        Trace.WriteLine( "postponed edit script: s" + _startSourceIndex + "-s" + _endSourceIndex );
        if ( _nextEditScript != null ) _nextEditScript.Dump();
    }
#endif
}    


};