@echo off
setlocal EnableDelayedExpansion

set SrcDir=%~dp0
set TargetDir=%1

if "%TargetDir%"=="" (
    echo Please provide the TargetDir binary folder.
    exit /b 1
)

echo SrcDir=%SrcDir%
set "DropDir=%SrcDir%drop"
set "SamplesDir=%SrcDir%drop\samples"

if exist "!DropDir!" (
    echo Removing existing drop directory...
    rd /s /q "!DropDir!"
)

echo Creating directories...
mkdir "!DropDir!"
mkdir "!DropDir!\runtimes"
mkdir "!SamplesDir!"

echo TargetDir=!TargetDir!
REM remove double quotes
set "RawTarget=!TargetDir:"=!"


set RawTarget=%RawTarget%

echo Copying updates to !RawTarget! and drop directory...
xcopy /y "%SrcDir%Updates\*" "!RawTarget!"
xcopy /y "%SrcDir%Updates\*" "!DropDir!"

echo Copying samples to !SamplesDir!...
xcopy /y "%SrcDir%Application\Samples\*.*" "!SamplesDir!"

echo Copying all files from !RawTarget! to !DropDir!...
xcopy /y "!RawTarget!*.*" "!DropDir!"
xcopy /y /s "!RawTarget!runtimes\*.*" "!DropDir!\runtimes"

echo Done.
