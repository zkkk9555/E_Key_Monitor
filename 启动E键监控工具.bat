@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start_E_Key_Monitor.ps1"
exit /b %ERRORLEVEL%
