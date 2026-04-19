using SignalScrubber.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace SignalScrubber.UI
{
    /// <summary>
    /// Paints the per-signal countdown into the CRT's top-right readout.
    /// Swaps a .timer-readout--low modifier on for the final few seconds
    /// so it flips red and glows. Hides the label entirely between runs
    /// so the readout doesn't linger on the intro / outro cards.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TimerDriver : MonoBehaviour
    {
        [SerializeField] SignalTimer timer;

        Label _readout;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _readout = root?.Q<Label>("timer-readout");
            if (_readout != null) _readout.style.display = DisplayStyle.None;
        }

        void Update()
        {
            if (_readout == null || timer == null) return;

            // Only show while the timer is actively counting — hides during
            // intro, between signals (archive card hold), and after the
            // run completes so the readout doesn't linger on frozen values.
            if (!timer.Running)
            {
                _readout.style.display = DisplayStyle.None;
                return;
            }

            _readout.style.display = DisplayStyle.Flex;
            _readout.text = Format(timer.Remaining);
            _readout.EnableInClassList("timer-readout--low", timer.IsLow);
        }

        static string Format(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int total = Mathf.CeilToInt(seconds);
            int m = total / 60;
            int s = total % 60;
            return $"{m:00}:{s:00}";
        }
    }
}
