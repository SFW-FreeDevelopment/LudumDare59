# Roadmap — Signal Scrubber

Weekend-jam delivery plan. Stories are authored in `stories/` and sequenced here. An agent picking up a story should read it, check [ARCHITECTURE.md](ARCHITECTURE.md), and deliver against the story's Acceptance Criteria.

## Legend

- 🟦 **MVP** — must ship
- 🟨 **Polish** — ship if on track
- 🟪 **Stretch** — only if clearly ahead

## Milestones

### M0 — Bootstrap (≤ 2h)

Goal: empty scene boots, folders exist, UI Toolkit renders a placeholder element.

| Story | Title | Priority |
|-------|-------|----------|
| [S01](stories/S01-project-bootstrap.md) | Project bootstrap & folder conventions | 🟦 |
| [S02](stories/S02-ui-toolkit-scaffolding.md) | UI Toolkit scaffolding (PanelSettings, theme) | 🟦 |

**Exit criteria:** pressing Play shows the scene with a world-space UIDocument reading "TUNE THE SIGNAL".

### M1 — Playable Core (end of Day 1)

Goal: a player can tune three controls, press Lock, see a result, and advance through 3 signals — with placeholder art.

| Story | Title | Priority | Depends on |
|-------|-------|----------|------------|
| [S03](stories/S03-scene-composition.md) | Main scene composition & camera | 🟦 | S01 |
| [S04](stories/S04-crt-desk-prefab.md) | CRT + desk prefab structure | 🟦 | S03 |
| [S05](stories/S05-controls-uxml.md) | Controls UXML + KnobElement | 🟦 | S02 |
| [S06](stories/S06-tuning-state.md) | TuningState & control bindings | 🟦 | S05 |
| [S07](stories/S07-signal-data.md) | SignalData ScriptableObject + authoring | 🟦 | S01 |
| [S08](stories/S08-signal-evaluator.md) | SignalEvaluator (clarity math) | 🟦 | S07 |
| [S09](stories/S09-signal-manager.md) | SignalManager & LockEvaluator | 🟦 | S07, S08 |

**Exit criteria:** end-to-end loop works with placeholder visuals and logged outcomes. Screen is readable but ugly.

### M2 — Feel (Day 2 AM)

Goal: the CRT looks and sounds like a CRT. Tuning feels tactile.

| Story | Title | Priority | Depends on |
|-------|-------|----------|------------|
| [S10](stories/S10-crt-shader.md) | CRT Shader Graph | 🟦 | S04 |
| [S11](stories/S11-reveal-pipeline.md) | Reveal/distortion pipeline | 🟦 | S10, S09 |
| [S12](stories/S12-waveform-visual.md) | WaveformElement reactive visual | 🟦 | S06, S08 |
| [S13](stories/S13-postfx-ambient.md) | Post-FX + ambient flicker | 🟨 | S10 |
| [S14](stories/S14-audio-director.md) | AudioDirector with reactive mix | 🟦 | S06 |

**Exit criteria:** tuning visibly and audibly changes the screen. A clean signal looks clean; a noisy signal looks noisy.

### M3 — Polish (Day 2 PM)

Goal: the game has a beginning, a middle, and an end. It feels authored.

| Story | Title | Priority | Depends on |
|-------|-------|----------|------------|
| [S15](stories/S15-lock-feedback.md) | Lock feedback & transitions | 🟦 | S09, S10, S14 |
| [S16](stories/S16-intro-outro.md) | Intro / outro cards | 🟦 | S02 |
| [S17](stories/S17-playtest-mix.md) | Playtest & mix pass | 🟦 | all above |

**Exit criteria:** full playthrough with 3+ signals, start card, end card, no placeholder logs visible, mix balanced.

### M4 — Stretch

Only if ahead after M3.

| Story | Title | Priority |
|-------|-------|----------|
| [S18](stories/S18-archive-gallery.md) | Archive / gallery of found transmissions | 🟪 |
| [S19](stories/S19-sticky-note-clues.md) | Sticky-note clues for tuning targets | 🟪 |
| [S20](stories/S20-final-special-signal.md) | Final "special" signal with payoff | 🟪 |

## Dependency Graph (textual)

```
S01 ──┬── S03 ── S04 ── S10 ── S11
      ├── S07 ── S08 ── S09 ─────────── S15 ── S17
      └── S02 ── S05 ── S06 ── S12
                          └──── S14
S10 ── S13
S02 ── S16 ─────────────────────────────── S17
```

## Parallelization Hints for Multi-Agent Execution

These stories can run concurrently once their dependencies are met:

- After S01: **S02, S03, S07** can run in parallel.
- After S06: **S08, S12, S14** can run in parallel.
- After S09+S10: **S11, S13, S15** can run in parallel.
- **S16** can be authored any time after S02.

## Ship Gate Checklist (for S17)

- [ ] 3+ authored signals play in order
- [ ] All three controls respond with audible and visible feedback
- [ ] Lock button produces distinct success / partial / fail states
- [ ] Intro card appears at start; outro card appears after last signal
- [ ] No `Debug.Log` noise in console on a clean playthrough
- [ ] Build (Windows standalone) opens and is playable end-to-end
- [ ] README points to jam submission
