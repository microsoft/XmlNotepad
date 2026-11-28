# Where settings are stored

XML Notepad saves its window position, recent files, search history, and [options](options.md) in `XmlNotepad.settings`. Use **File > Open XML Settings** to see which file the app is using.

| Location | Folder |
| --- | --- |
| **Portable** | Beside `XmlNotepad.exe`. The folder must be writable. |
| **Local** | `%LOCALAPPDATA%\Microsoft\Xml Notepad\` |
| **Roaming** | `%APPDATA%\Microsoft\Xml Notepad\` |

Change the folder under **View > Options > Settings location**. XML Notepad moves the active settings file. If you already have a file at the destination, move it aside first.

The app checks for a writable portable settings file, then uses your saved location preference. If you have yet to choose a location, it looks for existing local and roaming settings. It also recognizes the local settings file from earlier WinUI builds.

## Set up defaults for a deployment

Put `XmlNotepad.template.settings` next to `XmlNotepad.exe` to supply initial settings for new profiles. Use the template flag to preview it with a separate profile.

```powershell
XmlNotepad.exe -template
```

Preview changes go to `XmlNotepad.template-preview.settings` under `%TEMP%\Microsoft\Xml Notepad\`. Use `XmlNotepad.exe -test` for a separate test profile named `XmlNotepad.test.settings` in the same folder.
