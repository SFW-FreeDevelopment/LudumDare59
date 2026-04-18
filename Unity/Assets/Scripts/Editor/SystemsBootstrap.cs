using SignalScrubber.Core;
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
            return go.GetComponent<T>() ?? go.AddComponent<T>();
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
