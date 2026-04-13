using System.Collections.Generic;
using UnityEngine;
using Territory.Roads;
using Territory.Economy;
using Territory.Core;

namespace Territory.Geography
{
/// <summary>Manage regional map: generation, neighbor data queries for interstate routing + border signs.</summary>
public class RegionalMapManager : MonoBehaviour
{
    [Header("References")]
    public InterstateManager interstateManager;
    public CityStats cityStats;
    public GridManager gridManager;

    [Header("Regional Map Settings")]
    [Tooltip("Width of the regional territory grid")]
    public int regionWidth = 5;
    [Tooltip("Height of the regional territory grid")]
    public int regionHeight = 5;

    [Header("Interstate Sign")]
    [Tooltip("Prefab for the highway sign placed at map borders")]
    public GameObject interstateSignPrefab;

    private RegionalMap regionalMap;
    private List<GameObject> activeSignInstances = new List<GameObject>();

    public RegionalMap GetRegionalMap()
    {
        return regionalMap;
    }

    public void InitializeRegionalMap(int seed = -1)
    {
        regionalMap = RegionalMap.Generate(regionWidth, regionHeight, seed);

        TerritoryData playerTerritory = regionalMap.GetPlayerTerritory();
        if (cityStats != null && playerTerritory != null)
            cityStats.cityName = playerTerritory.cityName;
    }

    public bool TryGetInterstateBorders(out int borderA, out int borderB)
    {
        borderA = 0;
        borderB = 1;

        if (regionalMap == null)
            return false;

        TerritoryData player = regionalMap.GetPlayerTerritory();
        if (player == null)
            return false;

        List<int> connected = player.GetConnectedBorders();
        if (connected.Count == 0)
            return false;

        borderA = connected[0];
        int opposite = TerritoryData.OppositeBorder(borderA);
        borderB = connected.Contains(opposite) ? opposite : (connected.Count >= 2 ? connected[1] : opposite);

        return true;
    }

    public struct BorderSignInfo
    {
        public int border;
        public string destinationCity;
        public int destinationPopulation;
        public TerritoryData.CityCategory destinationCategory;
        public Vector2Int borderCellPosition;
    }

    public List<BorderSignInfo> GetBorderSignData()
    {
        var signs = new List<BorderSignInfo>();
        if (regionalMap == null || interstateManager == null)
            return signs;

        if (interstateManager.EntryBorder >= 0 && interstateManager.EntryPoint.HasValue)
        {
            var neighbor = regionalMap.GetPlayerNeighbor(interstateManager.EntryBorder);
            if (neighbor != null && neighbor.category != TerritoryData.CityCategory.Uninhabited)
            {
                signs.Add(new BorderSignInfo
                {
                    border = interstateManager.EntryBorder,
                    destinationCity = neighbor.cityName,
                    destinationPopulation = neighbor.population,
                    destinationCategory = neighbor.category,
                    borderCellPosition = interstateManager.EntryPoint.Value
                });
            }
        }

        if (interstateManager.ExitBorder >= 0 && interstateManager.ExitPoint.HasValue)
        {
            var neighbor = regionalMap.GetPlayerNeighbor(interstateManager.ExitBorder);
            if (neighbor != null && neighbor.category != TerritoryData.CityCategory.Uninhabited)
            {
                signs.Add(new BorderSignInfo
                {
                    border = interstateManager.ExitBorder,
                    destinationCity = neighbor.cityName,
                    destinationPopulation = neighbor.population,
                    destinationCategory = neighbor.category,
                    borderCellPosition = interstateManager.ExitPoint.Value
                });
            }
        }

        return signs;
    }

    public void PlaceBorderSigns()
    {
        ClearBorderSigns();

        if (interstateSignPrefab == null || gridManager == null)
            return;

        List<BorderSignInfo> signData = GetBorderSignData();

        foreach (var info in signData)
        {
            Vector2Int gridPos = info.borderCellPosition;

            CityCell cell = gridManager.GetCell(gridPos.x, gridPos.y);
            if (cell == null) continue;

            Vector2 worldPos = gridManager.GetCellWorldPosition(cell);
            Vector3 signWorldPos = new Vector3(worldPos.x, worldPos.y + 0.35f, 0f);

            GameObject signObj = Instantiate(interstateSignPrefab, signWorldPos, Quaternion.identity);
            signObj.transform.SetParent(cell.gameObject.transform);

            InterstateSign sign = signObj.GetComponent<InterstateSign>();
            if (sign != null)
            {
                sign.Initialize(
                    info.destinationCity,
                    info.destinationPopulation,
                    info.destinationCategory,
                    info.border,
                    cell.sortingOrder
                );
            }

            activeSignInstances.Add(signObj);
        }
    }

    public void ClearBorderSigns()
    {
        foreach (var signObj in activeSignInstances)
        {
            if (signObj != null)
                Destroy(signObj);
        }
        activeSignInstances.Clear();
    }

    /// <summary>
    /// Copy current city name from CityStats → player territory in regional map.
    /// Call before save → saved map contains (possibly player-edited) name.
    /// </summary>
    public void SyncCityNameToPlayerTerritory()
    {
        if (regionalMap == null || cityStats == null) return;
        var player = regionalMap.GetPlayerTerritory();
        if (player != null && !string.IsNullOrEmpty(cityStats.cityName))
            player.cityName = cityStats.cityName;
    }

    public RegionalMap GetRegionalMapForSave()
    {
        return regionalMap;
    }

    public void RestoreRegionalMap(RegionalMap savedMap)
    {
        regionalMap = savedMap;

        if (cityStats != null && regionalMap != null)
        {
            TerritoryData player = regionalMap.GetPlayerTerritory();
            if (player != null)
                cityStats.cityName = player.cityName;
        }
    }
}
}
