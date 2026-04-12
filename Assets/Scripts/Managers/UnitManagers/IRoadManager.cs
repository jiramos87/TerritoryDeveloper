using UnityEngine;
using System.Collections.Generic;

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
}
}
