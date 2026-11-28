# Install XML Notepad

## WinGet

Install from PowerShell or Windows Terminal.

```powershell
winget install --exact --id Microsoft.XMLNotepad
```

Use WinGet to update an existing WinGet installation.

```powershell
winget upgrade --exact --id Microsoft.XMLNotepad
```

## Manual and offline installs

Open **Assets** on the [latest GitHub release](https://github.com/microsoft/XmlNotepad/releases/latest). Pick an installer for your PC if the release offers separate versions for x64, x86, or ARM64. **Windows Settings > System > About** shows your system type.

- For an MSIX release, open the `.msixbundle` or `.appinstaller` file in Windows App Installer. An App Installer feed can download prerequisites and check for updates.
- For an offline install, copy the Windows Installer download to the target PC. If it is a ZIP, extract it before running `XmlNotepadSetup.msi`. Releases may also offer a setup EXE.
- If the release includes a portable build, extract the whole folder and run `XmlNotepad.exe`. Keep the files together.

The older Windows Forms releases use .NET Framework 4.8. They are available from the [ClickOnce download](https://lovettsoftwarestorage.blob.core.windows.net/downloads/XmlNotepad/XmlNotepad.application) and the [standalone ZIP](https://lovettsoftwarestorage.blob.core.windows.net/downloads/XmlNotepad/XmlNotepadSetup.zip).

HTML previews of XSLT results use the [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/). The separate host for approved legacy XSLT scripts uses .NET Framework 4.8 or later. See [XSLT transformations](help/xslt.md) for details.

To open XML Notepad from File Explorer by default, [set up file associations](help/fileassociation.md).
