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

            // Sticky notes taped to the monitor. They live on the CRT body
            // so they scale + move with the TV rather than sitting on the
            // desk. Positions are in CRT.Body local space (Body is at the
            // CRT origin, so world = CRT.pos + these * CRT.scale).
            var notes = EnsureChildTracked(body, "Notes", out bool notesCreated);
            if (notesCreated) notes.transform.localPosition = Vector3.zero;

            bool n1Existed = notes.transform.Find("StickyNote1") != null;
            var note1 = EnsureSpriteSlot(notes, "StickyNote1",
                localPos: new Vector3(-2.8f, 1.6f, -0.05f),
                sortingOrder: 4,
                color: Color.white);
            if (!n1Existed) note1.transform.localScale = Vector3.one;
            AutoAssignSprite(note1, "Assets/Art/CRT/sticky-note-1.png");
            AddStickyNoteText(note1, "it's closer\nthan you think");

            bool n2Existed = notes.transform.Find("StickyNote2") != null;
            var note2 = EnsureSpriteSlot(notes, "StickyNote2",
                localPos: new Vector3(2.8f, -1.4f, -0.05f),
                sortingOrder: 4,
                color: Color.white);
            if (!n2Existed) note2.transform.localScale = Vector3.one;
            AutoAssignSprite(note2, "Assets/Art/CRT/sticky-note-2.png");
            AddStickyNoteText(note2, "don't let\nit lock you");
        }

        const string JasonSharpiePath = "Assets/Fonts/JasonSharpie.ttf";

        /// <summary>
        /// Parents a TextMesh child under the sticky note so the note art
        /// reads as a hand-scrawled message. Uses legacy TextMesh (not TMP)
        /// so a raw TTF works without the SDF font-asset conversion step.
        /// Font, material, and transform are seeded on first creation;
        /// text is re-synced every run so code edits propagate.
        /// </summary>
        static void AddStickyNoteText(GameObject note, string text)
        {
            var go = EnsureChildTracked(note, "Text", out bool created);
            if (created)
            {
                go.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale    = Vector3.one;
            }

            var tm = go.GetComponent<TextMesh>();
            if (tm == null) tm = go.AddComponent<TextMesh>();

            if (created)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(JasonSharpiePath);
                if (font != null)
                {
                    tm.font = font;
                    var mr = go.GetComponent<MeshRenderer>();
                    if (mr != null && font.material != null)
                        mr.sharedMaterial = font.material;
                }
                tm.fontSize      = 64;
                tm.characterSize = 0.015f;
                tm.anchor        = TextAnchor.MiddleCenter;
                tm.alignment     = TextAlignment.Center;
                tm.color         = new Color(0.08f, 0.08f, 0.08f, 1f);

                var meshRenderer = go.GetComponent<MeshRenderer>();
                if (meshRenderer != null) meshRenderer.sortingOrder = 20;
            }

            tm.text = text;
        }

        static void RefineDesk(GameObject desk)
        {
            EnsureSpriteSlot(desk, "DeskSurface",
                             localPos: new Vector3(0f, 0f, 0f),
                             sortingOrder: 0, color: Color.white);

            // Clutter lives on the desk surface, below the monitor. Seed
            // its position on first creation so the sticky notes + other
            // props sit on the brown desk band (world Y ≈ -2.5) instead
            // of stranded at viewport centre.
            var clutter = EnsureChildTracked(desk, "Clutter", out bool clutterCreated);
            // Seed on first creation OR when it's still at the origin (so
            // previous Build Prefabs runs with Clutter stranded at 0,0,0
            // migrate cleanly). Respect any manual tweak away from zero.
            if (clutterCreated || clutter.transform.localPosition == Vector3.zero)
                clutter.transform.localPosition = new Vector3(0f, -2.5f, 0f);

            string[] clutterSlots = { "Mug", "Keyboard", "Papers", "Tapes", "Books" };
            int order = 1;
            foreach (var name in clutterSlots)
            {
                EnsureSpriteSlot(clutter, name,
                                 localPos: new Vector3((order - 3f) * 0.6f, 0.1f, -0.01f),
                                 sortingOrder: order, color: Color.white);
                order++;
            }

            // Sticky notes have moved to CRT/Body/Notes so they scale with
            // the monitor. Clean up any stragglers under Desk/Clutter.
            foreach (var legacyName in new[] { "StickyNotes", "StickyNote1", "StickyNote2" })
            {
                var legacy = clutter.transform.Find(legacyName);
                if (legacy != null) Object.DestroyImmediate(legacy.gameObject);
            }

            // NOTE: we intentionally leave DeskPlate alone. It is created
            // by SceneBootstrap as a brown MeshRenderer placeholder that
            // represents the desk surface until the artist drops in real
            // desk art on DeskSurface. Deleting it makes the desk vanish.
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
