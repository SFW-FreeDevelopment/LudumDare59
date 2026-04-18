using System.Collections;
using SignalScrubber.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace SignalScrubber.Polish
{
    /// <summary>
    /// Bookends the run with intro and outro cards rendered by the
    /// overlay UIDocument. Holds the intro up with the CRT playing
    /// idle static beneath until the player clicks; dismisses it and
    /// calls <see cref="SignalManager.Begin"/>; shows the outro card
    /// after <see cref="SignalManager.OnRunCompleted"/>.
    /// </summary>
    public sealed class IntroOutroController : MonoBehaviour
    {
        [SerializeField] UIDocument overlayDocument;
        [SerializeField] SignalManager manager;

        [Header("Input filter")]
        [Tooltip("DiegeticUI GameObject. Its UIDocument tree is disabled while intro/outro is up.")]
        [SerializeField] GameObject controlsToDisable;

        [Header("Timings (seconds)")]
        [SerializeField] float fadeDuration = 0.6f;
        [SerializeField] float outroDelay   = 0.5f;

        VisualElement _intro;
        VisualElement _outro;

        void OnEnable()
        {
            CacheOverlay();
            if (manager != null) manager.OnRunCompleted += HandleRunCompleted;
            SetControlsInteractable(false);
        }

        void OnDisable()
        {
            if (manager != null) manager.OnRunCompleted -= HandleRunCompleted;
            if (_intro != null) _intro.UnregisterCallback<PointerDownEvent>(OnIntroClick);
        }

        void CacheOverlay()
        {
            var root = overlayDocument != null ? overlayDocument.rootVisualElement : null;
            if (root == null) return;

            _intro = root.Q<VisualElement>("intro");
            _outro = root.Q<VisualElement>("outro");

            if (_intro != null)
            {
                _intro.style.opacity = 1f;
                _intro.style.display = DisplayStyle.Flex;
                _intro.RegisterCallback<PointerDownEvent>(OnIntroClick);
            }
            if (_outro != null)
            {
                _outro.style.opacity = 0f;
                _outro.style.display = DisplayStyle.None;
            }
        }

        void OnIntroClick(PointerDownEvent _) => StartCoroutine(DismissIntroThenBegin());

        IEnumerator DismissIntroThenBegin()
        {
            if (_intro != null) _intro.UnregisterCallback<PointerDownEvent>(OnIntroClick);
            yield return FadeOut(_intro, fadeDuration);
            if (_intro != null) _intro.style.display = DisplayStyle.None;
            SetControlsInteractable(true);
            if (manager != null) manager.Begin();
        }

        void HandleRunCompleted() => StartCoroutine(ShowOutro());

        IEnumerator ShowOutro()
        {
            SetControlsInteractable(false);
            yield return new WaitForSeconds(outroDelay);
            if (_outro != null)
            {
                _outro.style.display = DisplayStyle.Flex;
                _outro.style.opacity = 0f;
                yield return FadeIn(_outro, fadeDuration);
            }
        }

        void SetControlsInteractable(bool on)
        {
            if (controlsToDisable == null) return;
            var doc = controlsToDisable.GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            if (root == null) return;
            root.pickingMode = on ? PickingMode.Position : PickingMode.Ignore;
            root.SetEnabled(on);
        }

        IEnumerator FadeIn(VisualElement ve, float duration)  => Fade(ve, 0f, 1f, duration);
        IEnumerator FadeOut(VisualElement ve, float duration) => Fade(ve, 1f, 0f, duration);

        IEnumerator Fade(VisualElement ve, float from, float to, float duration)
        {
            if (ve == null || duration <= 0f)
            {
                if (ve != null) ve.style.opacity = to;
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                ve.style.opacity = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            ve.style.opacity = to;
        }
    }
}
