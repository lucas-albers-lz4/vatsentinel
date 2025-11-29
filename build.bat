@echo off
REM Set up the Visual Studio environment
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\VsDevCmd.bat" -no_logo
::call C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe -noe -c "&{Import-Module """C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\Microsoft.VisualStudio.DevShell.dll"""; Enter-VsDevShell 8b891a43}"

REM Change to the project directory
cd C:\Users\fred\gitroot\vat-timer

REM Run MSBuild
msbuild VatSentinel.sln /t:Build