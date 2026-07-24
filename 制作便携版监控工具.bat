@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Package_E_Key_Monitor.ps1"
set "EXIT_CODE=%ERRORLEVEL%"
pause
exit /b %EXIT_CODE%
