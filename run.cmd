@echo off
rem Uruchamia aplikacje bez Visual Studio. Dwuklik albo: run.cmd [-Sqlite]
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run.ps1" %*
if errorlevel 1 pause
