
## Menus
XML Notepad provides the following menu commands.

### File
The file menu contains the following commands:

- **New** Start a new XML document.
- **Open** Open an XML document for editing.  Can also open .csv and .htm files converting them to XML.
- **Reload** Discard any edits you've made and reload the file as it exists on disk.
- **Open Xml Settings...** Opens the current XmlNotepad.settings file in Xml Notepad.
- **Open XmlDiff Styles...** Opens the XmlDiffStyles.css file in your text editor so you 
can change the appearence of the XmlDiff output window. Delete the file to reset back to the original styles.  The files lives in %LOCALAPPDATA%\Microsoft\Xml Notepad\XmlDiffStyles.css.
- **Save** Save any edits you've made to the file disk.
- **Save As** Save the current document to a different file name on disk.
- **Export Errors** Save the contents of the Error List in an XML format.
- **Recent Files** This menu provides quick access to the last 10 XML documents you've edited.
- **Exit** Closes XML Notepad.

### Edit
The edit menu contains the following commands:

- **Undo** Undo the previous edit operation.
- **Redo** Reverses the last undo operation.
- **Cut** Copy the selected node to the clipboard (and its children) and remove that node from the tree. See [Clipboard support](clipboard.md).
- **Copy** Copy the selected node to the clipboard (and its children).
- **Copy XPath** Copy the XPath expression that locates the selected node.
- **Paste** Parse the XML in the clipboard and create new nodes in the tree under the selected node.
- **Delete** Delete the selected node.
- **Insert** Insert a new node of the same type (element, attribute, processing instruction, comment, etc) as the currently selected node.  This makes it easy to build a list of nodes that have the same type.
- **Rename** Enter edit mode on the current element or attribute so you can rename it (or type Enter key)
- **Duplicate** Clone the selected node (and its children) and insert the clone as the next sibling.
- **Change To** Changes the selected node to the specified node type.
- **Goto Definition** Open the selected XInclude or open the XML Schema that defines the selected node in a new instance of XML Notepad.
- **Expand XIncludes** Expand all XInlcude elements with the contents of the XML documents they point to.
- **Nudge** Moves the selected node:
    - **Up**     Before the previous sibling, or before the parent if there are no previous siblings.
    - **Down**     After the next sibling, or after the parent if there is no next sibling.
    - **Left**     Before the parent if this is the first child, otherwise after the parent.
    - **Right** so it becomes the last child of the previous sibling.
- **Find...** Brings up the [find dialog](find.md).
- **Replace...** Brings up the [replace dialog](find.md).
- **Incremental Search...** Allows you to instantly find what you type.

### View

- **Expand All** Expand all collapsed nodes in the entire tree. See "Expand" item on the context menu if you just want to expand the selected node.
- **Collapse all** Collapse all expanded nodes in the entire tree (except the root node). See "Collapse" item on the context menu if you just want to collapse the selected node.
- **Status Bar** Toggle the visibility of the status bar.
- **Source** Show the current XML document in text form using "Notepad".
- **Options...** Display the [Options Dialog](options.md).
- **Schemas...** Display the [Schemas Dialog](schemas.md).
- **Statistics...** Opens a command prompt with the results of running XmlStats.exe on the current file.
The XmlStats.exe program is in the current working directory so you can play with it further.  Type `XmlStats -?` for help.
- **Next Error** Navigate to the next error in the list of errors.
- **Compare XML Files...** Launches XML Diff which compares the current document you are editing with another document on disk and displays the results.

### Insert
Contains commands for inserting various node types. You can insert before, after or as a child of the currently selected node. Only those insert operations that result in well formed document will be enabled.

### Window
Provides a "New window" command for launching another instance of XML Notepad.

### Help
Bring up the help contents and index as well as the About Dialog.