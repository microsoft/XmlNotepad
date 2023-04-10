@echo off
setlocal EnableDelayedExpansion

set TargetDir=%1

if "%TargetDir%"=="" (
    echo Please provide the TargetDir binary folder.
    exit /b 1
)

set "DropDir=%~dp0\drop"
set "HelpDir=%~dp0\drop\Help"
set "SamplesDir=%~dp0\drop\samples"

if exist "!DropDir!" (
    echo Removing existing drop directory...
    rd /s /q "!DropDir!"
)

echo Creating directories...
mkdir "!DropDir!"
mkdir "!HelpDir!"

echo TargetDir=!TargetDir!
REM remove double quotes
set "RawTarget=!TargetDir:"=!"

echo Copying updates to !RawTarget! and drop directory...
xcopy /y "Updates\*" "!RawTarget!"
xcopy /y "Updates\*" "!DropDir!"

echo Copying help to !HelpDir! and %TargetDir%\Help...
xcopy /s /y "Application\Help" "!HelpDir!"
if not exist "%TargetDir%\Help" mkdir "%TargetDir%\Help"
xcopy /s /y "Application\Help" "%TargetDir%\Help"

echo Copying samples to !SamplesDir!...
xcopy /y "Application\Samples\*.*" "!SamplesDir!"

echo Copying all files from !RawTarget! to !DropDir!...
xcopy /y "!RawTarget!\*.*" "!DropDir!"

echo Done.
