@echo off
setlocal EnableDelayedExpansion

:: CONFIGURATION
set STARTUP_PROJECT=ECS.API
set MIGRATION_PROJECT=ECS.Infrastructure
set DB_CONTEXT=AppDbContext
set MIGRATIONS_OUTPUT_DIR=Persistence\Migrations

:MENU
cls
echo.
echo ========================================================
echo             ECS MIGRATION MANAGER - ENHANCED            
echo ========================================================
echo Startup Project   : %STARTUP_PROJECT%                 
echo Migration Project : %MIGRATION_PROJECT%               
echo DbContext         : %DB_CONTEXT%                      
echo Migrations Folder : %MIGRATION_PROJECT%\%MIGRATIONS_OUTPUT_DIR% 
echo ========================================================
echo [1] Update to Latest - Update DB to the latest migration
echo [2] Add Migration    - Scaffold a new migration
echo [3] Remove Last      - Remove the most recent migration
echo [4] Revert           - Revert to a specific migration
echo [5] List             - List all existing migrations
echo [6] Generate SQL     - Create SQL script from migrations
echo [7] Hard Delete DB   - Drop the entire database
echo [8] Reset DB         - Drop and re-apply all migrations
echo [9] Clean and Init   - Fresh start (delete and new init)
echo [10] Quick Refresh   - Drop DB, Add New Migration, Update DB
echo [0] Exit
echo ========================================================
echo.
set /p choice=Select option (0-10): 

if "%choice%"=="1" goto UPDATE
if "%choice%"=="2" goto ADD_MIGRATION
if "%choice%"=="3" goto REMOVE_LAST
if "%choice%"=="4" goto REVERT
if "%choice%"=="5" goto LIST
if "%choice%"=="6" goto GENERATE_SQL
if "%choice%"=="7" goto HARD_DELETE
if "%choice%"=="8" goto RESET
if "%choice%"=="9" goto CLEAN_INIT
if "%choice%"=="10" goto QUICK_REFRESH
if "%choice%"=="0" goto EXIT

echo Invalid option. Please try again.
timeout /t 2 >nul
goto MENU

:UPDATE
echo.
echo --- UPDATE DATABASE TO LATEST MIGRATION ---
dotnet ef database update --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT%
pause
goto MENU

:ADD_MIGRATION
echo.
echo --- ADD NEW MIGRATION ---
set /p migName=Migration name: 
if "%migName%"=="" goto ADD_MIGRATION
dotnet ef migrations add "%migName%" --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT% --output-dir %MIGRATIONS_OUTPUT_DIR%
pause
goto MENU

:REMOVE_LAST
echo.
echo --- REMOVE LAST MIGRATION ---
dotnet ef migrations remove --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT%
pause
goto MENU

:REVERT
echo.
echo --- REVERT TO MIGRATION ---
set /p target=Target migration name: 
if "%target%"=="" goto MENU
dotnet ef database update "%target%" --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT%
pause
goto MENU

:LIST
echo.
echo --- LIST MIGRATIONS ---
dotnet ef migrations list --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT%
pause
goto MENU

:GENERATE_SQL
echo.
echo --- GENERATE SQL SCRIPT ---
set /p scriptName=Output filename: 
if "%scriptName%"=="" set scriptName=migrate.sql
dotnet ef migrations script --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT% --output "%scriptName%"
pause
goto MENU

:HARD_DELETE
echo.
echo --- HARD DELETE DATABASE ---
dotnet ef database drop --force --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT%
pause
goto MENU

:RESET
echo.
echo --- RESET DATABASE ---
dotnet ef database drop --force --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT%
dotnet ef database update --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT%
pause
goto MENU

:CLEAN_INIT
echo.
echo --- CLEAN AND INIT DATABASE ---
dotnet ef database drop --force --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT%
set MIG_FULL_PATH=%MIGRATION_PROJECT%\%MIGRATIONS_OUTPUT_DIR%
if exist "%MIG_FULL_PATH%" rmdir /s /q "%MIG_FULL_PATH%"
dotnet ef migrations add InitialCreate --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT% --output-dir %MIGRATIONS_OUTPUT_DIR%
dotnet ef database update --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT%
pause
goto MENU

:QUICK_REFRESH
echo.
echo --- QUICK REFRESH (DROP - ADD - UPDATE) ---
echo Step 1/3: Dropping old database...
dotnet ef database drop --force --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT%

echo.
echo Step 2/3: Adding new migration...
set /p migName=Enter new migration name: 
if "%migName%"=="" (
    echo Migration name cannot be empty. Aborting.
    pause
    goto MENU
)
dotnet ef migrations add "%migName%" --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT% --output-dir %MIGRATIONS_OUTPUT_DIR%

echo.
echo Step 3/3: Updating database to the latest migration...
dotnet ef database update --project %MIGRATION_PROJECT% --startup-project %STARTUP_PROJECT% --context %DB_CONTEXT%

echo.
echo DONE!
pause
goto MENU

:EXIT
endlocal
exit /b 0