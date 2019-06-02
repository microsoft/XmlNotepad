set TargetDir=%1
PUSHD "%~dp0"

if not exist  "drop" mkdir  "drop"
if not exist  "drop\samples" mkdir  "drop\samples"

echo TargetDir=%TargetDir%
REM remove double quotes
set RawTarget=%TargetDir:"=%

xcopy  /y "Updates\*" "%RawTarget%""
xcopy  /y "Updates\*" "drop"
if not exist drop\Help mkdir drop\Help
xcopy  /s /y "Application\Help" "drop\Help"
xcopy  /y "Samples\*.*" "drop\samples"
xcopy  /y "%RawTarget%*.*" "drop"