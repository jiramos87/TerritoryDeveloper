using System.Collections.Generic;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    public List<GameObject> lightResidential1x1Prefabs;
    public List<GameObject> lightResidential2x2Prefabs;
    public List<GameObject> lightResidential3x3Prefabs;

    public List<GameObject> mediumResidential1x1Prefabs;
    public List<GameObject> mediumResidential2x2Prefabs;
    public List<GameObject> mediumResidential3x3Prefabs;

    public List<GameObject> heavyResidential1x1Prefabs;
    public List<GameObject> heavyResidential2x2Prefabs;
    public List<GameObject> heavyResidential3x3Prefabs;

    public List<GameObject> lightCommercial1x1Prefabs;
    public List<GameObject> lightCommercial2x2Prefabs;
    public List<GameObject> lightCommercial3x3Prefabs;

    public List<GameObject> mediumCommercial1x1Prefabs;
    public List<GameObject> mediumCommercial2x2Prefabs;
    public List<GameObject> mediumCommercial3x3Prefabs;

    public List<GameObject> heavyCommercial1x1Prefabs;
    public List<GameObject> heavyCommercial2x2Prefabs;
    public List<GameObject> heavyCommercial3x3Prefabs;

    public List<GameObject> lightIndustrial1x1Prefabs;
    public List<GameObject> lightIndustrial2x2Prefabs;
    public List<GameObject> lightIndustrial3x3Prefabs;

    public List<GameObject> mediumIndustrial1x1Prefabs;
    public List<GameObject> mediumIndustrial2x2Prefabs;
    public List<GameObject> mediumIndustrial3x3Prefabs;

    public List<GameObject> heavyIndustrial1x1Prefabs;
    public List<GameObject> heavyIndustrial2x2Prefabs;
    public List<GameObject> heavyIndustrial3x3Prefabs;

    public List<GameObject> residentialLightZoningPrefabs;
    public List<GameObject> residentialMediumZoningPrefabs;
    public List<GameObject> residentialHeavyZoningPrefabs;

    public List<GameObject> commercialLightZoningPrefabs;
    public List<GameObject> commercialMediumZoningPrefabs;
    public List<GameObject> commercialHeavyZoningPrefabs;

    public List<GameObject> industrialLightZoningPrefabs;
    public List<GameObject> industrialMediumZoningPrefabs;
    public List<GameObject> industrialHeavyZoningPrefabs;

    public List<GameObject> roadPrefabs;
    public List<GameObject> grassPrefabs;
    public List<GameObject> waterPrefabs;

    private Dictionary<(Zone.ZoneType, int), List<GameObject>> zonePrefabs;

    public GridManager gridManager;
    public PowerPlant powerPlantManager;
    public WaterPlant waterPlantManager;

    void Start()
    {
        zonePrefabs = new Dictionary<(Zone.ZoneType, int), List<GameObject>>
        {
            { (Zone.ZoneType.ResidentialLightBuilding, 1), lightResidential1x1Prefabs },
            { (Zone.ZoneType.ResidentialLightBuilding, 2), lightResidential2x2Prefabs },
            { (Zone.ZoneType.ResidentialLightBuilding, 3), lightResidential3x3Prefabs },
            { (Zone.ZoneType.ResidentialMediumBuilding, 1), mediumResidential1x1Prefabs },
            { (Zone.ZoneType.ResidentialMediumBuilding, 2), mediumResidential2x2Prefabs },
            { (Zone.ZoneType.ResidentialMediumBuilding, 3), mediumResidential3x3Prefabs },
            { (Zone.ZoneType.ResidentialHeavyBuilding, 1), heavyResidential1x1Prefabs },
            { (Zone.ZoneType.ResidentialHeavyBuilding, 2), heavyResidential2x2Prefabs },
            { (Zone.ZoneType.ResidentialHeavyBuilding, 3), heavyResidential3x3Prefabs },
            { (Zone.ZoneType.CommercialLightBuilding, 1), lightCommercial1x1Prefabs },
            { (Zone.ZoneType.CommercialLightBuilding, 2), lightCommercial2x2Prefabs },
            { (Zone.ZoneType.CommercialLightBuilding, 3), lightCommercial3x3Prefabs },
            { (Zone.ZoneType.CommercialMediumBuilding, 1), mediumCommercial1x1Prefabs },
            { (Zone.ZoneType.CommercialMediumBuilding, 2), mediumCommercial2x2Prefabs },
            { (Zone.ZoneType.CommercialMediumBuilding, 3), mediumCommercial3x3Prefabs },
            { (Zone.ZoneType.CommercialHeavyBuilding, 1), heavyCommercial1x1Prefabs },
            { (Zone.ZoneType.CommercialHeavyBuilding, 2), heavyCommercial2x2Prefabs },
            { (Zone.ZoneType.CommercialHeavyBuilding, 3), heavyCommercial3x3Prefabs },
            { (Zone.ZoneType.IndustrialLightBuilding, 1), lightIndustrial1x1Prefabs },
            { (Zone.ZoneType.IndustrialLightBuilding, 2), lightIndustrial2x2Prefabs },
            { (Zone.ZoneType.IndustrialLightBuilding, 3), lightIndustrial3x3Prefabs },
            { (Zone.ZoneType.IndustrialMediumBuilding, 1), mediumIndustrial1x1Prefabs },
            { (Zone.ZoneType.IndustrialMediumBuilding, 2), mediumIndustrial2x2Prefabs },
            { (Zone.ZoneType.IndustrialMediumBuilding, 3), mediumIndustrial3x3Prefabs },
            { (Zone.ZoneType.IndustrialHeavyBuilding, 1), heavyIndustrial1x1Prefabs },
            { (Zone.ZoneType.IndustrialHeavyBuilding, 2), heavyIndustrial2x2Prefabs },
            { (Zone.ZoneType.IndustrialHeavyBuilding, 3), heavyIndustrial3x3Prefabs },
            { (Zone.ZoneType.ResidentialLightZoning, 1), residentialLightZoningPrefabs },
            { (Zone.ZoneType.ResidentialMediumZoning, 1), residentialMediumZoningPrefabs },
            { (Zone.ZoneType.ResidentialHeavyZoning, 1), residentialHeavyZoningPrefabs },
            { (Zone.ZoneType.CommercialLightZoning, 1), commercialLightZoningPrefabs },
            { (Zone.ZoneType.CommercialMediumZoning, 1), commercialMediumZoningPrefabs },
            { (Zone.ZoneType.CommercialHeavyZoning, 1), commercialHeavyZoningPrefabs },
            { (Zone.ZoneType.IndustrialLightZoning, 1), industrialLightZoningPrefabs },
            { (Zone.ZoneType.IndustrialMediumZoning, 1), industrialMediumZoningPrefabs },
            { (Zone.ZoneType.IndustrialHeavyZoning, 1), industrialHeavyZoningPrefabs },
            { (Zone.ZoneType.Grass, 1), grassPrefabs },
            { (Zone.ZoneType.Road, 1), roadPrefabs },
            { (Zone.ZoneType.Water, 1), waterPrefabs }
        };
    }

    public GameObject GetRandomZonePrefab(Zone.ZoneType zoneType, int size = 1)
    {
        var key = (zoneType, size);

        if (!zonePrefabs.ContainsKey(key)) return null;

        List<GameObject> prefabs = zonePrefabs[key];
        if (prefabs.Count == 0) return null;

        return prefabs[Random.Range(0, prefabs.Count)];
    }

    public GameObject GetGrassPrefab()
    {
        return grassPrefabs[0];
    }

    public GameObject GetWaterPrefab()
    {
        return waterPrefabs[0];
    }

    public GameObject FindPrefabByName(string prefabName)
    {
        string trimmedName = prefabName.Replace("(Clone)", "");

        List<GameObject> roadPrefabs = gridManager.GetRoadPrefabs();
        List<GameObject> powerPlantPrefabs = powerPlantManager.GetPowerPlantPrefabs();
        List<GameObject> waterPlantPrefabs = waterPlantManager.GetWaterPlantPrefabs();

        if (roadPrefabs == null || powerPlantPrefabs == null || waterPlantPrefabs == null)
        {
            Debug.LogWarning("One or more prefab lists are null.");
            return null;
        }

        foreach (var prefabList in zonePrefabs.Values)
        {
            foreach (GameObject prefab in prefabList)
            {
                if (prefab.name == trimmedName)
                {
                    return prefab;
                }
            }
        }

        foreach (var prefab in roadPrefabs)
        {
            if (prefab.name == trimmedName)
            {
                return prefab;
            }
        }

        foreach (var prefab in powerPlantPrefabs)
        {
            if (prefab.name == trimmedName)
            {
                return prefab;
            }
        }

        foreach (var prefab in waterPlantPrefabs)
        {
            if (prefab.name == trimmedName)
            {
                return prefab;
            }
        }

        Debug.LogWarning($"Prefab with name {trimmedName} not found.");
        return null;
    }
}