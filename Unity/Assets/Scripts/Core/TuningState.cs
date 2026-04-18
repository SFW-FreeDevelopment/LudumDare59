using System;
using UnityEngine;

namespace SignalScrubber.Core
{
    /// <summary>
    /// Single source of truth for the three live tuning axes. Raises
    /// <see cref="OnChanged"/> whenever any axis moves, so downstream
    /// systems (evaluator, renderer, audio, waveform) subscribe once
    /// and react without per-frame polling.
    /// </summary>
    public sealed class TuningState : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float frequency = 0.5f;
        [SerializeField, Range(0f, 1f)] float noise     = 0.5f;
        [SerializeField, Range(0f, 1f)] float phase     = 0.5f;

        public float Frequency => frequency;
        public float Noise     => noise;
        public float Phase     => phase;

        public event Action<TuningState> OnChanged;

        public void SetFrequency(float v) { frequency = Mathf.Clamp01(v); Raise(); }
        public void SetNoise(float v)     { noise     = Mathf.Clamp01(v); Raise(); }
        public void SetPhase(float v)     { phase     = Mathf.Clamp01(v); Raise(); }

        void Raise() => OnChanged?.Invoke(this);
    }
}
