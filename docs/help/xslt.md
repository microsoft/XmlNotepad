# Run an XSLT transformation

Use **View > View XSLT result** to run the stylesheet linked by the current document. The bundled Hamlet sample links to `willy.xsl` with this processing instruction.

```xml
<?xml-stylesheet type="text/xsl" href="willy.xsl"?>
```

To choose a different stylesheet, use **View > Transform with XSLT**. Browse for a file, pick a recent stylesheet, or enter a path or an HTTP or HTTPS URL. Relative paths are resolved against the current document.

The preview displays HTML in WebView2 and shows XML or plain text in a read-only editor. Choose **Save output as** to keep the result. If a transformation takes a while, use **Cancel** in the progress dialog.

Add `<?xsl-output default="hamlet"?>` to suggest an output filename. If you leave out the extension, XML Notepad chooses it from the output method. Without a linked stylesheet, the built-in stylesheet can display colorized XML. Change that preference in **View > Options**.

## Stylesheets with scripts

Ordinary XSLT 1.0 runs on .NET 11. A local stylesheet with an embedded `msxsl:script` block needs your approval for that run. The prompt identifies the source file, including a script loaded through `xsl:include` or `xsl:import`.

Approved scripts run in a separate .NET Framework process with your Windows account’s permissions. Its XSLT file references are limited to local drives. Turn off **Allow approval for local XSLT scripts** to disable those runs. See [Microsoft’s script-block documentation](https://learn.microsoft.com/dotnet/standard/data/xml/script-blocks-using-msxsl-script).
