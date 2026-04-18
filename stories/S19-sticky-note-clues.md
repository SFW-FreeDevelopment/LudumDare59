# S19 — Sticky-Note Clues (Stretch)

**Milestone:** M4 — Stretch
**Priority:** 🟪 Stretch
**Depends on:** S04, S09

## Goal

Add desk sticky notes with partial hints about target tuning values ("TRY LOWER FREQ", "MORE NOISE", "SHIFT PHASE"), making the puzzle solvable through observation rather than brute-force tuning.

## Deliverables

1. `Assets/Scripts/Polish/StickyNoteClueSystem.cs`:
   - Holds `SpriteRenderer[] stickyNotes;` and `Sprite[] hintSprites;` plus a mapping strategy: on `OnSignalStarted`, pick 2–3 hint sprites whose content roughly describes the current signal's target (e.g. if `targetFrequency < 0.4`, show "LOWER FREQ").
2. 6–12 sticky-note sprites drawn by the artist (list of phrases in `Assets/Art/README.md` for artist handoff):
   - `LOWER FREQ`, `HIGHER FREQ`
   - `MORE NOISE`, `LESS NOISE`
   - `SHIFT PHASE ←`, `SHIFT PHASE →`
   - `ALMOST THERE`
   - `DON'T LOCK IT YET`
   - `IT'S CLOSER THAN YOU THINK`
   - `SIGNAL WORK. SLEEP. REPEAT.`
3. Sticky note slots already exist on the desk prefab (S04). This story just wires sprite swapping per signal.
4. Hover interaction: mousing over a sticky note scales it up 1.1× and brings it to front. Use a small `MonoBehaviour` with `OnMouseEnter/Exit` and a `SortingOrder` swap.

## Acceptance Criteria

- [ ] Each signal shows a plausible set of 2–3 hint notes.
- [ ] Hovering a note enlarges it readably.
- [ ] Hints change when the signal changes.

## Out of Scope

- Notes that contradict each other (intentional red herrings) — could be fun but adds design debt.
- Handwriting font rendering — sticky note art is raster sprites.

## Implementation Notes

- The hint-selection rule can be dead simple (if/else per axis). Don't build a generalized cue engine.
