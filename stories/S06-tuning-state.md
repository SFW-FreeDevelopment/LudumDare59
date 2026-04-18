# S06 — TuningState & Control Bindings

**Milestone:** M1 — Playable Core
**Priority:** 🟦 MVP
**Depends on:** S05

## Goal

Turn the logging control hookup from S05 into a single source of truth (`TuningState`) that downstream systems (evaluator, renderer, audio) subscribe to.

## Deliverables

1. `Assets/Scripts/Core/TuningState.cs`:
   ```csharp
   namespace SignalScrubber.Core {
     public sealed class TuningState : MonoBehaviour {
       public float Frequency { get; private set; }
       public float Noise { get; private set; }
       public float Phase { get; private set; }
       public event System.Action<TuningState> OnChanged;

       public void SetFrequency(float v) { Frequency = v; Raise(); }
       public void SetNoise(float v)     { Noise = v;     Raise(); }
       public void SetPhase(float v)     { Phase = v;     Raise(); }
       void Raise() => OnChanged?.Invoke(this);
     }
   }
   ```
2. Add a `TuningState` component to a `Systems/TuningState` GameObject in the scene.
3. Update `CrtFrameController` from S05:
   - Remove the `Debug.Log` calls.
   - Hold a `[SerializeField] TuningState tuning;` reference.
   - Wire slider + knob `RegisterValueChangedCallback<float>(...)` to `tuning.SetFrequency` / `SetNoise` / `SetPhase`.
   - Wire Lock button click to emit a separate event `public event Action OnLockPressed;`.
4. Bootstrap initial values: in `CrtFrameController.OnEnable`, call the setters once with current UI values so downstream systems have non-zero data.

## Acceptance Criteria

- [ ] Dragging any control fires `TuningState.OnChanged` with updated values (verify by attaching a temporary subscriber that logs).
- [ ] `OnLockPressed` fires once per click.
- [ ] No per-frame polling: `Update()` is not used for input in `CrtFrameController`.

## Out of Scope

- SignalEvaluator consumption (S08).
- SFX on change (S14).

## Implementation Notes

- Do not make `TuningState` a ScriptableObject — scene-scoped state is simpler and avoids domain-reload pitfalls.
- Don't bother with clamping; slider/knob already clamp 0..1.
