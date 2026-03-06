using UnityEngine;
using System.Collections.Generic;
using Territory.Zones;
using Territory.Terrain;
using Territory.Buildings;
using Territory.UI;

namespace Territory.Core
{
    /// <summary>
    /// Handles building validation, placement, and footprint attribute updates for both
    /// player-initiated and programmatic (auto-resource-planner) building placement.
    /// Extracted from GridManager to reduce its responsibilities.
    /// </summary>
    public class BuildingPlacementService
    {
        private readonly GridManager grid;
        private readonly GridSortingOrderService sortingService;

        public BuildingPlacementService(GridManager grid, GridSortingOrderService sortingService)
        {
            this.grid = grid;
            this.sortingService = sortingService;
        }

        /// <summary>
        /// Returns true if a building of the given size can be placed at the grid position.
        /// Infers water plant status from the currently selected building.
        /// </summary>
        public bool CanPlaceBuilding(Vector2 gridPosition, int buildingSize)
        {
            bool isWaterPlant = grid.uiManager != null && grid.uiManager.GetSelectedBuilding() is WaterPlant;
            return TryValidateBuildingPlacement(gridPosition, buildingSize, isWaterPlant, out _);
        }

        /// <summary>
        /// Returns true if a building of the given size can be placed at the grid position, with explicit water plant flag.
        /// </summary>
        public bool CanPlaceBuilding(Vector2 gridPosition, int buildingSize, bool isWaterPlant)
        {
            return TryValidateBuildingPlacement(gridPosition, buildingSize, isWaterPlant, out _);
        }

        /// <summary>
        /// Returns the reason why building placement would fail at this position, or null if placement would succeed.
        /// </summary>
        public string GetBuildingPlacementFailReason(Vector2 gridPosition, int buildingSize, bool isWaterPlant)
        {
            TryValidateBuildingPlacement(gridPosition, buildingSize, isWaterPlant, out string failReason);
            return failReason;
        }

        private bool TryValidateBuildingPlacement(Vector2 gridPosition, int buildingSize, bool isWaterPlant, out string failReason)
        {
            failReason = null;

            if (buildingSize == 0)
            {
                failReason = "Building size is 0.";
                return false;
            }

            if (grid.interstateManager != null)
            {
                grid.interstateManager.CheckInterstateConnectivity();
                if (!grid.interstateManager.IsConnectedToInterstate)
                {
                    failReason = "No connection to Interstate Highway.";
                    if (grid.GameNotificationManager != null)
                        grid.GameNotificationManager.PostWarning("Connect a road to the Interstate Highway before building.");
                    return false;
                }
            }

            if (isWaterPlant)
                return TryValidateWaterPlantPlacement(gridPosition, buildingSize, out failReason);

            bool allowCoastalSlope = false;
            grid.GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);

            if (!grid.terrainManager.CanPlaceBuildingInTerrain(gridPosition, buildingSize, out failReason, allowCoastalSlope, false))
            {
                if (string.IsNullOrEmpty(failReason))
                    failReason = "Terrain: slope, water in footprint, height mismatch, or out of bounds.";
                return false;
            }

            for (int x = 0; x < buildingSize; x++)
            {
                for (int y = 0; y < buildingSize; y++)
                {
                    int gridX = (int)gridPosition.x + x - offsetX;
                    int gridY = (int)gridPosition.y + y - offsetY;

                    if (gridX < 0 || gridX >= grid.width || gridY < 0 || gridY >= grid.height)
                    {
                        failReason = "Footprint out of grid bounds.";
                        return false;
                    }

                    Cell cell = grid.cellArray[gridX, gridY];
                    if (cell.isInterstate)
                    {
                        failReason = "Cannot build on Interstate Highway.";
                        return false;
                    }
                    if (cell.zoneType != Zone.ZoneType.Grass)
                    {
                        failReason = $"Tile ({gridX},{gridY}) is not Grass (current: {cell.zoneType}).";
                        return false;
                    }
                }
            }

            return true;
        }

        private bool TryValidateWaterPlantPlacement(Vector2 gridPosition, int buildingSize, out string failReason)
        {
            failReason = null;
            grid.GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);

            for (int x = 0; x < buildingSize; x++)
            {
                for (int y = 0; y < buildingSize; y++)
                {
                    int gridX = (int)gridPosition.x + x - offsetX;
                    int gridY = (int)gridPosition.y + y - offsetY;
                    if (gridX < 0 || gridX >= grid.width || gridY < 0 || gridY >= grid.height)
                    {
                        failReason = "Footprint out of grid bounds.";
                        return false;
                    }
                }
            }

            if (!grid.terrainManager.CanPlaceBuildingInTerrain(gridPosition, buildingSize, out failReason, true, true))
                return false;

            bool hasLandTile = false;
            for (int x = 0; x < buildingSize; x++)
            {
                for (int y = 0; y < buildingSize; y++)
                {
                    int gridX = (int)gridPosition.x + x - offsetX;
                    int gridY = (int)gridPosition.y + y - offsetY;
                    if (grid.waterManager == null || !grid.waterManager.IsWaterAt(gridX, gridY))
                    {
                        hasLandTile = true;
                        Cell cell = grid.cellArray[gridX, gridY];
                        if (cell.zoneType != Zone.ZoneType.Grass)
                        {
                            failReason = $"Tile ({gridX},{gridY}) is not Grass (current: {cell.zoneType}).";
                            return false;
                        }
                    }
                }
            }

            if (!hasLandTile)
            {
                failReason = "Water plant must have at least one land tile in footprint.";
                return false;
            }

            if (grid.waterManager != null)
            {
                if (!HasWaterInFootprintPerimeter(gridPosition, buildingSize))
                {
                    failReason = "Water plant must be adjacent to water.";
                    return false;
                }
            }

            return true;
        }

        private bool IsCellWater(int x, int y)
        {
            bool fromWaterMap = grid.waterManager != null && grid.waterManager.IsWaterAt(x, y);

            int h = -1;
            var heightMap = grid.terrainManager != null ? grid.terrainManager.GetHeightMap() : null;
            bool validHeightMap = heightMap != null && heightMap.IsValidPosition(x, y);
            if (validHeightMap)
                h = heightMap.GetHeight(x, y);

            bool fromHeightMap = validHeightMap && h == TerrainManager.SEA_LEVEL;

            bool fromCellHeight = false;
            if (!fromHeightMap && x >= 0 && x < grid.width && y >= 0 && y < grid.height)
            {
                Cell cell = grid.cellArray[x, y];
                if (cell != null)
                {
                    h = cell.GetCellInstanceHeight();
                    fromCellHeight = h == TerrainManager.SEA_LEVEL;
                }
            }

            if (fromWaterMap) return true;
            if (fromHeightMap || fromCellHeight) return true;
            return false;
        }

        private bool HasWaterInFootprintPerimeter(Vector2 gridPosition, int buildingSize)
        {
            grid.GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);
            int minFx = (int)gridPosition.x - offsetX;
            int minFy = (int)gridPosition.y - offsetY;
            int maxFx = minFx + buildingSize - 1;
            int maxFy = minFy + buildingSize - 1;

            for (int px = minFx - 1; px <= maxFx + 1; px++)
            {
                for (int py = minFy - 1; py <= maxFy + 1; py++)
                {
                    if (px >= minFx && px <= maxFx && py >= minFy && py <= maxFy)
                        continue;
                    if (px >= 0 && px < grid.width && py >= 0 && py < grid.height)
                    {
                        if (IsCellWater(px, py))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Places a building at the grid position after checking affordability and validity.
        /// Deducts construction cost and posts notifications.
        /// </summary>
        public void PlaceBuilding(Vector2 gridPos, IBuilding iBuilding)
        {
            if (!grid.cityStats.CanAfford(iBuilding.ConstructionCost))
            {
                grid.uiManager.ShowInsufficientFundsTooltip("building", iBuilding.ConstructionCost);
                return;
            }

            bool isWaterPlant = iBuilding is WaterPlant;
            if (CanPlaceBuilding(gridPos, iBuilding.BuildingSize))
            {
                grid.cityStats.RemoveMoney(iBuilding.ConstructionCost);

                PlaceBuildingTile(iBuilding, gridPos);

                GameNotificationManager.Instance.PostBuildingConstructed(
                    iBuilding.Prefab.name
                );
            }
            else
            {
                string reason = GetBuildingPlacementFailReason(gridPos, iBuilding.BuildingSize, isWaterPlant);
                GameNotificationManager.Instance.PostBuildingPlacementError(
                    string.IsNullOrEmpty(reason) ? "Cannot place building here, area is not available." : reason
                );
            }
        }

        /// <summary>
        /// Instantiates a building tile at the grid position, sets sorting order, and updates cell attributes.
        /// </summary>
        public void PlaceBuildingTile(IBuilding iBuilding, Vector2 gridPos)
        {
            GameObject buildingPrefab = iBuilding.Prefab;
            int buildingSize = iBuilding.BuildingSize;

            Vector2 pivotGridPos = new Vector2(gridPos.x, gridPos.y);
            Vector2 position = grid.GetBuildingPlacementWorldPosition(pivotGridPos, buildingSize);

            Vector3 worldPosition = new Vector3(position.x, position.y, 0f);
            GameObject building = Object.Instantiate(buildingPrefab, worldPosition, Quaternion.identity);
            building.transform.SetParent(grid.gridArray[(int)pivotGridPos.x, (int)pivotGridPos.y].transform);

            sortingService.SetZoneBuildingSortingOrder(building, (int)pivotGridPos.x, (int)pivotGridPos.y, buildingSize);

            HandleBuildingPlacementAttributesUpdate(iBuilding, pivotGridPos, building, buildingPrefab);
        }

        /// <summary>
        /// Instantiates a building prefab at the grid position for save/load restore. Does not update cell attributes.
        /// </summary>
        public void LoadBuildingTile(GameObject prefab, Vector2 gridPos, int buildingSize)
        {
            Cell pivotCell = grid.cellArray[(int)gridPos.x, (int)gridPos.y];
            Vector2 worldPos = grid.GetBuildingPlacementWorldPosition(gridPos, buildingSize);

            Vector3 position = new Vector3(worldPos.x, worldPos.y, 0f);
            GameObject building = Object.Instantiate(prefab, position, Quaternion.identity);
            building.transform.SetParent(pivotCell.gameObject.transform);

            sortingService.SetZoneBuildingSortingOrder(building, (int)gridPos.x, (int)gridPos.y, buildingSize);
        }

        /// <summary>
        /// Place a building programmatically (e.g. auto resource planner). Caller is responsible for budget and affordability.
        /// Does not deduct money. Returns true if placed.
        /// </summary>
        public bool PlaceBuildingProgrammatic(Vector2 gridPos, IBuilding buildingTemplate)
        {
            if (buildingTemplate == null) return false;
            int buildingSize = buildingTemplate.BuildingSize;
            bool isWaterPlant = buildingTemplate is WaterPlant;
            if (!TryValidateBuildingPlacement(gridPos, buildingSize, isWaterPlant, out _))
                return false;

            Vector2 pivotGridPos = new Vector2(gridPos.x, gridPos.y);
            Vector2 position = grid.GetBuildingPlacementWorldPosition(pivotGridPos, buildingSize);
            Vector3 worldPosition = new Vector3(position.x, position.y, 0f);
            GameObject building = Object.Instantiate(buildingTemplate.Prefab, worldPosition, Quaternion.identity);
            building.transform.SetParent(grid.gridArray[(int)pivotGridPos.x, (int)pivotGridPos.y].transform);
            sortingService.SetZoneBuildingSortingOrder(building, (int)pivotGridPos.x, (int)pivotGridPos.y, buildingSize);

            IBuilding placedIBuilding = building.GetComponent<PowerPlant>() as IBuilding ?? building.GetComponent<WaterPlant>() as IBuilding;
            if (placedIBuilding != null)
                HandleBuildingPlacementAttributesUpdate(placedIBuilding, pivotGridPos, building, buildingTemplate.Prefab);
            return true;
        }

        private void UpdatePlacedBuildingCellAttributes(Cell cell, int buildingSize, PowerPlant powerPlant, WaterPlant waterPlant, GameObject buildingPrefab, Zone.ZoneType zoneType = Zone.ZoneType.Building, GameObject building = null)
        {
            cell.occupiedBuilding = building;
            cell.buildingSize = buildingSize;
            cell.prefab = buildingPrefab;
            cell.prefabName = buildingPrefab.name;
            cell.zoneType = zoneType;

            if (powerPlant != null)
            {
                cell.buildingType = "PowerPlant";
                cell.powerPlant = powerPlant;
            }

            if (waterPlant != null)
            {
                cell.buildingType = "WaterPlant";
                cell.waterPlant = waterPlant;
            }
        }

        private void SetCellAsBuildingPivot(Cell cell)
        {
            cell.isPivot = true;
        }

        private void UpdateBuildingTilesAttributes(Vector2 gridPos, GameObject building, int buildingSize, PowerPlant powerPlant, WaterPlant waterPlant, GameObject buildingPrefab)
        {
            grid.GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);

            for (int x = 0; x < buildingSize; x++)
            {
                for (int y = 0; y < buildingSize; y++)
                {
                    int gridX = (int)gridPos.x + x - offsetX;
                    int gridY = (int)gridPos.y + y - offsetY;

                    if (gridX >= 0 && gridX < grid.width && gridY >= 0 && gridY < grid.height)
                    {
                        GameObject gridCell = grid.gridArray[gridX, gridY];
                        Cell cell = grid.cellArray[gridX, gridY];

                        cell.RemoveForestForBuilding();
                        UpdatePlacedBuildingCellAttributes(cell, buildingSize, powerPlant, waterPlant, buildingPrefab, Zone.ZoneType.Building, building);

                        bool isPivot = (gridX == gridPos.x && gridY == gridPos.y);
                        grid.DestroyCellChildren(gridCell, new Vector2(gridX, gridY), isPivot ? building : null);
                        if (isPivot)
                            SetCellAsBuildingPivot(cell);
                    }
                }
            }
        }

        private void HandleBuildingPlacementAttributesUpdate(IBuilding iBuilding, Vector2 gridPos, GameObject building, GameObject buildingPrefab)
        {
            int buildingSize = iBuilding.BuildingSize;
            PowerPlant powerPlant = iBuilding.GameObjectReference.GetComponent<PowerPlant>();
            WaterPlant waterPlant = iBuilding.GameObjectReference.GetComponent<WaterPlant>();

            if (powerPlant != null)
            {
                grid.cityStats.RegisterPowerPlant(powerPlant);
            }

            if (waterPlant != null && grid.waterManager != null)
            {
                grid.waterManager.RegisterWaterPlant(waterPlant);
                grid.cityStats.cityWaterOutput = grid.waterManager.GetTotalWaterOutput();
            }

            UpdateBuildingTilesAttributes(gridPos, building, buildingSize, powerPlant, waterPlant, buildingPrefab);

            grid.cursorManager.RemovePreview();
        }
    }
}
