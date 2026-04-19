using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SignalScrubber.UI.Editor
{
    /// <summary>
    /// Creates the two PanelSettings assets required by S02 if they are missing.
    /// Ensures the diegetic and overlay UIDocuments exist in Main.unity via an
    /// explicit menu action (Tools > Signal Scrubber > Scaffold UI Scene).
    /// Safe to run multiple times; all operations are idempotent.
    /// </summary>
    [InitializeOnLoad]
    internal static class UIScaffoldingSetup
    {
        const string DiegeticPath = "Assets/UI/Diegetic.asset";
        const string OverlayPath  = "Assets/UI/Overlay.asset";
        const string CrtFrameUxmlPath = "Assets/UI/Documents/CrtFrame.uxml";
        const string OverlayUxmlPath  = "Assets/UI/Documents/Overlay.uxml";

        static UIScaffoldingSetup()
        {
            EditorApplication.delayCall += EnsurePanelSettings;
        }

        // Game is designed around a 16:9 aspect ratio at 960 px height.
        // Reference panel resolution matches that so ScaleWithScreenSize +
        // match-height scales the UI uniformly across monitors.
        static readonly Vector2Int ReferenceResolution = new Vector2Int(1707, 960);

        static void EnsurePanelSettings()
        {
            EnsurePanel(DiegeticPath, "Diegetic", ConfigureDiegetic);
            EnsurePanel(OverlayPath,  "Overlay",  ConfigureOverlay);
            AssetDatabase.SaveAssets();
        }

        static void EnsurePanel(string path, string name, System.Action<PanelSettings> configure)
        {
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (ps == null)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                ps.name = name;
                EnsureDir(Path.GetDirectoryName(path));
                AssetDatabase.CreateAsset(ps, path);
            }
            configure(ps);
            EditorUtility.SetDirty(ps);
        }

        static void ConfigureDiegetic(PanelSettings ps)
        {
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = ReferenceResolution;
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 1f; // match height — game is locked at 960 px design height
            ps.sortingOrder = 0;
        }

        static void ConfigureOverlay(PanelSettings ps)
        {
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = ReferenceResolution;
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 1f;
            ps.sortingOrder = 10;
        }

        static void EnsureDir(string dir)
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        [MenuItem("Tools/Signal Scrubber/Scaffold UI Scene")]
        static void ScaffoldUIScene()
        {
            EnsurePanelSettings();

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Scaffold UI Scene",
                    "No active scene. Open Assets/Scenes/Main.unity first.", "OK");
                return;
            }

            var diegeticPanel = AssetDatabase.LoadAssetAtPath<PanelSettings>(DiegeticPath);
            var overlayPanel  = AssetDatabase.LoadAssetAtPath<PanelSettings>(OverlayPath);
            var crtFrameUxml  = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CrtFrameUxmlPath);
            var overlayUxml   = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(OverlayUxmlPath);

            var uiRoot = FindOrCreateRoot(scene, "UI");
            var diegetic = EnsureUIDocumentChild(uiRoot, "DiegeticUI", diegeticPanel, crtFrameUxml);
            EnsureUIDocumentChild(uiRoot, "OverlayUI",  overlayPanel,  overlayUxml);

            if (diegetic.GetComponent<CrtFrameController>() == null)
                diegetic.AddComponent<CrtFrameController>();
            if (diegetic.GetComponent<WaveformDriver>() == null)
                diegetic.AddComponent<WaveformDriver>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SignalScrubber] UI scaffolding complete.");
        }

        static GameObject FindOrCreateRoot(Scene scene, string name)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == name) return go;

            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        static GameObject EnsureUIDocumentChild(GameObject parent, string childName,
            PanelSettings panel, VisualTreeAsset uxml)
        {
            var existing = parent.transform.Find(childName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(childName);
            if (existing == null) go.transform.SetParent(parent.transform, false);

            var existing2 = go.GetComponent<UIDocument>();
            var doc = existing2 != null ? existing2 : go.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.visualTreeAsset = uxml;
            return go;
        }
    }
}
