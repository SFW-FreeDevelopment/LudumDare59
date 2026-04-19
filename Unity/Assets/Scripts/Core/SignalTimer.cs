using System;
using UnityEngine;

namespace SignalScrubber.Core
{
    /// <summary>
    /// Per-signal countdown. Resets to the current SignalData's
    /// allottedSeconds when a new signal starts, stops ticking while a
    /// lock transition is playing out, and fires <see cref="SignalManager.
    /// ForceTimeout"/> if time runs out before the player locks.
    /// </summary>
    public sealed class SignalTimer : MonoBehaviour
    {
        [SerializeField] SignalManager manager;

        [Header("Warning band")]
        [Tooltip("Remaining seconds at or below which the UI should read as 'about to fail'.")]
        [SerializeField] float lowThreshold = 5f;

        public float Remaining { get; private set; }
        public float Allotted  { get; private set; }
        public bool Running    { get; private set; }
        public bool IsLow => Running && Remaining <= lowThreshold;

        /// <summary>Fires every frame the timer is active so UI can refresh.</summary>
        public event Action<float, float> OnTick; // (remaining, allotted)

        void OnEnable()
        {
            if (manager == null) return;
            manager.OnSignalStarted += HandleStarted;
            manager.OnSignalLocked  += HandleLocked;
            manager.OnRunCompleted  += HandleRunCompleted;
        }

        void OnDisable()
        {
            if (manager == null) return;
            manager.OnSignalStarted -= HandleStarted;
            manager.OnSignalLocked  -= HandleLocked;
            manager.OnRunCompleted  -= HandleRunCompleted;
        }

        void HandleStarted(SignalData s)
        {
            Allotted = s != null ? Mathf.Max(1f, s.allottedSeconds) : 30f;
            Remaining = Allotted;
            Running = true;
            OnTick?.Invoke(Remaining, Allotted);
        }

        void HandleLocked(SignalData s, LockOutcome outcome, float clarity)
        {
            Running = false;
        }

        void HandleRunCompleted()
        {
            Running = false;
        }

        void Update()
        {
            if (!Running) return;
            Remaining -= Time.deltaTime;
            if (Remaining <= 0f)
            {
                Remaining = 0f;
                Running = false;
                OnTick?.Invoke(Remaining, Allotted);
                if (manager != null) manager.ForceTimeout();
                return;
            }
            OnTick?.Invoke(Remaining, Allotted);
        }
    }
}
