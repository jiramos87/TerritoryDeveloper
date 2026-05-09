using UnityEngine;
using System.Collections.Generic;
using Territory.Terrain;

namespace Territory.Roads
{
/// <summary>
/// Contract for road placement, drawing, prefab selection.
/// Read this interface to understand <see cref="RoadManager"/> public API without full impl.
/// </summary>
public interface IRoadManager
{
    void HandleRoadDrawing(Vector2 gridPosition);
    void UpdateAdjacentRoadPrefabsAt(Vector2 gridPos);
    bool CanPlaceRoadAt(Vector2 gridPos);
    bool PlaceRoadTileAt(Vector2 gridPos);
    GameObject GetCorrectRoadPrefabForPath(Vector2 prevGridPos, Vector2 currGridPos, HashSet<Vector2Int> forceFlatCells = null);
    void PlaceInterstateTile(Vector2 prevGridPos, Vector2 currGridPos, bool isInterstate);
    void ReplaceRoadTileAt(Vector2Int gridPos, GameObject newPrefab, bool keepInterstateTint);
    List<GameObject> GetRoadPrefabs();
    void GetRoadGhostPreviewForCell(Vector2 gridPos, out GameObject prefab, out Vector2 worldPos, out int sortingOrder);

    // Prefab fields consumed by Domains.Roads.Services.PrefabResolverService.
    GameObject roadTilePrefab1 { get; }
    GameObject roadTilePrefab2 { get; }
    GameObject roadTilePrefabCrossing { get; }
    GameObject roadTilePrefabTIntersectionUp { get; }
    GameObject roadTilePrefabTIntersectionDown { get; }
    GameObject roadTilePrefabTIntersectionLeft { get; }
    GameObject roadTilePrefabTIntersectionRight { get; }
    GameObject roadTilePrefabElbowUpLeft { get; }
    GameObject roadTilePrefabElbowUpRight { get; }
    GameObject roadTilePrefabElbowDownLeft { get; }
    GameObject roadTilePrefabElbowDownRight { get; }
    GameObject roadTileBridgeVertical { get; }
    GameObject roadTileBridgeHorizontal { get; }
    GameObject roadTilePrefabEastSlope { get; }
    GameObject roadTilePrefabWestSlope { get; }
    GameObject roadTilePrefabNorthSlope { get; }
    GameObject roadTilePrefabSouthSlope { get; }

    // Methods consumed by Domains.Roads.Services.AutoBuildService.
    void PlaceRoadTileFromResolved(ResolvedRoadTile resolved);
    void RefreshRoadPrefabsAfterBatchPlacement(IReadOnlyList<Vector2Int> newlyPlacedRoadCells);
    bool TryPrepareRoadPlacementPlanLongestValidPrefix(List<Vector2> pathRaw, RoadPathValidationContext ctx, bool postUserWarnings, ref int longestPrefixLengthHint, out List<Vector2> expandedPath, out PathTerraformPlan plan, out List<Vector2> filteredPathUsedOrNull);
    bool TryPrepareRoadPlacementPlanWithProgrammaticDeckSpanChord(List<Vector2> straightCardinalPath, Vector2Int segmentDir, RoadPathValidationContext ctx, out List<Vector2> expandedPath, out PathTerraformPlan plan);
    bool StrokeHasWaterOrWaterSlopeCells(IList<Vector2> stroke);
    bool StrokeLastCellIsFirmDryLand(IList<Vector2> stroke);
    bool TryExtendCardinalStreetPathWithBridgeChord(List<Vector2> pathVec2, Vector2Int dir);
    List<ResolvedRoadTile> ResolvePathForRoads(List<Vector2> path, PathTerraformPlan plan);

    // Interstate-specific: consumed by Domains.Roads.Services.InterstateService (cutover Stage 1.0 / TECH-26630).
    bool ValidateBridgePath(List<Vector2Int> path, HeightMap heightMap);
    bool ValidateInterstatePathForPlacement(List<Vector2Int> path);
    bool PlaceInterstateFromPath(List<Vector2Int> path);
}
}
