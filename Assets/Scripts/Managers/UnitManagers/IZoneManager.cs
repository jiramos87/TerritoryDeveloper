using UnityEngine;
using Territory.Zones;

namespace Territory.Zones
{
/// <summary>
/// Contract for zone placement, zone queries, and zone building lifecycle.
/// Read this interface to understand ZoneManager's public API without reading its full implementation.
/// </summary>
public interface IZoneManager
{
    void HandleZoning(Vector2 gridPosition);
    ZoneAttributes GetZoneAttributes(Zone.ZoneType zoneType);
    GameObject GetRandomZonePrefab(Zone.ZoneType zoneType, int size = 1);
    GameObject GetGrassPrefab();
    GameObject GetWaterPrefab();
    void PlaceZonedBuildings(Zone.ZoneType zoningType);
    bool PlaceZoneAt(Vector2 gridPosition, Zone.ZoneType zoneType);
    void ClearZonedPositions();
}
}
