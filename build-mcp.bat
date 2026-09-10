@echo off
setlocal

if "%~1"=="" (
    echo Usage: build-mcp.bat ^<tag^>
    echo Example: build-mcp.bat v0.12.0
    exit /b 1
)

set TAG=%~1
set IMAGE=jchristn77/assistanthub-mcp

echo Building and pushing %IMAGE%:latest and %IMAGE%:%TAG%...
docker buildx build ^
    --builder cloud-jchristn77-jchristn77 ^
    --platform linux/amd64,linux/arm64/v8 ^
    -t %IMAGE%:latest ^
    -t %IMAGE%:%TAG% ^
    -f src/AssistantHub.McpServer/Dockerfile ^
    --push ^
    .
if errorlevel 1 (
    echo Build/push failed.
    endlocal
    exit /b 1
)

echo Pulling %IMAGE% into the local registry...
docker pull %IMAGE%:%TAG%
if errorlevel 1 (
    echo Pull of %IMAGE%:%TAG% failed.
    endlocal
    exit /b 1
)
docker pull %IMAGE%:latest
if errorlevel 1 (
    echo Pull of %IMAGE%:latest failed.
    endlocal
    exit /b 1
)

echo Done.
endlocal
