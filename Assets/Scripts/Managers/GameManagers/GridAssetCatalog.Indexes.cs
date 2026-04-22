using System.Collections.Generic;
using UnityEngine;

/// <summary>PK and (category, slug) indexes over parsed snapshot assets (Stage 2.2, TECH-670).</summary>
public partial class GridAssetCatalog
{
    private readonly Dictionary<int, CatalogAssetRowDto> _assetsById = new();
    private readonly Dictionary<string, CatalogAssetRowDto> _assetsByCategorySlug = new();
    private readonly Dictionary<int, CatalogEconomyRowDto> _economyByAssetId = new();
    private GridAssetSnapshotRoot _lastSnapshot;

    /// <summary>Last successfully indexed snapshot, or null before first load.</summary>
    public GridAssetSnapshotRoot LastSnapshot => _lastSnapshot;

    /// <summary>Build lookup tables from a parsed root. Call after every successful <see cref="TryParseSnapshotJson"/>; duplicate id or composite key logs a warning and keeps the first row.</summary>
    public void RebuildIndexes(GridAssetSnapshotRoot data)
    {
        if (data == null)
        {
            Debug.LogError("[GridAssetCatalog] RebuildIndexes called with null snapshot.");
            return;
        }

        _lastSnapshot = data;
        _assetsById.Clear();
        _assetsByCategorySlug.Clear();
        _economyByAssetId.Clear();

        if (data.assets != null)
        {
            foreach (var row in data.assets)
            {
                if (row == null) continue;
                if (!int.TryParse(row.id, out int id))
                {
                    Debug.LogWarning($"[GridAssetCatalog] Skipping asset row: invalid id string \"{row.id}\".");
                    continue;
                }

                if (_assetsById.ContainsKey(id))
                {
                    Debug.LogWarning($"[GridAssetCatalog] Duplicate asset_id {id}. Keeping first row.");
                    continue;
                }
                _assetsById.Add(id, row);

                string cat = row.category ?? "";
                string slug = row.slug ?? "";
                string key = CategorySlugKey(cat, slug);
                if (_assetsByCategorySlug.ContainsKey(key))
                {
                    Debug.LogWarning(
                        $"[GridAssetCatalog] Duplicate (category, slug) (\"{cat}\",\"{slug}\") for id {id}. Keeping first row.");
                    continue;
                }
                _assetsByCategorySlug.Add(key, row);
            }
        }

        if (data.economy != null)
        {
            foreach (var econ in data.economy)
            {
                if (econ == null) continue;
                if (!int.TryParse(econ.asset_id, out int assetId))
                {
                    Debug.LogWarning($"[GridAssetCatalog] Skipping economy row: invalid asset_id \"{econ.asset_id}\".");
                    continue;
                }
                if (_economyByAssetId.ContainsKey(assetId))
                {
                    Debug.LogWarning($"[GridAssetCatalog] Duplicate economy asset_id {assetId}. Keeping first row.");
                    continue;
                }
                _economyByAssetId.Add(assetId, econ);
            }
        }
    }

    private static string CategorySlugKey(string category, string slug) => (category ?? "") + "\0" + (slug ?? "");

    /// <summary>Lookup by <c>catalog_asset.id</c> (numeric string in JSON, parsed to int here).</summary>
    public bool TryGetAsset(int id, out CatalogAssetRowDto row) => _assetsById.TryGetValue(id, out row);

    /// <summary>Lookup by category + slug, unique per index build.</summary>
    public bool TryGetAssetByCategorySlug(string category, string slug, out CatalogAssetRowDto row) =>
        _assetsByCategorySlug.TryGetValue(CategorySlugKey(category, slug), out row);

    /// <summary>Lookup economy row by <c>catalog_economy.asset_id</c>.</summary>
    public bool TryGetEconomyForAsset(int assetId, out CatalogEconomyRowDto row) =>
        _economyByAssetId.TryGetValue(assetId, out row);
}
