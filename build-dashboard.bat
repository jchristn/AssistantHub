@echo off
setlocal

if "%~1"=="" (
    echo Usage: build-dashboard.bat ^<tag^>
    echo Example: build-dashboard.bat v0.12.0
    exit /b 1
)

set TAG=%~1
set IMAGE=jchristn77/assistanthub-dashboard

echo Building and pushing %IMAGE%:latest and %IMAGE%:%TAG%...
docker buildx build ^
    --builder cloud-jchristn77-jchristn77 ^
    --platform linux/amd64,linux/arm64/v8 ^
    -t %IMAGE%:latest ^
    -t %IMAGE%:%TAG% ^
    -f dashboard/Dockerfile ^
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
