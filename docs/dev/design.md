# Architecture

| Folder | What lives there |
| --- | --- |
| `src/Application` | WinUI 3 editor. `Editor` holds the main page, `Features` groups views with their logic, and `Services` holds shared document, editing, and shell code. |
| `src/Core` | Headless XML engine, including schema handling, namespace helpers, XInclude, and undo. |
| `src/UnitTests` | .NET 11 unit tests for the headless library. |
| `src/XmlStats` | Standalone command-line statistics tool. |
| `src/XsltScriptHost` | .NET Framework process for approved local XSLT script blocks. |
| `src/Packaging` | MSIX, MSI, and setup EXE projects. |

## How an edit travels through the app

`MainPage` owns the tree, value editor, search bar, and diagnostics. `MainPageViewModel` coordinates selection and turns an edit into a command. The command changes the XML document, and the view model updates the tree and selection.

`XmlDocumentSession` tracks the document, encoding, opened file, and unsaved changes. `SourceXmlDocument` keeps source positions for commands such as **Go To Line** and for validation diagnostics.

`UndoManager` tracks commands. To make several changes undo together, group them in a `CompoundCommand`. Replace All uses this approach so one Undo reverses the whole operation.

## Where to start a change

- **Search** starts in `Application/Features/FindReplace`. `XmlSearchService` finds matches, and the editor applies replacements through its command history.
- **Schemas and validation** start in `Application/Features/Schemas`. The shared `SchemaCache` loads XSDs. `XmlValidationService` validates a copy of the document and maps issues back to the editable tree.
- **Transformations** start in `Application/Features/Xslt`. The editor runs ordinary XSLT itself and starts `XsltScriptHost` for approved local scripts.
- **Packaging** starts in `tools/publish.ps1`. See [Build packages](index.md#build-packages) for targets and output paths.
