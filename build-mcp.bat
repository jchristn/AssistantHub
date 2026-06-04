@echo off
setlocal

if "%~1"=="" (
    echo Usage: build-mcp.bat ^<tag^>
    echo Example: build-mcp.bat v0.12.0
    exit /b 1
)

set TAG=%~1
set IMAGE=jchristn77/assistanthub-mcp

echo Building %IMAGE%:latest and %IMAGE%:%TAG%...
docker buildx build ^
    --builder cloud-jchristn77-jchristn77 ^
    --platform linux/amd64,linux/arm64/v8 ^
    -t %IMAGE%:latest ^
    -t %IMAGE%:%TAG% ^
    -f src/AssistantHub.McpServer/Dockerfile ^
    --push ^
    .

echo Done.
endlocal
