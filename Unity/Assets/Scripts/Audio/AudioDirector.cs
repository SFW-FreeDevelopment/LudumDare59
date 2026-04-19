using SignalScrubber.Core;
using UnityEngine;

namespace SignalScrubber.Audio
{
    /// <summary>
    /// Scene-singleton audio bus. Owns the four continuous beds
    /// (static, hum, desk ambience, per-signal tone) and a single
    /// one-shot <see cref="AudioSource"/> pool. Reacts to TuningState
    /// changes (clarity → static/tone volumes, frequency → tone pitch),
    /// to SignalManager events (tone swap on start, stinger on lock,
    /// broadcast-end stinger on run complete), and to SignalTimer
    /// (heartbeat tick each second during the low-time warning).
    /// Every clip field is nullable so the build still runs with
    /// missing audio.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] TuningState tuning;
        [SerializeField] SignalManager manager;
        [SerializeField] SignalTimer timer;

        [Header("Beds")]
        [SerializeField] AudioSource staticBed;
        [SerializeField] AudioSource humBed;
        [SerializeField] AudioSource deskAmbience;
        [SerializeField] AudioSource signalTone;
        [SerializeField] AudioSource musicBed;

        [Header("One-shots")]
        [SerializeField] AudioSource oneShot;
        [SerializeField] AudioClip click;
        [SerializeField] AudioClip lockSuccess;
        [SerializeField] AudioClip lockPartial;
        [SerializeField] AudioClip lockFail;
        [SerializeField] AudioClip timerTick;
        [SerializeField] AudioClip powerOn;
        [SerializeField] AudioClip broadcastEnd;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] float staticMin = 0.05f;
        [SerializeField, Range(0f, 1f)] float staticMax = 0.90f;
        [SerializeField] float tonePitchMin = 0.9f;
        [SerializeField] float tonePitchMax = 1.1f;

        [Header("Music")]
        [SerializeField, Range(0f, 1f)] float musicBaseVolume = 0.35f;
        [Tooltip("Multiplier applied to music volume while the archive card is up, so stingers land cleanly.")]
        [SerializeField, Range(0f, 1f)] float musicDuckedMultiplier = 0.45f;
        [SerializeField] float musicFadeSeconds = 0.6f;

        SignalData _current;
        int _lastTickSecond = -1;
        Coroutine _musicFade;

        void OnEnable()
        {
            if (tuning  != null) tuning.OnChanged += HandleTuningChanged;
            if (manager != null)
            {
                manager.OnSignalStarted += HandleSignalStarted;
                manager.OnSignalLocked  += HandleSignalLocked;
                manager.OnRunCompleted  += HandleRunCompleted;
            }
            if (timer != null) timer.OnTick += HandleTimerTick;

            if (musicBed != null)
            {
                musicBed.loop = true;
                musicBed.volume = musicBaseVolume;
                if (musicBed.clip != null && !musicBed.isPlaying) musicBed.Play();
            }
        }

        void OnDisable()
        {
            if (tuning  != null) tuning.OnChanged -= HandleTuningChanged;
            if (manager != null)
            {
                manager.OnSignalStarted -= HandleSignalStarted;
                manager.OnSignalLocked  -= HandleSignalLocked;
                manager.OnRunCompleted  -= HandleRunCompleted;
            }
            if (timer != null) timer.OnTick -= HandleTimerTick;
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
            _lastTickSecond = -1;
            // Restore music volume as the next level spins up.
            FadeMusic(musicBaseVolume);
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
            _lastTickSecond = -1;

            // Duck the music under the archive / SIGNAL LOST card so the
            // lock stinger and the card text land cleanly. Restored on the
            // next OnSignalStarted (or once the run completes).
            DuckMusic();
        }

        void HandleRunCompleted()
        {
            if (broadcastEnd != null && oneShot != null) oneShot.PlayOneShot(broadcastEnd);
            // Fade the bed out entirely for the outro card — no need to
            // restore afterward since the scene reloads on restart.
            FadeMusic(0f);
        }

        void HandleTimerTick(float remaining, float allotted)
        {
            if (timer == null || !timer.IsLow) { _lastTickSecond = -1; return; }
            // Tick at each whole-second boundary while in the low band.
            int second = Mathf.CeilToInt(remaining);
            if (second == _lastTickSecond) return;
            _lastTickSecond = second;
            if (timerTick != null && oneShot != null) oneShot.PlayOneShot(timerTick, 0.7f);
        }

        /// <summary>Detent click on knob / slider step. Hooked by CrtFrameController.</summary>
        public void Click()
        {
            if (click != null && oneShot != null) oneShot.PlayOneShot(click, 0.4f);
        }

        /// <summary>Intro dismissed — the set powers on.</summary>
        public void PlayPowerOn()
        {
            if (powerOn != null && oneShot != null) oneShot.PlayOneShot(powerOn);
        }

        // ---------- music helpers ----------

        void DuckMusic() => FadeMusic(musicBaseVolume * musicDuckedMultiplier);

        void FadeMusic(float targetVolume)
        {
            if (musicBed == null) return;
            if (_musicFade != null) StopCoroutine(_musicFade);
            _musicFade = StartCoroutine(FadeMusicCoroutine(targetVolume));
        }

        System.Collections.IEnumerator FadeMusicCoroutine(float target)
        {
            float start = musicBed.volume;
            float duration = Mathf.Max(0.01f, musicFadeSeconds);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                musicBed.volume = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            musicBed.volume = target;
            _musicFade = null;
        }
    }
}
