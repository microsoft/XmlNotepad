## XmlNotepad Development

You can build and test XML Notepad using [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/). It uses
.NET frameworks, so be sure to install the ".NET desktop development" feature when using the Visual Studio Installer.
XML Notepad targets .NET Framework version 4.7.2 which is the default target for VS 2019.

### Build the code

After cloning the repo:

- Load `src/XmlNotepad.sln` into VS 2019.
- Select Debug or Release and target "Any CPU".
- Run Build Solution.

### Debug the app

- Right click the `Application` project and select `Set as Startup Project`.
- Select Debug configuration.
- Press F5 to start debugging.

### Test the app

After building the app:

- Right click the `UnitTests` project and select `Run tests`.

This is a GUI test, so do not move your mouse or type on your keyboard or let your
screen lock until this test is completed.  Total test run time is about 12 minutes.

### BuildTasks

The `BuildTasks` project contains special MSBuild task that is used to synchronize the `Versions.cs` information
across multiple places so you can edit the version number in one place and this is then propagated to:

1. The WIX based setup file `Product.wxs`.
2. The windows package manifest file `Package.appxmanifest`.
3. The updates.xml file.
4. The readme.htm file.

**Note**: if you change the `SyncVersions.cs` code, and build a new DLL you will need to close VS, and copy the
resulting `BuildTasks\bin\Debug\XmlNotepadBuildTasks.dll` to `BuildTasks\XmlNotepadBuildTasks.dll`, then reload the
XmlNotepad.sln. This is done this way because Visual Studio will lock this file after doing a build, so you wouldn't be
able to compile the new version.

### Publish the ClickOnce installer

Open the `Application` project properties and you will see a Publish option there.
This is only something that can be done by the current author.  This setup is also only performed on strongly
signed bits using a certificate belonging to the author.  This certificate is specified using environment variable
`MYKEYFILE`, but you can build, debug and test XML Notepad without this environment variable set.

This setup provides the ClickOnce installed version of XML Notepad installable from [lovettsoftware](https://lovettsoftwarestorage.blob.core.windows.net/downloads/XmlNotepad/XmlNotepad.application).  This is the most convenient installer since it is
a single click and also provide auto-updating whenever a new version is published.

### Build the setup .msi installer

XML Notepad uses the [WIX Toolset](https://wixtoolset.org/) to build a standalone windows .msi installer. To build that
setup you will need to install the WIX toolset then the [Wix Toolset Visual Studio 2019
Extension](https://marketplace.visualstudio.com/items?itemName=WixToolset.WixToolsetVisualStudio2019Extension).

Then right click the `XmlNotepadSetup` project and select "build".  This will produce an .msi installer in the
XmlNotepadSetup\bin\release folder.  There is also a `sign.cmd` script invoked by this build that will try and sign the
resulting .msi using the certificate installed by the author.  This step will only work for the author who owns the
certificate.

This `msi` installer gives folks the option to install XML Notepad on machines that are isolated from the internet and
there are quite a few customers who have requested this, which is why it exists.

### Build the winget setup package

The `winget` setup package is built by the `XmlNotepadPackage` project.  Right click this project in the Solution
Explorer and select `Publish` and `Create App Packages`.  Choose Sideloading, and the package files will be written to
the XmlNotepadPackage\AppPackages folder.  These can then be uploaded to the server hosting these packages and you can
then update the manifest in
[winget-pkgs](https://github.com/microsoft/winget-pkgs/tree/master/manifests/Microsoft/XMLNotepad).  

This package provides the `winget install xmlnotepad` setup option.

### Design

See [XML Notepad Design ](help/design.md) for more detailed information about how this application is designed.
