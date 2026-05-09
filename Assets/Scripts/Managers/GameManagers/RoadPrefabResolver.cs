using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Terrain;
using Territory.Zones;
using Domains.Roads.Services;

namespace Territory.Roads
{
/// <summary>
/// Pass-through wrapper over PrefabResolverService (stage-3 cutover).
/// Preserves class name + namespace + file path. All algorithm body lives in PrefabResolverService.
/// </summary>
public class RoadPrefabResolver
{
    private readonly PrefabResolverService _prefabResolverService;

    public RoadPrefabResolver(GridManager grid, TerrainManager terrain, RoadManager roads)
    {
        _prefabResolverService = new PrefabResolverService(grid, terrain, roads);
    }

    /// <summary>Resolve prefabs for full path via terraform plan.</summary>
    public List<ResolvedRoadTile> ResolveForPath(List<Vector2> path, PathTerraformPlan plan)
        => _prefabResolverService.ResolveForPath(path, plan);

    /// <summary>Resolve prefab for single cell via neighbor connectivity.</summary>
    public ResolvedRoadTile? ResolveForCell(Vector2 currGridPos, Vector2 prevGridPos)
        => _prefabResolverService.ResolveForCell(currGridPos, prevGridPos);

    /// <summary>Resolve prefab for ghost preview (single cell, no path).</summary>
    public void ResolveForGhostPreview(Vector2 gridPos, out GameObject prefab, out Vector2 worldPos, out int sortingOrder)
        => _prefabResolverService.ResolveForGhostPreview(gridPos, out prefab, out worldPos, out sortingOrder);
}
}
