
## Updates

The [Options Dialog](options.md) can be used to configure how auto-update works in XML Notepad. The Options Dialog
points to an [Updates.xml](https://github.com/microsoft/XmlNotepad/blob/master/src/Updates/Updates.xml) file that
contains the following kind of information:

```xml
<application>
    <title>Microsoft XML Notepad</title>
    <location>https://lovettsoftwarestorage.blob.core.windows.net/downloads/XmlNotepad/Updates.xml</location>
    <installer>https://microsoft.github.io/XmlNotepad/#install/</installer>
    <history>https://github.com/microsoft/XmlNotepad/blob/master/src/Updates/Updates.xml/</history>
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
- **location** - this field allows the remote administrator to move where XML Notepad is looking for updates. When XML
  Notepad discovers a new value here it automatically updates the setting displayed in the options dialog.
- **installer** - this field contains the web page location that lists the different XML notepad installers that can be
  used to update the app.
- **history** - the list that describes all the changes in each version.
- **frequency** - how often to ping the updates.xml page for updates. The default is no more than once per day.

You can configure the auto-update mechanism using the following settings in the [Options Dialog](options.md):
- **Enable Updates** - if this is false XML Notepad will not ping to see if there are any updates available and the menu
item `Check for udpates...` under the Help menu will be hidden.
- **Update location** - which endpoint to use to look for updated `updates.xml` files.

You can add the `DisableUpdateUI` setting the `XmlNotepad.settings` file so that users will never see any options
regarding auto-updates or the `Check for updates...` menu item as follows:
```xml
  <DisableUpdateUI>True</DisableUpdateUI>
```

The `updates.xml` file contains a list of `<version number= "...">` tags describing the features and bugs fixed in each
version of the application. XML Notepad will compare this with the version of the current assembly that is running to
see if a newer version is available.

If it finds a newer version, XML Notepad displays a button on the right hand side of the main menu bar telling the user
that a new version is available. When the user clicks on that button it opens the web browser at the download page so
the user can then install the new version.

If you are using the "ClickOnce" installer then the updates will be automatic so the user will not need to do anything.

An `updates.xml` document is included with the XML Notepad installation containing the version information matching the
version of XML Notepad that is currently installed.
