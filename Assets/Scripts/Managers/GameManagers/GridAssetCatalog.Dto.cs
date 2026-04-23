using System;
using UnityEngine;

/// <summary>
/// DTOs and JSON parse for the catalog snapshot (TECH-663 envelope). See <c>web/lib/catalog/build-catalog-snapshot.ts</c>.
/// </summary>
public partial class GridAssetCatalog : MonoBehaviour
{
    /// <summary>Parse a snapshot JSON string into a root DTO. Uses <see cref="JsonUtility"/>; field names match export keys (snake_case where applicable).</summary>
    public static bool TryParseSnapshotJson(string json, out GridAssetSnapshotRoot root, out string err)
    {
        root = null;
        err = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            err = "JSON is null or empty.";
            return false;
        }

        try
        {
            string t = json.Trim();
            if (!t.StartsWith("{", StringComparison.Ordinal))
            {
                err = "JSON must be a single object.";
                return false;
            }

            var parsed = JsonUtility.FromJson<GridAssetSnapshotRoot>(t);
            if (parsed == null)
            {
                err = "JsonUtility returned null root.";
                return false;
            }

            if (parsed.schemaVersion < 1)
            {
                err = "Missing or invalid schemaVersion (expected >= 1).";
                return false;
            }

            NormalizeEmptyArrays(parsed);
            root = parsed;
            return true;
        }
        catch (Exception ex)
        {
            err = ex.Message;
            return false;
        }
    }

    private static void NormalizeEmptyArrays(GridAssetSnapshotRoot p)
    {
        if (p.assets == null) p.assets = Array.Empty<CatalogAssetRowDto>();
        if (p.sprites == null) p.sprites = Array.Empty<CatalogSpriteRowDto>();
        if (p.bindings == null) p.bindings = Array.Empty<CatalogAssetSpriteRowDto>();
        if (p.economy == null) p.economy = Array.Empty<CatalogEconomyRowDto>();
        if (p.importHygiene == null) p.importHygiene = Array.Empty<CatalogImportHygieneEntryDto>();
    }
}

/// <summary>Versioned root written by <c>catalog:export</c>.</summary>
[Serializable]
public class GridAssetSnapshotRoot
{
    public int schemaVersion;
    public string generatedAt;
    public bool includeDrafts;
    public CatalogAssetRowDto[] assets;
    public CatalogSpriteRowDto[] sprites;
    public CatalogAssetSpriteRowDto[] bindings;
    public CatalogEconomyRowDto[] economy;
    public CatalogImportHygieneEntryDto[] importHygiene;
}

/// <summary>Row for <c>catalog_asset</c> (export JSON key names mirror TS DTOs).</summary>
[Serializable]
public class CatalogAssetRowDto
{
    public string id;
    public string category;
    public string slug;
    public string display_name;
    public string status;
    public string replaced_by;
    public int footprint_w;
    public int footprint_h;
    public string placement_mode;
    public string unlocks_after; // PlacementValidator (TECH-692) reads via catalog snapshot row
    public bool has_button;
    public string updated_at;
}

/// <summary>Row for <c>catalog_sprite</c>.</summary>
[Serializable]
public class CatalogSpriteRowDto
{
    public string id;
    public string path;
    public int ppu;
    public float pivot_x;
    public float pivot_y;
    public string provenance;
    public string generator_archetype_id;
    public string generator_build_fingerprint;
    public int art_revision;
}

/// <summary>Row for <c>catalog_asset_sprite</c> composite (asset, slot, sprite).</summary>
[Serializable]
public class CatalogAssetSpriteRowDto
{
    public string asset_id;
    public string sprite_id;
    public string slot;
}

/// <summary>Row for <c>catalog_economy</c> (cents, ticks).</summary>
[Serializable]
public class CatalogEconomyRowDto
{
    public string asset_id;
    public int base_cost_cents;
    public int monthly_upkeep_cents;
    public int demolition_refund_pct;
    public int construction_ticks;
    public int budget_envelope_id;
    public string cost_catalog_row_id;
}

/// <summary>Import hygiene entry: texture path and importer hints.</summary>
[Serializable]
public class CatalogImportHygieneEntryDto
{
    public string texturePath;
    public int pixelsPerUnit;
    public CatalogPivotDto pivot;
}

[Serializable]
public class CatalogPivotDto
{
    public float x;
    public float y;
}
