"""Explicit optional upstream download; artwork is not bundled or relicensed."""
import argparse
import hashlib
import os
from pathlib import Path
import subprocess
import time
import urllib.request
from import_pet import import_pet

URL = "https://raw.githubusercontent.com/legeling/awesome-codex-pet/main/pets/gpt-muse--opask/spritesheet.webp"
SHA256 = "1ba124cc9972b2fa14c272c02c7c5c81550fb06e5304a02b4c7209c7d44b5fc8"

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--start", action="store_true")
    args = parser.parse_args()
    project = Path(__file__).resolve().parent.parent
    base = Path(os.environ.get("PETBRIDGE_DATA_DIR") or Path(os.environ["LOCALAPPDATA"]) / "PetBridge")
    folder = base / "characters" / "gpt-muse"
    folder.mkdir(parents=True, exist_ok=True)
    print("GPT-muse by opask. Upstream redistribution license: UNKNOWN. Not part of PetBridge's MIT assets.")
    request = urllib.request.Request(URL, headers={"User-Agent": "PetBridge-optional-character-import/1.0"})
    with urllib.request.urlopen(request, timeout=45) as response:
        data = response.read(10 * 1024 * 1024)
    if hashlib.sha256(data).hexdigest() != SHA256:
        raise SystemExit("Upstream checksum changed. Ask Codex to review the source and license before updating.")
    source = folder / "source.webp"
    source.write_bytes(data)
    manifest = import_pet(source, folder, name="GPT-muse", overwrite=True)
    (folder / "SOURCE.txt").write_text("GPT-muse by opask\n" + URL + "\nRedistribution license unknown; not covered by PetBridge MIT license.\n", encoding="utf-8")
    exe = project / "PetBridge.exe"
    if not exe.is_file():
        raise SystemExit("Downloaded and imported. Run Build.cmd, then select the character.json in Settings > Binding.")
    flags = getattr(subprocess, "CREATE_NO_WINDOW", 0)
    subprocess.run([str(exe), "--signal", "quit"], creationflags=flags, check=False)
    time.sleep(0.7)
    subprocess.run([str(exe), "--set-character", str(manifest)], creationflags=flags, check=True)
    if args.start:
        subprocess.Popen([str(exe)], cwd=project)
    print("Ready. The original source and other desktop pets were not modified.")
