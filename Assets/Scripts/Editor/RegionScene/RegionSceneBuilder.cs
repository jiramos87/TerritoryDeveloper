using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Territory.RegionScene;

namespace Territory.Editor.RegionScene
{
    /// <summary>
    /// One-off builder for Assets/Scenes/RegionScene.unity. Invoke via batchmode:
    ///   Unity -batchmode -quit -projectPath {REPO_ROOT} -executeMethod \
    ///     Territory.Editor.RegionScene.RegionSceneBuilder.BuildAndSave
    /// Idempotent: rebuilds the scene from scratch every invocation, overwrites existing file.
    /// </summary>
    public static class RegionSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/RegionScene.unity";
        private const string PlaceholderPath = "Assets/Resources/region/placeholder.png";

        [MenuItem("Tools/Territory/Build RegionScene")]
        public static void BuildAndSave()
        {
            var sceneDir = Path.GetDirectoryName(ScenePath);
            if (!string.IsNullOrEmpty(sceneDir) && !Directory.Exists(sceneDir))
            {
                Directory.CreateDirectory(sceneDir);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.18f, 0.20f, 0.24f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            // Region root hub
            var regionRoot = new GameObject("RegionRoot");
            var manager = regionRoot.AddComponent<RegionManager>();

            // Wire mainCamera SerializeField via SerializedObject (private field reflection-friendly).
            var so = new SerializedObject(manager);
            var camProp = so.FindProperty("mainCamera");
            if (camProp != null)
            {
                camProp.objectReferenceValue = camera;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // Placeholder sprite at grid center (col=31,row=31 → world = (0, 15.5, 0))
            var placeholderGo = new GameObject("PlaceholderSprite");
            placeholderGo.transform.SetParent(regionRoot.transform);
            placeholderGo.transform.position = new Vector3(0f, 15.5f, 0f);
            var sr = placeholderGo.AddComponent<SpriteRenderer>();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderPath);
            if (sprite != null)
            {
                sr.sprite = sprite;
            }
            else
            {
                Debug.LogWarning($"[RegionSceneBuilder] Placeholder sprite not found at {PlaceholderPath} — leaving SpriteRenderer empty.");
            }

            // Save scene
            EditorSceneManager.MarkSceneDirty(scene);
            var ok = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!ok)
            {
                Debug.LogError($"[RegionSceneBuilder] Failed to save scene at {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            // Add to EditorBuildSettings scenes if not present (non-fatal if missing).
            var scenes = EditorBuildSettings.scenes;
            var alreadyPresent = false;
            foreach (var s in scenes)
            {
                if (s.path == ScenePath) { alreadyPresent = true; break; }
            }
            if (!alreadyPresent)
            {
                var next = new EditorBuildSettingsScene[scenes.Length + 1];
                System.Array.Copy(scenes, next, scenes.Length);
                next[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
                EditorBuildSettings.scenes = next;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[RegionSceneBuilder] RegionScene saved to {ScenePath}");
        }
    }
}
