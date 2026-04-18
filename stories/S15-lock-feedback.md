# S15 — Lock Feedback & Transitions

**Milestone:** M3 — Polish
**Priority:** 🟦 MVP
**Depends on:** S09, S10, S14

## Goal

Make the Lock Signal press feel decisive: a visual flash, a screen-wide reaction keyed to the outcome, an optional archive-text overlay, and a short transition to the next signal.

## Deliverables

1. `Assets/Scripts/Polish/LockFlash.cs` — a MonoBehaviour that:
   - Subscribes to `SignalManager.OnSignalLocked`.
   - Runs a coroutine that spikes `CrtMaterialBinder.SetFlicker(1f)` for ~0.15s on Success, longer for Partial, and plays a "collapse to noise" ramp on Fail.
   - For Success: drives `_Reveal` to 1 and holds for 1.5s while a UXML element in `Overlay.uxml` fades in a `<Label>` with `SignalData.archiveNote`, then fades out before `OnSignalStarted` fires for the next signal.
   - For Partial: hold ~1s, archive note displayed but dimmer.
   - For Fail: ramp `_Reveal` to 0 and `_NoiseStrength` to max for 1s.

2. Add to `Overlay.uxml`:
   ```xml
   <ui:VisualElement name="archive-card" class="archive-card" style="opacity: 0">
     <ui:Label name="archive-title" class="archive-title" text="TRANSMISSION ARCHIVED" />
     <ui:Label name="archive-body" class="archive-body" />
   </ui:VisualElement>
   ```
   and matching styles in `overlay.uss` (centered, phosphor green, ~600px wide).

3. Inter-signal transition:
   - `SignalManager` currently calls `StartSignal(_index + 1)` inline in `HandleLock`. Refactor: expose a public `AdvanceAfter(float seconds)` that `LockFlash` calls when its coroutine completes. Remove the inline advance from `HandleLock`.

4. Reset tuning controls between signals? **No** — leave them where the player had them. Makes subsequent signals feel harder on purpose.

## Acceptance Criteria

- [ ] Success: screen "locks in" with a brief flash, archive text appears for ~1.5s, next signal starts.
- [ ] Partial: similar but dimmer, shorter dwell.
- [ ] Fail: screen collapses into noise, no archive text, next signal still starts.
- [ ] No overlapping coroutines if Lock is spammed — `LockFlash` should ignore new events while mid-transition, OR disable the Lock button while transitioning.

## Out of Scope

- Permanent archive gallery (S18 stretch).
- Retry loop on fail.

## Implementation Notes

- Disable the Lock button during transitions via `button.SetEnabled(false)` and re-enable in `OnSignalStarted`.
- Use `UnityEngine.UIElements.Experimental.ValueAnimation<float>` for fades, or a simple coroutine — either is fine.
