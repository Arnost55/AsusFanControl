@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0batch-smoke.ps1" %*
exit /b %ERRORLEVEL%
