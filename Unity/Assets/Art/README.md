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

| Story | GameObject path                         | Slot                     | Notes |
|-------|-----------------------------------------|--------------------------|-------|
| _TBD_ | _added as stories create slots_         |                          |       |
