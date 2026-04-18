using System.Collections;
using SignalScrubber.Core;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SignalScrubber.Polish
{
    /// <summary>
    /// Bookends the run with intro and outro cards rendered by the overlay
    /// UIDocument. Holds the intro up with the CRT playing idle static
    /// beneath until the player clicks or presses a key; dismisses it and
    /// calls <see cref="SignalManager.Begin"/>; shows the outro card after
    /// <see cref="SignalManager.OnRunCompleted"/>.
    ///
    /// Defensively works even when the overlay UIDocument is missing or
    /// broken: if the intro visual can't be found the component still polls
    /// for input and calls Begin() directly, so the game never soft-locks
    /// on a UI Toolkit wiring issue.
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

        [Header("Diagnostics")]
        [SerializeField] bool verbose = true;

        VisualElement _intro;
        VisualElement _outro;
        VisualElement _overlayRoot;
        bool _dismissing;
        bool _started;
        bool _outroShowing;

        void OnEnable()
        {
            ResolveMissingRefs();
            if (manager != null) manager.OnRunCompleted += HandleRunCompleted;
            SetControlsInteractable(false);
            Log("OnEnable: manager=" + (manager != null) + " overlayDocument=" + (overlayDocument != null));
        }

        void OnDisable()
        {
            if (manager != null) manager.OnRunCompleted -= HandleRunCompleted;
            if (_overlayRoot != null) _overlayRoot.UnregisterCallback<KeyDownEvent>(OnAnyKey);
            if (_intro != null) _intro.UnregisterCallback<PointerDownEvent>(OnAnyClick);
        }

        void Start()
        {
            Log("Start: caching overlay...");
            CacheOverlay();
        }

        void Update()
        {
            // Already started — nothing to do.
            if (_started) return;

            // Poll input unconditionally while waiting to start. Works even
            // if _intro failed to cache (the intro overlay may be visually
            // broken, but the game still needs to become playable).
            if (PollDismissInput())
            {
                Log("Update: input detected -> dismissing intro");
                DismissNow();
            }
        }

        static bool PollDismissInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame) return true;
                if (kb.enterKey.wasPressedThisFrame) return true;
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space))  return true;
            if (Input.GetKeyDown(KeyCode.Return)) return true;
            if (Input.GetMouseButtonDown(0))      return true;
#endif
            return false;
        }

        void ResolveMissingRefs()
        {
            if (manager == null)
            {
                manager = FindFirstObjectByType<SignalManager>();
                if (manager != null) Log("ResolveMissingRefs: found SignalManager");
            }

            var docs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            if (overlayDocument == null)
            {
                foreach (var doc in docs)
                    if (doc.name == "OverlayUI") { overlayDocument = doc; break; }
                if (overlayDocument != null) Log("ResolveMissingRefs: found OverlayUI");
            }
            if (controlsToDisable == null)
            {
                foreach (var doc in docs)
                    if (doc.name == "DiegeticUI") { controlsToDisable = doc.gameObject; break; }
            }
        }

        void CacheOverlay()
        {
            var root = overlayDocument != null ? overlayDocument.rootVisualElement : null;
            if (root == null)
            {
                Log("CacheOverlay: overlay root null, retrying next frame");
                StartCoroutine(RetryCacheOverlay());
                return;
            }

            _overlayRoot = root;
            _intro = root.Q<VisualElement>("intro");
            _outro = root.Q<VisualElement>("outro");
            Log($"CacheOverlay: intro={_intro != null} outro={_outro != null} started={_started}");

            // The overlay UIDocument is toggled off/on across the run. Each
            // time it comes back on its visualTree is re-instantiated from
            // the UXML defaults — so we must drive card visibility from our
            // state flags, not from the freshly-built defaults.
            if (_intro != null)
            {
                bool showIntro = !_started;
                _intro.style.opacity = showIntro ? 1f : 0f;
                _intro.style.display = showIntro ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_outro != null && !_outroShowing)
            {
                _outro.style.opacity = 0f;
                _outro.style.display = DisplayStyle.None;
            }

            root.pickingMode = PickingMode.Ignore;
            foreach (var child in root.Children())
                if (child != _intro && child != _outro) child.pickingMode = PickingMode.Ignore;

            if (!_started)
            {
                root.focusable = true;
                root.Focus();
                root.RegisterCallback<KeyDownEvent>(OnAnyKey);
                if (_intro != null) _intro.RegisterCallback<PointerDownEvent>(OnAnyClick);
            }
        }

        IEnumerator RetryCacheOverlay()
        {
            yield return null;
            CacheOverlay();
        }

        void OnAnyKey(KeyDownEvent _) => DismissNow();
        void OnAnyClick(PointerDownEvent _) => DismissNow();

        void DismissNow()
        {
            if (_started || _dismissing) return;
            _dismissing = true;

            if (_overlayRoot != null)
            {
                _overlayRoot.UnregisterCallback<KeyDownEvent>(OnAnyKey);
            }
            if (_intro != null)
            {
                _intro.UnregisterCallback<PointerDownEvent>(OnAnyClick);
            }
            StartCoroutine(DismissIntroThenBegin());
        }

        IEnumerator DismissIntroThenBegin()
        {
            Log("DismissIntroThenBegin: fading and starting...");
            if (_intro != null)
            {
                yield return FadeOut(_intro, fadeDuration);
                _intro.style.display = DisplayStyle.None;
            }
            SetControlsInteractable(true);

            // Disable the overlay UIDocument entirely while no card is up.
            // The panel blocks pointer events to the diegetic panel below
            // even when its contents are all picking-mode: ignore, so the
            // only reliable way to let the CRT controls receive clicks is
            // to take the panel offline. LockFlash / ShowOutro flip it
            // back on when they need it.
            SetOverlayActive(false);

            _started = true;
            if (manager != null)
            {
                Log("DismissIntroThenBegin: calling SignalManager.Begin()");
                manager.Begin();
            }
            else
            {
                Debug.LogError("[Intro] SignalManager is null — cannot begin the run.");
            }
        }

        /// <summary>
        /// Toggles the overlay UIDocument's component enabled state.
        /// Used by LockFlash while showing the archive card, and by
        /// ShowOutro. When re-enabled, the rootVisualElement is rebuilt;
        /// callers must re-query their cached VisualElement references.
        /// </summary>
        public void SetOverlayActive(bool on)
        {
            if (overlayDocument == null) return;
            overlayDocument.enabled = on;
        }

        void HandleRunCompleted() => StartCoroutine(ShowOutro());

        IEnumerator ShowOutro()
        {
            SetControlsInteractable(false);
            _outroShowing = true;

            SetOverlayActive(true);
            yield return null;
            CacheOverlay();

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

        void Log(string msg)
        {
            if (verbose) Debug.Log("[Intro] " + msg);
        }
    }
}
