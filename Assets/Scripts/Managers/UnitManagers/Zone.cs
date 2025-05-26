using UnityEngine;

public class Zone : MonoBehaviour
{
    public enum ZoneType
    {
        ResidentialLightBuilding,
        ResidentialMediumBuilding,
        ResidentialHeavyBuilding,
        CommercialLightBuilding,
        CommercialMediumBuilding,
        CommercialHeavyBuilding,
        IndustrialLightBuilding,
        IndustrialMediumBuilding,
        IndustrialHeavyBuilding,
        ResidentialLightZoning,
        ResidentialMediumZoning,
        ResidentialHeavyZoning,
        CommercialLightZoning,
        CommercialMediumZoning,
        CommercialHeavyZoning,
        IndustrialLightZoning,
        IndustrialMediumZoning,
        IndustrialHeavyZoning,
        Road,
        Grass,
        Water,
        Forest, // New forest type for tree placement
        None,
        Building
    }

    public enum ZoneCategory
    {
        Zoning,
        Road,
        Grass,
        Water,
        Forest, // New forest category
        Building
    }
    
    public ZoneType zoneType;
    public ZoneCategory zoneCategory;

    void Start()
    {
        // Initialize zone-specific properties
        SetZoneCategoryFromType();
    }

    /// <summary>
    /// Automatically set zone category based on zone type
    /// </summary>
    private void SetZoneCategoryFromType()
    {
        switch (zoneType)
        {
            case ZoneType.ResidentialLightBuilding:
            case ZoneType.ResidentialMediumBuilding:
            case ZoneType.ResidentialHeavyBuilding:
            case ZoneType.CommercialLightBuilding:
            case ZoneType.CommercialMediumBuilding:
            case ZoneType.CommercialHeavyBuilding:
            case ZoneType.IndustrialLightBuilding:
            case ZoneType.IndustrialMediumBuilding:
            case ZoneType.IndustrialHeavyBuilding:
            case ZoneType.Building:
                zoneCategory = ZoneCategory.Building;
                break;
                
            case ZoneType.ResidentialLightZoning:
            case ZoneType.ResidentialMediumZoning:
            case ZoneType.ResidentialHeavyZoning:
            case ZoneType.CommercialLightZoning:
            case ZoneType.CommercialMediumZoning:
            case ZoneType.CommercialHeavyZoning:
            case ZoneType.IndustrialLightZoning:
            case ZoneType.IndustrialMediumZoning:
            case ZoneType.IndustrialHeavyZoning:
                zoneCategory = ZoneCategory.Zoning;
                break;
                
            case ZoneType.Road:
                zoneCategory = ZoneCategory.Road;
                break;
                
            case ZoneType.Grass:
                zoneCategory = ZoneCategory.Grass;
                break;
                
            case ZoneType.Water:
                zoneCategory = ZoneCategory.Water;
                break;
                
            case ZoneType.Forest:
                zoneCategory = ZoneCategory.Forest;
                break;
                
            default:
                zoneCategory = ZoneCategory.Grass;
                break;
        }
    }

    /// <summary>
    /// Check if this zone type is a building
    /// </summary>
    public bool IsBuilding()
    {
        return zoneCategory == ZoneCategory.Building;
    }

    /// <summary>
    /// Check if this zone type is environmental (forest, water, grass)
    /// </summary>
    public bool IsEnvironmental()
    {
        return zoneCategory == ZoneCategory.Forest || 
               zoneCategory == ZoneCategory.Water || 
               zoneCategory == ZoneCategory.Grass;
    }

    /// <summary>
    /// Check if this zone type affects desirability
    /// </summary>
    public bool AffectsDesirability()
    {
        return zoneCategory == ZoneCategory.Forest || zoneCategory == ZoneCategory.Water;
    }

    /// <summary>
    /// Get the environmental impact value for this zone type
    /// </summary>
    public float GetEnvironmentalImpact()
    {
        switch (zoneType)
        {
            case ZoneType.Forest:
                return 2.0f; // Positive environmental impact
            case ZoneType.Water:
                return 3.0f; // Higher positive environmental impact
            case ZoneType.Grass:
                return 0.5f; // Minimal positive impact
            case ZoneType.IndustrialLightBuilding:
            case ZoneType.IndustrialMediumBuilding:
            case ZoneType.IndustrialHeavyBuilding:
                return -1.0f; // Negative environmental impact
            default:
                return 0f; // Neutral impact
        }
    }
}
