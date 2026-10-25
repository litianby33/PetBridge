param([Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$stage=Join-Path ([IO.Path]::GetTempPath()) ('petbridge-chat-preview-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $stage|Out-Null
$sources=@(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' -File|ForEach-Object FullName)
$exe=Join-Path $stage 'ChatModePreview.exe'
& $compiler /nologo /target:winexe /platform:x64 /main:PetBridge.CompactChatChecks /utf8output /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.Core.dll "/out:$exe" $sources (Join-Path $projectRoot 'tools/ChatModePreview.cs')
if($LASTEXITCODE-ne 0){throw 'Chat mode preview compiler failed'}
$info=New-Object Diagnostics.ProcessStartInfo
$info.FileName=$exe
$info.Arguments='"'+$projectRoot+'" "'+[IO.Path]::GetFullPath($Output)+'" "'+(Join-Path $stage 'verification.txt')+'"'
$info.UseShellExecute=$false
$info.CreateNoWindow=$true
$info.WindowStyle='Hidden'
$process=[Diagnostics.Process]::Start($info)
if(!$process.WaitForExit(30000)){$process.Kill();throw 'Chat mode preview timed out'}
if($process.ExitCode-ne 0){throw ('Chat mode preview failed; inspect '+$stage)}
Write-Output ('Rendered compact chat mode preview: '+$Output)
