//------------------------------------------------------------------------------
// <copyright file="XmlDiff.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

//#define MEASURE_PERF
#define VERIFY_HASH_VALUES

using System;
using System.IO;
using System.Xml;
using System.Timers;
using System.Diagnostics;
using System.Collections;
using System.Security.Permissions;

namespace Microsoft.XmlDiffPatch
{
    
//
// XmlDiffOptions
//
/// <include file='doc\XmlDiffOptions.uex' path='docs/doc[@for="XmlDiffOptions"]/*' />
/// <summary>
/// Options for comparing XML documents. 
/// </summary>
public enum XmlDiffOptions 
{
    None                 = 0x0,
    IgnoreChildOrder     = 0x1,
    IgnoreComments       = 0x2,
    IgnorePI             = 0x4,
    IgnoreWhitespace     = 0x8,
    IgnoreNamespaces     = 0x10,
    IgnorePrefixes       = 0x20,
    IgnoreXmlDecl        = 0x40,
    IgnoreDtd            = 0x80,
}

//
// XmlDiffAlgorithm
//
/// <include file='doc\XmlDiffAlgorithm.uex' path='docs/doc[@for="XmlDiffAlgorithm"]/*' />
/// <summary>
///   Types of algorithms that can be used for comparing XML documents by XmlDiff. Auto means XmlDiff will
///   automatically decide which algorithm to use for the particular case depending on the assumed number 
///   of changes.
/// </summary>
public enum XmlDiffAlgorithm {
    Auto,
    Fast,
    Precise,
}

// 
// XmlDiffOperation
//
// BEWARE: MinimalTreeDistanceAlgo.OperationCost is indexed by this enum.
// If you change this, make the appropriate changes to the MinimalTreeDistanceAlgo.OperationCost too.
internal enum XmlDiffOperation 
{ 
    Match                      = 0,
    Add                        = 1,
    Remove                     = 2,

	ChangeElementName          = 3,
	ChangeElementAttr1         = 4,
	ChangeElementAttr2         = 5,
	ChangeElementAttr3         = 6,
	ChangeElementNameAndAttr1  = 7,
	ChangeElementNameAndAttr2  = 8,
	ChangeElementNameAndAttr3  = 9,

	ChangePI                   = 10,
	ChangeER                   = 11,
    ChangeCharacterData        = 12,
    ChangeXmlDeclaration       = 13,
    ChangeDTD                  = 14,
    
    Undefined                  = 15,

    ChangeAttr                 = 16,
}

//
// XmlDiffDescriptorType
//
internal enum XmlDiffDescriptorType
{
    Move,
    PrefixChange,
    NamespaceChange,
}

//
// XmlDiffNodeType
//
internal enum XmlDiffNodeType
{
    XmlDeclaration        = -2,
    DocumentType          = -1,
    None                  = 0,
    Element               = XmlNodeType.Element,
    Attribute             = XmlNodeType.Attribute,
    Text                  = XmlNodeType.Text,
    CDATA                 = XmlNodeType.CDATA,
    Comment               = XmlNodeType.Comment,
    Document              = XmlNodeType.Document,
    EntityReference       = XmlNodeType.EntityReference,
    ProcessingInstruction = XmlNodeType.ProcessingInstruction,
    SignificantWhitespace = XmlNodeType.SignificantWhitespace,

    Namespace             = 100,
    ShrankNode            = 101,
}

internal enum TriStateBool {
    Yes,
    No,
    DontKnown,
}


//////////////////////////////////////////////////////////////////
// XmlPatch
//
/// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff"]/*' />
/// <summary>
///    Compares two documents or fragments. 
/// </summary>
public class XmlDiff
{
    private class XmlDiffNodeListMember
    {
        internal XmlDiffNode            _node;
        internal XmlDiffNodeListMember  _next;

        internal XmlDiffNodeListMember( XmlDiffNode node, XmlDiffNodeListMember next )
        {
            Debug.Assert( node != null );
            _node = node;
            _next = next;
        }
    }

    private class XmlDiffNodeListHead
    {
        internal XmlDiffNodeListMember _first;
        internal XmlDiffNodeListMember _last;

        internal XmlDiffNodeListHead( XmlDiffNodeListMember firstMember )
        {
            Debug.Assert( firstMember != null );
            _first = firstMember;
            _last = firstMember;
        }
    }

// Fields
	// Options flags
    bool _bIgnoreChildOrder  = false;
    bool _bIgnoreComments    = false;
    bool _bIgnorePI          = false;
    bool _bIgnoreWhitespace  = false;
    bool _bIgnoreNamespaces  = false;
    bool _bIgnorePrefixes    = false;
    bool _bIgnoreXmlDecl     = false;
    bool _bIgnoreDtd         = false;

    XmlDiffAlgorithm _algorithm = XmlDiffAlgorithm.Auto;

    // compared documents
    internal XmlDiffDocument _sourceDoc = null;
    internal XmlDiffDocument _targetDoc = null;

    // nodes sorted according to post-order numbering
    internal XmlDiffNode[] _sourceNodes = null;
    internal XmlDiffNode[] _targetNodes = null;

    internal TriStateBool _fragments = TriStateBool.DontKnown;

	private const int MininumNodesForQuicksort = 5;
    private const int MaxTotalNodesCountForTreeDistance = 256;

    // Tracing
#if DEBUG
    internal static BooleanSwitch T_Phases = new BooleanSwitch( "Phases", "Traces the current phase of the algorithm, number of compared nodes etc." );
    internal static BooleanSwitch T_LoadedDoc = new BooleanSwitch( "Loaded Document", "Dumps the loaded xml document." );
    internal static BooleanSwitch T_ForestDistance = new BooleanSwitch( "Forest Distance", "Trace the _forestDist array in each call of ComputeTreeDistance." );
    internal static BooleanSwitch T_EditScript = new BooleanSwitch( "Edit Script", "Traces the edit script." );
    internal static BooleanSwitch T_SubtreeMatching = new BooleanSwitch( "Identical subtrees matching", "Traces nodes that are shrinked when identical subtrees are matched." );
    internal static BooleanSwitch T_Tree = new BooleanSwitch( "Tree", "Traces tree that is passed to tree distance algorithm." );
#endif

    // Performance measurement
#if MEASURE_PERF
    public XmlDiffPerf _xmlDiffPerf = new XmlDiffPerf();
#endif

// Constructors
    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.XmlDiff1"]/*' />
    /// <summary>
    ///    Constructs XmlDiff object with default options.
    /// </summary>
    public XmlDiff() 
    {
#if DEBUG
        // TODO: this is temporary until I figure out why the config file does not work
        EnableTraceSwitches();
#endif
    }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.XmlDiff2"]/*' />
    /// <summary>
    ///    Constructs XmlDiff object with the given options. The values of XmlDiffOptions
    ///    may be combined using the operator '|'.
    /// </summary>
    public XmlDiff( XmlDiffOptions options ) : this()
    {
        Options = options;
    }

// Properties
    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.NamespaceUri"]/*' />
    /// <summary>
    ///    XmlDiff namespace. The diffgram nodes belongs to this namespace.
    /// </summary>
    public const string NamespaceUri        = "http://schemas.microsoft.com/xmltools/2002/xmldiff";
    internal const string Prefix            = "xd";
    internal const string XmlnsNamespaceUri = "http://www.w3.org/2000/xmlns/";

	// Options flags
    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.IgnoreChildren"]/*' />
    /// <summary>
    ///    If true, the order of child nodes of each element will be ignored when comparing 
    ///    the documents/fragments.
    /// </summary>
    public bool IgnoreChildOrder { get { return _bIgnoreChildOrder; } set { _bIgnoreChildOrder = value; } }
    
    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.IgnoreComments"]/*' />
    /// <summary>
    ///    If true, all comments in the compared documents/fragments will be ignored.
    /// </summary>
    public bool IgnoreComments   { get { return _bIgnoreComments; }   set { _bIgnoreComments = value; } }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.IgnorePI"]/*' />
    /// <summary>
    ///    If true, all processing instructions in the compared documents/fragments will be ignored.
    /// </summary>
    public bool IgnorePI		 { get { return _bIgnorePI ; }		  set { _bIgnorePI = value; } }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.IgnoreWhitespace"]/*' />
    /// <summary>
    ///    If true, all whitespace nodes in the compared documents/fragments will be ignored. Also, all
    ///    text nodes and values of attributes will be normalized; whitespace sequences will be replaced
    ///    by single space and beginning and trailing whitespaces will be trimmed.
    /// </summary>
    public bool IgnoreWhitespace { get { return _bIgnoreWhitespace; } set { _bIgnoreWhitespace = value; } }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.IgnoreNamespace"]/*' />
    /// <summary>
    ///    If true, the namespaces will be ignored when comparing the names of elements and attributes.
    ///    This also mean that the prefixes will be ignored too as if the IgnorePrefixes option is true.
    /// </summary>
    public bool IgnoreNamespaces { get { return _bIgnoreNamespaces; } set { _bIgnoreNamespaces = value; } }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.IgnorePrefixes"]/*' />
    /// <summary>
    ///    If true, the prefixes will be ignored when comparing the names of elements and attributes. 
    ///    The namespaces will not ne ignored unless IgnoreNamespaces flag is true.
    /// </summary>
    public bool IgnorePrefixes   { get { return _bIgnorePrefixes;   } set { _bIgnorePrefixes = value; } }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.IgnoreXmlDecl"]/*' />
    /// <summary>
    ///    If true, the xml declarations will not be compared.
    /// </summary>
    public bool IgnoreXmlDecl    { get { return _bIgnoreXmlDecl;    } set { _bIgnoreXmlDecl = value; } }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.IgnoreDtd"]/*' />
    /// <summary>
    ///    If true, the xml declarations will not be compared.
    /// </summary>
    public bool IgnoreDtd        { get { return _bIgnoreDtd;        } set { _bIgnoreDtd = value; } }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.Options"]/*' />
    /// <summary>
    ///    Options used when comparing xml documents/fragments.
    /// </summary>
	public XmlDiffOptions Options 
	{
        set {
            IgnoreChildOrder = ( ( (int)value & (int)(XmlDiffOptions.IgnoreChildOrder) ) > 0 ) ;
            IgnoreComments   = ( ( (int)value & (int)(XmlDiffOptions.IgnoreComments)   ) > 0 ) ;
            IgnorePI         = ( ( (int)value & (int)(XmlDiffOptions.IgnorePI)         ) > 0 ) ;
            IgnoreWhitespace = ( ( (int)value & (int)(XmlDiffOptions.IgnoreWhitespace) ) > 0 ) ;
            IgnoreNamespaces = ( ( (int)value & (int)(XmlDiffOptions.IgnoreNamespaces) ) > 0 ) ;
            IgnorePrefixes   = ( ( (int)value & (int)(XmlDiffOptions.IgnorePrefixes)   ) > 0 ) ;
            IgnoreXmlDecl    = ( ( (int)value & (int)(XmlDiffOptions.IgnoreXmlDecl)    ) > 0 ) ;
            IgnoreDtd        = ( ( (int)value & (int)(XmlDiffOptions.IgnoreDtd)        ) > 0 ) ;
        }
    }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.Algorithm"]/*' />
    /// <summary>
    ///    Algorithm that will be used for XML comparison.
    /// </summary>
    public XmlDiffAlgorithm Algorithm {
        get { 
            return _algorithm;
        }
        set {
            _algorithm = value;
        }
    }

// Methods
    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.Compare1"]/*' />
    /// <summary>
    ///    Compares two XML documents or fragments.
    /// </summary>
    /// <param name="sourceFile">The original xml document or fragment filename</param>
    /// <param name="changedFile">The changed xml document or fragment filename.</param>
    /// <param name="bFragments">If true, the passed files contain xml fragments; otherwise the files must contain xml documents.</param>
    /// <returns>True, if the documents/fragments are identical.</returns>
    public bool Compare( string sourceFile, string changedFile, bool bFragments ) 
    {
        return Compare( sourceFile, changedFile, bFragments, null );
    }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.Compare2"]/*' />
    /// <summary>
    ///    Compares two XML documents or fragments. 
    ///    If the diffgramWriter parameter is not null it will contain the list of changes 
    ///    between the two XML documents/fragments (diffgram).
    /// </summary>
    /// <param name="sourceFile">The original xml document or fragment filename</param>
    /// <param name="changedFile">The changed xml document or fragment filename.</param>
    /// <param name="bFragments">If true, the passed files contain xml fragments; otherwise the files must contain xml documents.</param>
    /// <param name="diffgramWriter">XmlWriter object for returning the list of changes (diffgram).</param>
    /// <returns>True, if the documents/fragments are identical.</returns>
    public bool Compare( string sourceFile, string changedFile, bool bFragments, XmlWriter diffgramWriter ) 
    {
        if ( sourceFile == null )
            throw new ArgumentNullException( "sourceFile" );
        if ( changedFile == null )
            throw new ArgumentNullException( "changedFile" );

        XmlReader sourceReader = null;
        XmlReader targetReader = null;

        try
        {
            _fragments = bFragments ? TriStateBool.Yes : TriStateBool.No;

            if ( bFragments )
                OpenFragments( sourceFile, changedFile, ref sourceReader, ref targetReader );
            else
                OpenDocuments( sourceFile, changedFile, ref sourceReader, ref targetReader );

            return Compare( sourceReader, targetReader, diffgramWriter );
        }
        finally
        {
            if ( sourceReader != null ) {
                sourceReader.Close();
                sourceReader = null;
            }
            if ( targetReader != null ) {
                targetReader.Close();
                targetReader = null;
            }
        }
    }

    private void OpenDocuments( String sourceFile, String changedFile, 
                                ref XmlReader sourceReader, ref XmlReader changedReader )
    {
        XmlTextReader tr = new XmlTextReader( sourceFile );
        tr.XmlResolver = null;
        sourceReader = tr;
        
        tr = new XmlTextReader( changedFile );
        tr.XmlResolver = null;
        changedReader = tr;
    }

    private void OpenFragments( String sourceFile, String changedFile, 
                                ref XmlReader sourceReader, ref XmlReader changedReader )
    {
        FileStream sourceStream = null;
        FileStream changedStream = null;

        try
        {
            XmlNameTable nameTable = new NameTable();
            XmlParserContext sourceParserContext = new XmlParserContext( nameTable,
                                                                        new XmlNamespaceManager( nameTable ), 
                                                                        string.Empty, 
                                                                        System.Xml.XmlSpace.Default );
            XmlParserContext changedParserContext = new XmlParserContext( nameTable,
                                                                        new XmlNamespaceManager( nameTable ), 
                                                                        string.Empty, 
                                                                        System.Xml.XmlSpace.Default );
            sourceStream = new FileStream( sourceFile, FileMode.Open, FileAccess.Read );
            changedStream = new FileStream( changedFile, FileMode.Open, FileAccess.Read );

            XmlTextReader tr = new XmlTextReader( sourceStream, XmlNodeType.Element, sourceParserContext );
            tr.XmlResolver = null;
            sourceReader = tr;

            tr = new XmlTextReader( changedStream, XmlNodeType.Element, changedParserContext ); 
            tr.XmlResolver = null;
            changedReader = tr;
        }
        catch 
        {
            if ( sourceStream != null )
                sourceStream.Close();
            if ( changedStream != null )
                changedStream.Close();
            throw;
        }
    }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.Compare3"]/*' />
    /// <summary>
    ///    Compares two XML documents or fragments.
    /// </summary>
    /// <param name="sourceReader">XmlReader representing the original xml document or fragment.</param>
    /// <param name="changedFile">XmlReaser representing the changed xml document or fragment.</param>
    /// <returns>True, if the documents/fragments are identical.</returns>
    public bool Compare( XmlReader sourceReader, XmlReader changedReader ) 
    {
        return Compare( sourceReader, changedReader, null );
    }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.Compare4"]/*' />
    /// <summary>
    ///    Compares two XML documents or fragments.
    ///    If the diffgramWriter parameter is not null it will contain the list of changes 
    ///    between the two XML documents/fragments (diffgram).
    /// </summary>
    /// <param name="sourceReader">XmlReader representing the original xml document or fragment.</param>
    /// <param name="changedFile">XmlReaser representing the changed xml document or fragment.</param>
    /// <param name="diffgramWriter">XmlWriter object for returning the list of changes (diffgram).</param>
    /// <returns>True, if the documents/fragments are identical.</returns>
    public bool Compare( XmlReader sourceReader, XmlReader changedReader, XmlWriter diffgramWriter ) 
    {
        if ( sourceReader == null )
            throw new ArgumentNullException( "sourceReader" );
        if ( changedReader == null )
            throw new ArgumentNullException( "changedReader" );

        try
        {
            XmlHash xmlHash = new XmlHash( this );

#if MEASURE_PERF
            _xmlDiffPerf.Clean();
            int startTickCount = Environment.TickCount;
#endif
            // load source document
            _sourceDoc = new XmlDiffDocument( this );
            _sourceDoc.Load( sourceReader, xmlHash );

#if DEBUG
            Trace.WriteLineIf( T_Phases.Enabled, "* Source document loaded: " + _sourceDoc.NodesCount + " nodes." );
#endif
        
            // load target document
            _targetDoc = new XmlDiffDocument( this );
            _targetDoc.Load( changedReader, xmlHash );

            if ( _fragments == TriStateBool.DontKnown ) {
                _fragments = ( _sourceDoc.IsFragment || _targetDoc.IsFragment ) ? TriStateBool.Yes : TriStateBool.No;
            }

#if DEBUG
            Trace.WriteLineIf( T_Phases.Enabled, "* Target document loaded: " + _targetDoc.NodesCount + " nodes." );
#endif
#if MEASURE_PERF
            _xmlDiffPerf._loadTime = Environment.TickCount - startTickCount;
#endif

            // compare
            return Diff( diffgramWriter );
        }
        finally
        {
            _sourceDoc = null;
            _targetDoc = null;
        }
    }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.Compare5"]/*' />
    /// <summary>
    ///    Compares two XML nodes.
    ///    If the diffgramWriter parameter is not null it will contain the list of changes 
    ///    between the two XML documents/fragments (diffgram).
    /// </summary>
    /// <param name="sourceNode">Original XML node</param>
    /// <param name="changedNode">Changed XML node</param>
    /// <param name="diffgramWriter">XmlWriter object for returning the list of changes (diffgram).</param>
    /// <returns>True, if the documents/fragments are identical.</returns>
    public bool Compare( XmlNode sourceNode, XmlNode changedNode )
    {
        return Compare( sourceNode, changedNode, null );
    }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.Compare6"]/*' />
    /// <summary>
    ///    Compares two XML nodes.
    ///    If the diffgramWriter parameter is not null it will contain the list of changes 
    ///    between the two XML documents/fragments (diffgram).
    /// </summary>
    /// <param name="sourceNode">Original XML node</param>
    /// <param name="changedNode">Changed XML node</param>
    /// <param name="diffgramWriter">XmlWriter object for returning the list of changes (diffgram).</param>
    /// <returns>True, if the documents/fragments are identical.</returns>
    public bool Compare( XmlNode sourceNode, XmlNode changedNode, XmlWriter diffgramWriter )
    {
        if ( sourceNode == null )
            throw new ArgumentNullException( "sourceNode" );
        if ( changedNode == null )
            throw new ArgumentNullException( "changedNode" );

        try
        {
            XmlHash xmlHash = new XmlHash( this );

#if MEASURE_PERF
            _xmlDiffPerf.Clean();
            int startTickCount = Environment.TickCount;
#endif
            // load source document
            _sourceDoc = new XmlDiffDocument( this );
            _sourceDoc.Load( sourceNode, xmlHash );

#if DEBUG
            Trace.WriteLineIf( T_Phases.Enabled, "* Source document loaded: " + _sourceDoc.NodesCount + " nodes." );
#endif
        
            // load target document
            _targetDoc = new XmlDiffDocument( this );
            _targetDoc.Load( changedNode, xmlHash );

            _fragments = ( sourceNode.NodeType != XmlNodeType.Document || 
                           changedNode.NodeType != XmlNodeType.Document ) ? TriStateBool.Yes : TriStateBool.No;

#if DEBUG
            Trace.WriteLineIf( T_Phases.Enabled, "* Target document loaded: " + _targetDoc.NodesCount + " nodes." );
#endif
#if MEASURE_PERF
            _xmlDiffPerf._loadTime = Environment.TickCount - startTickCount;
#endif

            // compare
            return Diff( diffgramWriter );
        }
        finally
        {
            _sourceDoc = null;
            _targetDoc = null;
        }
    }

    private bool Diff( XmlWriter diffgramWriter )
    {
        try
        {
#if MEASURE_PERF 
            int startTickCount = Environment.TickCount;
#endif
            // compare hash values of root nodes and return if same (the hash values were computed during load)
            if ( IdenticalSubtrees( _sourceDoc, _targetDoc ) )
            {
				if ( diffgramWriter != null )
				{
					Diffgram emptyDiffgram = new DiffgramGenerator( this ).GenerateEmptyDiffgram();

					emptyDiffgram.WriteTo( diffgramWriter );
					diffgramWriter.Flush();
				}
#if DEBUG
                Trace.WriteLineIf( T_Phases.Enabled, "* Done." );
#endif
#if MEASURE_PERF 
                _xmlDiffPerf._identicalOrNoDiffWriterTime = Environment.TickCount - startTickCount;
#endif
                return true;
            }
			else if ( diffgramWriter == null )
            {
#if DEBUG
                Trace.WriteLineIf( T_Phases.Enabled, "* Done." );
#endif
#if MEASURE_PERF 
                _xmlDiffPerf._identicalOrNoDiffWriterTime = Environment.TickCount - startTickCount;
#endif
				return false;
            }
#if MEASURE_PERF 
            _xmlDiffPerf._identicalOrNoDiffWriterTime = Environment.TickCount - startTickCount;
#endif

            // Match & shrink identical subtrees
#if DEBUG
            Trace.WriteLineIf( T_Phases.Enabled, "* Matching identical subtrees..." );
#endif
#if MEASURE_PERF 
            startTickCount = Environment.TickCount;
#endif
            MatchIdenticalSubtrees();

#if MEASURE_PERF 
            _xmlDiffPerf._matchTime = Environment.TickCount - startTickCount;
#endif
#if DEBUG
            Trace.WriteLineIf( T_Phases.Enabled, "* Source document shrinked: " + _sourceDoc.NodesCount + " nodes" );
            Trace.WriteLineIf( T_Phases.Enabled, "* Target document shrinked: " + _targetDoc.NodesCount + " nodes" );
#endif

            Diffgram diffgram = null;

            // Choose algorithm
            switch ( _algorithm ) {
                case XmlDiffAlgorithm.Fast:
                    diffgram = WalkTreeAlgorithm();
                    break;
                case XmlDiffAlgorithm.Precise:
                    diffgram = ZhangShashaAlgorithm();
                    break;
                case XmlDiffAlgorithm.Auto:
                    if ( _sourceDoc.NodesCount + _targetDoc.NodesCount <= MaxTotalNodesCountForTreeDistance )
                        diffgram = ZhangShashaAlgorithm();
                    else
                        diffgram = WalkTreeAlgorithm();
                    break;
                default:
                    Debug.Assert( false );
                    break;
            }

            // Output the diffgram
#if MEASURE_PERF 
            startTickCount = Environment.TickCount;
#endif
            Debug.Assert( diffgramWriter != null );
            diffgram.WriteTo( diffgramWriter );
            diffgramWriter.Flush();
#if MEASURE_PERF 
            _xmlDiffPerf._diffgramSaveTime = Environment.TickCount - startTickCount;
#endif

#if DEBUG
            Trace.WriteLineIf( T_Phases.Enabled, "* Done." );
#endif
        }
        finally
        {
            _sourceNodes = null;
            _targetNodes = null;

        }

        return false;
    }

    Diffgram ZhangShashaAlgorithm()
    {
        // Preprocess the trees for the tree-to-tree comparison algorithm and diffgram generation.
        // This includes post-order numbering of all nodes (the source and target nodes are stored
        // in post-order in the _sourceNodes and _targetNodes arrays).
#if DEBUG
        Trace.WriteLineIf( T_Phases.Enabled, "* Using Zhang-Shasha Algorithm" );
        Trace.WriteLineIf( T_Phases.Enabled, "* Preprocessing trees..." );
#endif
#if MEASURE_PERF 
        int startTickCount = Environment.TickCount;
#endif
        PreprocessTree( _sourceDoc, ref _sourceNodes );
        PreprocessTree( _targetDoc, ref _targetNodes );
#if MEASURE_PERF 
        _xmlDiffPerf._preprocessTime = Environment.TickCount - startTickCount;
#endif

        // Find minimal edit distance between the trees
#if DEBUG
        Trace.WriteLineIf( T_Phases.Enabled, "* Computing minimal tree distance..." );
        if ( T_Tree.Enabled ) 
        {
            Trace.WriteLine( "Source tree: " );
            _sourceDoc.Dump( string.Empty );
            Trace.WriteLine( "Target tree: " );
            _targetDoc.Dump( string.Empty );
        }
#endif
#if MEASURE_PERF 
        startTickCount = Environment.TickCount;
#endif

        EditScript editScript = ( new MinimalTreeDistanceAlgo( this ) ).FindMinimalDistance();
        Debug.Assert( editScript != null );
#if MEASURE_PERF 
        _xmlDiffPerf._treeDistanceTime = Environment.TickCount - startTickCount;
#endif
#if DEBUG 
        if ( T_EditScript.Enabled )
        {
            Trace.Write( "\nMinimal edit script: \n" ); 
            editScript.Dump();
        }
#endif

        // Generate the diffgram
#if DEBUG
        Trace.WriteLineIf( T_Phases.Enabled, "* Generating diffgram..." );
#endif
#if MEASURE_PERF 
        startTickCount = Environment.TickCount;
#endif
        Diffgram diffgram = new DiffgramGenerator( this ).GenerateFromEditScript( editScript );

#if MEASURE_PERF 
        _xmlDiffPerf._diffgramGenerationTime = Environment.TickCount - startTickCount;
#endif
        return diffgram;
    }

    private void PreprocessTree( XmlDiffDocument doc, ref XmlDiffNode[] postOrderArray )
    {
        // allocate the array for post-ordered nodes.
        // The index 0 is not used; this is to have the consistent indexing of all arrays in the algorithm;
        postOrderArray = new XmlDiffNode[ doc.NodesCount + 1 ];
        postOrderArray[0] = null;

        // recursivelly process all nodes
        int index = 1;
        PreprocessNode( doc, ref postOrderArray, ref index );

        // root node is a 'key root' node
        doc._bKeyRoot = true;

        Debug.Assert( index - 1 == doc.NodesCount );
    }

    private void PreprocessNode( XmlDiffNode node, ref XmlDiffNode[] postOrderArray, ref int currentIndex )
    {
        // process children
		if ( node.HasChildNodes )
		{
			Debug.Assert( node.FirstChildNode != null );

#if DEBUG
            int nodesCount = 0;
#endif
			XmlDiffNode curChild = node.FirstChildNode;
			curChild._bKeyRoot = false;
			for (;;)
			{
				PreprocessNode( curChild, ref postOrderArray, ref currentIndex );
#if DEBUG
				nodesCount += curChild.NodesCount;
#endif

				curChild = curChild._nextSibling;
                // 'key root' node is the root node and each node that has a previous sibling node
				if ( curChild != null ) 
                    curChild._bKeyRoot = true;
				else break;
			}

            // leftist leaf in the subtree rooted at 'node'
			node.Left = node.FirstChildNode.Left; 
#if DEBUG
			Debug.Assert( node.NodesCount == nodesCount + 1 );
#endif
		}
		else
		{
            // leftist leaf in the subtree rooted at 'node'
			node.Left = currentIndex; 
            // total number of nodes in the subtree rooted at 'node'
			node.NodesCount = 1;
		}

#if DEBUG
    node._index = currentIndex;
#endif
        // put the node in post-order array
        Debug.Assert( postOrderArray.Length > currentIndex );
        postOrderArray[ currentIndex++ ] = node;
    }

    // Finds identical subtrees in both trees and skrinks them into XmlDiffShrankNode instances
    private void MatchIdenticalSubtrees()
    {
        Hashtable sourceUnmatchedNodes = new Hashtable( 16 );
        Hashtable targetUnmatchedNodes = new Hashtable( 16 );

        Queue sourceNodesToExpand = new Queue( 16 );
        Queue targetNodesToExpand = new Queue( 16 );

        sourceNodesToExpand.Enqueue( _sourceDoc );
        targetNodesToExpand.Enqueue( _targetDoc );

        AddNodeToHashTable( sourceUnmatchedNodes, _sourceDoc );
        AddNodeToHashTable( targetUnmatchedNodes, _targetDoc );

        while ( sourceNodesToExpand.Count > 0  ||
                targetNodesToExpand.Count > 0 )
        {
            // Expand next level of source nodes and add them to the sourceUnmatchedNodes hashtable.
            // Leave the parents of expanded nodes in the sourceNodesToExpand queue for later use.
            {
                IEnumerator en = sourceNodesToExpand.GetEnumerator();
                while ( en.MoveNext() )
                {
                    XmlDiffParentNode sourceParentNode = (XmlDiffParentNode) en.Current;
                    Debug.Assert( !sourceParentNode._bExpanded );

                    sourceParentNode._bExpanded = true;
                    
                    if ( !sourceParentNode.HasChildNodes )
                        continue;

                    XmlDiffNode curSourceNode = sourceParentNode._firstChildNode;
                    while ( curSourceNode != null )
                    {
                        AddNodeToHashTable( sourceUnmatchedNodes, curSourceNode );
                        curSourceNode = curSourceNode._nextSibling;
                    }
                }
            }

            // Expand next level of target nodes and try to match them against the sourceUnmatchedNodes hashtable.
            // to find matching node. 
            int count = targetNodesToExpand.Count;
            for ( int i = 0; i < count; i++ )
            {
                XmlDiffParentNode targetParentNode = (XmlDiffParentNode) targetNodesToExpand.Dequeue();
                Debug.Assert( !targetParentNode._bExpanded );

                if ( !NodeInHashTable( targetUnmatchedNodes, targetParentNode ) ) {
                    continue;
                }

                targetParentNode._bExpanded = true;
                
                if ( !targetParentNode.HasChildNodes )
                    continue;

                XmlDiffNode curTargetNode = targetParentNode._firstChildNode;
                while ( curTargetNode != null )
                {
                    Debug.Assert( !( curTargetNode is XmlDiffAttributeOrNamespace ) );

                    // try to match
                    XmlDiffNode firstSourceNode = null;
                    XmlDiffNodeListHead matchingSourceNodes = (XmlDiffNodeListHead) sourceUnmatchedNodes[ curTargetNode.HashValue ];

                    if ( matchingSourceNodes != null  ) {
                        // find matching node and remove it from the hashtable
                        firstSourceNode = HTFindAndRemoveMatchingNode( sourceUnmatchedNodes, matchingSourceNodes, curTargetNode );
                    }
                    
                    // no match
                    if ( firstSourceNode == null || 
                         // do not shrink xml declarations and DTD
                         (int)curTargetNode.NodeType < 0 )
                    {
                        if ( curTargetNode.HasChildNodes )
                            targetNodesToExpand.Enqueue( curTargetNode );
                        else
                            curTargetNode._bExpanded = true;

                        AddNodeToHashTable( targetUnmatchedNodes, curTargetNode );
                        curTargetNode = curTargetNode._nextSibling;
                        continue;
                    }

                    HTRemoveAncestors( sourceUnmatchedNodes, firstSourceNode );
                    HTRemoveDescendants( sourceUnmatchedNodes, firstSourceNode );

                    HTRemoveAncestors( targetUnmatchedNodes, curTargetNode );
                    // there are no target node descendants in the hash table

                    // find matching interval - starts at startSourceNode and startTargetNode
                    XmlDiffNode firstTargetNode = curTargetNode;
                    XmlDiffNode lastSourceNode = firstSourceNode;
                    XmlDiffNode lastTargetNode = firstTargetNode;

                    curTargetNode = curTargetNode._nextSibling;
                    XmlDiffNode curSourceNode = firstSourceNode._nextSibling;

                    while ( curTargetNode != null  &&  
                            curSourceNode != null  &&
                            curSourceNode.NodeType != XmlDiffNodeType.ShrankNode )
                    {
                        // still matches and the nodes has not been matched elsewhere
                        if ( IdenticalSubtrees( curSourceNode, curTargetNode )  &&
                             sourceUnmatchedNodes.Contains( curSourceNode.HashValue ) )
                        {
                            HTRemoveNode( sourceUnmatchedNodes, curSourceNode );
                            HTRemoveDescendants( sourceUnmatchedNodes, curSourceNode );
                        }
                        // no match -> end of interval
                        else
                            break;

                        lastSourceNode = curSourceNode;
                        curSourceNode = curSourceNode._nextSibling;
                        //Debug.Assert( curSourceNode == null || curSourceNode.NodeType != XmlDiffNodeType.ShrankNode );

                        lastTargetNode = curTargetNode;
                        curTargetNode = curTargetNode._nextSibling;
                        //Debug.Assert( curTargetNode == null || curTargetNode.NodeType != XmlDiffNodeType.ShrankNode );
                    }

                    if ( firstSourceNode != lastSourceNode || 
                         firstSourceNode.NodeType != XmlDiffNodeType.Element ) {
                        ShrinkNodeInterval( firstSourceNode, lastSourceNode, firstTargetNode, lastTargetNode);
                    }
                    else {
                        XmlDiffElement e = (XmlDiffElement)firstSourceNode;
                        if ( e.FirstChildNode != null || e._attributes != null ) {
                            ShrinkNodeInterval( firstSourceNode, lastSourceNode, firstTargetNode, lastTargetNode);
                        }
                    }
                }
            }

            // Walk through the newly expanded source nodes (=children of nodes in sourceNodesToExpand queue)
            // and try to match them against targetUnmatchedNodes hashtable.
            count = sourceNodesToExpand.Count;
            for ( int i = 0; i < count; i++ )
            {
                XmlDiffParentNode sourceParentNode = (XmlDiffParentNode) sourceNodesToExpand.Dequeue();
                Debug.Assert( sourceParentNode._bExpanded );

                if ( !sourceParentNode.HasChildNodes )
                    continue;

                XmlDiffNode curSourceNode = sourceParentNode._firstChildNode;
                while ( curSourceNode != null )
                {
                    // it it's an attribute or the node has already been matched -> continue
                    Debug.Assert( ! ( curSourceNode is XmlDiffAttributeOrNamespace ) ) ;
                    if ( curSourceNode is XmlDiffShrankNode ||
                         !NodeInHashTable( sourceUnmatchedNodes, curSourceNode ) )
                    {
                        curSourceNode = curSourceNode._nextSibling;
                        continue;
                    }

                    // try to match
                    XmlDiffNode firstTargetNode = null;
                    XmlDiffNodeListHead matchingTargetNodes = (XmlDiffNodeListHead) targetUnmatchedNodes[ curSourceNode.HashValue ];

                    if ( matchingTargetNodes != null  ) {
                        // find matching node and remove it from the hashtable
                        firstTargetNode = HTFindAndRemoveMatchingNode( targetUnmatchedNodes, matchingTargetNodes, curSourceNode );
                    }
                    
                    // no match
                    if ( firstTargetNode == null || 
                         // do not shrink xml declarations and DTD
                         (int)curSourceNode.NodeType < 0 )
                    {
                        if ( curSourceNode.HasChildNodes )
                            sourceNodesToExpand.Enqueue( curSourceNode );
                        else
                            curSourceNode._bExpanded = true;

                        curSourceNode = curSourceNode._nextSibling;
                        continue;
                    }

                    HTRemoveAncestors( targetUnmatchedNodes, firstTargetNode );
                    HTRemoveDescendants( targetUnmatchedNodes, firstTargetNode );

                    if ( !HTRemoveNode( sourceUnmatchedNodes, curSourceNode ) )
                        Debug.Assert( false );
                    HTRemoveAncestors( sourceUnmatchedNodes, curSourceNode );
                    // there are no source node descendants in the hash table

                    Debug.Assert( !( curSourceNode is XmlDiffAttributeOrNamespace ) );

                    // find matching interval - starts at startSourceNode and startTargetNode
                    XmlDiffNode firstSourceNode = curSourceNode;
                    XmlDiffNode lastSourceNode = firstSourceNode;
                    XmlDiffNode lastTargetNode = firstTargetNode;

                    curSourceNode = curSourceNode._nextSibling;
                    XmlDiffNode curTargetNode = firstTargetNode._nextSibling;

                    while ( curSourceNode != null  && 
                            curTargetNode != null  &&
                            curTargetNode.NodeType != XmlDiffNodeType.ShrankNode )
                    {
                        // still matches and the nodes has not been matched elsewhere
                        if ( IdenticalSubtrees( curSourceNode, curTargetNode ) &&
                            sourceUnmatchedNodes.Contains( curSourceNode.HashValue ) &&
                            targetUnmatchedNodes.Contains( curTargetNode.HashValue ) )
                        {
                            HTRemoveNode( sourceUnmatchedNodes, curSourceNode );
                            HTRemoveDescendants( sourceUnmatchedNodes, curSourceNode );

                            HTRemoveNode( targetUnmatchedNodes, curTargetNode );
                            HTRemoveDescendants( targetUnmatchedNodes, curTargetNode );
                        }
                        // no match -> end of interval
                        else {
                            break;
                        }

                        lastSourceNode = curSourceNode;
                        curSourceNode = curSourceNode._nextSibling;

                        lastTargetNode = curTargetNode;
                        curTargetNode = curTargetNode._nextSibling;
                    }

                    if ( firstSourceNode != lastSourceNode || 
                         firstSourceNode.NodeType != XmlDiffNodeType.Element ) {
                        ShrinkNodeInterval( firstSourceNode, lastSourceNode, firstTargetNode, lastTargetNode);
                    }
                    else {
                        XmlDiffElement e = (XmlDiffElement)firstSourceNode;
                        if ( e.FirstChildNode != null || e._attributes != null ) {
                            ShrinkNodeInterval( firstSourceNode, lastSourceNode, firstTargetNode, lastTargetNode);
                        }
                    }
                }
            }
        }
    }

    private void AddNodeToHashTable( Hashtable hashtable, XmlDiffNode node )
    {
        Debug.Assert( hashtable != null );
        Debug.Assert( node != null );
        Debug.Assert( node.NodeType != XmlDiffNodeType.ShrankNode );

        ulong hashValue = node.HashValue;

        XmlDiffNodeListHead nodeListHead = (XmlDiffNodeListHead) hashtable[ hashValue ];
        if ( nodeListHead == null ) {
            hashtable[ hashValue ] = new XmlDiffNodeListHead( new XmlDiffNodeListMember( node, null ) ); 
        }
        else 
        {
            XmlDiffNodeListMember newMember = new XmlDiffNodeListMember( node, null );
            nodeListHead._last._next = newMember;
            nodeListHead._last = newMember;
        }
    }

    private bool HTRemoveNode( Hashtable hashtable, XmlDiffNode node )
    {
        Debug.Assert( hashtable != null );
        Debug.Assert( node != null );

        XmlDiffNodeListHead xmlNodeListHead = (XmlDiffNodeListHead) hashtable[ node.HashValue ];
        if ( xmlNodeListHead == null ) {
            return false;
        }

        XmlDiffNodeListMember xmlNodeList = xmlNodeListHead._first;
        if ( xmlNodeList._node == node )
        {
            if ( xmlNodeList._next == null ) {
                hashtable.Remove( node.HashValue );
            }
            else {
                Debug.Assert( xmlNodeListHead._first != xmlNodeListHead._last );
                xmlNodeListHead._first = xmlNodeList._next;
            }
        }
        else
        {
            if ( xmlNodeList._next == null ) {
                return false;
            }

            while ( xmlNodeList._next._node != node )
            {
                xmlNodeList = xmlNodeList._next;
                if ( xmlNodeList._next == null ) {
                    return false;
                }
            }

            xmlNodeList._next = xmlNodeList._next._next;
            if ( xmlNodeList._next == null ) {
                xmlNodeListHead._last = xmlNodeList;
            }
        }
        return true;
    }

    private bool NodeInHashTable( Hashtable hashtable, XmlDiffNode node ) {
        XmlDiffNodeListHead nodeListHeader = (XmlDiffNodeListHead)hashtable[node.HashValue];
        
        if ( nodeListHeader == null ) {
            return false;
        }

        XmlDiffNodeListMember nodeList = nodeListHeader._first;
        while ( nodeList != null ) {
            if ( nodeList._node == node ) {
                return true;
            }
            nodeList = nodeList._next;
        }
        return false;
    }

    // Shrinks the interval of nodes in one or mode XmlDiffShrankNode instances;
    // The shrank interval can contain only adjacent nodes => the position of two adjacent nodes differs by 1.
    private void ShrinkNodeInterval( XmlDiffNode firstSourceNode, XmlDiffNode lastSourceNode, 
                                     XmlDiffNode firstTargetNode, XmlDiffNode lastTargetNode )
    {
		XmlDiffNode sourcePreviousSibling = null;
		XmlDiffNode targetPreviousSibling = null;

        // calculate subtree hash value
        ulong hashValue = 0;
        XmlDiffNode curNode = firstSourceNode;
        for (;;) {
            hashValue += ( hashValue << 7 ) + curNode.HashValue;
            if ( curNode == lastSourceNode )
                break;
            curNode = curNode._nextSibling;
        }

#if DEBUG
        // calculate hash value of the second subtree and make sure they are the same
        ulong hashValue2 = 0;
        curNode = firstTargetNode;
        for (;;) {
            hashValue2 += ( hashValue2 << 7 ) + curNode.HashValue;
            if ( curNode == lastTargetNode )
                break;
            curNode = curNode._nextSibling;
        }
        Debug.Assert( hashValue == hashValue2 );
#endif

        // IgnoreChildOrder -> the nodes has been sorted by name/value before comparing.
        // 'Unsort' the matching interval of nodes (=sort by node position) to
        // group adjacent nodes that can be shrank.
        if ( IgnoreChildOrder  &&  firstSourceNode != lastSourceNode )
        {
			Debug.Assert( firstTargetNode != lastTargetNode );

            SortNodesByPosition( ref firstSourceNode, ref lastSourceNode, ref sourcePreviousSibling );
            SortNodesByPosition( ref firstTargetNode, ref lastTargetNode, ref targetPreviousSibling );
        }

#if DEBUG
        Trace.WriteIf( T_SubtreeMatching.Enabled, "Shrinking nodes: " );
        XmlDiffNode node = firstSourceNode;
        for (;;)
        {
            Trace.WriteIf( T_SubtreeMatching.Enabled, node.OuterXml );
            if ( node == lastSourceNode )
                break;
            node = node._nextSibling;
        }
        Trace.WriteIf( T_SubtreeMatching.Enabled, "\n" );
#endif

        // replace the interval by XmlDiffShrankNode instance
		XmlDiffShrankNode sourceShrankNode = ReplaceNodeIntervalWithShrankNode( firstSourceNode, 
                                                                                lastSourceNode, 
                                                                                sourcePreviousSibling,
                                                                                hashValue );
		XmlDiffShrankNode targetShrankNode = ReplaceNodeIntervalWithShrankNode( firstTargetNode, 
                                                                                lastTargetNode, 
                                                                                targetPreviousSibling,
                                                                                hashValue );

		sourceShrankNode.MatchingShrankNode = targetShrankNode;
		targetShrankNode.MatchingShrankNode = sourceShrankNode;
    }

	private XmlDiffShrankNode ReplaceNodeIntervalWithShrankNode( XmlDiffNode firstNode, 
		                                                         XmlDiffNode lastNode,
		                                                         XmlDiffNode previousSibling,
                                                                 ulong hashValue )
	{
		XmlDiffShrankNode shrankNode = new XmlDiffShrankNode( firstNode, lastNode, hashValue );
		XmlDiffParentNode parent = firstNode._parent;

		// find previous sibling node
		if ( previousSibling == null  &&
			 firstNode != parent._firstChildNode )
		{
			previousSibling = parent._firstChildNode;
			while ( previousSibling._nextSibling != firstNode )
				previousSibling = previousSibling._nextSibling;
		}

		// insert shrank node
		if ( previousSibling == null )
		{
			Debug.Assert( firstNode == parent._firstChildNode );

			shrankNode._nextSibling = parent._firstChildNode;
			parent._firstChildNode = shrankNode;
		}
		else
		{
			shrankNode._nextSibling = previousSibling._nextSibling;
			previousSibling._nextSibling = shrankNode;
		}
        shrankNode._parent = parent;

		// remove the node interval & count the total number of nodes
		XmlDiffNode tmpNode;
        int totalNodesCount = 0;
        do
        {
            tmpNode = shrankNode._nextSibling;
            totalNodesCount += tmpNode.NodesCount;
			shrankNode._nextSibling = shrankNode._nextSibling._nextSibling;

        } while ( tmpNode != lastNode );

        // adjust nodes count
        Debug.Assert( totalNodesCount > 0 );
        if ( totalNodesCount > 1 )
        {
            totalNodesCount--;
            while ( parent != null )
            {
                parent.NodesCount -= totalNodesCount;
                parent = parent._parent;
            }
        }

		return shrankNode;
	}

    private XmlDiffNode HTFindAndRemoveMatchingNode( Hashtable hashtable, XmlDiffNodeListHead nodeListHead, XmlDiffNode nodeToMatch )
    {
        Debug.Assert( hashtable != null );
        Debug.Assert( nodeListHead != null );

        // find matching node in the list
        XmlDiffNodeListMember nodeList = nodeListHead._first;
        XmlDiffNode node = nodeList._node;
        if ( IdenticalSubtrees( node, nodeToMatch ) ) {
            // remove the node itself
            if ( nodeList._next == null ) {
                hashtable.Remove( node.HashValue );
            }
            else {
                Debug.Assert( nodeListHead._first != nodeListHead._last );
                nodeListHead._first = nodeList._next;
            }
            return node;
        } 
        else {
            while ( nodeList._next != null ) {
                if ( IdenticalSubtrees( nodeList._node, nodeToMatch ) ) {
                    nodeList._next = nodeList._next._next;
                    if ( nodeList._next == null ) {
                        nodeListHead._last = nodeList;
                    }
                    return node;
                }
            }
            return null;
        }
    }

    private void HTRemoveAncestors( Hashtable hashtable, XmlDiffNode node )
    {
        XmlDiffNode curAncestorNode = node._parent;
        while ( curAncestorNode != null )
        {
            if ( !HTRemoveNode( hashtable, curAncestorNode ) )
                break;
            curAncestorNode._bSomeDescendantMatches = true;
            curAncestorNode = curAncestorNode._parent;
        }
    }

    private void HTRemoveDescendants( Hashtable hashtable, XmlDiffNode parent ) {
        if ( !parent._bExpanded || !parent.HasChildNodes )
            return;

        XmlDiffNode curNode = parent.FirstChildNode;
        for (;;)
        {
            Debug.Assert( curNode != null );
            if ( curNode._bExpanded  &&  curNode.HasChildNodes )
            {
                curNode = ((XmlDiffParentNode) curNode)._firstChildNode;
                continue;
            }

            HTRemoveNode( hashtable, curNode );

        TryNext:
            if ( curNode._nextSibling != null )
            {
                curNode = curNode._nextSibling;
                continue;
            }
            else if ( curNode._parent != parent )
            {
                curNode = curNode._parent;
                goto TryNext;
            }
            else {
                break;
            }
        }
    }


    private void RemoveDescendantsFromHashTable( Hashtable hashtable, XmlDiffNode parentNode )
    {
        
    }

    internal static void SortNodesByPosition( ref XmlDiffNode firstNode,
                                              ref XmlDiffNode lastNode,
                                              ref XmlDiffNode firstPreviousSibbling )
    {
        XmlDiffParentNode parent = firstNode._parent;
        
        // find previous sibling node for the first node
		if ( firstPreviousSibbling == null  &&
			 firstNode != parent._firstChildNode )
		{
			firstPreviousSibbling = parent._firstChildNode;
			while ( firstPreviousSibbling._nextSibling != firstNode )
				firstPreviousSibbling = firstPreviousSibbling._nextSibling;
		}

        // save the next sibling node for the last node
        XmlDiffNode lastNextSibling = lastNode._nextSibling;
        lastNode._nextSibling = null;

		// count the number of nodes to sort
		int count = 0;
		XmlDiffNode curNode = firstNode;
		while ( curNode != null )
		{
            count++;
			curNode = curNode._nextSibling;
		}

		Debug.Assert( count > 0 );
        if ( count >= MininumNodesForQuicksort ) 
            QuickSortNodes( ref firstNode, ref lastNode, count, firstPreviousSibbling, lastNextSibling );
        else
		    SlowSortNodes( ref firstNode, ref lastNode, firstPreviousSibbling, lastNextSibling );
	}

	static private void SlowSortNodes( ref XmlDiffNode firstNode, ref XmlDiffNode lastNode, 
		                        XmlDiffNode firstPreviousSibbling, XmlDiffNode lastNextSibling )
	{
        Debug.Assert( firstNode != null );
        Debug.Assert( lastNode != null );

        XmlDiffNode firstSortedNode = firstNode;
        XmlDiffNode lastSortedNode = firstNode;
        XmlDiffNode nodeToSort = firstNode._nextSibling;
		lastSortedNode._nextSibling = null;

        while ( nodeToSort != null )
        {
            XmlDiffNode curNode = firstSortedNode;
            if ( nodeToSort.Position < firstSortedNode.Position )
            {
                XmlDiffNode tmpNode = nodeToSort._nextSibling;
                
                nodeToSort._nextSibling = firstSortedNode;
                firstSortedNode = nodeToSort;

                nodeToSort = tmpNode;
            }
            else
            {
                while ( curNode._nextSibling != null &&
                        nodeToSort.Position > curNode._nextSibling.Position )
                    curNode = curNode._nextSibling;

                XmlDiffNode tmpNode = nodeToSort._nextSibling;

                if ( curNode._nextSibling == null )
                    lastSortedNode = nodeToSort;

                nodeToSort._nextSibling = curNode._nextSibling;
                curNode._nextSibling = nodeToSort;

                nodeToSort = tmpNode;
            }
        }

        // reconnect the sorted part in the tree
        if ( firstPreviousSibbling == null )
            firstNode._parent._firstChildNode = firstSortedNode;
        else
            firstPreviousSibbling._nextSibling = firstSortedNode;

        lastSortedNode._nextSibling = lastNextSibling;

        // return
        firstNode = firstSortedNode;
        lastNode = lastSortedNode;
	}

	static private void QuickSortNodes( ref XmlDiffNode firstNode, ref XmlDiffNode lastNode, 
		                         int count, XmlDiffNode firstPreviousSibbling, XmlDiffNode lastNextSibling )
	{
		Debug.Assert( count >= MininumNodesForQuicksort );
		Debug.Assert( MininumNodesForQuicksort >= 2 );

		// allocate & fill in the array
		XmlDiffNode[] sortArray = new XmlDiffNode[ count ];
		{
			XmlDiffNode curNode = firstNode;
			for ( int i = 0; i < count; i++, curNode = curNode._nextSibling )
			{
				Debug.Assert( curNode != null );
				sortArray[i] = curNode;
			}
		}

		// sort
		QuickSortNodesRecursion( ref sortArray, 0, count - 1 );

		// link the nodes
		for ( int i = 0; i < count - 1; i++ )
			sortArray[i]._nextSibling = sortArray[i+1];

        if ( firstPreviousSibbling == null )
            firstNode._parent._firstChildNode = sortArray[0];
        else
            firstPreviousSibbling._nextSibling = sortArray[0];

        sortArray[count-1]._nextSibling = lastNextSibling;

        // return
        firstNode = sortArray[0];
        lastNode = sortArray[count-1];
	}

	static private void QuickSortNodesRecursion( ref XmlDiffNode[] sortArray, int firstIndex, int lastIndex )
	{
		Debug.Assert( firstIndex < lastIndex );

		int pivotPosition = sortArray[ ( firstIndex + lastIndex ) / 2 ].Position;
		int i = firstIndex;
		int j = lastIndex;

		while ( i < j )
		{
			while ( sortArray[i].Position < pivotPosition ) i++;
			while ( sortArray[j].Position > pivotPosition ) j--;

			if ( i < j )
			{
				XmlDiffNode tmpNode = sortArray[i];
				sortArray[i] = sortArray[j];
				sortArray[j] = tmpNode;
				i++;
				j--;
			}
			else if ( i == j )
			{
				i++;
				j--;
			}
		}

		if ( firstIndex < j )
			QuickSortNodesRecursion( ref sortArray, firstIndex, j );
		if ( i < lastIndex )
			QuickSortNodesRecursion( ref sortArray, i, lastIndex );
	}

    // returs true if the two subtrees are identical
    private bool IdenticalSubtrees( XmlDiffNode node1, XmlDiffNode node2 )
    {
        if ( node1.HashValue != node2.HashValue )
            return false;
        else
#if VERIFY_HASH_VALUES
            return CompareSubtrees( node1, node2 );
#else
            return true;
#endif
    }

    // compares two subtrees and returns true if they are identical
    private bool CompareSubtrees( XmlDiffNode node1, XmlDiffNode node2 )
    {
		Debug.Assert( node1.NodeType != XmlDiffNodeType.Namespace );
		Debug.Assert( node2.NodeType != XmlDiffNodeType.Namespace );

        if ( !node1.IsSameAs( node2, this ) )
            return false;

        if ( !node1.HasChildNodes )
            return true;

        XmlDiffNode childNode1 = ((XmlDiffParentNode)node1).FirstChildNode;
        XmlDiffNode childNode2 = ((XmlDiffParentNode)node2).FirstChildNode;

        while ( childNode1 != null &&
                childNode2 != null )
        {
            if ( !CompareSubtrees( childNode1, childNode2 )) 
                return false;
            childNode1 = childNode1._nextSibling;
            childNode2 = childNode2._nextSibling;
        }

        Debug.Assert( childNode1 == null  &&  childNode2 == null );
        return ( childNode1 == childNode2 );
    }

// Static methods
    static internal bool IsChangeOperation( XmlDiffOperation op )
    {
        return ( (int)op >= (int)XmlDiffOperation.ChangeElementName ) &&
               ( (int)op <= (int)XmlDiffOperation.ChangeDTD );
    }

    static internal bool IsChangeOperationOnAttributesOnly( XmlDiffOperation op )
    {
        return (int)op >= (int)XmlDiffOperation.ChangeElementAttr1 &&
               (int)op <= (int)XmlDiffOperation.ChangeElementAttr3;
    }

    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.ParseOptions"]/*' />
    /// <summary>
    ///    Translates string representation of XmlDiff options into XmlDiffOptions enum.
    /// </summary>
    /// <param name="options">Value of the 'options' attribute of the 'xd:xmldiff' element in diffgram.</param>
    static public XmlDiffOptions ParseOptions( string options )
    {
        if ( options == null )
            throw new ArgumentNullException( "options" );

        if ( options == XmlDiffOptions.None.ToString() ) 
            return XmlDiffOptions.None;
        else {
            XmlDiffOptions optionsEnum = XmlDiffOptions.None;

            int j = 0, i = 0;
            while ( i < options.Length )
            {
                j = options.IndexOf( ' ', i );
                if ( j == -1 ) 
                    j = options.Length;

                string opt = options.Substring( i, j-i );

                switch ( opt )
                {
                    case "IgnoreChildOrder": optionsEnum |= XmlDiffOptions.IgnoreChildOrder;  break;
                    case "IgnoreComments":   optionsEnum |= XmlDiffOptions.IgnoreComments;    break;
                    case "IgnoreNamespaces": optionsEnum |= XmlDiffOptions.IgnoreNamespaces;  break;
                    case "IgnorePI":         optionsEnum |= XmlDiffOptions.IgnorePI;          break;
                    case "IgnorePrefixes":   optionsEnum |= XmlDiffOptions.IgnorePrefixes;    break;
                    case "IgnoreWhitespace": optionsEnum |= XmlDiffOptions.IgnoreWhitespace;  break;
                    case "IgnoreXmlDecl":    optionsEnum |= XmlDiffOptions.IgnoreXmlDecl;     break;
                    case "IgnoreDtd":        optionsEnum |= XmlDiffOptions.IgnoreDtd;         break;
                    default:
                        throw new ArgumentException("options" );
                }

                i = j + 1;
            }    

            return optionsEnum;
        }
    }

	internal string GetXmlDiffOptionsString()
	{
        string options = string.Empty;
		if ( _bIgnoreChildOrder )    options += XmlDiffOptions.IgnoreChildOrder.ToString() + " ";
		if ( _bIgnoreComments )      options += XmlDiffOptions.IgnoreComments.ToString() + " ";
		if ( _bIgnoreNamespaces )    options += XmlDiffOptions.IgnoreNamespaces.ToString() + " ";
		if ( _bIgnorePI )            options += XmlDiffOptions.IgnorePI.ToString() + " ";
		if ( _bIgnorePrefixes  )     options += XmlDiffOptions.IgnorePrefixes.ToString() + " ";
		if ( _bIgnoreWhitespace )    options += XmlDiffOptions.IgnoreWhitespace.ToString() + " ";
		if ( _bIgnoreXmlDecl )       options += XmlDiffOptions.IgnoreXmlDecl.ToString() + " ";
        if ( _bIgnoreDtd     )       options += XmlDiffOptions.IgnoreDtd.ToString() + " ";
        if ( options == string.Empty )      options = XmlDiffOptions.None.ToString();
		options.Trim();

		return options;
	}



    /// <include file='doc\XmlDiff.uex' path='docs/doc[@for="XmlDiff.VerifySource"]/*' />
    /// <summary>
    ///    Given a diffgram, this method verifies whether the given document/node is the original
    ///    source document/node for the diffgram. 
    /// </summary>
    /// <param name="node">Document/node to be verified.</param>
    /// <param name="hashValue">Value of the 'srcDocHash' attribute of the 'xd:xmldiff' element in diffgram.
    /// This is the hash value of the original source document. The document/node is verified if it yields
    /// the same hash value.</param>
    /// <param name="options">XmlDiff options selected when the document/node was compared. The hash value 
    /// of the document/node depends on these options.</param>
    /// <returns>True if the given document is the original source document for the diffgram.</returns>
    static public bool VerifySource( XmlNode node, ulong hashValue, XmlDiffOptions options )
    {
        if ( node == null )
            throw new ArgumentNullException( "node" );

        ulong computedHashValue = new XmlHash().ComputeHash( node, options );
        return hashValue == computedHashValue;
    }

    internal static string NormalizeText( string text )
    {
        char[] chars = text.ToCharArray();
        int i = 0;
        int j = 0;

        for (;;)
        {
            while ( j < chars.Length  &&  IsWhitespace( text[j] ) )
                j++;

            while ( j < chars.Length && !IsWhitespace( text[j] ) )
                chars[i++]=chars[j++];

            if ( j < chars.Length )
            {
                chars[i++]=' ';
                j++;
            }
            else
            {
                if ( j == 0 || i == 0)
                    return string.Empty;

                if ( IsWhitespace( chars[j-1] ))
                    i--;

                return new string( chars, 0, i );
            }
        }
    }

    internal static string NormalizeXmlDeclaration( string value )
    {
        value = value.Replace( '\'', '"' );
        return NormalizeText( value );
    }

    internal static bool IsWhitespace( char c )
    {
        return ( c == ' ' ||
                 c == '\t' ||
                 c == '\n' ||
                 c == '\r' );
    }

    private Diffgram WalkTreeAlgorithm() {
#if DEBUG
        Trace.WriteLineIf( T_Phases.Enabled, "* Using WalkTree Algorithm" );
#endif
        return ( new DiffgramGenerator( this ) ).GenerateFromWalkTree();
    }

#if DEBUG
    private static void EnableTraceSwitches()
    {
        T_Phases.Enabled            = false;
        T_LoadedDoc.Enabled         = false;
        T_ForestDistance.Enabled    = false;
        T_EditScript.Enabled        = false;
        T_SubtreeMatching.Enabled   = false;
        T_Tree.Enabled              = false;
    }
#endif
}
}
