using UnityEngine;

namespace SignalScrubber.Rendering
{
    /// <summary>
    /// Owns the runtime <c>Material</c> instance on the CRT screen quad
    /// and exposes the shader contract from ARCHITECTURE.md as simple
    /// C# setters. Caches <c>.material</c> once in Awake to avoid
    /// per-frame allocations — callers invoke these setters on demand
    /// (tuning changes, signal starts, ambient flicker tick).
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class CrtMaterialBinder : MonoBehaviour
    {
        static readonly int Reveal       = Shader.PropertyToID("_Reveal");
        static readonly int NoiseStr     = Shader.PropertyToID("_NoiseStrength");
        static readonly int Chromatic    = Shader.PropertyToID("_Chromatic");
        static readonly int Rolling      = Shader.PropertyToID("_Rolling");
        static readonly int Ghost        = Shader.PropertyToID("_Ghost");
        static readonly int Flicker      = Shader.PropertyToID("_Flicker");
        static readonly int Tint         = Shader.PropertyToID("_Tint");
        static readonly int HiddenImage  = Shader.PropertyToID("_HiddenImage");

        Material _mat;

        void Awake()
        {
            _mat = GetComponent<MeshRenderer>().material;
        }

        public void SetHiddenImage(Texture tex)
        {
            if (_mat == null) return;
            _mat.SetTexture(HiddenImage, tex);
        }

        public void SetTint(Color c)
        {
            if (_mat == null) return;
            _mat.SetColor(Tint, c);
        }

        public void SetClarity(float clarity)
        {
            if (_mat == null) return;
            clarity = Mathf.Clamp01(clarity);
            float inv = 1f - clarity;
            _mat.SetFloat(Reveal, clarity);
            _mat.SetFloat(NoiseStr, Mathf.Lerp(0.2f, 1.0f, inv));
            _mat.SetFloat(Chromatic, Mathf.Lerp(0.05f, 0.6f, inv));
            _mat.SetFloat(Rolling, Mathf.Lerp(0.05f, 0.9f, inv));
            _mat.SetFloat(Ghost, Mathf.Lerp(0.05f, 0.7f, inv));
        }

        public void SetFlicker(float f)
        {
            if (_mat == null) return;
            _mat.SetFloat(Flicker, Mathf.Clamp01(f));
        }
    }
}
