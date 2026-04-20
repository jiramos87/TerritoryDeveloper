using UnityEngine;

namespace Territory.Zones
{
/// <summary>
/// MonoBehaviour attached to zone GameObjects.
/// Stores ZoneType (residential, commercial, industrial, road, etc.), zone category, building level.
/// </summary>
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
        Building,
        // Zone S — State Service sub-types (ints 24–29); append-only for save-file compatibility
        StateServiceLightBuilding,
        StateServiceMediumBuilding,
        StateServiceHeavyBuilding,
        StateServiceLightZoning,
        StateServiceMediumZoning,
        StateServiceHeavyZoning
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

    /// <summary>Zone S sub-type index. -1 = "RCI, no sub-type" (all legacy zones). Indexes a ZoneSubTypeRegistry row (TECH-280).</summary>
    [SerializeField] private int subTypeId = -1;
    public int SubTypeId { get => subTypeId; set => subTypeId = value; }

    void Awake()
    {
        SetZoneCategoryFromType();
    }

    void Start()
    {
        SetZoneCategoryFromType();
    }

    /// <summary>Auto-set zone category from zone type.</summary>
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
            case ZoneType.StateServiceLightBuilding:
            case ZoneType.StateServiceMediumBuilding:
            case ZoneType.StateServiceHeavyBuilding:
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
            case ZoneType.StateServiceLightZoning:
            case ZoneType.StateServiceMediumZoning:
            case ZoneType.StateServiceHeavyZoning:
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

    /// <summary>True if zone type is building.</summary>
    public bool IsBuilding()
    {
        return zoneCategory == ZoneCategory.Building;
    }

    /// <summary>True if zone type environmental (forest, water, grass).</summary>
    public bool IsEnvironmental()
    {
        return zoneCategory == ZoneCategory.Forest ||
               zoneCategory == ZoneCategory.Water ||
               zoneCategory == ZoneCategory.Grass;
    }

    /// <summary>True if zone type affects desirability.</summary>
    public bool AffectsDesirability()
    {
        return zoneCategory == ZoneCategory.Forest || zoneCategory == ZoneCategory.Water;
    }

    /// <summary>Environmental impact value for zone type.</summary>
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
}
