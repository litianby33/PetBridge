@echo off
setlocal
cd /d "%~dp0"
if not exist "PetBridge.exe" call Build.cmd
if not exist "PetBridge.exe" exit /b 1
start "" "%~dp0PetBridge.exe"
