using UnityEngine;

/// <summary>Dev placeholder vs release unusable policy for missing <see cref="Resources"/> sprite paths (Stage 2.2, TECH-671).</summary>
public partial class GridAssetCatalog
{
    [Header("Dev missing-sprite (Editor / dev player)")]
    [Tooltip("Loud stand-in when a catalog sprite path fails to load; release builds do not use this for gameplay.")]
    [SerializeField] private Sprite _missingSpriteDevPlaceholder;

    /// <summary>Loads <paramref name="row"/>.<c>path</c> from Resources, or applies dev/ship policy on failure — see grid-asset snapshot exploration.</summary>
    public bool TryResolveSpriteFromRow(CatalogSpriteRowDto row, out Sprite sprite, out bool isUsable)
    {
        sprite = null;
        isUsable = false;
        if (row == null)
            return false;

        if (string.IsNullOrEmpty(row.path))
            return ApplyMissingRowPolicy(out sprite, out isUsable, row);

        var loaded = Resources.Load<Sprite>(row.path);
        if (loaded != null)
        {
            sprite = loaded;
            isUsable = true;
            return true;
        }

        return ApplyMissingRowPolicy(out sprite, out isUsable, row);
    }

    private bool ApplyMissingRowPolicy(out Sprite sprite, out bool isUsable, CatalogSpriteRowDto row)
    {
        sprite = null;
        isUsable = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_missingSpriteDevPlaceholder != null)
        {
            sprite = _missingSpriteDevPlaceholder;
            isUsable = true;
            Debug.LogWarning($"[GridAssetCatalog] Missing sprite; using dev placeholder (path='{row.path}').");
            return true;
        }
        Debug.LogError($"[GridAssetCatalog] Missing sprite and no dev placeholder assigned (path='{row.path}').");
#else
        Debug.LogWarning($"[GridAssetCatalog] Missing sprite; row unusable in release (path='{row.path}').");
#endif
        return false;
    }
}
