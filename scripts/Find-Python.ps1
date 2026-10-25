function Find-PetBridgePython {
    $candidates = @((Join-Path (Split-Path $PSScriptRoot -Parent) ".venv\Scripts\python.exe"))
    foreach ($command in @('python.exe','python3.exe')) {
        $found = Get-Command $command -ErrorAction SilentlyContinue
        if ($found) { $candidates += $found.Source }
    }
    $candidates += (Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe')
    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $candidate)) { continue }
        & $candidate -c 'from PIL import Image; import sys; assert sys.version_info >= (3,9)' 2>$null
        if ($LASTEXITCODE -eq 0) { return $candidate }
    }
    throw 'Python 3.9+ with Pillow is needed to decode WebP. Ask your Codex to create a project venv and install requirements-tools.txt, then run .venv\Scripts\python.exe tools\import_pet.py SOURCE DESTINATION. The app itself does not require Python.'
}
