using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Economy;
using Territory.UI;
using Territory.Buildings;
using Territory.Zones;

namespace Territory.Simulation
{
/// <summary>
/// Auto-plan + place resource buildings (power plants, water plants) when city capacity insufficient.
/// Coords with <see cref="CityStats"/> (capacity checks) + <see cref="GridManager"/> (placement validation).
/// </summary>
public class AutoResourcePlanner : MonoBehaviour
{
    public CityStats cityStats;
    public GridManager gridManager;
    public GrowthBudgetManager growthBudgetManager;

    public GameObject powerPlantPrefab;
    public GameObject waterPlantPrefab;
    public float powerThreshold = 0.9f;
    public float waterThreshold = 0.9f;

    private const int PowerPlantCost = 5000;
    private const int PowerPlantSize = 3;
    private const int WaterPlantCost = 4000;
    private const int WaterPlantSize = 2;

    void Start()
    {
        if (cityStats == null) cityStats = FindObjectOfType<CityStats>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (growthBudgetManager == null) growthBudgetManager = FindObjectOfType<GrowthBudgetManager>();
        if (powerPlantPrefab == null)
        {
            var ui = FindObjectOfType<UIManager>();
            if (ui != null) powerPlantPrefab = ui.powerPlantAPrefab;
        }
        if (waterPlantPrefab == null)
        {
            var ui = FindObjectOfType<UIManager>();
            if (ui != null) waterPlantPrefab = ui.waterPumpPrefab;
        }
    }

    public void ProcessTick()
    {
        if (cityStats == null || gridManager == null || growthBudgetManager == null)
            return;
        if (!cityStats.simulateGrowth)
            return;

        // When no plants exist, treat any demand as "need first plant" (ratio = infinity), not 0.
        bool needPower = cityStats.cityPowerOutput > 0
            ? (float)cityStats.cityPowerConsumption / cityStats.cityPowerOutput >= powerThreshold
            : cityStats.cityPowerConsumption > 0;
        if (needPower && powerPlantPrefab != null)
            TryBuildPowerPlant();

        bool needWater = cityStats.cityWaterOutput > 0
            ? (float)cityStats.cityWaterConsumption / cityStats.cityWaterOutput >= waterThreshold
            : cityStats.cityWaterConsumption > 0;
        if (needWater && waterPlantPrefab != null)
            TryBuildWaterPlant();
    }

    private void TryBuildPowerPlant()
    {
        if (!cityStats.CanAfford(PowerPlantCost))
            return;

        Vector2Int? pos = FindPlacementAdjacentToRoad(PowerPlantSize) ?? FindPlacementAdjacentToAnyRoad(PowerPlantSize, true);
        if (!pos.HasValue) return;

        GameObject templateGo = Instantiate(powerPlantPrefab);
        templateGo.SetActive(false);
        var pp = templateGo.GetComponent<PowerPlant>();
        if (pp == null) pp = templateGo.AddComponent<PowerPlant>();
        pp.Initialize("Power Plant A", PowerPlantCost, 100, 50, 25, PowerPlantSize, 20000, powerPlantPrefab);

        bool spent = growthBudgetManager.GetAvailableBudget(GrowthCategory.Energy) >= PowerPlantCost
            && growthBudgetManager.TrySpend(GrowthCategory.Energy, PowerPlantCost);
        if (!spent)
        {
            Destroy(templateGo);
            return;
        }

        if (gridManager.PlaceBuildingProgrammatic(new Vector2(pos.Value.x, pos.Value.y), pp))
        {
            if (GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostInfo("Auto-built Power Plant at " + pos.Value);
        }
        else
            growthBudgetManager.RefundSpend(GrowthCategory.Energy, PowerPlantCost);
        Destroy(templateGo);
    }

    private void TryBuildWaterPlant()
    {
        if (!cityStats.CanAfford(WaterPlantCost))
            return;

        Vector2Int? pos = FindPlacementAdjacentToRoad(WaterPlantSize) ?? FindPlacementAdjacentToAnyRoad(WaterPlantSize, false);
        if (!pos.HasValue) return;

        GameObject templateGo = Instantiate(waterPlantPrefab);
        templateGo.SetActive(false);
        var wp = templateGo.GetComponent<WaterPlant>();
        if (wp == null) wp = templateGo.AddComponent<WaterPlant>();
        wp.Initialize("Water Pump", WaterPlantCost, 80, 30, 20, WaterPlantSize, 16000, waterPlantPrefab);

        bool spent = growthBudgetManager.GetAvailableBudget(GrowthCategory.Water) >= WaterPlantCost
            && growthBudgetManager.TrySpend(GrowthCategory.Water, WaterPlantCost);
        if (!spent)
        {
            Destroy(templateGo);
            return;
        }

        if (gridManager.PlaceBuildingProgrammatic(new Vector2(pos.Value.x, pos.Value.y), wp))
        {
            if (GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostInfo("Auto-built Water Plant at " + pos.Value);
        }
        else
            growthBudgetManager.RefundSpend(GrowthCategory.Water, WaterPlantCost);
        Destroy(templateGo);
    }

    private Vector2Int? FindPlacementAdjacentToRoad(int buildingSize)
    {
        var edges = gridManager.GetRoadEdgePositions();
        var shuffled = new List<Vector2Int>(edges);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var t = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = t;
        }
        foreach (Vector2Int road in shuffled)
        {
            int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
            for (int d = 0; d < 4; d++)
            {
                int cx = road.x + dx[d], cy = road.y + dy[d];
                if (cx >= 0 && cx < gridManager.width && cy >= 0 && cy < gridManager.height)
                {
                    if (gridManager.canPlaceBuilding(new Vector2(cx, cy), buildingSize, buildingSize == WaterPlantSize))
                        return new Vector2Int(cx, cy);
                }
            }
        }
        return null;
    }

    /// <summary>Fallback: search any cell adjacent to any road (not just edges) when edges scarce.</summary>
    private Vector2Int? FindPlacementAdjacentToAnyRoad(int buildingSize, bool isPowerPlant)
    {
        var roads = gridManager.GetAllRoadPositions();
        var candidates = new HashSet<Vector2Int>();
        int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
        foreach (Vector2Int r in roads)
        {
            for (int d = 0; d < 4; d++)
            {
                int cx = r.x + dx[d], cy = r.y + dy[d];
                if (cx >= 0 && cx < gridManager.width && cy >= 0 && cy < gridManager.height)
                    candidates.Add(new Vector2Int(cx, cy));
            }
        }
        var list = new List<Vector2Int>(candidates);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var t = list[i];
            list[i] = list[j];
            list[j] = t;
        }
        foreach (Vector2Int c in list)
        {
            if (gridManager.canPlaceBuilding(new Vector2(c.x, c.y), buildingSize, !isPowerPlant))
                return c;
        }
        return null;
    }
}
}
