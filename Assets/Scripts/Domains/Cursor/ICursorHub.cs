using UnityEngine;
using Territory.Core;
using Territory.Zones;

namespace Domains.Cursor
{
    /// <summary>Hub seam — lets CursorService call back to the hub MonoBehaviour without MonoBehaviour dep.</summary>
    public interface ICursorHub
    {
        // ── Textures ──────────────────────────────────────────────────────────────
        Texture2D CursorTexture { get; }
        Texture2D BulldozerTexture { get; }
        Texture2D DetailsTexture { get; }

        // ── UI probes ─────────────────────────────────────────────────────────────
        bool IsPointerOverUI();

        // ── Events ────────────────────────────────────────────────────────────────
        void FirePlacementResultChanged(PlacementResult result);
        void FirePlacementReasonChanged(PlacementFailReason reason);

        // ── Grid/world queries (passthrough — avoids Game asmdef dep in service) ──
        Vector2 ScreenToWorldOnGrid(Camera cam, Vector3 screenPos);
        CityCell GetMouseCell(Vector2 worldPos);
        CityCell GetCell(int x, int y);
        Vector2 GetBuildingPlacementPos(Vector2 gridPos, int buildingSize);
        float TileHeight { get; }

        // ── Road ghost ────────────────────────────────────────────────────────────
        bool HasRoadManager { get; }
        void GetRoadGhostPreview(Vector2 gridPos, out GameObject prefab, out Vector2 worldPos, out int sortingOrder);

        // ── Selection probes ──────────────────────────────────────────────────────
        Zone.ZoneType GetSelectedZoneType();
        int GetSelectedBuildingSize();
        bool IsSelectedBuildingWaterPlant();

        // ── Placement ─────────────────────────────────────────────────────────────
        PlacementResult CanPlace(int assetId, int cellX, int cellY, int rotation, Zone.ZoneType zoneType);
    }
}
