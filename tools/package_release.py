"""Create source and Windows ZIPs from explicit public paths; never zip the workspace."""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile
from check_release import scan

ROOT_FILES = {"README.md", "LICENSE", "THIRD_PARTY_NOTICES.md", "AGENTS.md", "CONTRIBUTING.md", ".gitignore", ".gitattributes", "Build.cmd", "Start.cmd", "Start-GPT-muse.cmd", "requirements-tools.txt", "给Codex的提示词.txt"}
ROOT_DIRS = {"src", "scripts", "tools", "docs", "assets", ".github"}

def public_files(root):
    for file in sorted(root.rglob("*")):
        if not file.is_file():
            continue
        parts = file.relative_to(root).parts
        if "__pycache__" in parts or file.suffix == ".pyc":
            continue
        if len(parts) == 1 and parts[0] in ROOT_FILES:
            yield file
        elif parts[0] in ROOT_DIRS:
            yield file
        elif len(parts) > 2 and parts[0] == "characters" and parts[1] in {"default", "examples"}:
            yield file

if __name__ == "__main__":
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--output", required=True)
    p.add_argument("--version", default="1.3.2")
    args = p.parse_args()
    root = Path(__file__).resolve().parent.parent
    output = Path(args.output).resolve()
    if output == root or root in output.parents:
        raise SystemExit("Release output must be outside the checkout")
    output.mkdir(parents=True, exist_ok=True)
    files = list(public_files(root))
    exe = root / "PetBridge.exe"
    if not exe.is_file():
        raise SystemExit("Build PetBridge.exe first")
    artifacts = []
    for flavor, extra in (("source", []), ("windows-x64", [exe])):
        name = f"PetBridge-v{args.version}-{flavor}.zip"
        archive = output / name
        with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
            for file in files + extra:
                z.write(file, "PetBridge/" + file.relative_to(root).as_posix())
        errors = scan(archive)
        if errors:
            raise SystemExit("Release scan failed:\n" + "\n".join(errors))
        artifacts.append(dict(file=name, bytes=archive.stat().st_size, sha256=hashlib.sha256(archive.read_bytes()).hexdigest(), files=len(files)+len(extra)))
    (output / "SHA256SUMS.txt").write_text("".join(a["sha256"] + "  " + a["file"] + "\n" for a in artifacts), encoding="utf-8")
    (output / "release-manifest.json").write_text(json.dumps(dict(version=args.version, codeLicense="MIT", thirdPartyCharacterPixelsBundled=False, artifacts=artifacts), indent=2), encoding="utf-8")
    print(json.dumps(artifacts, indent=2))
