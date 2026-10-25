@echo off
setlocal
cd /d "%~dp0"
if not exist PetBridge.exe call Build.cmd
if not exist PetBridge.exe exit /b 1
set "PETBRIDGE_PYTHON="
if exist ".venv\Scripts\python.exe" set "PETBRIDGE_PYTHON=%~dp0.venv\Scripts\python.exe"
if not defined PETBRIDGE_PYTHON if exist "%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe" set "PETBRIDGE_PYTHON=%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"
if not defined PETBRIDGE_PYTHON for /f "delims=" %%P in ('where python.exe 2^>nul') do if not defined PETBRIDGE_PYTHON set "PETBRIDGE_PYTHON=%%P"
if not defined PETBRIDGE_PYTHON (
  echo Ask Codex to install Python and requirements-tools.txt in a project venv.
  pause
  exit /b 1
)
"%PETBRIDGE_PYTHON%" -c "from PIL import Image" 2>nul
if errorlevel 1 (
  echo Ask Codex to install requirements-tools.txt in .venv. The base app needs no Python.
  pause
  exit /b 1
)
"%PETBRIDGE_PYTHON%" tools\get_gpt_muse.py --start
if errorlevel 1 pause
