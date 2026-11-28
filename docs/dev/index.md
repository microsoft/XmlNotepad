# Build and contribute

On Windows, install the .NET 11 SDK and the Windows application development tools for Visual Studio. Open `src/xmlnotepad.slnx`, choose **x64**, and set **XmlNotepad** as the startup project.

Build the solution and run the tests from the repository root.

```powershell
dotnet build src/xmlnotepad.slnx -c Debug -p:Platform=x64 -warnaserror
dotnet test src/UnitTests/UnitTests.csproj
```

For a local run with package identity, use [WinApp CLI](https://github.com/microsoft/WinAppCli).

```powershell
winapp run src/Application/XmlNotepad.csproj --arch x64
```

Use braces for control-flow blocks, explicit access modifiers, and a leading underscore for private fields. Keep new types in their own file unless closely related types read better together. See [Architecture](design.md) for where editor code and tests live.

## Prepare a release

Update `ApplicationVersion` in `src/Version/Version.props` and the identity version in both `Package.appxmanifest` files, under `src/Application` and `src/Packaging/Msix`. Add an entry to `src/Updates/Updates.xml`. Advance one of the first three version fields for every MSI release because Windows Installer uses only those fields for upgrades.

## Build packages

Run `tools/publish.ps1` from the repository root in PowerShell. MSIX builds require Visual Studio MSBuild. The script finds it on `PATH` or through `vswhere`.

| Target | Output under `artifacts` |
| --- | --- |
| `Unsigned` (default) | `msix`. Unsigned MSIX bundle for x86, x64, and ARM64. |
| `WebPreview` | `web-preview`. Unsigned bundle and App Installer feed for checking the web release layout. |
| `Signed` | `web-release`. Signed bundle, dependencies, and App Installer feed. |
| `Portable` | `portable/win-x64`. Self-contained editor and XmlStats. |
| `Msi` | `msi/win-x64`. Unsigned Windows Installer package. |
| `Bundle` | `bundle/win-x64`. Unsigned setup EXE wrapping the MSI. |
| `XmlStats` | `xmlstats/win-x64`. Standalone XmlStats executable. |

For the portable, MSI, setup EXE, and XmlStats targets, choose `x64`, `x86`, or `arm64` with `-Architecture`. This command builds the x64 MSI.

```powershell
.\tools\publish.ps1 -Target Msi -Architecture x64
```

Keep the portable output together when moving it. The portable and MSI builds include the .NET and Windows App SDK runtimes and `XmlStats.exe`. Run `XmlStats.exe -help` for its options. For example, `XmlStats.exe -nologo -s item report.xml` reports statistics for `item` elements and their contents.

The WiX projects restore through NuGet and accept the WiX 7 EULA through `src/Packaging/WixInstaller.props`. The MSI skips ICE03 because Windows Installer validation rejects some language metadata in the Windows App SDK binaries. See the [WiX maintenance fee and EULA guidance](https://docs.firegiant.com/wix/osmf/).

## Sign and publish

For `-Target Signed`, install the release certificate. If you use a replacement certificate with the same publisher identity, set `PackageCertificateThumbprint` before building. Use Windows SDK SignTool to sign the MSI or setup EXE.

```cmd
signtool.exe sign /v /i "Sectigo Public Code Signing CA R36" /tr http://timestamp.sectigo.com /td sha256 /fd sha256 "<artifact-path>"
```

For a web release, upload the bundle and dependencies to the versioned URLs in the generated `.appinstaller` file. Publish the feed last at its stable URL, and serve each file with the appropriate content type. Use the signed bundle for the [WinGet manifest](https://github.com/microsoft/winget-pkgs/tree/master/manifests/m/Microsoft/XMLNotepad). See [Microsoft’s App Installer publishing guide](https://learn.microsoft.com/windows/msix/app-installer/installing-windows10-apps-web).

The MSIX project pulls the ARM64 Visual C++ framework from the pinned WinGet dependencies archive if the installed SDK leaves it out.

## Preview documentation

```powershell
python -m pip install mkdocs mkdocs-macros-plugin
python -m mkdocs serve -f mkdocs-local.yml
```

Open the local address printed by MkDocs. Use `python -m mkdocs build` to check the published configuration.
