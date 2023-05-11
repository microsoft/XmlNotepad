@echo off

nuget restore src\XmlNotepad.sln
if ERRORLEVEL 1 goto :eof

msbuild /target:rebuild src\UpdateVersions\UpdateVersions.csproj /p:Configuration=Release "/p:Platform=AnyCPU"
if ERRORLEVEL 1 goto :eof

%~dp0\src\UpdateVersions\bin\Release\UpdateVersions.exe
if ERRORLEVEL 1 goto :eof

for %%i in (Release, Debug) do (     
  msbuild /target:rebuild src\XmlNotepad.sln /p:Configuration=%%i "/p:Platform=Any CPU"
  if ERRORLEVEL 1 goto :eof
)

goto :eof
