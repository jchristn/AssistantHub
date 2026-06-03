@echo off
REM Run AssistantHub test suites and print a summary.
REM Exit code: 0 only if every project returned 0.

setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "OVERALL_EXIT=0"
set "FAIL_COUNT=0"
set "R1=SKIP"
set "R2=SKIP"
set "R3=SKIP"

echo ==============================================================
echo   AssistantHub Test Runner
echo ==============================================================
if defined ASSISTANTHUB_TEST_SUITES (
    echo   ASSISTANTHUB_TEST_SUITES=%ASSISTANTHUB_TEST_SUITES%
    echo   Note: suite filtering applies to Touchstone-backed .NET test projects.
)
if defined ASSISTANTHUB_TEST_KEEP_ARTIFACTS (
    echo   ASSISTANTHUB_TEST_KEEP_ARTIFACTS=%ASSISTANTHUB_TEST_KEEP_ARTIFACTS%
)
echo.

REM --- Test.Automated (console runner) ---
echo --- Test.Automated ---
dotnet run --project "%SCRIPT_DIR%src\Test.Automated"
if !ERRORLEVEL! equ 0 ( set "R1=PASS" ) else ( set "R1=FAIL" & set "OVERALL_EXIT=1" & set /a FAIL_COUNT+=1 )
echo.

REM --- Test.Xunit (xUnit runner) ---
echo --- Test.Xunit ---
dotnet test "%SCRIPT_DIR%src\Test.Xunit" --no-build --verbosity normal
if !ERRORLEVEL! equ 0 ( set "R2=PASS" ) else ( set "R2=FAIL" & set "OVERALL_EXIT=1" & set /a FAIL_COUNT+=1 )
echo.

REM --- Test.Nunit (NUnit runner) ---
echo --- Test.Nunit ---
dotnet test "%SCRIPT_DIR%src\Test.Nunit" --no-build --verbosity normal
if !ERRORLEVEL! equ 0 ( set "R3=PASS" ) else ( set "R3=FAIL" & set "OVERALL_EXIT=1" & set /a FAIL_COUNT+=1 )
echo.

echo ==============================================================
echo   CROSS-PROJECT TEST SUMMARY
echo ==============================================================
echo   !R1!  Test.Automated
echo   !R2!  Test.Xunit
echo   !R3!  Test.Nunit
echo --------------------------------------------------------------
if !OVERALL_EXIT! equ 0 (
    echo   OVERALL: PASS
) else (
    echo   OVERALL: FAIL ^(!FAIL_COUNT! project^(s^) failed^)
)
echo ==============================================================

exit /b !OVERALL_EXIT!
