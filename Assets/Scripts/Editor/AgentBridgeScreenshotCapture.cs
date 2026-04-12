using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Play Mode screenshot capture for IDE agent bridge (<c>capture_screenshot</c>).
/// Default prefers <see cref="Camera.main"/> (sync PNG) in Play Mode. Falls back to
/// <see cref="ScreenCapture.CaptureScreenshot"/> with project-relative path when no main camera.
/// When <paramref name="includeGameViewWithOverlayUi"/> true → always <see cref="ScreenCapture.CaptureScreenshot"/>
/// (Game view, includes Screen Space - Overlay UI); <paramref name="cameraNameOrEmpty"/> ignored.
/// Named <see cref="Camera"/> capture writes PNG sync. Output: <c>tools/reports/bridge-screenshots/</c> (gitignored).
/// </summary>
public static class AgentBridgeScreenshotCapture
{
    static readonly Regex s_safeStem = new Regex(@"[^a-zA-Z0-9_-]+", RegexOptions.Compiled);

    /// <summary>
    /// Prepare capture. When <paramref name="deferredToNextEditorFrame"/> true → file written at end of frame;
    /// caller must complete bridge job after game view renders (see <c>AgentBridgeCommandRunner</c> pump).
    /// </summary>
    public static bool TryBeginCapture(
        string repoRoot,
        string cameraNameOrEmpty,
        string filenameStemOrEmpty,
        bool includeGameViewWithOverlayUi,
        out string repoRelativePath,
        out string absolutePath,
        out bool deferredToNextEditorFrame,
        out string englishError)
    {
        repoRelativePath = null;
        absolutePath = null;
        deferredToNextEditorFrame = false;
        englishError = null;

        if (string.IsNullOrEmpty(repoRoot) || !Directory.Exists(repoRoot))
        {
            englishError = "Invalid repository root for screenshot.";
            return false;
        }

        if (!EditorApplication.isPlaying)
        {
            englishError = "Screenshot requires Play Mode.";
            return false;
        }

        string relDir = Path.Combine("tools", "reports", "bridge-screenshots");
        string absDir = Path.Combine(repoRoot, relDir);
        try
        {
            Directory.CreateDirectory(absDir);
        }
        catch (Exception ex)
        {
            englishError = $"Could not create screenshot directory: {ex.Message}";
            return false;
        }

        string stem = SanitizeStem(filenameStemOrEmpty);
        if (string.IsNullOrEmpty(stem))
            stem = "screenshot";
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        string fileName = $"{stem}-{stamp}.png";
        string relFile = Path.Combine(relDir, fileName).Replace('\\', '/');
        string absFile = Path.Combine(repoRoot, relFile);

        try
        {
            if (includeGameViewWithOverlayUi)
            {
                ScreenCapture.CaptureScreenshot(relFile);
                repoRelativePath = relFile.Replace('\\', '/');
                absolutePath = absFile;
                deferredToNextEditorFrame = true;
                return true;
            }

            if (string.IsNullOrWhiteSpace(cameraNameOrEmpty))
            {
                Camera playCam = Camera.main;
                if (playCam == null)
                {
                    Camera[] cams = UnityEngine.Object.FindObjectsOfType<Camera>();
                    for (int i = 0; i < cams.Length; i++)
                    {
                        Camera c = cams[i];
                        if (c != null && c.enabled && c.gameObject.activeInHierarchy)
                        {
                            playCam = c;
                            break;
                        }
                    }
                }

                if (playCam != null)
                {
                    if (!TryCaptureFromCamera(playCam.gameObject.name, absFile, out string mainErr))
                    {
                        englishError = mainErr;
                        return false;
                    }
                }
                else
                {
                    // Project-relative path; absolute paths are unreliable for ScreenCapture on some Editor platforms.
                    ScreenCapture.CaptureScreenshot(relFile);
                    repoRelativePath = relFile.Replace('\\', '/');
                    absolutePath = absFile;
                    deferredToNextEditorFrame = true;
                    return true;
                }
            }
            else if (!TryCaptureFromCamera(cameraNameOrEmpty.Trim(), absFile, out string camErr))
            {
                englishError = camErr;
                return false;
            }
        }
        catch (Exception ex)
        {
            englishError = $"Screenshot failed: {ex.Message}";
            return false;
        }

        if (!File.Exists(absFile))
        {
            englishError = "Screenshot file was not created.";
            return false;
        }

        repoRelativePath = relFile.Replace('\\', '/');
        absolutePath = absFile;
        deferredToNextEditorFrame = false;
        return true;
    }

    static string SanitizeStem(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        string s = s_safeStem.Replace(raw.Trim(), "_");
        if (s.Length > 80)
            s = s.Substring(0, 80);
        return string.IsNullOrEmpty(s) ? null : s;
    }

    static bool TryCaptureFromCamera(string cameraObjectName, string absolutePngPath, out string error)
    {
        error = null;
        Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
        Camera chosen = null;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].gameObject.name == cameraObjectName)
            {
                chosen = cameras[i];
                break;
            }
        }

        if (chosen == null)
        {
            error = $"No Camera on a GameObject named '{cameraObjectName}'.";
            return false;
        }

        int w = chosen.pixelWidth > 0 ? chosen.pixelWidth : 1920;
        int h = chosen.pixelHeight > 0 ? chosen.pixelHeight : 1080;
        RenderTexture rt = null;
        RenderTexture previous = chosen.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        try
        {
            rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            chosen.targetTexture = rt;
            chosen.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            File.WriteAllBytes(absolutePngPath, png);
            return true;
        }
        finally
        {
            chosen.targetTexture = previous;
            RenderTexture.active = previousActive;
            if (rt != null)
                UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
