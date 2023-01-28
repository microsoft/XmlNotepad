@echo off

nuget restore src\XmlNotepad.sln

for %%i in (Release, Debug) do (     

  msbuild /target:rebuild src\XmlNotepad.sln /p:Configuration=%%i "/p:Platform=Any CPU"
  if ERRORLEVEL 1 goto :err_build
)

goto :eof
