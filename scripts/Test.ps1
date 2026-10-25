$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'Build.ps1')
$testProfile = Join-Path ([IO.Path]::GetTempPath()) ('PetBridge-checks-' + [Guid]::NewGuid().ToString('N'))
$info = New-Object Diagnostics.ProcessStartInfo
$info.FileName = Join-Path $projectRoot 'PetBridge.exe'
$info.Arguments = '--self-test'
$info.UseShellExecute = $false
$info.CreateNoWindow = $true
$info.EnvironmentVariables['PETBRIDGE_DATA_DIR'] = $testProfile
$process = [Diagnostics.Process]::Start($info)
if (-not $process.WaitForExit(30000)) { $process.Kill(); throw 'Tests timed out.' }
if ($process.ExitCode -ne 0) { throw (Get-Content -LiteralPath (Join-Path $testProfile 'checks\startup-error.txt') -Raw) }
Write-Output "PASS. Test reports: $testProfile\checks"
