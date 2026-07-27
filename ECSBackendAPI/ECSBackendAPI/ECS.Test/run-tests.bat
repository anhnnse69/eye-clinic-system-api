@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

:: ============================================================
:: CONFIGURATION
:: ============================================================
set PROJECT=ECS.Test.csproj
set RESULTS_DIR=TestResults
set COVERAGE_REPORT_DIR=%RESULTS_DIR%\coverage-report

:: ============================================================
:: 1. Clean previous results
:: ============================================================
echo.
echo [ECS] Secure Test Runner & Coverage Reporter
echo =============================================
echo.
echo [1/5] Cleaning previous results...
if exist %RESULTS_DIR% rmdir /s /q %RESULTS_DIR%
mkdir %RESULTS_DIR% 2>nul
mkdir %COVERAGE_REPORT_DIR% 2>nul
echo       Done.
echo.

:: ============================================================
:: 2. Build solution
:: ============================================================
echo [2/5] Building solution...
dotnet build %PROJECT% --configuration Release --verbosity quiet
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Build failed. Aborting.
    pause
    exit /b 1
)
echo       Build successful.
echo.

:: ============================================================
:: 3. Run tests with coverage (using --collect)
:: ============================================================
echo [3/5] Running tests with coverage...
dotnet test %PROJECT% ^
    --configuration Release ^
    --no-build ^
    --logger "trx;LogFileName=test-results.trx" ^
    --results-directory "%RESULTS_DIR%" ^
    --settings coverlet.runsettings ^
    --collect:"XPlat Code Coverage"

set TEST_EXIT=%ERRORLEVEL%

:: Find the coverage file (inside a subfolder).
:: Prefer the GUID-named coverage folder produced by coverlet (e.g.
:: TestResults\<guid>\coverage.cobertura.xml). The dotnet test run also
:: creates a secondary copy at
:: TestResults\<user>_<machine>_<date>\In\<machine>\coverage.cobertura.xml
:: via the VSTest logger; we want to skip that and pick the primary one.
::
:: To distinguish them we look at the parent folder name: the primary file
:: has a parent folder that is exactly 36 chars long and matches the GUID
:: pattern (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx). The secondary one has a
:: parent folder that contains "In" or machine names with non-hex chars.
set COVERAGE_FILE=
for /r "%RESULTS_DIR%" %%f in (coverage.cobertura.xml) do (
    if not defined COVERAGE_FILE (
        set "CAND=%%f"
        set "CAND_DIR=%%~dpf"
        rem Strip trailing backslash from CAND_DIR.
        if "!CAND_DIR:~-1!"=="\" set "CAND_DIR=!CAND_DIR:~0,-1!"
        rem Get the immediate parent folder name.
        for %%d in ("!CAND_DIR!") do set "PARENT_NAME=%%~nxd"
        rem A GUID folder is 36 chars: 8-4-4-4-12. Match exactly.
        if "!PARENT_NAME:~8,1!"=="-" if "!PARENT_NAME:~13,1!"=="-" if "!PARENT_NAME:~18,1!"=="-" if "!PARENT_NAME:~23,1!"=="-" (
            set "COVERAGE_FILE=%%f"
        )
    )
)

:: If no GUID-named file was found, fall back to any cobertura file.
if not defined COVERAGE_FILE (
    for /r "%RESULTS_DIR%" %%f in (coverage.cobertura.xml) do (
        if not defined COVERAGE_FILE set "COVERAGE_FILE=%%f"
    )
)
if not defined COVERAGE_FILE (
    for /r "%RESULTS_DIR%" %%f in (coverage.xml) do (
        if not defined COVERAGE_FILE set "COVERAGE_FILE=%%f"
    )
)

:: Resolve COVERAGE_FILE to an absolute path so that reportgenerator can
:: locate it regardless of the target-dir resolution rules.
for %%f in ("%COVERAGE_FILE%") do set "COVERAGE_FILE=%%~ff"

if defined COVERAGE_FILE (
    echo       Coverage file found.
) else (
    echo [WARN] No coverage file generated. Check coverlet.collector.
)

if %TEST_EXIT% neq 0 (
    echo [WARN] Some tests FAILED
) else (
    echo       All tests PASSED.
)
echo.

:: ============================================================
:: 4. Generate HTML coverage report (modern dark theme)
::    The report is scoped to ONLY the concrete service classes
::    inside ECS.Application\Services. Other classes (controllers,
::    validators, DTOs, helpers, infrastructure) are excluded.
::    The PowerShell helper enumerates the services and invokes
::    reportgenerator directly, avoiding the cmd `for /f` 8 KB
::    buffer truncation that would otherwise happen on the long
::    class-filter string.
:: ============================================================
echo [4/5] Generating service-only coverage report...

where powershell >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo [ERROR] PowerShell is required to build the service class filter.
    pause
    exit /b 1
)

where reportgenerator >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo       Installing ReportGenerator...
    dotnet tool install -g dotnet-reportgenerator-globaltool
)

if defined COVERAGE_FILE (
    rem Pass the ServicesRoot path as a relative path. The PowerShell helper
    rem calls Resolve-Path on it internally, so it does not matter whether
    rem it is relative or absolute. We previously tried to convert to
    rem absolute in batch first, but for /f + pushd had edge cases that
    rem silently produced a wrong ServicesRoot, which caused
    rem "Mapped 0 concrete service classes" and a wide-open report.
    rem Letting PowerShell resolve it is more reliable.
    set APP_SERVICES_DIR=%~dp0..\ECS.Application\Services
    set "ABS_TARGET_DIR=%~dp0%COVERAGE_REPORT_DIR%"
    powershell -NoProfile -ExecutionPolicy Bypass ^
        -File "%~dp0build-service-classfilter.ps1" ^
        -CoverageFile "!COVERAGE_FILE!" ^
        -TargetDir "!ABS_TARGET_DIR!" ^
        -ServicesRoot "!APP_SERVICES_DIR!"
    if !ERRORLEVEL! neq 0 (
        echo [ERROR] Service coverage report generation failed.
        echo         Inspect coverage-report\_debug\classfilter.txt to verify the class names.
        pause
        exit /b 1
    )
) else (
    echo [ERROR] Coverage file missing. Cannot generate report.
)
echo.

:: ============================================================
:: 5. Clean sensitive subfolders (remove GUID and computer-name folders)
:: ============================================================
echo [5/5] Removing leaky folders...
for /d %%d in ("%RESULTS_DIR%\*") do (
    if /i not "%%~nxd"=="coverage-report" (
        echo       Removing: %%d
        rmdir /s /q "%%d" 2>nul
    )
)
echo.

:: ============================================================
:: 6. Open coverage report
:: ============================================================
echo === SUMMARY ===
echo   Coverage report : %COVERAGE_REPORT_DIR%\index.html
echo   Coverage badge  : %COVERAGE_REPORT_DIR%\badge.svg
echo.

if exist "%COVERAGE_REPORT_DIR%\index.html" (
    start "" "%COVERAGE_REPORT_DIR%\index.html"
) else (
    echo [ERROR] Coverage report not found.
)

if %TEST_EXIT% equ 0 (echo   [RESULT] SUCCESS) else (echo   [RESULT] FAILED)
echo.
pause
exit /b %TEST_EXIT%