@echo off
pushd "%~dp0"
if exist assistanthub.json del /f assistanthub.json
if exist logs rd /s /q logs
if exist assistanthub.db del /f assistanthub.db
echo Cleanup complete.
popd
