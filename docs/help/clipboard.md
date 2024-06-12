## Clipboard

Cut/Copy/Paste and [Drag/drop](dragdrop.md) functionalities in XML Notepad are based on the same XML clipboard format. For instance, dragging a node out of XML Notepad into any text editor that supports a text clipboard format is equivalent to performing a Cut & Paste operation.

The clipboard format consists of the XML serialization of the selected node, along with any necessary namespace declarations to ensure the fragment is well-formed.

When XML is pasted from the clipboard or dropped onto the [tree view](overview.md), the namespace declarations are matched with those already present in the target document and normalized accordingly. This prevents your document from accumulating redundant namespace declarations as a side effect of these editing operations.
