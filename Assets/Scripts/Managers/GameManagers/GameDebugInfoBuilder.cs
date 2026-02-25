using UnityEngine;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Centralized builder for in-game debug / analysis text shown in the UI (e.g. coordinates,
/// cell under cursor, building placement info, fail reason). Use this to keep debug strings
/// consistent and to improve LLM/developer analysis from screenshots.
/// </summary>
public class GameDebugInfoBuilder : MonoBehaviour
{
    [Header("Optional references (resolved automatically if not set)")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TerrainManager terrainManager;
    [SerializeField] private WaterManager waterManager;
    [SerializeField] private UIManager uiManager;

    private const string SectionSeparator = " | ";
    private const string LineBreak = "\n";

    void Awake()
    {
        ResolveRefsIfNeeded();
    }

    /// <summary>
    /// Resolve manager references from the scene if not assigned.
    /// </summary>
    private void ResolveRefsIfNeeded()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
        if (waterManager == null) waterManager = FindObjectOfType<WaterManager>();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
    }

    /// <summary>
    /// Returns a single line with grid coordinates, e.g. "x: 12, y: 9".
    /// </summary>
    public string GetCoordinatesLine(Vector2 gridPosition)
    {
        return $"x: {gridPosition.x}, y: {gridPosition.y}";
    }

    /// <summary>
    /// Returns debug info for the cell under the cursor: height, zoneType, isWater, hasForest, adjacentToWater.
    /// Uses TerrainManager.HeightMap, Cell, and WaterManager when available.
    /// </summary>
    public string GetCellUnderCursorInfo(Vector2 gridPosition)
    {
        ResolveRefsIfNeeded();
        if (gridManager == null) return "";

        int x = (int)gridPosition.x;
        int y = (int)gridPosition.y;
        if (x < 0 || x >= gridManager.width || y < 0 || y >= gridManager.height)
            return "Cell: out of bounds";

        Cell cell = gridManager.GetCell(x, y);
        if (cell == null) return "Cell: null";

        int height = cell.GetCellInstanceHeight();
        if (terrainManager != null && terrainManager.GetHeightMap() != null)
            height = terrainManager.GetHeightMap().GetHeight(x, y);

        bool isWater = waterManager != null && waterManager.IsWaterAt(x, y);
        bool hasForest = cell.HasForest();
        bool adjacentToWater = waterManager != null && waterManager.IsAdjacentToWater(x, y);

        var parts = new List<string>
        {
            $"h:{height}",
            cell.zoneType.ToString(),
            isWater ? "water" : "",
            hasForest ? "forest" : "",
            adjacentToWater ? "adjWater" : ""
        };
        return "Cell: " + string.Join(SectionSeparator, parts).Replace(SectionSeparator + SectionSeparator, SectionSeparator).Trim();
    }

    /// <summary>
    /// Returns a short line listing footprint tile coordinates, e.g. "Footprint: (12,9)(13,9)(12,10)(13,10)".
    /// </summary>
    public string GetFootprintTilesLine(Vector2 gridPosition, int buildingSize)
    {
        if (gridManager == null) return "";
        gridManager.GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);
        var sb = new StringBuilder("Footprint: ");
        for (int dx = 0; dx < buildingSize; dx++)
        {
            for (int dy = 0; dy < buildingSize; dy++)
            {
                int gx = (int)gridPosition.x + dx - offsetX;
                int gy = (int)gridPosition.y + dy - offsetY;
                sb.Append($"({gx},{gy})");
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Returns a multi-line summary of footprint cells: coords, height, zoneType, isWater, adjacentToWater per tile.
    /// </summary>
    private string GetFootprintSummary(Vector2 gridPosition, int buildingSize)
    {
        ResolveRefsIfNeeded();
        if (gridManager == null || buildingSize <= 0) return "";

        gridManager.GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);
        var lines = new List<string>();

        for (int dx = 0; dx < buildingSize; dx++)
        {
            for (int dy = 0; dy < buildingSize; dy++)
            {
                int gx = (int)gridPosition.x + dx - offsetX;
                int gy = (int)gridPosition.y + dy - offsetY;
                int h = terrainManager != null && terrainManager.GetHeightMap() != null
                    ? terrainManager.GetHeightMap().GetHeight(gx, gy)
                    : 0;
                bool water = waterManager != null && waterManager.IsWaterAt(gx, gy);
                bool adjWater = waterManager != null && waterManager.IsAdjacentToWater(gx, gy);
                string zone = "?";
                if (gx >= 0 && gx < gridManager.width && gy >= 0 && gy < gridManager.height)
                {
                    Cell c = gridManager.GetCell(gx, gy);
                    if (c != null) zone = c.zoneType.ToString();
                }
                lines.Add($"  ({gx},{gy}) h:{h} {zone}" + (water ? " water" : "") + (adjWater ? " adjWater" : ""));
            }
        }
        return string.Join(LineBreak, lines);
    }

    /// <summary>
    /// Returns building placement debug block: building name, footprint size, footprint summary, and fail reason if any.
    /// </summary>
    public string GetBuildingPlacementInfo(Vector2 gridPosition, int buildingSize, string buildingName, bool isWaterPlant)
    {
        ResolveRefsIfNeeded();
        if (gridManager == null) return "";

        var lines = new List<string>
        {
            "Building: " + (string.IsNullOrEmpty(buildingName) ? "?" : buildingName),
            "Size: " + buildingSize + "x" + buildingSize,
            GetFootprintTilesLine(gridPosition, buildingSize)
        };

        string failReason = gridManager.GetBuildingPlacementFailReason(gridPosition, buildingSize, isWaterPlant);
        if (!string.IsNullOrEmpty(failReason))
            lines.Add("Fail: " + failReason);
        else
            lines.Add("Placement: OK");

        lines.Add(GetFootprintSummary(gridPosition, buildingSize));
        return string.Join(LineBreak, lines);
    }

    /// <summary>
    /// Returns the full debug text for the current cursor position: coordinates, cell under cursor,
    /// selectedPoint (last clicked grid cell), and (if a building is selected) building placement info.
    /// Use this for the main debug panel.
    /// </summary>
    /// <param name="gridPosition">Current cursor grid position.</param>
    /// <param name="selectedPoint">Last clicked grid position (left or right). Use (-1,-1) for none.</param>
    public string GetFullDebugText(Vector2 gridPosition, Vector2 selectedPoint)
    {
        ResolveRefsIfNeeded();

        var parts = new List<string>
        {
            GetCoordinatesLine(gridPosition),
            GetCellUnderCursorInfo(gridPosition)
        };

        int sx = (int)selectedPoint.x;
        int sy = (int)selectedPoint.y;
        if (sx >= 0 && sy >= 0)
            parts.Add("selectedPoint: " + GetCoordinatesLine(selectedPoint));

        if (uiManager != null && uiManager.GetSelectedBuilding() != null)
        {
            IBuilding b = uiManager.GetSelectedBuilding();
            string name = b.Prefab != null ? b.Prefab.name : "?";
            bool isWaterPlant = b is WaterPlant;
            parts.Add(GetBuildingPlacementInfo(gridPosition, b.BuildingSize, name, isWaterPlant));
        }

        return string.Join(LineBreak, parts);
    }

    /// <summary>
    /// Short one-line debug string (coordinates + cell summary). Use when UI space is limited.
    /// </summary>
    public string GetShortDebugLine(Vector2 gridPosition)
    {
        return GetCoordinatesLine(gridPosition) + SectionSeparator + GetCellUnderCursorInfo(gridPosition);
    }
}
