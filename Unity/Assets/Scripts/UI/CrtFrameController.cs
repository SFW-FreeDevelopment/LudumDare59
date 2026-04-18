using System;
using SignalScrubber.Audio;
using SignalScrubber.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace SignalScrubber.UI
{
    /// <summary>
    /// Queries the diegetic CRT UIDocument for its slider, two knobs, and
    /// Lock Signal button, and routes their values into <c>TuningState</c>.
    /// The Lock press is surfaced as a C# event for <c>SignalManager</c>,
    /// and detent-level changes are clicked through <c>AudioDirector</c>.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CrtFrameController : MonoBehaviour
    {
        const int Detents = 20;

        [SerializeField] TuningState tuning;
        [SerializeField] AudioDirector audio;

        public event Action OnLockPressed;

        int _freqStep = int.MinValue;
        int _noiseStep = int.MinValue;
        int _phaseStep = int.MinValue;

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

        void HandleFrequency(ChangeEvent<float> e)
        {
            tuning?.SetFrequency(e.newValue);
            ClickIfStepChanged(e.newValue, ref _freqStep);
        }

        void HandleNoise(ChangeEvent<float> e)
        {
            tuning?.SetNoise(e.newValue);
            ClickIfStepChanged(e.newValue, ref _noiseStep);
        }

        void HandlePhase(ChangeEvent<float> e)
        {
            tuning?.SetPhase(e.newValue);
            ClickIfStepChanged(e.newValue, ref _phaseStep);
        }

        void HandleLock() => OnLockPressed?.Invoke();

        void ClickIfStepChanged(float value, ref int lastStep)
        {
            int step = Mathf.RoundToInt(value * Detents);
            if (step == lastStep) return;
            lastStep = step;
            if (audio != null) audio.Click();
        }
    }
}
