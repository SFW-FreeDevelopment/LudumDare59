# S01 — Project Bootstrap & Folder Conventions

**Milestone:** M0 — Bootstrap
**Priority:** 🟦 MVP
**Depends on:** —

## Goal

Stand up the empty shell: folders, scene stub, URP profile verified, script namespaces. Everything downstream assumes this layout.

## Deliverables

1. Create the folder layout under `Unity/Assets/` exactly as specified in [ARCHITECTURE.md](../ARCHITECTURE.md) §Folder Map. Empty folders get a `.gitkeep`.
2. Rename the default scene to `Main.unity` under `Assets/Scenes/`. Delete any other sample scenes.
3. Confirm URP is active: `Edit > Project Settings > Graphics` references the URP asset in `Assets/Settings/`. Fix if not.
4. Add an `Assets/Art/README.md` with one-paragraph art import conventions (pixels-per-unit 100, filter mode notes).
5. Add assembly namespaces `SignalScrubber.Core`, `SignalScrubber.Rendering`, `SignalScrubber.UI`, `SignalScrubber.Audio` — by convention only (no asmdef files for MVP).
6. Commit nothing. Leave changes on the working tree.

## Acceptance Criteria

- [ ] All folders from ARCHITECTURE.md exist.
- [ ] `Main.unity` opens without errors and no missing scripts.
- [ ] URP is confirmed active (Game view does not show the "no SRP" pink).
- [ ] `Assets/Art/README.md` exists.

## Out of Scope

- No scripts yet (other stories create them).
- No prefabs or materials.

## Implementation Notes

- The Unity project already has `com.unity.render-pipelines.universal` 17.3 and 2D importers installed (see `Unity/Packages/manifest.json`). Do not add packages here.
- Do not touch `ProjectSettings/` beyond verification.
