//------------------------------------------------------------------------------
// <copyright file="TreeMappingAlgorithm.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace Microsoft.XmlDiffPatch
{
//////////////////////////////////////////////////////////////////
// MinimalTreeDistanceAlgo
//
internal class MinimalTreeDistanceAlgo
{
    //////////////////////////////////////////////////////////////////
    // MinimalTreeDistanceAlgo.Distance
    //
    private struct Distance
    {
        internal int _cost;
        internal EditScript _editScript;
    }

// Fields
    // XmlDiff
    XmlDiff _xmlDiff;

    // nodes in the post-order numbering - cached from XmlDiff 
    XmlDiffNode[] _sourceNodes;
    XmlDiffNode[] _targetNodes;

    // distances between all possible pairs of subtrees
    Distance[,] _treeDist;

    // distances between all possible pairs of forests - used by ComputeTreeDistance
    // method. It is hold here and allocated just once in the FindMinimalDistance method
    // instead of allocating it as a local variable in the ComputeTreeDistance method.
    Distance[,] _forestDist;

// Static fields
    static readonly EditScriptEmpty EmptyEditScript = new EditScriptEmpty();

    // Operation costs
    internal static readonly int[] OperationCost = {
            0,              // Match                      = 0,
            4,              // Add                        = 1,
            4,              // Remove                     = 2,
            1,              // ChangeElementName          = 3,
            1,              // ChangeElementAttr1         = 4,
            2,              // ChangeElementAttr2         = 5,
	        3,              // ChangeElementAttr3         = 6,
	        2,              // ChangeElementNameAndAttr1  = 7,
	        3,              // ChangeElementNameAndAttr2  = 8,
	        4,              // ChangeElementNameAndAttr3  = 9,
            4,              // ChangePI                   = 10,
            4,              // ChangeER                   = 11,
            4,              // ChangeCharacterData        = 12,
            4,              // ChangeXmlDeclaration       = 13,
            4,              // ChangeDTD                  = 14,
            int.MaxValue/2, // Undefined                  = 15,
    };

// Constructor
    internal MinimalTreeDistanceAlgo( XmlDiff xmlDiff ) 
    {
        Debug.Assert( OperationCost.Length - 1 == (int)XmlDiffOperation.Undefined,
                      "Correct the OperationCost array so that it reflects the XmlDiffOperation enum." );

        Debug.Assert( xmlDiff != null );
        _xmlDiff = xmlDiff;
    }

// Methods
    internal EditScript FindMinimalDistance()
    {
        EditScript resultEditScript = null;

        try
        {
            // cache sourceNodes and targetNodes arrays
            _sourceNodes = _xmlDiff._sourceNodes;
            _targetNodes = _xmlDiff._targetNodes;

            // create the _treeDist array - it contains distances between subtrees.
            // The zero-indexed row and column are not used.
            // This is to have the consistent indexing of all arrays in the algorithm;
            // _forestDist array requires 0-indexed border fields for recording the distance 
            // of empty forest.
            _treeDist = new Distance[ _sourceNodes.Length, _targetNodes.Length ];

            // create _forestDist array;
            // Parts of this array are independently used in subsequent calls of ComputeTreeDistance.
            // The array is allocated just once here in the biggest bounds it will ever need
            // instead of allocating it in each call of ComputeTreeDistance as a local variable.
            _forestDist = new Distance[ _sourceNodes.Length, _targetNodes.Length ];

            // the algorithm; computes the _treeDist array
            int i, j;
            for ( i = 1; i < _sourceNodes.Length; i++ )
            {
                if ( _sourceNodes[i].IsKeyRoot )
                {
                    for ( j = 1; j < _targetNodes.Length; j++ )
                    {
                        if ( _targetNodes[j].IsKeyRoot )
                        {
                            ComputeTreeDistance( i, j );
                        }
                    }
                }
            }

            // get the result edit script
            resultEditScript = _treeDist[ _sourceNodes.Length-1, _targetNodes.Length-1 ]._editScript;
        }
        finally
        {
            _forestDist = null; 
            _treeDist = null;
            _sourceNodes = null;
            _targetNodes = null;
        }

        // normalize the found edit script (expands script references etc.)
        return NormalizeScript( resultEditScript );
    }

    private void ComputeTreeDistance( int sourcePos, int targetPos ) 
    {
        int sourcePosLeft = _sourceNodes[ sourcePos ].Left;
        int targetPosLeft = _targetNodes[ targetPos ].Left;
        int i, j;

        // init borders of _forestDist array
        EditScriptAddOpened esAdd = new EditScriptAddOpened( targetPosLeft, EmptyEditScript );
        EditScriptRemoveOpened esRemove = new EditScriptRemoveOpened( sourcePosLeft, EmptyEditScript );

        _forestDist[ sourcePosLeft-1, targetPosLeft-1 ]._cost = 0;
        _forestDist[ sourcePosLeft-1, targetPosLeft-1 ]._editScript = EmptyEditScript;

        for ( i = sourcePosLeft; i <= sourcePos; i++ ) 
        {
            _forestDist[ i, targetPosLeft-1 ]._cost = ( i - sourcePosLeft + 1 ) * OperationCost[ (int)XmlDiffOperation.Remove ];
            _forestDist[ i, targetPosLeft-1 ]._editScript = esRemove;
        }
        for ( j = targetPosLeft; j <= targetPos; j++ )
        {
            _forestDist[ sourcePosLeft-1, j ]._cost = ( j - targetPosLeft + 1 ) * OperationCost[ (int)XmlDiffOperation.Add ];
            _forestDist[ sourcePosLeft-1, j ]._editScript = esAdd;
        }

#if DEBUG
        Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, "\nForest distance (" + sourcePos + "," + targetPos + "):\n" );
        Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, "       0    " );
        for ( j = targetPosLeft; j <= targetPos; j++ )
            Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, j + "    " );
        Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, "\n   " );
        for ( j = targetPosLeft-1; j <= targetPos; j++ )
            Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, "*****" );
        Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, "\n0  *   0   " );
        for ( j = targetPosLeft; j <= targetPos; j++ )
            Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, "-" + _forestDist[ sourcePosLeft-1, j ]._cost + "   " );
        Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, "\n" );
#endif

        // compute the inside of _forestDist array
        for ( i = sourcePosLeft; i <= sourcePos; i++ )
        {
#if DEBUG
            Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, i + "  *  |" + _forestDist[ i, targetPosLeft-1 ]._cost + "   " );
#endif

            for ( j = targetPosLeft; j <= targetPos; j++ )
            {
                int sourceCurLeft = _sourceNodes[i].Left;
                int targetCurLeft = _targetNodes[j].Left;
 
                int removeCost = _forestDist[ i-1, j ]._cost + OperationCost[ (int)XmlDiffOperation.Remove ]; 
                int addCost  = _forestDist[ i, j-1 ]._cost + OperationCost[ (int)XmlDiffOperation.Add ];

                if ( sourceCurLeft == sourcePosLeft  &&  targetCurLeft == targetPosLeft )
                {
                    XmlDiffOperation changeOp = _sourceNodes[i].GetDiffOperation( _targetNodes[j], _xmlDiff );
                    
                    Debug.Assert( XmlDiff.IsChangeOperation( changeOp ) || 
                                  changeOp == XmlDiffOperation.Match ||
                                  changeOp == XmlDiffOperation.Undefined );

                    if ( changeOp == XmlDiffOperation.Match ) 
                    {
                        // identical nodes matched
                        OpNodesMatch( i, j );
                    }
                    else
                    {
                        int changeCost = _forestDist[ i-1, j-1 ]._cost + OperationCost[ (int)changeOp ];

                        if ( changeCost < addCost )
                        {
                            // operation 'change'
                            if ( changeCost < removeCost )
                                OpChange( i, j, changeOp, changeCost );
                            // operation 'remove'
                            else
                                OpRemove( i, j, removeCost );
                        }
                        else
                        {
                            // operation 'add'
                            if ( addCost < removeCost )
                                OpAdd( i, j, addCost );
                            // operation 'remove'
                            else 
                                OpRemove( i, j, removeCost );
                        }
                    }
                        
                    _treeDist[ i, j ]._cost = _forestDist[ i, j ]._cost;
                    _treeDist[ i, j ]._editScript = _forestDist[ i, j ]._editScript.GetClosedScript( i, j );;
                }
                else
                {
                    int m = sourceCurLeft - 1;
                    int n = targetCurLeft - 1;

                    if ( m < sourcePosLeft - 1 ) m = sourcePosLeft - 1;
                    if ( n < targetPosLeft - 1 ) n = targetPosLeft - 1;

                    // cost of concatenating of the two edit scripts
                    int compoundEditCost = _forestDist[ m, n ]._cost + _treeDist[ i, j ]._cost;

                    if ( compoundEditCost < addCost )
                    {
                        if ( compoundEditCost < removeCost )
                        {
                            // copy script
                            if ( _treeDist[ i, j ]._editScript == EmptyEditScript ) 
                            {
                                Debug.Assert( _treeDist[ i, j ]._cost == 0 );
                                OpCopyScript( i, j, m, n );
                            }
                            // concatenate scripts
                            else
                                OpConcatScripts( i, j, m, n );
                        }
                        // operation 'remove'
                        else
                            OpRemove( i, j, removeCost );
                    }
                    else 
                    {
                        // operation 'add'
                        if ( addCost < removeCost) 
                            OpAdd( i, j, addCost );
                        // operation 'remove'
                        else 
                            OpRemove( i, j, removeCost );
                    }
                }
#if DEBUG
                Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, _forestDist[ i, j ]._cost + "   " );
#endif
            }
#if DEBUG
            Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, "\n" );
#endif            
        }
    }

    private void OpChange( int i, int j, XmlDiffOperation changeOp, int cost )
    {
#if DEBUG
        Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, "*" );
#endif
        _forestDist[ i, j ]._editScript = new EditScriptChange( i, j, changeOp, _forestDist[ i-1, j-1 ]._editScript.GetClosedScript( i-1, j-1 ) );
        _forestDist[ i, j ]._cost = cost;
    }

    private void OpAdd( int i, int j, int cost )
    {
#if DEBUG
        Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, "-" );
#endif
        EditScriptAddOpened openedAdd = _forestDist[ i, j-1 ]._editScript as EditScriptAddOpened;

        if ( openedAdd == null )
            openedAdd = new EditScriptAddOpened( j, _forestDist[ i, j-1 ]._editScript.GetClosedScript( i, j-1 ) );

        _forestDist[ i, j ]._editScript = openedAdd;
        _forestDist[ i, j ]._cost = cost;
    }

    private void OpRemove( int i, int j, int cost )
    {
#if DEBUG
        Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, "|" );
#endif
        EditScriptRemoveOpened openedRemove = _forestDist[ i-1, j ]._editScript as EditScriptRemoveOpened;

        if ( openedRemove == null )
            openedRemove  = new EditScriptRemoveOpened( i, _forestDist[ i-1, j ]._editScript.GetClosedScript( i-1, j ) );

        _forestDist[ i, j ]._editScript = openedRemove;
        _forestDist[ i, j ]._cost = cost;
    }

    private void OpNodesMatch( int i, int j )
    {
#if DEBUG
        Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, "\\" );
#endif

        EditScriptMatchOpened openedMatch = _forestDist[ i-1, j-1 ]._editScript as EditScriptMatchOpened;

        if ( openedMatch == null )
            openedMatch  = new EditScriptMatchOpened( i, j, _forestDist[ i-1, j-1 ]._editScript.GetClosedScript( i-1, j-1 ) );

        _forestDist[ i, j ]._editScript = openedMatch;
        _forestDist[ i, j ]._cost = _forestDist[ i-1, j-1 ]._cost;
    }

    private void OpCopyScript( int i, int j,
                               int m, int n )
    {
#if DEBUG
        Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, "&" );
#endif
        _forestDist[ i, j ]._cost = _forestDist[ m, n ]._cost;
        _forestDist[ i, j ]._editScript = _forestDist[ m, n ]._editScript.GetClosedScript( m, n );
    }

    private void OpConcatScripts( int i, int j, int m, int n )
    {
#if DEBUG
        Trace.WriteIf( XmlDiff.T_ForestDistance.Enabled, "&" );
#endif
        _forestDist[ i, j ]._editScript = new EditScriptReference( _treeDist[ i, j ]._editScript, _forestDist[ m, n ]._editScript.GetClosedScript( m, n ));
        _forestDist[ i, j ]._cost = _treeDist[ i, j ]._cost + _forestDist[ m, n ]._cost;

    }

// Static methods
    // This method expands 'reference edit script' items and removes the last item 
    // (which is the static instance of EmptyEditScript).
    static private EditScript NormalizeScript( EditScript es )
    {
        EditScript returnES = es;
        EditScript curES = es;
        EditScript prevES = null;

        while ( curES != EmptyEditScript )
        {
            Debug.Assert( curES != null );

            if ( curES.Operation != EditScriptOperation.EditScriptReference )
            {
                prevES = curES;
                curES = curES._nextEditScript;
            }
            else
            {
                EditScriptReference refES = curES as EditScriptReference;
                
                EditScript lastES = refES._editScriptReference;
                Debug.Assert( lastES != EmptyEditScript  && lastES != null );
                while ( lastES.Next != EmptyEditScript )
                {
                    lastES = lastES._nextEditScript;
                    Debug.Assert( lastES != null );
                }

                lastES._nextEditScript = curES._nextEditScript;
                curES = refES._editScriptReference;

                if ( prevES == null ) 
                    returnES = curES;
                else
                    prevES._nextEditScript = curES;
            }
        }

        if ( prevES != null ) 
            prevES._nextEditScript = null;
        else
            returnES = null;

        return returnES;
    }
}
}


