using System;
using UnityEngine;
using Territory.Economy;
using Territory.Zones;

namespace Territory.Core
{
    /// <summary>Structured denial reason for <see cref="PlacementValidator.CanPlace"/> (Stage 3.1, TECH-689).</summary>
    public enum PlacementFailReason
    {
        None = 0,
        Footprint,
        Zoning,
        Locked,
        Unaffordable,
        Occupied
    }

    /// <summary>Outcome of a placement check (TECH-689).</summary>
    public readonly struct PlacementResult
    {
        public bool IsAllowed => Reason == PlacementFailReason.None;
        public PlacementFailReason Reason { get; }
        public string Detail { get; }

        private PlacementResult(PlacementFailReason reason, string detail)
        {
            Reason = reason;
            Detail = detail ?? string.Empty;
        }

        /// <summary>Placement permitted.</summary>
        public static PlacementResult Allowed(string detail = null) => new PlacementResult(PlacementFailReason.None, detail);

        /// <summary>Placement denied for <paramref name="reason"/>.</summary>
        public static PlacementResult Fail(PlacementFailReason reason, string detail = null) =>
            new PlacementResult(reason, detail);
    }

    /// <summary>
    /// Single owner of <c>CanPlace(assetId, cell, …)</c> for grid assets (grid-asset-visual-registry Stage 3.1).
    /// Uses <see cref="GridManager"/> public API only; does not read <c>cellArray</c> directly.
    /// </summary>
    public class PlacementValidator : MonoBehaviour
    {
        [Header("Required references")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private GridAssetCatalog catalog;
        [SerializeField] private EconomyManager economyManager;

        private void Awake()
        {
            if (gridManager == null)
                gridManager = FindObjectOfType<GridManager>();
            if (catalog == null)
                catalog = FindObjectOfType<GridAssetCatalog>();
            if (economyManager == null)
                economyManager = FindObjectOfType<EconomyManager>();
        }

        /// <summary>
        /// Returns whether a catalog asset may be placed at the cell (Stage 3.1 MVP: 1×1, unlock + zoning + afford).
        /// </summary>
        /// <param name="assetId">Catalog <c>asset_id</c> (numeric row key).</param>
        /// <param name="cellX">Grid X (integer cell).</param>
        /// <param name="cellY">Grid Y.</param>
        /// <param name="rotation">Reserved (MVP: ignored).</param>
        /// <param name="intendedZoneType">Simulated <see cref="Zone.ZoneType"/> for this placement; drives zoning check.</param>
        public PlacementResult CanPlace(
            int assetId,
            int cellX,
            int cellY,
            int rotation,
            Zone.ZoneType intendedZoneType)
        {
            _ = rotation;
            if (gridManager == null)
                return PlacementResult.Fail(PlacementFailReason.Footprint, "GridManager is not set.");

            CityCell cell = gridManager.GetCell(cellX, cellY);
            if (cell == null)
                return PlacementResult.Fail(PlacementFailReason.Footprint, "Out of grid bounds.");

            if (cell.occupiedBuilding != null)
                return PlacementResult.Fail(PlacementFailReason.Occupied, "Cell already has a building.");

            if (catalog == null || !catalog.TryGetAsset(assetId, out CatalogAssetRowDto asset))
            {
                // Unseeded dev snapshot: do not block until catalog rows exist (empty StreamingAssets is common in repo).
                return PlacementResult.Allowed("Catalog row missing; skipped legality (unseeded snapshot).");
            }

            if (asset.footprint_w > 1 || asset.footprint_h > 1)
                return PlacementResult.Fail(PlacementFailReason.Footprint, "MVP only supports 1x1 footprint.");

            // TECH-692: when unlocks_after is set and no tech tree is wired, allow (see Decision Log on TECH-692).
            if (!string.IsNullOrEmpty(asset.unlocks_after))
            {
                // Future: IUnlockService or tech manager — default allow per Stage 3.1 stub.
            }

            if (ZoneManager.IsStateServiceZoneType(intendedZoneType))
            {
                if (!IsStateServiceCatalogCategory(asset.category))
                    return PlacementResult.Fail(PlacementFailReason.Zoning, "Asset category is not a state_service channel for this zone.");
            }

            if (catalog.TryGetEconomyForAsset(assetId, out CatalogEconomyRowDto econ) && economyManager != null)
            {
                int simCost = Math.Max(0, econ.base_cost_cents / 100);
                if (simCost > 0 && !economyManager.CanAfford(simCost))
                    return PlacementResult.Fail(PlacementFailReason.Unaffordable, "Insufficient treasury for base cost.");
            }

            return PlacementResult.Allowed();
        }

        private static bool IsStateServiceCatalogCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return true;
            return category.Trim().Equals("state_service", StringComparison.OrdinalIgnoreCase);
        }
    }
}
