using UnityEngine;
using Territory.Core;
using Territory.Economy;
using Territory.Zones;

namespace Territory.Simulation
{
/// <summary>
/// Shared service for urban centroid + ring classification. Owns <see cref="UrbanMetrics"/>; exposes
/// centroid, ring, street/zone params. Consumers: <c>AutoZoningManager</c>, <c>AutoRoadBuilder</c>,
/// <c>UrbanizationProposalManager</c>, <c>MiniMapController</c>. Recalc from grid each simulation tick.
/// </summary>
public class UrbanCentroidService : MonoBehaviour, IUrbanCentroidService
{
    public GridManager gridManager;

    private UrbanMetrics urbanMetrics;
    private Vector2 previousCentroid = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
    private bool hasPreviousCentroid;

    /// <summary>True → centroid moved significantly from last tick (enables densification boost in new core).</summary>
    public bool CentroidShiftedRecently { get; private set; }

    void Start()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();
    }

    /// <summary>Recalc centroid from grid. Call once per simulation tick before roads/zoning.</summary>
    public void RecalculateFromGrid()
    {
        if (gridManager == null || !gridManager.isInitialized)
            return;
        if (urbanMetrics == null)
            urbanMetrics = new UrbanMetrics(gridManager.width, gridManager.height);
        urbanMetrics.RecalculateFromGrid(gridManager);
        Vector2 newCentroid = GetCentroid();
        float radius = GetUrbanRadius();
        float shiftThreshold = radius * 0.15f;
        CentroidShiftedRecently = hasPreviousCentroid && Vector2.Distance(previousCentroid, newCentroid) > shiftThreshold;
        previousCentroid = newCentroid;
        hasPreviousCentroid = true;
    }

    /// <summary>Urban centroid (center of mass of all building cells).</summary>
    public Vector2 GetCentroid()
    {
        if (urbanMetrics == null)
            return gridManager != null ? new Vector2(gridManager.width / 2f, gridManager.height / 2f) : Vector2.zero;
        return urbanMetrics.GetCentroid();
    }

    /// <summary>Discrete pole for multipolar/connurbation experiments. Does not change ring math by itself.</summary>
    public UrbanCentroidPole GetUrbanCentroidPole(float weight = 1f)
    {
        return UrbanCentroidPole.FromContinuous(GetCentroid(), weight);
    }

    /// <summary>Effective urban radius for ring classification.</summary>
    public float GetUrbanRadius()
    {
        if (urbanMetrics == null)
            return 5f;
        return urbanMetrics.GetUrbanRadius();
    }

    /// <summary>Returns the 3 ring boundary distances for MiniMap visualization.</summary>
    public float[] GetRingBoundaryDistances()
    {
        if (urbanMetrics == null)
            return new float[0];
        return urbanMetrics.GetRingBoundaryDistances();
    }

    /// <summary>Classifies a cell position by urban ring based on distance to centroid.</summary>
    public UrbanRing GetUrbanRing(Vector2 cellPos)
    {
        if (urbanMetrics == null)
            return UrbanRing.Mid;
        return urbanMetrics.GetUrbanRing(cellPos);
    }

    /// <summary>Street extension parameters for the given ring.</summary>
    public RingStreetParams GetStreetParamsForRing(UrbanRing ring)
    {
        if (urbanMetrics == null)
            return default;
        return urbanMetrics.GetStreetParamsForRing(ring);
    }

    /// <summary>Base zone probabilities (R, C, I) for the given ring.</summary>
    public RingZoneProbabilities GetBaseZoneProbabilities(UrbanRing ring)
    {
        if (urbanMetrics == null)
            return default;
        return urbanMetrics.GetBaseZoneProbabilities(ring);
    }

    /// <summary>Light/Medium/Heavy zoning probabilities for the given ring.</summary>
    public RingZoningDensity GetZoningDensityForRing(UrbanRing ring)
    {
        if (urbanMetrics == null)
            return default;
        return urbanMetrics.GetZoningDensityForRing(ring);
    }

    /// <summary>Internal UrbanMetrics for backward compatibility (e.g. MiniMapController).</summary>
    public UrbanMetrics GetUrbanMetrics()
    {
        return urbanMetrics;
    }
}
}
