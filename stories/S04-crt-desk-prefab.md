# S04 — CRT + Desk Prefab Structure

**Milestone:** M1 — Playable Core
**Priority:** 🟦 MVP
**Depends on:** S03

## Goal

Convert the blocked-out CRT and desk into prefabs with well-named slots the sprite artist (wife) can drop art into without hunting through the hierarchy. The CRT screen quad becomes a real `MeshRenderer` ready for the CRT shader in S10.

## Deliverables

1. Create prefab `Assets/Prefabs/CRT.prefab` from `World/CRT`, containing:
   - `Body/FrameSprite` (SpriteRenderer, art slot)
   - `Body/PowerLed` (SpriteRenderer, small red dot placeholder)
   - `Screen/ScreenQuad` (MeshRenderer + MeshFilter using a built-in quad mesh, scaled to the inner bezel). Material: a new `Assets/Materials/CRT.mat` using URP/Unlit for now (S10 replaces with Shader Graph).
   - `Screen/DiegeticUIAnchor` (empty transform; S02's `UI/DiegeticUI` UIDocument will be re-parented here).
   - `Foreground/Glass` (SpriteRenderer, art slot, initially empty sprite).
2. Create prefab `Assets/Prefabs/Desk.prefab` from `World/Desk` with child slots:
   - `DeskSurface` (SpriteRenderer)
   - `Clutter/Mug`, `Clutter/Keyboard`, `Clutter/Papers`, `Clutter/Tapes`, `Clutter/StickyNotes`, `Clutter/Books` — all empty SpriteRenderers with descriptive names.
3. Re-parent the existing `UI/DiegeticUI` under `CRT/Screen/DiegeticUIAnchor` so the UIDocument visually sits on the monitor.
4. Update `Assets/Art/README.md`: list exactly which SpriteRenderer slots exist and what aspect ratio each expects (pixel dims as guidance, not hard limits).

## Acceptance Criteria

- [ ] Both prefabs exist and can be dropped into an empty scene to recreate the composition.
- [ ] Every SpriteRenderer slot has a clear name and null sprite that the artist can populate.
- [ ] Diegetic UIDocument renders aligned to the screen quad (does not need pixel-perfect alignment yet).

## Out of Scope

- CRT shader (S10).
- Final art — artist drops sprites in independently; this story just builds the slots.

## Implementation Notes

- Keep the `CRT.mat` material using `Universal Render Pipeline/Unlit` so the screen has a flat dark tone until S10 swaps the shader.
- Use `.prefab` variants if the artist wants to tweak per-asset; not needed for MVP.
