
## Updates

The Options Dialog can be used to configure how auto-update works in XML Notepad. The Options Dialog points to an [Updates.xml](http://www.lovettsoftware.com/downloads/xmlnotepad/Updates.xml) file that contains the following kind of information:

```xml
<application>
    <title>Microsoft XML Notepad</title>
    <location>https://lovettsoftwarestorage.blob.core.windows.net/downloads/XmlNotepad/Updates.xml</location>
    <download>https://microsoft.github.io/XmlNotepad/#install/</download>
    <frequency>1.00:00:00</frequency>
  </application>
  <version number="2.8.0.9">
    <bug>Fix locked file bug after doing xml comparison (GitHub issue# 44).</bug>
    <bug>Fix BOM option so it is honored on XSLT output files also (GitHub issue# 46).</bug>
  </version>
  ...
```

This section contains the following information:

- **title** - the title of the application being updated.
- **location** - this field allows the remote administrator to move where XML Notepad is looking for updates.  When XML Notepad discovers a new value here it automatically updates the setting displayed in the options dialog.
- **download** - the location of the page where the user can download the new installer.
- **frequency** - how often to ping the updates.xml page for updates.  The default is every 20 days.  The minimum value supported here is every 5 seconds - but this is not recommended.

Following this is a list of `<version number= "...">` tags describing the features and bugs fixed in each version of the application.  XML Notepad will compare this with the version of the current assembly that is running to see if a newer version is available.

If it finds a newer version, XML Notepad displays a button on the right hand side of the main menu bar telling the user that a new version is available. When the user clicks on that button it opens the web browser at the download page so the user can then install the new version.

An example "Updates.xml" document is included with the XML Notepad installation containing the version information matching the version of XML Notepad that is currently installed.
