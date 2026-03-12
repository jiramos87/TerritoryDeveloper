using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;

namespace Territory.Simulation
{
/// <summary>
/// Urban ring classification for density gradient and sector-based zoning.
/// Inner = urban center (dense, no industrial), Mid = residential, Outer = transition to rural, Rural = sparse.
/// </summary>
public enum UrbanRing
{
    Inner,
    Mid,
    Outer,
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
    private const float MIN_URBAN_RADIUS = 20f;
    private const float RADIUS_SCALE = 1.8f;

    private const float INNER_BOUNDARY = 0.70f;
    private const float MID_BOUNDARY = 1.00f;
    private const float OUTER_BOUNDARY = 1.80f;

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
        zoneProbabilitiesByRing = new RingZoneProbabilities[4];
        zoneProbabilitiesByRing[(int)UrbanRing.Inner] = new RingZoneProbabilities { residential = 0.15f, commercial = 0.85f, industrial = 0f };
        zoneProbabilitiesByRing[(int)UrbanRing.Mid] = new RingZoneProbabilities { residential = 0.70f, commercial = 0.15f, industrial = 0.10f };
        zoneProbabilitiesByRing[(int)UrbanRing.Outer] = new RingZoneProbabilities { residential = 0.20f, commercial = 0.05f, industrial = 0.75f };
        zoneProbabilitiesByRing[(int)UrbanRing.Rural] = new RingZoneProbabilities { residential = 0.55f, commercial = 0.35f, industrial = 0f };
    }

    private void InitializeStreetParams()
    {
        streetParamsByRing = new RingStreetParams[4];
        streetParamsByRing[(int)UrbanRing.Inner] = new RingStreetParams { minLength = 2, maxLength = 8, parallelSpacing = 1, parallelSpacingMin = 1, parallelSpacingMax = 1 };
        streetParamsByRing[(int)UrbanRing.Mid] = new RingStreetParams { minLength = 4, maxLength = 12, parallelSpacing = 3, parallelSpacingMin = 3, parallelSpacingMax = 3 };
        streetParamsByRing[(int)UrbanRing.Outer] = new RingStreetParams { minLength = 7, maxLength = 25, parallelSpacing = 6, parallelSpacingMin = 5, parallelSpacingMax = 6 };
        streetParamsByRing[(int)UrbanRing.Rural] = new RingStreetParams { minLength = 10, maxLength = 35, parallelSpacing = 6, parallelSpacingMin = 5, parallelSpacingMax = 6 };
    }

    private void InitializeZoningDensity()
    {
        densityByRing = new RingZoningDensity[4];
        densityByRing[(int)UrbanRing.Inner] = new RingZoningDensity { lightProb = 0.25f, mediumProb = 0.50f, heavyProb = 0.25f };
        densityByRing[(int)UrbanRing.Mid] = new RingZoningDensity { lightProb = 0.70f, mediumProb = 0.25f, heavyProb = 0.05f };
        densityByRing[(int)UrbanRing.Outer] = new RingZoningDensity { lightProb = 0.90f, mediumProb = 0.08f, heavyProb = 0.02f };
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
        float r = RADIUS_SCALE * Mathf.Sqrt(urbanCellCount / Mathf.PI);
        return Mathf.Max(MIN_URBAN_RADIUS, r);
    }

    /// <summary>Returns the 3 ring boundary distances (at 70%, 100%, 180% of radius) for visualization.</summary>
    public float[] GetRingBoundaryDistances()
    {
        float r = GetUrbanRadius();
        return new[] { r * INNER_BOUNDARY, r * MID_BOUNDARY, r * OUTER_BOUNDARY };
    }

    /// <summary>Classifies a cell position by urban ring based on distance to centroid.</summary>
    public UrbanRing GetUrbanRing(Vector2 cellPos)
    {
        Vector2 centroid = GetCentroid();
        float dist = Vector2.Distance(cellPos, centroid);
        float radius = GetUrbanRadius();

        if (dist <= radius * INNER_BOUNDARY) return UrbanRing.Inner;
        if (dist <= radius * MID_BOUNDARY) return UrbanRing.Mid;
        if (dist <= radius * OUTER_BOUNDARY) return UrbanRing.Outer;
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

    /// <summary>Deterministic density by ring for segment-based zoning: Inner→Heavy, Mid→Medium, Outer/Rural→Light.</summary>
    public static Zone.ZoneType ApplyDensityByRing(Zone.ZoneType lightType, UrbanRing ring)
    {
        switch (ring)
        {
            case UrbanRing.Inner:
                return LightToHeavy(lightType);
            case UrbanRing.Mid:
                return LightToMedium(lightType);
            case UrbanRing.Outer:
            case UrbanRing.Rural:
            default:
                return lightType;
        }
    }

    private static Zone.ZoneType LightToMedium(Zone.ZoneType lightType)
    {
        switch (lightType)
        {
            case Zone.ZoneType.ResidentialLightZoning: return Zone.ZoneType.ResidentialMediumZoning;
            case Zone.ZoneType.CommercialLightZoning: return Zone.ZoneType.CommercialMediumZoning;
            case Zone.ZoneType.IndustrialLightZoning: return Zone.ZoneType.IndustrialMediumZoning;
            default: return lightType;
        }
    }

    private static Zone.ZoneType LightToHeavy(Zone.ZoneType lightType)
    {
        switch (lightType)
        {
            case Zone.ZoneType.ResidentialLightZoning: return Zone.ZoneType.ResidentialHeavyZoning;
            case Zone.ZoneType.CommercialLightZoning: return Zone.ZoneType.CommercialHeavyZoning;
            case Zone.ZoneType.IndustrialLightZoning: return Zone.ZoneType.IndustrialHeavyZoning;
            default: return lightType;
        }
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
