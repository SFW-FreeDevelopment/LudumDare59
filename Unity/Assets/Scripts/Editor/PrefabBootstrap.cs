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

            // Body: FrameSprite + PowerLed (SpriteRenderer slots). FrameSprite
            // auto-picks up the CRT-monitor art if the artist has dropped it in.
            var frame = EnsureSpriteSlot(body, "FrameSprite", localPos: new Vector3(0f, 0f, -0.01f),
                             sortingOrder: 0, color: Color.white);
            AutoAssignSprite(frame, "Assets/Art/CRT/CRT-monitor.png");
            bool ledExisted = body.transform.Find("PowerLed") != null;
            var led = EnsureSpriteSlot(body, "PowerLed",
                                       localPos: new Vector3(3.2f, 2.2f, -0.02f),
                                       sortingOrder: 1,
                                       color: new Color(1f, 0.25f, 0.2f, 0.9f));
            // Seed a small initial scale only on first creation.
            if (!ledExisted)
                led.transform.localScale = new Vector3(0.15f, 0.15f, 1f);

            // Screen: ScreenQuad (MeshRenderer) + DiegeticUIAnchor (empty).
            // Transform is seeded once on creation; any later manual layout
            // tweaks in the scene must survive re-runs of Build Prefabs.
            var screenQuad = EnsureChildTracked(screen, "ScreenQuad", out bool screenQuadCreated);
            if (screenQuadCreated)
            {
                screenQuad.transform.localPosition = new Vector3(0f, -0.6f, -0.1f);
                screenQuad.transform.localScale    = new Vector3(5.2f, 3.2f, 1f);
            }
            var mf = GetOrAdd<MeshFilter>(screenQuad);
            if (mf.sharedMesh == null) mf.sharedMesh = GetQuadMesh();
            var mr = GetOrAdd<MeshRenderer>(screenQuad);
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
            bool glassExisted = foreground.transform.Find("Glass") != null;
            var glass = EnsureSpriteSlot(foreground, "Glass",
                                         localPos: new Vector3(0f, 0f, -0.2f),
                                         sortingOrder: 10, color: new Color(1f,1f,1f,0f));
            if (!glassExisted)
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
            string[] clutterSlots =
            {
                "Mug", "Keyboard", "Papers", "Tapes",
                "StickyNote1", "StickyNote2", "Books",
            };
            int order = 1;
            foreach (var name in clutterSlots)
            {
                EnsureSpriteSlot(clutter, name,
                                 localPos: new Vector3((order - 4f) * 0.6f, 0.1f, -0.01f),
                                 sortingOrder: order, color: Color.white);
                order++;
            }

            // Auto-assign the artist-supplied sticky note PNGs if present.
            var note1 = clutter.transform.Find("StickyNote1")?.gameObject;
            var note2 = clutter.transform.Find("StickyNote2")?.gameObject;
            if (note1 != null) AutoAssignSprite(note1, "Assets/Art/CRT/sticky-note-1.png");
            if (note2 != null) AutoAssignSprite(note2, "Assets/Art/CRT/sticky-note-2.png");

            // Legacy slot from the pre-split layout. Delete if present to
            // avoid confusion with the new StickyNote1 / StickyNote2 pair.
            var legacy = clutter.transform.Find("StickyNotes");
            if (legacy != null) Object.DestroyImmediate(legacy.gameObject);

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
            return EnsureChildTracked(parent, name, out _);
        }

        /// <summary>
        /// Like EnsureChild but reports whether the object was just created.
        /// Callers that want to initialise transform-y things only on first
        /// creation (so manual layout tweaks aren't clobbered on re-runs of
        /// Build Prefabs) use the out flag to gate those writes.
        /// </summary>
        static GameObject EnsureChildTracked(GameObject parent, string name, out bool created)
        {
            var t = parent.transform.Find(name);
            if (t != null) { created = false; return t.gameObject; }
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            created = true;
            return go;
        }

        // Unity's "fake null" breaks `??` — use overloaded == which treats
        // destroyed components as null.
        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        static GameObject EnsureSpriteSlot(GameObject parent, string name,
            Vector3 localPos, int sortingOrder, Color color)
        {
            var go = EnsureChildTracked(parent, name, out bool created);

            // Only seed transform / sort / tint on first creation. Re-runs
            // of Build Prefabs must not clobber manual layout tweaks the
            // designer has made in the scene.
            if (created)
            {
                go.transform.localPosition = localPos;
                if (go.transform.localScale == Vector3.zero)
                    go.transform.localScale = Vector3.one;
            }

            var existing = go.GetComponent<SpriteRenderer>();
            var sr = existing != null ? existing : go.AddComponent<SpriteRenderer>();
            if (created)
            {
                sr.sortingOrder = sortingOrder;
                sr.color = color;
            }
            // Intentionally leave sr.sprite null — artist drops in, or
            // AutoAssignSprite wires it after this call.
            return go;
        }

        static void AutoAssignSprite(GameObject slot, string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                // Unity imports PNGs in Multiple sprite mode with no primary
                // sprite, so LoadAssetAtPath<Sprite> returns null. Fall back
                // to walking all sub-assets and grabbing the first Sprite.
                var all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var obj in all)
                {
                    if (obj is Sprite s) { sprite = s; break; }
                }
            }
            if (sprite == null)
            {
                Debug.LogWarning($"[SignalScrubber] AutoAssignSprite: no Sprite found at {assetPath} (is textureType set to Sprite?)");
                return;
            }
            var sr = slot.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            if (sr.sprite == sprite) return;
            sr.sprite = sprite;
            EditorUtility.SetDirty(sr);
            Debug.Log($"[SignalScrubber] {slot.name}  <-  {sprite.name}");
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
