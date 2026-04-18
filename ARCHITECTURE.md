# Architecture — Signal Scrubber

This document describes how the Unity build is wired together. It is the contract between agents/stories; if a story changes the architecture, update this file in the same PR.

## Target Stack

- **Unity** (project in `Unity/`)
- **URP 17.3** (installed)
- **Shader Graph** (ships with URP) — primary authoring tool for CRT effects
- **UI Toolkit** (`com.unity.modules.uielements`) — all player-facing controls and HUD
- **Input System** (`com.unity.inputsystem`) — pointer input; no keyboard/gamepad required for MVP
- **2D Sprite / Aseprite / PSD importers** (installed) — wife's sprite art pipeline
- **No third-party packages** unless a story explicitly adds one

## High-Level Data Flow

```
 ┌──────────────┐    UIEvents    ┌──────────────┐   TuningState   ┌─────────────────┐
 │  UIDocument  │ ─────────────▶ │ TuningState  │ ──────────────▶ │ SignalEvaluator │
 │ (CRT frame)  │                │ (ScriptableO │                 │   (pure calc)   │
 └──────────────┘                │  bject /     │                 └────────┬────────┘
        ▲                        │  runtime)    │                          │ clarity (0..1)
        │ visual state           └──────┬───────┘                          ▼
        │                               │                         ┌────────────────┐
        │                               ▼                         │ SignalRenderer │
        │                       ┌──────────────┐                  │  (materials +  │
        │                       │ AudioDirector│                  │   waveform)    │
        │                       └──────────────┘                  └────────────────┘
        │                                                                  │
        │                                                                  ▼
        │                                                         Shader parameters
        │                                                         on CRT screen quad
        │
        └───────────── SignalManager (progression) ◀──── LockEvaluator (success/partial/fail)
```

- **TuningState** is the single source of truth for the three control values (`frequency`, `noise`, `phase`) and exposes a C# event `OnChanged`.
- **Everything reactive listens to `TuningState.OnChanged`.** No polling in `Update()`.
- **SignalManager** owns the authored signal list, current index, and dispatches lock outcomes.

## Scene Graph

Single scene: `Assets/Scenes/Main.unity`.

```
Main
├── Camera (Orthographic, fixed)
├── GlobalVolume (post-process)
├── Lighting
├── World
│   ├── Background       (back wall, shelves, ambient equipment — sprites)
│   ├── Desk             (desk surface, clutter slots)
│   └── CRT
│       ├── Body         (frame sprite, LED)
│       ├── Screen       (MeshRenderer quad with CRT material)
│       └── Foreground   (glass, dust overlays)
├── UI
│   ├── DiegeticUI       (UIDocument — PanelSettings: world-space on CRT)
│   └── OverlayUI        (UIDocument — PanelSettings: screen-space for intro/outro)
└── Systems
    ├── SignalManager
    ├── TuningState
    ├── SignalEvaluator
    ├── LockEvaluator
    ├── SignalRenderer
    └── AudioDirector
```

## UI Toolkit Layering

Two `PanelSettings` assets, two `UIDocument` components:

### DiegeticUI (`Assets/UI/Diegetic.asset`)
- **Render Mode:** World Space, attached to the CRT GameObject.
- **Scale Mode:** Constant Pixel Size, tuned so UXML pixels map 1:1 to the CRT bezel art.
- Contains: the CRT frame labels ("TUNE THE SIGNAL", "FREQUENCY", etc.), the three controls (frequency slider, noise knob, phase knob), the Lock Signal button, the small waveform/status readouts. These all **live on the monitor frame**, not floating in front of the camera.

### OverlayUI (`Assets/UI/Overlay.asset`)
- **Render Mode:** Screen Space Overlay.
- Contains: intro title card, outro card, final fade/vignette transitions, any debug readouts.

Stylesheets live in `Assets/UI/Styles/`:
- `theme.uss` — fonts, colors (phosphor green, warm amber, dim cream), shared element classes
- `crt.uss` — diegetic CRT frame styles
- `overlay.uss` — full-screen overlay styles

UXML files live in `Assets/UI/Documents/`:
- `CrtFrame.uxml`
- `Overlay.uxml`

### Custom UI Toolkit Controls

- **`KnobElement : VisualElement`** — a custom control with `value` (0..1), `angleRange` (e.g. 270°), and drag-to-rotate via `PointerMoveEvent`. Emits `ChangeEvent<float>`. Rotates an inner image via `style.rotate`. Declared with `UxmlElementAttribute` so it can be placed in UXML.
- **`WaveformElement : VisualElement`** — overrides `generateVisualContent` to draw a `Painter2D` line that smooths with clarity.

## Shader Architecture

All CRT effects live in **one Shader Graph** (`Assets/Shaders/CRT.shadergraph`) driving a material on the screen quad. The shader composes layers in this order:

1. Sample `_HiddenImage` (current signal art Texture2D or RT).
2. Sample `_NoiseTex` scrolled by `_Time` and `_NoiseScroll`.
3. Blend hidden ← noise by `1 - _Reveal`.
4. Apply **horizontal chromatic separation** scaled by `_Chromatic`.
5. Apply **rolling vertical offset** (sine of time + `_Rolling`).
6. Apply **ghost/double-image** offset scaled by `_Ghost`.
7. Apply **scanlines** (modulate by `sin(uv.y * _Scanlines)`).
8. Apply **barrel distortion** UVs with strength `_Curvature`.
9. Multiply by subtle **flicker** (`_Flicker` noise over time).
10. Output with phosphor tint `_Tint`.

Shader exposed properties:

| Property          | Range   | Driven by                              |
|-------------------|---------|----------------------------------------|
| `_HiddenImage`    | Texture | `SignalManager` per signal             |
| `_Reveal`         | 0..1    | `clarity` from `SignalEvaluator`       |
| `_NoiseStrength`  | 0..1    | `1 - clarity` (plus per-signal bias)   |
| `_Chromatic`      | 0..1    | `1 - clarity`                          |
| `_Rolling`        | 0..1    | `1 - clarity`                          |
| `_Ghost`          | 0..1    | `1 - clarity`                          |
| `_Scanlines`      | static  | constant per scene                     |
| `_Curvature`      | static  | constant per scene                     |
| `_Flicker`        | 0..1    | `AudioDirector`/ambient random         |
| `_Tint`           | Color   | per-signal (phosphor green default)    |

A thin MonoBehaviour `CrtMaterialBinder` on the screen quad owns the `Material` instance and exposes C# setters called by `SignalRenderer`.

## Post-Processing

A single `GlobalVolume` with a URP `VolumeProfile` at `Assets/Settings/PostFx.asset`:

- Bloom (mid threshold, soft knee)
- Vignette (dark, off-center)
- Film Grain (low)
- Color Adjustments (lift blacks, slight warm tint on desk)
- Chromatic Aberration **disabled** — done in the CRT shader instead, so it only affects the monitor.

## Audio Architecture

`AudioDirector` is a scene singleton that owns three `AudioSource`s (beds) and a pool of one-shot sources:

- **StaticBed** — continuous static; `volume = lerp(0.05, 0.9, 1 - clarity)`
- **HumBed** — always on; low-volume constant
- **SignalTone** — per-signal clip; `volume = clarity`, `pitch = lerp(0.9, 1.1, frequency)`
- **OneShots** — click (on discrete knob step), lock-success, lock-partial, lock-fail

Reactive updates happen on `TuningState.OnChanged`, not per-frame.

All clips live under `Assets/Audio/` with subfolders `Beds/`, `SFX/`, `Signals/`, `Music/`. User will supply real assets; placeholders are silent `AudioClip` stubs or royalty-free temp clips.

## Asset Pipeline

### Sprite art (wife)
- Drop into `Assets/Art/<category>/` per the folder map below.
- Import settings: **Sprite (2D and UI)**, pixels-per-unit **100** (uniform across project), filter mode **Point** for pixel art OR **Bilinear** if painted — decision pinned per asset in `Assets/Art/README.md`.
- Aseprite files go to `Assets/Art/Source/` (not referenced in scene) and their `.asset` imported sprite is what scene objects reference.

### Audio (Steven)
- Drop into `Assets/Audio/<category>/`. Naming: `bed_static_01.wav`, `sfx_lock_success.wav`, `signal_monolith_01.wav`, `music_intro.ogg`.
- `AudioDirector` resolves clips via serialized fields set in the scene, not `Resources.Load`.

### Folder Map

```
Assets/
├── Art/
│   ├── Source/         (.aseprite, .psd)
│   ├── CRT/            (monitor body, bezel, knob, slider parts, button, glass)
│   ├── Desk/           (desk, mug, keyboard, papers, books, tapes, sticky notes)
│   ├── Background/     (wall, shelves, ambient equipment)
│   ├── Signals/        (hidden transmission art, one sprite per signal)
│   └── Fx/             (noise textures, dust overlays)
├── Audio/
│   ├── Beds/
│   ├── SFX/
│   ├── Signals/
│   └── Music/
├── Scenes/
│   └── Main.unity
├── Scripts/
│   ├── Core/           (TuningState, SignalData, SignalManager, SignalEvaluator, LockEvaluator)
│   ├── Rendering/      (CrtMaterialBinder, SignalRenderer, WaveformElement)
│   ├── UI/             (KnobElement, UI controllers, PanelSettings binders)
│   ├── Audio/          (AudioDirector)
│   └── Polish/         (intro/outro, ambient flicker)
├── Shaders/
│   └── CRT.shadergraph
├── UI/
│   ├── Documents/      (.uxml)
│   ├── Styles/         (.uss)
│   └── *.asset
├── Settings/           (URP, PostFx, InputActions)
└── ScriptableObjects/
    └── Signals/        (SignalData assets)
```

## Naming & Style

- C# files match type names. One type per file except trivial DTOs.
- Assembly definitions: **not used** for MVP (jam speed). Revisit only if compile times hurt.
- Namespaces: `SignalScrubber.Core`, `SignalScrubber.Rendering`, `SignalScrubber.UI`, `SignalScrubber.Audio`.
- No DI framework. Scene wiring via inspector references and a single `SignalManager` that finds its neighbors in `Awake()` via `[SerializeField]` fields.

## Testing

- No PlayMode tests for MVP (jam scope).
- EditMode tests **optional** for `SignalEvaluator` (pure function, cheap to cover). Story S08 ships a small test file if time allows.

## Non-Goals / Explicit "Don'ts"

- No uGUI (Canvas) for gameplay UI. Overlay-only fallback is allowed for intro/outro if a UI Toolkit limitation blocks it; flag in the story.
- No multi-scene additive loading.
- No save system. Session state lives in memory.
- No localization. English only.
- No analytics, telemetry, or network calls.
