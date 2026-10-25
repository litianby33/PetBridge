param([Parameter(Mandatory=$true)][string]$ThreadLink)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'PetBridge.exe'))) { & (Join-Path $PSScriptRoot 'Build.ps1') }
$quit = Start-Process -FilePath (Join-Path $projectRoot 'PetBridge.exe') -ArgumentList '--signal','quit' -WindowStyle Hidden -PassThru
$quit.WaitForExit(3000) | Out-Null
Start-Sleep -Milliseconds 700
$process = Start-Process -FilePath (Join-Path $projectRoot 'PetBridge.exe') -ArgumentList '--bind',$ThreadLink -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw 'Binding failed. Use the technical ID of one local Codex chat.' }
Write-Output 'Bound locally. No message was sent and no credentials were copied. Run Start.cmd.'
