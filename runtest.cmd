@echo off
set TEST_DLL=.\src\UnitTests\bin\Debug\UnitTests.dll

vstest.console "%TEST_DLL%"
