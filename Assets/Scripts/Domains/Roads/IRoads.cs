using UnityEngine;
using System.Collections.Generic;

namespace Domains.Roads
{
    /// <summary>
    /// Public facade interface for the Roads domain. Consumers bind to this interface only
    /// — never to RoadManager or concrete service classes directly.
    /// Stage 3 surface: StrokeService extracted in tracer slice; PrefabService + CacheService follow.
    /// Invariants #2 (InvalidateRoadCache) + #10 (PathTerraformPlan) preserved in RoadManager.
    /// </summary>
    public interface IRoads
    {
        /// <summary>True if road can be placed at grid position.</summary>
        bool CanPlaceRoadAt(Vector2 gridPos);

        /// <summary>Place road tile at grid position. Returns true on success.</summary>
        bool PlaceRoadTileAt(Vector2 gridPos);

        /// <summary>
        /// Commit a street road stroke for scenario build.
        /// Skips affordability + money changes — tooling path only.
        /// </summary>
        bool TryCommitStreetStrokeForScenarioBuild(List<Vector2> pathRaw, out string error);

        /// <summary>Update adjacent road prefabs at grid position after placement.</summary>
        void UpdateAdjacentRoadPrefabsAt(Vector2 gridPos);

        /// <summary>Return all road prefabs registered on this manager.</summary>
        List<GameObject> GetRoadPrefabs();
    }
}
