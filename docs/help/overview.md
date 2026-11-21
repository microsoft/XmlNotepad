# Getting started

1. Press **Ctrl+O** to open a file, or use **Help > Open sample** to try the editor with a sample document.
2. Expand an element in the **Node** column. Its text appears alongside it in the **Value** column.
3. Double-click a value or select its row and press **Enter**. Press **Enter** to apply an inline edit or **Esc** to cancel.
4. Press **Ctrl+S** to save. For a new copy, use **File > Save As**.

Long or multiline values open in the editor below the tree. Press **Ctrl+Enter** to apply the change. You can drag the divider between the columns and the dividers above the value editor and bottom pane.

The **Error List** and **Dynamic Help** share the bottom pane. Select a validation issue to jump to its node, or select a node in the tree to read any schema documentation for it. The status bar shows the current XPath and, when available, its line and column.

Use **File > Open from URL** to load an HTTP or HTTPS document. XML Notepad also opens CSV, JSON, and HTML and converts them to XML.

## Open a file from the command line

```powershell
XmlNotepad.exe "C:\work\document.xml"
```

You can pass a file URI or an HTTP or HTTPS URL. Put `--` before a relative filename that starts with a dash.

See [Keyboard shortcuts](keyboard.md), [Menus](menus.md), or [Find and Replace](find.md).
