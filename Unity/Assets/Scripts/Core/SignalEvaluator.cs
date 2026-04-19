using UnityEngine;

namespace SignalScrubber.Core
{
    /// <summary>
    /// Pure tuning-clarity math. Given a control value, its target, and a
    /// <c>sharpness</c> exponent, returns a [0, 1] per-axis clarity using a
    /// smooth power curve across the whole 0–1 control range:
    ///
    ///     clarity = max(0, 1 - (|value - target| / 0.5) ^ sharpness)
    ///
    /// Properties of the curve (sharpness &lt; 1 like 0.45):
    ///   • clarity hits exactly 1 at the target and smoothly descends to 0
    ///     at the opposite end of the control range (180° away) — no flat
    ///     dead zone, so players always see some feedback when they move.
    ///   • the curve is steepest near the target, so small movements around
    ///     the correct value cause large clarity swings (hard to dial in
    ///     exactly — you have to commit to the right zone).
    ///   • far from the target the curve flattens, so motion out there
    ///     still reads, just subtly.
    ///
    /// Per-signal difficulty is controlled by <see cref="SignalData.sharpness"/>.
    /// Lower = steeper / harder; higher = more linear / forgiving.
    /// </summary>
    public static class SignalEvaluator
    {
        const float MaxDistance = 0.5f;

        /// <summary>Per-axis clarity in [0, 1].</summary>
        public static float Clarity(float value, float target, float sharpness)
        {
            float d = Mathf.Abs(value - target);
            float x = Mathf.Clamp01(d / MaxDistance);
            float falloff = Mathf.Pow(x, Mathf.Max(0.01f, sharpness));
            return Mathf.Clamp01(1f - falloff);
        }

        /// <summary>Signal-level clarity: average of the three axis scores.</summary>
        public static float Clarity(TuningState tuning, SignalData signal)
        {
            if (tuning == null || signal == null) return 0f;
            float s = signal.sharpness;
            float f = Clarity(tuning.Frequency, signal.targetFrequency, s);
            float n = Clarity(tuning.Noise,     signal.targetNoise,     s);
            float p = Clarity(tuning.Phase,     signal.targetPhase,     s);
            return (f + n + p) / 3f;
        }
    }
}
