$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x x64 compiler not found. Windows x64 with .NET Framework 4.8 is required.' }
$sources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' -File | ForEach-Object FullName)
& $compiler /nologo /target:winexe /optimize+ /utf8output /platform:x64 "/win32icon:$projectRoot\assets\app.ico" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.Core.dll "/out:$projectRoot\PetBridge.exe" $sources
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output "Built $projectRoot\PetBridge.exe"
