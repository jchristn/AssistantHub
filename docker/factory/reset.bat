@echo off
setlocal enabledelayedexpansion

REM ==========================================================================
REM reset.bat - Reset AssistantHub docker environment to factory defaults
REM
REM This script destroys all runtime data (databases, logs, object storage,
REM vector data) and restores factory-default databases. Configuration files
REM are preserved.
REM
REM Usage: factory\reset.bat [--include-models]
REM   --include-models  Also remove downloaded Ollama models (requires re-download)
REM ==========================================================================

set "SCRIPT_DIR=%~dp0"
set "DOCKER_DIR=%SCRIPT_DIR%..\"
set "FACTORY_DIR=%SCRIPT_DIR%"
set "INCLUDE_MODELS=false"

if "%~1"=="--include-models" set "INCLUDE_MODELS=true"

REM -------------------------------------------------------------------------
REM Confirmation prompt
REM -------------------------------------------------------------------------
echo.
echo ==========================================================
echo   AssistantHub - Reset to Factory Defaults
echo ==========================================================
echo.
echo WARNING: This is a DESTRUCTIVE action. The following will
echo be permanently deleted:
echo.
echo   - All SQLite databases (AssistantHub, Less3, Partio)
echo   - All PostgreSQL/pgvector data (RecallDB collections,
echo     embeddings, tenants, users)
echo   - All object storage files (uploaded documents)
echo   - All log files and processing logs
echo   - All Partio request history
if "%INCLUDE_MODELS%"=="true" (
    echo   - All downloaded Ollama models
)
echo.
echo Configuration files will NOT be modified.
echo.
set /p "CONFIRM=Type 'RESET' to confirm: "
echo.

if not "%CONFIRM%"=="RESET" (
    echo Aborted. No changes were made.
    exit /b 1
)

REM -------------------------------------------------------------------------
REM Ensure containers are stopped
REM -------------------------------------------------------------------------
echo [1/6] Stopping containers...
pushd "%DOCKER_DIR%"
docker compose down 2>nul
popd

REM -------------------------------------------------------------------------
REM Remove Docker named volumes
REM -------------------------------------------------------------------------
echo [2/6] Removing Docker volumes...
docker volume rm docker_pgvector-data 2>nul
if errorlevel 1 docker volume rm pgvector-data 2>nul
if "%INCLUDE_MODELS%"=="true" (
    docker volume rm docker_ollama-models 2>nul
    if errorlevel 1 docker volume rm ollama-models 2>nul
    echo         Removed pgvector-data and ollama-models volumes
) else (
    echo         Removed pgvector-data volume ^(ollama-models preserved^)
)

REM -------------------------------------------------------------------------
REM Restore factory databases
REM -------------------------------------------------------------------------
echo [3/6] Restoring factory databases...

del /q "%DOCKER_DIR%assistanthub\data\assistanthub.db" 2>nul
del /q "%DOCKER_DIR%assistanthub\data\assistanthub.db-shm" 2>nul
del /q "%DOCKER_DIR%assistanthub\data\assistanthub.db-wal" 2>nul
copy /y "%FACTORY_DIR%assistanthub.db" "%DOCKER_DIR%assistanthub\data\assistanthub.db" >nul
copy /y "%FACTORY_DIR%assistanthub.db-shm" "%DOCKER_DIR%assistanthub\data\assistanthub.db-shm" >nul 2>nul
copy /y "%FACTORY_DIR%assistanthub.db-wal" "%DOCKER_DIR%assistanthub\data\assistanthub.db-wal" >nul 2>nul
echo         Restored assistanthub.db

del /q "%DOCKER_DIR%less3\less3.db" 2>nul
copy /y "%FACTORY_DIR%less3.db" "%DOCKER_DIR%less3\less3.db" >nul
echo         Restored less3.db

del /q "%DOCKER_DIR%partio\data\partio.db" 2>nul
del /q "%DOCKER_DIR%partio\data\partio.db-shm" 2>nul
del /q "%DOCKER_DIR%partio\data\partio.db-wal" 2>nul
copy /y "%FACTORY_DIR%partio.db" "%DOCKER_DIR%partio\data\partio.db" >nul
echo         Restored partio.db

REM -------------------------------------------------------------------------
REM Clear object storage
REM -------------------------------------------------------------------------
echo [4/6] Clearing object storage...
for /d %%d in ("%DOCKER_DIR%less3\disk\*") do (
    if exist "%%d\Objects" rd /s /q "%%d\Objects" 2>nul && mkdir "%%d\Objects" 2>nul
)
del /q "%DOCKER_DIR%less3\temp\*" 2>nul
echo         Cleared Less3 objects and temp files

REM -------------------------------------------------------------------------
REM Clear logs and request history
REM -------------------------------------------------------------------------
echo [5/6] Clearing logs and history...

del /q "%DOCKER_DIR%assistanthub\logs\*" 2>nul
rd /s /q "%DOCKER_DIR%assistanthub\processing-logs" 2>nul
mkdir "%DOCKER_DIR%assistanthub\processing-logs" 2>nul
echo         Cleared AssistantHub logs and processing logs

del /q "%DOCKER_DIR%less3\logs\*" 2>nul
echo         Cleared Less3 logs

del /q "%DOCKER_DIR%documentatom\logs\*" 2>nul
echo         Cleared DocumentAtom logs

del /q "%DOCKER_DIR%partio\logs\*" 2>nul
del /q "%DOCKER_DIR%partio\request-history\*" 2>nul
echo         Cleared Partio logs and request history

REM -------------------------------------------------------------------------
REM Done
REM -------------------------------------------------------------------------
echo [6/6] Factory reset complete.
echo.
echo To start the environment:
echo   cd %DOCKER_DIR%
echo   docker compose up -d
echo.

endlocal
