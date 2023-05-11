@echo off

nuget restore src\XmlNotepad.sln
if ERRORLEVEL 1 goto :eof

msbuild /target:rebuild src\UpdateVersions\UpdateVersions.csproj /p:Configuration=Release
if ERRORLEVEL 1 goto :eof

D:\git\lovettchris\XmlNotepad\src\UpdateVersions\bin\x64\Release\net7.0\UpdateVersions.exe
if ERRORLEVEL 1 goto :eof

for %%i in (Release, Debug) do (     
  msbuild /target:rebuild src\XmlNotepad.sln /p:Configuration=%%i "/p:Platform=Any CPU"
  if ERRORLEVEL 1 goto :eof
)

goto :eof
