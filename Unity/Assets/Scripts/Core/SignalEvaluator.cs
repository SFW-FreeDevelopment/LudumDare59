using UnityEngine;

namespace SignalScrubber.Core
{
    /// <summary>
    /// Pure tuning-clarity math. Given a control value, its target, and a
    /// tolerance band, returns a [0, 1] clarity score: full inside the inner
    /// band, linearly falling off to zero at the outer band, zero beyond.
    /// The signal-level <see cref="Clarity(TuningState, SignalData)"/>
    /// averages the three axes.
    /// </summary>
    public static class SignalEvaluator
    {
        public static float Clarity(float value, float target, float inner, float outer)
        {
            float d = Mathf.Abs(value - target);
            if (d <= inner) return 1f;
            if (d >= outer) return 0f;
            return 1f - (d - inner) / (outer - inner);
        }

        public static float Clarity(TuningState tuning, SignalData signal)
        {
            if (tuning == null || signal == null) return 0f;
            float f = Clarity(tuning.Frequency, signal.targetFrequency,
                              signal.innerTolerance, signal.outerTolerance);
            float n = Clarity(tuning.Noise,     signal.targetNoise,
                              signal.innerTolerance, signal.outerTolerance);
            float p = Clarity(tuning.Phase,     signal.targetPhase,
                              signal.innerTolerance, signal.outerTolerance);
            return (f + n + p) / 3f;
        }
    }
}
