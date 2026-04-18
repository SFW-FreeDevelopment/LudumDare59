# S11 — Reveal / Distortion Pipeline

**Milestone:** M2 — Feel
**Priority:** 🟦 MVP
**Depends on:** S10, S09

## Goal

Bind the CRT shader's runtime parameters to the live clarity score so tuning actually changes what's on screen. Also hot-swap `_HiddenImage` and `_Tint` when a new signal starts.

## Deliverables

1. `Assets/Scripts/Rendering/CrtMaterialBinder.cs`:
   ```csharp
   namespace SignalScrubber.Rendering {
     public sealed class CrtMaterialBinder : MonoBehaviour {
       [SerializeField] MeshRenderer target;
       Material _mat;
       static readonly int Reveal       = Shader.PropertyToID("_Reveal");
       static readonly int NoiseStr     = Shader.PropertyToID("_NoiseStrength");
       static readonly int Chromatic    = Shader.PropertyToID("_Chromatic");
       static readonly int Rolling      = Shader.PropertyToID("_Rolling");
       static readonly int Ghost        = Shader.PropertyToID("_Ghost");
       static readonly int Flicker      = Shader.PropertyToID("_Flicker");
       static readonly int Tint         = Shader.PropertyToID("_Tint");
       static readonly int HiddenImage  = Shader.PropertyToID("_HiddenImage");

       void Awake() => _mat = target.material; // instance, not sharedMaterial

       public void SetHiddenImage(Texture tex)   => _mat.SetTexture(HiddenImage, tex);
       public void SetTint(Color c)              => _mat.SetColor(Tint, c);
       public void SetClarity(float clarity) {
         float inv = 1f - clarity;
         _mat.SetFloat(Reveal, clarity);
         _mat.SetFloat(NoiseStr, Mathf.Lerp(0.2f, 1.0f, inv));
         _mat.SetFloat(Chromatic, Mathf.Lerp(0.05f, 0.6f, inv));
         _mat.SetFloat(Rolling, Mathf.Lerp(0.05f, 0.9f, inv));
         _mat.SetFloat(Ghost, Mathf.Lerp(0.05f, 0.7f, inv));
       }
       public void SetFlicker(float f) => _mat.SetFloat(Flicker, f);
     }
   }
   ```

2. `Assets/Scripts/Rendering/SignalRenderer.cs`:
   ```csharp
   namespace SignalScrubber.Rendering {
     public sealed class SignalRenderer : MonoBehaviour {
       [SerializeField] Core.TuningState tuning;
       [SerializeField] Core.SignalManager manager;
       [SerializeField] CrtMaterialBinder binder;

       Core.SignalData _current;

       void OnEnable() {
         tuning.OnChanged += _ => Refresh();
         manager.OnSignalStarted += OnStart;
       }
       void OnDisable() {
         tuning.OnChanged -= _ => Refresh();
         manager.OnSignalStarted -= OnStart;
       }

       void OnStart(Core.SignalData s) {
         _current = s;
         binder.SetHiddenImage(s.hiddenImage ? s.hiddenImage.texture : null);
         binder.SetTint(s.tint);
         Refresh();
       }

       void Refresh() {
         if (_current == null) return;
         float c = Core.SignalEvaluator.Clarity(tuning, _current);
         binder.SetClarity(c);
       }
     }
   }
   ```

3. Add `CrtMaterialBinder` to `CRT/Screen/ScreenQuad`. Add `SignalRenderer` to `Systems/SignalRenderer` and wire references.

4. Fix the `OnChanged -= _ => Refresh();` unsubscribe correctness — the lambdas won't match. Use a named method:
   ```csharp
   void HandleChanged(Core.TuningState _) => Refresh();
   void OnEnable() { tuning.OnChanged += HandleChanged; manager.OnSignalStarted += OnStart; }
   void OnDisable() { tuning.OnChanged -= HandleChanged; manager.OnSignalStarted -= OnStart; }
   ```
   (Treat the lambda form in step 2 as pseudocode.)

## Acceptance Criteria

- [ ] Dragging controls visibly cleans or dirties the CRT in real time.
- [ ] Advancing to signal 2 / 3 swaps the hidden image and tint.
- [ ] No per-frame material allocations (verify via Profiler — `.material` is cached once in `Awake`).

## Out of Scope

- Waveform (S12).
- Lock flash (S15).

## Implementation Notes

- `Sprite.texture` returns the atlas if sprite packing is enabled. For MVP, disable sprite packing on signal images or assign each signal sprite to its own texture. Alternatively, render the sprite into a `RenderTexture` — deferred; only do this if the atlas texture looks wrong.
