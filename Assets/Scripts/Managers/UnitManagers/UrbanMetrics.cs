using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;

namespace Territory.Simulation
{
/// <summary>
/// Urban ring classification for density gradient and sector-based zoning.
/// Core = downtown, Inner = inner city, Mid = residential, Outer = transition, Edge = industrial margins, Rural = sparse.
/// </summary>
public enum UrbanRing
{
    Core,
    Inner,
    Mid,
    Outer,
    Edge,
    Rural
}

/// <summary>
/// Base zone type probabilities (R, C, I) per urban ring for sector-coherent auto-zoning.
/// </summary>
[System.Serializable]
public struct RingZoneProbabilities
{
    [Range(0f, 1f)] public float residential;
    [Range(0f, 1f)] public float commercial;
    [Range(0f, 1f)] public float industrial;
}

/// <summary>
/// Light/Medium/Heavy zoning probabilities per urban ring for density gradient (FEAT-29).
/// </summary>
[System.Serializable]
public struct RingZoningDensity
{
    [Range(0f, 1f)] public float lightProb;
    [Range(0f, 1f)] public float mediumProb;
    [Range(0f, 1f)] public float heavyProb;
}

/// <summary>
/// Street extension parameters per urban ring for density gradient (FEAT-29).
/// </summary>
[System.Serializable]
public struct RingStreetParams
{
    public int minLength;
    public int maxLength;
    public int branchIntervalMin;
    public int branchIntervalMax;
    public float branchChanceAtEnd;
    public int parallelSpacing;
    public int parallelSpacingMin;
    public int parallelSpacingMax;
}

/// <summary>
/// Tracks urban centroid and radius, classifies cells by urban ring.
/// Shared by AutoZoningManager and AutoRoadBuilder for sector-coherent zoning and road density gradient.
/// Plain C# class (not MonoBehaviour); instantiated and wired by AutoZoningManager.
/// </summary>
public class UrbanMetrics
{
    private const float MIN_URBAN_RADIUS = 5f;

    private float centroidSumX;
    private float centroidSumY;
    private int urbanCellCount;
    private int gridWidth;
    private int gridHeight;

    private RingZoneProbabilities[] zoneProbabilitiesByRing;
    private RingStreetParams[] streetParamsByRing;
    private RingZoningDensity[] densityByRing;

    public UrbanMetrics(int width, int height)
    {
        gridWidth = width;
        gridHeight = height;
        InitializeZoneProbabilities();
        InitializeStreetParams();
        InitializeZoningDensity();
    }

    private void InitializeZoneProbabilities()
    {
        zoneProbabilitiesByRing = new RingZoneProbabilities[6];
        zoneProbabilitiesByRing[(int)UrbanRing.Core] = new RingZoneProbabilities { residential = 0.05f, commercial = 0.90f, industrial = 0f };
        zoneProbabilitiesByRing[(int)UrbanRing.Inner] = new RingZoneProbabilities { residential = 0.25f, commercial = 0.70f, industrial = 0f };
        zoneProbabilitiesByRing[(int)UrbanRing.Mid] = new RingZoneProbabilities { residential = 0.70f, commercial = 0.15f, industrial = 0.10f };
        zoneProbabilitiesByRing[(int)UrbanRing.Outer] = new RingZoneProbabilities { residential = 0.35f, commercial = 0.05f, industrial = 0.50f };
        zoneProbabilitiesByRing[(int)UrbanRing.Edge] = new RingZoneProbabilities { residential = 0.05f, commercial = 0f, industrial = 0.90f };
        zoneProbabilitiesByRing[(int)UrbanRing.Rural] = new RingZoneProbabilities { residential = 0.55f, commercial = 0.35f, industrial = 0f };
    }

    private void InitializeStreetParams()
    {
        streetParamsByRing = new RingStreetParams[6];
        streetParamsByRing[(int)UrbanRing.Core] = new RingStreetParams { minLength = 2, maxLength = 6, branchIntervalMin = 3, branchIntervalMax = 4, branchChanceAtEnd = 0.90f, parallelSpacing = 1, parallelSpacingMin = 1, parallelSpacingMax = 1 };
        streetParamsByRing[(int)UrbanRing.Inner] = new RingStreetParams { minLength = 4, maxLength = 10, branchIntervalMin = 3, branchIntervalMax = 5, branchChanceAtEnd = 0.80f, parallelSpacing = 3, parallelSpacingMin = 3, parallelSpacingMax = 3 };
        streetParamsByRing[(int)UrbanRing.Mid] = new RingStreetParams { minLength = 6, maxLength = 15, branchIntervalMin = 5, branchIntervalMax = 8, branchChanceAtEnd = 0.50f, parallelSpacing = 4, parallelSpacingMin = 4, parallelSpacingMax = 4 };
        streetParamsByRing[(int)UrbanRing.Outer] = new RingStreetParams { minLength = 8, maxLength = 20, branchIntervalMin = 7, branchIntervalMax = 12, branchChanceAtEnd = 0.35f, parallelSpacing = 5, parallelSpacingMin = 5, parallelSpacingMax = 5 };
        streetParamsByRing[(int)UrbanRing.Edge] = new RingStreetParams { minLength = 10, maxLength = 25, branchIntervalMin = 10, branchIntervalMax = 16, branchChanceAtEnd = 0.20f, parallelSpacing = 6, parallelSpacingMin = 4, parallelSpacingMax = 6 };
        streetParamsByRing[(int)UrbanRing.Rural] = new RingStreetParams { minLength = 15, maxLength = 35, branchIntervalMin = 15, branchIntervalMax = 25, branchChanceAtEnd = 0.10f, parallelSpacing = 8, parallelSpacingMin = 5, parallelSpacingMax = 8 };
    }

    private void InitializeZoningDensity()
    {
        densityByRing = new RingZoningDensity[6];
        densityByRing[(int)UrbanRing.Core] = new RingZoningDensity { lightProb = 0.20f, mediumProb = 0.50f, heavyProb = 0.30f };
        densityByRing[(int)UrbanRing.Inner] = new RingZoningDensity { lightProb = 0.40f, mediumProb = 0.45f, heavyProb = 0.15f };
        densityByRing[(int)UrbanRing.Mid] = new RingZoningDensity { lightProb = 0.70f, mediumProb = 0.25f, heavyProb = 0.05f };
        densityByRing[(int)UrbanRing.Outer] = new RingZoningDensity { lightProb = 0.90f, mediumProb = 0.08f, heavyProb = 0.02f };
        densityByRing[(int)UrbanRing.Edge] = new RingZoningDensity { lightProb = 0.90f, mediumProb = 0.08f, heavyProb = 0.02f };
        densityByRing[(int)UrbanRing.Rural] = new RingZoningDensity { lightProb = 0.90f, mediumProb = 0.08f, heavyProb = 0.02f };
    }

    /// <summary>Urban centroid (center of mass of all urban cells).</summary>
    public Vector2 GetCentroid()
    {
        if (urbanCellCount == 0)
            return new Vector2(gridWidth / 2f, gridHeight / 2f);
        return new Vector2(centroidSumX / urbanCellCount, centroidSumY / urbanCellCount);
    }

    /// <summary>Effective urban radius for ring classification.</summary>
    public float GetUrbanRadius()
    {
        float r = Mathf.Sqrt(urbanCellCount / Mathf.PI);
        return Mathf.Max(MIN_URBAN_RADIUS, r);
    }

    /// <summary>Classifies a cell position by urban ring based on distance to centroid.</summary>
    public UrbanRing GetUrbanRing(Vector2 cellPos)
    {
        Vector2 centroid = GetCentroid();
        float dist = Vector2.Distance(cellPos, centroid);
        float radius = GetUrbanRadius();

        if (dist <= radius * 0.15f) return UrbanRing.Core;
        if (dist <= radius * 0.40f) return UrbanRing.Inner;
        if (dist <= radius * 0.70f) return UrbanRing.Mid;
        if (dist <= radius * 1.00f) return UrbanRing.Outer;
        if (dist <= radius * 1.50f) return UrbanRing.Edge;
        return UrbanRing.Rural;
    }

    /// <summary>Base zone probabilities (R, C, I) for the given ring.</summary>
    public RingZoneProbabilities GetBaseZoneProbabilities(UrbanRing ring)
    {
        return zoneProbabilitiesByRing[(int)ring];
    }

    /// <summary>Street extension parameters for the given ring.</summary>
    public RingStreetParams GetStreetParamsForRing(UrbanRing ring)
    {
        return streetParamsByRing[(int)ring];
    }

    /// <summary>Light/Medium/Heavy zoning probabilities for the given ring.</summary>
    public RingZoningDensity GetZoningDensityForRing(UrbanRing ring)
    {
        return densityByRing[(int)ring];
    }

    /// <summary>No-op: centroid is recalculated from grid each tick (buildings only).</summary>
    public void OnUrbanCellAdded(Vector2 pos)
    {
    }

    /// <summary>No-op: centroid is recalculated from grid each tick (buildings only).</summary>
    public void OnUrbanCellRemoved(Vector2 pos)
    {
    }

    /// <summary>No-op: centroid is recalculated from grid each tick (buildings only).</summary>
    public void OnUrbanCellsBulldozed(IReadOnlyList<Vector2Int> positions)
    {
    }

    /// <summary>Recalculates centroid from grid. Only counts constructed buildings (excludes roads, interstate, zoning without spawn).</summary>
    public void RecalculateFromGrid(GridManager gridManager)
    {
        if (gridManager == null) return;
        centroidSumX = 0;
        centroidSumY = 0;
        urbanCellCount = 0;
        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                Cell c = gridManager.GetCell(x, y);
                if (c == null) continue;
                if (IsBuildingZoneType(c.zoneType) && !c.isInterstate)
                {
                    centroidSumX += x;
                    centroidSumY += y;
                    urbanCellCount++;
                }
            }
        }
    }

    private static bool IsBuildingZoneType(Zone.ZoneType zt)
    {
        return zt == Zone.ZoneType.ResidentialLightBuilding || zt == Zone.ZoneType.ResidentialMediumBuilding || zt == Zone.ZoneType.ResidentialHeavyBuilding ||
               zt == Zone.ZoneType.CommercialLightBuilding || zt == Zone.ZoneType.CommercialMediumBuilding || zt == Zone.ZoneType.CommercialHeavyBuilding ||
               zt == Zone.ZoneType.IndustrialLightBuilding || zt == Zone.ZoneType.IndustrialMediumBuilding || zt == Zone.ZoneType.IndustrialHeavyBuilding ||
               zt == Zone.ZoneType.Building;
    }
}

}
