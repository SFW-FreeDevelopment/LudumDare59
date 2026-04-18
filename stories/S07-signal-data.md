# S07 — SignalData ScriptableObject + Authoring

**Milestone:** M1 — Playable Core
**Priority:** 🟦 MVP
**Depends on:** S01

## Goal

Define the data shape for an authored signal and create 3 placeholder `SignalData` assets so the rest of the pipeline has real inputs.

## Deliverables

1. `Assets/Scripts/Core/SignalData.cs`:
   ```csharp
   namespace SignalScrubber.Core {
     [CreateAssetMenu(menuName="SignalScrubber/Signal", fileName="Signal")]
     public sealed class SignalData : ScriptableObject {
       public string id;
       public Sprite hiddenImage;
       public AudioClip signalTone;
       [Range(0,1)] public float targetFrequency = 0.5f;
       [Range(0,1)] public float targetNoise = 0.5f;
       [Range(0,1)] public float targetPhase = 0.5f;
       [Range(0.01f, 0.2f)] public float innerTolerance = 0.05f;
       [Range(0.05f, 0.4f)] public float outerTolerance = 0.20f;
       public Color tint = new Color(0.49f, 1f, 0.62f); // phosphor green
       [TextArea] public string archiveNote;
     }
   }
   ```
2. Create 3 placeholder assets under `Assets/ScriptableObjects/Signals/`:
   - `Signal_01_Monolith.asset` — targets (freq=0.30, noise=0.70, phase=0.55)
   - `Signal_02_Diagram.asset` — targets (freq=0.65, noise=0.35, phase=0.25)
   - `Signal_03_Silhouette.asset` — targets (freq=0.85, noise=0.50, phase=0.75)
3. Assign placeholder hidden images: any three distinct sprites (e.g., Unity's built-in shapes tinted differently, or solid-color sprites). The artist replaces these later.
4. Leave `signalTone` null for now; `AudioDirector` will tolerate nulls (S14).
5. Append to `Assets/Art/README.md`: a table listing each signal id, its target values, and the expected sprite slot.

## Acceptance Criteria

- [ ] `Assets > Create > SignalScrubber > Signal` menu works and creates a new `SignalData`.
- [ ] Three `.asset` files exist with distinct target tunings.
- [ ] Inspector shows ranges clamped 0..1.

## Out of Scope

- Consuming these assets at runtime (S09).
- Real illustrated hidden images (drop-in later).

## Implementation Notes

- The `id` field is for logging/archive display; filename already encodes order.
- Keep `tint` phosphor green by default; vary per signal only if the look demands it.
