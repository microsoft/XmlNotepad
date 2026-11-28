# Validate XML

The **Error List** shows issues found while you edit. Select an issue to jump to its node. Use **F4** or **View > Next Error** to step through the list. To save a report, use **View > Export Errors**.

XML Notepad can find the schema from `xsi:schemaLocation` or `xsi:noNamespaceSchemaLocation` in your document. You can also [add a schema manually](schemas.md) under **View > Manage schemas**. Use **View > Validate XML** to run validation on demand.

If the Error List is hidden, enable **Show Error List and Dynamic Help** under **View > Options**.

## DTD entity leakage

DTD processing is off by default. You can turn it on under **View > Options > Validation and navigation** for a document and DTD you trust, then reopen the file. External entities can cause a parser to read local files or make network requests. Recursive entity expansion can also consume large amounts of memory.

Keep DTD processing off for documents from unknown sources. See [Microsoft’s guidance on XML external entities](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca3075).
