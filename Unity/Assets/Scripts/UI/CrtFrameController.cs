using System;
using SignalScrubber.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace SignalScrubber.UI
{
    /// <summary>
    /// Queries the diegetic CRT UIDocument for its slider, two knobs, and
    /// Lock Signal button, and routes their values into <c>TuningState</c>.
    /// The Lock press is surfaced as a C# event for <c>SignalManager</c>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CrtFrameController : MonoBehaviour
    {
        [SerializeField] TuningState tuning;

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

            if (_frequency != null) _frequency.RegisterValueChangedCallback(HandleFrequency);
            if (_noise     != null) _noise.RegisterCallback<ChangeEvent<float>>(HandleNoise);
            if (_phase     != null) _phase.RegisterCallback<ChangeEvent<float>>(HandlePhase);
            if (_lock      != null) _lock.clicked += HandleLock;

            // Seed downstream systems with the current UI values so they do
            // not start at zeroed defaults before the first interaction.
            if (tuning != null)
            {
                if (_frequency != null) tuning.SetFrequency(_frequency.value);
                if (_noise     != null) tuning.SetNoise(_noise.value);
                if (_phase     != null) tuning.SetPhase(_phase.value);
            }
        }

        void OnDisable()
        {
            if (_frequency != null) _frequency.UnregisterValueChangedCallback(HandleFrequency);
            if (_noise     != null) _noise.UnregisterCallback<ChangeEvent<float>>(HandleNoise);
            if (_phase     != null) _phase.UnregisterCallback<ChangeEvent<float>>(HandlePhase);
            if (_lock      != null) _lock.clicked -= HandleLock;
        }

        void HandleFrequency(ChangeEvent<float> e) => tuning?.SetFrequency(e.newValue);
        void HandleNoise(ChangeEvent<float> e)     => tuning?.SetNoise(e.newValue);
        void HandlePhase(ChangeEvent<float> e)     => tuning?.SetPhase(e.newValue);
        void HandleLock()                          => OnLockPressed?.Invoke();
    }
}
