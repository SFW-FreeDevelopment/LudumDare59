# S05 — Controls UXML + KnobElement

**Milestone:** M1 — Playable Core
**Priority:** 🟦 MVP
**Depends on:** S02

## Goal

Author the diegetic CRT frame UI in UXML/USS and implement a custom `KnobElement` that supports drag-to-rotate. Two knobs (noise, phase), one slider (frequency), and one Lock button sit inside the frame.

## Deliverables

1. `Assets/Scripts/UI/KnobElement.cs`:
   ```csharp
   namespace SignalScrubber.UI {
     [UxmlElement]
     public partial class KnobElement : VisualElement {
       [UxmlAttribute] public float Value { get; set; }          // 0..1
       [UxmlAttribute] public float AngleRange { get; set; } = 270f;
       // Emits ChangeEvent<float> on drag.
     }
   }
   ```
   - On `PointerDownEvent`: capture pointer, record start angle.
   - On `PointerMoveEvent` while captured: update `Value` by delta (vertical drag or radial — pick vertical for jam: 200px drag = full range).
   - On `PointerUpEvent`: release capture.
   - Rotates an inner element (class `.knob-indicator`) via `style.rotate`.
   - Sends `ChangeEvent<float>.GetPooled(oldValue, Value)` so bindings fire.

2. `Assets/UI/Styles/crt.uss` additions:
   - `.knob` — circular element, 80×80px, dark with border.
   - `.knob-indicator` — small white tick anchored top-center that rotates.
   - `.knob-label` — small amber caption below.
   - `.freq-slider` — a vertical `unity-slider` reskin (tall, narrow).
   - `.lock-button` — amber button with phosphor hover tint.
   - `.controls-row` — flex row with `justify-content: space-around;`.

3. `Assets/UI/Documents/CrtFrame.uxml` filled out:
   ```xml
   <ui:UXML xmlns:ui="UnityEngine.UIElements"
            xmlns:ss="SignalScrubber.UI">
     <ui:VisualElement class="crt-panel">
       <ui:Label text="TUNE THE SIGNAL" class="crt-title" />
       <ui:VisualElement class="signal-area">
         <!-- screen-space readouts like waveform placeholder -->
         <ui:VisualElement name="waveform" class="waveform" />
       </ui:VisualElement>
       <ui:VisualElement class="controls-row">
         <ui:VisualElement class="control-column">
           <ui:Slider name="frequency" class="freq-slider" direction="Vertical"
                      low-value="0" high-value="1" />
           <ui:Label text="FREQUENCY" class="crt-label" />
         </ui:VisualElement>
         <ui:VisualElement class="control-column">
           <ss:KnobElement name="noise" class="knob" />
           <ui:Label text="NOISE FILTER" class="crt-label" />
         </ui:VisualElement>
         <ui:VisualElement class="control-column">
           <ui:Button name="lock" class="lock-button" text="LOCK SIGNAL" />
         </ui:VisualElement>
         <ui:VisualElement class="control-column">
           <ss:KnobElement name="phase" class="knob" />
           <ui:Label text="PHASE ADJUST" class="crt-label" />
         </ui:VisualElement>
       </ui:VisualElement>
     </ui:VisualElement>
   </ui:UXML>
   ```

4. Wire up a minimal `Assets/Scripts/UI/CrtFrameController.cs` MonoBehaviour attached to the `DiegeticUI` GameObject that queries the UIDocument for the controls and logs value changes to the console. This proves bindings work; S06 replaces the log with real state.

## Acceptance Criteria

- [ ] Slider and both knobs respond to drag and visibly move.
- [ ] Console logs `frequency=0.73 noise=0.42 phase=0.10` (or similar) as controls change.
- [ ] Lock button logs `LOCK` on click.
- [ ] Layout renders readably on the CRT screen region (alignment fine-tuned later).

## Out of Scope

- Applying values to a signal (S06).
- Real click SFX (S14).

## Implementation Notes

- `[UxmlElement]` requires Unity 2023.2+ / 6.x. The project uses a recent Unity version; if `UxmlElement` isn't recognized, fall back to the legacy `UxmlFactory<KnobElement, UxmlTraits>` pattern.
- Put the USS in `crt.uss`, imported from `CrtFrame.uxml` via `<Style src="project://database/Assets/UI/Styles/crt.uss" />` at the top of the UXML.
- Knob drag: keep math simple. `value += -deltaY / 200f;` clamped 0..1 works fine.
