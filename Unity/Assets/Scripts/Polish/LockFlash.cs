using System.Collections;
using SignalScrubber.Core;
using SignalScrubber.Rendering;
using UnityEngine;
using UnityEngine.UIElements;

namespace SignalScrubber.Polish
{
    /// <summary>
    /// Handles the visual+timing beat between a Lock Signal press and the
    /// next signal starting. On Success we snap reveal to 1 and flash the
    /// archive card for a hold; on Partial we dim the card and shorten the
    /// hold; on Fail we collapse reveal to 0 and let the card stay dark.
    ///
    /// Owns the post-lock delay so <see cref="SignalManager.HandleLock"/>
    /// can stay a pure classifier. Calls <see cref="SignalManager.Advance"/>
    /// when the transition completes.
    /// </summary>
    public sealed class LockFlash : MonoBehaviour
    {
        [SerializeField] SignalManager manager;
        [SerializeField] CrtMaterialBinder binder;
        [SerializeField] UIDocument overlayDocument;

        [Header("Timings (seconds)")]
        [SerializeField] float successHold = 1.5f;
        [SerializeField] float partialHold = 1.0f;
        [SerializeField] float failHold    = 1.0f;

        VisualElement _archiveCard;
        Label _archiveBody;
        bool _transitioning;

        void OnEnable()
        {
            if (manager != null) manager.OnSignalLocked += HandleLocked;
            CacheOverlay();
        }

        void OnDisable()
        {
            if (manager != null) manager.OnSignalLocked -= HandleLocked;
        }

        void CacheOverlay()
        {
            var root = overlayDocument != null ? overlayDocument.rootVisualElement : null;
            _archiveCard = root?.Q<VisualElement>("archive-card");
            _archiveBody = root?.Q<Label>("archive-body");
        }

        void HandleLocked(SignalData signal, LockOutcome outcome, float clarity)
        {
            if (_transitioning) return;
            _transitioning = true;
            StartCoroutine(RunTransition(signal, outcome));
        }

        IEnumerator RunTransition(SignalData signal, LockOutcome outcome)
        {
            if (_archiveCard == null) CacheOverlay();

            switch (outcome)
            {
                case LockOutcome.Success:
                    if (binder != null) binder.SetClarity(1f);
                    ShowArchive(signal, dim: false);
                    yield return Hold(successHold);
                    break;

                case LockOutcome.Partial:
                    if (binder != null) binder.SetClarity(0.7f);
                    ShowArchive(signal, dim: true);
                    yield return Hold(partialHold);
                    break;

                default:
                    if (binder != null) binder.SetClarity(0f);
                    yield return Hold(failHold);
                    break;
            }

            HideArchive();
            _transitioning = false;
            if (manager != null) manager.Advance();
        }

        void ShowArchive(SignalData signal, bool dim)
        {
            if (_archiveCard == null) return;
            if (_archiveBody != null)
                _archiveBody.text = signal != null ? signal.archiveNote : string.Empty;
            _archiveCard.EnableInClassList("archive-card--dim", dim);
            _archiveCard.style.opacity = 1f;
        }

        void HideArchive()
        {
            if (_archiveCard == null) return;
            _archiveCard.style.opacity = 0f;
            _archiveCard.EnableInClassList("archive-card--dim", false);
        }

        static IEnumerator Hold(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }
    }
}
