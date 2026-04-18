# S13 — Post-FX Tune + Ambient Flicker

**Milestone:** M2 — Feel
**Priority:** 🟨 Polish (highly recommended)
**Depends on:** S10

## Goal

Tune the global post-process volume from S03's default values into a mood that matches the reference art, and add subtle environment flicker (LED blinks, light jitter, dust particles) so the scene feels alive even when the player is idle.

## Deliverables

1. Dial in `Assets/Settings/PostFx.asset`:
   - **Bloom** — intensity 0.8, threshold 0.9, scatter 0.7
   - **Vignette** — intensity 0.45, smoothness 0.4, rounded off
   - **Film Grain** — type Thin1, intensity 0.25
   - **Color Adjustments** — post-exposure 0, contrast +10, saturation -15, slight warm filter
   - **Tonemapping** — Neutral

2. `Assets/Scripts/Polish/AmbientFlicker.cs`:
   ```csharp
   namespace SignalScrubber.Polish {
     public sealed class AmbientFlicker : MonoBehaviour {
       [SerializeField] Rendering.CrtMaterialBinder binder;
       [SerializeField] SpriteRenderer powerLed;
       [SerializeField] float ledBaseAlpha = 0.9f;

       void Update() {
         float f = 0.1f + 0.1f * Mathf.PerlinNoise(Time.time * 3f, 0f);
         binder.SetFlicker(f);
         if (powerLed) {
           var c = powerLed.color;
           c.a = ledBaseAlpha + (Mathf.PerlinNoise(Time.time * 5f, 1f) - 0.5f) * 0.1f;
           powerLed.color = c;
         }
       }
     }
   }
   ```

3. Add `AmbientFlicker` to `Systems/AmbientFlicker` and wire the CRT binder and `PowerLed` sprite renderer.

4. Optional: a `ParticleSystem` as a child of `World/Foreground` emitting slow white dust motes at very low alpha. Skip if it doesn't land on first try.

## Acceptance Criteria

- [ ] Scene has a distinctly moodier look than the default post-process.
- [ ] Power LED visibly breathes.
- [ ] CRT flicker is present but not seizure-inducing.

## Out of Scope

- Per-signal post-fx variation.
- Audio-reactive flicker (S14 can feed into this later if there's time).

## Implementation Notes

- Keep PerlinNoise-driven flicker to ≤0.1 amplitude — any more reads as "broken monitor" rather than "old CRT."
