@echo off
setlocal enabledelayedexpansion

REM ==========================================================================
REM reset.bat - Reset AssistantHub docker environment to factory defaults
REM
REM This script destroys all runtime data (PostgreSQL data, logs, object
REM storage, request history) and restores factory-default configuration.
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
echo   - All PostgreSQL data (AssistantHub, Less3, Partio,
echo     RecallDB collections, Verbex indices, embeddings,
echo     tenants, users)
echo   - Stale local SQLite database files from older deployments
echo   - All object storage files (uploaded documents)
echo   - All log files and processing logs
echo   - All Partio request history
echo   - All Verbex index data
echo   - Service configuration changes
if "%INCLUDE_MODELS%"=="true" (
    echo   - All downloaded Ollama models
)
echo.
echo Service configuration will be restored to factory defaults.
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
docker volume rm docker_postgres-data 2>nul
docker volume rm postgres-data 2>nul
docker volume rm docker_pgvector-data 2>nul
docker volume rm pgvector-data 2>nul
if "%INCLUDE_MODELS%"=="true" (
    docker volume rm docker_ollama-models 2>nul
    docker volume rm ollama-models 2>nul
    echo         Removed postgres-data and ollama-models volumes
) else (
    echo         Removed postgres-data volume ^(ollama-models preserved^)
)

REM -------------------------------------------------------------------------
REM Restore factory configuration and clear stale SQLite files
REM -------------------------------------------------------------------------
echo [3/6] Restoring factory configuration...

del /q "%DOCKER_DIR%assistanthub\data\assistanthub.db" 2>nul
del /q "%DOCKER_DIR%assistanthub\data\assistanthub.db-shm" 2>nul
del /q "%DOCKER_DIR%assistanthub\data\assistanthub.db-wal" 2>nul
copy /y "%FACTORY_DIR%assistanthub.json" "%DOCKER_DIR%assistanthub\assistanthub.json" >nul
echo         Restored assistanthub.json and removed stale AssistantHub SQLite files

del /q "%DOCKER_DIR%less3\less3.db" 2>nul
del /q "%DOCKER_DIR%less3\less3.db-shm" 2>nul
del /q "%DOCKER_DIR%less3\less3.db-wal" 2>nul
copy /y "%FACTORY_DIR%less3.system.json" "%DOCKER_DIR%less3\system.json" >nul
echo         Restored Less3 system.json and removed stale Less3 SQLite files

del /q "%DOCKER_DIR%partio\data\partio.db" 2>nul
del /q "%DOCKER_DIR%partio\data\partio.db-shm" 2>nul
del /q "%DOCKER_DIR%partio\data\partio.db-wal" 2>nul
copy /y "%FACTORY_DIR%partio.json" "%DOCKER_DIR%partio\partio.json" >nul
echo         Restored partio.json and removed stale Partio SQLite files

copy /y "%FACTORY_DIR%recalldb.json" "%DOCKER_DIR%recalldb\recalldb.json" >nul
echo         Restored recalldb.json

del /q "%DOCKER_DIR%verbex\data\verbex.db" 2>nul
del /q "%DOCKER_DIR%verbex\data\verbex.db-shm" 2>nul
del /q "%DOCKER_DIR%verbex\data\verbex.db-wal" 2>nul
copy /y "%FACTORY_DIR%verbex.json" "%DOCKER_DIR%verbex\verbex.json" >nul
echo         Restored verbex.json and removed stale Verbex SQLite files

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
rd /s /q "%DOCKER_DIR%assistanthub\crawl-enumerations" 2>nul
mkdir "%DOCKER_DIR%assistanthub\crawl-enumerations" 2>nul
echo         Cleared AssistantHub logs, processing logs, and crawl enumerations

del /q "%DOCKER_DIR%less3\logs\*" 2>nul
echo         Cleared Less3 logs

del /q "%DOCKER_DIR%documentatom\logs\*" 2>nul
echo         Cleared DocumentAtom logs

del /q "%DOCKER_DIR%partio\logs\*" 2>nul
del /q "%DOCKER_DIR%partio\request-history\*" 2>nul
echo         Cleared Partio logs and request history

del /q "%DOCKER_DIR%verbex\logs\*" 2>nul
rd /s /q "%DOCKER_DIR%verbex\data" 2>nul
mkdir "%DOCKER_DIR%verbex\data" 2>nul
echo         Cleared Verbex logs and index data

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
