@echo off
setlocal

docker ps -a --format "table {{.ID}}\t{{.Names}}\t{{.CreatedAt}}\t{{.Status}}\t{{.Ports}}"

endlocal
