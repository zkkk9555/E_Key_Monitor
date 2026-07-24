@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Create_Naraka_Monitor_Shortcut.ps1"
exit /b %ERRORLEVEL%
