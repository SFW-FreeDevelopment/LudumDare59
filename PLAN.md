# Signal Scrubber — Unity Implementation Plan

A weekend-jam build plan. Optimized for finishing a polished, atmospheric game — not reusable architecture.

---

## 1. High Concept Recap

The player sits at a retro workstation and tunes unstable transmissions on a CRT monitor by adjusting analog controls (frequency, noise filter, phase). Correct tuning reveals creepy sci-fi imagery and audio. Pressing **Lock Signal** evaluates closeness to hidden target values.

Tone: creepy sci-fi, analog retro-futurist, uncanny. Not horror, not action.

---

## 2. Scene Breakdown

**One scene: `Main.unity`.**

Fixed camera facing the workstation. Single composition.

### Layered structure (back to front)

1. **Background environment** — back wall, shelves, faint equipment, dim lighting.
2. **Desk surface** — mug, keyboard, papers, manuals, tapes, sticky notes.
3. **CRT monitor body** — frame, bezel, red power LED, knobs/sliders/button as child objects.
4. **CRT screen content** (inside the bezel):
   - Hidden transmission art layer (sprite or RenderTexture)
   - Distortion / noise / scanline shader pass
   - Waveform + frame-UI text overlays ("TUNE THE SIGNAL", "LOCK SIGNAL", "FREQUENCY" label)
5. **CRT glass** — curvature, bloom, reflection highlights.
6. **Foreground props** — cables, foreground dust/flicker, vignette.

### Camera

- `Orthographic` 2D camera, fixed transform.
- Post-process volume for bloom, slight chromatic aberration, film grain, vignette.

---

## 3. Core Systems

Keep each system to one small MonoBehaviour unless noted. No interfaces, no DI.

### 3.1 `SignalManager`
Single scene-level controller. Holds the authored signal list, tracks current index, advances on lock.

Responsibilities:
- Load current `SignalData`
- Push target values to `SignalEvaluator`
- Drive `SignalRenderer` with the current hidden image
- Handle win/partial/fail state and progression

### 3.2 `TuningControls`
Reads player input from three UI elements:
- **Frequency slider** (0–1)
- **Noise Filter knob** (0–1)
- **Phase Adjust knob** (0–1)

Exposes current values as floats. Raises `OnValueChanged` to feed the renderer and audio.

Implementation tip: knobs are UI `Image` objects rotated on drag; slider is a standard `Slider` reskinned.

### 3.3 `SignalEvaluator`
Pure function. Given current control values and a `SignalData`, returns a **clarity score** (0–1) based on distance to each target range. Used both for live preview feedback and final lock evaluation.

```
clarity = avg( freqClarity, noiseClarity, phaseClarity )
```

Each subscore uses a tolerance band: full score inside inner band, linear falloff through outer band, zero beyond.

### 3.4 `SignalRenderer`
Drives the CRT shader material parameters in real time based on `clarity`:
- `_HiddenImage` texture (from `SignalData`)
- `_Reveal` (0–1) = clarity
- `_NoiseStrength`, `_Chromatic`, `_Rolling`, `_Ghost` bound inversely to clarity
- `_Flicker` randomized subtle baseline

Also drives an on-screen **waveform** sprite/line renderer that gets "cleaner" as clarity rises.

### 3.5 `LockEvaluator`
When the Lock Signal button fires:
- Samples current clarity
- Classifies: `>= 0.85` success, `>= 0.55` partial, else fail
- Plays matching stinger + visual flash
- Tells `SignalManager` to advance (or retry on fail, design call)

### 3.6 `AudioDirector`
One manager. Continuous loops + reactive parameters:
- Static bed (volume ∝ 1 − clarity)
- Hum bed (always on)
- Signal tone (volume ∝ clarity, pitch nudged by frequency)
- Click SFX on knob/slider discrete steps
- Stingers: success / partial / fail

Use Unity's built-in `AudioSource` with volume/pitch tweens. No FMOD/Wwise for jam scope.

---

## 4. UI / Control Implementation

- All controls are **world-space UI** children of the CRT GameObject so they visually "belong" to the monitor.
- Slider: stock Unity UI `Slider` with custom art.
- Knobs: custom `KnobControl : MonoBehaviour` that converts pointer drag into rotation (−135° to +135°) and a normalized value.
- Button: stock `Button` with custom art and a pressed-state tween.
- Labels ("FREQUENCY", "NOISE FILTER", "PHASE ADJUST", "LOCK SIGNAL", "TUNE THE SIGNAL") are `TextMeshPro` baked into the frame.

Hover / pressed feedback:
- Small scale tween
- Click SFX via `AudioDirector`

---

## 5. Signal Data Structure

A `SignalData` **ScriptableObject** per signal. Authored in the Editor, no runtime data entry needed.

```csharp
[CreateAssetMenu]
public class SignalData : ScriptableObject {
    public string id;               // e.g. "monolith_01"
    public Sprite hiddenImage;      // revealed art
    public AudioClip signalTone;    // optional per-signal tone
    [Range(0,1)] public float targetFrequency;
    [Range(0,1)] public float targetNoise;
    [Range(0,1)] public float targetPhase;
    public float innerTolerance = 0.05f; // full score within
    public float outerTolerance = 0.20f; // zero score beyond
    [TextArea] public string archiveNote; // optional flavor text on success
}
```

`SignalManager` holds `public SignalData[] signals;` — authored order is play order.

---

## 6. Art & Audio Integration Points

### Art drop-in
- `Assets/Art/CRT/` — monitor body, bezel, knob, slider, button, glass overlay
- `Assets/Art/Desk/` — desk, mug, keyboard, papers, books, tapes, sticky notes
- `Assets/Art/Signals/` — one sprite per authored signal (hidden image)
- `Assets/Art/Background/` — back wall, shelves, ambient equipment

### Shader
- One CRT shader (URP Shader Graph or unlit HLSL) applied to the screen quad. Parameters listed in 3.4.

### Audio drop-in
- `Assets/Audio/Beds/` — static, hum
- `Assets/Audio/SFX/` — click, lock-success, lock-partial, lock-fail
- `Assets/Audio/Signals/` — one tone per signal (optional)

---

## 7. Weekend Build Order

Priorities front-loaded so the game is "shippable" by end of Day 1.

### Day 1 — Playable Core

1. **Scene skeleton**: fixed camera, placeholder CRT quad, placeholder desk sprite.
2. **Controls**: wire up slider + two knobs + Lock button with placeholder art. Emit float values.
3. **`SignalData` SO** + one test signal.
4. **`SignalEvaluator`** with tolerance math + live clarity readout (temporary on-screen number for debugging).
5. **`SignalRenderer` v0**: blend hidden image with a noise texture using `_Reveal`. Looks ugly, works.
6. **Lock flow**: button → evaluator → console log of success/partial/fail → advance to next signal.
7. **3 signals authored** with distinct hidden images (even placeholder).

**Checkpoint:** can play through 3 signals and get a result. No polish, but loop closed.

### Day 2 — Feel & Polish

8. **CRT shader**: scanlines, curvature, chromatic aberration, rolling distortion, flicker. Bind to clarity.
9. **Waveform**: reactive line renderer that smooths as clarity rises.
10. **Audio**: static bed, hum, tone, click, three stingers. Bind volumes to clarity.
11. **Desk dressing**: mug, keyboard, papers, sticky notes, manuals, tapes. Moody lighting + vignette.
12. **Final signal art** for all authored signals.
13. **Lock result visuals**: success flash + archive text overlay; fail collapse-to-noise animation.
14. **Title + ending card**: minimal "TUNE THE SIGNAL" intro and a short outro after last signal.

**Checkpoint:** game feels atmospheric, screen feels like a real CRT, tuning feels tactile.

### Stretch (only if ahead of schedule)

- Archive gallery of completed transmissions.
- Hidden sticky notes with clues for target tunings.
- Animated background equipment (blinking LEDs).
- A final "special" signal with a narrative payoff.

---

## 8. Stub First vs Polish Later

| Area                  | Stub first                         | Polish later                         |
|-----------------------|------------------------------------|--------------------------------------|
| CRT shader            | Alpha-blend image + noise          | Full scanline/curvature/chromatic    |
| Knobs                 | Unity sliders                      | Custom rotating knob control         |
| Hidden signal art     | Solid-color silhouettes            | Authored creepy sci-fi illustrations |
| Audio                 | One static loop                    | Layered beds + reactive volumes      |
| Desk                  | Flat colored rectangles            | Painted/kitbashed desk composition   |
| Lock feedback         | Console log                        | Flash, stinger, archive text         |
| Signal progression    | Hardcoded array index              | Same, just with real art/audio       |

---

## 9. MVP vs Stretch

### MVP (must ship)
- One scene, fixed camera, CRT + desk composition
- 3 signals with distinct hidden imagery
- Three working controls + Lock button
- Live distortion feedback driven by clarity
- Success / partial / fail lock outcomes with audio+visual stingers
- Start and end states

### Stretch (nice-to-have)
- 6 signals instead of 3
- Archive/gallery of decoded transmissions
- Sticky-note clues readable on close inspection
- Animated desk ambience (flicker, dust, blinking LEDs)
- Narrative final signal

---

## 10. Risk Watchlist

- **Shader rabbit hole.** Cap Day 2 shader work at a fixed time box. Ship with fewer effects if needed.
- **Art pipeline stall.** Use grayscale/silhouette placeholder signals until the system works end-to-end. Swap art in last.
- **Knob UX.** Drag-to-rotate can feel bad. If it's not feeling right after ~1 hour, fall back to sliders styled as knobs.
- **Audio mixing.** Reactive volumes can get muddy. Reserve a short pass at the end for mix levels only.

---

## 11. Done Definition

The game is done when a new player can:
1. Sit at the workstation, see the CRT, hear static.
2. Intuitively grab a control, feel the screen change.
3. Reach a recognizable transmission, press Lock, get clear feedback.
4. Experience 3+ distinct signals and a conclusion.
5. Walk away feeling like they tuned into *something*.
