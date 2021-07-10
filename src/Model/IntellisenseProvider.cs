using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace XmlNotepad {

    public interface IXmlTreeNode
    {
        XmlNodeType NodeType { get; }
        IXmlTreeNode ParentNode { get; }
        IEnumerable<IXmlTreeNode> Nodes { get; }
        XmlNode Node { get; }
    }

    public interface IIntellisenseProvider {
        Uri BaseUri { get; }
        IXmlTreeNode ContextNode {get;set;}
        void SetContextNode(IXmlTreeNode node);
        bool IsNameEditable { get; }
        bool IsValueEditable { get; }
        XmlSchemaType GetSchemaType();
        string GetDefaultValue();
        IIntellisenseList GetExpectedNames();
        IIntellisenseList GetExpectedValues();
        IXmlBuilder Builder { get; }
        IXmlEditor Editor { get; }
        void RegisterBuilder(string name, Type t);
        void RegisterEditor(string name, Type t);
    }

    public interface IIntellisenseList {
        // If open  is true then the user can enter something other than
        // what is in the list of values returned below.
        bool IsOpen { get; }
        
        // Count of items in the list
        int Count { get; }
        // Returns intellisense string at given position.
        string GetValue(int i);
        // Returns tooltip for given item
        string GetTooltip(int i);
    }
}
