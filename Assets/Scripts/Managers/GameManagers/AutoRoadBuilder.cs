using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Roads;
using Territory.Economy;
using Territory.Terrain;
using Territory.Zones;
using Territory.Utilities;
using Domains.Roads.Services;

namespace Territory.Simulation
{
/// <summary>
/// MonoBehaviour facade: scene-wired in CityScene.unity. Preserves inspector refs + public API.
/// All internal logic delegated to <see cref="AutoBuildService"/> (POCO, no MonoBehaviour).
/// Service holds references to the facade's own lists so AutoZoningManager mutations survive.
/// Stage 10 atomization — hub-preservation Strategy γ (file stays at original path, GUID unchanged).
/// </summary>
public class AutoRoadBuilder : MonoBehaviour
{
    #region Dependencies (inspector-wired; preserved for scene serialization)
    public GridManager gridManager;
    public RoadManager roadManager;
    public GrowthBudgetManager growthBudgetManager;
    public CityStats cityStats;
    public InterstateManager interstateManager;
    public TerrainManager terrainManager;
    public TerraformingService terraformingService;
    public AutoZoningManager autoZoningManager;
    public UrbanCentroidService urbanCentroidService;
    #endregion

    #region Configuration (inspector fields; preserved for scene serialization)
    [Header("Budget per tick")]
    public int maxTilesPerTick = 10;

    [Header("Street project: segment-based growth")]
    public int minStreetLength = 4;
    /// <summary>When edges &lt; 4 (recovery), use lower min length so short streets open new fronts.</summary>
    public int minStreetLengthRecovery = 3;
    public int maxStreetLength = 20;
    public int maxActiveProjects = 5;
    /// <summary>Min distance from existing parallel road when starting new street from edge.</summary>
    public int minParallelSpacingFromEdge = 3;
    /// <summary>Min distance between two edges when starting new projects same tick.</summary>
    public int minEdgeSpacing = 2;
    /// <summary>Max water tiles in straight line for auto bridge.</summary>
    public const int MaxBridgeWaterTiles = 5;
    /// <summary>When connecting to interstate or between clusters, prefer paths this many cells away.</summary>
    public int minRoadSpacingWhenConnecting = 4;
    /// <summary>Unused; roads now prefer high-desirability dirs.</summary>
    [SerializeField] float desirabilityGrowthPenalty = 0f;

    [Header("Ring-dependent overrides (FEAT-32)")]
    [Tooltip("Extra concurrent projects when 2+ edges are in Inner.")]
    public int coreInnerExtraProjects = 2;
    [Tooltip("Reduction to minEdgeSpacing in Inner (allows more intersections).")]
    public int coreInnerMinEdgeSpacing = 1;

    /// <summary>Completed segment data published for <see cref="AutoZoningManager"/> to zone strips along.</summary>
    public struct CompletedSegment
    {
        public Vector2Int origin;
        public Vector2Int dir;
        public int length;
        public UrbanRing ring;
    }

    /// <summary>Segment with zoning progress; <see cref="AutoZoningManager"/> updates zonedUpToIndex + removes when done.</summary>
    public struct PendingZoningSegment
    {
        public CompletedSegment segment;
        public int zonedUpToIndex;
    }
    #endregion

    #region Public state (owned by facade; service holds refs — mutations from AutoZoningManager survive)
    /// <summary>Segments completed this tick; cleared at start of each <c>ProcessTick</c>.</summary>
    public List<CompletedSegment> CompletedSegmentsThisTick { get; private set; } = new List<CompletedSegment>();

    /// <summary>Segments built still needing zoning; <see cref="AutoZoningManager"/> removes when fully zoned.</summary>
    public List<PendingZoningSegment> PendingZoningSegments { get; private set; } = new List<PendingZoningSegment>();

    /// <summary>Cells expropriated this tick; must not zone until road placed.</summary>
    public HashSet<Vector2Int> ExpropriatedCellsPendingRoad { get; private set; } = new HashSet<Vector2Int>();
    #endregion

    private AutoBuildService _service;

    void Start()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (roadManager == null) roadManager = FindObjectOfType<RoadManager>();
        if (growthBudgetManager == null) growthBudgetManager = FindObjectOfType<GrowthBudgetManager>();
        if (cityStats == null) cityStats = FindObjectOfType<CityStats>();
        if (interstateManager == null) interstateManager = FindObjectOfType<InterstateManager>();
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
        if (terraformingService == null) terraformingService = FindObjectOfType<TerraformingService>();
        if (autoZoningManager == null) autoZoningManager = FindObjectOfType<AutoZoningManager>();
        if (urbanCentroidService == null) urbanCentroidService = FindObjectOfType<UrbanCentroidService>();

        // Callback design: service fires events; facade owns lists so AutoZoningManager mutations survive
        _service = new AutoBuildService(
            gridManager, roadManager, growthBudgetManager, cityStats,
            interstateManager, terrainManager, terraformingService,
            autoZoningManager, urbanCentroidService,
            ExpropriatedCellsPendingRoad);

        BindServiceConfig();

        // Wire callbacks so service events populate facade-owned lists
        _service.OnTickStart = () => CompletedSegmentsThisTick.Clear();
        _service.OnSegmentCompleted = (origin, dir, length, ring) =>
        {
            var completed = new CompletedSegment { origin = origin, dir = dir, length = length, ring = ring };
            CompletedSegmentsThisTick.Add(completed);
            PendingZoningSegments.Add(new PendingZoningSegment { segment = completed, zonedUpToIndex = -1 });
        };
    }

    /// <summary>Push facade config fields into service (called once after Start resolves deps).</summary>
    private void BindServiceConfig()
    {
        _service.maxTilesPerTick = maxTilesPerTick;
        _service.minStreetLength = minStreetLength;
        _service.minStreetLengthRecovery = minStreetLengthRecovery;
        _service.maxStreetLength = maxStreetLength;
        _service.maxActiveProjects = maxActiveProjects;
        _service.minParallelSpacingFromEdge = minParallelSpacingFromEdge;
        _service.minEdgeSpacing = minEdgeSpacing;
        _service.minRoadSpacingWhenConnecting = minRoadSpacingWhenConnecting;
        _service.coreInnerExtraProjects = coreInnerExtraProjects;
        _service.coreInnerMinEdgeSpacing = coreInnerMinEdgeSpacing;
    }

    public void ProcessTick()
    {
        if (_service == null) return;

        // Re-bind config in case inspector values changed at runtime
        BindServiceConfig();

        _service.ProcessTick();
        // No sync needed: service mutates the facade's own list instances directly
    }
}
}
