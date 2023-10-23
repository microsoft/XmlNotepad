## XmlNotepad Development

You can build and test XML Notepad using [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/). It uses
.NET frameworks, so be sure to install the ".NET desktop development" feature when using the Visual Studio Installer.
XML Notepad targets .NET Framework version 4.8.

### Coding Guidelines

XmlNotepad follows the standard [C# Coding Guidelines](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) with
the default C# formatting settings that ship with VS 2022.

The following additional conventions are also followed:

1. All if/else statements use curly brackets.
1. Add private/internal/public access explicitly on all members.
1. Do not use "s_" prefix on statics, instead use Class name qualification.
1. All private fields prefixed with `_` except for WinForms generated code.
1. Accessing fields with `this.` is ok, do not strip `this.` prefix if used.
1. Constants are PascalCased even if they are private to a class.
1. Try and collect all private fields at the top of the class.
1. Public fields are PascalCased like public properties and methods.
1. Generally one class per file, unless there is a super natural family of classes, like Commands.cs, that would cause an unnecessary explosion in number of files.
1. Nice to have blank line between methods.
1. Use "Remove and Sort Usings" command in VS.

### Build the code

First clone the repo:
```
git clone https://github.com/microsoft/XmlNotepad.git
```
Then:

- Load `src/XmlNotepad.sln` into Visual Studio.
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

The tests all pass on Windows 10, but currently some tests fail on Windows 11, there seems to be
some breaking changes in the Windows Automation layer that XML notepad tests are using.  This is
being investigated.

### UpdateVersions

The `UpdateVersions` project synchronizes the `Version.props` information
across multiple places so you can edit the version number in `Version.props` and this tool will
replicate that across the following:

1. The `Version.cs` file which sets the assembly version for all projects in the solution.
1. The WIX based setup file `Product.wxs`.
2. The windows package manifest file `Package.appxmanifest`.
3. The updates.xml file.
4. The readme.htm file.

You will also have to restart Visual Studio so that the new versions are picked up by the ClickOnce
deployment information in  `Application.csproj`.

### Publish the app and all setups

The `publish.cmd` script runs UpdateVersions, builds Release bits, builds the ClickOnce installer and the winget.msix
installer from `XmlNotepadPackage`, and the standalone .msi installer from `XmlNotepadSetup`, it zips these files so
they can be available on the Github Release page then it creates the new github release for the current version.  It
also signs the assemblies and installers using the signing certificate specified by the environment variable
`MYKEYFILE`.

The ClickOnce installed is uploaded to
[lovettsoftware](https://lovettsoftwarestorage.blob.core.windows.net/downloads/XmlNotepad/XmlNotepad.application) using
the environment variable LOVETTSOFTWARE_STORAGE_CONNECTION_STRING.  Click once is convenient because it provides
auto-updating whenever a new version is published.

### Build the setup .msi installer

After building the `Release` configuration of `XmlNotepad.sln` load the `XmlNotepadSetup.sln`.  This
solution uses the [WIX Toolset](https://wixtoolset.org/) to build a standalone windows .msi
installer. To build that setup you will need to install the WIX toolset then the [Wix Toolset Visual
Studio 2022
Extension](https://marketplace.visualstudio.com/items?itemName=WixToolset.WixToolsetVisualStudio2022Extension).

Then right click the `XmlNotepadSetup` project and select "build".  This will produce an .msi installer in the
XmlNotepadSetup\bin\release folder.  There is also a `sign.cmd` script invoked by this build that will try and sign the
resulting .msi using the certificate installed by the author.  This step will only work for the author who owns the
certificate.

This `msi` installer gives folks the option to install XML Notepad on machines that are isolated from the internet and
there are quite a few customers who have requested this, which is why it exists.

### Build the winget setup package

The `winget` setup package is built by the `XmlNotepadPackage` project in the `XmlNotepadSetup.sln`
solution.  Right click this project in the Solution Explorer and select `Publish` and `Create App
Packages`.  Choose Sideloading, and the package files will be written to the
XmlNotepadPackage\AppPackages folder.  These can then be uploaded to the server hosting these
packages and you can then update the manifest in
[winget-pkgs](https://github.com/microsoft/winget-pkgs/tree/master/manifests/m/Microsoft/XMLNotepad).

This package provides the `winget install xmlnotepad` setup option.

### Publishing the bits to Azure Blob Store

The `publish.cmd` script uses the [AzurePublishClickOnce](https://github.com/clovett/tools/tree/master/AzurePublishClickOnce) tool
which uses the environment variable named `LOVETTSOFTWARE_STORAGE_CONNECTION_STRING` to access the Azure storage account
where it uploads the new clickonce installer, and removes the older versions so that the storage account does not
keep growing and growing.

### Design

See [XML Notepad Design ](design.md) for more detailed information about how this application is designed.

### Issues

Feedback and suggestions are welcome, just use the [GitHub  issues
list](https://github.com/microsoft/XmlNotepad/issues).  Pull requests are also welcome, in fact, a number of good pull
requests have already been merged.  Thanks to all who are helping to make XML notepad a great tool!
