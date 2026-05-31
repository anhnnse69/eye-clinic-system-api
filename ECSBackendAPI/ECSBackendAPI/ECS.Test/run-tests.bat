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

:: Find the coverage file (inside a subfolder)
set COVERAGE_FILE=
for /r "%RESULTS_DIR%" %%f in (coverage.cobertura.xml) do (
    if exist "%%f" set "COVERAGE_FILE=%%f"
)
if not defined COVERAGE_FILE (
    for /r "%RESULTS_DIR%" %%f in (coverage.xml) do (
        if exist "%%f" set "COVERAGE_FILE=%%f"
    )
)

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
:: ============================================================
echo [4/5] Generating coverage report...
where reportgenerator >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo       Installing ReportGenerator...
    dotnet tool install -g dotnet-reportgenerator-globaltool
)

if defined COVERAGE_FILE (
    reportgenerator ^
        "-reports:!COVERAGE_FILE!" ^
        "-targetdir:%COVERAGE_REPORT_DIR%" ^
        "-reporttypes:Html_Dark;Badges" ^
        "-title:ECS Backend Code Coverage" ^
        "-verbosity:Warning" ^
        "-assemblyfilters:+ECS.*" ^
        "-classfilters:+ECS.*"
    echo       Coverage report generated.
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