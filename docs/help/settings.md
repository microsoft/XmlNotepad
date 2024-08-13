## Settings

XML Notepad stores your user settings in a file located in this folder, by default:

```cmd
%APPDATA%\Microsoft\Xml Notepad\XmlNotepad.settings
```

The settings include the last size and location of the window, a list of most recently opened files, recent find
strings, and all the things you see in the [Options Dialog](options.md).

You can change where XML Notepad stores the settings file if you go to the [Options Dialog](options.md) and change the
`Settings Location` option. The options are:

| Name         | Location      |
| ------------- |-------------|
| Portable | Stored where ever XmlNotepad.exe was installed |
| Local   | %LOCALAPPDATA%\Microsoft\Xml Notepad\ |
| Roaming | %APPDATA%\Microsoft\Xml Notepad\ |

The `Roaming` folder might be automatically migrated to all your machines because it associated with a [Roaming User
Profile](https://blogs.windows.com/windowsdeveloper/2016/05/03/getting-started-with-roaming-app-data/).

The `Portable` option makes it easy for you to `xcopy` the folder to other machines and get the same settings. This
option will not be available if you are running XML Notepad in a folder that is read only (like `c:\Program Files`).

Changing the `Settings Location` option moves the `XmlNotepad.settings` file accordingly. This means you should not have
multiple `XmlNotepad.settings` files in all these locations, if you do it will search in this priority order:

1. Portable
2. Local
3. Roaming

and it will use the first one that it finds.

## Settings Template

o make it easier to pre-configure XML notepad across a bunch of machines, you can provide a customized
`XmlNotepad.template.settings` template file next to `XmlNotepad.exe` and the first time a user launches XmlNotepad on a
machine it will use this template for the initial default settings for that user, which is then copied to the `Roaming`
location.

See also [XML Notepad File Association](fileassociation.md).