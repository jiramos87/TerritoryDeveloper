using UnityEngine;

namespace Territory.Zones
{
/// <summary>
/// Defines the BuildingType enum for categorizing buildings (e.g., PowerPlant, WaterPlant).
/// </summary>
public class Building
{
    public enum BuildingType
    {
        None,
        Residential,
        Commercial,
        Industrial,
        Power,
        Water // Added Water type
    }
}
}
