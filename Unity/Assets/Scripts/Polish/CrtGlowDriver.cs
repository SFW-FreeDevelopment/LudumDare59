using SignalScrubber.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SignalScrubber.Polish
{
    /// <summary>
    /// Breathes the CRT's phosphor Light2D based on live signal clarity —
    /// dim + unstable while the signal is noisy, steady + bright once the
    /// player tunes in. A small Perlin-driven wobble is mixed in on top so
    /// the glow always feels "alive" like a real CRT's phosphor coat.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public sealed class CrtGlowDriver : MonoBehaviour
    {
        [SerializeField] TuningState tuning;
        [SerializeField] SignalManager manager;

        [Header("Intensity band")]
        [SerializeField] float minIntensity = 0.35f;
        [SerializeField] float maxIntensity = 1.6f;

        [Header("Wobble")]
        [SerializeField] float wobbleRate = 2.5f;
        [SerializeField] float wobbleAmplitude = 0.08f;

        Light2D _light;

        void Awake() => _light = GetComponent<Light2D>();

        void OnEnable()
        {
            if (_light == null) _light = GetComponent<Light2D>();
            Rebind();
        }

        /// <summary>
        /// Editor helper — LightsBootstrap calls this after wiring refs.
        /// </summary>
        public void Rebind()
        {
            if (tuning == null)  tuning  = FindFirstObjectByType<TuningState>();
            if (manager == null) manager = FindFirstObjectByType<SignalManager>();
        }

        void Update()
        {
            if (_light == null || tuning == null || manager == null) return;
            var current = manager.Current;
            float clarity = current != null
                ? SignalEvaluator.Clarity(tuning, current)
                : 0f;

            float baseIntensity = Mathf.Lerp(minIntensity, maxIntensity, clarity);
            float wobble = (Mathf.PerlinNoise(Time.time * wobbleRate, 0f) - 0.5f) * 2f * wobbleAmplitude;
            _light.intensity = Mathf.Max(0f, baseIntensity + wobble);
        }
    }
}
