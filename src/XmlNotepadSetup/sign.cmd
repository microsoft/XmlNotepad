REM build release
REM 
REM run d:\tools\signcode.exe
REM sign d:\CodePlex\XmlNotepad\xmlnotepad\XmlNotepadRegistration\bin\Release\XmlNotepadRegistration.exe
REM sign d:\CodePlex\XmlNotepad\xmlnotepad\Application\bin\Release\XmlNotepad.exe
REM
REM build setup (but don't do rebuild because it will blow away the signed XmlNotepadRegistration.exe)
REM sign d:\CodePlex\XmlNotepad\xmlnotepad\XmlNotepadSetup\bin\Release\XmlNotepadSetup.msi
REM 
REM Zip the output files:
REM  cab1.cab
REM  XmlNotepadSetup.msi
REM 
REM and post to ftp://www.lovettsoftware.com/LovettSoftware/Downloads/XmlNotepad/
REM 
set dll=%~dp0bin\Release\XmlNotepadSetup.msi
d:\tools\signcode.exe -i http://xmlnotepad.codeplex.com -t http://timestamp.comodoca.com/authenticode -cn "CN=Chris Lovett"  %dll
