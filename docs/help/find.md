# Find and Replace

Press **Ctrl+F** to open Find over the tree. Press **Ctrl+H** or expand the arrow on the left to show the replacement field. The arrows next to Find move through matches. **Esc** dismisses suggestions or closes the bar.

Type part of a node name or value and choose a suggestion with the arrow keys and **Enter**. The bar can suggest recent searches and XML names from the document. Use **Replace** for the current match or **Replace all** to change every match in the search.

## Search options

| Option | Use it to |
| --- | --- |
| **Match case** | Distinguish `Name` from `name`. |
| **Match whole word** | Find `item` without matching `items`. |
| **Regular expression** | Match a pattern such as `^INV-[0-9]+$`. |
| **Search in** | Search everything, or limit the search to names, text, or comments. |
| **Replace backwards** | Move backward through the matches after each replacement. |

Use **Ctrl+I** to find names starting with what you type. If you start from a value editor, it matches the beginning of values instead.

## Find nodes with XPath

Enable **XPath** in **Search options**. For example, `//item[@status='draft']` finds `item` elements with a draft status.

**XPath namespaces** lists the prefixes in scope for the selected node. Use one of those prefixes in your query. If the document uses a default namespace with no prefix, use **Edit > Copy XPath** to get an expression for a selected node in that namespace.

Use **Edit > Copy XPath** to start from the path of a node you already selected.
