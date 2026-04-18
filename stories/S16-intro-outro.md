# S16 — Intro / Outro Cards

**Milestone:** M3 — Polish
**Priority:** 🟦 MVP
**Depends on:** S02

## Goal

Give the game a start and an end. On launch, a brief intro card sets the mood and prompts the player to begin tuning. After the last signal, an outro card gives the experience a button.

## Deliverables

1. Extend `Overlay.uxml`:
   ```xml
   <ui:VisualElement name="intro" class="card">
     <ui:Label text="SIGNAL SCRUBBER" class="card-title" />
     <ui:Label text="Tune. Listen. Archive what comes through." class="card-sub" />
     <ui:Label text="[ click anywhere to begin ]" class="card-hint" />
   </ui:VisualElement>
   <ui:VisualElement name="outro" class="card" style="opacity: 0">
     <ui:Label text="END OF BROADCAST" class="card-title" />
     <ui:Label text="Some signals were never meant for us." class="card-sub" />
   </ui:VisualElement>
   ```

2. `Assets/UI/Styles/overlay.uss`:
   - `.card` — full-screen, flex-centered, black 90% background
   - `.card-title` — large phosphor green
   - `.card-sub` — cream, italic
   - `.card-hint` — amber, small

3. `Assets/Scripts/Polish/IntroOutroController.cs`:
   - On `OnEnable`, pause `SignalManager` (or simply delay its `OnEnable` via a `[SerializeField] bool startPaused`). The intro card is visible with CRT already running static.
   - Register a global click on the intro card: fade it out (~0.6s) and call `SignalManager.Begin()`.
   - Subscribe to `SignalManager.OnRunCompleted`: fade in the outro card.

4. Adjust `SignalManager`:
   - Split `OnEnable` auto-start into an explicit `Begin()` method.
   - Expose `[SerializeField] bool autoStart = false;` for editor iteration.

## Acceptance Criteria

- [ ] First frame shows intro card over an already-live CRT (static loop playing).
- [ ] Clicking dismisses the intro and tuning begins.
- [ ] After the final signal, the outro card fades in and stays.
- [ ] No input on controls while intro or outro is visible.

## Out of Scope

- Restart button on outro (stretch if time allows).
- Scene-level fade to black at boot.

## Implementation Notes

- Disable pointer events on the CRT controls while intro/outro is visible by setting the controls-row `pickingMode = PickingMode.Ignore` or just toggling `SetEnabled(false)`.
