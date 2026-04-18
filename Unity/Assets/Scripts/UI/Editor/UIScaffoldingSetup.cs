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

        static void EnsurePanelSettings()
        {
            bool changed = false;
            if (AssetDatabase.LoadAssetAtPath<PanelSettings>(DiegeticPath) == null)
            {
                CreateDiegeticPanelSettings();
                changed = true;
            }
            if (AssetDatabase.LoadAssetAtPath<PanelSettings>(OverlayPath) == null)
            {
                CreateOverlayPanelSettings();
                changed = true;
            }
            if (changed) AssetDatabase.SaveAssets();
        }

        static void CreateDiegeticPanelSettings()
        {
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.name = "Diegetic";
            ps.scaleMode = PanelScaleMode.ConstantPixelSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.sortingOrder = 0;
            EnsureDir(Path.GetDirectoryName(DiegeticPath));
            AssetDatabase.CreateAsset(ps, DiegeticPath);
        }

        static void CreateOverlayPanelSettings()
        {
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.name = "Overlay";
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            ps.sortingOrder = 10;
            EnsureDir(Path.GetDirectoryName(OverlayPath));
            AssetDatabase.CreateAsset(ps, OverlayPath);
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
            EnsureUIDocumentChild(uiRoot, "DiegeticUI", diegeticPanel, crtFrameUxml);
            EnsureUIDocumentChild(uiRoot, "OverlayUI",  overlayPanel,  overlayUxml);

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

        static void EnsureUIDocumentChild(GameObject parent, string childName,
            PanelSettings panel, VisualTreeAsset uxml)
        {
            var existing = parent.transform.Find(childName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(childName);
            if (existing == null) go.transform.SetParent(parent.transform, false);

            var doc = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.visualTreeAsset = uxml;
        }
    }
}
