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

        struct Seed
        {
            public string Filename;
            public string Id;
            public float F, N, P;
            public float Sharpness;
            public float Seconds;
            public string Archive;
        }

        // Difficulty ramps across the three signals:
        //   level 1 — forgiving curve (sharpness 0.7), generous time
        //   level 2 — medium         (sharpness 0.5)
        //   level 3 — sharp peak     (sharpness 0.3), tight time
        static readonly Seed[] Seeds =
        {
            new Seed { Filename = "Signal_01_Monolith.asset",   Id = "monolith_01",
                       F = 0.30f, N = 0.70f, P = 0.55f,
                       Sharpness = 0.7f, Seconds = 40f,
                       Archive = "Monolith silhouette recovered. Source bearing unknown." },
            new Seed { Filename = "Signal_02_Diagram.asset",    Id = "diagram_02",
                       F = 0.65f, N = 0.35f, P = 0.25f,
                       Sharpness = 0.5f, Seconds = 30f,
                       Archive = "Annotated diagram fragment. Notation unfamiliar." },
            new Seed { Filename = "Signal_03_Silhouette.asset", Id = "silhouette_03",
                       F = 0.85f, N = 0.50f, P = 0.75f,
                       Sharpness = 0.3f, Seconds = 22f,
                       Archive = "Tall figure at the treeline. Still there." },
        };

        [MenuItem("Tools/Signal Scrubber/Create Placeholder Signals")]
        static void CreatePlaceholderSignals()
        {
            if (!Directory.Exists(SignalsDir)) Directory.CreateDirectory(SignalsDir);

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
                    asset.sharpness       = seed.Sharpness;
                    asset.allottedSeconds = seed.Seconds;
                    AssetDatabase.CreateAsset(asset, path);
                    created++;
                }
                else
                {
                    // Keep existing target tunings + archive note untouched
                    // (designer may have edited them) but migrate the new
                    // difficulty-curve fields if they are still at the
                    // old defaults.
                    bool dirty = false;
                    if (Mathf.Approximately(asset.sharpness, 0.5f))
                    {
                        asset.sharpness = seed.Sharpness;
                        dirty = true;
                    }
                    if (Mathf.Approximately(asset.allottedSeconds, 30f))
                    {
                        asset.allottedSeconds = seed.Seconds;
                        dirty = true;
                    }
                    if (dirty) { EditorUtility.SetDirty(asset); updated++; }
                }
            }

            if (created > 0 || updated > 0) AssetDatabase.SaveAssets();
            Debug.Log($"[SignalScrubber] Placeholder signals: {created} created, {updated} migrated.");
        }
    }
}
