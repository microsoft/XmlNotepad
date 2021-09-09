using System;
using System.Xml;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Collections.Generic;
using SR = XmlNotepad.StringResources;


namespace XmlNotepad
{

    public enum InsertPosition { Child, Before, After }

    /// <summary>
    /// This class normalizes the concept of parent for XmlTreeNode and XmlNode node.
    /// The reason for this is that XmlTreeNode.Parent returns null on root level nodes and
    /// XmlNode.ParentNode returns null on XmlAttributes. So this class provides one
    /// uniform way to insert and remove both TreeNodes and their associated XmlNodes. 
    /// </summary>
    /// <remarks>MCorning made public so subforms have access.</remarks>
    public class TreeParent
    {
        private bool _nodeInTree;    // is the reference node in the tree?
        private XmlTreeNode _parent;
        private TreeView _view;
        private XmlNode _xparent;
        private XmlNode _originalParent;
        private XmlDocument _doc;

        // There is no reference node, so the parent is the TreeView and XmlDocument.
        public TreeParent(TreeView view, XmlDocument doc)
        {
            this._view = view;
            this._doc = doc;
            this._xparent = doc;
        }

        public TreeParent(XmlTreeView xview, XmlDocument doc, XmlTreeNode node)
            : this(xview.TreeView, doc, node)
        {
        }


        // This constructor takes the reference node
        public TreeParent(TreeView view, XmlDocument doc, XmlTreeNode node)
        {
            //this.node = node;
            if (node != null) this._nodeInTree = ((TreeView)node.TreeView == view);
            this._view = view;
            this._doc = doc;
            this._parent = (XmlTreeNode)node.Parent;

            //this.xnode = node.Node;
            if (node.Parent != null)
            {
                this._xparent = ((XmlTreeNode)node.Parent).Node;
            }
            else if (node.Node != null)
            {
                this._xparent = node.Node.ParentNode;
            }
            if (this._xparent == null)
            {
                this._xparent = this._doc;
            }
            _originalParent = this._xparent;
            if (this._parent == null)
            {
                this._xparent = this._originalParent = this._doc;
            }
        }
        public int Count
        {
            get
            {
                if (_parent != null) return _parent.Children.Count;
                return _view.Nodes.Count;
            }
        }
        public TreeView View
        {
            get { return this._view; }
        }
        public XmlDocument Document
        {
            get { return this._doc; }
        }
        public bool IsNodeInTree
        {
            get { return this._nodeInTree; }
        }
        public XmlTreeNode GetChild(int i)
        {
            if (_parent != null) return (XmlTreeNode)_parent.Children[i];
            return (XmlTreeNode)_view.Nodes[i];
        }
        public void SetParent(XmlTreeNode parent)
        {
            this._parent = parent;
            if (parent != null && parent.Node != null)
            {
                this.SetXmlParent(parent.Node);
            }
        }
        void SetXmlParent(XmlNode parent)
        {
            if (parent != null)
            {
                this._xparent = parent;
                _doc = _xparent.OwnerDocument;
            }
        }
        public bool IsRoot
        {
            get { return _xparent == null || _xparent is XmlDocument; }
        }
        public bool IsElement
        {
            get { return _xparent is XmlElement; }
        }
        public XmlNode ParentNode
        {
            get { return this._xparent; }
        }
        public int AttributeCount
        {
            get
            {
                if (_xparent == null || _xparent.Attributes == null) return 0;
                return _xparent.Attributes.Count;
            }
        }
        public int ChildCount
        {
            get
            {
                if (_xparent != null && _xparent.HasChildNodes)
                    return _xparent.ChildNodes.Count;
                return 0;
            }
        }

        public void Insert(int pos, InsertPosition position, XmlTreeNode n, bool selectIt)
        {

            if (n.Node != null)
            {
                this.Insert(pos, position, n.Node);
            }

            int i = pos;
            if (position == InsertPosition.After) i++;
            if (_parent == null)
            {
                _view.Nodes.Insert(i, n);
            }
            else
            {
                _parent.Children.Insert(i, n);
                if (selectIt && !_parent.IsExpanded)
                {
                    _parent.Expand(); // this will change image index of leaf nodes.
                }
            }
            n.Invalidate();
            if (selectIt)
            {
                _view.SelectedNode = n;
            }
        }
        public void Insert(int i, InsertPosition position, XmlNode n)
        {
            if (n == null) return;
            if (n.NodeType == XmlNodeType.Attribute)
            {
                Debug.Assert(this._xparent is XmlElement);
                XmlElement pe = (XmlElement)this._xparent;
                if (pe.Attributes != null)
                {
                    XmlNode already = pe.Attributes.GetNamedItem(n.LocalName, n.NamespaceURI);
                    if (already != null)
                    {
                        throw new ApplicationException(SR.DuplicateAttribute);
                    }
                }
                if (pe.Attributes != null && i < pe.Attributes.Count)
                {
                    XmlAttribute refNode = this._xparent.Attributes[i];
                    if (position == InsertPosition.After)
                    {
                        pe.Attributes.InsertAfter((XmlAttribute)n, refNode);
                    }
                    else
                    {
                        pe.Attributes.InsertBefore((XmlAttribute)n, refNode);
                    }
                }
                else
                {
                    pe.Attributes.Append((XmlAttribute)n);
                }
            }
            else
            {
                i -= this.AttributeCount;
                if (this._xparent.HasChildNodes && i < this._xparent.ChildNodes.Count)
                {
                    XmlNode refNode = this._xparent.ChildNodes[i];
                    if (position == InsertPosition.After)
                    {
                        this._xparent.InsertAfter(n, refNode);
                    }
                    else
                    {
                        this._xparent.InsertBefore(n, refNode);
                    }
                }
                else
                {
                    this._xparent.AppendChild(n);
                }
            }
        }
        public void Remove(XmlTreeNode n)
        {
            if (n.Node != null)
            {
                Remove(n.Node);
            }
            n.Remove();
        }

        void Remove(XmlNode n)
        {
            if (n != null && this._originalParent != null)
            {
                if (n.NodeType == XmlNodeType.Attribute)
                {
                    Debug.Assert(this._originalParent is XmlElement);
                    this._originalParent.Attributes.Remove((XmlAttribute)n);
                }
                else
                {
                    this._originalParent.RemoveChild(n);
                }
            }
        }
    }

    public class EditNodeName : Command
    {
        private Command _cmd;

        public EditNodeName(XmlTreeNode node, XmlName newName, bool autoGenPrefixes)
        {
            if (node.Node == null)
            {
                throw new ArgumentException(SR.NodeNotCreated);
            }
            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    _cmd = new EditElementName(node, newName, autoGenPrefixes);
                    break;
                case XmlNodeType.Attribute:
                    _cmd = new EditAttributeName(node, newName, autoGenPrefixes);
                    break;
                case XmlNodeType.ProcessingInstruction:
                    _cmd = new EditProcessingInstructionName(node, newName.LocalName);
                    break;
                default:
                    throw new ArgumentException(
                        string.Format(SR.NodeNameNotEditable, node.NodeType.ToString()));
            }
        }
        public EditNodeName(XmlTreeNode node, string newName)
        {
            if (node.Node == null)
            {
                throw new ArgumentException(SR.NodeNotCreated);
            }
            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    _cmd = new EditElementName(node, newName);
                    break;
                case XmlNodeType.Attribute:
                    _cmd = new EditAttributeName(node, newName);
                    break;
                case XmlNodeType.ProcessingInstruction:
                    _cmd = new EditProcessingInstructionName(node, newName);
                    break;
                default:
                    throw new ArgumentException(
                        string.Format(SR.NodeNameNotEditable, node.NodeType.ToString()));
            }
        }
        public override string Name { get { return _cmd.Name; } }

        public override void Do()
        {
            _cmd.Do();
        }
        public override void Undo()
        {
            _cmd.Undo();
        }
        public override void Redo()
        {
            _cmd.Redo();
        }
        public override bool IsNoop
        {
            get { return false; }
        }
    }


    /// <summary>
    /// Change the name of an attribute.
    /// </summary>
    public class EditAttributeName : Command
    {
        private XmlAttribute _a;
        private XmlAttribute _na;
        private XmlElement _p;
        private XmlTreeNode _node;
        private XmlName _name;
        private InsertNode _xmlns; // generated prefix
        private bool _autoGenPrefixes = true;

        public EditAttributeName(XmlAttribute attr, NodeLabelEditEventArgs e)
        {
            this._a = attr;
            this._node = e.Node as XmlTreeNode;
            this._p = this._a.OwnerElement;
            Debug.Assert(this._p != null);
            _name = XmlHelpers.ParseName(this._p, e.Label, XmlNodeType.Attribute);
        }
        public EditAttributeName(XmlTreeNode node, string newName)
        {
            this._a = (XmlAttribute)node.Node;
            this._node = node;
            this._p = this._a.OwnerElement;
            Debug.Assert(this._p != null);
            _name = XmlHelpers.ParseName(this._p, newName, XmlNodeType.Attribute);
        }
        public EditAttributeName(XmlTreeNode node, XmlName newName, bool autoGenPrefixes)
        {
            this._a = (XmlAttribute)node.Node;
            this._node = node;
            _name = newName;
            this._autoGenPrefixes = autoGenPrefixes;
        }

        public override string Name { get { return SR.EditNameCommand; } }

        public override bool IsNoop
        {
            get
            {
                return this._a.LocalName == _name.LocalName && this._a.Prefix == _name.Prefix &&
                    this._a.NamespaceURI == _name.NamespaceUri;
            }
        }
        public override void Do()
        {
            XmlAttribute nsa = null;
            this._p = this._a.OwnerElement; // just in case a prior command changed this!
            if (_autoGenPrefixes && XmlHelpers.MissingNamespace(_name))
            {
                nsa = XmlHelpers.GenerateNamespaceDeclaration(this._p, _name);
            }
            this._na = _a.OwnerDocument.CreateAttribute(_name.Prefix, _name.LocalName, _name.NamespaceUri);
            this._na.Value = this._a.Value; // todo: copy children properly.
            Redo();
            if (nsa != null)
            {
                _xmlns = new InsertNode(_node, InsertPosition.After, nsa, false, false);
                _xmlns.Do();
            }
        }
        public override void Undo()
        {
            if (this._xmlns != null) _xmlns.Undo();
            this._p.Attributes.InsertBefore(this._a, this._na);
            this._p.RemoveAttributeNode(this._na);
            this._node.Label = this._a.Name;
            this._node.Node = this._a;
            this._node.TreeView.SelectedNode = this._node;
        }
        public override void Redo()
        {
            this._p.Attributes.InsertBefore(this._na, this._a);
            this._p.RemoveAttributeNode(this._a);
            this._node.Node = this._na;
            if (this._node.Label != this._na.Name)
            {
                this._node.Label = this._na.Name;
            }
            if (this._xmlns != null) _xmlns.Redo();
            this._node.TreeView.SelectedNode = this._node;
        }

    }

    /// <summary>
    /// Change the name of a processing instruction.
    /// </summary>
    public class EditProcessingInstructionName : Command
    {
        private XmlProcessingInstruction _pi;
        private XmlProcessingInstruction _newpi;
        private XmlNode _p;
        private XmlTreeNode _node;
        private string _name;

        public EditProcessingInstructionName(XmlProcessingInstruction pi, NodeLabelEditEventArgs e)
        {
            this._pi = pi;
            this._p = this._pi.ParentNode;
            this._node = e.Node as XmlTreeNode;
            Debug.Assert(this._p != null);
            _name = e.Label;
            this._newpi = pi.OwnerDocument.CreateProcessingInstruction(_name, pi.Data);
        }
        public EditProcessingInstructionName(XmlTreeNode node, string newName)
        {
            this._pi = (XmlProcessingInstruction)node.Node;
            this._p = this._pi.ParentNode;
            this._node = node;
            Debug.Assert(this._p != null);
            _name = newName;
            this._newpi = _pi.OwnerDocument.CreateProcessingInstruction(_name, _pi.Data);
        }

        public override string Name { get { return SR.EditNameCommand; } }

        public override void Do()
        {
            Swap(this._pi, this._newpi);
        }
        public override void Undo()
        {
            Swap(this._newpi, this._pi);
        }
        public override void Redo()
        {
            Swap(this._pi, this._newpi);
        }
        public override bool IsNoop
        {
            get
            {
                return this._pi.Target == _name;
            }
        }
        public void Swap(XmlProcessingInstruction op, XmlProcessingInstruction np)
        {
            this._p.InsertBefore(np, op);
            this._p.RemoveChild(op);
            this._node.Node = np;
            this._node.Label = np.Target;
            this._node.TreeView.SelectedNode = this._node;
        }

    }

    /// <summary>
    /// </summary>
    public class InsertNode : Command
    {
        private XmlTreeView _view;
        private XmlDocument _doc;

        //XmlTreeNode n;
        private XmlTreeNode _newNode;
        private TreeParent _parent;

        private XmlNode _theNode;
        private int _pos;
        private XmlNodeType _type;
        private bool _requiresName;
        private InsertPosition _position;
        private bool _selectNewNode = true;
        private bool _expandNewNode = true;

        /// <summary>
        /// Insert a new element as a sibling or child of current node. This command can create
        /// new XmlTreeNodes and new XmlNodes to go with it, or it can 
        /// </summary>
        public InsertNode(XmlTreeView view)
        {
            this._view = view;
            this._newNode = view.CreateTreeNode();
            this._doc = view.Model.Document;
            this._position = InsertPosition.Child;
        }

        /// <summary>
        /// Insert an existing XmlNode into the tree and create a corresponding XmlTreeNode for it.
        /// </summary>
        /// <param name="target">Anchor point for insertion</param>
        /// <param name="position">Where to insert the new node relative to target node</param>
        /// <param name="xnode">Provided XmlNode that the new XmlTreeNode will wrap</param>
        /// <param name="selectNewNode">Whether to select the node in the tree after it's inserted.</param>
        public InsertNode(XmlTreeNode target, InsertPosition position, XmlNode xnode, bool selectNewNode, bool expandNewNode)
        {
            this._view = target.XmlTreeView;
            this._doc = this._view.Model.Document;
            this._position = position;
            this._type = xnode.NodeType;
            this._newNode = new XmlTreeNode(this._view, xnode);
            Initialize(_newNode, target, position);
            this._selectNewNode = selectNewNode;
            this._expandNewNode = expandNewNode;
        }

        public override string Name { get { return SR.InsertNodeCommand; } }

        // Returns false if the given insertion is illegal
        public bool Initialize(XmlTreeNode n, InsertPosition position, XmlNodeType type)
        {
            this._position = position;
            this._type = type;
            XmlNode xn = null;
            this._newNode.NodeType = type;
            if (n != null)
            {
                this._parent = new TreeParent(_view, _doc, n);
                xn = n.Node;
            }
            else
            {
                position = InsertPosition.Child; ;
                xn = _view.Model.Document;
                this._parent = new TreeParent(_view.TreeView, _view.Model.Document);
            }
            bool result = CanInsertNode(position, type, xn);
            if (result)
            {
                if (position == InsertPosition.Child)
                {
                    if (xn != null) _parent.SetParent(n);
                    _pos = _parent.AttributeCount;
                    if (type != XmlNodeType.Attribute)
                        _pos += _parent.ChildCount;
                }
                else
                {
                    if (type == XmlNodeType.Attribute ^ xn is XmlAttribute)
                    {
                        _pos = this._parent.AttributeCount;
                        this._position = InsertPosition.Before;
                    }
                    else if (n != null)
                    {
                        _pos = n.Index;
                    }
                }
            }
            return result;
        }

        static bool[][] insertMap = new bool[][] {
        // child -> parent                          None,  Element, Attribute, Text,  CDATA, EntityRef, Entity, PI,    Comment, Doc,   DOCTYPE, Frag,  Notation, WhiteSpace, SigWS, EndElement, EndEntity, XmlDecl                                                
        /* None                     */ new bool[] { false, false,   false,     false, false, false,     false,  false, false,   false, false,   false, false,    false,      false, false,      false,     false },
        /* Element,                 */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   true,  false,   true,  false,    false,      false, false,      false,     false },
        /* Attribute,               */ new bool[] { false, true,    false,     false, false, false,     false,  false, false,   false, false,   false, false,    false,      false, false,      false,     false },
        /* Text,                    */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   false, false,   true,  false,    false,      false, false,      false,     false },
        /* CDATA,                   */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   false, false,   true,  false,    false,      false, false,      false,     false },
        /* EntityReference,         */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   false, false,   true,  false,    false,      false, false,      false,     false },
        /* Entity,                  */ new bool[] { false, false,   false,     false, false, false,     false,  false, false,   false, false,   false, false,    false,      false, false,      false,     false },
        /* ProcessingInstruction,   */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   true,  false,   true,  false,    false,      false, false,      false,     false },
        /* Comment,                 */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   true,  false,   true,  false,    false,      false, false,      false,     false },
        /* Document,                */ new bool[] { false, false,   false,     false, false, false,     false,  false, false,   false, false,   false, false,    false,      false, false,      false,     false },
        /* DocumentType,            */ new bool[] { false, false,   false,     false, false, false,     false,  false, false,   true,  false,   true,  false,    false,      false, false,      false,     false },
        /* DocumentFragment,        */ new bool[] { false, true,    false,     false, false, false,     false,  false, false,   true,  false,   true,  false,    false,      false, false,      false,     false },
        /* Notation,                */ new bool[] { false, false,   false,     false, false, true,      false,  false, false,   false, false,   false, false,    false,      false, false,      false,     false },
        /* Whitespace,              */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   true,  false,   true,  false,    false,      false, false,      false,     false },
        /* SignificantWhitespace,   */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   true,  false,   true,  false,    false,      false, false,      false,     false },
        /* EndElement,              */ new bool[] { false, false,   false,     false, false, false,     false,  false, false,   false, false,   false, false,    false,      false, false,      false,     false },
        /* EndEntity,               */ new bool[] { false, false,   false,     false, false, false,     false,  false, false,   false, false,   false, false,    false,      false, false,      false,     false },
        /* XmlDeclaration           */ new bool[] { false, false,   false,     false, false, false,     false,  false, false,   true,  false,   false, false,    false,      false, false,      false,     false }
        };

        private bool CanInsertNode(InsertPosition position, XmlNodeType type, XmlNode xn)
        {
            if (position == InsertPosition.Before && xn.NodeType == XmlNodeType.XmlDeclaration)
            {
                return false; // cannot insert anything before xml declaration.
            }
            if (position != InsertPosition.Child)
            {
                xn = _parent.ParentNode;
            }
            XmlNodeType parentType = (xn != null) ? xn.NodeType : XmlNodeType.None;
            bool result = insertMap[(int)type][(int)parentType];

            // Check a few extra things...
            switch (type)
            {
                case XmlNodeType.Attribute:
                    this._requiresName = true;
                    break;
                case XmlNodeType.Element:
                    this._requiresName = true;
                    if (position != InsertPosition.Child && _parent.IsRoot && _parent.Document != null && _parent.Document.DocumentElement != null)
                    {
                        result = false; // don't allow multiple root elements.
                    }
                    break;
                case XmlNodeType.ProcessingInstruction:
                    this._requiresName = true;
                    break;
            }
            return result;
        }

        public void Initialize(XmlTreeNode newNode, XmlTreeNode target, InsertPosition position)
        {
            this._newNode = newNode;
            this._position = position;

            if (target == null)
            {
                this._parent = new TreeParent(this._view.TreeView, this._doc);
            }
            else
            {
                this._parent = new TreeParent(this._view, this._doc, target);
                if (position == InsertPosition.Child)
                {
                    if (CanHaveChildren(target))
                    {
                        this._parent.SetParent(target);
                    }
                    else
                    {
                        // if it's not an element it cannot have children!
                        this._position = InsertPosition.After;
                    }
                }
            }
            if (position == InsertPosition.Child)
            {
                if (target == null)
                {
                    // inserting at rool level
                    this._pos = this._view.TreeView.Nodes.Count;
                }
                else
                {
                    if (!CanHaveChildren(target))
                    {
                        this._position = InsertPosition.After;
                    }
                    if (newNode.NodeImage == NodeImage.Attribute)
                    {
                        this._pos = this._parent.AttributeCount;
                    }
                    else if (target != null)
                    {
                        this._pos = target.Children.Count;
                    }
                }
            }
            if (this._position != InsertPosition.Child)
            {
                if (target.Node is XmlAttribute ^ newNode.Node is XmlAttribute)
                {
                    _pos = this._parent.AttributeCount;
                    this._position = InsertPosition.Before;
                }
                else if (target != null)
                {
                    this._pos = target.Index;
                }
            }
        }
        bool CanHaveChildren(XmlTreeNode target)
        {
            return target.NodeType == XmlNodeType.Element ||
                    target.NodeType == XmlNodeType.Document;
        }
        public bool RequiresName
        {
            get { return this._requiresName; }
        }
        public XmlTreeNode NewNode
        {
            get { return this._newNode; }
        }

        public XmlNode CreateNode(XmlNode context, string name)
        {
            XmlNode n = null;
            switch (_type)
            {
                case XmlNodeType.Attribute:
                    {
                        XmlName qname = XmlHelpers.ParseName(context, name, _type);
                        if (qname.Prefix == null)
                        {
                            n = _doc.CreateAttribute(qname.LocalName);
                        }
                        else
                        {
                            n = _doc.CreateAttribute(qname.Prefix, qname.LocalName, qname.NamespaceUri);
                        }
                    }
                    break;
                case XmlNodeType.CDATA:
                    n = _doc.CreateCDataSection("");
                    break;
                case XmlNodeType.Comment:
                    n = _doc.CreateComment("");
                    break;
                case XmlNodeType.DocumentType:
                    XmlConvert.VerifyName(name);
                    n = _doc.CreateDocumentType(name, null, null, null);
                    break;
                case XmlNodeType.Element:
                    {
                        XmlName qname = XmlHelpers.ParseName(context, name, _type);
                        n = _doc.CreateElement(qname.Prefix, qname.LocalName, qname.NamespaceUri);
                        break;
                    }
                case XmlNodeType.ProcessingInstruction:
                    XmlConvert.VerifyName(name);
                    if (name == "xml")
                    {
                        n = _doc.CreateXmlDeclaration("1.0", null, null);
                    }
                    else
                    {
                        n = _doc.CreateProcessingInstruction(name, "");
                    }
                    break;
                case XmlNodeType.Text:
                    n = _doc.CreateTextNode("");
                    break;
                default:
                    throw new ApplicationException(string.Format(SR.UnexpectedNodeType, _type.ToString()));
            }
            return n;
        }
        public XmlNode CreateDocumentElement(string namespaceUri, string name)
        {
            XmlNode n = null;
            n = _doc.CreateElement(name, namespaceUri);
            return n;
        }
        public XmlNode XmlNode
        {
            get { return _newNode.Node; }
            set
            {
                _parent.Insert(this._pos, this._position, value);
                _newNode.Node = this._theNode = value;
                _view.TreeView.OnSelectionChanged();
            }
        }
        public override bool IsNoop
        {
            get
            {
                return false;
            }
        }
        public override void Do()
        {
            Debug.Assert(_parent != null);
            this._view.BeginUpdate();
            try
            {
                _parent.Insert(this._pos, this._position, _newNode, this._selectNewNode);
                if (this.RequiresName)
                {
                    Debug.Assert(_newNode != null);
                    if (this._theNode != null && _newNode.Node == null)
                    {
                        this.XmlNode = this._theNode;
                    }
                    Debug.Assert(_view != null);
                    if (_selectNewNode)
                    {
                        this._view.SelectedNode = _newNode;
                        _newNode.XmlTreeView.ScrollIntoView(_newNode);
                    }
                }
                else if (_newNode.Node == null)
                {
                    this.XmlNode = CreateNode(null, null);
                    this._view.OnNodeInserted(_newNode);
                }
                if (_expandNewNode)
                {
                    _newNode.Expand();
                }
            }
            finally
            {
                this._view.EndUpdate();
            }
        }
        public override void Undo()
        {
            this._view.BeginUpdate();
            try
            {
                if (_newNode.IsEditing)
                {
                    _newNode.EndEdit(true);
                }
                TreeParent np = new TreeParent(this._view, this._doc, _newNode);
                np.Remove(_newNode);
            }
            finally
            {
                this._view.EndUpdate();
            }
        }
        public override void Redo()
        {
            Do();
        }

    }

    public class ChangeNode : Command
    {
        private XmlDocument _doc;
        private XmlNodeType _oldnt = XmlNodeType.Text;
        private XmlNodeType _nt;
        private XmlTreeView _view;
        private XmlTreeNode _node;
        private XmlNode _newNode;
        private CompoundCommand _group;
        private XmlTreeNode _newTreeNode;

        public ChangeNode(XmlTreeView view, XmlTreeNode node, XmlNodeType nt)
        {
            this._doc = view.Model.Document;
            this._nt = nt;
            this._view = view;
            this._node = node;
            XmlNode n = node.Node;
            if (n == null) return;

            init:
            this._oldnt = n.NodeType;
            string innerXml = (_oldnt == XmlNodeType.Element) ? n.InnerXml : SpecialUnescape(_oldnt, n.Value);
            string outerXml = n.OuterXml;
            string qname = n.Name;
            string localName = n.LocalName;
            string ns = n.NamespaceURI;
            bool noName = false;
            if (qname.StartsWith("#"))
            {
                qname = localName = "";
            }
            noName = string.IsNullOrEmpty(qname);

            if (noName && IsNamedNodeType(nt))
            {
                // Try parsing the content of the node as markup! (but first check for special unescaping
                // that we do for nested comment/cdata blocks)                
                PasteCommand paste = new PasteCommand(_doc, view, InsertPosition.Before, new TreeData(innerXml));
                XmlTreeNode nte = paste.NewNode;
                if (nte != null && IsNamedNodeType(nte.NodeType))
                {
                    // then it worked - we extracted a node with a name, so start over.
                    n = _newNode = nte.Node;
                    goto init;
                }
            }
            if (_newNode == null || _newNode.NodeType != nt)
            {
                switch (nt)
                {
                    case XmlNodeType.Element:
                        if (noName)
                        {
                            qname = "element";
                        }
                        _newNode = _doc.CreateElement(qname, ns);
                        _newNode.InnerXml = innerXml;
                        break;
                    case XmlNodeType.Attribute:
                        if (noName)
                        {
                            qname = "attribute";
                        }
                        _newNode = _doc.CreateAttribute(qname, ns);
                        _newNode.Value = innerXml;
                        break;
                    case XmlNodeType.Comment:
                        _newNode = _doc.CreateComment(SpecialEscape(nt, noName ? innerXml : outerXml));
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        if (noName)
                        {
                            localName = "pi";
                        }
                        _newNode = _doc.CreateProcessingInstruction(localName, innerXml);
                        break;
                    case XmlNodeType.Text:
                        _newNode = _doc.CreateTextNode(noName ? innerXml : outerXml);
                        break;
                    case XmlNodeType.CDATA:
                        _newNode = _doc.CreateCDataSection(SpecialEscape(nt, noName ? innerXml : outerXml));
                        break;
                }
            }
            InsertNode icmd = new InsertNode(node, InsertPosition.Before, _newNode, true, true);
            _newTreeNode = icmd.NewNode;
            DeleteNode del = new DeleteNode(_doc, node);
            _group = new CompoundCommand(this.Name);
            _group.Add(icmd);
            _group.Add(del);
        }

        static string SpecialEscape(XmlNodeType nt, string value)
        {
            if (nt == XmlNodeType.Comment)
            {
                // Comments cannot contain nested "--", so we do special escaping here.
                return EscapeSequence(value, "<!--", "/*", "-->", "*/");
            }
            else if (nt == XmlNodeType.CDATA)
            {
                // CDATA blocks cannot nest inside CDATA blocks, so we do special escaping for that also,
                return EscapeSequence(value, "<![CDATA[", "/[", "]]>", "]/");
            }
            return value;
        }

        static string SpecialUnescape(XmlNodeType nt, string value)
        {
            if (nt == XmlNodeType.Comment)
            {
                return UnescapeSequence(value, "<!--", "/*", "-->", "*/");
            }
            else if (nt == XmlNodeType.CDATA)
            {
                return UnescapeSequence(value, "<![CDATA[", "/[", "]]>", "]/");
            }
            return value;
        }

        static string EscapeSequence(string value, string start, string open, string end, string close)
        {
            value = value.Replace(start, open);
            value = value.Replace(end, close);
            return value;
        }

        static string UnescapeSequence(string value, string start, string open, string end, string close)
        {
            Debug.Assert(open.Length == 2 && close.Length == 2);
            char a = open[0];
            char b = open[1];
            char x = close[0];
            char y = close[1];
            StringBuilder sb = new StringBuilder();
            int depth = 0;
            for (int i = 0, n = value.Length; i < n; i++)
            {
                char c = value[i];
                if (c == a && i + 1 < n && value[i + 1] == b)
                {
                    if (depth == 0)
                    {
                        sb.Append(start);
                    }
                    else
                    {
                        sb.Append(a);
                        sb.Append(b);
                    }
                    depth++;
                    i++;
                }
                else if (c == x && i + 1 < n && value[i + 1] == y)
                {
                    depth--;
                    if (depth == 0)
                    {
                        sb.Append(end);
                    }
                    else
                    {
                        sb.Append(x);
                        sb.Append(y);
                    }
                    i++;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        bool IsNamedNodeType(XmlNodeType nt)
        {
            switch (nt)
            {
                case XmlNodeType.Attribute:
                case XmlNodeType.Element:
                case XmlNodeType.ProcessingInstruction:
                    return true;
            }
            return false;
        }

        public XmlTreeNode NewNode
        {
            get
            {
                return _newTreeNode;
            }
        }

        public override bool IsNoop
        {
            get { return false; }
        }
        public override string Name
        {
            get { return string.Format(SR.ChangeNodeCommand, _nt.ToString()); }
        }
        public override void Do()
        {
            this._view.BeginUpdate();
            try
            {
                _group.Do();
            }
            finally
            {
                this._view.EndUpdate();
            }
        }
        public override void Redo()
        {
            this._view.BeginUpdate();
            try
            {
                _group.Redo();
            }
            finally
            {
                this._view.EndUpdate();
            }

        }
        public override void Undo()
        {
            this._view.BeginUpdate();
            try
            {
                _group.Undo();
            }
            finally
            {
                this._view.EndUpdate();
            }
        }
    }

    public class EditElementName : Command
    {
        private XmlElement _xe;
        private XmlElement _ne;
        private XmlNode _p;
        private XmlTreeNode _node;
        private XmlName _name;
        private InsertNode _xmlns; // generated prefix
        private bool _autoGenPrefixes = true;

        public EditElementName(XmlElement n, NodeLabelEditEventArgs e)
        {
            this._xe = n;
            this._node = e.Node as XmlTreeNode;
            this._name = XmlHelpers.ParseName(n, e.Label, n.NodeType);
        }
        public EditElementName(XmlTreeNode node, string newName)
        {
            this._xe = (XmlElement)node.Node; ;
            this._node = node;
            this._name = XmlHelpers.ParseName(this._xe, newName, node.NodeType);
        }
        public EditElementName(XmlTreeNode node, XmlName newName, bool autoGenPrefixes)
        {
            this._xe = (XmlElement)node.Node; ;
            this._node = node;
            this._name = newName;
            this._autoGenPrefixes = autoGenPrefixes;
        }
        public override string Name { get { return SR.EditNameCommand; } }

        public override bool IsNoop
        {
            get
            {
                return this._xe.LocalName == _name.LocalName && this._xe.Prefix == _name.Prefix &&
                    this._xe.NamespaceURI == _name.NamespaceUri;
            }
        }
        public override void Do()
        {
            this._p = _xe.ParentNode; // in case a prior command changed this!
            XmlAttribute a = null;
            if (_autoGenPrefixes && XmlHelpers.MissingNamespace(_name))
            {
                a = XmlHelpers.GenerateNamespaceDeclaration(_xe, _name);
            }
            this._ne = _xe.OwnerDocument.CreateElement(_name.Prefix, _name.LocalName, _name.NamespaceUri);
            Redo();
            if (a != null)
            {
                _xmlns = new InsertNode(_node, InsertPosition.Child, a, false, false);
                _xmlns.Do();
            }
        }

        public override void Undo()
        {
            _node.XmlTreeView.BeginUpdate();
            try
            {
                if (this._xmlns != null) _xmlns.Undo();
                Move(_ne, _xe);
                this._p.ReplaceChild(_xe, _ne);
                _node.Node = _xe;
                _node.TreeView.SelectedNode = _node;
            }
            finally
            {
                _node.XmlTreeView.EndUpdate();
            }
        }

        public override void Redo()
        {
            _node.XmlTreeView.BeginUpdate();
            try
            {
                // Since you cannot rename an element using the DOM, create new element 
                // and copy all children over.
                Move(_xe, _ne);
                this._p.ReplaceChild(_ne, _xe);
                _node.Node = _ne;
                if (this._xmlns != null) _xmlns.Redo();
                _node.TreeView.SelectedNode = _node;
            }
            finally
            {
                _node.XmlTreeView.EndUpdate();
            }
        }

        static void Move(XmlElement from, XmlElement to)
        {
            ArrayList move = new ArrayList();
            foreach (XmlAttribute a in from.Attributes)
            {
                if (a.Specified)
                {
                    move.Add(a);
                }
            }
            foreach (XmlAttribute a in move)
            {
                from.Attributes.Remove(a);
                to.Attributes.Append(a);
            }
            while (from.HasChildNodes)
            {
                to.AppendChild(from.FirstChild);
            }
        }

    }

    /// <summary>
    /// Change the value of a node.
    /// </summary>
    public class EditNodeValue : Command
    {
        private XmlTreeNode _n;
        private XmlNode _xn;
        private string _newValue;
        private string _oldValue;
        private XmlTreeView _view;

        public EditNodeValue(XmlTreeView view, XmlTreeNode n, string newValue)
        {
            this._view = view;
            this._n = n;
            this._xn = n.Node;
            this._newValue = newValue;

            if (_xn is XmlElement)
            {
                this._oldValue = _xn.InnerText;
            }
            else if (_xn is XmlProcessingInstruction)
            {
                XmlProcessingInstruction pi = ((XmlProcessingInstruction)_xn);
                this._oldValue = pi.Data;
            }
            else if (_xn != null)
            {
                this._oldValue = _xn.Value;
            }
        }
        public override string Name { get { return SR.EditValueCommand; } }

        public override bool IsNoop
        {
            get
            {
                return this._oldValue == this._newValue;
            }
        }

        void SetValue(string value)
        {
            this._view.BeginUpdate();
            try
            {
                if (_xn is XmlElement)
                {
                    _xn.InnerText = value;
                    _n.RemoveChildren();
                    if (!string.IsNullOrEmpty(value))
                    {
                        // Add text node child.
                        XmlTreeNode text = _view.CreateTreeNode();
                        text.Node = _xn.FirstChild;
                        _n.Children.Add(text);
                    }
                }
                else if (_xn is XmlProcessingInstruction)
                {
                    XmlProcessingInstruction pi = ((XmlProcessingInstruction)_xn);
                    pi.Data = value;
                }
                else if (_xn != null)
                {
                    _xn.Value = value;
                }
                if (_view != null)
                {
                    _view.SelectedNode = _n;
                    _view.ScrollIntoView(_n);
                }
            }
            finally
            {
                this._view.EndUpdate();
            }
        }
        public override void Do()
        {
            SetValue(_newValue);
        }

        public override void Undo()
        {
            SetValue(_oldValue);
        }
        public override void Redo()
        {
            SetValue(_newValue);
        }

    }

    public class DeleteNode : Command
    {
        private XmlDocument _doc;
        private XmlTreeNode _e;
        private TreeParent _parent;
        private int _pos;
        private XmlTreeView _view;

        public DeleteNode(XmlDocument doc, XmlTreeNode e)
        {
            this._e = e;
            this._doc = doc;
            this._view = e.XmlTreeView;
            Debug.Assert(this._view != null);
        }
        public override bool IsNoop
        {
            get { return false; }
        }
        public override string Name { get { return SR.DeleteCommand; } }

        public override void Do()
        {
            if (this._parent == null)
            {
                this._pos = _e.Index;
                this._parent = new TreeParent(_e.TreeView, _doc, _e);
            }
            _parent.Remove(_e);
        }

        public override void Undo()
        {
            _view.BeginUpdate();
            _parent.Insert(this._pos, InsertPosition.Before, this._e, true);
            _view.EndUpdate();
        }

        public override void Redo()
        {
            Do();
        }

    }

    public class MoveNode : Command
    {
        private XmlTreeNode _source;
        private XmlTreeNode _target;
        private TreeParent _tp;
        private InsertPosition _where;
        private TreeParent _sourceParent;
        private int _sourcePosition;
        private bool _copy;
        private bool _bound;
        private bool _wasExpanded;
        private XmlTreeView _view;

        /// <summary>
        /// Move or copy a node from one place to another place in the tree.
        /// </summary>
        /// <param name="view">The MyTreeView that we are inserting into</param>
        /// <param name="source">The node that we are moving.  This node may not be in the tree
        /// and that is ok, so it might be a node that is being cut&paste from another process
        /// for example</param>
        /// <param name="target">The existing node that establishes where in the tree we want
        /// to move the source node to</param>
        /// <param name="where">The position relative to the target node (before or after)</param>
        /// <param name="copy">Whether we are moving or copying the source node</param>
        public MoveNode(XmlTreeView view, XmlTreeNode source, XmlTreeNode target, InsertPosition where, bool copy)
        {
            XmlNode sn = source.Node;
            XmlNode dn = target.Node;

            this._copy = copy;
            TreeView tv = view.TreeView;
            XmlDocument doc = view.Model.Document;
            this._view = view;
            this._sourcePosition = source.Index;

            view.Model.BeginUpdate();
            try
            {
                if (copy)
                {
                    this._wasExpanded = source.IsExpanded;
                    XmlTreeNode newSource = view.CreateTreeNode();
                    if (sn != null)
                    {
                        if (sn.NodeType == XmlNodeType.Attribute)
                        {
                            string name = GetUniqueAttributeName((XmlAttribute)sn);
                            XmlAttribute na = doc.CreateAttribute(name);
                            na.Value = sn.Value;
                            sn = na;
                        }
                        else
                        {
                            sn = sn.CloneNode(true);
                        }
                        newSource.Node = sn;
                    }
                    source = newSource;
                }

                this._sourceParent = new TreeParent(tv, doc, source);
                this._tp = new TreeParent(tv, doc, target);

                // normalize destination based on source node type.
                // for example, if source is an attribute, then it can only be
                // inserted amongst attributes of another node.
                if (_tp.IsRoot && where != InsertPosition.Child)
                {
                    if (sn is XmlAttribute)
                        throw new Exception(SR.RootLevelAttributes);
                    if (sn is XmlText || sn is XmlCDataSection)
                        throw new Exception(SR.RootLevelText);
                    if (sn is XmlElement && sn.OwnerDocument.DocumentElement != null && sn.OwnerDocument.DocumentElement != sn)
                        throw new Exception(SR.RootLevelElements);
                    if (dn is XmlDeclaration && where == InsertPosition.Before)
                        throw new Exception(SR.RootLevelBeforeXmlDecl);
                }
                if (where != InsertPosition.Child)
                {
                    if (sn is XmlAttribute)
                    {
                        if (!(dn is XmlAttribute))
                        {
                            if (_tp.AttributeCount != 0)
                            {
                                // move target to valid location for attributes.
                                target = _tp.GetChild(_tp.AttributeCount - 1);
                                where = InsertPosition.After;
                            }
                            else
                            {
                                // append the attribute.
                                where = InsertPosition.Child;
                                target = (XmlTreeNode)target.Parent;
                            }

                        }
                    }
                    else if (dn is XmlAttribute)
                    {
                        if (!(sn is XmlAttribute))
                        {
                            int skip = _tp.AttributeCount;
                            if (_tp.Count > skip)
                            {
                                // Move non-attribute down to beginning of child elements.
                                target = _tp.GetChild(skip);
                                where = InsertPosition.Before;
                            }
                            else
                            {
                                // append the node.
                                where = InsertPosition.Child;
                                target = (XmlTreeNode)target.Parent;
                            }
                        }
                    }
                }
                this._source = source;
                this._target = target;
                this._where = where;
                this._tp = new TreeParent(tv, doc, target);

                if (where == InsertPosition.Child)
                {
                    this._tp.SetParent(target);
                }
            }
            finally
            {
                view.Model.EndUpdate();
            }
        }

        private string GetUniqueAttributeName(XmlAttribute sn)
        {
            XmlElement parent = sn.OwnerElement;
            HashSet<string> existing = new HashSet<string>();
            foreach (XmlAttribute a in parent.Attributes)
            {
                existing.Add(a.Name);
            }
            string name = sn.Name;
            ulong n = 0;
            string newName = null;

            if (name.Length == 1)
            {
                int ch = Convert.ToInt32(name[0]);
                while ((ch >= Convert.ToInt32('a') && ch < Convert.ToInt32('z')) || (ch >= Convert.ToInt32('A') && ch < Convert.ToInt32('Z')))
                {
                    newName = Convert.ToChar(ch + 1).ToString();
                    if (!existing.Contains(newName))
                    {
                        return newName;
                    }
                    ch++;
                }
            }
            else if (Char.IsDigit(name[name.Length - 1]))
            {
                int i = name.Length - 1;
                while (Char.IsDigit(name[i]))
                {
                    i--;
                }
                string baseName = name.Substring(0, i + 1);
                // pick up where we left off...
                ulong.TryParse(name.Substring(i + 1), out n);
                name = baseName;
            }

            // last chance, just keep adding digits...
            do
            {
                n++;
                newName = name + n;
            }
            while (existing.Contains(newName));
            return newName;
        }

        public override string Name { get { return SR.MoveCommand; } }

        public XmlTreeNode Source { get { return this._source; } }

        public override bool IsNoop
        {
            get { return this._source == this._target; }
        }

        public override void Do()
        {
            XmlNode sn = _source.Node;
            //XmlNode dn = target.Node;

            this._view.BeginUpdate();
            try
            {
                XmlTreeNode sp = (XmlTreeNode)_source.Parent;

                int sindex = this._sourcePosition;
                TreeView tv = _source.TreeView;
                if (tv != null)
                {
                    this._sourceParent.Remove(_source);
                }
                int index = _target.Index;

                if (_where == InsertPosition.Child)
                {
                    if (sn is XmlAttribute)
                    {
                        index = this._tp.AttributeCount;
                    }
                    else
                    {
                        index = this._tp.AttributeCount + this._tp.ChildCount;
                    }
                }
                try
                {
                    this._tp.Insert(index, _where, _source, true);
                }
                catch (Exception)
                {
                    if (sp != null)
                    {
                        sp.Children.Insert(sindex, _source);
                    }
                    else if (tv != null)
                    {
                        tv.Nodes.Insert(sindex, _source);
                    }
                    if (tv != null)
                    {
                        _source.TreeView.SelectedNode = _source;
                    }
                    throw;
                }

                if (this._copy && !_bound)
                {
                    _bound = true;
                    this._view.Invalidate(); // Bind(source.Nodes, (XmlNode)source.Tag);
                }
                if (this._wasExpanded)
                {
                    _source.Expand();
                }

                _source.TreeView.SelectedNode = _source;
            }
            finally
            {
                _view.EndUpdate();
            }
        }

        public override void Undo()
        {
            this._view.BeginUpdate();
            try
            {
                // Cannot use this.sourceParent because this points to the old source position
                // not the current position.
                TreeParent parent = new TreeParent(this._sourceParent.View, this._sourceParent.Document, this._source);
                parent.Remove(this._source);

                // If the node was not in the tree, then undo just removes it, it does not
                // have to re-insert back in a previous position, because it was a new node
                // (probably inserted via drag/drop).
                if (this._sourceParent.IsNodeInTree)
                {
                    this._sourceParent.Insert(this._sourcePosition, InsertPosition.Before, _source, true);
                    if (_source.Parent != null && _source.Parent.Children.Count == 1)
                    {
                        _source.Parent.Expand();
                    }
                    _source.TreeView.SelectedNode = _source;
                }
                else
                {
                    this._target.TreeView.SelectedNode = _target;
                }
            }
            finally
            {
                _view.EndUpdate();
            }
        }

        public override void Redo()
        {
            Do();
        }

    }

    public enum NudgeDirection { Up, Down, Left, Right }

    public class NudgeNode : Command
    {
        private XmlTreeNode _node;
        private NudgeDirection _dir;
        private MoveNode _mover;
        private XmlTreeView _view;

        public NudgeNode(XmlTreeView view, XmlTreeNode node, NudgeDirection dir)
        {
            this._node = node;
            this._dir = dir;
            this._view = view;
        }

        public override string Name { get { return SR.NudgeCommand; } }

        public override bool IsNoop
        {
            get
            {
                MoveNode cmd = GetMover();
                return (cmd == null || cmd.IsNoop);
            }
        }
        public bool IsEnabled
        {
            get
            {
                switch (_dir)
                {
                    case NudgeDirection.Up:
                        return CanNudgeUp;
                    case NudgeDirection.Down:
                        return CanNudgeDown;
                    case NudgeDirection.Left:
                        return CanNudgeLeft;
                    case NudgeDirection.Right:
                        return CanNudgeRight;
                }
                return false;
            }
        }

        public override void Do()
        {
            MoveNode cmd = GetMover();
            if (cmd != null)
                cmd.Do();
        }

        public override void Undo()
        {
            MoveNode cmd = GetMover();
            if (cmd != null)
                cmd.Undo();
        }

        public override void Redo()
        {
            Do();
        }

        MoveNode GetMover()
        {
            if (this._mover == null)
            {
                switch (_dir)
                {
                    case NudgeDirection.Up:
                        this._mover = GetNudgeUp();
                        break;
                    case NudgeDirection.Down:
                        this._mover = GetNudgeDown();
                        break;
                    case NudgeDirection.Left:
                        this._mover = GetNudgeLeft();
                        break;
                    case NudgeDirection.Right:
                        this._mover = GetNudgeRight();
                        break;
                }
            }
            return this._mover;
        }

        public bool CanNudgeUp
        {
            get
            {
                if (_node != null)
                {
                    XmlTreeNode prev = (XmlTreeNode)_node.PrevNode;
                    if (prev != null)
                    {
                        if (prev.Node is XmlAttribute && !(_node.Node is XmlAttribute))
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public MoveNode GetNudgeUp()
        {
            if (_node != null)
            {
                XmlTreeNode prev = (XmlTreeNode)_node.PrevNode;
                if (prev != null)
                {
                    if (prev.Node is XmlAttribute && !(_node.Node is XmlAttribute))
                    {
                        prev = (XmlTreeNode)_node.Parent;
                    }
                    return new MoveNode(this._view, _node, prev, InsertPosition.Before, false);
                }
            }
            return null;
        }

        public bool CanNudgeDown
        {
            get
            {
                if (_node != null)
                {
                    XmlTreeNode next = (XmlTreeNode)_node.NextSiblingNode;
                    if (next != null)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public MoveNode GetNudgeDown()
        {
            if (_node != null)
            {
                XmlTreeNode next = (XmlTreeNode)_node.NextSiblingNode;
                if (next != null)
                {
                    if (_node.Parent != next.Parent)
                    {
                        return new MoveNode(this._view, _node, next, InsertPosition.Before, false);
                    }
                    else
                    {
                        return new MoveNode(this._view, _node, next, InsertPosition.After, false);
                    }
                }
            }
            return null;
        }
        public bool CanNudgeLeft
        {
            get
            {
                if (_node != null)
                {
                    XmlTreeNode xn = (XmlTreeNode)_node;
                    XmlTreeNode parent = (XmlTreeNode)_node.Parent;
                    if (parent != null)
                    {
                        if (xn.Node is XmlAttribute)
                        {
                            XmlAttribute a = (XmlAttribute)xn.Node;
                            if (IsDocumentElement(parent))
                            {
                                // cannot move attributes outside of document element
                                return false;
                            }
                            if (parent.Node != null && parent.Node.ParentNode != null)
                            {
                                XmlAttributeCollection ac = parent.Node.ParentNode.Attributes;
                                if (ac != null && ac.GetNamedItem(a.Name, a.NamespaceURI) != null)
                                {
                                    // parent already has this attribute!
                                    return false;
                                }
                            }
                        }
                        return true;
                    }
                }
                return false;
            }
        }

        static bool IsDocumentElement(XmlTreeNode node)
        {
            return node.Node != null && node.Node == node.Node.OwnerDocument.DocumentElement;
        }

        public MoveNode GetNudgeLeft()
        {
            if (_node != null)
            {
                XmlTreeNode parent = (XmlTreeNode)_node.Parent;
                if (parent != null)
                {
                    if (_node.Index == 0 && _node.Index != parent.Children.Count - 1)
                    {
                        return new MoveNode(this._view, _node, parent, InsertPosition.Before, false);
                    }
                    else
                    {
                        return new MoveNode(this._view, _node, parent, InsertPosition.After, false);
                    }
                }
            }
            return null;
        }
        public bool CanNudgeRight
        {
            get
            {
                if (_node != null)
                {
                    XmlTreeNode prev = (XmlTreeNode)_node.PrevNode;
                    if (prev != null && prev != _node.Parent)
                    {
                        if (prev.Node is XmlElement)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        public MoveNode GetNudgeRight()
        {
            if (_node != null)
            {
                XmlTreeNode prev = (XmlTreeNode)_node.PrevNode;
                if (prev != null && prev != _node.Parent)
                {
                    if (prev.Node is XmlElement)
                    {
                        return new MoveNode(this._view, _node, prev, InsertPosition.Child, false);
                    }
                }
            }
            return null;
        }

    }

    /// <summary>
    /// The TreeData class encapsulates the process of copying the XmlTreeNode to the
    /// clipboard and back. This is a custom IDataObject that supports the custom
    /// TreeData format, and string.
    /// </summary>
    [Serializable]
    public class TreeData : IDataObject
    {
        private int _img;
        private string _xml;
        private int _nodeType;
        private MemoryStream _stm;

        public TreeData(MemoryStream stm)
        {
            this._stm = stm;
            this._img = -1;
        }

        public TreeData(string xml)
        {
            this._xml = xml;
            this._img = -1;
        }

        public TreeData(XmlTreeNode node)
        {
            _img = node.ImageIndex;
            XmlNode x = node.Node;
            if (x != null)
            {
                _nodeType = (int)x.NodeType;
                this._xml = x.OuterXml;
            }
        }

        public static void SetData(XmlTreeNode node)
        {
            if (node.Node != null)
            {
                Clipboard.SetDataObject(new TreeData(node));
            }
        }

        public static bool HasData
        {
            get
            {
                try
                {
                    IDataObject data = Clipboard.GetDataObject();
                    if (data.GetDataPresent(typeof(string)))
                        return true;

                    foreach (string format in data.GetFormats())
                    {
                        if (format == typeof(TreeData).FullName)
                            return true;
                        if (format.ToUpper().StartsWith("XML"))
                            return true;
                    }
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                }
                catch (System.Threading.ThreadStateException)
                {
                }
                return false;
            }
        }

        public static TreeData GetData()
        {
            try
            {
                IDataObject data = Clipboard.GetDataObject();
                try
                {
                    if (data.GetDataPresent(typeof(TreeData)))
                    {
                        return data.GetData(typeof(TreeData)) as TreeData;
                    }
                    foreach (string format in data.GetFormats())
                    {
                        if (format.ToUpper().StartsWith("XML"))
                        {
                            MemoryStream raw = data.GetData(format) as MemoryStream;
                            return new TreeData(raw);
                        }
                    }
                }
                catch (Exception)
                {
                }
                if (data.GetDataPresent(typeof(string)))
                {
                    string xml = data.GetData(typeof(string)).ToString();
                    // We don't want to actually parse the XML at this point, 
                    // because we don't have the namespace context yet.
                    // So we just sniff the XML to determine the node type.
                    return new TreeData(xml);
                }
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
            }
            catch (System.Threading.ThreadStateException)
            {
            }
            return null;
        }

        public XmlTreeNode GetTreeNode(XmlDocument owner, XmlTreeNode target, XmlTreeView view)
        {
            view.BeginUpdate();
            XmlTreeNode node = null;
            try
            {
                node = view.CreateTreeNode();

                if (this._img == -1 && this._xml != null)
                {
                    Regex regex = new Regex(@"[:_.\w]+\s*=\s*(""[^""]*"")|('[^']*')\s*");
                    Match m = regex.Match(_xml);
                    string trimmed = _xml.Trim();
                    if (m.Success && m.Index == 0 && m.Length == _xml.Length)
                    {
                        _nodeType = (int)XmlNodeType.Attribute;
                        _img = (int)NodeImage.Attribute - 1;
                    }
                    else if (trimmed.StartsWith("<?"))
                    {
                        _nodeType = (int)XmlNodeType.ProcessingInstruction;
                        _img = (int)NodeImage.PI - 1;
                    }
                    else if (trimmed.StartsWith("<!--"))
                    {
                        _nodeType = (int)XmlNodeType.Comment;
                        _img = (int)NodeImage.Comment - 1;
                    }
                    else if (trimmed.StartsWith("<![CDATA["))
                    {
                        _nodeType = (int)XmlNodeType.CDATA;
                        _img = (int)NodeImage.CData - 1;
                    }
                    else if (trimmed.StartsWith("<"))
                    {
                        _nodeType = (int)XmlNodeType.Element;
                        _img = (int)NodeImage.Element - 1;
                    }
                    else
                    {
                        _nodeType = (int)XmlNodeType.Text;
                        _img = (int)NodeImage.Text - 1;
                    }
                }

                XmlNode xn = null;
                XmlNode context = (target != null) ? target.Node : owner;

                if (this._nodeType == (int)XmlNodeType.Attribute)
                {
                    int i = this._xml.IndexOf('=');
                    if (i > 0)
                    {
                        string name = this._xml.Substring(0, i).Trim();
                        XmlName qname = XmlHelpers.ParseName(context, name, XmlNodeType.Attribute);
                        xn = owner.CreateAttribute(qname.Prefix, qname.LocalName, qname.NamespaceUri);
                        string s = this._xml.Substring(i + 1).Trim();
                        if (s.Length > 2)
                        {
                            char quote = s[0];
                            s = s.Substring(1, s.Length - 2); // strip off quotes
                                                              // un-escape quotes in the value.
                            xn.Value = s.Replace(quote == '\'' ? "&apos;" : "&quot;", quote.ToString());
                        }
                    }

                }
                else
                {
                    XmlNamespaceManager nsmgr = XmlHelpers.GetNamespaceScope(context);
                    XmlParserContext pcontext = new XmlParserContext(owner.NameTable, nsmgr, null, XmlSpace.None);
                    XmlTextReader r = null;
                    if (this._xml != null)
                    {
                        r = new XmlTextReader(this._xml, XmlNodeType.Element, pcontext);
                    }
                    else
                    {
                        r = new XmlTextReader(this._stm, XmlNodeType.Element, pcontext);
                    }
                    using (r)
                    {
                        r.WhitespaceHandling = WhitespaceHandling.Significant;

                        // TODO: add multi-select support, so we can insert multiple nodes also.
                        // And find valid nodes (for example, don't attempt to insert Xml declaration
                        // if target node is not at the top of the document, etc).
                        // For now we just favor XML elements over other node types.
                        ArrayList list = new ArrayList();
                        while (true)
                        {
                            XmlNode rn = owner.ReadNode(r);
                            if (rn == null)
                                break;
                            if (rn is XmlElement)
                            {
                                xn = rn;
                                NormalizeNamespaces((XmlElement)rn, nsmgr);
                            }
                            list.Add(rn);
                        }
                        if (xn == null && list.Count > 0)
                            xn = list[0] as XmlNode;
                    }
                }
                node.Node = xn;

                if (!(xn is XmlAttribute))
                {
                    view.Invalidate();
                    if (xn is XmlElement)
                    {
                        if (node.Children.Count <= 1)
                        {
                            this._img = ((int)NodeImage.Leaf - 1);
                        }
                    }
                }
            }
            finally
            {
                view.EndUpdate();
            }
            return node;
        }

        static void NormalizeNamespaces(XmlElement node, XmlNamespaceManager mgr)
        {
            if (node.HasAttributes)
            {
                ArrayList toRemove = null;
                foreach (XmlAttribute a in node.Attributes)
                {
                    string prefix = null;
                    string nsuri = null;
                    if (a.Prefix == "xmlns")
                    {
                        prefix = a.LocalName;
                        nsuri = a.Value;
                    }
                    else if (a.LocalName == "xmlns")
                    {
                        prefix = a.Prefix;
                        nsuri = a.Value;
                    }
                    if (prefix != null && mgr.LookupNamespace(prefix) == nsuri)
                    {
                        if (toRemove == null) toRemove = new ArrayList();
                        toRemove.Add(a);
                    }
                }
                if (toRemove != null)
                {
                    foreach (XmlAttribute a in toRemove)
                    {
                        node.Attributes.Remove(a);
                    }
                }
            }
        }

        #region IDataObject Members

        public object GetData(Type format)
        {
            if (format == typeof(string))
            {
                return this._xml;
            }
            else if (format == typeof(TreeData))
            {
                return this;
            }
            return null;
        }

        public object GetData(string format)
        {
            if (format == DataFormats.Text || format == DataFormats.UnicodeText ||
                format == DataFormats.GetFormat("XML").Name)
            {
                return this._xml;
            }
            else if (format == DataFormats.GetFormat(typeof(TreeData).FullName).Name)
            {
                return this;
            }
            return null;
        }

        public object GetData(string format, bool autoConvert)
        {
            return GetData(format);
        }

        public bool GetDataPresent(Type format)
        {
            return (format == typeof(string) ||
                format == typeof(XmlTreeNode));
        }

        public bool GetDataPresent(string format)
        {
            return (format == DataFormats.Text || format == DataFormats.UnicodeText ||
                format == DataFormats.GetFormat("XML").Name ||
                format == DataFormats.GetFormat(typeof(TreeData).FullName).Name);
        }

        public bool GetDataPresent(string format, bool autoConvert)
        {
            return GetDataPresent(format);
        }

        public string[] GetFormats()
        {
            return new string[] {
                 DataFormats.Text,
                 DataFormats.UnicodeText,
                 DataFormats.GetFormat("XML").Name,
                 DataFormats.GetFormat(typeof(TreeData).FullName).Name
             };
        }

        public string[] GetFormats(bool autoConvert)
        {
            return GetFormats();
        }

        public void SetData(object data)
        {
            throw new NotImplementedException();
        }

        public void SetData(Type format, object data)
        {
            throw new NotImplementedException();
        }

        public void SetData(string format, object data)
        {
            throw new NotImplementedException();
        }

        public void SetData(string format, bool autoConvert, object data)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class CutCommand : Command
    {
        XmlTreeNode node;
        TreeParent parent;
        int index;
        XmlTreeView view;

        public CutCommand(XmlTreeView view, XmlTreeNode node)
        {
            this.node = node;
            this.parent = new TreeParent(view, view.Model.Document, node);
            index = node.Index;
            this.view = view;
        }

        public override bool IsNoop
        {
            get
            {
                return false;
            }
        }

        public override string Name { get { return SR.CutCommand; } }

        public override void Do()
        {
            TreeData.SetData(node);
            this.parent.Remove(node);
        }

        public override void Undo()
        {
            this.view.BeginUpdate();
            parent.Insert(this.index, InsertPosition.Before, node, true);
            this.view.EndUpdate();
        }

        public override void Redo()
        {
            Do();
        }

    }

    public class PasteCommand : Command
    {
        XmlTreeNode target;
        //int index;
        XmlDocument doc;
        XmlTreeNode source;
        Command cmd;
        XmlTreeView view;
        InsertPosition position;
        TreeData td;

        public PasteCommand(XmlDocument doc, XmlTreeView view, InsertPosition position, TreeData data)
        {
            this.td = data;
            this.doc = doc;
            this.target = (XmlTreeNode)view.SelectedNode;
            if (this.target == null && view.TreeView.Nodes.Count > 0)
            {
                this.target = (XmlTreeNode)view.TreeView.Nodes[0];
            }
            if (this.target != null && this.target.NodeType != XmlNodeType.Element &&
                this.target.NodeType != XmlNodeType.Document)
            {
                position = InsertPosition.After;
            }
            this.position = position;
            this.view = view;
            if (td != null)
            {
                this.source = td.GetTreeNode(this.doc, this.target, this.view);
            }
        }

        public XmlTreeNode NewNode { get { return this.source; } }

        public override bool IsNoop
        {
            get
            {
                return false;
            }
        }
        public override string Name { get { return SR.PasteCommand; } }

        public override void Do()
        {

            if (td != null)
            {
                InsertNode icmd = new InsertNode(this.view);
                icmd.Initialize(source, this.target, position);
                this.cmd = icmd;
                this.cmd.Do();
                if (this.source.Children.Count > 1)
                {
                    this.source.Expand();
                }
            }
        }

        public override void Undo()
        {
            if (this.cmd != null)
            {
                this.cmd.Undo();
            }
        }

        public override void Redo()
        {
            if (this.cmd != null)
            {
                this.cmd.Do();
            }
        }

    }

}