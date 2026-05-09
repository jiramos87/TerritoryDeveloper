using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Terrain;
using Territory.Zones;
using Territory.Geography;
using Territory.Utilities;

namespace Territory.Roads
{
/// <summary>
/// Interstate highway Manager — pass-through delegate over InterstateService (POCO port).
/// Class name, namespace, file path, and every [SerializeField] field preserved per Approach 1b.
/// Cutover Stage 1.0 (TECH-26630). Implements Domains.Roads.IInterstate facade.
/// </summary>
public class InterstateManager : MonoBehaviour, Domains.Roads.IInterstate
{
    #region Dependencies
    public GridManager gridManager;
    public TerrainManager terrainManager;
    public RoadManager roadManager;
    public TerraformingService terraformingService;
    #endregion

    #region State (delegated to service)
    private Domains.Roads.Services.InterstateService _interstateService;

    /// <summary>Border + position of interstate entry/exit points (set during gen or RebuildFromGrid).</summary>
    public Vector2Int? EntryPoint => _interstateService != null ? _interstateService.EntryPoint : null;
    public Vector2Int? ExitPoint => _interstateService != null ? _interstateService.ExitPoint : null;
    public int EntryBorder => _interstateService != null ? _interstateService.EntryBorder : -1;
    public int ExitBorder => _interstateService != null ? _interstateService.ExitBorder : -1;

    /// <summary>Whether player road network connected to interstate (updated monthly).</summary>
    public bool IsConnectedToInterstate => _interstateService != null && _interstateService.IsConnectedToInterstate;

    /// <summary>Read-only list of grid positions part of interstate.</summary>
    public IReadOnlyList<Vector2Int> InterstatePositions => _interstateService != null
        ? _interstateService.InterstatePositions
        : System.Array.Empty<Vector2Int>();

    void Awake()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
        if (roadManager == null) roadManager = FindObjectOfType<RoadManager>();
        if (terraformingService == null) terraformingService = FindObjectOfType<TerraformingService>();
        _interstateService = new Domains.Roads.Services.InterstateService(gridManager, terrainManager, roadManager);
    }
    #endregion

    #region Interstate Placement
    /// <summary>Generate interstate route, place tiles. Call after water map, before forest map.</summary>
    public bool GenerateAndPlaceInterstate(int attemptOffset = 0) => _interstateService.GenerateAndPlaceInterstate(attemptOffset);

    /// <summary>Try all border cells deterministically. Guarantees interstate if any valid route exists.</summary>
    public bool TryGenerateInterstateDeterministic()
    {
        return _interstateService.TryGenerateInterstateDeterministic();
    }
    #endregion

    #region Interstate Generation
    /// <summary>Generate interstate route + store positions. Does not place tiles.</summary>
    public List<Vector2Int> GenerateInterstateRoute(int attemptOffset = 0)
    {
        return _interstateService.GenerateInterstateRoute(attemptOffset);
    }
    #endregion

    #region Utility Methods
    /// <summary>Rebuild interstate positions list from grid (e.g. after load). Call after RestoreGrid.</summary>
    public void RebuildFromGrid()
    {
        _interstateService.RebuildFromGrid();
    }

    /// <summary>Check if given grid position is interstate cell.</summary>
    public bool IsInterstateAt(int x, int y)
    {
        return _interstateService.IsInterstateAt(x, y);
    }

    /// <summary>Check if given grid position is interstate cell.</summary>
    public bool IsInterstateAt(Vector2 gridPos)
    {
        return _interstateService.IsInterstateAt(gridPos);
    }

    /// <summary>BFS from all interstate cells through road cells. Set IsConnectedToInterstate if any player road reached.</summary>
    public void CheckInterstateConnectivity()
    {
        _interstateService.CheckInterstateConnectivity();
    }

    /// <summary>Whether player can start placing street from this position.</summary>
    public bool CanPlaceStreetFrom(Vector2 gridPosition)
    {
        return _interstateService.CanPlaceStreetFrom(gridPosition);
    }

    /// <summary>Whether player can start placing street from (x,y).</summary>
    public bool CanPlaceStreetFrom(int x, int y)
    {
        return _interstateService.CanPlaceStreetFrom(x, y);
    }

    /// <summary>Set connectivity flag (e.g. on load from save).</summary>
    public void SetConnectedToInterstate(bool connected)
    {
        _interstateService.SetConnectedToInterstate(connected);
    }
    #endregion
}
}
