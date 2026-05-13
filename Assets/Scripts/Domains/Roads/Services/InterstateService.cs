// registry-resolve-exempt: internal factory — constructs own sub-services (InterstateConformanceService, InterstateGenService, InterstateFlowTrackerService) within Roads domain
using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Geography;
using Territory.Terrain;
using Territory.Roads;
// RoadStrokeTerrainRules (Territory.Utilities) lives in TerritoryDeveloper.Game — IsLandSlopeAllowedForRoadStroke inlined in InterstateConformanceService.

namespace Domains.Roads.Services
{
/// <summary>
/// Orchestrator: delegates to InterstateGenService, InterstateConformanceService, InterstateFlowTrackerService.
/// Facade contract (public API) unchanged — consumers resolve via IRoads / registry.
/// No MonoBehaviour, no SerializeField.
/// </summary>
public class InterstateService
{
    // ------------------------------------------------------------------
    // Static direction helpers (border index constants)
    // ------------------------------------------------------------------

    /// <summary>Border index for south edge (y == 0).</summary>
    public const int BorderSouth = 0;
    /// <summary>Border index for north edge (y == h-1).</summary>
    public const int BorderNorth = 1;
    /// <summary>Border index for west edge (x == 0).</summary>
    public const int BorderWest = 2;
    /// <summary>Border index for east edge (x == w-1).</summary>
    public const int BorderEast = 3;

    // ------------------------------------------------------------------
    // Sub-services + retained refs for TryGenerateInterstateDeterministic
    // ------------------------------------------------------------------

    private readonly InterstateGenService _gen;
    private readonly InterstateConformanceService _conformance;
    private readonly InterstateFlowTrackerService _flowTracker;
    private readonly IGridManager _grid;
    private readonly IRoadManager _roads;

    // ------------------------------------------------------------------
    // Mutable algorithm state
    // ------------------------------------------------------------------

    private List<Vector2Int> _interstatePositions = new List<Vector2Int>();
    private bool _isConnectedToInterstate;

    public Vector2Int? EntryPoint { get; private set; }
    public Vector2Int? ExitPoint { get; private set; }
    public int EntryBorder { get; private set; } = -1;
    public int ExitBorder { get; private set; } = -1;
    public bool IsConnectedToInterstate => _isConnectedToInterstate;
    public IReadOnlyList<Vector2Int> InterstatePositions => _interstatePositions;

    // ------------------------------------------------------------------
    // Constructors
    // ------------------------------------------------------------------

    /// <summary>Full dep injection — use in production (Manager Awake).</summary>
    public InterstateService(IGridManager grid, ITerrainManager terrain, IRoadManager roads)
    {
        _grid = grid;
        _roads = roads;
        _conformance = new InterstateConformanceService(terrain);
        _gen = new InterstateGenService(grid, terrain, roads, _conformance);
        _flowTracker = new InterstateFlowTrackerService(grid);
    }

    /// <summary>No-dep ctor — valid for unit tests exercising pure static helpers.</summary>
    public InterstateService() { }

    // ------------------------------------------------------------------
    // Public API — matches every public method on InterstateManager
    // ------------------------------------------------------------------

    /// <summary>Generate interstate route, place tiles via centralized terraform + resolve pipeline.</summary>
    public bool GenerateAndPlaceInterstate(int attemptOffset = 0)
    {
        GenerateInterstateRoute(attemptOffset);
        if (_interstatePositions.Count == 0 || _gen == null)
            return false;
        bool placed = _gen.PlaceCurrentPath(_interstatePositions);
        if (!placed)
            _interstatePositions.Clear();
        return placed;
    }

    /// <summary>Try all border cells deterministically. Pick best valid path by cost.</summary>
    public bool TryGenerateInterstateDeterministic()
    {
        if (_gen == null || _roads == null || _grid == null || _conformance == null) return false;
        var heightMap = _gen.GetHeightMapFromTerrain();
        if (heightMap == null) return false;

        int w = _grid.width;
        int h = _grid.height;
        List<int> bordersWithLand = _conformance.GetBordersWithLand(w, h, heightMap);
        if (bordersWithLand.Count < 2) return false;

        int runSeed = System.Environment.TickCount ^ (int)(Time.realtimeSinceStartup * 1000);
        Random.InitState(runSeed);
        int randomEntryBorder = bordersWithLand[Random.Range(0, bordersWithLand.Count)];
        int exitBorderVal = TerritoryData.OppositeBorder(randomEntryBorder);
        if (!bordersWithLand.Contains(exitBorderVal))
        {
            foreach (int b in bordersWithLand)
            {
                if (b != randomEntryBorder) { exitBorderVal = b; break; }
            }
        }

        var entries = _conformance.GetValidBorderCellsWithPreference(randomEntryBorder, w, h, heightMap);
        if (entries.Count == 0) return false;
        var exits = _conformance.GetValidBorderCellsWithPreference(exitBorderVal, w, h, heightMap);
        if (exits.Count == 0) return false;

        var candidates = new List<(int borderA, int borderB, Vector2Int entry, Vector2Int exit)>();
        for (int ei = 0; ei < entries.Count; ei++)
            for (int xi = 0; xi < exits.Count; xi++)
            {
                if (entries[ei] != exits[xi])
                    candidates.Add((randomEntryBorder, exitBorderVal, entries[ei], exits[xi]));
            }

        candidates.Sort((a, b) =>
        {
            int mA = Mathf.Abs(a.entry.x - a.exit.x) + Mathf.Abs(a.entry.y - a.exit.y);
            int mB = Mathf.Abs(b.entry.x - b.exit.x) + Mathf.Abs(b.entry.y - b.exit.y);
            int cmp = mA.CompareTo(mB);
            if (cmp != 0) return cmp;
            int sA = _conformance.ComputeInterstateBorderEndpointScore(a.entry, w, h, heightMap)
                   + _conformance.ComputeInterstateBorderEndpointScore(a.exit, w, h, heightMap);
            int sB = _conformance.ComputeInterstateBorderEndpointScore(b.entry, w, h, heightMap)
                   + _conformance.ComputeInterstateBorderEndpointScore(b.exit, w, h, heightMap);
            cmp = sB.CompareTo(sA);
            if (cmp != 0) return cmp;
            if (a.entry.x != b.entry.x) return a.entry.x.CompareTo(b.entry.x);
            if (a.entry.y != b.entry.y) return a.entry.y.CompareTo(b.entry.y);
            if (a.exit.x != b.exit.x) return a.exit.x.CompareTo(b.exit.x);
            return a.exit.y.CompareTo(b.exit.y);
        });

        List<Vector2Int> bestPath = null;
        int bestCost = int.MaxValue;
        int bestBorderA = -1, bestBorderB = -1;
        const int maxDeterministicTries = 800;
        for (int i = 0; i < Mathf.Min(candidates.Count, maxDeterministicTries); i++)
        {
            var (borderA, borderB, entry, exit) = candidates[i];
            Random.InitState(InterstateGenService.InterstateGenSeed + 9999 + entry.x + entry.y * w + exit.x + exit.y * w);
            List<Vector2Int> path = _gen.FindInterstatePathAStar(entry, exit, w, h, heightMap);
            if (path == null || path.Count < 2 || path[path.Count - 1] != exit) continue;
            if (!_roads.ValidateBridgePath(path, heightMap)) continue;
            if (!_roads.ValidateInterstatePathForPlacement(path)) continue;
            int cost = _gen.ComputePathCost(path, heightMap);
            if (cost < bestCost) { bestCost = cost; bestPath = path; bestBorderA = borderA; bestBorderB = borderB; }
        }

        if (bestPath == null) return false;
        _interstatePositions = bestPath;
        EntryPoint = _interstatePositions[0];
        ExitPoint = _interstatePositions[_interstatePositions.Count - 1];
        EntryBorder = bestBorderA;
        ExitBorder = bestBorderB;
        bool placed = _roads.PlaceInterstateFromPath(_interstatePositions);
        if (!placed) _interstatePositions.Clear();
        return placed;
    }

    /// <summary>Generate interstate route + store positions.</summary>
    public List<Vector2Int> GenerateInterstateRoute(int attemptOffset = 0)
    {
        if (_gen == null)
        {
            _interstatePositions.Clear();
            EntryPoint = null;
            ExitPoint = null;
            EntryBorder = -1;
            ExitBorder = -1;
            return _interstatePositions;
        }
        Vector2Int? ep = null;
        Vector2Int? xp = null;
        int eb = -1;
        int xb = -1;
        _interstatePositions = _gen.GenerateInterstateRoute(attemptOffset, ref ep, ref xp, ref eb, ref xb);
        EntryPoint = ep;
        ExitPoint = xp;
        EntryBorder = eb;
        ExitBorder = xb;
        return _interstatePositions;
    }

    /// <summary>Rebuild interstate positions list from grid (e.g. after load).</summary>
    public void RebuildFromGrid()
    {
        if (_flowTracker == null) return;
        Vector2Int? ep = EntryPoint;
        Vector2Int? xp = ExitPoint;
        int eb = EntryBorder;
        int xb = ExitBorder;
        _flowTracker.RebuildFromGrid(_interstatePositions, ref ep, ref xp, ref eb, ref xb);
        EntryPoint = ep;
        ExitPoint = xp;
        EntryBorder = eb;
        ExitBorder = xb;
    }

    /// <summary>Check if given grid position is interstate cell.</summary>
    public bool IsInterstateAt(int x, int y)
        => _flowTracker != null && _flowTracker.IsInterstateAt(x, y);

    /// <summary>Check if given grid position is interstate cell (Vector2 overload).</summary>
    public bool IsInterstateAt(Vector2 gridPos)
        => _flowTracker != null && _flowTracker.IsInterstateAt(gridPos);

    /// <summary>BFS from all interstate cells through road cells. Set IsConnectedToInterstate if any player road reached.</summary>
    public void CheckInterstateConnectivity()
    {
        _isConnectedToInterstate = _flowTracker != null && _flowTracker.CheckInterstateConnectivity(_interstatePositions);
    }

    /// <summary>Whether player can start placing street from this position.</summary>
    public bool CanPlaceStreetFrom(Vector2 gridPosition)
        => _flowTracker != null && _flowTracker.CanPlaceStreetFrom(gridPosition);

    /// <summary>Whether player can start placing street from (x,y).</summary>
    public bool CanPlaceStreetFrom(int x, int y)
        => _flowTracker != null && _flowTracker.CanPlaceStreetFrom(x, y);

    /// <summary>Force-set connectivity flag (e.g. on load from save).</summary>
    public void SetConnectedToInterstate(bool connected)
    {
        _isConnectedToInterstate = connected;
    }

    // ------------------------------------------------------------------
    // Static direction helpers (Stage 16 stub preserved + extended)
    // ------------------------------------------------------------------

    /// <summary>Return border index for a cell on the map edge. -1 if not on border.</summary>
    public int GetBorderIndex(Vector2Int pos, int w, int h)
    {
        if (pos.y == 0) return BorderSouth;
        if (pos.y == h - 1) return BorderNorth;
        if (pos.x == 0) return BorderWest;
        if (pos.x == w - 1) return BorderEast;
        return -1;
    }

    /// <summary>True if cell sits on any map border.</summary>
    public bool IsOnMapBorder(Vector2Int pos, int w, int h)
    {
        return pos.x == 0 || pos.x == w - 1 || pos.y == 0 || pos.y == h - 1;
    }

    /// <summary>True if step from a to b is strictly cardinal.</summary>
    public bool IsCardinalStep(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(b.x - a.x);
        int dy = Mathf.Abs(b.y - a.y);
        return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
    }

    /// <summary>True if every consecutive step in path is strictly cardinal.</summary>
    public bool IsCardinalPath(List<Vector2Int> path)
    {
        if (path == null || path.Count < 2) return false;
        for (int i = 1; i < path.Count; i++)
        {
            if (!IsCardinalStep(path[i - 1], path[i]))
                return false;
        }
        return true;
    }

    /// <summary>Compute Manhattan distance between two grid cells.</summary>
    public int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    /// <summary>Return the first mandatory step direction when entering map from a border cell.</summary>
    public Vector2Int? GetFirstStepDirectionFromBorder(Vector2Int borderCell, int w, int h)
    {
        if (borderCell.y == 0) return new Vector2Int(0, 1);
        if (borderCell.y == h - 1) return new Vector2Int(0, -1);
        if (borderCell.x == 0) return new Vector2Int(1, 0);
        if (borderCell.x == w - 1) return new Vector2Int(-1, 0);
        return null;
    }
}
}
