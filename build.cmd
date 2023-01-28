@echo off

for %%i in (Release, Debug) do (     
  msbuild /target:restore src\XmlNotepad.sln /p:Configuration=%%i "/p:Platform=Any CPU"
  if ERRORLEVEL 1 goto :err_restore

  msbuild /target:rebuild src\XmlNotepad.sln /p:Configuration=%%i "/p:Platform=Any CPU"
  if ERRORLEVEL 1 goto :err_build
)

goto :eof
