using System.Collections.Generic;
using UnityEngine;
using Territory.Zones;

namespace Domains.Zones
{
    /// <summary>
    /// Public facade interface for the Zones domain.
    /// Consumers bind to this interface — never to ZoneManager or concrete service classes directly.
    /// Stage 8 surface: ZonePlacementService + ZoneSectionService + ZonePrefabRegistry extracted in tracer slice.
    /// Invariant #11 (UrbanizationProposal never re-enable) preserved: no UrbanizationProposal reference here.
    /// </summary>
    public interface IZones
    {
        /// <summary>Returns ZoneAttributes for given zone type; null if unrecognized.</summary>
        ZoneAttributes GetZoneAttributes(Zone.ZoneType zoneType);

        /// <summary>Returns random prefab for zone type + size; null if unavailable.</summary>
        GameObject GetRandomZonePrefab(Zone.ZoneType zoneType, int size = 1);

        /// <summary>Returns true if zone type is a zoning overlay (empty zone awaiting building spawn).</summary>
        bool IsZoningType(Zone.ZoneType zoneType);

        /// <summary>Maps zoning overlay type to corresponding building zone type.</summary>
        Zone.ZoneType GetBuildingZoneType(Zone.ZoneType zoningType);

        /// <summary>Recalcs all available contiguous square sections (1x1, 2x2, 3x3) per zone type.</summary>
        void CalculateAvailableSquareZonedSections();

        /// <summary>Adds grid position to tracked zoned-positions for specified zone type.</summary>
        void AddZonedTileToList(Vector2 zonedPosition, Zone.ZoneType zoneType);

        /// <summary>Removes grid position from tracked zoned-positions for specified zone type.</summary>
        void RemoveZonedPositionFromList(Vector2 zonedPosition, Zone.ZoneType zoneType, bool isConversionToBuilding = false);

        /// <summary>Clears all tracked zoned-position lists + available zone sections cache.</summary>
        void ClearZonedPositions();
    }
}
