"""Check a public archive for accidentally packed profiles, secrets or personal paths."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import zipfile

ALLOWED_IDS = {"11111111-1111-4111-8111-111111111111", "22222222-2222-4222-8222-222222222222"}
UUID = re.compile(r"\b[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}\b")
PERSONAL_PATH = re.compile(r"[A-Za-z]:[\\/](?:Users|OpenAI)[\\/]", re.I)
TOKEN = re.compile(r"\bsk-(?:proj-)?[A-Za-z0-9_-]{24,}")
FORBIDDEN_DIRS = {".git", ".venv", "__pycache__", "backups", "chat-work", "data"}

def scan(archive, private_hashes=()):
    problems = []
    with zipfile.ZipFile(archive) as z:
        for entry in z.infolist():
            if entry.is_dir():
                continue
            parts = Path(entry.filename).parts
            name = parts[-1].lower()
            if any(p.lower() in FORBIDDEN_DIRS for p in parts) or "characters/local/" in entry.filename.lower():
                problems.append(entry.filename + ": private directory")
            if name in {"auth.json", "settings.json", "settings.qa.json", "startup-error.txt"} or name.startswith(("runtime-state", "terminal-reminders", "task-usage", "qa-", "notice-jump")):
                problems.append(entry.filename + ": runtime file")
            blob = z.read(entry)
            if hashlib.sha256(blob).hexdigest().lower() in private_hashes:
                problems.append(entry.filename + ": matches excluded private binary")
            if Path(name).suffix in {".cs", ".ps1", ".py", ".json", ".md", ".txt", ".html", ".cmd", ".yml"}:
                text = blob.decode("utf-8-sig", errors="replace")
                if PERSONAL_PATH.search(text):
                    problems.append(entry.filename + ": absolute personal workspace path")
                if any(x.lower() not in ALLOWED_IDS for x in UUID.findall(text)):
                    problems.append(entry.filename + ": nonsynthetic conversation-like UUID")
                if TOKEN.search(text):
                    problems.append(entry.filename + ": possible API secret")
    return problems

if __name__ == "__main__":
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("archive", nargs="+")
    p.add_argument("--private-hashes", help="Local-only JSON list of excluded hashes; never ship this baseline")
    args = p.parse_args()
    hashes = set()
    if args.private_hashes:
        records = json.loads(Path(args.private_hashes).read_text(encoding="utf-8-sig"))
        hashes = {str(r.get("sha256", "")).lower() for r in records if Path(r.get("path", "")).suffix.lower() in {".png", ".webp", ".ico"}}
    results = [{"archive": Path(a).name, "problems": scan(a, hashes)} for a in args.archive]
    print(json.dumps(results, ensure_ascii=False, indent=2))
    raise SystemExit(1 if any(r["problems"] for r in results) else 0)
