using System.IO;
using SignalScrubber.Core;
using UnityEditor;
using UnityEngine;

namespace SignalScrubber.EditorTools
{
    /// <summary>
    /// Creates the three placeholder <see cref="SignalData"/> assets used by
    /// the M1 loop. Target tunings are spread across the (freq, noise, phase)
    /// cube so each signal requires a visibly distinct control configuration.
    /// Idempotent; leaves existing assets untouched.
    /// </summary>
    internal static class SignalAssetBootstrap
    {
        const string SignalsDir = "Assets/ScriptableObjects/Signals";
        const string PlaceholderHiddenImagePath = "Assets/Art/scene01.png";

        struct Seed
        {
            public string Filename;
            public string Id;
            public float F, N, P;
            public float Inner;
            public float Sharpness;
            public float Seconds;
            public string Archive;
        }

        // Difficulty ramps across the three signals. Each level tightens the
        // inner plateau and steepens the outside falloff.
        //   L1  inner ±0.08, sharpness 1.8, 40 s — generous landing zone,
        //        gentle shoulder; reads almost linear in the falloff.
        //   L2  inner ±0.05, sharpness 1.2, 30 s — standard
        //   L3  inner ±0.025, sharpness 0.8, 22 s — tight plateau, punchy
        //        drop-off right off the peak.
        static readonly Seed[] Seeds =
        {
            new Seed { Filename = "Signal_01_Monolith.asset",   Id = "monolith_01",
                       F = 0.30f, N = 0.70f, P = 0.55f,
                       Inner = 0.08f, Sharpness = 1.8f, Seconds = 40f,
                       Archive = "Monolith silhouette recovered. Source bearing unknown." },
            new Seed { Filename = "Signal_02_Diagram.asset",    Id = "diagram_02",
                       F = 0.65f, N = 0.35f, P = 0.25f,
                       Inner = 0.05f, Sharpness = 1.2f, Seconds = 30f,
                       Archive = "Annotated diagram fragment. Notation unfamiliar." },
            new Seed { Filename = "Signal_03_Silhouette.asset", Id = "silhouette_03",
                       F = 0.85f, N = 0.50f, P = 0.75f,
                       Inner = 0.025f, Sharpness = 0.8f, Seconds = 22f,
                       Archive = "Tall figure at the treeline. Still there." },
        };

        [MenuItem("Tools/Signal Scrubber/Create Placeholder Signals")]
        static void CreatePlaceholderSignals()
        {
            if (!Directory.Exists(SignalsDir)) Directory.CreateDirectory(SignalsDir);

            var placeholderSprite = LoadSpriteWithFallback(PlaceholderHiddenImagePath);

            int created = 0;
            int updated = 0;
            foreach (var seed in Seeds)
            {
                var path = $"{SignalsDir}/{seed.Filename}";
                var asset = AssetDatabase.LoadAssetAtPath<SignalData>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<SignalData>();
                    asset.id = seed.Id;
                    asset.targetFrequency = seed.F;
                    asset.targetNoise     = seed.N;
                    asset.targetPhase     = seed.P;
                    asset.archiveNote     = seed.Archive;
                    asset.innerTolerance  = seed.Inner;
                    asset.sharpness       = seed.Sharpness;
                    asset.allottedSeconds = seed.Seconds;
                    if (placeholderSprite != null) asset.hiddenImage = placeholderSprite;
                    AssetDatabase.CreateAsset(asset, path);
                    created++;
                }
                else
                {
                    // Always re-apply the seeded difficulty curve because we
                    // just reworked the math — old values (sharpness 0.3/0.5
                    // /0.7 with no plateau) were unplayably tight. Target
                    // tunings and archive copy are left alone so designer
                    // edits survive.
                    asset.innerTolerance  = seed.Inner;
                    asset.sharpness       = seed.Sharpness;
                    asset.allottedSeconds = seed.Seconds;

                    // Fill the hidden image slot if it's empty or still the
                    // previously-wired placeholder. Designer-assigned art is
                    // preserved once any of the hidden images is unique.
                    if (placeholderSprite != null
                        && (asset.hiddenImage == null || asset.hiddenImage == placeholderSprite))
                    {
                        asset.hiddenImage = placeholderSprite;
                    }

                    EditorUtility.SetDirty(asset);
                    updated++;
                }
            }

            if (created > 0 || updated > 0) AssetDatabase.SaveAssets();
            Debug.Log($"[SignalScrubber] Placeholder signals: {created} created, {updated} migrated. " +
                      $"hiddenImage = {(placeholderSprite != null ? placeholderSprite.name : "<missing>")}");
        }

        /// <summary>
        /// Loads a Sprite from a PNG. Handles the case where Unity imports
        /// the texture in spriteMode: Multiple, which makes
        /// LoadAssetAtPath&lt;Sprite&gt; return null — in that case we walk
        /// the sub-assets and return the first Sprite.
        /// </summary>
        static Sprite LoadSpriteWithFallback(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null) return sprite;

            var all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var obj in all)
                if (obj is Sprite s) return s;

            Debug.LogWarning($"[SignalScrubber] No Sprite found at {assetPath}");
            return null;
        }
    }
}
