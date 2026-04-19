using System.Collections;
using SignalScrubber.Core;
using SignalScrubber.Rendering;
using UnityEngine;
using UnityEngine.UIElements;

namespace SignalScrubber.Polish
{
    /// <summary>
    /// Handles the visual + timing beat between a Lock Signal press and the
    /// next signal starting.
    ///   • Success — snap reveal to 1, flash the archive card with the
    ///     signal's note.
    ///   • Partial — hold reveal at 0.7, dim archive card.
    ///   • Fail    — collapse reveal to 0, show a dedicated SIGNAL LOST
    ///     card so timeouts + bad locks read clearly instead of the screen
    ///     just flashing and a fresh clock starting.
    ///
    /// Owns the post-lock delay so <see cref="SignalManager.HandleLock"/>
    /// stays a pure classifier. Calls <see cref="SignalManager.Advance"/>
    /// when the transition completes.
    /// </summary>
    public sealed class LockFlash : MonoBehaviour
    {
        [SerializeField] SignalManager manager;
        [SerializeField] CrtMaterialBinder binder;
        [SerializeField] UIDocument overlayDocument;

        [Header("Timings (seconds)")]
        [SerializeField] float successHold = 3.5f;
        [SerializeField] float partialHold = 2.5f;
        [SerializeField] float failHold    = 2.0f;

        [Header("Fail copy")]
        [SerializeField] string failTitle = "SIGNAL LOST";
        [SerializeField] string failBody  = "The carrier slipped. Tune the next one while you still can.";

        VisualElement _archiveCard;
        Label _archiveTitle;
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
            _archiveCard  = root?.Q<VisualElement>("archive-card");
            _archiveTitle = root?.Q<Label>("archive-title");
            _archiveBody  = root?.Q<Label>("archive-body");
        }

        void HandleLocked(SignalData signal, LockOutcome outcome, float clarity)
        {
            if (_transitioning) return;
            _transitioning = true;
            StartCoroutine(RunTransition(signal, outcome));
        }

        IEnumerator RunTransition(SignalData signal, LockOutcome outcome)
        {
            // Bring the overlay online for the card (all three outcomes
            // now show a card so the player always gets explicit feedback).
            if (overlayDocument != null)
            {
                overlayDocument.enabled = true;
                yield return null; // let UIDocument rebuild
                CacheOverlay();
            }

            switch (outcome)
            {
                case LockOutcome.Success:
                    if (binder != null) binder.SetClarity(1f);
                    ShowArchive("TRANSMISSION ARCHIVED",
                                signal != null ? signal.archiveNote : string.Empty,
                                dim: false);
                    yield return Hold(successHold);
                    break;

                case LockOutcome.Partial:
                    if (binder != null) binder.SetClarity(0.7f);
                    ShowArchive("FRAGMENT RECOVERED",
                                signal != null ? signal.archiveNote : string.Empty,
                                dim: true);
                    yield return Hold(partialHold);
                    break;

                default:
                    if (binder != null) binder.SetClarity(0f);
                    ShowArchive(failTitle, failBody, dim: true);
                    yield return Hold(failHold);
                    break;
            }

            HideArchive();
            if (overlayDocument != null) overlayDocument.enabled = false;

            _transitioning = false;
            if (manager != null) manager.Advance();
        }

        void ShowArchive(string title, string body, bool dim)
        {
            if (_archiveCard == null) return;
            if (_archiveTitle != null) _archiveTitle.text = title;
            if (_archiveBody  != null) _archiveBody.text  = body;
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
