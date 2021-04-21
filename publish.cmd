@echo off
cd %~dp0

for /f "usebackq" %%i in (src\Version\Version.txt) do (
    set VERSION=%%i
)

echo ### Publishing version %VERSION%...

if not EXIST publish goto :nobits
if not EXIST src\XmlNotepadSetup\bin\Release\XmlNotepadSetup.msi goto :nomsi
if EXIST src\XmlNotepadSetup\bin\Release\XmlNotepadSetup.zip del src\XmlNotepadSetup\bin\Release\XmlNotepadSetup.zip
if "%LOVETTSOFTWARE_STORAGE_CONNECTION_STRING%" == "" goto :nokey

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

goto :eof

:nobits
echo 'publish' folder not found, please run Solution/Publish first.
exit /b 1

:nozip
echo 'XmlNotepadSetup\bin\Release\XmlNotepadSetup.msi' not found, please use src\XmlNotepadSetup.sln to build the msi.
exit /b 1

:nokey
echo Please set your LOVETTSOFTWARE_STORAGE_CONNECTION_STRING
exit /b 1

:noappx
echo Please build the .msixbundle using src\XmlNotepadSetup.sln XmlNotepadPackage project, publish/create appx packages.
exit /b 1
