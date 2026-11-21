# Options

Open **View > Options**. **Apply** saves your changes, **Cancel** closes the dialog, and **Reset options** loads the defaults for you to review and apply.

## Appearance and editing

- Choose **Use system setting**, **Light**, or **Dark**. Set the editor font and tree indentation, or expand **XML colors** to edit either palette.
- Turn on **Show identifying text next to element names** to tell similar rows apart. The editor uses schema IDs, `xml:id`, or the first match from your list of attribute or child names.
- Use **Show Error List and Dynamic Help** to show or hide the bottom pane.
- Under **Editing values**, set the long-line threshold, default wrapping, or the maximum length for new input. Enter `0` to disable a limit. Wrapping affects the display only.

## Opening, validating, and saving files

- Enable **Preserve whitespace when opening XML** if you want whitespace nodes to stay visible. Use **Ask before reloading files changed on disk** to control reload prompts.
- Set **Maximum nodes to index for line navigation** to limit how many source positions the editor records. Lower it for very large files, or set it to `0` to skip indexing. Reopen the file after changing the limit.
- Set **Dynamic Help language** to choose a language for schema annotations. Leave it blank to show all available languages.
- Leave **Process document type declarations (DTDs)** off for unfamiliar files. See [Validate XML](validation.md).
- Under **Saving XML**, choose whether to format on save, how to indent, and which line endings to write.

You can also choose the text editor executable, adjust [update checks](updates.md), or move the [settings file](settings.md). For built-in stylesheet and script approval settings, see [XSLT transformations](xslt.md).
