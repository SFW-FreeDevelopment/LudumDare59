using System.IO;
using SignalScrubber.Polish;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SignalScrubber.EditorTools
{
    /// <summary>
    /// Builds the Main.unity composition described in ARCHITECTURE.md §Scene Graph:
    /// fixed orthographic camera, World/Background/Desk/CRT layered roots with
    /// placeholder MeshRenderers, UI/{DiegeticUI,OverlayUI} parented, Systems
    /// container, and a GlobalVolume with a shared PostFx profile.
    ///
    /// Idempotent: runs safely on an empty scene or on one already scaffolded.
    /// </summary>
    internal static class SceneBootstrap
    {
        const string PostFxPath = "Assets/Settings/PostFx.asset";
        const string MaterialsDir = "Assets/Materials";

        [MenuItem("Tools/Signal Scrubber/Build Main Scene")]
        static void BuildMainScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Build Main Scene",
                    "No active scene. Open Assets/Scenes/Main.unity first.", "OK");
                return;
            }

            ConfigureCamera();
            BuildWorld();
            EnsureUIRoot();
            EnsureSystemsRoot();
            EnsureGlobalVolume();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[SignalScrubber] Scene composition built.");
        }

        static void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.backgroundColor = new Color(0.020f, 0.027f, 0.039f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            var urp = cam.GetUniversalAdditionalCameraData();
            urp.renderPostProcessing = true;

            // Lock the render to 16:9 on every aspect — letterbox bars
            // appear naturally on non-16:9 monitors because Unity clears
            // the screen before rendering the camera rect.
            if (cam.GetComponent<AspectRatioEnforcer>() == null)
                cam.gameObject.AddComponent<AspectRatioEnforcer>();
        }

        static void BuildWorld()
        {
            var world = EnsureRoot("World");

            var bg   = EnsureChild(world, "Background");
            var desk = EnsureChild(world, "Desk");
            bool crtExisted = world.transform.Find("CRT") != null;
            var crt  = EnsureChild(world, "CRT");
            var body       = EnsureChild(crt, "Body");
            var screen     = EnsureChild(crt, "Screen");
            var foreground = EnsureChild(crt, "Foreground");

            // Shift the whole CRT up and scale it so it dominates the upper
            // two-thirds of the view and leaves clear space at the bottom
            // for the desk / keyboard art. Seeded on first creation only so
            // manual layout tweaks survive re-runs of Build Main Scene.
            if (!crtExisted)
            {
                crt.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                crt.transform.localScale    = new Vector3(1.4f, 1.4f, 1f);
            }

            // Placeholder plates. Z-layering matches ARCHITECTURE.md.
            EnsurePlate(bg,   "BackgroundPlate", new Color(0.06f, 0.07f, 0.08f),
                        size: new Vector2(20f, 12f), localZ: 2f);
            EnsurePlate(desk, "DeskPlate",       new Color(0.18f, 0.12f, 0.08f),
                        size: new Vector2(20f, 4f),  localPos: new Vector3(0f, -3.4f, 1f));
            EnsurePlate(body, "BodyPlate",       new Color(0.08f, 0.08f, 0.09f),
                        size: new Vector2(8f, 5.2f), localZ: 0f);
            EnsurePlate(screen, "ScreenPlate",   new Color(0.03f, 0.08f, 0.04f),
                        size: new Vector2(6.8f, 4.0f), localZ: -0.1f);
            EnsurePlate(foreground, "GlassPlate", new Color(1f, 1f, 1f, 0f),
                        size: new Vector2(6.8f, 4.0f), localZ: -0.2f);
        }

        static void EnsureUIRoot()
        {
            var scene = SceneManager.GetActiveScene();
            var uiRoot = EnsureRoot("UI");

            foreach (var go in scene.GetRootGameObjects())
            {
                if (go == uiRoot) continue;
                if (go.name == "DiegeticUI" || go.name == "OverlayUI")
                    go.transform.SetParent(uiRoot.transform, true);
            }
        }

        static void EnsureSystemsRoot() => EnsureRoot("Systems");

        static void EnsureGlobalVolume()
        {
            var scene = SceneManager.GetActiveScene();
            GameObject volGo = null;
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == "GlobalVolume") { volGo = go; break; }

            if (volGo == null)
            {
                volGo = new GameObject("GlobalVolume");
                SceneManager.MoveGameObjectToScene(volGo, scene);
            }

            var existingVol = volGo.GetComponent<Volume>();
            var vol = existingVol != null ? existingVol : volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 0;

            if (vol.sharedProfile == null)
            {
                vol.sharedProfile = LoadOrCreatePostFxProfile();
            }
        }

        static VolumeProfile LoadOrCreatePostFxProfile()
        {
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostFxPath);
            if (existing != null) return existing;

            EnsureDir(Path.GetDirectoryName(PostFxPath));
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, PostFxPath);

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.8f);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.7f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.35f);
            vignette.smoothness.Override(0.4f);
            vignette.color.Override(Color.black);

            var grain = profile.Add<FilmGrain>(true);
            grain.intensity.Override(0.25f);
            grain.response.Override(0.8f);

            var color = profile.Add<ColorAdjustments>(true);
            color.contrast.Override(10f);
            color.saturation.Override(-15f);

            EditorUtility.SetDirty(profile);
            return profile;
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

        static GameObject EnsureChild(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static void EnsurePlate(GameObject parent, string name, Color color,
            Vector2 size, Vector3 localPos = default, float localZ = 0f)
        {
            var t = parent.transform.Find(name);
            bool created = t == null;
            GameObject go = t != null ? t.gameObject : new GameObject(name);
            if (created) go.transform.SetParent(parent.transform, false);

            // Seed transform only on first creation so manual layout
            // changes survive re-runs of Build Main Scene.
            if (created)
            {
                go.transform.localPosition = localPos == default
                    ? new Vector3(0f, 0f, localZ)
                    : localPos;
                go.transform.localScale = new Vector3(size.x, size.y, 1f);
            }

            var existingMf = go.GetComponent<MeshFilter>();
            var mf = existingMf != null ? existingMf : go.AddComponent<MeshFilter>();
            if (mf.sharedMesh == null) mf.sharedMesh = GetQuadMesh();
            var existingMr = go.GetComponent<MeshRenderer>();
            var mr = existingMr != null ? existingMr : go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sharedMaterial = GetOrCreatePlaceholderMaterial(name, color);
        }

        static Mesh _quad;
        static Mesh GetQuadMesh()
        {
            if (_quad != null) return _quad;
            // Unity's built-in Quad primitive mesh.
            var prim = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _quad = prim.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(prim);
            return _quad;
        }

        static Material GetOrCreatePlaceholderMaterial(string slotName, Color color)
        {
            EnsureDir(MaterialsDir);
            string path = $"{MaterialsDir}/Placeholder_{slotName}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = $"Placeholder_{slotName}" };
            mat.SetColor("_BaseColor", color);
            if (color.a < 1f)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void EnsureDir(string dir)
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
    }
}
