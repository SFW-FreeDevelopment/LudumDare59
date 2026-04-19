using UnityEngine;

namespace SignalScrubber.Core
{
    /// <summary>
    /// One authored transmission: hidden art + audio, plus the target tuning
    /// the player needs to approximate to reveal it. Evaluated by
    /// <c>SignalEvaluator</c> against a <see cref="TuningState"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "SignalScrubber/Signal", fileName = "Signal")]
    public sealed class SignalData : ScriptableObject
    {
        public string id;

        [Header("Payload")]
        public Sprite hiddenImage;
        public AudioClip signalTone;

        [Header("Target tuning")]
        [Range(0f, 1f)] public float targetFrequency = 0.5f;
        [Range(0f, 1f)] public float targetNoise     = 0.5f;
        [Range(0f, 1f)] public float targetPhase     = 0.5f;

        [Header("Difficulty")]
        [Tooltip("Plateau half-width around the target where per-axis clarity is locked at 1.0. Bigger = more forgiving landing zone.")]
        [Range(0f, 0.15f)] public float innerTolerance = 0.05f;

        [Tooltip("Power-curve exponent for the falloff *outside* the inner tolerance plateau. >1 = gentle shoulder near the plateau then fast drop mid-range. =1 = linear. <1 = steep drop right off the plateau.")]
        [Range(0.3f, 3f)] public float sharpness = 1.3f;

        [Tooltip("Seconds the player has to lock this signal before it times out and auto-fails.")]
        [Range(5f, 120f)] public float allottedSeconds = 30f;

        [Header("Legacy (unused)")]
        [Tooltip("Kept for serialization compatibility with older SignalData assets.")]
        [HideInInspector] public float outerTolerance = 0.20f;

        [Header("Presentation")]
        public Color tint = new Color(0.49f, 1f, 0.62f);

        [TextArea] public string archiveNote;
    }
}
