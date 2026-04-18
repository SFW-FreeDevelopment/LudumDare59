# S18 — Archive / Gallery of Found Transmissions (Stretch)

**Milestone:** M4 — Stretch
**Priority:** 🟪 Stretch
**Depends on:** S15, S16

## Goal

After the outro, show a gallery of the transmissions the player successfully (or partially) decoded, with their archive notes. Rewards attentive play and adds narrative weight.

## Deliverables

1. `Assets/Scripts/Polish/ArchiveLog.cs`:
   - Subscribes to `SignalManager.OnSignalLocked` and stores `(SignalData, LockOutcome, float clarity)` tuples in a `List<Entry>`.
   - Exposes a `IReadOnlyList<Entry> Entries` property.
2. Extend `Overlay.uxml` with an `archive-gallery` container populated when `OnRunCompleted` fires. Each entry renders the signal's `hiddenImage` as a small thumbnail, its `id`, outcome badge (Success / Partial / Fail), and `archiveNote` (greyed if Fail).
3. Display gallery after the outro card (e.g. outro appears for 2s, then fades into the gallery).
4. USS additions: `.archive-gallery` (flex wrap), `.archive-entry` (thumbnail + text column), outcome badge colors (green / amber / grey).

## Acceptance Criteria

- [ ] Playing through to the end shows a gallery of all attempted signals.
- [ ] Outcome badges match what happened in the run.
- [ ] Thumbnails render even for fail outcomes (just dimmer).

## Out of Scope

- Persisting the archive across sessions (no save system).
- Clicking entries for detail view.

## Implementation Notes

- Do not re-use the in-game CRT shader for thumbnails — just display the raw sprite. The distortion look isn't needed here and the shader expects signal-state inputs.
