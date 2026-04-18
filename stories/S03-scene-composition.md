# S03 — Main Scene Composition & Camera

**Milestone:** M1 — Playable Core
**Priority:** 🟦 MVP
**Depends on:** S01

## Goal

Block out the final composition: fixed orthographic camera, layered world GameObjects, post-process volume. Placeholder rectangles stand in for art.

## Deliverables

1. In `Main.unity`, configure `Main Camera`:
   - Projection: Orthographic
   - Orthographic Size: 5.4 (16:9 reference)
   - Position: `(0, 0, -10)`
   - Background: solid dark color `#05070A`
   - Post Processing: enabled
2. Scene root GameObjects matching ARCHITECTURE.md §Scene Graph:
   - `World/Background`, `World/Desk`, `World/CRT` (with `Body`, `Screen`, `Foreground` children)
   - `UI/DiegeticUI`, `UI/OverlayUI` (already created in S02, parent them under `UI`)
   - `Systems` (empty container for later managers)
3. Placeholder sprites:
   - `World/Background` — one dark gray sprite covering the camera
   - `World/Desk` — brown rectangle along the bottom third
   - `World/CRT/Body` — dark rounded rectangle centered
   - `World/CRT/Screen` — slightly smaller dark-green rectangle inside the body
4. A `GlobalVolume` GameObject with a `Volume` component referencing a new profile at `Assets/Settings/PostFx.asset`. Add Bloom, Vignette, Film Grain with conservative values (see ARCHITECTURE.md §Post-Processing).
5. 2D lighting: either a single `Global Light 2D` set to slightly warm, or leave the sprites unlit. Pick the cheaper path.

## Acceptance Criteria

- [ ] Scene in Game view shows the placeholder composition: monitor centered, desk below, dark background.
- [ ] Post-process volume is active and visibly bloom-y on the "screen" rectangle.
- [ ] No missing references or console errors.

## Out of Scope

- Real art (comes via S04 + external sprite drops).
- Controls (S05).

## Implementation Notes

- Use `SpriteRenderer` with default sprites (`Knob`, `Square`) for placeholders — zero art dependency.
- Keep Z values: Background `z=2`, Desk `z=1`, CRT.Body `z=0`, CRT.Screen `z=-0.1`, CRT.Foreground `z=-0.2`.
