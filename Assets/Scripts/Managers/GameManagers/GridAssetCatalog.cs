using System.IO;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Loads the grid-asset catalog snapshot (TECH-663 JSON) at boot, exposes query APIs for Stage 2.3+.</summary>
public partial class GridAssetCatalog
{
    [Header("Snapshot")]
    [Tooltip("Legacy v1 single-file path under Application.streamingAssetsPath. Superseded by per-kind exports under catalog/ + CatalogLoader (Stage 13.1, TECH-2675); leave empty unless reviving v1 in a fixture scene.")]
    [SerializeField] private string _streamingRelativePath = string.Empty;

    [Header("Events")]
    [SerializeField] private UnityEvent _onCatalogReloaded = new UnityEvent();

    /// <summary>Raised after a successful load or reload; safe to re-query indexes.</summary>
    public UnityEvent OnCatalogReloaded => _onCatalogReloaded;

    private void Awake() => LoadInternal();

    /// <summary>Read snapshot from disk, parse, rebuild indexes, fire <see cref="OnCatalogReloaded"/> on success (TECH-672).</summary>
    private void LoadInternal()
    {
        if (string.IsNullOrEmpty(_streamingRelativePath))
        {
            Debug.LogError("[GridAssetCatalog] Streaming relative path is not set.");
            return;
        }

        string full = Path.Combine(Application.streamingAssetsPath, _streamingRelativePath);
        if (!File.Exists(full))
        {
            Debug.LogError($"[GridAssetCatalog] Snapshot file not found: {full}");
            return;
        }

        string text = File.ReadAllText(full);
        if (!TryParseSnapshotJson(text, out var root, out var err))
        {
            Debug.LogError($"[GridAssetCatalog] Parse failed: {err}");
            return;
        }

        RebuildIndexes(root);
        if (_onCatalogReloaded != null) _onCatalogReloaded.Invoke();
    }
}
