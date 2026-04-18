using SignalScrubber.Core;
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
            if (diegetic != null)
            {
                var ctrl = diegetic.GetComponent<CrtFrameController>();
                if (ctrl != null)
                    SetSerializedReference(ctrl, "tuning", tuning);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SignalScrubber] Systems wired.");
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
