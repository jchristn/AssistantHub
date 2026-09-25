@echo off
REM Start an AssistantHub server built from the working tree against the isolated benchmark stack
REM (benchmarks\docker\compose.yaml). REST on :38800, admin API key "benchadmin", SQLite database and logs under
REM benchmarks\.run\server\. Build first: dotnet build src\AssistantHub.sln -c Release
setlocal
set "ROOT=%~dp0.."
set "RUN=%ROOT%\benchmarks\.run\server"
set "DLL=%ROOT%\src\AssistantHub.Server\bin\Release\net10.0\AssistantHub.Server.dll"
if not exist "%DLL%" (
  echo Server not built. Run: dotnet build src\AssistantHub.sln -c Release
  exit /b 1
)
if not exist "%RUN%" mkdir "%RUN%"
copy /Y "%ROOT%\benchmarks\docker\config\assistanthub.json" "%RUN%\assistanthub.json" >nul
pushd "%RUN%"
dotnet "%DLL%" %*
popd
endlocal
