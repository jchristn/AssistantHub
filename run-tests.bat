@echo off
REM Run all AssistantHub test projects sequentially and print a cross-project summary.
REM Exit code: 0 only if every project returned 0.

setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "OVERALL_EXIT=0"
set "FAIL_COUNT=0"
set "R1=SKIP"
set "R2=SKIP"
set "R3=SKIP"
set "R4=SKIP"
set "R5=SKIP"

echo ==============================================================
echo   Running AssistantHub Test Suite
echo ==============================================================
echo.

REM --- Test.Models ---
echo --- Test.Models ---
dotnet run --project "%SCRIPT_DIR%src\Test.Models"
if !ERRORLEVEL! equ 0 ( set "R1=PASS" ) else ( set "R1=FAIL" & set "OVERALL_EXIT=1" & set /a FAIL_COUNT+=1 )
echo.

REM --- Test.Database ---
echo --- Test.Database ---
dotnet run --project "%SCRIPT_DIR%src\Test.Database" -- --type sqlite
if !ERRORLEVEL! equ 0 ( set "R2=PASS" ) else ( set "R2=FAIL" & set "OVERALL_EXIT=1" & set /a FAIL_COUNT+=1 )
echo.

REM --- Test.Services ---
echo --- Test.Services ---
dotnet run --project "%SCRIPT_DIR%src\Test.Services"
if !ERRORLEVEL! equ 0 ( set "R3=PASS" ) else ( set "R3=FAIL" & set "OVERALL_EXIT=1" & set /a FAIL_COUNT+=1 )
echo.

REM --- Test.Api ---
echo --- Test.Api ---
dotnet run --project "%SCRIPT_DIR%src\Test.Api"
if !ERRORLEVEL! equ 0 ( set "R4=PASS" ) else ( set "R4=FAIL" & set "OVERALL_EXIT=1" & set /a FAIL_COUNT+=1 )
echo.

REM --- Test.Integration ---
echo --- Test.Integration ---
dotnet run --project "%SCRIPT_DIR%src\Test.Integration"
if !ERRORLEVEL! equ 0 ( set "R5=PASS" ) else ( set "R5=FAIL" & set "OVERALL_EXIT=1" & set /a FAIL_COUNT+=1 )
echo.

echo ==============================================================
echo   CROSS-PROJECT TEST SUMMARY
echo ==============================================================
echo   !R1!  Test.Models
echo   !R2!  Test.Database
echo   !R3!  Test.Services
echo   !R4!  Test.Api
echo   !R5!  Test.Integration
echo --------------------------------------------------------------
if !OVERALL_EXIT! equ 0 (
    echo   OVERALL: PASS
) else (
    echo   OVERALL: FAIL ^(!FAIL_COUNT! project^(s^) failed^)
)
echo ==============================================================

exit /b !OVERALL_EXIT!
