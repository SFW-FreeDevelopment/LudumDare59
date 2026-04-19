using SignalScrubber.Audio;
using SignalScrubber.Core;
using SignalScrubber.Polish;
using SignalScrubber.Rendering;
using SignalScrubber.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SignalScrubber.EditorTools
{
    /// <summary>
    /// Creates the scene-level manager GameObjects under the Systems root and
    /// wires their serialized references. Run after 'Build Main Scene' and
    /// 'Scaffold UI Scene'. Grown incrementally across stories S06–S14.
    /// </summary>
    internal static class SystemsBootstrap
    {
        [MenuItem("Tools/Signal Scrubber/Wire Systems")]
        static void WireSystems()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Wire Systems",
                    "No active scene. Open Assets/Scenes/Main.unity first.", "OK");
                return;
            }

            var systems = EnsureRoot("Systems");
            var tuning  = EnsureChildComponent<TuningState>(systems, "TuningState");

            // CrtFrameController.tuning -> TuningState
            var diegetic = FindInScene("UI/DiegeticUI");
            CrtFrameController ctrl = null;
            if (diegetic != null)
            {
                ctrl = diegetic.GetComponent<CrtFrameController>();
                if (ctrl != null)
                    SetSerializedReference(ctrl, "tuning", tuning);

                var waveform = diegetic.GetComponent<WaveformDriver>();
                if (waveform != null)
                    SetSerializedReference(waveform, "tuning", tuning);
            }

            // SignalManager (+ DebugSignalLogger) on its own child, with wired refs.
            var manager = EnsureChildComponent<SignalManager>(systems, "SignalManager");
            if (manager.gameObject.GetComponent<DebugSignalLogger>() == null)
                manager.gameObject.AddComponent<DebugSignalLogger>();
            SetSerializedReference(manager, "tuning", tuning);
            if (ctrl != null) SetSerializedReference(manager, "frame", ctrl);
            SetSerializedArray(manager, "signals", LoadAllSignals());

            // SignalTimer owns the per-level countdown.
            var timer = EnsureChildComponent<SignalTimer>(systems, "SignalTimer");
            SetSerializedReference(timer, "manager", manager);

            // TimerDriver on the DiegeticUI GameObject paints the readout.
            if (diegetic != null)
            {
                var timerDriver = diegetic.GetComponent<TimerDriver>();
                if (timerDriver == null) timerDriver = diegetic.AddComponent<TimerDriver>();
                SetSerializedReference(timerDriver, "timer", timer);
            }

            // SignalRenderer: needs TuningState, SignalManager, and the
            // CrtMaterialBinder sitting on CRT/Screen/ScreenQuad.
            var renderer = EnsureChildComponent<SignalRenderer>(systems, "SignalRenderer");
            var screenQuad = FindInScene("World/CRT/Screen/ScreenQuad");
            var binder = screenQuad != null ? screenQuad.GetComponent<CrtMaterialBinder>() : null;
            SetSerializedReference(renderer, "tuning",  tuning);
            SetSerializedReference(renderer, "manager", manager);
            if (binder != null) SetSerializedReference(renderer, "binder", binder);

            // WaveformDriver.manager -> SignalManager (tuning set above).
            if (diegetic != null)
            {
                var wave = diegetic.GetComponent<WaveformDriver>();
                if (wave != null) SetSerializedReference(wave, "manager", manager);
            }

            // AudioDirector with four child AudioSources.
            var audio = EnsureChildComponent<AudioDirector>(systems, "AudioDirector");
            var staticBed  = EnsureAudioSource(audio.gameObject, "StaticBed", loop: true, playOnAwake: true);
            var humBed     = EnsureAudioSource(audio.gameObject, "HumBed",    loop: true, playOnAwake: true);
            var signalTone = EnsureAudioSource(audio.gameObject, "SignalTone", loop: true, playOnAwake: false);
            var oneShot    = EnsureAudioSource(audio.gameObject, "OneShot",    loop: false, playOnAwake: false);
            SetSerializedReference(audio, "tuning",     tuning);
            SetSerializedReference(audio, "manager",    manager);
            SetSerializedReference(audio, "staticBed",  staticBed);
            SetSerializedReference(audio, "humBed",     humBed);
            SetSerializedReference(audio, "signalTone", signalTone);
            SetSerializedReference(audio, "oneShot",    oneShot);

            // CrtFrameController.audioDirector -> AudioDirector (for detent clicks).
            if (ctrl != null) SetSerializedReference(ctrl, "audioDirector", audio);

            // AmbientFlicker on Systems/AmbientFlicker, wired to the binder
            // and the PowerLed SpriteRenderer on the CRT body.
            var flicker = EnsureChildComponent<AmbientFlicker>(systems, "AmbientFlicker");
            if (binder != null) SetSerializedReference(flicker, "binder", binder);
            var led = FindInScene("World/CRT/Body/PowerLed");
            if (led != null)
            {
                var ledSr = led.GetComponent<SpriteRenderer>();
                if (ledSr != null) SetSerializedReference(flicker, "powerLed", ledSr);
            }

            // LockFlash on Systems/LockFlash, bridging SignalManager,
            // CrtMaterialBinder, and the overlay UIDocument.
            var lockFlash = EnsureChildComponent<LockFlash>(systems, "LockFlash");
            SetSerializedReference(lockFlash, "manager", manager);
            if (binder != null) SetSerializedReference(lockFlash, "binder", binder);
            var overlayGo = FindInScene("UI/OverlayUI");
            UnityEngine.UIElements.UIDocument overlayDoc = null;
            if (overlayGo != null)
            {
                overlayDoc = overlayGo.GetComponent<UnityEngine.UIElements.UIDocument>();
                if (overlayDoc != null) SetSerializedReference(lockFlash, "overlayDocument", overlayDoc);
            }

            // IntroOutroController drives Begin() on intro dismissal and the
            // outro card on OnRunCompleted.
            var intro = EnsureChildComponent<IntroOutroController>(systems, "IntroOutroController");
            if (overlayDoc != null) SetSerializedReference(intro, "overlayDocument", overlayDoc);
            SetSerializedReference(intro, "manager", manager);
            if (diegetic != null) SetSerializedReference(intro, "controlsToDisable", diegetic);

            // Minimal no-dependency input poller as a belts-and-braces
            // fallback in case IntroOutroController's UI Toolkit wiring
            // ever breaks. Safe to leave in shipping build.
            EnsureChildComponent<EmergencyStarter>(systems, "EmergencyStarter");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SignalScrubber] Systems wired.");
        }

        static SignalData[] LoadAllSignals()
        {
            var guids = AssetDatabase.FindAssets("t:SignalData", new[] { "Assets/ScriptableObjects/Signals" });
            var list = new System.Collections.Generic.List<SignalData>(guids.Length);
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var s = AssetDatabase.LoadAssetAtPath<SignalData>(path);
                if (s != null) list.Add(s);
            }
            // Sort by filename for stable ordering (Signal_01, Signal_02, ...).
            list.Sort((a, b) => string.Compare(
                AssetDatabase.GetAssetPath(a),
                AssetDatabase.GetAssetPath(b),
                System.StringComparison.Ordinal));
            return list.ToArray();
        }

        static void SetSerializedArray(Object target, string propertyPath, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyPath);
            if (prop == null || !prop.isArray)
            {
                Debug.LogWarning($"[SignalScrubber] {target.GetType().Name}.{propertyPath} is not an array.");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- helpers ----------

        static GameObject EnsureRoot(string name)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == name) return go;
            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        static T EnsureChildComponent<T>(GameObject parent, string childName)
            where T : Component
        {
            var t = parent.transform.Find(childName);
            GameObject go = t != null ? t.gameObject : new GameObject(childName);
            if (t == null) go.transform.SetParent(parent.transform, false);
            var existing = go.GetComponent<T>();
            if (existing != null) return existing;
            return go.AddComponent<T>();
        }

        static AudioSource EnsureAudioSource(GameObject parent, string childName,
            bool loop, bool playOnAwake)
        {
            var src = EnsureChildComponent<AudioSource>(parent, childName);
            src.loop = loop;
            src.playOnAwake = playOnAwake;
            src.spatialBlend = 0f; // 2D
            return src;
        }

        static GameObject FindInScene(string path)
        {
            var parts = path.Split('/');
            var scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name != parts[0]) continue;
                var t = go.transform;
                for (int i = 1; i < parts.Length && t != null; i++)
                    t = t.Find(parts[i]);
                return t != null ? t.gameObject : null;
            }
            return null;
        }

        static void SetSerializedReference(Object target, string propertyPath, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyPath);
            if (prop == null)
            {
                Debug.LogWarning($"[SignalScrubber] {target.GetType().Name} has no field '{propertyPath}'.");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
