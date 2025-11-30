@echo off
REM Lint-only script - runs build with focus on analyzer warnings
REM This is useful for checking code quality without a full build

REM Set up the Visual Studio environment
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\VsDevCmd.bat" -no_logo

REM Change to the project directory
cd /d "%~dp0"

echo Running code analyzers (StyleCop, NetAnalyzers)...
echo.

REM Run MSBuild with normal verbosity to show all analyzer warnings
REM /t:Build ensures analyzers run
REM /p:RunAnalyzersDuringBuild=true explicitly enables analyzers (they're on by default)
msbuild VatSentinel.sln /t:Build /p:Configuration=Release /verbosity:normal /nologo /p:RunAnalyzersDuringBuild=true

REM Count warnings (basic check - MSBuild exit code will be non-zero if TreatWarningsAsErrors is true)
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ========================================
    echo Analyzer check completed with issues.
    echo Review warnings above and fix as needed.
    echo ========================================
    exit /b %ERRORLEVEL%
) else (
    echo.
    echo ========================================
    echo Analyzer check completed successfully.
    echo ========================================
)

