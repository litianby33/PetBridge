$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$exe = Join-Path $projectRoot 'PetBridge.exe'
if (-not (Test-Path -LiteralPath $exe)) { & (Join-Path $PSScriptRoot 'Build.ps1') }
$base = if ($env:PETBRIDGE_DATA_DIR) { $env:PETBRIDGE_DATA_DIR } else { Join-Path $env:LOCALAPPDATA 'PetBridge' }
$profileRoot = Join-Path $base 'profile'
New-Item -ItemType Directory -Path $profileRoot -Force | Out-Null
$settingsFile = Join-Path $profileRoot 'settings.json'
$settings = if (Test-Path -LiteralPath $settingsFile) { Get-Content -LiteralPath $settingsFile -Raw | ConvertFrom-Json } else { New-Object PSObject }
$settings | Add-Member -NotePropertyName AutoStartWithChatGPT -NotePropertyValue $true -Force
$settings | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $settingsFile -Encoding UTF8
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Startup')) 'PetBridge-with-ChatGPT.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exe
$shortcut.Arguments = '--watch'
$shortcut.WorkingDirectory = $projectRoot
$shortcut.WindowStyle = 7
$shortcut.Description = 'Start only PetBridge when the ChatGPT desktop app is running.'
$shortcut.Save()
Start-Process -FilePath $exe -ArgumentList '--watch' -WorkingDirectory $projectRoot -WindowStyle Hidden
Write-Output 'Enabled only PetBridge autostart. Other pets and ChatGPT settings were not changed.'
