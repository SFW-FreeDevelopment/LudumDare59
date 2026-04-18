# S14 — AudioDirector with Reactive Mix

**Milestone:** M2 — Feel
**Priority:** 🟦 MVP
**Depends on:** S06

## Goal

A single `AudioDirector` that owns the static/hum/tone beds and one-shot SFX. Volumes and pitch react to `TuningState` and to `SignalManager` events. Real clips drop in later; placeholders are silent stubs.

## Deliverables

1. `Assets/Scripts/Audio/AudioDirector.cs`:
   ```csharp
   namespace SignalScrubber.Audio {
     public sealed class AudioDirector : MonoBehaviour {
       [Header("Refs")]
       [SerializeField] Core.TuningState tuning;
       [SerializeField] Core.SignalManager manager;
       [SerializeField] UI.CrtFrameController frame;

       [Header("Beds")]
       [SerializeField] AudioSource staticBed;
       [SerializeField] AudioSource humBed;
       [SerializeField] AudioSource signalTone;

       [Header("One-shots")]
       [SerializeField] AudioSource oneShot;
       [SerializeField] AudioClip click;
       [SerializeField] AudioClip lockSuccess;
       [SerializeField] AudioClip lockPartial;
       [SerializeField] AudioClip lockFail;

       Core.SignalData _current;

       void OnEnable() {
         tuning.OnChanged += OnTuningChanged;
         manager.OnSignalStarted += OnSignalStarted;
         manager.OnSignalLocked  += OnSignalLocked;
       }
       void OnDisable() {
         tuning.OnChanged -= OnTuningChanged;
         manager.OnSignalStarted -= OnSignalStarted;
         manager.OnSignalLocked  -= OnSignalLocked;
       }

       void OnSignalStarted(Core.SignalData s) {
         _current = s;
         if (s.signalTone) { signalTone.clip = s.signalTone; signalTone.loop = true; signalTone.Play(); }
         OnTuningChanged(tuning);
       }

       void OnTuningChanged(Core.TuningState t) {
         if (_current == null) return;
         float clarity = Core.SignalEvaluator.Clarity(t, _current);
         staticBed.volume  = Mathf.Lerp(0.9f, 0.05f, clarity);
         signalTone.volume = clarity;
         signalTone.pitch  = Mathf.Lerp(0.9f, 1.1f, t.Frequency);
       }

       void OnSignalLocked(Core.SignalData s, Core.LockOutcome o, float c) {
         var clip = o switch {
           Core.LockOutcome.Success => lockSuccess,
           Core.LockOutcome.Partial => lockPartial,
           _                        => lockFail,
         };
         if (clip) oneShot.PlayOneShot(clip);
       }

       public void Click() { if (click) oneShot.PlayOneShot(click, 0.4f); }
     }
   }
   ```

2. Scene setup:
   - `Systems/AudioDirector` GameObject with four child `AudioSource`s: `StaticBed`, `HumBed`, `SignalTone`, `OneShot`.
   - `StaticBed` and `HumBed` are `loop=true, playOnAwake=true`.
   - Assign placeholder silent `AudioClip` assets (or leave null for MVP; the code null-checks).

3. Hook `CrtFrameController` to call `AudioDirector.Click()` when a knob crosses an integer step of `value * 20` (i.e. discrete detents). Implementation:
   - In `CrtFrameController`, remember last `int step = Mathf.RoundToInt(value * 20);` per control. When it changes, call `audio.Click()`.

4. Asset drop-in folders (reminder — user provides):
   - `Assets/Audio/Beds/bed_static.wav`, `bed_hum.wav`
   - `Assets/Audio/SFX/click.wav`, `lock_success.wav`, `lock_partial.wav`, `lock_fail.wav`
   - `Assets/Audio/Signals/signal_01.wav`, etc.

## Acceptance Criteria

- [ ] No null-reference errors when clips are missing.
- [ ] Rotating a knob produces detent clicks.
- [ ] Tuning near target audibly drops static and raises signal tone.
- [ ] Lock press plays the correct stinger for success / partial / fail.

## Out of Scope

- Music cues (S17 may add a sparse ambient music loop if the user supplies one).
- AudioMixer groups — overkill for jam.

## Implementation Notes

- Use `AudioSource.volume` directly; do not introduce an AudioMixer.
- Click cooldown isn't needed — detent quantization naturally rate-limits.
