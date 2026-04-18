# S17 — Playtest & Mix Pass

**Milestone:** M3 — Polish
**Priority:** 🟦 MVP
**Depends on:** all prior MVP stories

## Goal

Take the game from "all systems wired" to "shippable." Playtest end-to-end, fix what feels wrong, balance difficulty and mix, produce a Windows build.

## Deliverables

1. **Playthrough checklist:** do three full runs and take notes. Specifically verify:
   - Intro → signal 1 → signal 2 → signal 3 → outro, no soft locks.
   - Each signal's target tuning is discoverable within ~30–60s of fiddling.
   - Lock thresholds produce believable distribution (rough target: 40% success, 30% partial, 30% fail on a casual first attempt).
2. **Difficulty tuning:** adjust `SignalData.innerTolerance` / `outerTolerance` per signal so the difficulty rises slightly across 1→3. Signal 3 can have narrower bands.
3. **Mix pass:**
   - Static bed caps at ~-20 dB (`volume ≤ 0.25`). Louder feels abusive.
   - Signal tone never exceeds static + 6 dB.
   - Stingers louder than beds by ~8 dB.
   - Overall build volume normalized so the CRT hum isn't inaudible on laptop speakers.
4. **Remove debug noise:**
   - Delete / disable `DebugSignalLogger` from S09.
   - Strip leftover `Debug.Log` calls in controllers.
5. **Final art pass** (coordinate with artist):
   - Replace any remaining placeholder sprites with real art if delivered.
   - Tag any slots still placeholder in `Assets/Art/README.md` with `// TODO` so the artist knows what's outstanding.
6. **Build a Windows standalone:** `File > Build Settings > PC, Mac & Linux Standalone > Windows > x86_64`. Output to `Builds/Windows/SignalScrubber.exe`. Add `Builds/` to `.gitignore` if not already ignored.
7. **README update:** short blurb, controls, screenshot, jam link.

## Acceptance Criteria

- [ ] Every ship-gate item in [ROADMAP.md](../ROADMAP.md) §Ship Gate Checklist is ticked.
- [ ] Build opens, plays, and exits cleanly on a Windows machine.
- [ ] Console is clean on a playthrough.
- [ ] README points at the build and jam submission.

## Out of Scope

- Stretch stories (S18–S20).

## Implementation Notes

- Time-box this pass. If balance is obviously wrong after 30 min of tuning, ship with "hard but fair" rather than continuing to iterate. Jam-graded games favor shipping over perfect.
