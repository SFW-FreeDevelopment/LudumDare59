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

        [Header("Tolerance")]
        [Range(0.01f, 0.2f)] public float innerTolerance = 0.05f;
        [Range(0.05f, 0.4f)] public float outerTolerance = 0.20f;

        [Header("Difficulty")]
        [Tooltip("Seconds the player has to lock this signal before it times out and auto-fails.")]
        [Range(5f, 120f)] public float allottedSeconds = 30f;

        [Header("Presentation")]
        public Color tint = new Color(0.49f, 1f, 0.62f);

        [TextArea] public string archiveNote;
    }
}
