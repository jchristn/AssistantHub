@echo off
REM Run AssistantHub test suites and print a summary.
REM Exit code: 0 only if every project returned 0.

setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "OVERALL_EXIT=0"
set "FAIL_COUNT=0"
set "R1=SKIP"
set "R2=SKIP"

echo ==============================================================
echo   AssistantHub Test Runner
echo ==============================================================
echo.

REM --- Test.Automated (console runner) ---
echo --- Test.Automated ---
dotnet run --project "%SCRIPT_DIR%src\Test.Automated"
if !ERRORLEVEL! equ 0 ( set "R1=PASS" ) else ( set "R1=FAIL" & set "OVERALL_EXIT=1" & set /a FAIL_COUNT+=1 )
echo.

REM --- Test.XUnit (xUnit runner) ---
echo --- Test.XUnit ---
dotnet test "%SCRIPT_DIR%src\Test.XUnit" --no-build --verbosity normal
if !ERRORLEVEL! equ 0 ( set "R2=PASS" ) else ( set "R2=FAIL" & set "OVERALL_EXIT=1" & set /a FAIL_COUNT+=1 )
echo.

echo ==============================================================
echo   CROSS-PROJECT TEST SUMMARY
echo ==============================================================
echo   !R1!  Test.Automated
echo   !R2!  Test.XUnit
echo --------------------------------------------------------------
if !OVERALL_EXIT! equ 0 (
    echo   OVERALL: PASS
) else (
    echo   OVERALL: FAIL ^(!FAIL_COUNT! project^(s^) failed^)
)
echo ==============================================================

exit /b !OVERALL_EXIT!
