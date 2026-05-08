using Territory.Zones;

namespace Domains.Zones.Services
{
    /// <summary>
    /// Pure service: zone-type classification queries extracted from ZoneManager.
    /// No MonoBehaviour dependency. Static helpers made instance methods for interface-ability.
    /// Invariant #11 preserved: no UrbanizationProposal reference.
    /// </summary>
    public class ZonePlacementService
    {
        /// <summary>Returns ZoneAttributes for the given zone type; null if unrecognized.</summary>
        public ZoneAttributes GetZoneAttributes(Zone.ZoneType zoneType)
        {
            switch (zoneType)
            {
                case Zone.ZoneType.ResidentialLightZoning:   return ZoneAttributes.ResidentialLightZoning;
                case Zone.ZoneType.ResidentialMediumZoning:  return ZoneAttributes.ResidentialMediumZoning;
                case Zone.ZoneType.ResidentialHeavyZoning:   return ZoneAttributes.ResidentialHeavyZoning;
                case Zone.ZoneType.ResidentialLightBuilding: return ZoneAttributes.ResidentialLightBuilding;
                case Zone.ZoneType.ResidentialMediumBuilding:return ZoneAttributes.ResidentialMediumBuilding;
                case Zone.ZoneType.ResidentialHeavyBuilding: return ZoneAttributes.ResidentialHeavyBuilding;
                case Zone.ZoneType.CommercialLightZoning:    return ZoneAttributes.CommercialLightZoning;
                case Zone.ZoneType.CommercialMediumZoning:   return ZoneAttributes.CommercialMediumZoning;
                case Zone.ZoneType.CommercialHeavyZoning:    return ZoneAttributes.CommercialHeavyZoning;
                case Zone.ZoneType.CommercialLightBuilding:  return ZoneAttributes.CommercialLightBuilding;
                case Zone.ZoneType.CommercialMediumBuilding: return ZoneAttributes.CommercialMediumBuilding;
                case Zone.ZoneType.CommercialHeavyBuilding:  return ZoneAttributes.CommercialHeavyBuilding;
                case Zone.ZoneType.IndustrialLightZoning:    return ZoneAttributes.IndustrialLightZoning;
                case Zone.ZoneType.IndustrialMediumZoning:   return ZoneAttributes.IndustrialMediumZoning;
                case Zone.ZoneType.IndustrialHeavyZoning:    return ZoneAttributes.IndustrialHeavyZoning;
                case Zone.ZoneType.IndustrialLightBuilding:  return ZoneAttributes.IndustrialLightBuilding;
                case Zone.ZoneType.IndustrialMediumBuilding: return ZoneAttributes.IndustrialMediumBuilding;
                case Zone.ZoneType.IndustrialHeavyBuilding:  return ZoneAttributes.IndustrialHeavyBuilding;
                case Zone.ZoneType.Road:                     return ZoneAttributes.Road;
                case Zone.ZoneType.Grass:                    return ZoneAttributes.Grass;
                case Zone.ZoneType.Water:                    return ZoneAttributes.Water;
                default:                                     return null;
            }
        }

        /// <summary>Maps zoning overlay type → corresponding building zone type (e.g. ResidentialLightZoning → ResidentialLightBuilding).</summary>
        public Zone.ZoneType GetBuildingZoneType(Zone.ZoneType zoningType)
        {
            switch (zoningType)
            {
                case Zone.ZoneType.ResidentialLightZoning:  return Zone.ZoneType.ResidentialLightBuilding;
                case Zone.ZoneType.ResidentialMediumZoning: return Zone.ZoneType.ResidentialMediumBuilding;
                case Zone.ZoneType.ResidentialHeavyZoning:  return Zone.ZoneType.ResidentialHeavyBuilding;
                case Zone.ZoneType.CommercialLightZoning:   return Zone.ZoneType.CommercialLightBuilding;
                case Zone.ZoneType.CommercialMediumZoning:  return Zone.ZoneType.CommercialMediumBuilding;
                case Zone.ZoneType.CommercialHeavyZoning:   return Zone.ZoneType.CommercialHeavyBuilding;
                case Zone.ZoneType.IndustrialLightZoning:   return Zone.ZoneType.IndustrialLightBuilding;
                case Zone.ZoneType.IndustrialMediumZoning:  return Zone.ZoneType.IndustrialMediumBuilding;
                case Zone.ZoneType.IndustrialHeavyZoning:   return Zone.ZoneType.IndustrialHeavyBuilding;
                default:                                    return Zone.ZoneType.Grass;
            }
        }

        /// <summary>Returns true if zone type is a zoning overlay (empty zone awaiting building spawn).</summary>
        public bool IsZoningType(Zone.ZoneType zoneType)
        {
            return zoneType == Zone.ZoneType.ResidentialLightZoning
                || zoneType == Zone.ZoneType.ResidentialMediumZoning
                || zoneType == Zone.ZoneType.ResidentialHeavyZoning
                || zoneType == Zone.ZoneType.CommercialLightZoning
                || zoneType == Zone.ZoneType.CommercialMediumZoning
                || zoneType == Zone.ZoneType.CommercialHeavyZoning
                || zoneType == Zone.ZoneType.IndustrialLightZoning
                || zoneType == Zone.ZoneType.IndustrialMediumZoning
                || zoneType == Zone.ZoneType.IndustrialHeavyZoning;
        }

        /// <summary>Returns true if zone type is any residential building density.</summary>
        public bool IsResidentialBuilding(Zone.ZoneType zoneType)
        {
            return zoneType == Zone.ZoneType.ResidentialLightBuilding
                || zoneType == Zone.ZoneType.ResidentialMediumBuilding
                || zoneType == Zone.ZoneType.ResidentialHeavyBuilding;
        }

        /// <summary>Returns true if zone type is any commercial or industrial building density.</summary>
        public bool IsCommercialOrIndustrialBuilding(Zone.ZoneType zoneType)
        {
            return zoneType == Zone.ZoneType.CommercialLightBuilding
                || zoneType == Zone.ZoneType.CommercialMediumBuilding
                || zoneType == Zone.ZoneType.CommercialHeavyBuilding
                || zoneType == Zone.ZoneType.IndustrialLightBuilding
                || zoneType == Zone.ZoneType.IndustrialMediumBuilding
                || zoneType == Zone.ZoneType.IndustrialHeavyBuilding;
        }

        /// <summary>Returns true if zone type is any state-service zone type.</summary>
        public bool IsStateServiceZoneType(Zone.ZoneType zoneType)
        {
            return zoneType == Zone.ZoneType.StateServiceLightBuilding
                || zoneType == Zone.ZoneType.StateServiceMediumBuilding
                || zoneType == Zone.ZoneType.StateServiceHeavyBuilding
                || zoneType == Zone.ZoneType.StateServiceLightZoning
                || zoneType == Zone.ZoneType.StateServiceMediumZoning
                || zoneType == Zone.ZoneType.StateServiceHeavyZoning;
        }

        /// <summary>Parses string → Zone.ZoneType; returns Grass on failure.</summary>
        public Zone.ZoneType ParseZoneType(string zoneTypeString)
        {
            if (string.IsNullOrEmpty(zoneTypeString))
                return Zone.ZoneType.Grass;
            if (System.Enum.TryParse(zoneTypeString, out Zone.ZoneType result))
                return result;
            return Zone.ZoneType.Grass;
        }
    }
}
