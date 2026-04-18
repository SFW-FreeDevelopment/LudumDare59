using UnityEngine;
using UnityEngine.UIElements;

namespace SignalScrubber.UI
{
    /// <summary>
    /// Waveform line rendered inside the CRT frame. Smooths toward a clean
    /// sine as clarity rises and jitters chaotically when clarity is low.
    /// Buffer is reused across repaints so the GC allocation from
    /// <c>generateVisualContent</c> is a single array at construction time.
    /// </summary>
    [UxmlElement]
    public partial class WaveformElement : VisualElement
    {
        const int Samples = 128;

        public float Clarity { get; set; }
        public Color StrokeColor { get; set; } = new Color(0.49f, 1f, 0.62f);

        readonly float[] _buf = new float[Samples];
        float _phase;

        public WaveformElement()
        {
            generateVisualContent += OnGenerate;
        }

        /// <summary>
        /// Advance the waveform by <paramref name="dt"/> seconds. Callers
        /// are expected to set <see cref="Clarity"/> just before ticking.
        /// </summary>
        public void Tick(float dt)
        {
            _phase += dt * Mathf.Lerp(2f, 8f, Clarity);
            float jitterAmp = Mathf.Lerp(1.2f, 0.1f, Clarity);

            for (int i = 0; i < Samples; i++)
            {
                float t = (float)i / (Samples - 1);
                float clean = Mathf.Sin((_phase + t * 6.283f) * 2f) * 0.4f;
                float noise = (Random.value - 0.5f) * jitterAmp;
                _buf[i] = Mathf.Lerp(noise, clean, Clarity);
            }
            MarkDirtyRepaint();
        }

        void OnGenerate(MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            p.strokeColor = StrokeColor;
            p.lineWidth = 2f;

            var r = contentRect;
            if (r.width <= 0f || r.height <= 0f) return;

            p.BeginPath();
            for (int i = 0; i < Samples; i++)
            {
                float x = r.xMin + (float)i / (Samples - 1) * r.width;
                float y = r.center.y + _buf[i] * r.height * 0.5f;
                if (i == 0) p.MoveTo(new Vector2(x, y));
                else        p.LineTo(new Vector2(x, y));
            }
            p.Stroke();
        }
    }
}
