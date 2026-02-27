#!/bin/bash
set -e

# Fix ownership on all volume-mounted paths so appuser can write to them.
# Docker volume mounts inherit host-side ownership, which may not match
# the container's appuser (UID 1001).  Running this at startup ensures
# permissions are correct regardless of the host environment.
chown -R appuser:appuser /app /home/appuser

# Drop privileges and run the application as appuser.
# Playwright's Firefox sandbox requires a non-root user.
exec gosu appuser dotnet AssistantHub.Server.dll
