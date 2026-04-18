# S10 — CRT Shader Graph

**Milestone:** M2 — Feel
**Priority:** 🟦 MVP
**Depends on:** S04

## Goal

Replace the placeholder unlit material on the CRT screen quad with a Shader Graph that composes scanlines, barrel curvature, chromatic aberration, rolling distortion, ghosting, flicker, phosphor tint, and a reveal blend between noise and hidden image.

## Deliverables

1. `Assets/Shaders/CRT.shadergraph` — URP Unlit Shader Graph with these exposed properties (names and ranges must match ARCHITECTURE.md §Shader Architecture):

   | Property         | Type       | Default                      |
   |------------------|------------|------------------------------|
   | `_HiddenImage`   | Texture2D  | White                         |
   | `_NoiseTex`      | Texture2D  | White (assign a noise sprite) |
   | `_Reveal`        | Float      | 0.0                           |
   | `_NoiseStrength` | Float      | 1.0                           |
   | `_Chromatic`     | Float      | 0.5                           |
   | `_Rolling`       | Float      | 0.5                           |
   | `_Ghost`         | Float      | 0.3                           |
   | `_Scanlines`     | Float      | 240                           |
   | `_Curvature`     | Float      | 0.15                          |
   | `_Flicker`       | Float      | 0.2                           |
   | `_NoiseScroll`   | Vector2    | (0.3, 0.9)                    |
   | `_Tint`          | Color      | (0.49, 1.0, 0.62, 1.0)        |

2. Compose the graph in this order (see ARCHITECTURE.md):
   - Barrel-distort UVs using `_Curvature`
   - Split UVs per color channel by `_Chromatic` for R / G / B
   - Offset V by `sin(Time * 1.3 + UV.y * 6) * _Rolling * 0.02` (subtle rolling)
   - Sample `_HiddenImage` at distorted UVs
   - Sample `_NoiseTex` scrolled by `_Time * _NoiseScroll`
   - Lerp hidden ← noise by `saturate(1 - _Reveal) * _NoiseStrength`
   - Sample the hidden image a second time at `UV + (_Ghost * 0.01, 0)` and add at 0.4 opacity
   - Multiply by scanline factor `1 - step(frac(UV.y * _Scanlines), 0.5) * 0.35`
   - Multiply by flicker `1 - _Flicker * 0.1 * noise1d(Time)`
   - Multiply by `_Tint`

3. Create `Assets/Art/Fx/noise_tile.png` — a 256×256 seamless grayscale noise texture (temporary; use a Photoshop noise filter or a simple Perlin render). Assign to `_NoiseTex`.

4. Update `Assets/Materials/CRT.mat` to use the new shader. Leave runtime-driven properties at their defaults; S11 binds them.

## Acceptance Criteria

- [ ] The CRT screen visibly shows scanlines, slight curvature, chromatic fringing, rolling offset, and flicker with default parameters.
- [ ] Changing `_Reveal` in the material inspector smoothly transitions from "pure noise" at 0 to "clean hidden image" at 1.
- [ ] No shader errors in the console.

## Out of Scope

- Binding parameters from `TuningState` (S11).
- Waveform overlay (S12).

## Implementation Notes

- If Shader Graph's barrel distortion feels janky, a simple `uv += (uv - 0.5) * dot(uv-0.5, uv-0.5) * _Curvature` custom function works fine.
- Keep scanline thickness configurable via `_Scanlines` — artist may want denser/sparser lines.
