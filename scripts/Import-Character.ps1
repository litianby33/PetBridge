param(
    [Parameter(Mandatory=$true)][string]$Source,
    [string]$Destination,
    [string]$Name,
    [switch]$Overwrite
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'Find-Python.ps1')
$python = Find-PetBridgePython
if (-not $Destination) { $Destination = Join-Path $env:LOCALAPPDATA ('PetBridge\characters\import-' + (Get-Date -Format yyyyMMdd-HHmmss)) }
$arguments = @((Join-Path $projectRoot 'tools\import_pet.py'), $Source, $Destination)
if ($Name) { $arguments += @('--name', $Name) }
if ($Overwrite) { $arguments += '--overwrite' }
& $python @arguments
if ($LASTEXITCODE -ne 0) { throw 'Character import failed; the source files were not modified.' }
$manifest = Join-Path ([IO.Path]::GetFullPath($Destination)) 'character.json'
if (-not (Test-Path -LiteralPath (Join-Path $projectRoot 'PetBridge.exe'))) { & (Join-Path $PSScriptRoot 'Build.ps1') }
# Only this product's IPC is contacted, never another pet or ChatGPT itself.
$quit = Start-Process -FilePath (Join-Path $projectRoot 'PetBridge.exe') -ArgumentList '--signal','quit' -WindowStyle Hidden -PassThru
$quit.WaitForExit(3000) | Out-Null
Start-Sleep -Milliseconds 700
$info = New-Object Diagnostics.ProcessStartInfo
$info.FileName = Join-Path $projectRoot 'PetBridge.exe'
$info.UseShellExecute = $false
$info.CreateNoWindow = $true
$info.Arguments = '--set-character "' + $manifest + '"'
$process = [Diagnostics.Process]::Start($info)
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw 'Character validation failed. See the PetBridge profile startup-error.txt.' }
Write-Output 'Imported and selected. Run Start.cmd to preview. Binding is separate; no chat was sent.'
