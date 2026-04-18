using System;
using SignalScrubber.UI;
using UnityEngine;

namespace SignalScrubber.Core
{
    /// <summary>
    /// Drives signal progression. Holds the authored list, exposes the
    /// current signal, and classifies Lock Signal presses into Fail /
    /// Partial / Success against thresholds that are tunable in the
    /// inspector. Emits events for renderer/audio/polish systems to
    /// react without cross-coupling.
    /// </summary>
    public sealed class SignalManager : MonoBehaviour
    {
        [SerializeField] SignalData[] signals;
        [SerializeField] TuningState tuning;
        [SerializeField] CrtFrameController frame;

        [Header("Thresholds")]
        [Range(0f, 1f)] [SerializeField] float successThreshold = 0.85f;
        [Range(0f, 1f)] [SerializeField] float partialThreshold = 0.55f;

        public SignalData Current => IsValidIndex ? signals[_index] : null;
        public int Index => _index;
        public int Count => signals?.Length ?? 0;
        public bool IsValidIndex => signals != null && _index >= 0 && _index < signals.Length;

        int _index;

        public event Action<SignalData> OnSignalStarted;
        public event Action<SignalData, LockOutcome, float> OnSignalLocked;
        public event Action OnRunCompleted;

        void OnEnable()
        {
            if (frame != null) frame.OnLockPressed += HandleLock;
            if (Count > 0) StartSignal(0);
        }

        void OnDisable()
        {
            if (frame != null) frame.OnLockPressed -= HandleLock;
        }

        void StartSignal(int i)
        {
            _index = i;
            OnSignalStarted?.Invoke(Current);
        }

        void HandleLock()
        {
            if (!IsValidIndex) return;

            float clarity = SignalEvaluator.Clarity(tuning, Current);
            LockOutcome outcome =
                clarity >= successThreshold ? LockOutcome.Success :
                clarity >= partialThreshold ? LockOutcome.Partial :
                                              LockOutcome.Fail;

            OnSignalLocked?.Invoke(Current, outcome, clarity);

            if (_index + 1 >= signals.Length) OnRunCompleted?.Invoke();
            else StartSignal(_index + 1);
        }
    }
}
