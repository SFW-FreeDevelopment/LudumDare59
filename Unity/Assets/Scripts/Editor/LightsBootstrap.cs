using System.IO;
using SignalScrubber.Polish;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SignalScrubber.EditorTools
{
    /// <summary>
    /// Sets up 2D ambient + accent lighting for the scene. Creates a shared
    /// Sprite-Lit material, switches every SpriteRenderer under World/* to
    /// use it, configures a dim Global Light 2D, and places three accent
    /// lights: a warm off-screen desk lamp spot, a phosphor CRT glow on
    /// the screen, and a small red LED point light on the power indicator.
    ///
    /// Run after Build Main Scene / Build Prefabs. Idempotent.
    /// </summary>
    internal static class LightsBootstrap
    {
        const string LitMatPath = "Assets/Materials/SpriteLit.mat";

        [MenuItem("Tools/Signal Scrubber/Build Lights")]
        static void BuildLights()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Build Lights",
                    "No active scene. Open Assets/Scenes/Main.unity first.", "OK");
                return;
            }

            var litMat = EnsureSpriteLitMaterial();
            int swapped = ApplyLitMaterialToWorldSprites(litMat);

            EnsureGlobalLight();
            EnsureDeskLamp();
            EnsureCrtGlow();
            EnsureLedLight();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SignalScrubber] Lights built. {swapped} SpriteRenderers switched to Sprite-Lit.");
        }

        static Material EnsureSpriteLitMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(LitMatPath);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader == null)
            {
                Debug.LogError("[SignalScrubber] Sprite-Lit-Default shader missing. Is URP 2D Renderer installed?");
                return null;
            }

            EnsureDir(Path.GetDirectoryName(LitMatPath));
            var mat = new Material(shader) { name = "SpriteLit" };
            AssetDatabase.CreateAsset(mat, LitMatPath);
            return mat;
        }

        static int ApplyLitMaterialToWorldSprites(Material lit)
        {
            if (lit == null) return 0;
            var scene = SceneManager.GetActiveScene();
            int count = 0;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name != "World") continue;
                foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.sharedMaterial == lit) continue;
                    sr.sharedMaterial = lit;
                    EditorUtility.SetDirty(sr);
                    count++;
                }
            }
            return count;
        }

        // ---------- lights ----------

        static void EnsureGlobalLight()
        {
            var existing = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
            foreach (var l in existing)
                if (l.lightType == Light2D.LightType.Global && l.gameObject.name == "GlobalLight")
                {
                    ConfigureGlobal(l);
                    return;
                }

            var lightsRoot = EnsureRoot("Lights");
            var go = EnsureChild(lightsRoot, "GlobalLight");
            var light = go.GetComponent<Light2D>() ?? go.AddComponent<Light2D>();
            ConfigureGlobal(light);
        }

        static void ConfigureGlobal(Light2D light)
        {
            light.lightType = Light2D.LightType.Global;
            light.intensity = 0.18f;
            light.color = new Color(0.55f, 0.62f, 0.78f);
        }

        static void EnsureDeskLamp()
        {
            var lightsRoot = EnsureRoot("Lights");
            var go = EnsureChild(lightsRoot, "DeskLamp");
            go.transform.position = new Vector3(5.5f, 4.5f, 0f);
            var light = go.GetComponent<Light2D>() ?? go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1f, 0.75f, 0.45f);
            light.intensity = 1.15f;
            light.pointLightInnerRadius = 1.5f;
            light.pointLightOuterRadius = 9.5f;
        }

        static void EnsureCrtGlow()
        {
            var lightsRoot = EnsureRoot("Lights");
            var go = EnsureChild(lightsRoot, "CrtGlow");
            // Hang the glow just in front of the CRT screen.
            go.transform.position = new Vector3(0f, 1f, -0.5f);
            var light = go.GetComponent<Light2D>() ?? go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(0.49f, 1f, 0.62f);
            light.intensity = 1.2f;
            light.pointLightInnerRadius = 0.5f;
            light.pointLightOuterRadius = 4.5f;

            // Drive intensity from clarity.
            var driver = go.GetComponent<CrtGlowDriver>() ?? go.AddComponent<CrtGlowDriver>();
            driver.Rebind();
        }

        static void EnsureLedLight()
        {
            var led = FindInScene("World/CRT/Body/PowerLed");
            if (led == null) return;
            var go = led.transform.Find("LedLight")?.gameObject;
            if (go == null)
            {
                go = new GameObject("LedLight");
                go.transform.SetParent(led.transform, false);
            }
            var light = go.GetComponent<Light2D>() ?? go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1f, 0.25f, 0.2f);
            light.intensity = 0.9f;
            light.pointLightInnerRadius = 0.05f;
            light.pointLightOuterRadius = 0.6f;
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

        static GameObject FindInScene(string path)
        {
            var parts = path.Split('/');
            var scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name != parts[0]) continue;
                var t = go.transform;
                for (int i = 1; i < parts.Length && t != null; i++) t = t.Find(parts[i]);
                return t != null ? t.gameObject : null;
            }
            return null;
        }

        static void EnsureDir(string dir)
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
    }
}
