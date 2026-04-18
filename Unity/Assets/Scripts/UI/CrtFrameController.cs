using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SignalScrubber.UI
{
    /// <summary>
    /// Queries the diegetic CRT UIDocument for its slider, two knobs, and
    /// Lock Signal button, and exposes their values. S05 logs on change;
    /// S06 redirects these into <c>TuningState</c>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CrtFrameController : MonoBehaviour
    {
        public event Action<float> OnFrequencyChanged;
        public event Action<float> OnNoiseChanged;
        public event Action<float> OnPhaseChanged;
        public event Action OnLockPressed;

        Slider _frequency;
        KnobElement _noise;
        KnobElement _phase;
        Button _lock;

        public float Frequency => _frequency?.value ?? 0f;
        public float Noise     => _noise?.value     ?? 0f;
        public float Phase     => _phase?.value     ?? 0f;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null) return;

            _frequency = root.Q<Slider>("frequency");
            _noise     = root.Q<KnobElement>("noise");
            _phase     = root.Q<KnobElement>("phase");
            _lock      = root.Q<Button>("lock");

            if (_frequency != null)
                _frequency.RegisterValueChangedCallback(HandleFrequency);
            if (_noise != null)
                _noise.RegisterCallback<ChangeEvent<float>>(HandleNoise);
            if (_phase != null)
                _phase.RegisterCallback<ChangeEvent<float>>(HandlePhase);
            if (_lock != null)
                _lock.clicked += HandleLock;
        }

        void OnDisable()
        {
            if (_frequency != null)
                _frequency.UnregisterValueChangedCallback(HandleFrequency);
            if (_noise != null)
                _noise.UnregisterCallback<ChangeEvent<float>>(HandleNoise);
            if (_phase != null)
                _phase.UnregisterCallback<ChangeEvent<float>>(HandlePhase);
            if (_lock != null)
                _lock.clicked -= HandleLock;
        }

        void HandleFrequency(ChangeEvent<float> e)
        {
            OnFrequencyChanged?.Invoke(e.newValue);
            Debug.Log($"[CRT] frequency={e.newValue:0.00} noise={Noise:0.00} phase={Phase:0.00}");
        }

        void HandleNoise(ChangeEvent<float> e)
        {
            OnNoiseChanged?.Invoke(e.newValue);
            Debug.Log($"[CRT] frequency={Frequency:0.00} noise={e.newValue:0.00} phase={Phase:0.00}");
        }

        void HandlePhase(ChangeEvent<float> e)
        {
            OnPhaseChanged?.Invoke(e.newValue);
            Debug.Log($"[CRT] frequency={Frequency:0.00} noise={Noise:0.00} phase={e.newValue:0.00}");
        }

        void HandleLock()
        {
            OnLockPressed?.Invoke();
            Debug.Log("[CRT] LOCK");
        }
    }
}
