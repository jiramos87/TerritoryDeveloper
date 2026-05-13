// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Territory.Core;
using Territory.Terrain;
using Territory.UI;
using Territory.Zones;
using Territory.Buildings;

namespace Territory.Utilities
{
/// <summary>
/// Centralized builder for in-game debug/analysis text in UI (coordinates, cell under cursor, building placement, fail reason).
/// Keeps debug strings consistent → improves LLM/dev analysis from screenshots.
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
    /// Resolve manager refs from scene if not assigned.
    /// </summary>
    private void ResolveRefsIfNeeded()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
        if (waterManager == null) waterManager = FindObjectOfType<WaterManager>();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
    }

    /// <summary>
    /// Single line: grid coords, chunk id, logical water surface height, water body classification + id when in bounds. E.g.
    /// <c>"x: 12, y: 9, chunk: (0,0), S: 2, body: Lake id=3"</c> or <c>"..., S: n/a, body: n/a"</c> when cell dry.
    /// </summary>
    public string GetCoordinatesLine(Vector2 gridPosition)
    {
        ResolveRefsIfNeeded();
        string coords = $"x: {gridPosition.x}, y: {gridPosition.y}";
        if (gridManager != null && gridManager.chunkSize > 0)
        {
            int cx = (int)gridPosition.x / gridManager.chunkSize;
            int cy = (int)gridPosition.y / gridManager.chunkSize;
            coords += $", chunk: ({cx},{cy})";
        }

        int ix = (int)gridPosition.x;
        int iy = (int)gridPosition.y;
        if (gridManager != null && ix >= 0 && ix < gridManager.width && iy >= 0 && iy < gridManager.height && waterManager != null)
        {
            WaterManager.CellWaterContext ctx = waterManager.GetCellWaterContext(ix, iy);
            coords += ctx.SurfaceHeight >= 0 ? $", S: {ctx.SurfaceHeight}" : ", S: n/a";
            coords += ctx.WaterBodyId != 0
                ? $", body: {ctx.Classification} id={ctx.WaterBodyId}"
                : ", body: n/a";
        }

        return coords;
    }

    /// <summary>
    /// Debug info for cell under cursor: height, zoneType, isWater, hasForest, adjacentToWater.
    /// Uses <see cref="TerrainManager"/> HeightMap, <see cref="CityCell"/>, <see cref="WaterManager"/> when available.
    /// </summary>
    public string GetCellUnderCursorInfo(Vector2 gridPosition)
    {
        ResolveRefsIfNeeded();
        if (gridManager == null) return "";

        int x = (int)gridPosition.x;
        int y = (int)gridPosition.y;
        if (x < 0 || x >= gridManager.width || y < 0 || y >= gridManager.height)
            return "CityCell: out of bounds";

        CityCell cell = gridManager.GetCell(x, y);
        if (cell == null) return "CityCell: null";

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
        return "CityCell: " + string.Join(SectionSeparator, parts).Replace(SectionSeparator + SectionSeparator, SectionSeparator).Trim();
    }

    /// <summary>
    /// Prefab instance names for cell: stored terrain/shore/forest labels, then live GameObject names
    /// for terrain prefab, forest, occupied building (dedup, insertion order).
    /// </summary>
    private static string FormatCellInstantiatedPrefabNames(CityCell cell)
    {
        if (cell == null) return "";

        var names = new List<string>();
        void AddUnique(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            if (!names.Contains(s))
                names.Add(s);
        }

        AddUnique(cell.prefabName);
        AddUnique(cell.secondaryPrefabName);
        AddUnique(cell.forestPrefabName);
        if (cell.prefab != null)
            AddUnique(cell.prefab.name);
        if (cell.forestObject != null)
            AddUnique(cell.forestObject.name);
        if (cell.occupiedBuilding != null)
            AddUnique(cell.occupiedBuilding.name);

        return names.Count == 0 ? "" : string.Join(", ", names);
    }

    /// <summary>
    /// Debug line for last clicked cell: coords (chunk, water S/body when in bounds), terrain height h,
    /// instantiated prefab name(s) for cell.
    /// </summary>
    public string GetSelectedPointDebugLine(Vector2 selectedPoint)
    {
        ResolveRefsIfNeeded();
        int sx = (int)selectedPoint.x;
        int sy = (int)selectedPoint.y;
        if (sx < 0 || sy < 0)
            return "";

        var sb = new StringBuilder("Last click: ");
        sb.Append(GetCoordinatesLine(selectedPoint));

        if (gridManager == null)
            return sb.ToString();

        CityCell cell = gridManager.GetCell(sx, sy);
        if (cell == null)
            return sb.ToString();

        int h = cell.GetCellInstanceHeight();
        if (terrainManager != null && terrainManager.GetHeightMap() != null)
            h = terrainManager.GetHeightMap().GetHeight(sx, sy);
        sb.Append(SectionSeparator).Append("h:").Append(h);

        string prefabNames = FormatCellInstantiatedPrefabNames(cell);
        if (!string.IsNullOrEmpty(prefabNames))
            sb.Append(SectionSeparator).Append("prefabs: ").Append(prefabNames);

        return sb.ToString();
    }

    /// <summary>
    /// Short line listing footprint tile coords. E.g. <c>"Footprint: (12,9)(13,9)(12,10)(13,10)"</c>.
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
    /// Multi-line summary of footprint cells: coords, height, zoneType, isWater, adjacentToWater per tile.
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
                    CityCell c = gridManager.GetCell(gx, gy);
                    if (c != null) zone = c.zoneType.ToString();
                }
                lines.Add($"  ({gx},{gy}) h:{h} {zone}" + (water ? " water" : "") + (adjWater ? " adjWater" : ""));
            }
        }
        return string.Join(LineBreak, lines);
    }

    /// <summary>
    /// Building placement debug block: name, footprint size, footprint summary, fail reason if any.
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
    /// Full debug text for cursor position: coords, cell under cursor, last-click grid cell (coords, h, prefab names),
    /// and (if building selected) building placement info. Main debug panel.
    /// </summary>
    /// <param name="gridPosition">Current cursor grid position.</param>
    /// <param name="selectedPoint">Last clicked grid position (left or right). (-1,-1) → none.</param>
    public string GetFullDebugText(Vector2 gridPosition, Vector2 selectedPoint)
    {
        ResolveRefsIfNeeded();

        var parts = new List<string>
        {
            "Cursor: " + GetCoordinatesLine(gridPosition),
            GetCellUnderCursorInfo(gridPosition)
        };

        int sx = (int)selectedPoint.x;
        int sy = (int)selectedPoint.y;
        if (sx >= 0 && sy >= 0)
            parts.Add(GetSelectedPointDebugLine(selectedPoint));

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
    /// Short one-line debug string (coords + cell summary). Use when UI space limited.
    /// </summary>
    public string GetShortDebugLine(Vector2 gridPosition)
    {
        return GetCoordinatesLine(gridPosition) + SectionSeparator + GetCellUnderCursorInfo(gridPosition);
    }

    /// <summary>
    /// BUG-60 — CellDataPanel two-section debug text. Hover + Last-clicked stacked,
    /// each renders 5 data points: grid coords, height, surface level, prefab, owner/city id.
    /// Last-clicked section omitted when <paramref name="lastClicked"/> negative.
    /// </summary>
    /// <param name="hover">Current cursor grid position.</param>
    /// <param name="lastClicked">Last clicked grid position (-1,-1 → none).</param>
    public string GetCellDataPanelText(Vector2 hover, Vector2 lastClicked)
    {
        ResolveRefsIfNeeded();
        var sb = new StringBuilder();
        sb.Append("[Hover]\n");
        sb.Append(FormatCellDataPanelSection(hover));
        int lx = (int)lastClicked.x;
        int ly = (int)lastClicked.y;
        if (lx >= 0 && ly >= 0)
        {
            sb.Append("\n\n[Last-clicked]\n");
            sb.Append(FormatCellDataPanelSection(lastClicked));
        }
        return sb.ToString();
    }

    private string FormatCellDataPanelSection(Vector2 gridPosition)
    {
        int x = (int)gridPosition.x;
        int y = (int)gridPosition.y;

        string coordsLine = $"Grid: ({x}, {y})";
        if (gridManager != null && gridManager.chunkSize > 0)
        {
            int cx = x / gridManager.chunkSize;
            int cy = y / gridManager.chunkSize;
            coordsLine += $"  chunk: ({cx},{cy})";
        }

        string heightLine = "Height: n/a";
        string surfaceLine = "Surface: n/a";
        string prefabLine = "Prefab: -";
        string ownerLine = "Owner: n/a";

        if (gridManager != null && x >= 0 && x < gridManager.width && y >= 0 && y < gridManager.height)
        {
            int h = 0;
            if (terrainManager != null && terrainManager.GetHeightMap() != null)
                h = terrainManager.GetHeightMap().GetHeight(x, y);
            heightLine = $"Height: {h}";

            if (waterManager != null)
            {
                WaterManager.CellWaterContext ctx = waterManager.GetCellWaterContext(x, y);
                if (ctx.SurfaceHeight >= 0)
                    surfaceLine = ctx.WaterBodyId != 0
                        ? $"Surface: {ctx.SurfaceHeight} ({ctx.Classification} id={ctx.WaterBodyId})"
                        : $"Surface: {ctx.SurfaceHeight}";
                else
                    surfaceLine = $"Surface: {h}";
            }

            CityCell cell = gridManager.GetCell(x, y);
            if (cell != null)
            {
                string prefabs = FormatCellInstantiatedPrefabNames(cell);
                prefabLine = string.IsNullOrEmpty(prefabs)
                    ? $"Prefab: empty ({cell.zoneType})"
                    : $"Prefab: {prefabs}";
            }
        }
        else
        {
            heightLine = "Height: oob";
            surfaceLine = "Surface: oob";
            prefabLine = "Prefab: oob";
        }

        return string.Join(LineBreak, new[] { coordsLine, heightLine, surfaceLine, prefabLine, ownerLine });
    }
}
}
