# S09 — SignalManager & LockEvaluator

**Milestone:** M1 — Playable Core
**Priority:** 🟦 MVP
**Depends on:** S07, S08

## Goal

Drive progression: hold the signal list, expose the current signal, and evaluate Lock Signal presses into success / partial / fail outcomes that advance the game.

## Deliverables

1. `Assets/Scripts/Core/LockOutcome.cs`:
   ```csharp
   namespace SignalScrubber.Core {
     public enum LockOutcome { Fail, Partial, Success }
   }
   ```

2. `Assets/Scripts/Core/SignalManager.cs`:
   ```csharp
   namespace SignalScrubber.Core {
     public sealed class SignalManager : MonoBehaviour {
       [SerializeField] SignalData[] signals;
       [SerializeField] TuningState tuning;
       [SerializeField] UI.CrtFrameController frame;

       public SignalData Current => signals[_index];
       public int Index => _index;
       public int Count => signals.Length;
       int _index;

       public event System.Action<SignalData> OnSignalStarted;
       public event System.Action<SignalData, LockOutcome, float> OnSignalLocked;
       public event System.Action OnRunCompleted;

       void OnEnable() {
         frame.OnLockPressed += HandleLock;
         StartSignal(0);
       }
       void OnDisable() => frame.OnLockPressed -= HandleLock;

       void StartSignal(int i) {
         _index = i;
         OnSignalStarted?.Invoke(Current);
       }

       void HandleLock() {
         float clarity = SignalEvaluator.Clarity(tuning, Current);
         LockOutcome outcome =
           clarity >= 0.85f ? LockOutcome.Success :
           clarity >= 0.55f ? LockOutcome.Partial :
                              LockOutcome.Fail;
         OnSignalLocked?.Invoke(Current, outcome, clarity);

         if (_index + 1 >= signals.Length) OnRunCompleted?.Invoke();
         else StartSignal(_index + 1);
       }
     }
   }
   ```

3. Wire `SignalManager` into the scene on `Systems/SignalManager` with references to `TuningState`, `CrtFrameController`, and the 3 `SignalData` assets from S07.
4. Temporary logger component `Assets/Scripts/Core/DebugSignalLogger.cs` that subscribes to all three events and prints them. This disappears during S15 polish.

## Acceptance Criteria

- [ ] Scene plays, first signal starts automatically.
- [ ] Pressing Lock logs outcome + clarity and advances to the next signal.
- [ ] After the last signal, `OnRunCompleted` fires once.
- [ ] Clamped thresholds: force good tuning → Success; worst tuning → Fail.

## Out of Scope

- Visual/audio lock feedback (S15).
- Per-signal renderer hookup (S11).

## Implementation Notes

- The thresholds 0.85 / 0.55 are tunable; expose them as `[SerializeField] float successThreshold = 0.85f;` etc. so S17 playtest can dial them.
- Don't retry on fail for MVP — advance regardless. S15 may add retry if the playtest shows fails feel like dead ends.
