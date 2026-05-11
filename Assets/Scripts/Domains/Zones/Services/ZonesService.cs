using System.Collections.Generic;
using UnityEngine;
using Territory.Zones;
using Domains.Grid;
using Domains.Water;
using Domains.Roads;

// Cross-domain interfaces wired for future use; position-tracking methods are assembly-safe (no concrete Manager types).

namespace Domains.Zones.Services
{
    /// <summary>
    /// Cross-domain POCO: owns zone-position tracking + IGrid/IWater/IRoads wiring.
    /// ZoneManager hub wires dependencies via WireDependencies(IGrid, IWater, IRoads) in Start.
    /// Cross-domain helpers that require concrete types live in ZoneManager (avoids circular asmdef reference).
    /// Invariant #11 preserved: no UrbanizationProposal reference.
    /// </summary>
    public class ZonesService
    {
        private IGrid _grid;
        private IWater _water;
        private IRoads _roads;

        // ── Position tracking ────────────────────────────────────────────────────────
        private readonly List<Vector2> _residentialLight  = new List<Vector2>();
        private readonly List<Vector2> _residentialMedium = new List<Vector2>();
        private readonly List<Vector2> _residentialHeavy  = new List<Vector2>();
        private readonly List<Vector2> _commercialLight   = new List<Vector2>();
        private readonly List<Vector2> _commercialMedium  = new List<Vector2>();
        private readonly List<Vector2> _commercialHeavy   = new List<Vector2>();
        private readonly List<Vector2> _industrialLight   = new List<Vector2>();
        private readonly List<Vector2> _industrialMedium  = new List<Vector2>();
        private readonly List<Vector2> _industrialHeavy   = new List<Vector2>();

        /// <summary>Wire cross-domain interfaces resolved from ServiceRegistry. Call from Start (never Awake).</summary>
        public void WireDependencies(IGrid grid, IWater water, IRoads roads)
        {
            _grid  = grid;
            _water = water;
            _roads = roads;
        }

        // ── Position tracking ────────────────────────────────────────────────────────

        /// <summary>Returns read-only tracked positions for given zone type.</summary>
        public IReadOnlyList<Vector2> GetZonedPositions(Zone.ZoneType zoneType)
        {
            switch (zoneType)
            {
                case Zone.ZoneType.ResidentialLightZoning:  return _residentialLight;
                case Zone.ZoneType.ResidentialMediumZoning: return _residentialMedium;
                case Zone.ZoneType.ResidentialHeavyZoning:  return _residentialHeavy;
                case Zone.ZoneType.CommercialLightZoning:   return _commercialLight;
                case Zone.ZoneType.CommercialMediumZoning:  return _commercialMedium;
                case Zone.ZoneType.CommercialHeavyZoning:   return _commercialHeavy;
                case Zone.ZoneType.IndustrialLightZoning:   return _industrialLight;
                case Zone.ZoneType.IndustrialMediumZoning:  return _industrialMedium;
                case Zone.ZoneType.IndustrialHeavyZoning:   return _industrialHeavy;
                default:                                    return new List<Vector2>();
            }
        }

        /// <summary>Add position to tracked list for zone type.</summary>
        public void AddPosition(Vector2 pos, Zone.ZoneType zoneType)
        {
            GetMutableList(zoneType)?.Add(pos);
        }

        /// <summary>Remove position from tracked list for zone type.</summary>
        public void RemovePosition(Vector2 pos, Zone.ZoneType zoneType)
        {
            GetMutableList(zoneType)?.Remove(pos);
        }

        /// <summary>Clear all tracked zone position lists.</summary>
        public void ClearAll()
        {
            _residentialLight.Clear(); _residentialMedium.Clear(); _residentialHeavy.Clear();
            _commercialLight.Clear();  _commercialMedium.Clear();  _commercialHeavy.Clear();
            _industrialLight.Clear();  _industrialMedium.Clear();  _industrialHeavy.Clear();
        }

        private List<Vector2> GetMutableList(Zone.ZoneType zoneType)
        {
            switch (zoneType)
            {
                case Zone.ZoneType.ResidentialLightZoning:  return _residentialLight;
                case Zone.ZoneType.ResidentialMediumZoning: return _residentialMedium;
                case Zone.ZoneType.ResidentialHeavyZoning:  return _residentialHeavy;
                case Zone.ZoneType.CommercialLightZoning:   return _commercialLight;
                case Zone.ZoneType.CommercialMediumZoning:  return _commercialMedium;
                case Zone.ZoneType.CommercialHeavyZoning:   return _commercialHeavy;
                case Zone.ZoneType.IndustrialLightZoning:   return _industrialLight;
                case Zone.ZoneType.IndustrialMediumZoning:  return _industrialMedium;
                case Zone.ZoneType.IndustrialHeavyZoning:   return _industrialHeavy;
                default:                                    return null;
            }
        }
    }
}
