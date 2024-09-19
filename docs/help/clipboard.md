

## Clipboard

Cut/Copy/Paste and [Drag/drop](dragdrop.md) are based on the same XML clipboard format. For example, you can drag a node
out of XML Notepad and into any editor that supports a text clipboard format and its the same as doing a Cut & Paste
operation.

The clipboard format is the XML serialization of the selected node, along with any necessary any namespace declarations
needed to ensure the fragment well formed.

When XML is pasted from the clipboard (or dropped) onto the [tree view](overview.md), the namespace declarations are
matched those already present in the target document and normalized accordingly. This prevents your document from
accumulating redundant namespace declarations as a side effect of these editing operations.