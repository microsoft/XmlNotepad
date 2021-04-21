@echo off
cd %~dp0
if not EXIST publish goto :nobits
if not EXIST src\XmlNotepadSetup\bin\Release\XmlNotepadSetup.msi goto :nomsi
if EXIST src\XmlNotepadSetup\bin\Release\XmlNotepadSetup.zip del src\XmlNotepadSetup\bin\Release\XmlNotepadSetup.zip
if "%LOVETTSOFTWARE_STORAGE_CONNECTION_STRING%" == "" goto :nokey

copy /y src\Updates\Updates.xml publish\
copy /y src\Updates\Updates.xslt publish\
copy /y src\Updates\Updates.xsd publish\
copy /y src\Updates\Updates.xml src\XmlNotepadSetup\bin\Release\
copy /y src\Updates\Updates.xslt src\XmlNotepadSetup\bin\Release\
copy /y src\Updates\Updates.xsd src\XmlNotepadSetup\bin\Release\
if exist publish\XmlNotepadSetup.zip del publish\XmlNotepadSetup.zip
pwsh -command "Compress-Archive -Path src\XmlNotepadSetup\bin\Release\* -DestinationPath publish\XmlNotepadSetup.zip"

AzurePublishClickOnce %~dp0publish downloads/XmlNotepad "%LOVETTSOFTWARE_STORAGE_CONNECTION_STRING%"
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
