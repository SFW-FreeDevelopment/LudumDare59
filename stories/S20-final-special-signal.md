# S20 — Final "Special" Signal with Payoff (Stretch)

**Milestone:** M4 — Stretch
**Priority:** 🟪 Stretch
**Depends on:** S09, S11, S14

## Goal

Cap the run with a final signal that breaks the normal pattern — harder to tune, carries a longer narrative payload, and earns a distinct outro variant.

## Deliverables

1. `Signal_Final.asset` authored in `Assets/ScriptableObjects/Signals/`:
   - Tighter tolerances (e.g. `innerTolerance = 0.03`, `outerTolerance = 0.12`).
   - Custom `archiveNote` that reads as a narrative close.
   - Custom `tint` (e.g. amber instead of phosphor green) to signal "this one is different."
2. `SignalManager` flag: `[SerializeField] SignalData finalSignal;` appended after the regular list. Or just put it at the end of `signals[]` and rely on the index.
3. On the final signal start, trigger a one-off effect:
   - Fade down hum bed, swap static bed to a sparser/quieter variant.
   - Flicker the CRT power LED more erratically.
4. On successful lock of the final signal: play a longer archive card (5s dwell) with the final narrative beat before the standard outro. On partial or fail: outro card reads differently ("The signal slipped away.").

## Acceptance Criteria

- [ ] Final signal feels mechanically and tonally distinct.
- [ ] Outro card respects final outcome.
- [ ] No regression to the earlier 3 signals.

## Out of Scope

- Multiple endings based on cumulative run score — too much design debt.
- New UI for the final signal.

## Implementation Notes

- The "branching outro text" can be a simple `switch` on the final `LockOutcome` in `IntroOutroController`.
- If you add this story, update the Ship Gate Checklist to ensure the final signal is in the playtest loop.
