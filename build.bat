@echo off
REM Set up the Visual Studio environment
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\VsDevCmd.bat" -no_logo
::call C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe -noe -c "&{Import-Module """C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\Microsoft.VisualStudio.DevShell.dll"""; Enter-VsDevShell 8b891a43}"

REM Change to the project directory (use %~dp0 to get the script's directory)
cd /d "%~dp0"

REM Run MSBuild with analyzers enabled
REM /verbosity:minimal - Shows errors and warnings (good for CI/build scripts)
REM /verbosity:normal - Shows more detail (default)
REM Analyzers (StyleCop, NetAnalyzers) run automatically during build
echo Building VatSentinel with code analysis...
msbuild VatSentinel.sln /t:Build /p:Configuration=Release /verbosity:minimal /nologo

REM Check exit code
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed with errors or warnings!
    echo To see detailed analyzer warnings, run with /verbosity:normal
    exit /b %ERRORLEVEL%
)

echo.
echo Build completed successfully.
echo Note: Analyzer warnings may appear above. Review and fix as needed.