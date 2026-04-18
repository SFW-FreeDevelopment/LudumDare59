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

        [Header("Start-up")]
        [Tooltip("When true, Begin() fires on enable. Leave off when IntroOutroController is driving the flow.")]
        [SerializeField] bool autoStart = false;

        public SignalData Current => IsValidIndex ? signals[_index] : null;
        public int Index => _index;
        public int Count => signals?.Length ?? 0;
        public bool IsValidIndex => signals != null && _index >= 0 && _index < signals.Length;

        int _index;

        public event Action<SignalData> OnSignalStarted;
        public event Action<SignalData, LockOutcome, float> OnSignalLocked;
        public event Action OnRunCompleted;

        bool _runCompleted;

        void OnEnable()
        {
            if (frame != null) frame.OnLockPressed += HandleLock;
            if (autoStart) Begin();
        }

        void OnDisable()
        {
            if (frame != null) frame.OnLockPressed -= HandleLock;
        }

        /// <summary>
        /// Starts the first signal. Called explicitly by the intro flow
        /// (S16) so the CRT can be "idle static" under the intro card
        /// before the player begins tuning.
        /// </summary>
        public void Begin()
        {
            _runCompleted = false;
            if (Count > 0) StartSignal(0);
        }

        /// <summary>
        /// Called by the lock-feedback coroutine once its visual/audio
        /// transition has played out. Advances to the next signal or
        /// emits OnRunCompleted.
        /// </summary>
        public void Advance()
        {
            if (_runCompleted) return;
            if (_index + 1 >= signals.Length)
            {
                _runCompleted = true;
                OnRunCompleted?.Invoke();
            }
            else
            {
                StartSignal(_index + 1);
            }
        }

        void StartSignal(int i)
        {
            _index = i;
            OnSignalStarted?.Invoke(Current);
        }

        void HandleLock()
        {
            if (!IsValidIndex || _runCompleted) return;

            float clarity = SignalEvaluator.Clarity(tuning, Current);
            LockOutcome outcome =
                clarity >= successThreshold ? LockOutcome.Success :
                clarity >= partialThreshold ? LockOutcome.Partial :
                                              LockOutcome.Fail;

            OnSignalLocked?.Invoke(Current, outcome, clarity);
            // Advance is deferred: LockFlash (S15) calls Advance() once
            // its transition completes so visuals can linger on the lock.
        }
    }
}
