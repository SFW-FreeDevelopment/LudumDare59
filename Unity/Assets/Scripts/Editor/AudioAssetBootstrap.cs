using System.IO;
using SignalScrubber.Audio;
using SignalScrubber.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SignalScrubber.EditorTools
{
    /// <summary>
    /// Applies correct Unity import settings to every audio file under
    /// Assets/Audio/ (streaming + Vorbis for long loops, decompress-on-load
    /// for one-shots), creates the DeskAmbience AudioSource if missing,
    /// auto-assigns every clip to its AudioDirector slot by filename, and
    /// wires the three per-signal tones to their matching SignalData
    /// ScriptableObjects.
    ///
    /// Idempotent — safe to re-run after dropping more audio in.
    /// </summary>
    internal static class AudioAssetBootstrap
    {
        const string BedsDir    = "Assets/Audio/Beds";
        const string SfxDir     = "Assets/Audio/SFX";
        const string SignalsDir = "Assets/Audio/Signals";
        const string SignalsSoDir = "Assets/ScriptableObjects/Signals";

        [MenuItem("Tools/Signal Scrubber/Wire Audio")]
        static void WireAudio()
        {
            ApplyImportSettings();
            WireAudioDirector();
            WireSignalTones();
            AssetDatabase.SaveAssets();
            Debug.Log("[SignalScrubber] Audio wired.");
        }

        // ---------- 1. Import settings ----------

        static void ApplyImportSettings()
        {
            // Long loops → streaming + compressed, don't need to sit in RAM.
            ConfigureAudioDir(BedsDir,    AudioClipLoadType.Streaming,
                              AudioCompressionFormat.Vorbis, quality: 0.7f,
                              preload: false);
            ConfigureAudioDir(SignalsDir, AudioClipLoadType.Streaming,
                              AudioCompressionFormat.Vorbis, quality: 0.7f,
                              preload: false);

            // One-shots fire instantly, no stutter — decompress on load.
            ConfigureAudioDir(SfxDir,     AudioClipLoadType.DecompressOnLoad,
                              AudioCompressionFormat.Vorbis, quality: 1.0f,
                              preload: true);
        }

        static void ConfigureAudioDir(string dir, AudioClipLoadType load,
            AudioCompressionFormat format, float quality, bool preload)
        {
            if (!Directory.Exists(dir)) return;
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { dir });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AudioImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;

                var sample = importer.defaultSampleSettings;
                sample.loadType = load;
                sample.compressionFormat = format;
                sample.quality = quality;
                sample.preloadAudioData = preload;
                importer.defaultSampleSettings = sample;

                importer.forceToMono = false;
                importer.loadInBackground = !preload;

                importer.SaveAndReimport();
            }
        }

        // ---------- 2. AudioDirector slot wiring ----------

        static void WireAudioDirector()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            var director = Object.FindFirstObjectByType<AudioDirector>();
            if (director == null)
            {
                Debug.LogWarning("[SignalScrubber] No AudioDirector in scene — run Wire Systems first.");
                return;
            }

            // Ensure all four bed AudioSources exist as children of the
            // AudioDirector GameObject.
            EnsureAudioSource(director.gameObject, "StaticBed",     loop: true,  playOnAwake: true);
            EnsureAudioSource(director.gameObject, "HumBed",        loop: true,  playOnAwake: true);
            EnsureAudioSource(director.gameObject, "DeskAmbience",  loop: true,  playOnAwake: true);
            EnsureAudioSource(director.gameObject, "SignalTone",    loop: true,  playOnAwake: false);
            EnsureAudioSource(director.gameObject, "OneShot",       loop: false, playOnAwake: false);

            // Wire each slot on AudioDirector.
            var so = new SerializedObject(director);
            SetSrc(so, "staticBed",    director.gameObject, "StaticBed");
            SetSrc(so, "humBed",       director.gameObject, "HumBed");
            SetSrc(so, "deskAmbience", director.gameObject, "DeskAmbience");
            SetSrc(so, "signalTone",   director.gameObject, "SignalTone");
            SetSrc(so, "oneShot",      director.gameObject, "OneShot");

            SetClip(so, "click",        $"{SfxDir}/sfx_click.wav");
            SetClip(so, "lockSuccess",  $"{SfxDir}/sfx_lock_success.wav");
            SetClip(so, "lockPartial",  $"{SfxDir}/sfx_lock_partial.wav");
            SetClip(so, "lockFail",     $"{SfxDir}/sfx_lock_fail.wav");
            SetClip(so, "timerTick",    $"{SfxDir}/sfx_timer_tick.wav");
            SetClip(so, "powerOn",      $"{SfxDir}/sfx_power_on.wav");
            SetClip(so, "broadcastEnd", $"{SfxDir}/sfx_broadcast_end.wav");

            // Also wire the SignalTimer ref if it's already in the scene.
            var timer = Object.FindFirstObjectByType<SignalTimer>();
            if (timer != null) SetObjectRef(so, "timer", timer);

            so.ApplyModifiedPropertiesWithoutUndo();

            // Pre-load the bed clips onto their sources so play-on-awake works.
            AssignBedClip(director.gameObject, "StaticBed",    $"{BedsDir}/bed_static.wav");
            AssignBedClip(director.gameObject, "HumBed",       $"{BedsDir}/bed_hum.wav");
            AssignBedClip(director.gameObject, "DeskAmbience", $"{BedsDir}/bed_desk_ambience.wav");

            // Hook AudioDirector reference into IntroOutroController if present.
            var intro = Object.FindFirstObjectByType<SignalScrubber.Polish.IntroOutroController>();
            if (intro != null)
            {
                var introSo = new SerializedObject(intro);
                var prop = introSo.FindProperty("audioDirector");
                if (prop != null)
                {
                    prop.objectReferenceValue = director;
                    introSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void EnsureAudioSource(GameObject parent, string childName,
            bool loop, bool playOnAwake)
        {
            var t = parent.transform.Find(childName);
            GameObject go = t != null ? t.gameObject : new GameObject(childName);
            if (t == null) go.transform.SetParent(parent.transform, false);

            var src = go.GetComponent<AudioSource>();
            if (src == null) src = go.AddComponent<AudioSource>();
            src.loop = loop;
            src.playOnAwake = playOnAwake;
            src.spatialBlend = 0f;
        }

        static void AssignBedClip(GameObject parent, string childName, string clipPath)
        {
            var t = parent.transform.Find(childName);
            if (t == null) return;
            var src = t.GetComponent<AudioSource>();
            if (src == null) return;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null) return;
            if (src.clip == clip) return;
            src.clip = clip;
            EditorUtility.SetDirty(src);
        }

        static void SetSrc(SerializedObject so, string property,
            GameObject parent, string childName)
        {
            var t = parent.transform.Find(childName);
            if (t == null) return;
            var src = t.GetComponent<AudioSource>();
            SetObjectRef(so, property, src);
        }

        static void SetClip(SerializedObject so, string property, string assetPath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            SetObjectRef(so, property, clip);
        }

        static void SetObjectRef(SerializedObject so, string property, Object value)
        {
            var prop = so.FindProperty(property);
            if (prop == null) return;
            prop.objectReferenceValue = value;
        }

        // ---------- 3. Per-signal tone wiring ----------

        static void WireSignalTones()
        {
            // Filename convention: Signal_0N_Foo.asset  <-  signal_foo.wav
            Map("Signal_01_Monolith.asset",   "signal_monolith.wav");
            Map("Signal_02_Diagram.asset",    "signal_diagram.wav");
            Map("Signal_03_Silhouette.asset", "signal_silhouette.wav");
        }

        static void Map(string signalAssetFilename, string clipFilename)
        {
            var signal = AssetDatabase.LoadAssetAtPath<SignalData>(
                $"{SignalsSoDir}/{signalAssetFilename}");
            var clip   = AssetDatabase.LoadAssetAtPath<AudioClip>(
                $"{SignalsDir}/{clipFilename}");
            if (signal == null || clip == null) return;
            if (signal.signalTone == clip) return;

            var so = new SerializedObject(signal);
            var prop = so.FindProperty("signalTone");
            if (prop == null) return;
            prop.objectReferenceValue = clip;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(signal);
        }
    }
}
