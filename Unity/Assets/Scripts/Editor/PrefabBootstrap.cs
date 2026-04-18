using System.IO;
using SignalScrubber.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SignalScrubber.EditorTools
{
    /// <summary>
    /// Refines the CRT and Desk hierarchies created by SceneBootstrap into
    /// artist-friendly slot layouts (named SpriteRenderer children with null
    /// sprites), materializes the real CRT.mat URP/Unlit material for the
    /// screen quad, re-parents DiegeticUI under Screen/DiegeticUIAnchor, and
    /// saves both hierarchies as prefabs under Assets/Prefabs.
    ///
    /// Idempotent; run after SceneBootstrap.
    /// </summary>
    internal static class PrefabBootstrap
    {
        const string PrefabsDir = "Assets/Prefabs";
        const string CrtPrefabPath  = "Assets/Prefabs/CRT.prefab";
        const string DeskPrefabPath = "Assets/Prefabs/Desk.prefab";
        const string CrtMaterialPath = "Assets/Materials/CRT.mat";

        [MenuItem("Tools/Signal Scrubber/Build Prefabs")]
        static void BuildPrefabs()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Build Prefabs",
                    "No active scene. Open Assets/Scenes/Main.unity first.", "OK");
                return;
            }

            EnsureDir(PrefabsDir);
            EnsureCrtMaterial();

            var crt  = FindRootChild("World", "CRT");
            var desk = FindRootChild("World", "Desk");
            if (crt == null || desk == null)
            {
                EditorUtility.DisplayDialog("Build Prefabs",
                    "CRT or Desk root missing. Run 'Build Main Scene' first.", "OK");
                return;
            }

            RefineCrt(crt);
            RefineDesk(desk);
            ReparentDiegeticUnderCrt(crt);

            PrefabUtility.SaveAsPrefabAssetAndConnect(crt,  CrtPrefabPath,  InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(desk, DeskPrefabPath, InteractionMode.AutomatedAction);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SignalScrubber] Prefabs built: CRT, Desk.");
        }

        static void RefineCrt(GameObject crt)
        {
            var body       = crt.transform.Find("Body")?.gameObject        ?? EnsureChild(crt, "Body");
            var screen     = crt.transform.Find("Screen")?.gameObject      ?? EnsureChild(crt, "Screen");
            var foreground = crt.transform.Find("Foreground")?.gameObject  ?? EnsureChild(crt, "Foreground");

            // Body: FrameSprite + PowerLed (SpriteRenderer slots, null sprites).
            EnsureSpriteSlot(body, "FrameSprite", localPos: new Vector3(0f, 0f, -0.01f),
                             sortingOrder: 0, color: Color.white);
            var led = EnsureSpriteSlot(body, "PowerLed",
                                       localPos: new Vector3(3.2f, 2.2f, -0.02f),
                                       sortingOrder: 1,
                                       color: new Color(1f, 0.25f, 0.2f, 0.9f));
            // Small scale for the LED slot until art arrives.
            led.transform.localScale = new Vector3(0.15f, 0.15f, 1f);

            // Screen: ScreenQuad (MeshRenderer) + DiegeticUIAnchor (empty).
            var screenQuad = screen.transform.Find("ScreenQuad")?.gameObject
                             ?? EnsureChild(screen, "ScreenQuad");
            screenQuad.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            screenQuad.transform.localScale    = new Vector3(6.8f, 4.0f, 1f);
            var mf = screenQuad.GetComponent<MeshFilter>() ?? screenQuad.AddComponent<MeshFilter>();
            if (mf.sharedMesh == null) mf.sharedMesh = GetQuadMesh();
            var mr = screenQuad.GetComponent<MeshRenderer>() ?? screenQuad.AddComponent<MeshRenderer>();
            mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(CrtMaterialPath);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            if (screenQuad.GetComponent<CrtMaterialBinder>() == null)
                screenQuad.AddComponent<CrtMaterialBinder>();

            EnsureChild(screen, "DiegeticUIAnchor");

            // Remove obsolete S03 placeholder ScreenPlate if present.
            var oldPlate = screen.transform.Find("ScreenPlate");
            if (oldPlate != null) Object.DestroyImmediate(oldPlate.gameObject);
            var oldBodyPlate = body.transform.Find("BodyPlate");
            if (oldBodyPlate != null) Object.DestroyImmediate(oldBodyPlate.gameObject);

            // Foreground: Glass slot.
            var glass = EnsureSpriteSlot(foreground, "Glass",
                                         localPos: new Vector3(0f, 0f, -0.2f),
                                         sortingOrder: 10, color: new Color(1f,1f,1f,0f));
            glass.transform.localScale = new Vector3(1f, 1f, 1f);

            var oldGlassPlate = foreground.transform.Find("GlassPlate");
            if (oldGlassPlate != null) Object.DestroyImmediate(oldGlassPlate.gameObject);
        }

        static void RefineDesk(GameObject desk)
        {
            EnsureSpriteSlot(desk, "DeskSurface",
                             localPos: new Vector3(0f, 0f, 0f),
                             sortingOrder: 0, color: Color.white);

            var clutter = desk.transform.Find("Clutter")?.gameObject ?? EnsureChild(desk, "Clutter");
            string[] clutterSlots = { "Mug", "Keyboard", "Papers", "Tapes", "StickyNotes", "Books" };
            int order = 1;
            foreach (var name in clutterSlots)
            {
                EnsureSpriteSlot(clutter, name,
                                 localPos: new Vector3((order - 3.5f) * 0.6f, 0.1f, -0.01f),
                                 sortingOrder: order, color: Color.white);
                order++;
            }

            var oldPlate = desk.transform.Find("DeskPlate");
            if (oldPlate != null) Object.DestroyImmediate(oldPlate.gameObject);
        }

        static void ReparentDiegeticUnderCrt(GameObject crt)
        {
            var anchor = crt.transform.Find("Screen/DiegeticUIAnchor");
            if (anchor == null) return;

            var scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name != "UI") continue;
                var diegetic = go.transform.Find("DiegeticUI");
                if (diegetic != null && diegetic.parent != anchor)
                {
                    diegetic.SetParent(anchor, worldPositionStays: true);
                    diegetic.localPosition = Vector3.zero;
                }
            }
        }

        static void EnsureCrtMaterial()
        {
            // Prefer the real CRT shader if it has imported; fall back to the
            // URP/Unlit placeholder so the scene still renders pre-S10.
            var crtShader = Shader.Find("SignalScrubber/CRT");
            var shader = crtShader != null ? crtShader : Shader.Find("Universal Render Pipeline/Unlit");

            var mat = AssetDatabase.LoadAssetAtPath<Material>(CrtMaterialPath);
            if (mat == null)
            {
                EnsureDir(Path.GetDirectoryName(CrtMaterialPath));
                mat = new Material(shader) { name = "CRT" };
                mat.SetColor("_BaseColor", new Color(0.03f, 0.08f, 0.04f, 1f));
                AssetDatabase.CreateAsset(mat, CrtMaterialPath);
                return;
            }

            // Upgrade path: swap placeholder URP/Unlit for the real shader
            // once it has imported.
            if (mat.shader != shader)
            {
                mat.shader = shader;
                EditorUtility.SetDirty(mat);
            }
        }

        // ---------- helpers ----------

        static GameObject FindRootChild(string rootName, string childName)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == rootName)
                    return go.transform.Find(childName)?.gameObject;
            return null;
        }

        static GameObject EnsureChild(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static GameObject EnsureSpriteSlot(GameObject parent, string name,
            Vector3 localPos, int sortingOrder, Color color)
        {
            var go = EnsureChild(parent, name);
            go.transform.localPosition = localPos;
            if (go.transform.localScale == Vector3.zero)
                go.transform.localScale = Vector3.one;
            var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            sr.color = color;
            // Intentionally leave sr.sprite null — artist drops in.
            return go;
        }

        static Mesh _quad;
        static Mesh GetQuadMesh()
        {
            if (_quad != null) return _quad;
            var prim = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _quad = prim.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(prim);
            return _quad;
        }

        static void EnsureDir(string dir)
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
    }
}
