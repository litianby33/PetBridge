param([switch]$Start)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
# Attribution is required in documentation; no redistribution permission is inferred.
# The asset is fetched only after the user explicitly runs this optional script.
$uri = 'https://raw.githubusercontent.com/legeling/awesome-codex-pet/main/pets/gpt-muse--opask/spritesheet.webp'
$expected = '1ba124cc9972b2fa14c272c02c7c5c81550fb06e5304a02b4c7209c7d44b5fc8'
$destination = Join-Path $env:LOCALAPPDATA 'PetBridge\characters\gpt-muse'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$download = Join-Path $destination 'source.webp'
Write-Output 'GPT-muse by opask. Upstream says redistribution license unknown; this artwork is not MIT-licensed by PetBridge.'
Invoke-WebRequest -Uri $uri -OutFile $download -UseBasicParsing
if ((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) { throw 'Upstream asset changed; stopped before importing. Ask Codex to review source and license.' }
& (Join-Path $PSScriptRoot 'Import-Character.ps1') -Source $download -Destination $destination -Name 'GPT-muse' -Overwrite
if ($Start) { Start-Process -FilePath (Join-Path $projectRoot 'PetBridge.exe') -WorkingDirectory $projectRoot -WindowStyle Normal }
