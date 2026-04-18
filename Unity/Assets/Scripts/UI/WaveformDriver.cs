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

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _waveform = root?.Q<WaveformElement>("waveform");
        }

        void Update()
        {
            if (_waveform == null || tuning == null || manager == null) return;
            var current = manager.Current;
            _waveform.Clarity = current != null
                ? SignalEvaluator.Clarity(tuning, current)
                : 0f;
            _waveform.Tick(Time.deltaTime);
        }
    }
}
