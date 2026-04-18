# Stories

One story per file. Each story is scoped so a single agent can own it, deliver against its Acceptance Criteria, and hand off. Sequence and dependencies are in [../ROADMAP.md](../ROADMAP.md); architectural contracts are in [../ARCHITECTURE.md](../ARCHITECTURE.md).

## Conventions

- **Read [../AGENTS.md](../AGENTS.md) and [../ARCHITECTURE.md](../ARCHITECTURE.md) before starting a story.**
- Don't expand scope mid-story. If a dependency gap or architectural conflict appears, update ARCHITECTURE.md and flag it; don't silently reshape the design.
- Don't commit unless the user asks. Leave changes on the working tree.
- Placeholder art/audio is fine — the artist and user will drop in real assets against the slots defined in each story.

## Index

### M0 — Bootstrap
- [S01 — Project Bootstrap & Folder Conventions](S01-project-bootstrap.md)
- [S02 — UI Toolkit Scaffolding](S02-ui-toolkit-scaffolding.md)

### M1 — Playable Core
- [S03 — Main Scene Composition & Camera](S03-scene-composition.md)
- [S04 — CRT + Desk Prefab Structure](S04-crt-desk-prefab.md)
- [S05 — Controls UXML + KnobElement](S05-controls-uxml.md)
- [S06 — TuningState & Control Bindings](S06-tuning-state.md)
- [S07 — SignalData ScriptableObject + Authoring](S07-signal-data.md)
- [S08 — SignalEvaluator (Clarity Math)](S08-signal-evaluator.md)
- [S09 — SignalManager & LockEvaluator](S09-signal-manager.md)

### M2 — Feel
- [S10 — CRT Shader Graph](S10-crt-shader.md)
- [S11 — Reveal / Distortion Pipeline](S11-reveal-pipeline.md)
- [S12 — WaveformElement Reactive Visual](S12-waveform-visual.md)
- [S13 — Post-FX + Ambient Flicker](S13-postfx-ambient.md)
- [S14 — AudioDirector with Reactive Mix](S14-audio-director.md)

### M3 — Polish
- [S15 — Lock Feedback & Transitions](S15-lock-feedback.md)
- [S16 — Intro / Outro Cards](S16-intro-outro.md)
- [S17 — Playtest & Mix Pass](S17-playtest-mix.md)

### M4 — Stretch
- [S18 — Archive / Gallery](S18-archive-gallery.md)
- [S19 — Sticky-Note Clues](S19-sticky-note-clues.md)
- [S20 — Final "Special" Signal](S20-final-special-signal.md)
