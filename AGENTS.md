# PetBridge adaptation instructions

This repository is a standalone Windows desktop pet. Adapt only this checkout and
the recipient's PetBridge profile; do not overwrite another pet or a ChatGPT install.

## Start here

1. Read `README.md`, `docs/CODEX-ADAPT.zh-CN.md` and `docs/ANIMATIONS.zh-CN.md`.
2. Inspect the artwork the user actually supplies. Keep private artwork outside
   the checkout, normally in `%LOCALAPPDATA%/PetBridge/characters/`.
3. Do not read/copy `auth.json`, cookies, tokens, transcript bodies or the user's
   entire `.codex` folder into this project. Never add runtime settings to Git.
4. Code is MIT. Imported images keep their own rights. GPT-muse is optional and
   its upstream redistribution license is unknown; do not bundle its pixels.

## Bind the recipient's own environment

- Detect the installed Codex CLI without starting inference. Honor `CODEX_HOME`.
- Use this conversation's real **local Codex technical thread ID** if available.
  The app tool `list_threads` can verify kind `codex`, host `local` and the ID.
  Never use the synthetic IDs from tests as a real binding.
- With the recipient's authorization to adapt/bind, run the local `--bind` command
  or `scripts/Bind-Codex.ps1`. It only saves PetBridge's private profile.
- Use `--bridge-probe` to check read-only app-tools connectivity. If the plugin
  or desktop app is unavailable, report the exact unsupported component and
  retain rendering/local mode. Do not claim API availability from an idle screen.
- Do not send test chat messages or change other conversations just to test.
- Autostart is opt-in. Do not run `Install-Autostart.ps1` unless requested.

## Bind artwork

- Prefer `tools/import_pet.py` or `scripts/Import-Character.ps1` for original PNG /
  WebP atlases or a static image. Find an existing Python+Pillow first; if absent,
  create a project `.venv` and install `requirements-tools.txt` there.
- For unknown atlas layouts, inspect a contact sheet, then edit row, column,
  frameCount, fps and `purpose` per slot. Do not infer rows from names alone.
- `frameCount: 0` or `enabled: false` means unavailable. Every missing non-idle
  action must resolve to a useful baseline. A missing jump must remain the
  whole-character vertical simulation, never a blank frame or index exception.
- Do not generate extra images unless the user requests them. Retain approved
  source art. For v1 without gaze frames, keep the front-facing fallback.
- Validate with `--validate-character`, then test a preview and edge poses.
  `--character` is a temporary override; `--set-character` persists selection.

## Verify and explain

- Build with `Build.cmd` or `scripts/Build.ps1`, then run `scripts/Test.ps1`.
- Use `--qa --offline` and a separate `PETBRIDGE_DATA_DIR` for UI previews.
  Previews hide taskbar entries by default; use `--qa-window` only when window
  inventory is explicitly needed for development inspection.
  PetBridge's QA IPC is different from its normal IPC and other pet products.
- Cover idle, moving, thinking, no-jump fallback, below/above notices, input
  reminders, desktop navigation and quota dedup when touching those paths.
- State what was actually tested. Cloud Work's unexported asynchronous prompts
  are not guaranteed; the quota panel is Codex usage, not universal ChatGPT usage.
- Actual GitHub publication is outside local adaptation unless the user asks.
  Use the allowlist packager and release scanner before preparing public files.

## Main extension points

- `src/CharacterBank.cs`: action slot names, atlas slicing, fallback graph, peek.
- `characters/examples/*.json`: annotated role mapping examples.
- `src/NoticeJump.cs`, `src/Behavior.cs`: sprite / simulated jump and bubble contact.
- `src/Integration.cs`: local lifecycle, quota reader, distinct IPC and watcher.
- `src/AppBridge.cs`, `src/AppNavigation.cs`: installed-app compatibility adapter.
- `src/InputReminder.cs`: structured questions and answers; no text guessing.
- `src/QuotaPolicy.cs`, `src/TaskEndLedger.cs`: periodic silence at zero, one per turn.
