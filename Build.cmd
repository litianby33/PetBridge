@echo off
setlocal
cd /d "%~dp0"
if not exist "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
  echo .NET Framework 4.x x64 compiler is required.
  exit /b 1
)
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /utf8output /platform:x64 /win32icon:assets\app.ico /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.Core.dll /out:PetBridge.exe src\*.cs
exit /b %errorlevel%
