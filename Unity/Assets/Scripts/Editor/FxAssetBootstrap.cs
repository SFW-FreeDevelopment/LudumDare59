using System.IO;
using UnityEditor;
using UnityEngine;

namespace SignalScrubber.EditorTools
{
    /// <summary>
    /// Procedurally generates the placeholder textures used by the CRT
    /// shader. Currently: a 256x256 seamless grayscale value-noise PNG
    /// saved to Assets/Art/Fx/noise_tile.png. Imported as a repeating
    /// Default texture the CRT material samples via _NoiseTex.
    /// </summary>
    internal static class FxAssetBootstrap
    {
        const string NoisePath = "Assets/Art/Fx/noise_tile.png";
        const int Size = 256;

        [MenuItem("Tools/Signal Scrubber/Generate Noise Texture")]
        static void GenerateNoiseTexture()
        {
            EnsureDir(Path.GetDirectoryName(NoisePath));
            var tex = BuildSeamlessValueNoise(Size, seed: 1337);
            var bytes = tex.EncodeToPNG();
            File.WriteAllBytes(NoisePath, bytes);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(NoisePath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(NoisePath);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            AssignNoiseToCrtMaterial();
            Debug.Log("[SignalScrubber] Noise texture generated at " + NoisePath);
        }

        static Texture2D BuildSeamlessValueNoise(int size, int seed)
        {
            var tex = new Texture2D(size, size, TextureFormat.R8, mipChain: false, linear: true);
            tex.wrapMode = TextureWrapMode.Repeat;

            // Blend a handful of sine-based frequency bands with hash-jitter
            // seeded from texture coords. Wrapping in trig keeps the result
            // seamless across tile edges.
            var rng = new System.Random(seed);
            var phases = new Vector2[4];
            for (int i = 0; i < phases.Length; i++)
                phases[i] = new Vector2((float)rng.NextDouble() * 6.28f,
                                         (float)rng.NextDouble() * 6.28f);

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size * 6.2831853f;
                float v = (float)y / size * 6.2831853f;
                float n = 0f;
                n += 0.5f  * Mathf.Sin(u * 1f + phases[0].x) * Mathf.Cos(v * 1f + phases[0].y);
                n += 0.3f  * Mathf.Sin(u * 2f + phases[1].x) * Mathf.Cos(v * 2f + phases[1].y);
                n += 0.15f * Mathf.Sin(u * 4f + phases[2].x) * Mathf.Cos(v * 4f + phases[2].y);
                n += 0.10f * Mathf.Sin(u * 8f + phases[3].x) * Mathf.Cos(v * 8f + phases[3].y);

                float hash = HashFloat(x, y) * 0.5f - 0.25f;
                n = Mathf.Clamp01(0.5f + 0.5f * n + hash);
                pixels[y * size + x] = new Color(n, n, n, 1f);
            }
            tex.SetPixels(pixels);
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return tex;
        }

        static float HashFloat(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663);
                h = (h ^ 61u) ^ (h >> 16);
                h *= 9u;
                h = h ^ (h >> 4);
                h *= 0x27d4eb2du;
                h = h ^ (h >> 15);
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        static void AssignNoiseToCrtMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/CRT.mat");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(NoisePath);
            if (mat == null || tex == null) return;
            if (!mat.HasProperty("_NoiseTex")) return;
            mat.SetTexture("_NoiseTex", tex);
            EditorUtility.SetDirty(mat);
        }

        static void EnsureDir(string dir)
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
    }
}
