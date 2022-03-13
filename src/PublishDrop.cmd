set TargetDir=%1
if "%TargetDir%"=="" goto :notarget
PUSHD "%~dp0"

if exist drop rd /s /q drop
mkdir "drop\Help""
mkdir "drop\samples"

echo TargetDir=%TargetDir%
REM remove double quotes
set RawTarget=%TargetDir:"=%

xcopy  /y "Updates\*" "%RawTarget%"
xcopy  /y "Updates\*" "drop"

echo "Copy to drop\help"
xcopy  /s /y "Application\Help" "drop\Help"

echo "Copy to %TargetDir%\Help"
if not exist "%TargetDir%\Help" mkdir "%TargetDir%\Help"
xcopy  /s /y "Application\Help" "%TargetDir%\Help"
xcopy  /y "Application\Samples\*.*" "drop\samples"
xcopy  /y "%RawTarget%*.*" "drop"
goto :eof

:notarget
echo Please provide the TargetDir binary folder.