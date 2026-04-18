using SignalScrubber.Core;
using SignalScrubber.UI;
using UnityEngine;

namespace SignalScrubber.Audio
{
    /// <summary>
    /// Scene-singleton audio bus. Owns the continuous static/hum/signal
    /// beds and a single one-shot <see cref="AudioSource"/> pool. Reacts
    /// to TuningState changes (clarity -> static/tone volumes, frequency
    /// -> tone pitch) and to SignalManager events (tone swap on start,
    /// stinger on lock). Null clip fields are tolerated so the build runs
    /// before the real audio drops in.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] TuningState tuning;
        [SerializeField] SignalManager manager;

        [Header("Beds")]
        [SerializeField] AudioSource staticBed;
        [SerializeField] AudioSource humBed;
        [SerializeField] AudioSource signalTone;

        [Header("One-shots")]
        [SerializeField] AudioSource oneShot;
        [SerializeField] AudioClip click;
        [SerializeField] AudioClip lockSuccess;
        [SerializeField] AudioClip lockPartial;
        [SerializeField] AudioClip lockFail;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] float staticMin = 0.05f;
        [SerializeField, Range(0f, 1f)] float staticMax = 0.90f;
        [SerializeField] float tonePitchMin = 0.9f;
        [SerializeField] float tonePitchMax = 1.1f;

        SignalData _current;

        void OnEnable()
        {
            if (tuning  != null) tuning.OnChanged        += HandleTuningChanged;
            if (manager != null)
            {
                manager.OnSignalStarted += HandleSignalStarted;
                manager.OnSignalLocked  += HandleSignalLocked;
            }
        }

        void OnDisable()
        {
            if (tuning  != null) tuning.OnChanged        -= HandleTuningChanged;
            if (manager != null)
            {
                manager.OnSignalStarted -= HandleSignalStarted;
                manager.OnSignalLocked  -= HandleSignalLocked;
            }
        }

        void HandleSignalStarted(SignalData s)
        {
            _current = s;
            if (signalTone != null)
            {
                signalTone.Stop();
                signalTone.clip = s?.signalTone;
                signalTone.loop = true;
                if (signalTone.clip != null) signalTone.Play();
            }
            HandleTuningChanged(tuning);
        }

        void HandleTuningChanged(TuningState t)
        {
            if (t == null || _current == null) return;
            float clarity = SignalEvaluator.Clarity(t, _current);
            if (staticBed  != null) staticBed.volume  = Mathf.Lerp(staticMax, staticMin, clarity);
            if (signalTone != null)
            {
                signalTone.volume = clarity;
                signalTone.pitch  = Mathf.Lerp(tonePitchMin, tonePitchMax, t.Frequency);
            }
        }

        void HandleSignalLocked(SignalData s, LockOutcome outcome, float clarity)
        {
            AudioClip clip = outcome switch
            {
                LockOutcome.Success => lockSuccess,
                LockOutcome.Partial => lockPartial,
                _                   => lockFail,
            };
            if (clip != null && oneShot != null) oneShot.PlayOneShot(clip);
        }

        public void Click()
        {
            if (click != null && oneShot != null) oneShot.PlayOneShot(click, 0.4f);
        }
    }
}
