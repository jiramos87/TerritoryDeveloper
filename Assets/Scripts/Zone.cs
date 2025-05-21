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
      None,
      Building
    }

    public enum ZoneCategory
    {
      Zoning,
      Road,
      Grass,
      Water,
      Building
    }
    public ZoneType zoneType;
    public ZoneCategory zoneCategory;

    void Start()
    {
        // Initialize zone-specific properties
    }
}