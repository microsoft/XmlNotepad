# Manage schemas

Open **View > Manage schemas** to choose which XSDs XML Notepad uses for [validation](validation.md) and [IntelliSense](intellisense.md).

1. Choose **Add files** for local XSDs. To enter an absolute path or an HTTP or HTTPS URL, use the location field and choose **Add location**.
2. Use the switch beside each schema to enable or disable it.
3. Choose **Apply** to save the list, or **Cancel** to discard the changes.

Select an entry to replace its location. Hold **Ctrl** or **Shift** to select multiple rows for copy, cut, or removal. **Paste locations** accepts one location per line, optionally preceded by a namespace. Valid entries are still added if another line is invalid.

You can sort by name or namespace without changing the registration order. **Undo** and **Redo** work on the edits you make in the schema dialog before applying them.

## Start a document from an XSD

Select a schema and choose **Generate XML**. Pick a root element, then choose **Create document**. The generated XML opens in a new unsaved window. Review the generated values before saving.
