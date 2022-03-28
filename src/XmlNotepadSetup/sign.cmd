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
set signtool=signtool.exe
set dll=%~dp0bin\Release\XmlNotepadSetup.msi
echo %signtool% sign /v /debug /i "COMODO RSA Code Signing CA"  /t http://timestamp.sectigo.com /fd sha256 %dll%
%signtool% sign /v /debug /i "COMODO RSA Code Signing CA"  /t http://timestamp.sectigo.com /fd sha256 %dll%
