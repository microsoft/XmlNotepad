# Menu reference

## File

| Command | What it does |
| --- | --- |
| **New** / **New Window** | Start a blank document or open another window for the current document. |
| **Open** / **Open from URL** | Open a local file or an HTTP or HTTPS address. CSV, JSON, and HTML input is converted to XML. |
| **Save** / **Save As** | Save the current document or choose a new name. XML Notepad asks before overwriting a file that changed on disk. |
| **Reload from disk** | Read the file again. Save any edits you want to keep first. |
| **Compare XML files** | Compare the current document with another XML file. Save the report from the results dialog. |
| **Open Comparison Styles** | Edit the CSS used by the next comparison and its saved report. Delete the custom file to restore the default style. |
| **Recent Files** | Reopen a recent document. |
| **Open XML Settings** | Open the active settings file. See [Settings](settings.md). |

## Edit and Insert

Use **Edit** to undo changes, work with the clipboard, rename or duplicate a node, or convert it to a different XML node type. **Copy XPath** copies the selected node’s path. **Go To Line** jumps to a line in the opened file.

**Find and Replace**, **Incremental Search**, and **Find Next** use the [Find bar](find.md). Choose **Edit Multiline Value** when you need room to work on a longer value.

Use **Insert** to add an element, attribute, text node, comment, CDATA section, or processing instruction before, after, or inside the selected node. The choices depend on what is valid at that position.

## View

- **Expand Branch**, **Collapse Branch**, **Expand All**, and **Collapse All** control how much of the tree is visible.
- **Open in text editor** opens the source in the editor configured under [Options](options.md).
- **Go to Definition** follows an ID reference or opens an XInclude or a schema declaration.
- **Validate XML**, **Next Error**, and **Export Errors** work with the [Error List](validation.md).
- **XML Statistics** counts nodes and characters, including unsaved changes. Use **Copy report** to keep the results.
- **Expand XIncludes** replaces XInclude nodes with the referenced content.
- **View XSLT result** and **Transform with XSLT** open a [transformation preview](xslt.md).
- **Manage schemas** opens the [schema list](schemas.md). **Options** controls appearance, validation, and settings storage.

## Help

Use **Documentation** for the help pages or **Open sample** to try a bundled document. **File Associations** opens the Windows defaults workflow. **Check for updates** checks the release feed. **About XML Notepad** shows the running version.
