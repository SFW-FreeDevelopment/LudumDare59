using UnityEngine;

namespace SignalScrubber.Core
{
    /// <summary>
    /// Temporary during M1: mirrors <see cref="SignalManager"/> events to
    /// the console so humans can watch progression while the renderer,
    /// audio, and lock-feedback systems land. Deleted during S17 polish.
    /// </summary>
    [RequireComponent(typeof(SignalManager))]
    public sealed class DebugSignalLogger : MonoBehaviour
    {
        SignalManager _manager;

        void OnEnable()
        {
            _manager = GetComponent<SignalManager>();
            _manager.OnSignalStarted += HandleStarted;
            _manager.OnSignalLocked  += HandleLocked;
            _manager.OnRunCompleted  += HandleCompleted;
        }

        void OnDisable()
        {
            if (_manager == null) return;
            _manager.OnSignalStarted -= HandleStarted;
            _manager.OnSignalLocked  -= HandleLocked;
            _manager.OnRunCompleted  -= HandleCompleted;
        }

        void HandleStarted(SignalData s)
            => Debug.Log($"[Signal] START {s?.id} idx={_manager.Index + 1}/{_manager.Count}");

        void HandleLocked(SignalData s, LockOutcome outcome, float clarity)
            => Debug.Log($"[Signal] LOCK  {s?.id} {outcome} clarity={clarity:0.00}");

        void HandleCompleted()
            => Debug.Log("[Signal] RUN COMPLETED");
    }
}
