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
        [Tooltip("Power-curve exponent used by SignalEvaluator. Lower = steeper drop-off near the target (harder to pin exactly, but you can still read direction from far away). Higher = more forgiving / linear.")]
        [Range(0.15f, 1f)] public float sharpness = 0.5f;

        [Tooltip("Seconds the player has to lock this signal before it times out and auto-fails.")]
        [Range(5f, 120f)] public float allottedSeconds = 30f;

        [Header("Legacy (unused)")]
        [Tooltip("Kept for serialization compatibility with older SignalData assets; SignalEvaluator now uses sharpness instead.")]
        [HideInInspector] public float innerTolerance = 0.05f;
        [HideInInspector] public float outerTolerance = 0.20f;

        [Header("Presentation")]
        public Color tint = new Color(0.49f, 1f, 0.62f);

        [TextArea] public string archiveNote;
    }
}
