# S12 — WaveformElement Reactive Visual

**Milestone:** M2 — Feel
**Priority:** 🟦 MVP
**Depends on:** S06, S08

## Goal

A visible waveform line inside the CRT frame that smooths toward a clean sine as clarity rises and jitters chaotically when clarity is low. Sells the "I'm tuning into something" fantasy.

## Deliverables

1. `Assets/Scripts/UI/WaveformElement.cs`:
   ```csharp
   namespace SignalScrubber.UI {
     [UxmlElement]
     public partial class WaveformElement : VisualElement {
       public float Clarity { get; set; } = 0f;
       const int Samples = 128;
       readonly float[] _buf = new float[Samples];
       float _phase;

       public WaveformElement() {
         generateVisualContent += OnGenerate;
       }

       public void Tick(float dt) {
         _phase += dt * Mathf.Lerp(2f, 8f, Clarity);
         for (int i = 0; i < Samples; i++) {
           float t = (float)i / (Samples - 1);
           float clean = Mathf.Sin((_phase + t * 6.283f) * 2f) * 0.4f;
           float noise = (UnityEngine.Random.value - 0.5f) * Mathf.Lerp(1.2f, 0.1f, Clarity);
           _buf[i] = Mathf.Lerp(noise, clean, Clarity);
         }
         MarkDirtyRepaint();
       }

       void OnGenerate(MeshGenerationContext ctx) {
         var p = ctx.painter2D;
         p.strokeColor = new Color(0.49f, 1f, 0.62f);
         p.lineWidth = 2f;
         p.BeginPath();
         var r = contentRect;
         for (int i = 0; i < Samples; i++) {
           float x = r.xMin + (float)i / (Samples - 1) * r.width;
           float y = r.center.y + _buf[i] * r.height * 0.5f;
           if (i == 0) p.MoveTo(new Vector2(x, y));
           else        p.LineTo(new Vector2(x, y));
         }
         p.Stroke();
       }
     }
   }
   ```

2. Replace the placeholder `<ui:VisualElement name="waveform" class="waveform" />` in `CrtFrame.uxml` with `<ss:WaveformElement name="waveform" class="waveform" />`.

3. Add `.waveform { height: 60px; width: 70%; align-self: center; }` to `crt.uss`.

4. `Assets/Scripts/UI/WaveformDriver.cs` — a MonoBehaviour that:
   - Holds refs to `TuningState`, `SignalManager`, and the `UIDocument`.
   - Finds the waveform element in `OnEnable` via `document.rootVisualElement.Q<WaveformElement>("waveform")`.
   - In `Update`, calls `waveform.Clarity = SignalEvaluator.Clarity(tuning, manager.Current);` then `waveform.Tick(Time.deltaTime);`.

## Acceptance Criteria

- [ ] Waveform is visible inside the CRT.
- [ ] Bad tuning → spiky chaotic jitter.
- [ ] Good tuning → smooth sine at higher rate.
- [ ] No GC spikes per frame (buffer is reused).

## Out of Scope

- Matching waveform color to `SignalData.tint` (nice-to-have for S17).

## Implementation Notes

- `UnityEngine.Random.value` per frame is fine at 128 samples; don't over-engineer a noise generator.
- If `[UxmlElement]` source-gen isn't working, fall back to `UxmlFactory<WaveformElement, UxmlTraits>`.
