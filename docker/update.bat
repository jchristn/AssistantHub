@echo off
setlocal

REM ==========================================================================
REM update.bat - Pull and restart the AssistantHub docker deployment
REM
REM Usage: update.bat
REM ==========================================================================

set "SCRIPT_DIR=%~dp0"

pushd "%SCRIPT_DIR%" >nul
if errorlevel 1 exit /b 1

echo.
echo ==========================================================
echo   AssistantHub - Update Docker Deployment
echo ==========================================================
echo.

echo [1/3] Stopping containers...
docker compose down
if errorlevel 1 goto :error

echo.
echo [2/3] Pulling images...
docker compose pull
if errorlevel 1 goto :error

echo.
echo [3/3] Starting containers...
docker compose up -d
if errorlevel 1 goto :error

echo.
echo Update complete.
popd >nul
exit /b 0

:error
echo.
echo Update failed.
popd >nul
exit /b 1
