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
            public string Archive;
        }

        static readonly Seed[] Seeds =
        {
            new Seed { Filename = "Signal_01_Monolith.asset",   Id = "monolith_01",
                       F = 0.30f, N = 0.70f, P = 0.55f,
                       Archive = "Monolith silhouette recovered. Source bearing unknown." },
            new Seed { Filename = "Signal_02_Diagram.asset",    Id = "diagram_02",
                       F = 0.65f, N = 0.35f, P = 0.25f,
                       Archive = "Annotated diagram fragment. Notation unfamiliar." },
            new Seed { Filename = "Signal_03_Silhouette.asset", Id = "silhouette_03",
                       F = 0.85f, N = 0.50f, P = 0.75f,
                       Archive = "Tall figure at the treeline. Still there." },
        };

        [MenuItem("Tools/Signal Scrubber/Create Placeholder Signals")]
        static void CreatePlaceholderSignals()
        {
            if (!Directory.Exists(SignalsDir)) Directory.CreateDirectory(SignalsDir);

            int created = 0;
            foreach (var seed in Seeds)
            {
                var path = $"{SignalsDir}/{seed.Filename}";
                if (AssetDatabase.LoadAssetAtPath<SignalData>(path) != null) continue;

                var asset = ScriptableObject.CreateInstance<SignalData>();
                asset.id = seed.Id;
                asset.targetFrequency = seed.F;
                asset.targetNoise     = seed.N;
                asset.targetPhase     = seed.P;
                asset.archiveNote     = seed.Archive;
                AssetDatabase.CreateAsset(asset, path);
                created++;
            }

            if (created > 0) AssetDatabase.SaveAssets();
            Debug.Log($"[SignalScrubber] Placeholder signals ensured ({created} created).");
        }
    }
}
