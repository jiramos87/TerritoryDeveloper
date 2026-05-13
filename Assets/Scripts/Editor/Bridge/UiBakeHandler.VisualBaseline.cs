using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace Territory.Editor.Bridge
{
    /// <summary>
    /// Visual baseline capture partial for UiBakeHandler.
    /// Adds CaptureBaselineCandidate to emit candidate PNGs for pixel-diff regression.
    /// ui-visual-regression Stage 1.0 (TECH-31891).
    /// </summary>
    public static partial class UiBakeHandler
    {
        /// <summary>
        /// Result returned by CaptureBaselineCandidate.
        /// </summary>
        public class BaselineCaptureResult
        {
            public string panel_slug;
            public string candidate_path;
            public string sha256;
            public string resolution;
            public string theme;
            public string error;
        }

        /// <summary>
        /// Render the published panel prefab for panelSlug via RenderTexture at
        /// (width x height) and write the PNG to
        /// Library/UiBaselines/_candidate/{panelSlug}.png.
        /// Returns a BaselineCaptureResult with candidate_path + sha256.
        /// </summary>
        public static BaselineCaptureResult CaptureBaselineCandidate(
            string panelSlug,
            int width = 1920,
            int height = 1080,
            string theme = "dark")
        {
            if (string.IsNullOrEmpty(panelSlug))
            {
                return new BaselineCaptureResult
                {
                    error = "missing_arg:panel_slug",
                };
            }

            // Resolve prefab path — canonical bake output location.
            string prefabPath = $"Assets/UI/Prefabs/Generated/{panelSlug}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return new BaselineCaptureResult
                {
                    panel_slug = panelSlug,
                    error = $"prefab_not_found:{prefabPath}",
                };
            }

            // Set up RenderTexture + Camera.
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 1;

            var cameraGo = new GameObject("__VR_CaptureCamera__");
            var cam = cameraGo.AddComponent<Camera>();
            cam.targetTexture = rt;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.orthographic = true;
            cam.orthographicSize = height / 2f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;

            // Instantiate panel prefab at origin.
            var panelGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            panelGo.name = "__VR_CapturePanel__";
            panelGo.transform.position = Vector3.zero;

            string candidatePath = null;
            string sha256Hex = null;
            string errorMsg = null;

            try
            {
                cam.Render();

                // Read pixels from RenderTexture.
                var prevActive = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = prevActive;

                byte[] pngBytes = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);

                // Write to Library/UiBaselines/_candidate/.
                string candidateDir = Path.Combine("Library", "UiBaselines", "_candidate");
                Directory.CreateDirectory(candidateDir);
                candidatePath = Path.Combine(candidateDir, $"{panelSlug}.png");
                File.WriteAllBytes(candidatePath, pngBytes);

                // Compute sha256.
                using var sha = SHA256.Create();
                var hashBytes = sha.ComputeHash(pngBytes);
                sha256Hex = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }
            finally
            {
                // Cleanup.
                UnityEngine.Object.DestroyImmediate(panelGo);
                UnityEngine.Object.DestroyImmediate(cameraGo);
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }

            if (errorMsg != null)
            {
                return new BaselineCaptureResult
                {
                    panel_slug = panelSlug,
                    error = errorMsg,
                };
            }

            return new BaselineCaptureResult
            {
                panel_slug = panelSlug,
                candidate_path = candidatePath,
                sha256 = sha256Hex,
                resolution = $"{width}x{height}",
                theme = theme,
            };
        }
    }
}
