param([Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$taskBuild=Join-Path ([IO.Path]::GetTempPath()) ('petbridge-doc-render-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $taskBuild | Out-Null
$sources=@(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' -File | ForEach-Object FullName)
$exporter=Join-Path $taskBuild 'DemoExport.exe'
& $compiler /nologo /target:winexe /main:PetBridge.DemoExport /platform:x64 /utf8output /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.Core.dll "/out:$exporter" $sources (Join-Path $projectRoot 'tools/DemoExport.cs')
if($LASTEXITCODE -ne 0){throw 'Documentation renderer build failed'}
$process=Start-Process -FilePath $exporter -ArgumentList ('"'+$projectRoot+'"'),('"'+[IO.Path]::GetFullPath($Output)+'"') -WindowStyle Hidden -Wait -PassThru
if($process.ExitCode -ne 0){throw 'Documentation renderer failed'}
& (Join-Path $PSScriptRoot 'Export-Chat-Modes.ps1') -Output (Join-Path $Output 'chat-mode-picker.png')
Write-Output "Rendered offline demonstration images: $Output"
