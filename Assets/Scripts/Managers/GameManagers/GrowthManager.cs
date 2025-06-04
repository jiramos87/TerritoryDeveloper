using UnityEngine;
using System.Collections.Generic;

public class GrowthManager : MonoBehaviour
{
    [Header("Auto-Growth Configuration")]
    public bool autoGrowthEnabled = true;
    public float zoningSucessRate = 0.7f; // Probability of successful auto-zoning
    public int maxZonesPerGrowth = 3; // Maximum zones to create per growth event
    public int searchRadius = 10; // Radius to search for suitable growth locations

    [Header("Growth Preferences")]
    public bool preferAdjacentToExisting = true;
    public bool avoidOvercrowding = true;
    public float maxDensityFactor = 0.8f; // Maximum allowed density before avoiding an area

    [Header("Demand-Based Growth")]
    public bool respectDemandLimits = true; // NEW: Whether to check demand before growth
    public float demandMultiplier = 1.0f; // NEW: Multiplier for success rate based on demand

    private GridManager gridManager;
    private DemandManager demandManager;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
        demandManager = FindObjectOfType<DemandManager>();
    }

    public void TriggerAutoGrowth(Zone.ZoneType zoneType)
    {
        if (!autoGrowthEnabled || gridManager == null) return;

        // NEW: Check demand before attempting growth
        if (respectDemandLimits && demandManager != null)
        {
            if (!demandManager.CanZoneTypeGrow(zoneType))
            {
                return;
            }
        }

        List<Vector2> suitableLocations = FindSuitableGrowthLocations(zoneType);

        if (suitableLocations.Count > 0)
        {
            int zonesToPlace = Mathf.Min(maxZonesPerGrowth, suitableLocations.Count);

            // NEW: Adjust success rate based on demand level
            float adjustedSuccessRate = CalculateAdjustedSuccessRate(zoneType);

            int placedZones = 0;
            for (int i = 0; i < zonesToPlace; i++)
            {
                if (Random.value <= adjustedSuccessRate)
                {
                    Vector2 location = suitableLocations[Random.Range(0, suitableLocations.Count)];

                    // NEW: Double-check demand before each placement
                    if (CanPlaceBasedOnDemand(zoneType))
                    {
                        PlaceAutoZone(location, zoneType);
                        placedZones++;
                    }

                    suitableLocations.Remove(location);
                }
            }
        }
    }

    // NEW: Calculate success rate adjusted by demand level
    private float CalculateAdjustedSuccessRate(Zone.ZoneType zoneType)
    {
        float baseRate = zoningSucessRate;

        if (demandManager == null || !respectDemandLimits)
            return baseRate;

        float demandLevel = demandManager.GetDemandLevel(zoneType);

        // Normalize demand level (0-100 → 0.5-1.5 multiplier)
        float demandFactor = Mathf.Clamp(demandLevel / 100f + 1f, 0.5f, 1.5f);

        return baseRate * demandFactor * demandMultiplier;
    }

    // NEW: Check if placement is allowed based on current demand
    private bool CanPlaceBasedOnDemand(Zone.ZoneType zoneType)
    {
        if (demandManager == null || !respectDemandLimits)
            return true;

        return demandManager.CanZoneTypeGrow(zoneType);
    }

    // NEW: Get current demand level for logging
    private float GetDemandLevel(Zone.ZoneType zoneType)
    {
        if (demandManager == null)
            return 0f;

        return demandManager.GetDemandLevel(zoneType);
    }

    private List<Vector2> FindSuitableGrowthLocations(Zone.ZoneType zoneType)
    {
        List<Vector2> suitableLocations = new List<Vector2>();

        // Search the entire grid for suitable locations
        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                Vector2 position = new Vector2(x, y);

                if (IsSuitableForGrowth(position, zoneType))
                {
                    suitableLocations.Add(position);
                }
            }
        }

        // Prioritize locations adjacent to existing zones if preference is enabled
        if (preferAdjacentToExisting)
        {
            suitableLocations.Sort((a, b) =>
            {
                int adjacentA = CountAdjacentZones(a, zoneType);
                int adjacentB = CountAdjacentZones(b, zoneType);
                return adjacentB.CompareTo(adjacentA); // Sort by descending adjacent count
            });
        }

        return suitableLocations;
    }

    private bool IsSuitableForGrowth(Vector2 position, Zone.ZoneType zoneType)
    {
        // Check if the position is within grid bounds
        if (position.x < 0 || position.x >= gridManager.width ||
            position.y < 0 || position.y >= gridManager.height)
            return false;

        // Check if position is already occupied by grass (buildable)
        Cell cell = gridManager.GetCell((int)position.x, (int)position.y);
        if (cell == null || cell.zoneType != Zone.ZoneType.Grass)
            return false;

        // Check terrain constraints (reuse existing method from GridManager)
        if (!gridManager.terrainManager.CanPlaceBuildingInTerrain(position, 1))
            return false;

        // Check overcrowding if enabled
        if (avoidOvercrowding && IsAreaOvercrowded(position, zoneType))
            return false;

        return true;
    }

    private int CountAdjacentZones(Vector2 position, Zone.ZoneType zoneType)
    {
        int count = 0;
        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        foreach (Vector2 dir in directions)
        {
            Vector2 adjacentPos = position + dir;
            if (adjacentPos.x >= 0 && adjacentPos.x < gridManager.width &&
                adjacentPos.y >= 0 && adjacentPos.y < gridManager.height)
            {
                Cell adjacentCell = gridManager.GetCell((int)adjacentPos.x, (int)adjacentPos.y);
                if (adjacentCell != null && IsRelatedZoneType(adjacentCell.zoneType, zoneType))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private bool IsRelatedZoneType(Zone.ZoneType existing, Zone.ZoneType target)
    {
        // Check if zones are of the same category (residential, commercial, industrial)
        if (IsResidentialType(existing) && IsResidentialType(target)) return true;
        if (IsCommercialType(existing) && IsCommercialType(target)) return true;
        if (IsIndustrialType(existing) && IsIndustrialType(target)) return true;

        return false;
    }

    private bool IsResidentialType(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.ResidentialLightZoning ||
               zoneType == Zone.ZoneType.ResidentialMediumZoning ||
               zoneType == Zone.ZoneType.ResidentialHeavyZoning ||
               zoneType == Zone.ZoneType.ResidentialLightBuilding ||
               zoneType == Zone.ZoneType.ResidentialMediumBuilding ||
               zoneType == Zone.ZoneType.ResidentialHeavyBuilding;
    }

    private bool IsCommercialType(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.CommercialLightZoning ||
               zoneType == Zone.ZoneType.CommercialMediumZoning ||
               zoneType == Zone.ZoneType.CommercialHeavyZoning ||
               zoneType == Zone.ZoneType.CommercialLightBuilding ||
               zoneType == Zone.ZoneType.CommercialMediumBuilding ||
               zoneType == Zone.ZoneType.CommercialHeavyBuilding;
    }

    private bool IsIndustrialType(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.IndustrialLightZoning ||
               zoneType == Zone.ZoneType.IndustrialMediumZoning ||
               zoneType == Zone.ZoneType.IndustrialHeavyZoning ||
               zoneType == Zone.ZoneType.IndustrialLightBuilding ||
               zoneType == Zone.ZoneType.IndustrialMediumBuilding ||
               zoneType == Zone.ZoneType.IndustrialHeavyBuilding;
    }

    private bool IsAreaOvercrowded(Vector2 position, Zone.ZoneType zoneType)
    {
        int zoneCount = 0;
        int totalCells = 0;

        // Check area around position
        for (int x = -searchRadius; x <= searchRadius; x++)
        {
            for (int y = -searchRadius; y <= searchRadius; y++)
            {
                Vector2 checkPos = position + new Vector2(x, y);
                if (checkPos.x >= 0 && checkPos.x < gridManager.width &&
                    checkPos.y >= 0 && checkPos.y < gridManager.height)
                {
                    Cell cell = gridManager.GetCell((int)checkPos.x, (int)checkPos.y);
                    if (cell != null)
                    {
                        totalCells++;
                        if (IsRelatedZoneType(cell.zoneType, zoneType))
                        {
                            zoneCount++;
                        }
                    }
                }
            }
        }

        float density = totalCells > 0 ? (float)zoneCount / totalCells : 0f;
        return density > maxDensityFactor;
    }

    private void PlaceAutoZone(Vector2 position, Zone.ZoneType zoneType)
    {
        // NEW: Use GridManager's existing zone placement system
        // This ensures demand checking is consistent between manual and auto placement

        // Temporarily set the UI to the auto-growth zone type
        var originalZoneType = gridManager.uiManager.GetSelectedZoneType();

        // Since we can't directly set the UI selection, we'll call the zone placement method directly
        // This is a simplified implementation - in a real scenario, you might want to implement
        // a direct placement method in GridManager that bypasses UI

        // TODO: Implement actual zone placement using GridManager methods
        // For now, this is a placeholder that would need to integrate with your existing zone placement system
    }

    // Public methods for player control
    public void SetAutoGrowthEnabled(bool enabled) => autoGrowthEnabled = enabled;
    public void SetZoningSuccessRate(float rate) => zoningSucessRate = Mathf.Clamp01(rate);
    public void SetMaxZonesPerGrowth(int max) => maxZonesPerGrowth = Mathf.Max(1, max);
    public void SetRespectDemandLimits(bool respect) => respectDemandLimits = respect;
    public void SetDemandMultiplier(float multiplier) => demandMultiplier = Mathf.Clamp(multiplier, 0.1f, 3.0f);
}
