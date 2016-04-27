//------------------------------------------------------------------------------
// <copyright file="DiffgramGenerator.cs" company="Microsoft">
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
// DiffgramGenerator
//
internal class DiffgramGenerator
{
    //////////////////////////////////////////////////////////////////
    // DiffgramGenerator.SubstituteEditScript
    //
    internal struct PostponedEditScriptInfo
    {
        internal EditScriptPostponed _firstES;
        internal EditScriptPostponed _lastES;
        internal int _startSourceIndex;
        internal int _endSourceIndex;

        internal void Reset()
        {
            _firstES = null;
            _lastES = null;
            _startSourceIndex = 0;
            _endSourceIndex = 0;
        }
    }

    internal class PrefixChange
    {
        internal string _oldPrefix;
        internal string _newPrefix;
        internal string _NS;
        internal ulong _opid;
        internal PrefixChange _next;

        internal PrefixChange( string oldPrefix, string newPrefix, string ns, ulong opid, 
                             PrefixChange next )
        {
            _oldPrefix = oldPrefix;
            _newPrefix = newPrefix;
            _NS = ns;
            _opid = opid;
            _next = next;
        }
    }

    internal class NamespaceChange
    {
        internal string _prefix;
        internal string _oldNS;
        internal string _newNS;
        internal ulong _opid;
        internal NamespaceChange _next;

        internal NamespaceChange( string prefix, string oldNamespace, string newNamespace, 
                                ulong opid, NamespaceChange next )
        {
            _prefix = prefix;
            _oldNS = oldNamespace;
            _newNS = newNamespace;
            _opid = opid;
            _next = next;
        }
    }

// Fields for both generation methods
    XmlDiff _xmlDiff;

    // cached !XmlDiff.IgnoreChildOrder
    bool _bChildOrderSignificant;

    // Descriptors & operation IDs
    ulong _lastOperationID;

    // 'move' descriptors
    internal const int MoveHashtableInitialSize = 8;
    internal Hashtable _moveDescriptors = new Hashtable( MoveHashtableInitialSize );

    // 'prefix' descriptors
    PrefixChange _prefixChangeDescr = null;
    // 'namespace' descriptors
    NamespaceChange _namespaceChangeDescr = null;

// Fields for generation from edit script:
    // processed edit script
    EditScript _editScript;
    
    // nodes in the post-order numbering - cached from XmlDiff 
    XmlDiffNode[] _sourceNodes;
    XmlDiffNode[] _targetNodes;

    // current processed nodes
    int _curSourceIndex;
    int _curTargetIndex;

    // substitute edit script 
    PostponedEditScriptInfo _postponedEditScript;
    bool _bBuildingAddTree = false;

    // cached DiffgramPosition object
    DiffgramPosition _cachedDiffgramPosition = new DiffgramPosition( null );

// Fields for generation from walktree
    private const int LogMultiplier = 4;

// Constructor
	internal DiffgramGenerator( XmlDiff xmlDiff ) 
    {
        Debug.Assert( xmlDiff != null );

        _xmlDiff = xmlDiff;
        _bChildOrderSignificant = !xmlDiff.IgnoreChildOrder;

        _lastOperationID = 0;
    }

// Methods
    internal Diffgram GenerateFromEditScript( EditScript editScript )
    {
        Debug.Assert( editScript != null );

        Debug.Assert( _xmlDiff._sourceNodes != null );
        Debug.Assert( _xmlDiff._targetNodes != null );
        _sourceNodes = _xmlDiff._sourceNodes;
        _targetNodes = _xmlDiff._targetNodes;

        Diffgram diffgram = new Diffgram( _xmlDiff );

        // root nodes always match; remove them from the edit script
        EditScriptMatch esm = editScript as EditScriptMatch;
        if ( editScript.Operation == EditScriptOperation.Match  && 
             ( esm._firstSourceIndex + esm._length == _sourceNodes.Length  &&
               esm._firstTargetIndex + esm._length == _targetNodes.Length ) )
        {
            esm._length--;
            if ( esm._length == 0 )
                editScript = esm._nextEditScript;
        }
        else
            Debug.Assert( false, "The root nodes does not match!" );

        // init globals
        _curSourceIndex = _sourceNodes.Length - 2;
        _curTargetIndex = _targetNodes.Length - 2;
        _editScript = editScript;

        // generate diffgram
        GenerateDiffgramMatch( diffgram, 1, 1 );

        // add descriptors
        AppendDescriptors( diffgram );

        return diffgram;
    }

    private void AppendDescriptors( Diffgram diffgram ) {
        IDictionaryEnumerator en = _moveDescriptors.GetEnumerator();
        while ( en.MoveNext() )
            diffgram.AddDescriptor( new OperationDescrMove( (ulong)en.Value ) );
        NamespaceChange nsChange = _namespaceChangeDescr;
        while ( nsChange != null )
        {
            diffgram.AddDescriptor( new OperationDescrNamespaceChange( nsChange ) );
            nsChange = nsChange._next;
        }

        PrefixChange prefixChange = _prefixChangeDescr;
        while ( prefixChange != null )
        {
            diffgram.AddDescriptor( new OperationDescrPrefixChange( prefixChange ) );
            prefixChange = prefixChange._next;
        }
    }

    private void GenerateDiffgramMatch( DiffgramParentOperation parent, int sourceBorderIndex, int targetBorderIndex )
    {
        bool bNeedPosition = false;

        while ( _curSourceIndex >= sourceBorderIndex  ||
                _curTargetIndex >= targetBorderIndex )
        {
            Debug.Assert( _editScript != null );

            switch ( _editScript.Operation )
            {
                case EditScriptOperation.Match:
                    OnMatch( parent, bNeedPosition );
                    bNeedPosition = false;
                    break;
                case EditScriptOperation.Add:
                    bNeedPosition = OnAdd( parent, sourceBorderIndex, targetBorderIndex );
                    break;
                case EditScriptOperation.Remove:
                    if ( _curSourceIndex < sourceBorderIndex )
                        return;
                    OnRemove( parent );
                    break;
                case EditScriptOperation.ChangeNode:
                    if ( _curSourceIndex < sourceBorderIndex )
                        return;
                    OnChange( parent );
                    break;
                case EditScriptOperation.EditScriptPostponed:
                    if ( _curSourceIndex < sourceBorderIndex )
                        return;
                    OnEditScriptPostponed( parent, targetBorderIndex );
                    break;
                default:
                    Debug.Assert( false, "Invalid edit script operation type in final edit script." );
                    break;
            }
        }
    }

    private void GenerateDiffgramAdd( DiffgramParentOperation parent, int sourceBorderIndex, int targetBorderIndex )
    {
        while ( _curTargetIndex >= targetBorderIndex )
        {
            Debug.Assert( _editScript != null );

            switch ( _editScript.Operation )
            {
                case EditScriptOperation.Match:
                    OnMatch( parent, false );
                    break;
                case EditScriptOperation.Add:
                    OnAdd( parent, sourceBorderIndex, targetBorderIndex );
                    break;
                case EditScriptOperation.Remove:
                    OnRemove( parent );
                    break;
                case EditScriptOperation.ChangeNode:
                    OnChange( parent );
                    break;
                case EditScriptOperation.EditScriptPostponed:
                    OnEditScriptPostponed( parent, targetBorderIndex );
                    break;
                default:
                    Debug.Assert( false, "Invalid edit script operation type in final edit script." );
                    break;
            }
        }
    }

    private void GenerateDiffgramPostponed( DiffgramParentOperation parent, ref EditScript editScript, int sourceBorderIndex, int targetBorderIndex )
    {
        while ( _curSourceIndex >= sourceBorderIndex  &&  editScript != null )
        {
            EditScriptPostponed esp = editScript as EditScriptPostponed;
            if ( esp == null ) {
                GenerateDiffgramMatch( parent, sourceBorderIndex, targetBorderIndex );
                return;
            }
                
            Debug.Assert( esp._endSourceIndex == _curSourceIndex );
            
            int sourceStartIndex = esp._startSourceIndex;
            int sourceLeft = _sourceNodes[ esp._endSourceIndex ].Left;
            DiffgramOperation diffOp = esp._diffOperation;

            // adjust current source index
            _curSourceIndex = esp._startSourceIndex - 1;
           
            // move to next edit script
            editScript = esp._nextEditScript;

            // not a subtree or leaf node operation -> process child operations
            if ( sourceStartIndex > sourceLeft ) {
                GenerateDiffgramPostponed( (DiffgramParentOperation) diffOp, ref editScript, sourceLeft, targetBorderIndex );
            }

            // insert operation
            parent.InsertAtBeginning( diffOp );
        }
    }

    private void OnMatch( DiffgramParentOperation parent, bool bNeedPosition )
    {
        EditScriptMatch matchOp = _editScript as EditScriptMatch;

        Debug.Assert( _curSourceIndex == matchOp._firstSourceIndex + matchOp._length - 1 );
        Debug.Assert( _curTargetIndex == matchOp._firstTargetIndex + matchOp._length - 1 );

        // cache
        int endTargetIndex = matchOp._firstTargetIndex + matchOp._length - 1;
        int endSourceIndex = matchOp._firstSourceIndex + matchOp._length - 1;
        XmlDiffNode targetRoot = _targetNodes[ endTargetIndex ];
        XmlDiffNode sourceRoot = _sourceNodes[ endSourceIndex ];

        // a subtree or leaf node matches
        if ( matchOp._firstTargetIndex <= targetRoot.Left  &&
             matchOp._firstSourceIndex <= sourceRoot.Left )
        {
            if ( _bBuildingAddTree )
            {
                Debug.Assert( !bNeedPosition );

                ulong opid = GenerateOperationID( XmlDiffDescriptorType.Move );

                // output <add match=" "> to diffgram and "remove" to substitute script
                parent.InsertAtBeginning( new DiffgramCopy( sourceRoot, true, opid ) );

                // add 'remove' operation to postponed operations
                PostponedRemoveSubtrees( sourceRoot, opid, 
                //AddToPosponedOperations( new DiffgramRemoveSubtrees( sourceRoot, opid ), 
                                        sourceRoot.Left, 
                                        endSourceIndex );
            }
            else
            {
                // matched element -> check attributes if they really match (hash values of attributes matches)
                if ( sourceRoot.NodeType == XmlDiffNodeType.Element )
                {
                    DiffgramPosition diffPos = _cachedDiffgramPosition;
                    diffPos._sourceNode = sourceRoot;

                    GenerateChangeDiffgramForAttributes( diffPos, (XmlDiffElement)sourceRoot, (XmlDiffElement)targetRoot );

                    if ( diffPos._firstChildOp != null || bNeedPosition )
                    {
                        parent.InsertAtBeginning( diffPos );
                        _cachedDiffgramPosition = new DiffgramPosition( null );
                        bNeedPosition = false;
                    }
                }
                // otherwise output <node> - only if we need the position (<=> preceding operation was 'add')
                else
                {
                    if ( bNeedPosition )
                    {
                        parent.InsertAtBeginning( new DiffgramPosition( sourceRoot, targetRoot ) );
                        bNeedPosition = false;
                    }
                    // xml declaration, DTD
                    else if ( !_bChildOrderSignificant && (int)sourceRoot.NodeType < 0 ) {
                        DiffgramOperation op = parent._firstChildOp;
                        if ( op is DiffgramAddNode || op is DiffgramAddSubtrees || op is DiffgramCopy ) {
                            parent.InsertAtBeginning( new DiffgramPosition( sourceRoot, targetRoot ) );
                        }
                    }
                }
            }

            // adjust current position
            _curSourceIndex = sourceRoot.Left - 1;
            _curTargetIndex = targetRoot.Left - 1;

            // adjust boundaries in the edit script or move to next edit script operation
            matchOp._length -= endTargetIndex - targetRoot.Left + 1;
            if ( matchOp._length <= 0 )
                _editScript = _editScript._nextEditScript;
        }
        // single but non-leaf node matches (-> recursively generate the diffgram subtree)
        else
        {
            // adjust current position
            _curSourceIndex--;
            _curTargetIndex--;

            // adjust boundaries in the edit script or move to next edit script operation
            matchOp._length--;
            if ( matchOp._length <= 0 )
                _editScript = _editScript._nextEditScript;

            DiffgramParentOperation diffgramNode;
            if ( _bBuildingAddTree )
            {
                Debug.Assert( !bNeedPosition );

                ulong opid = GenerateOperationID( XmlDiffDescriptorType.Move );
                bool bCopySubtree = sourceRoot.NodeType != XmlDiffNodeType.Element;

                // output <add match=".." subtree="no">
                diffgramNode = new DiffgramCopy( sourceRoot, bCopySubtree, opid );
                // add 'remove' operation to postponed operations
                PostponedRemoveNode( sourceRoot, bCopySubtree, opid, 
                                     endSourceIndex, 
                                     endSourceIndex );

                // recursively generate the diffgram subtree
                GenerateDiffgramAdd( diffgramNode, sourceRoot.Left, targetRoot.Left );

                // insert to diffgram tree
                parent.InsertAtBeginning( diffgramNode );
            }
            else
            {
                // output <node>
                diffgramNode = new DiffgramPosition( sourceRoot, targetRoot );

                // recursively generate the diffgram subtree
                GenerateDiffgramMatch( diffgramNode, sourceRoot.Left, targetRoot.Left );

                // insert to diffgram tree
                if ( diffgramNode._firstChildOp != null )
                    parent.InsertAtBeginning( diffgramNode );
            }
        }
    }

    // returns true if a positioning <node> element is needed in diffgram
    private bool OnAdd( DiffgramParentOperation parent, int sourceBorderIndex, int targetBorderIndex )
    {
        EditScriptAdd addOp = _editScript as EditScriptAdd;
        
        Debug.Assert( addOp._endTargetIndex == _curTargetIndex );
        XmlDiffNode targetRoot = _targetNodes[ addOp._endTargetIndex ];

        // add subtree or leaf node and no descendant node matches (= has been moved from somewhere else)
        if ( addOp._startTargetIndex <= targetRoot.Left &&
             !targetRoot._bSomeDescendantMatches )
        {
            switch ( targetRoot.NodeType )
            {
                case XmlDiffNodeType.ShrankNode:
				    XmlDiffShrankNode shrankNode = (XmlDiffShrankNode)targetRoot;

                    if ( _xmlDiff.IgnoreChildOrder && ParentMatches( shrankNode.MatchingShrankNode, shrankNode, parent ) ) {
                        break;
                    }
                    else {
				        if ( shrankNode.MoveOperationId == 0 )
					        shrankNode.MoveOperationId = GenerateOperationID( XmlDiffDescriptorType.Move );

				        parent.InsertAtBeginning( new DiffgramCopy( shrankNode.MatchingShrankNode, true, shrankNode.MoveOperationId ) );
                    }
                    break;

                case XmlDiffNodeType.XmlDeclaration:
                case XmlDiffNodeType.DocumentType:
                case XmlDiffNodeType.EntityReference:
    				parent.InsertAtBeginning( new DiffgramAddNode( targetRoot, 0 ) );
                    break;

                default:
                    if ( !parent.MergeAddSubtreeAtBeginning( targetRoot ) )
                    {
                        parent.InsertAtBeginning( new DiffgramAddSubtrees( targetRoot, 0, !_xmlDiff.IgnoreChildOrder ) );
                    }
                    break;
            }
            // adjust current position
            _curTargetIndex = targetRoot.Left - 1;

            // adjust boundaries in the edit script or move to next edit script operation
            addOp._endTargetIndex = targetRoot.Left - 1;
            if ( addOp._startTargetIndex > addOp._endTargetIndex )
                _editScript = _editScript._nextEditScript;
        }
        // add single but non-leaf node, or some descendant matches (= has been moved from somewhere else )
        // -> recursively process diffgram subtree  
        else
        {
			Debug.Assert( !( targetRoot is XmlDiffShrankNode) );
            DiffgramAddNode addNode = new DiffgramAddNode( targetRoot, 0 );

            // adjust current position
            _curTargetIndex--;

            // adjust boundaries in the edit script or move to next edit script operation
            addOp._endTargetIndex--;
            if ( addOp._startTargetIndex > addOp._endTargetIndex )
                _editScript = _editScript._nextEditScript;

            if ( _bBuildingAddTree )
            {
                GenerateDiffgramAdd( addNode, sourceBorderIndex, targetRoot.Left );
            }
            else
            {
                // switch to 'building add-tree' mode
                _postponedEditScript.Reset();
                _bBuildingAddTree = true;

                // generate new tree
                GenerateDiffgramAdd( addNode, sourceBorderIndex, targetRoot.Left );

                _bBuildingAddTree = false;

                // attach postponed edit script to _editScript for futher processing
                if ( _postponedEditScript._firstES != null )
                {
                    Debug.Assert( _postponedEditScript._lastES != null );
                    Debug.Assert( _postponedEditScript._startSourceIndex != 0 );
                    Debug.Assert( _postponedEditScript._endSourceIndex != 0 );
                    _curSourceIndex = _postponedEditScript._endSourceIndex;
                    _postponedEditScript._lastES._nextEditScript = _editScript;
                    _editScript = _postponedEditScript._firstES;
                }
            }

            // add attributes
            if ( targetRoot.NodeType == XmlDiffNodeType.Element ) 
                GenerateAddDiffgramForAttributes( addNode, (XmlDiffElement)targetRoot );
            
            parent.InsertAtBeginning( addNode );
        }

        // return true if positioning <node> element is needed in diffgram
        if ( _bChildOrderSignificant ) {
            return !_bBuildingAddTree;
        }
        else {
            return false;
        }
    }

    private void OnRemove( DiffgramParentOperation parent )
    {
        EditScriptRemove remOp = _editScript as EditScriptRemove;

        Debug.Assert( remOp._endSourceIndex == _curSourceIndex );
        XmlDiffNode sourceRoot = _sourceNodes[ remOp._endSourceIndex ];

        // remove subtree or leaf node and no descendant node matches (=has been moved somewhere else)
        if ( remOp._startSourceIndex <= sourceRoot.Left )
        {
            bool bShrankNode = sourceRoot is XmlDiffShrankNode;

            if ( sourceRoot._bSomeDescendantMatches && !bShrankNode )
            {
                DiffgramOperation newDiffOp = GenerateDiffgramRemoveWhenDescendantMatches( (XmlDiffParentNode)sourceRoot );
                if ( _bBuildingAddTree )
                {
                    PostponedOperation( newDiffOp, sourceRoot.Left, remOp._endSourceIndex );
                }
                else
                {
                    parent.InsertAtBeginning( newDiffOp );
                }
            }
            else
            {
			    ulong opid = 0;
                // shrank node -> output as 'move' operation
			    if ( bShrankNode )
			    {
				    XmlDiffShrankNode shrankNode = (XmlDiffShrankNode) sourceRoot;
                    if ( _xmlDiff.IgnoreChildOrder && ParentMatches( shrankNode, shrankNode.MatchingShrankNode, parent ) ) {
                        goto End;
                    }
				    if ( shrankNode.MoveOperationId == 0 )
					    shrankNode.MoveOperationId = GenerateOperationID( XmlDiffDescriptorType.Move );
				    opid = shrankNode.MoveOperationId;

				    Debug.Assert( sourceRoot == _sourceNodes[ sourceRoot.Left ] );
			    }

                // insert 'remove' operation 
			    if ( _bBuildingAddTree )
                {
                    PostponedRemoveSubtrees( sourceRoot, opid, sourceRoot.Left, remOp._endSourceIndex );
                }
			    else
			    {
				    if ( opid != 0 ||
					    !parent.MergeRemoveSubtreeAtBeginning( sourceRoot ) )
				    {
					    parent.InsertAtBeginning( new DiffgramRemoveSubtrees( sourceRoot, opid, !_xmlDiff.IgnoreChildOrder ) );
				    }
			    }
            }
        End:
            // adjust current position
            _curSourceIndex = sourceRoot.Left - 1;

            // adjust boundaries in the edit script or move to next edit script operation
            remOp._endSourceIndex = sourceRoot.Left - 1;
            if ( remOp._startSourceIndex > remOp._endSourceIndex )
                _editScript = _editScript._nextEditScript;
        }
        // remove single but non-leaf node or some descendant matches (=has been moved somewhere else)
        // -> recursively process diffgram subtree  
        else
        {
			Debug.Assert( !( sourceRoot is XmlDiffShrankNode) );

            // adjust current position
            _curSourceIndex--;

            // adjust boundaries in the edit script or move to next edit script operation
            remOp._endSourceIndex--;
            if ( remOp._startSourceIndex > remOp._endSourceIndex )
                _editScript = _editScript._nextEditScript;

            bool bRemoveSubtree = sourceRoot.NodeType != XmlDiffNodeType.Element;

            if ( _bBuildingAddTree )
            {
                // add 'remove' to postponed operations 
                PostponedRemoveNode( sourceRoot, bRemoveSubtree, 0,
                //AddToPosponedOperations( new DiffgramRemoveNode( sourceRoot, bRemoveSubtree, 0 ), 
                    remOp._endSourceIndex + 1, remOp._endSourceIndex + 1 );
                
                // recursively parse subtree
                GenerateDiffgramAdd( parent, sourceRoot.Left, _targetNodes[ _curTargetIndex ].Left );
            }
            else
            {
                // 'remove' operation
                DiffgramRemoveNode remNode = new DiffgramRemoveNode( sourceRoot, bRemoveSubtree, 0 );
                
                // parse subtree
                GenerateDiffgramMatch( remNode, sourceRoot.Left, _targetNodes[ _curTargetIndex ].Left );
                
                parent.InsertAtBeginning( remNode );
            }
        }
    }

    // produces <change> element in diffgram
    private void OnChange( DiffgramParentOperation parent )
    {
        EditScriptChange chOp = _editScript as EditScriptChange;

        Debug.Assert( chOp._targetIndex == _curTargetIndex );
        Debug.Assert( chOp._sourceIndex == _curSourceIndex );

        XmlDiffNode sourceRoot = _sourceNodes[ chOp._sourceIndex ];
        XmlDiffNode targetRoot = _targetNodes[ chOp._targetIndex ];

		Debug.Assert( !( sourceRoot is XmlDiffShrankNode) );
		Debug.Assert( !( targetRoot is XmlDiffShrankNode) );

        // adjust current position
        _curSourceIndex--;
        _curTargetIndex--;

        // move to next edit script operation
        _editScript = _editScript._nextEditScript;

        DiffgramOperation diffgramNode = null;

        if ( _bBuildingAddTree )
        {
            // <add> changed node to the new location
            if ( targetRoot.NodeType == XmlDiffNodeType.Element )
                diffgramNode = new DiffgramAddNode( targetRoot, 0 );
            else
                diffgramNode = new DiffgramAddSubtrees( targetRoot, 0, !_xmlDiff.IgnoreChildOrder );

            // <remove> old node from old location -> add to postponed operations
            bool bSubtree = sourceRoot.NodeType != XmlDiffNodeType.Element;
            PostponedRemoveNode( sourceRoot, bSubtree, 0,
            //AddToPosponedOperations( new DiffgramRemoveNode( sourceRoot, bSubtree, 0 ), 
                chOp._sourceIndex, chOp._sourceIndex );

            // recursively process children
            if ( sourceRoot.Left < chOp._sourceIndex ||
                targetRoot.Left < chOp._targetIndex )
            {
                Debug.Assert( targetRoot.NodeType == XmlDiffNodeType.Element );
                GenerateDiffgramAdd( (DiffgramParentOperation)diffgramNode, sourceRoot.Left, targetRoot.Left );
            }

            // add attributes, if element
            if ( targetRoot.NodeType == XmlDiffNodeType.Element )
                GenerateAddDiffgramForAttributes( (DiffgramParentOperation)diffgramNode, (XmlDiffElement)targetRoot );
        }
        else
        {
            ulong opid = 0;

            // change of namespace or prefix -> get the appropriate operation id
            if ( !_xmlDiff.IgnoreNamespaces && 
                 sourceRoot.NodeType == XmlDiffNodeType.Element )
            {
                XmlDiffElement sourceEl = (XmlDiffElement)sourceRoot;
                XmlDiffElement targetEl = (XmlDiffElement)targetRoot;

                if ( sourceEl.LocalName == targetEl.LocalName )
                {
                    opid = GetNamespaceChangeOpid( sourceEl.NamespaceURI, sourceEl.Prefix,
                                                   targetEl.NamespaceURI, targetEl.Prefix );
                }  
            }

            if ( sourceRoot.NodeType == XmlDiffNodeType.Element )
            {
                if ( XmlDiff.IsChangeOperationOnAttributesOnly( chOp._changeOp ) )
                    diffgramNode = new DiffgramPosition( sourceRoot );
                else
                {
                    Debug.Assert( (int)chOp._changeOp == (int)XmlDiffOperation.ChangeElementName ||
                                  ( (int)chOp._changeOp >= (int)XmlDiffOperation.ChangeElementNameAndAttr1 &&
                                    (int)chOp._changeOp <= (int)XmlDiffOperation.ChangeElementNameAndAttr2 ) );

                    diffgramNode = new DiffgramChangeNode( sourceRoot, targetRoot, XmlDiffOperation.ChangeElementName, opid );
                }

                // recursively process children
                if ( sourceRoot.Left < chOp._sourceIndex ||
                    targetRoot.Left < chOp._targetIndex )
                {
                    GenerateDiffgramMatch( (DiffgramParentOperation) diffgramNode, sourceRoot.Left, targetRoot.Left );
                }

                GenerateChangeDiffgramForAttributes( (DiffgramParentOperation)diffgramNode, (XmlDiffElement)sourceRoot, (XmlDiffElement)targetRoot );
            }
            else
            {
                // '<change>'
                diffgramNode = new DiffgramChangeNode( sourceRoot, targetRoot, chOp._changeOp, opid );
                Debug.Assert( !sourceRoot.HasChildNodes );
            }
        }

        parent.InsertAtBeginning( diffgramNode );
    }

    private void PostponedRemoveNode( XmlDiffNode sourceNode, bool bSubtree, ulong operationID,
                                      int startSourceIndex, int endSourceIndex )
    {
        Debug.Assert( sourceNode != null );
        PostponedOperation( new DiffgramRemoveNode( sourceNode, bSubtree, operationID ), startSourceIndex, endSourceIndex );
    }

    private void PostponedRemoveSubtrees( XmlDiffNode sourceNode, ulong operationID,
                                          int startSourceIndex, int endSourceIndex )
    {
        Debug.Assert( _bBuildingAddTree );
        Debug.Assert( sourceNode != null );

        if ( operationID == 0 &&
             _postponedEditScript._firstES != null )
        {
            Debug.Assert( _postponedEditScript._lastES._startSourceIndex > endSourceIndex );

            DiffgramRemoveSubtrees remSubtrees = _postponedEditScript._lastES._diffOperation as DiffgramRemoveSubtrees;
            if ( remSubtrees != null  && 
                remSubtrees.SetNewFirstNode( sourceNode ) )
            {
                _postponedEditScript._lastES._startSourceIndex = startSourceIndex;
                _postponedEditScript._startSourceIndex = startSourceIndex;
                return;
            }
        }
        
        PostponedOperation( new DiffgramRemoveSubtrees( sourceNode, operationID, !_xmlDiff.IgnoreChildOrder ), startSourceIndex, endSourceIndex );
    }

    private void PostponedOperation( DiffgramOperation op, int startSourceIndex, int endSourceIndex )
    {
        Debug.Assert( _bBuildingAddTree );
        Debug.Assert( op != null );

        EditScriptPostponed es = new EditScriptPostponed( op, startSourceIndex, endSourceIndex );

        if ( _postponedEditScript._firstES == null )
        {
            _postponedEditScript._firstES = es;
            _postponedEditScript._lastES = es;
            _postponedEditScript._startSourceIndex = startSourceIndex;
            _postponedEditScript._endSourceIndex = endSourceIndex;
        }
        else
        {
            Debug.Assert( _postponedEditScript._lastES != null );
            Debug.Assert( _postponedEditScript._lastES._startSourceIndex > endSourceIndex );

            _postponedEditScript._lastES._nextEditScript = es;
            _postponedEditScript._lastES = es;

            _postponedEditScript._startSourceIndex = startSourceIndex;
        }
    }

    // generates a new operation ID
    private ulong GenerateOperationID( XmlDiffDescriptorType descriptorType )
    {
        ulong opid = ++_lastOperationID;

        if ( descriptorType == XmlDiffDescriptorType.Move )
            _moveDescriptors.Add( opid, opid );
        return opid;
    }

    // returns appropriate operation ID for this namespace or prefix change
    private ulong GetNamespaceChangeOpid( string oldNamespaceURI, string oldPrefix, 
                                          string newNamespaceURI, string newPrefix )
    {
        Debug.Assert( !_xmlDiff.IgnoreNamespaces );

        ulong opid = 0;

        // namespace change
        if ( oldNamespaceURI != newNamespaceURI )
        {
            // prefix must remain the same
            if ( oldPrefix != newPrefix )
                return 0;

            // lookup this change in the list of namespace changes
            NamespaceChange nsChange = _namespaceChangeDescr;
            while ( nsChange != null )
            {
                if ( nsChange._oldNS == oldNamespaceURI &&
                     nsChange._prefix == oldPrefix &&
                     nsChange._newNS == newNamespaceURI )
                {
                    return nsChange._opid;
                }

                nsChange = nsChange._next;
            }
            
            // the change record was not found -> create a new one
            opid = GenerateOperationID( XmlDiffDescriptorType.NamespaceChange );
            _namespaceChangeDescr = new NamespaceChange( oldPrefix, oldNamespaceURI, newNamespaceURI,
                                                         opid, _namespaceChangeDescr );
        }
        // prefix change
        else if ( !_xmlDiff.IgnorePrefixes && 
                  oldPrefix != newPrefix )
        {
            // lookup this change in the list of prefix changes
            PrefixChange prefixChange = _prefixChangeDescr;
            while ( prefixChange != null )
            {
                if ( prefixChange._NS == oldNamespaceURI &&
                     prefixChange._oldPrefix == oldPrefix &&
                     prefixChange._newPrefix == newPrefix )
                {
                    return prefixChange._opid;
                }

                prefixChange = prefixChange._next;
            }

            // the change record was not found -> create a new one
            opid = GenerateOperationID( XmlDiffDescriptorType.PrefixChange );
            _prefixChangeDescr = new PrefixChange( oldPrefix, newPrefix, 
                                                   oldNamespaceURI,
                                                   opid, 
                                                   _prefixChangeDescr );
        }

        return opid;
    }

	internal Diffgram GenerateEmptyDiffgram()
	{
		return new Diffgram( _xmlDiff );
	}

    private void GenerateChangeDiffgramForAttributes( DiffgramParentOperation diffgramParent, 
                                                      XmlDiffElement sourceElement, 
                                                      XmlDiffElement targetElement )
    {
        XmlDiffAttributeOrNamespace sourceAttr = sourceElement._attributes;
        XmlDiffAttributeOrNamespace targetAttr = targetElement._attributes;
        int nCompare;
        ulong opid;

        while ( sourceAttr != null && targetAttr != null ) 
        {
            opid = 0;

            if ( sourceAttr.NodeType == targetAttr.NodeType )
            {
                if ( sourceAttr.NodeType == XmlDiffNodeType.Attribute ) 
                {
                    if ( (nCompare = XmlDiffDocument.OrderStrings( sourceAttr.LocalName, targetAttr.LocalName )) == 0 )
                    {
                        if ( _xmlDiff.IgnoreNamespaces )
                        {
                            if ( XmlDiffDocument.OrderStrings( sourceAttr.Value, targetAttr.Value ) == 0 )
                            {
                                // attributes match
                                goto Next;
                            }
                        }
                        else
                        {
                            if ( XmlDiffDocument.OrderStrings( sourceAttr.NamespaceURI, targetAttr.NamespaceURI ) == 0  &&  
		                         (_xmlDiff.IgnorePrefixes || XmlDiffDocument.OrderStrings( sourceAttr.Prefix, targetAttr.Prefix )  == 0 ) &&
                                XmlDiffDocument.OrderStrings( sourceAttr.Value, targetAttr.Value ) == 0 )
                            {
                                // attributes match
                                goto Next;
                            }
                        }

                        diffgramParent.InsertAtBeginning( new DiffgramChangeNode( sourceAttr, targetAttr, XmlDiffOperation.ChangeAttr, 0 ) );
                        goto Next;
                    }

                    goto AddRemove;
                }
                else // sourceAttr.NodeType != XmlDiffNodeType.Attribute 
                {
                    if ( _xmlDiff.IgnorePrefixes ) {
                        if ( ( nCompare = XmlDiffDocument.OrderStrings( sourceAttr.NamespaceURI, targetAttr.NamespaceURI ) ) == 0 )
                            goto Next;
                        else
                            goto AddRemove;
                    }
                    else if ( ( nCompare = XmlDiffDocument.OrderStrings( sourceAttr.Prefix, targetAttr.Prefix ) ) == 0 ) {
                        if ( ( nCompare = XmlDiffDocument.OrderStrings( sourceAttr.NamespaceURI, targetAttr.NamespaceURI ) ) == 0 )
                            goto Next;
                        else {
                            // change of namespace
                            opid = GetNamespaceChangeOpid( sourceAttr.NamespaceURI, sourceAttr.Prefix, 
                                                        targetAttr.NamespaceURI, targetAttr.Prefix );
                            goto AddRemoveBoth;
                        }
                    }
                    else {
                        if ( ( nCompare = XmlDiffDocument.OrderStrings( sourceAttr.NamespaceURI, targetAttr.NamespaceURI ) ) == 0 ) {
                            // change of prefix
                            opid = GetNamespaceChangeOpid( sourceAttr.NamespaceURI, sourceAttr.Prefix, 
                                                        targetAttr.NamespaceURI, targetAttr.Prefix );
                            goto AddRemoveBoth;
                        }
                        else {
                            goto AddRemove;
                        }
                    }
                }
            }
            else // ( sourceAttr.NodeType != targetAttr.NodeType )
            {
			    if ( sourceAttr.NodeType == XmlDiffNodeType.Namespace )
    			    goto RemoveSource;
	    	    else
		    	    goto AddTarget;
            }

        Next:
            sourceAttr = (XmlDiffAttributeOrNamespace)sourceAttr._nextSibling;
            targetAttr = (XmlDiffAttributeOrNamespace)targetAttr._nextSibling;
            continue;

        AddRemove:
            if ( nCompare == -1 )
                goto RemoveSource;
            else
            {
                Debug.Assert( nCompare == 1 );
                goto AddTarget;
            }
                
        AddRemoveBoth:
            if ( !diffgramParent.MergeRemoveAttributeAtBeginning( sourceAttr ) )
                diffgramParent.InsertAtBeginning( new DiffgramRemoveNode( sourceAttr, true, opid ) );
            sourceAttr = (XmlDiffAttributeOrNamespace)sourceAttr._nextSibling;

            diffgramParent.InsertAtBeginning( new DiffgramAddNode( targetAttr, opid ) );
            targetAttr = (XmlDiffAttributeOrNamespace)targetAttr._nextSibling;
            continue;
            
        RemoveSource:
            if ( !diffgramParent.MergeRemoveAttributeAtBeginning( sourceAttr ) )
                diffgramParent.InsertAtBeginning( new DiffgramRemoveNode( sourceAttr, true, opid ) );
            sourceAttr = (XmlDiffAttributeOrNamespace)sourceAttr._nextSibling;
            continue;
                
        AddTarget:
            diffgramParent.InsertAtBeginning( new DiffgramAddNode( targetAttr, opid ) );
            targetAttr = (XmlDiffAttributeOrNamespace)targetAttr._nextSibling;
            continue;
        }

        while ( sourceAttr != null ) 
        {
            if ( !diffgramParent.MergeRemoveAttributeAtBeginning( sourceAttr ) )
                diffgramParent.InsertAtBeginning( new DiffgramRemoveNode( sourceAttr, true, 0 ) );
            sourceAttr = (XmlDiffAttributeOrNamespace)sourceAttr._nextSibling;
        }

        while ( targetAttr != null )
        {
            diffgramParent.InsertAtBeginning( new DiffgramAddNode( targetAttr, 0 ) );
            targetAttr = (XmlDiffAttributeOrNamespace)targetAttr._nextSibling;
        }
    }

    private void GenerateAddDiffgramForAttributes( DiffgramParentOperation diffgramParent, XmlDiffElement targetElement )
    {
        XmlDiffAttributeOrNamespace attr = targetElement._attributes;
        while ( attr != null )
        {
            diffgramParent.InsertAtBeginning( new DiffgramAddNode( attr, 0 ) );
            attr = (XmlDiffAttributeOrNamespace)attr._nextSibling;
        }
    }

    private DiffgramOperation GenerateDiffgramRemoveWhenDescendantMatches( XmlDiffNode sourceParent )
    {
        Debug.Assert( sourceParent._bSomeDescendantMatches );
        Debug.Assert( sourceParent.NodeType != XmlDiffNodeType.ShrankNode );

        DiffgramParentOperation diffOp = new DiffgramRemoveNode( sourceParent, false, 0 );
        XmlDiffNode child = ((XmlDiffParentNode)sourceParent)._firstChildNode;
        while ( child != null )
        {
            if ( child.NodeType == XmlDiffNodeType.ShrankNode )
            {
                XmlDiffShrankNode shrankNode = (XmlDiffShrankNode) child;

                if ( shrankNode.MoveOperationId == 0 )
    				shrankNode.MoveOperationId = GenerateOperationID( XmlDiffDescriptorType.Move );

				diffOp.InsertAtEnd( new DiffgramRemoveSubtrees( child, shrankNode.MoveOperationId, !_xmlDiff.IgnoreChildOrder ) );

            }
            else if ( child.HasChildNodes && child._bSomeDescendantMatches )
            {
                diffOp.InsertAtEnd( GenerateDiffgramRemoveWhenDescendantMatches( (XmlDiffParentNode)child ) );
            }
            else
            {
                if ( !diffOp.MergeRemoveSubtreeAtEnd( child ) )
                    diffOp.InsertAtEnd( new DiffgramRemoveSubtrees( child, 0, !_xmlDiff.IgnoreChildOrder ) );
            }

            child = child._nextSibling;
        }
        return diffOp;
    }

    private DiffgramOperation GenerateDiffgramAddWhenDescendantMatches( XmlDiffNode targetParent ) {
        Debug.Assert( targetParent.HasChildNodes );
        Debug.Assert( targetParent._bSomeDescendantMatches );
        Debug.Assert( targetParent.NodeType != XmlDiffNodeType.ShrankNode );

        DiffgramParentOperation diffOp = new DiffgramAddNode( targetParent, 0 );
        if ( targetParent.NodeType == XmlDiffNodeType.Element ) {
            XmlDiffAttributeOrNamespace attr = ((XmlDiffElement)targetParent)._attributes;
            while ( attr != null ) {
                diffOp.InsertAtEnd( new DiffgramAddNode( attr, 0 ) );
                attr = (XmlDiffAttributeOrNamespace) attr._nextSibling;
            }
        }
        
        XmlDiffNode child = ((XmlDiffParentNode)targetParent)._firstChildNode;
        while ( child != null )
        {
            if ( child.NodeType == XmlDiffNodeType.ShrankNode )
            {
                XmlDiffShrankNode shrankNode = (XmlDiffShrankNode) child;

                if ( shrankNode.MoveOperationId == 0 )
    				shrankNode.MoveOperationId = GenerateOperationID( XmlDiffDescriptorType.Move );

				diffOp.InsertAtEnd( new DiffgramCopy( shrankNode.MatchingShrankNode, true, shrankNode.MoveOperationId ) );
            }
            else if ( child.HasChildNodes && child._bSomeDescendantMatches )
            {
                diffOp.InsertAtEnd( GenerateDiffgramAddWhenDescendantMatches( (XmlDiffParentNode)child ) );
            }
            else
            {
                if ( !diffOp.MergeAddSubtreeAtEnd( child ) )
                    diffOp.InsertAtEnd( new DiffgramAddSubtrees( child, 0, !_xmlDiff.IgnoreChildOrder ) );
            }

            child = child._nextSibling;
        }
        return diffOp;
    }

    void OnEditScriptPostponed( DiffgramParentOperation parent, int targetBorderIndex ) {
        EditScriptPostponed esp = (EditScriptPostponed)_editScript;
        Debug.Assert( _curSourceIndex == esp._endSourceIndex );

        DiffgramOperation diffOp = esp._diffOperation;
        int sourceStartIndex = esp._startSourceIndex;
        int sourceLeft = _sourceNodes[ esp._endSourceIndex ].Left;

        // adjust current source index
        _curSourceIndex = esp._startSourceIndex - 1;
           
        // move to next edit script
        _editScript = esp._nextEditScript;

        // not a subtree or leaf node operation -> process child operations
        if ( sourceStartIndex > sourceLeft ) {
            GenerateDiffgramPostponed( (DiffgramParentOperation)diffOp, ref _editScript, sourceLeft, targetBorderIndex );
        }

        parent.InsertAtBeginning( diffOp );
    }

// Walktree diffgram generation

    internal Diffgram GenerateFromWalkTree() {
        Diffgram diffgram = new Diffgram( _xmlDiff );
        WalkTreeGenerateDiffgramMatch( diffgram, _xmlDiff._sourceDoc, _xmlDiff._targetDoc );
        AppendDescriptors( diffgram );
        return diffgram;
    }

    private void WalkTreeGenerateDiffgramMatch( DiffgramParentOperation diffParent, XmlDiffParentNode sourceParent, XmlDiffParentNode targetParent ) {
        XmlDiffNode sourceNode = sourceParent.FirstChildNode;
        XmlDiffNode targetNode = targetParent.FirstChildNode;
        XmlDiffNode needPositionSourceNode = null;

        while ( sourceNode != null || targetNode != null ) {
            if ( sourceNode != null ) {
                if ( targetNode != null ) {
                    XmlDiffOperation op = sourceNode.GetDiffOperation( targetNode, _xmlDiff );
                    // match
                    if ( op == XmlDiffOperation.Match ) {
                        WalkTreeOnMatchNode( diffParent, sourceNode, targetNode, ref needPositionSourceNode );
                    }
                    // no match
                    else {
                        int walkNodesCount = ( sourceNode._parent.ChildNodesCount + targetNode._parent.ChildNodesCount ) / 2;
                        walkNodesCount = (int)( LogMultiplier * Math.Log( (double)walkNodesCount ) + 1 );

                        XmlDiffNode sourceClosestMatchNode = targetNode;
                        XmlDiffNode targetClosestMatchNode = sourceNode;
                        XmlDiffOperation sourceOp = op;
                        XmlDiffOperation targetOp = op;
                        int sourceNodesToAddCount = targetNode.NodesCount;
                        int targetNodesToRemoveCount = sourceNode.NodesCount;

                        XmlDiffNode nextSourceNode = sourceNode._nextSibling;
                        XmlDiffNode nextTargetNode = targetNode._nextSibling;

                        // walk limited number of siblings to find the closest matching node
                        for ( int i = 0; i < walkNodesCount && ( nextTargetNode != null || nextSourceNode != null ); i++ ) {
                            if ( nextTargetNode != null && sourceOp != XmlDiffOperation.Match ) {
                                XmlDiffOperation o = sourceNode.GetDiffOperation( nextTargetNode, _xmlDiff );
                                if ( MinimalTreeDistanceAlgo.OperationCost[(int)o] < MinimalTreeDistanceAlgo.OperationCost[(int)sourceOp] ) {
                                    sourceOp = o;
                                    sourceClosestMatchNode = nextTargetNode;
                                }
                                else {
                                    sourceNodesToAddCount += nextTargetNode.NodesCount;
                                    nextTargetNode = nextTargetNode._nextSibling;
                                }
                            }

                            if ( nextSourceNode != null && targetOp != XmlDiffOperation.Match ) {
                                XmlDiffOperation o = targetNode.GetDiffOperation( nextSourceNode, _xmlDiff );
                                if ( MinimalTreeDistanceAlgo.OperationCost[(int)o] < MinimalTreeDistanceAlgo.OperationCost[(int)targetOp] ) {
                                    targetOp = o;
                                    targetClosestMatchNode = nextSourceNode;
                                }
                                else {
                                    targetNodesToRemoveCount += nextSourceNode.NodesCount;
                                    nextSourceNode = nextSourceNode._nextSibling;
                                }
                            }

                            if ( sourceOp == XmlDiffOperation.Match || targetOp == XmlDiffOperation.Match ) {
                                break;
                            }

                            if ( _xmlDiff.IgnoreChildOrder ) {
                                if ( nextTargetNode != null ) {
                                    if ( XmlDiffDocument.OrderChildren( sourceNode, nextTargetNode ) < 0 ) {
                                        nextTargetNode = null;
                                    }
                                }
                                if ( nextSourceNode != null ) {
                                    if ( XmlDiffDocument.OrderChildren( targetNode, nextSourceNode ) < 0 ) {
                                        nextSourceNode = null;
                                    }
                                }
                            }
                        }

                        // source match exists and is better
                        if ( sourceOp == XmlDiffOperation.Match ) {
                            if ( targetOp != XmlDiffOperation.Match || sourceNodesToAddCount < targetNodesToRemoveCount ) {
                                while ( targetNode != sourceClosestMatchNode ) {
                                    WalkTreeOnAddNode( diffParent, targetNode, needPositionSourceNode );
                                    needPositionSourceNode = null;
                                    targetNode = targetNode._nextSibling;
                                }
                                WalkTreeOnMatchNode( diffParent, sourceNode, targetNode, ref needPositionSourceNode ); 
                                goto MoveToNextPair;
                            }
                        }
                        // target match exists and is better
                        else if ( targetOp == XmlDiffOperation.Match ) {
                            while ( sourceNode != targetClosestMatchNode ) {
                                WalkTreeOnRemoveNode( diffParent, sourceNode );
                                sourceNode = sourceNode._nextSibling;
                            }
                            needPositionSourceNode = null;
                            WalkTreeOnMatchNode( diffParent, sourceNode, targetNode, ref needPositionSourceNode );
                            goto MoveToNextPair;
                        }
                        // partial match
                        else {
                            Debug.Assert( sourceOp != XmlDiffOperation.Match && targetOp != XmlDiffOperation.Match );

                            int sourceOpCost = MinimalTreeDistanceAlgo.OperationCost[ (int)sourceOp ];
                            int targetOpCost = MinimalTreeDistanceAlgo.OperationCost[ (int)targetOp ];

                            if ( sourceOpCost < targetOpCost || ( sourceOpCost == targetOpCost && sourceNodesToAddCount < targetNodesToRemoveCount ) ) {
                                while ( targetNode != sourceClosestMatchNode ) {
                                    WalkTreeOnAddNode( diffParent, targetNode, needPositionSourceNode );
                                    needPositionSourceNode = null;
                                    targetNode = targetNode._nextSibling;
                                }
                                op = sourceOp;
                            }
                            else {
                                while ( sourceNode != targetClosestMatchNode ) {
                                    WalkTreeOnRemoveNode( diffParent, sourceNode );
                                    sourceNode = sourceNode._nextSibling;
                                }
                                op = targetOp;
                            }
                        }

                        // decide whether do 'change' or 'add / delete'
                        switch ( op ) {
                            case XmlDiffOperation.ChangeElementName:
                                WalkTreeOnChangeElement( diffParent, (XmlDiffElement)sourceNode, (XmlDiffElement)targetNode, op );
                                needPositionSourceNode = null;
                                break;
                            case XmlDiffOperation.ChangeElementAttr1:
                            case XmlDiffOperation.ChangeElementAttr2:
                            case XmlDiffOperation.ChangeElementAttr3:
                            case XmlDiffOperation.ChangeElementNameAndAttr1:
                            case XmlDiffOperation.ChangeElementNameAndAttr2:
                            case XmlDiffOperation.ChangeElementNameAndAttr3:
                                if ( GoForElementChange( (XmlDiffElement)sourceNode, (XmlDiffElement)targetNode ) ) {
                                    WalkTreeOnChangeElement( diffParent, (XmlDiffElement) sourceNode, (XmlDiffElement)targetNode, op );
                                    needPositionSourceNode = null;
                                }
                                else {
                                    goto case XmlDiffOperation.Undefined;
                                }
                                break;
                            case XmlDiffOperation.ChangePI:
                            case XmlDiffOperation.ChangeER:
                            case XmlDiffOperation.ChangeCharacterData:
                            case XmlDiffOperation.ChangeXmlDeclaration:
                            case XmlDiffOperation.ChangeDTD:
                                diffParent.InsertAtEnd( new DiffgramChangeNode( sourceNode, targetNode, op, 0 ) );
                                needPositionSourceNode = null;
                                break;

                            case XmlDiffOperation.Undefined:
                                // Prefer inserts against removes
                                WalkTreeOnAddNode( diffParent, targetNode, needPositionSourceNode );
                                needPositionSourceNode = null;
                                targetNode = targetNode._nextSibling;
                                continue;
                            default:
                                Debug.Assert( false );
                                break;
                        }
                    }

                MoveToNextPair:
                    sourceNode = sourceNode._nextSibling;
                    targetNode = targetNode._nextSibling;
                }
                else { // targetNode == null
                    do {
                        WalkTreeOnRemoveNode( diffParent, sourceNode );
                        sourceNode = sourceNode._nextSibling;
                    } while ( sourceNode != null );
                }
            } 
            else { // sourceNode == null
                Debug.Assert( targetNode != null );

                while ( targetNode != null ) {
                    WalkTreeOnAddNode( diffParent, targetNode, needPositionSourceNode );
                    needPositionSourceNode = null;
                    targetNode = targetNode._nextSibling;
                }
            }
        }
    }

    // returns true if the two elements have at least 50% in common (common name & attributes)
    private bool GoForElementChange( XmlDiffElement sourceElement, XmlDiffElement targetElement ) {
        int identicalAttrCount = 0;
        int addedAttrCount = 0;
        int removedAttrCount = 0;
        int changedAttrCount = 0;
        bool bNameChange;

        bNameChange = ( sourceElement.LocalName != targetElement.LocalName );

        XmlDiffAttributeOrNamespace sourceAttr = sourceElement._attributes;
        XmlDiffAttributeOrNamespace targetAttr = targetElement._attributes;
        while ( sourceAttr != null && targetAttr != null ) {
            if ( sourceAttr.LocalName == targetAttr.LocalName ) {
                if ( ( _xmlDiff.IgnorePrefixes || _xmlDiff.IgnoreNamespaces || sourceAttr.Prefix == targetAttr.Prefix ) &&
                     ( _xmlDiff.IgnoreNamespaces || sourceAttr.NamespaceURI == targetAttr.NamespaceURI ) ) {
                    if ( sourceAttr.Value == targetAttr.Value ) {
                        identicalAttrCount++;
                    }
                    else {
                        changedAttrCount++;
                    }
                }
                else {
                    changedAttrCount++;
                }
                sourceAttr = (XmlDiffAttributeOrNamespace)sourceAttr._nextSibling;
                targetAttr = (XmlDiffAttributeOrNamespace)targetAttr._nextSibling;
            }
            else {
                int compare = XmlDiffDocument.OrderAttributesOrNamespaces( sourceAttr, targetAttr );
                if ( compare < 0 ) {
                    removedAttrCount++;
                    sourceAttr = (XmlDiffAttributeOrNamespace)sourceAttr._nextSibling;
                }
                else {
                    addedAttrCount++;
                    targetAttr = (XmlDiffAttributeOrNamespace)targetAttr._nextSibling;
                }
            }
        }

        while ( sourceAttr != null ) {
            removedAttrCount++;
            sourceAttr = (XmlDiffAttributeOrNamespace)sourceAttr._nextSibling;
        }

        while ( targetAttr != null ) {
            addedAttrCount++;
            targetAttr = (XmlDiffAttributeOrNamespace)targetAttr._nextSibling;
        }

        if ( bNameChange ) {
            // total number of changes is less than 50%
            if ( removedAttrCount + addedAttrCount + changedAttrCount <= identicalAttrCount )
                return true;

            return false;
        }
        else {
            // only added
            if ( removedAttrCount + changedAttrCount == 0 )
                return true;

            // only removed
            if ( addedAttrCount + changedAttrCount == 0 )
                return true;

            // no removed or added: 
            if ( removedAttrCount + addedAttrCount == 0 ) {
                return true;
            }

            // total number of changes is less than 75% - or - 
            // no other sibling node
            if ( removedAttrCount + addedAttrCount + changedAttrCount <= identicalAttrCount * 3 ||
                sourceElement._nextSibling == null )
                return true;

            return false;
        }
    }

    private void WalkTreeOnRemoveNode( DiffgramParentOperation diffParent, XmlDiffNode sourceNode ) {
        bool bShrankNode = sourceNode is XmlDiffShrankNode;

        if ( sourceNode._bSomeDescendantMatches && !bShrankNode )
        {
            DiffgramOperation removeOp = GenerateDiffgramRemoveWhenDescendantMatches( (XmlDiffParentNode)sourceNode );
            diffParent.InsertAtEnd( removeOp );
        }
        else
        {
			ulong opid = 0;
            // shrank node -> output as 'move' operation
			if ( bShrankNode )
			{
				XmlDiffShrankNode shrankNode = (XmlDiffShrankNode) sourceNode;
				if ( shrankNode.MoveOperationId == 0 )
					shrankNode.MoveOperationId = GenerateOperationID( XmlDiffDescriptorType.Move );
				opid = shrankNode.MoveOperationId;
			}

			if ( opid != 0 ||
				!diffParent.MergeRemoveSubtreeAtEnd( sourceNode ) )
			{
				diffParent.InsertAtEnd( new DiffgramRemoveSubtrees( sourceNode, opid, !_xmlDiff.IgnoreChildOrder ) );
			}
        }
    }

    private void WalkTreeOnAddNode( DiffgramParentOperation diffParent, XmlDiffNode targetNode, XmlDiffNode sourcePositionNode ) {
        bool bShrankNode = targetNode is XmlDiffShrankNode;

        if ( _bChildOrderSignificant ) {
            if ( sourcePositionNode != null ) {
                diffParent.InsertAtEnd( new DiffgramPosition( sourcePositionNode ) );
            }
        }
        else {
            if ( diffParent._firstChildOp == null && diffParent is Diffgram ) {
                diffParent.InsertAtEnd( new DiffgramPosition( sourcePositionNode ) );
            }
        }
        
        if ( targetNode._bSomeDescendantMatches && !bShrankNode ) {
            DiffgramOperation addOp = GenerateDiffgramAddWhenDescendantMatches( (XmlDiffParentNode)targetNode );
            diffParent.InsertAtEnd( addOp );
        }
        else {
            // shrank node -> output as 'move' operation
            if ( bShrankNode ) {
                ulong opid = 0;
                XmlDiffShrankNode shrankNode = (XmlDiffShrankNode) targetNode;
                if ( shrankNode.MoveOperationId == 0 )
                    shrankNode.MoveOperationId = GenerateOperationID( XmlDiffDescriptorType.Move );
                opid = shrankNode.MoveOperationId;

                diffParent.InsertAtEnd( new DiffgramCopy( shrankNode.MatchingShrankNode, true, opid ) );
            }
            else {
                switch ( targetNode.NodeType ) {
                    case XmlDiffNodeType.XmlDeclaration:
                    case XmlDiffNodeType.DocumentType:
                    case XmlDiffNodeType.EntityReference:
        				diffParent.InsertAtEnd( new DiffgramAddNode( targetNode, 0 ) );
                        break;
                    default:
                        if ( !diffParent.MergeAddSubtreeAtEnd( targetNode ) ) {
                            diffParent.InsertAtEnd( new DiffgramAddSubtrees( targetNode, 0, !_xmlDiff.IgnoreChildOrder ) );
                        }
                        break;
                }
            }
        }
    }

    private void WalkTreeOnChangeNode( DiffgramParentOperation diffParent, XmlDiffNode sourceNode, XmlDiffNode targetNode, XmlDiffOperation op ) {
        Debug.Assert( sourceNode.NodeType != XmlDiffNodeType.Element && targetNode.NodeType != XmlDiffNodeType.Element );

        DiffgramChangeNode changeOp = new DiffgramChangeNode( sourceNode, targetNode, op, 0 );
        if ( sourceNode.HasChildNodes || targetNode.HasChildNodes ) {
            WalkTreeGenerateDiffgramMatch( changeOp, (XmlDiffParentNode) sourceNode, (XmlDiffParentNode) targetNode );
        }
        diffParent.InsertAtEnd( changeOp );
    }

    private void WalkTreeOnChangeElement( DiffgramParentOperation diffParent, XmlDiffElement sourceElement, XmlDiffElement targetElement, XmlDiffOperation op ) {

        DiffgramParentOperation diffOp;

        if ( XmlDiff.IsChangeOperationOnAttributesOnly( op ) ) {
            diffOp = new DiffgramPosition( sourceElement );
        }
        else
        {
            ulong opid = 0;
            if ( !_xmlDiff.IgnoreNamespaces && sourceElement.LocalName == targetElement.LocalName)
            {
                opid = GetNamespaceChangeOpid( sourceElement.NamespaceURI, sourceElement.Prefix,
                                               targetElement.NamespaceURI, targetElement.Prefix );
            }

            Debug.Assert( (int)op >= (int)XmlDiffOperation.ChangeElementName &&
                          (int)op <= (int)XmlDiffOperation.ChangeElementNameAndAttr3 );

            diffOp = new DiffgramChangeNode( sourceElement, targetElement, XmlDiffOperation.ChangeElementName, opid );
        }

        GenerateChangeDiffgramForAttributes( diffOp, sourceElement, targetElement );

        if ( sourceElement.HasChildNodes || targetElement.HasChildNodes ) {
            WalkTreeGenerateDiffgramMatch( diffOp, sourceElement, targetElement );
        }

        diffParent.InsertAtEnd( diffOp );
    }

    private void WalkTreeOnMatchNode( DiffgramParentOperation diffParent, XmlDiffNode sourceNode, XmlDiffNode targetNode, ref XmlDiffNode needPositionSourceNode ) {
        if ( sourceNode.HasChildNodes || targetNode.HasChildNodes ) {
            DiffgramPosition diffMatch = new DiffgramPosition( sourceNode, targetNode );
            WalkTreeGenerateDiffgramMatch( diffMatch, (XmlDiffParentNode)sourceNode, (XmlDiffParentNode)targetNode );
            diffParent.InsertAtEnd( diffMatch );
            needPositionSourceNode = null;
        }
        else {
            if ( sourceNode.NodeType == XmlDiffNodeType.ShrankNode ) {
                needPositionSourceNode = ((XmlDiffShrankNode)sourceNode)._lastNode;
            }
            else {
                needPositionSourceNode = sourceNode;
            }
        }
    }

    private bool ParentMatches( XmlDiffNode sourceNode, XmlDiffNode targetNode, DiffgramOperation diffParent ) {
        switch ( diffParent.Operation ) {
            case XmlDiffOperation.Match:
                DiffgramPosition diffMatch = diffParent as DiffgramPosition;
                if ( diffMatch != null ) {
                    return ( diffMatch._sourceNode == sourceNode._parent && 
                             diffMatch._targetNode == targetNode._parent );
                }
                return false;
            case XmlDiffOperation.Remove:
                DiffgramRemoveNode diffRemove = diffParent as DiffgramRemoveNode;
                if ( diffRemove != null && diffRemove._sourceNode == sourceNode._parent ) {
                    return ParentMatches( diffRemove._sourceNode, targetNode, diffParent._parent );
                }
                return false;
            case XmlDiffOperation.Add:
                return false;
            default:
                DiffgramChangeNode diffChange = diffParent as DiffgramChangeNode;
                if ( diffChange != null ) {
                    return ( diffChange._sourceNode == sourceNode._parent &&
                             diffChange._targetNode == targetNode._parent );
                }
                return false;
        }

    }
}
}
