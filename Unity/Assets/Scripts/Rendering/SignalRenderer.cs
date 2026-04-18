using SignalScrubber.Core;
using UnityEngine;

namespace SignalScrubber.Rendering
{
    /// <summary>
    /// Bridges the tuning + signal state onto the CRT shader via
    /// <see cref="CrtMaterialBinder"/>. On every tuning change, recomputes
    /// clarity and pushes it to the material; on signal start, swaps the
    /// hidden image texture and the phosphor tint.
    /// </summary>
    public sealed class SignalRenderer : MonoBehaviour
    {
        [SerializeField] TuningState tuning;
        [SerializeField] SignalManager manager;
        [SerializeField] CrtMaterialBinder binder;

        SignalData _current;

        void OnEnable()
        {
            if (tuning  != null) tuning.OnChanged        += HandleTuningChanged;
            if (manager != null) manager.OnSignalStarted += HandleSignalStarted;
        }

        void OnDisable()
        {
            if (tuning  != null) tuning.OnChanged        -= HandleTuningChanged;
            if (manager != null) manager.OnSignalStarted -= HandleSignalStarted;
        }

        void HandleSignalStarted(SignalData s)
        {
            _current = s;
            if (binder == null || s == null) return;
            binder.SetHiddenImage(s.hiddenImage != null ? s.hiddenImage.texture : null);
            binder.SetTint(s.tint);
            Refresh();
        }

        void HandleTuningChanged(TuningState _) => Refresh();

        void Refresh()
        {
            if (_current == null || binder == null || tuning == null) return;
            float clarity = SignalEvaluator.Clarity(tuning, _current);
            binder.SetClarity(clarity);
        }
    }
}
