using UnityEngine;
using Territory.IsoSceneCore.Contracts;
using Territory.Persistence;
using Territory.RegionScene.Evolution;
using Territory.RegionScene.Persistence;
using Territory.RegionScene.Terrain;

namespace Territory.RegionScene.Tools
{
    /// <summary>
    /// Region-level city-placement tool. Slug = "region.create-city"; Slot = Primary.
    /// Registered into IIsoSceneToolRegistry from RegionManager.Start (invariant #12).
    /// Implements IIsoSceneCellClickHandler to receive left-click events from RegionCellClickHandler.
    /// Tracer anchor: PlacesCityAndCreatesLazyCityData
    /// </summary>
    public sealed class RegionToolCreateCity : IIsoSceneCellClickHandler
    {
        /// <summary>Unique tool slug used for tile name and action routing.</summary>
        public const string ToolSlug = "region.create-city";

        /// <summary>Label shown in toolbar tile.</summary>
        public const string ToolLabel = "Found City";

        private RegionData _regionData;
        private RegionHeightMap _heightMap;
        private RegionWaterMap _waterMap;
        private RegionCliffMap _cliffMap;
        private RegionSaveService _saveService;

        private IIsoSceneCellClickDispatcher _dispatcher;
        private bool _isActive;

        /// <summary>Build the IsoSceneTool descriptor for registry registration.</summary>
        public static IsoSceneTool MakeDescriptor()
            => new IsoSceneTool(ToolSlug, ToolLabel, ToolbarSlot.Primary);

        /// <summary>Wire scene dependencies. Called from RegionManager.Start after registry resolve.</summary>
        public void Configure(
            RegionData regionData,
            RegionHeightMap heightMap,
            RegionWaterMap waterMap,
            RegionCliffMap cliffMap,
            IIsoSceneCellClickDispatcher dispatcher,
            RegionSaveService saveService = null)
        {
            _regionData  = regionData;
            _heightMap   = heightMap;
            _waterMap    = waterMap;
            _cliffMap    = cliffMap;
            _saveService = saveService;
            _dispatcher  = dispatcher;

            // Subscribe to click events in Configure (called from Start, satisfies invariant #12).
            _dispatcher?.Subscribe(this);
        }

        /// <summary>Enable this tool so click events trigger city placement. Deactivate other tools first.</summary>
        public void Activate()  => _isActive = true;

        /// <summary>Deactivate — click events received but no-op.</summary>
        public void Deactivate() => _isActive = false;

        // IIsoSceneCellClickHandler
        public void OnLeftClick(Vector2Int cell)
        {
            if (!_isActive) return;
            PlacesCityAndCreatesLazyCityData(cell);
        }

        public void OnRightClick(Vector2Int cell) { /* no-op */ }

        /// <summary>
        /// Core placement logic — anchor method for §Red-Stage Proof keyword scan.
        /// Guards: in-bounds, not already owned, not water/cliff.
        /// Delegates to RegionSaveService.LinkCity + CityDataFactory.CreateLazy (Stage 5.0.3).
        /// </summary>
        internal void PlacesCityAndCreatesLazyCityData(Vector2Int cell)
        {
            // In-bounds guard
            if (!InBounds(cell)) return;

            var cellData = _regionData?.GetCell(cell.x, cell.y);
            if (cellData == null) return;

            // Already owned by a city → early return
            if (!string.IsNullOrEmpty(cellData.owningCityId)) return;

            // Terrain guard: water and cliff cells are invalid placement targets
            bool isWater = _waterMap != null && _waterMap.IsWater(cell.x, cell.y);
            bool isCliff = _cliffMap != null && _cliffMap.IsCliff(cell.x, cell.y);
            if (isWater || isCliff) return;

            // Atomic placement: CreateLazy → LinkCity → auto-save
            var cityData = CityDataFactory.CreateLazy(cell);
            _saveService?.LinkCity(cell, cityData);
            // Immediately persist (atomic write per spec)
            _saveService?.WriteSave("region");
            Debug.Log($"[RegionToolCreateCity] City placed at ({cell.x},{cell.y}) id={cityData.cityId}");
        }

        private static bool InBounds(Vector2Int cell)
            => cell.x >= 0 && cell.x < RegionHeightMap.RegionGridSize
            && cell.y >= 0 && cell.y < RegionHeightMap.RegionGridSize;
    }
}
