using SignalScrubber.Rendering;
using UnityEngine;

namespace SignalScrubber.Polish
{
    /// <summary>
    /// Keeps the scene feeling alive when the player is idle: pushes a
    /// Perlin-driven flicker into the CRT material every frame, and
    /// breathes the power LED's alpha on a separate Perlin lane.
    /// Amplitudes are intentionally small — any more reads as "broken
    /// monitor" rather than "old CRT."
    /// </summary>
    public sealed class AmbientFlicker : MonoBehaviour
    {
        [SerializeField] CrtMaterialBinder binder;
        [SerializeField] SpriteRenderer powerLed;
        [SerializeField, Range(0f, 1f)] float ledBaseAlpha = 0.9f;
        [SerializeField] float crtFlickerRate = 3f;
        [SerializeField] float ledBreathRate  = 5f;

        void Update()
        {
            if (binder != null)
            {
                float f = 0.1f + 0.1f * Mathf.PerlinNoise(Time.time * crtFlickerRate, 0f);
                binder.SetFlicker(f);
            }

            if (powerLed != null)
            {
                var c = powerLed.color;
                c.a = ledBaseAlpha + (Mathf.PerlinNoise(Time.time * ledBreathRate, 1f) - 0.5f) * 0.1f;
                powerLed.color = c;
            }
        }
    }
}
