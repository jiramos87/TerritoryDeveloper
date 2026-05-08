using UnityEngine;

namespace Territory.Roads
{
    /// <summary>
    /// Prefab resolution result: prefab to instantiate, world position, sorting order.
    /// Lifted to Core leaf so IRoadManager + Roads service surface can reference without crossing into Game.asmdef.
    /// Canonical (replaces legacy nested structs in PrefabResolverService + RoadPrefabResolver).
    /// </summary>
    public struct ResolvedRoadTile
    {
        public Vector2Int gridPos;
        public GameObject prefab;
        public Vector2 worldPos;
        public int sortingOrder;
        public bool hasSegmentPrevHint;
        public Vector2Int segmentPrevGridPos;
        public bool hasSegmentNextHint;
        public Vector2Int segmentNextGridPos;
        public Vector2Int routeEntryStep;
        public Vector2Int routeExitStep;
    }
}
