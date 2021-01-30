## XmlNotepad Development

You can build and test XML Notepad using [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/). It uses
.NET frameworks, so be sure to install the ".NET desktop development" feature when using the Visual Studio Installer.
XML Notepad targets .NET Framework version 4.7.2 which is the default target for VS 2019.

XML Notepad uses the [WIX Toolset](https://wixtoolset.org/) to build a standalone windows .msi installer.
To build that setup you will need to install the WIX toolset then the [Wix Toolset Visual Studio 2019 Extension](https://marketplace.visualstudio.com/items?itemName=WixToolset.WixToolsetVisualStudio2019Extension).

### Build the code

After cloning the repo:
- Load `src/XmlNotepad.sln` into VS 2019.
- Select Debug or Release and target "Any CPU".
- Run Build Solution.

### Debug the app

- Right click the `Application` project and slelect `Set as Startup Project`.
- Select Debug configuration.
- Press F5 to start debugging.

### Test the app

After building the app select `Run all tests` from the Visual Studio `Test` menu.

- Right click the `UnitTests` project and select `Run tests`.

This is a GUI test, so do not move your mouse or type on your keyboard or let your
screen lock until this test is completed.  Total test run time is about 12 minutes.

### Design

See [XML Notepad Design ](about.md) for information about how this application is built.
