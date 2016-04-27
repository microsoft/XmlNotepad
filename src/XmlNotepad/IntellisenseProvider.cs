using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Schema;

namespace XmlNotepad {
    public interface IIntellisenseProvider {
        Uri BaseUri { get; }
        TreeNode ContextNode {get;set;}
        void SetContextNode(TreeNode node);
        bool IsNameEditable { get; }
        bool IsValueEditable { get; }
        XmlSchemaType GetSchemaType();
        string GetDefaultValue();
        IIntellisenseList GetExpectedNames();
        IIntellisenseList GetExpectedValues();
        IXmlBuilder Builder { get; }
        IXmlEditor Editor { get; }
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
