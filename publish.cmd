@echo off
cd %~dp0

for /f "usebackq" %%i in (`xsl -e -s src\Version\version.xsl src\Version\version.props`) do (
    set VERSION=%%i
)

echo ### Publishing version %VERSION%...
where sed2 > nul 2>&1
if ERRORLEVEL 1 echo goto :nosed
if not EXIST publish goto :nobits
if not EXIST src\XmlNotepadSetup\bin\Release\XmlNotepadSetup.msi goto :nomsi
if EXIST src\XmlNotepadSetup\bin\Release\XmlNotepadSetup.zip del src\XmlNotepadSetup\bin\Release\XmlNotepadSetup.zip
if "%LOVETTSOFTWARE_STORAGE_CONNECTION_STRING%" == "" goto :nokey

goto :winget

copy /y src\Updates\Updates.xml publish\
if ERRORLEVEL 1 goto :eof
copy /y src\Updates\Updates.xslt publish\
if ERRORLEVEL 1 goto :eof
copy /y src\Updates\Updates.xsd publish\
if ERRORLEVEL 1 goto :eof
copy /y src\Updates\Updates.xml src\XmlNotepadSetup\bin\Release\
if ERRORLEVEL 1 goto :eof
copy /y src\Updates\Updates.xslt src\XmlNotepadSetup\bin\Release\
if ERRORLEVEL 1 goto :eof
copy /y src\Updates\Updates.xsd src\XmlNotepadSetup\bin\Release\
if ERRORLEVEL 1 goto :eof
if exist publish\XmlNotepadSetup.zip del publish\XmlNotepadSetup.zip
pwsh -command "Compress-Archive -Path src\XmlNotepadSetup\bin\Release\* -DestinationPath publish\XmlNotepadSetup.zip"

if not EXIST src\XmlNotepadPackage\AppPackages\%VERSION%\XmlNotepadPackage_%VERSION%_Test\XmlNotepadPackage_%VERSION%_AnyCPU.msixbundle goto :noappx
if not EXIST publish_appx mkdir publish_appx
xcopy /y /s src\XmlNotepadPackage\AppPackages\%VERSION%\ publish_appx\%VERSION%\
if ERRORLEVEL 1 goto :eof
copy /y src\XmlNotepadPackage\AppPackages\%VERSION%\index.html publish_appx
if ERRORLEVEL 1 goto :eof

echo Uploading ClickOnce installer to XmlNotepad
AzurePublishClickOnce %~dp0publish downloads/XmlNotepad "%LOVETTSOFTWARE_STORAGE_CONNECTION_STRING%"
if ERRORLEVEL 1 goto :eof

echo Uploading MSIX installer to XmlNotepad.Net
AzurePublishClickOnce %~dp0publish_appx downloads/XmlNotepad.Net "%LOVETTSOFTWARE_STORAGE_CONNECTION_STRING%"
if ERRORLEVEL 1 goto :eof

:winget
echo Preparing winget package
mkdir d:\git\lovettchris\winget-pkgs\manifests\m\Microsoft\XMLNotepad\%VERSION%
for /f "usebackq tokens=1,2 delims=: " %%i in (`winget hash -m src\XmlNotepadPackage\AppPackages\%VERSION%\XmlNotepadPackage_%VERSION%_Test\XmlNotepadPackage_%VERSION%_AnyCPU.msixbundle`) do (
    set %%i=%%j
)

set SEDFILE=%TEMP%\patterns.txt
echo s/$(VERSION)/%VERSION%/g > %SEDFILE%
echo s/$(Sha256)/%Sha256%/g >> %SEDFILE%
echo s/$(SignatureSha256)/%SignatureSha256%/g >> %SEDFILE%
sed -f %SEDFILE% tools\Microsoft.XMLNotepad.installer.yaml > ..\winget-pkgs\manifests\m\Microsoft\XMLNotepad\%VERSION%\Microsoft.XMLNotepad.installer.yaml
sed -f %SEDFILE% tools\Microsoft.XMLNotepad.locale.en-US.yaml > ..\winget-pkgs\manifests\m\Microsoft\XMLNotepad\%VERSION%\Microsoft.XMLNotepad.locale.en-US.yaml
sed -f %SEDFILE% tools\Microsoft.XMLNotepad.yaml > ..\winget-pkgs\manifests\m\Microsoft\XMLNotepad\%VERSION%\Microsoft.XMLNotepad.yaml

pushd d:\git\lovettchris\winget-pkgs\manifests\m\Microsoft\XMLNotepad\%VERSION%\
winget validate .
winget install .
if ERRORLEVEL 1 goto :installfailed
echo ===========================================================================
echo Please create pull request for new winget package.
goto :eof

:nobits
echo 'publish' folder not found, please run Solution/Publish first.
exit /b 1

:nomsi
echo 'XmlNotepadSetup\bin\Release\XmlNotepadSetup.msi' not found, please use src\XmlNotepadSetup.sln to build the msi.
exit /b 1

:nokey
echo Please set your LOVETTSOFTWARE_STORAGE_CONNECTION_STRING
exit /b 1

:noappx
echo Please build the .msixbundle using src\XmlNotepadSetup.sln XmlNotepadPackage project, publish/create appx packages.
exit /b 1

:nosed
echo Missing sed.exe tool, please add c:\Program Files\Git\usr\bin to your PATH
exit /b 1

:installfailed
echo winget install failed
exit /b 1