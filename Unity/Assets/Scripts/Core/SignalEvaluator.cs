using UnityEngine;

namespace SignalScrubber.Core
{
    /// <summary>
    /// Per-axis clarity model. Three regions along the control range:
    ///
    ///   1. Inner plateau (|value - target| ≤ innerTolerance):
    ///      clarity is locked at 1.0. Gives the player a small forgiving
    ///      landing zone — once they're in it, tiny knob jitter doesn't
    ///      cost them the lock.
    ///   2. Falloff band (innerTolerance &lt; |d| &lt; 0.5):
    ///      clarity = 1 - ((|d| - inner) / (0.5 - inner)) ^ sharpness.
    ///      With sharpness &gt; 1 the curve has a gentle shoulder just
    ///      outside the plateau and drops faster toward the far end —
    ///      so you can always read direction but the far-field is low.
    ///   3. Opposite end (|d| ≥ 0.5): clarity = 0.
    ///
    /// Per-signal difficulty uses both <see cref="SignalData.innerTolerance"/>
    /// (smaller = tighter landing zone) and <see cref="SignalData.sharpness"/>
    /// (lower = steeper drop off the plateau).
    /// </summary>
    public static class SignalEvaluator
    {
        const float MaxDistance = 0.5f;

        public static float Clarity(float value, float target, float inner, float sharpness)
        {
            float d = Mathf.Abs(value - target);
            if (d <= inner) return 1f;

            float span = Mathf.Max(0.0001f, MaxDistance - inner);
            float x = Mathf.Clamp01((d - inner) / span);
            float falloff = Mathf.Pow(x, Mathf.Max(0.1f, sharpness));
            return Mathf.Clamp01(1f - falloff);
        }

        public static float Clarity(TuningState tuning, SignalData signal)
        {
            if (tuning == null || signal == null) return 0f;
            float inner = signal.innerTolerance;
            float sharp = signal.sharpness;
            float f = Clarity(tuning.Frequency, signal.targetFrequency, inner, sharp);
            float n = Clarity(tuning.Noise,     signal.targetNoise,     inner, sharp);
            float p = Clarity(tuning.Phase,     signal.targetPhase,     inner, sharp);
            return (f + n + p) / 3f;
        }
    }
}
