using System.Collections.Generic;
using UnityEngine;
using Territory.Zones;

namespace Domains.Zones.Services
{
    /// <summary>
    /// Pure service: maps (ZoneType, size) → prefab list. Extracted from ZoneManager prefab-dict init logic.
    /// No MonoBehaviour dependency. Caller (ZoneManager) builds + passes prefab lists at init time.
    /// Invariant #11 preserved: no UrbanizationProposal reference.
    /// </summary>
    public class ZonePrefabRegistry
    {
        private readonly Dictionary<(Zone.ZoneType, int), List<GameObject>> _prefabs;

        /// <summary>Construct registry with zone+size→prefab list dictionary.</summary>
        public ZonePrefabRegistry(Dictionary<(Zone.ZoneType, int), List<GameObject>> prefabs)
        {
            _prefabs = prefabs ?? new Dictionary<(Zone.ZoneType, int), List<GameObject>>();
        }

        /// <summary>
        /// Returns random prefab for zone type + size with industrial fallback chain.
        /// Returns null if no prefabs registered.
        /// </summary>
        public GameObject GetRandom(Zone.ZoneType zoneType, int size = 1)
        {
            var key = (zoneType, size);
            if (!_prefabs.TryGetValue(key, out var list) || list == null || list.Count == 0)
            {
                if (zoneType == Zone.ZoneType.IndustrialHeavyBuilding)
                    return GetRandom(Zone.ZoneType.IndustrialMediumBuilding, size);
                if (zoneType == Zone.ZoneType.IndustrialMediumBuilding)
                    return GetRandom(Zone.ZoneType.IndustrialLightBuilding, size);
                return null;
            }
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>Returns all prefab lists (for search operations).</summary>
        public IEnumerable<List<GameObject>> AllLists() => _prefabs.Values;

        /// <summary>Returns true if registry contains the given key.</summary>
        public bool HasKey(Zone.ZoneType zoneType, int size) => _prefabs.ContainsKey((zoneType, size));
    }
}
