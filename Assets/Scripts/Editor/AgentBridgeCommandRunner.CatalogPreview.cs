using System;
using System.IO;
using Territory.Catalog;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// <c>catalog_preview</c> kind for <see cref="AgentBridgeCommandRunner"/>.
/// Loads the sandboxed <c>CatalogPreview.unity</c> scene additively, resolves the
/// <see cref="PreviewCatalog"/> component, calls <see cref="PreviewCatalog.Resolve"/>,
/// and optionally captures a screenshot via <see cref="ScheduleDeferredScreenshotComplete"/>.
/// </summary>
public static partial class AgentBridgeCommandRunner
{
    [Serializable]
    class CatalogPreviewParams
    {
        public string catalog_entry_id;
        public bool include_screenshot;
    }

    internal static bool TryDispatchCatalogPreviewKind(
        string kind,
        string repoRoot,
        string commandId,
        string requestJson)
    {
        if (!TryParseRequestEnvelope(requestJson, out AgentBridgeRequestEnvelopeDto env, out string parseErr))
        {
            TryFinalizeFailed(repoRoot, commandId, parseErr);
            return false;
        }

        AgentBridgeParamsPayloadDto p = env.bridge_params ?? new AgentBridgeParamsPayloadDto();

        string entryId = p.catalog_entry_id ?? string.Empty;
        if (string.IsNullOrWhiteSpace(entryId))
        {
            TryFinalizeFailed(repoRoot, commandId, "catalog_preview requires catalog_entry_id.");
            return false;
        }

        bool includeScreenshot = p.include_screenshot;

        const string ScenePath = "Assets/Scenes/CatalogPreview.unity";
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }
        catch (Exception ex)
        {
            TryFinalizeFailed(repoRoot, commandId, $"catalog_preview: could not open scene '{ScenePath}': {ex.Message}");
            return false;
        }

        PreviewCatalog previewCatalog = UnityEngine.Object.FindObjectOfType<PreviewCatalog>();
        if (previewCatalog == null)
        {
            TryFinalizeFailed(repoRoot, commandId,
                $"catalog_preview: PreviewCatalog component not found in '{ScenePath}'. " +
                "Ensure a GameObject with PreviewCatalog is present in the scene.");
            return false;
        }

        var entry = new CatalogEntity { entity_id = entryId };
        previewCatalog.Resolve(entry);

        if (!includeScreenshot)
        {
            string resultJson = $"{{\"resolved\":true,\"entry_id\":\"{entryId}\",\"screenshot_path\":\"\"}}";
            var resp = AgentBridgeResponseFileDto.CreateOk(commandId, "catalog_preview");
            resp.catalog_preview_result = resultJson;
            CompleteOrFail(repoRoot, commandId, JsonUtility.ToJson(resp, true));
            return true;
        }

        string screenshotsDir = Path.Combine(repoRoot, "tools", "reports", "bridge-screenshots");
        Directory.CreateDirectory(screenshotsDir);
        string fileName = $"catalog-preview-{commandId}.png";
        string absPath = Path.Combine(screenshotsDir, fileName);
        string relPath = Path.Combine("tools", "reports", "bridge-screenshots", fileName);

        FocusGameViewIfPossible();
        ScreenCapture.CaptureScreenshot(absPath);
        ScheduleDeferredScreenshotComplete(repoRoot, commandId, absPath, relPath, null);
        return true;
    }
}
