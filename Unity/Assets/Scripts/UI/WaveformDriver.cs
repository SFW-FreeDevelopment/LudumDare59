using SignalScrubber.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace SignalScrubber.UI
{
    /// <summary>
    /// Locates the <see cref="WaveformElement"/> in the diegetic UIDocument
    /// and drives its clarity + tick each frame. Kept as its own component
    /// (not folded into CrtFrameController) so the waveform can disable
    /// independently during intro/outro without interfering with tuning.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class WaveformDriver : MonoBehaviour
    {
        [SerializeField] TuningState tuning;
        [SerializeField] SignalManager manager;

        WaveformElement _waveform;
        Label _signalStrength;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _waveform       = root?.Q<WaveformElement>("waveform");
            _signalStrength = root?.Q<Label>("signal-strength");
        }

        void Update()
        {
            if (_waveform == null || tuning == null || manager == null) return;
            var current = manager.Current;
            float clarity = current != null
                ? SignalEvaluator.Clarity(tuning, current)
                : 0f;
            _waveform.Clarity = clarity;
            _waveform.Tick(Time.deltaTime);

            if (_signalStrength != null)
            {
                if (current == null)
                {
                    _signalStrength.text = "SIGNAL STRENGTH  — —";
                }
                else
                {
                    int pct = Mathf.RoundToInt(clarity * 100f);
                    string bar = BuildBar(clarity, 12);
                    _signalStrength.text = $"SIGNAL STRENGTH  {bar}  {pct,3}%";
                }
            }
        }

        static string BuildBar(float clarity, int cells)
        {
            int filled = Mathf.Clamp(Mathf.RoundToInt(clarity * cells), 0, cells);
            var sb = new System.Text.StringBuilder(cells + 2);
            sb.Append('[');
            for (int i = 0; i < cells; i++) sb.Append(i < filled ? '=' : ' ');
            sb.Append(']');
            return sb.ToString();
        }
    }
}
