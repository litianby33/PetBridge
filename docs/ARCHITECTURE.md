# Architecture / adaptation map

## App and profiles

Windows x64 WinForms + layered windows; .NET Framework 4.8 runtime and Windows 4.x compiler. No Node or Python dependency for drawing the pet. Optional app bridging launches the recipient's installed, unmodified app-tools server with its own Node runtime.

`Program.Root` is read-only project content. `Program.DataRoot` holds private settings and diagnostics, selected via `PETBRIDGE_DATA_DIR` or LocalApplicationData. QA and self-tests use separate profile subdirectories. Named pipes and mutexes use the PetBridge name, never another pet's names.

## Character adapter

`CharacterBank.cs` owns frame extraction, a named animation-slot table, fallback graph, normalization, baseline bounds and half-body peeking. The rest of the UI deals with resolved frames, not a particular person's costume or coordinates. Missing jump poses use vertical rendering displacement in `Behavior.cs`; native jump timing follows the configured frame count and fps. Missing gaze keeps an idle pose.

No third-party role is embedded in the executable. The built-in fallback and icon are drawn from project-owned code. Import tools decode user-provided art outside the checkout.

## State adapters

- `Integration.cs`: local rollout lifecycle and Codex quota reads.
- `Activity.cs`: active-turn reducer and small loading capsule.
- `AppBridge.cs`: read-only list/read compatibility queries, explicit terminal state mapping.
- `AppNavigation.cs`: app navigation only when the user clicks a conversation reminder.
- `InputReminder.cs`: structured question/answer reducer; async `accepted` is not a human answer.
- `QuotaPolicy.cs`: threshold and reset policy. Polling at exhausted quota stays silent.
- `TaskEndLedger.cs`: cross-source, cross-restart terminal-turn dedup.

State adapters do not send user chat messages, scrape authentication, or automate ChatGPT UI. The pet's own composer invokes the installed CLI only after an explicit Send action.

## Integration compatibility

The local runtime adapter is tested against the developer's current desktop version. Protocol and directory names may change. Cloud asynchronous questions may not appear in normalized query results. Keep those limits visible; never turn an inaccessible/idle/error state into a fabricated completion or permission request.

## Testing

`scripts/Test.ps1` launches self-tests with an isolated temporary profile, offline. They cover fallback art, variable native frame counts, simulated jumps, message placement, input requests, terminal state mapping, quota silence at zero, persistent dedup, and route validation.

UI testing may use `--qa --offline --character <manifest>` and QA IPC commands such as `notify-test`, `input-test`, `dock-left`, `near-top` or `think-test`. Do not put a recipient's real profile or character in Git to reproduce a bug.

## Settings UI

`src/SettingsChrome.cs` provides the compact context menu, draggable sidebar settings shell, switches, buttons and sliders. `SettingsForm.cs` retains the existing setting bindings. Window coordinates are saved as `SettingsX` / `SettingsY` and clamped to an attached monitor when reopened.
