using UnityEngine;

/// <summary>Editor / dev entry point to re-run the same load pipeline as <see cref="GridAssetCatalog.Awake"/> (Stage 2.2, TECH-673).</summary>
public partial class GridAssetCatalog
{
    /// <summary>Re-read the snapshot and rebuild indexes. Invokes <see cref="GridAssetCatalog.OnCatalogReloaded"/> on success.</summary>
    public void ReloadFromDisk() => LoadInternal();
}
