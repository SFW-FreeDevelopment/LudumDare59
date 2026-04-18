# Art Assets — Signal Scrubber

Sprite art drops into subfolders here. Source files (.aseprite, .psd) live in `Source/`; exported sprites live in their categorical folder and are the ones referenced by scene objects.

## Import conventions

- **Pixels per unit:** 100 (uniform across the project). Override only with a comment in this file explaining why.
- **Filter mode:**
  - Pixel art → **Point (no filter)**
  - Painted / high-res → **Bilinear**
- **Compression:** None for hand-drawn signal art and CRT frame art; default for background clutter.
- **Max size:** 2048 unless the asset is genuinely large (e.g. desk-wide background).
- **Sprite mode:** Single unless a sheet is obviously needed (e.g. animated flicker frames).

## Folders

| Folder         | Contents                                                         |
|----------------|------------------------------------------------------------------|
| `Source/`      | `.aseprite`, `.psd`, layered sources — not referenced in scene   |
| `CRT/`         | Monitor body, bezel, knob, slider parts, button, glass overlay   |
| `Desk/`        | Desk surface, mug, keyboard, papers, books, tapes, sticky notes  |
| `Background/`  | Back wall, shelves, ambient equipment                            |
| `Signals/`     | Hidden transmission art — one sprite per authored signal         |
| `Fx/`          | Noise textures, dust overlays                                    |

## Slot list

Populated as stories land. Each row names a `SpriteRenderer` slot created by a story; the artist can drop the sprite into that slot.

| Story | Prefab | GameObject path                       | Slot type      | Notes |
|-------|--------|---------------------------------------|----------------|-------|
| S04   | CRT    | `Body/FrameSprite`                    | SpriteRenderer | Full monitor shell, ~800×520 px |
| S04   | CRT    | `Body/PowerLed`                       | SpriteRenderer | Small red dot, ~24×24 px, pre-tinted |
| S04   | CRT    | `Foreground/Glass`                    | SpriteRenderer | Glass reflection/curvature overlay |
| S04   | Desk   | `DeskSurface`                         | SpriteRenderer | Wide desk top, ~2000×400 px |
| S04   | Desk   | `Clutter/Mug`                         | SpriteRenderer | Coffee mug |
| S04   | Desk   | `Clutter/Keyboard`                    | SpriteRenderer | Retro keyboard |
| S04   | Desk   | `Clutter/Papers`                      | SpriteRenderer | Loose scribbled notes |
| S04   | Desk   | `Clutter/Tapes`                       | SpriteRenderer | VHS / cassette stack |
| S04   | CRT    | `Body/Notes/StickyNote1`              | SpriteRenderer | Auto-assigned from `Art/CRT/sticky-note-1.png` — taped to monitor |
| S04   | CRT    | `Body/Notes/StickyNote2`              | SpriteRenderer | Auto-assigned from `Art/CRT/sticky-note-2.png` — taped to monitor |
| S04   | Desk   | `Clutter/Books`                       | SpriteRenderer | Manuals / technical books |

### Signals (S07)

One sprite per authored signal, dropped into the `hiddenImage` slot of the matching `SignalData` asset at `Assets/ScriptableObjects/Signals/`.

| Asset                              | id             | Target (f, n, p)      | Suggested imagery |
|------------------------------------|----------------|-----------------------|-------------------|
| `Signal_01_Monolith.asset`         | `monolith_01`  | (0.30, 0.70, 0.55)    | Black obelisk against starfield |
| `Signal_02_Diagram.asset`          | `diagram_02`   | (0.65, 0.35, 0.25)    | Annotated alien schematic or orbital map |
| `Signal_03_Silhouette.asset`       | `silhouette_03`| (0.85, 0.50, 0.75)    | Tall figure at a treeline, uncanny proportions |

### Drop-in workflow

1. Export the sprite into the matching `Assets/Art/<category>/` folder.
2. Select the prefab (`Assets/Prefabs/CRT.prefab` or `Desk.prefab`).
3. Drag the sprite onto the named `SpriteRenderer` slot's `Sprite` field.
4. For clutter pieces, tweak the slot's local position and scale to taste — those transforms are placeholders.
