using UnityEngine;
using System.Collections.Generic;

namespace Territory.Terrain
{
/// <summary>
/// Registry for slope-variant prefabs (zoning overlays + buildings).
/// Holds flat list of all slope prefabs. Awake builds name-keyed dictionary.
/// Given flat (base) prefab + TerrainSlopeType, derive slope variant name via convention ({baseName}_{slopeCode}Slope) + return match.
/// </summary>
public class SlopePrefabRegistry : MonoBehaviour
{
    [Header("All slope zoning + building prefabs")]
    public List<GameObject> slopePrefabs;

    private Dictionary<string, GameObject> registry;

    void Awake()
    {
        BuildRegistry();
    }

    /// <summary>Build name-to-prefab dictionary from slopePrefabs list.</summary>
    public void BuildRegistry()
    {
        registry = new Dictionary<string, GameObject>();
        if (slopePrefabs == null) return;
        foreach (GameObject p in slopePrefabs)
        {
            if (p != null && !registry.ContainsKey(p.name))
                registry[p.name] = p;
        }
    }

    /// <summary>
    /// Return slope variant of flatPrefab for given slope type, or null if no variant in registry.
    /// Return flatPrefab unchanged if slopeType is Flat.
    /// </summary>
    public GameObject GetSlopeVariant(GameObject flatPrefab, TerrainSlopeType slopeType)
    {
        if (flatPrefab == null || slopeType == TerrainSlopeType.Flat)
            return flatPrefab;

        string suffix = GetSlopeSuffix(slopeType);
        if (suffix == null) return flatPrefab;

        string slopeName = flatPrefab.name + suffix;
        if (registry != null && registry.TryGetValue(slopeName, out GameObject variant))
            return variant;

        return null;
    }

    /// <summary>Find prefab by exact name in slope registry. Used by FindPrefabByName for save/load.</summary>
    public GameObject FindByName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return null;
        string trimmed = prefabName.Replace("(Clone)", "");
        if (registry != null && registry.TryGetValue(trimmed, out GameObject p))
            return p;
        return null;
    }

    /// <summary>Return suffix string for given slope type (e.g. North → "_NSlope").</summary>
    public static string GetSlopeSuffix(TerrainSlopeType slopeType)
    {
        switch (slopeType)
        {
            case TerrainSlopeType.North: return "_NSlope";
            case TerrainSlopeType.South: return "_SSlope";
            case TerrainSlopeType.East: return "_ESlope";
            case TerrainSlopeType.West: return "_WSlope";
            case TerrainSlopeType.NorthEast: return "_NESlope";
            case TerrainSlopeType.NorthWest: return "_NWSlope";
            case TerrainSlopeType.SouthEast: return "_SESlope";
            case TerrainSlopeType.SouthWest: return "_SWSlope";
            case TerrainSlopeType.NorthEastUp: return "_NEUpSlope";
            case TerrainSlopeType.NorthWestUp: return "_NWUpSlope";
            case TerrainSlopeType.SouthEastUp: return "_SEUpSlope";
            case TerrainSlopeType.SouthWestUp: return "_SWUpSlope";
            default: return null;
        }
    }

    /// <summary>Extract slope type from prefab name matching naming convention. Return Flat if no slope suffix.</summary>
    public static TerrainSlopeType ParseSlopeFromName(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName)) return TerrainSlopeType.Flat;
        if (prefabName.EndsWith("_NEUpSlope")) return TerrainSlopeType.NorthEastUp;
        if (prefabName.EndsWith("_NWUpSlope")) return TerrainSlopeType.NorthWestUp;
        if (prefabName.EndsWith("_SEUpSlope")) return TerrainSlopeType.SouthEastUp;
        if (prefabName.EndsWith("_SWUpSlope")) return TerrainSlopeType.SouthWestUp;
        if (prefabName.EndsWith("_NESlope")) return TerrainSlopeType.NorthEast;
        if (prefabName.EndsWith("_NWSlope")) return TerrainSlopeType.NorthWest;
        if (prefabName.EndsWith("_SESlope")) return TerrainSlopeType.SouthEast;
        if (prefabName.EndsWith("_SWSlope")) return TerrainSlopeType.SouthWest;
        if (prefabName.EndsWith("_NSlope")) return TerrainSlopeType.North;
        if (prefabName.EndsWith("_SSlope")) return TerrainSlopeType.South;
        if (prefabName.EndsWith("_ESlope")) return TerrainSlopeType.East;
        if (prefabName.EndsWith("_WSlope")) return TerrainSlopeType.West;
        return TerrainSlopeType.Flat;
    }
}
}
