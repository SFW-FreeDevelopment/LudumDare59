# S08 — SignalEvaluator (Clarity Math)

**Milestone:** M1 — Playable Core
**Priority:** 🟦 MVP
**Depends on:** S07

## Goal

Implement the pure clarity function that converts `TuningState + SignalData` into a single `clarity` float in `[0, 1]`. Used by every reactive system (renderer, audio, waveform, lock evaluator).

## Deliverables

1. `Assets/Scripts/Core/SignalEvaluator.cs`:
   ```csharp
   namespace SignalScrubber.Core {
     public static class SignalEvaluator {
       public static float Clarity(float value, float target, float inner, float outer) {
         float d = Mathf.Abs(value - target);
         if (d <= inner) return 1f;
         if (d >= outer) return 0f;
         return 1f - (d - inner) / (outer - inner);
       }

       public static float Clarity(TuningState t, SignalData s) {
         float f = Clarity(t.Frequency, s.targetFrequency, s.innerTolerance, s.outerTolerance);
         float n = Clarity(t.Noise,     s.targetNoise,     s.innerTolerance, s.outerTolerance);
         float p = Clarity(t.Phase,     s.targetPhase,     s.innerTolerance, s.outerTolerance);
         return (f + n + p) / 3f;
       }
     }
   }
   ```
2. (Optional, only if time allows) `Assets/Tests/EditMode/SignalEvaluatorTests.cs` with 3–5 cases:
   - Exact match → 1.0
   - Beyond outer tolerance → 0.0
   - Midpoint of inner/outer band → ~0.5
   - Each axis contributes equally

## Acceptance Criteria

- [ ] Class compiles and is callable from anywhere.
- [ ] Exact-match tuning returns 1.0.
- [ ] Far-off tuning returns 0.0.
- [ ] (If added) EditMode tests pass.

## Out of Scope

- UI display of clarity (S12 waveform, S11 shader binding).
- Lock decision (S09).

## Implementation Notes

- Keep the class `static` and pure. No Unity API calls besides `Mathf`.
- Don't over-tune the band curve; a linear falloff feels honest. A smoothstep can be tried in S17 polish.
