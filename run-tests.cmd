@echo off
rem Uruchamia wszystkie testy (nie wymagaja SQL Servera ani poczty).
dotnet test "%~dp0InternetVotingApplication.sln"
pause
