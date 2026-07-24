@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start_Naraka_With_Monitor_Dev.ps1"
exit /b %ERRORLEVEL%
