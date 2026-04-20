using UnityEngine;

namespace SignalScrubber.Polish
{
    /// <summary>
    /// Drops a small ParticleSystem above the mug at runtime so the prefab
    /// stays authored entirely in code. Soft, slow, semi-transparent white
    /// puffs that drift upward — reads as lukewarm coffee steam against the
    /// desk backdrop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MugSteam : MonoBehaviour
    {
        [SerializeField] Vector3 localOffset = new Vector3(0f, 1f, -0.02f);
        [SerializeField] float emitRadius = 0.15f;
        [SerializeField] float rate = 6f;
        [SerializeField] float lifetime = 2.5f;
        [SerializeField] float startSize = 0.35f;
        [SerializeField] float riseSpeed = 0.6f;
        [SerializeField, Range(0f, 1f)] float startAlpha = 0.35f;
        [SerializeField] int sortingOrder = 2;

        void Awake()
        {
            var go = new GameObject("Steam");
            var t = go.transform;
            t.SetParent(transform, false);
            t.localPosition = localOffset;
            t.rotation = Quaternion.identity;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = lifetime;
            main.startSpeed = riseSpeed;
            main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.7f, startSize * 1.3f);
            main.startColor = new Color(1f, 1f, 1f, startAlpha);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            main.gravityModifier = 0f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = emitRadius;
            shape.radiusThickness = 1f;
            shape.rotation = new Vector3(90f, 0f, 0f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            velocity.y = new ParticleSystem.MinMaxCurve(riseSpeed * 0.8f, riseSpeed * 1.2f);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.4f),
                new Keyframe(0.3f, 1f),
                new Keyframe(1f, 1.6f));
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(0.6f, 0.6f),
                    new GradientAlphaKey(0f, 1f),
                });
            color.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = sortingOrder;
            renderer.sharedMaterial = BuildParticleMaterial();

            ps.Play();
        }

        static Material BuildParticleMaterial()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            var mat = new Material(shader) { name = "SteamParticle" };
            if (mat.HasProperty("_MainTex"))
                mat.mainTexture = BuildPuffTexture();
            return mat;
        }

        static Texture2D BuildPuffTexture()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "SteamPuff",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[size * size];
            float cx = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx) / cx;
                float dy = (y - cx) / cx;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                float a  = Mathf.Clamp01(1f - d);
                a = a * a;
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
