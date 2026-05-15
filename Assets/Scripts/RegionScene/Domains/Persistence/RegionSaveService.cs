using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Domains.Registry;
using Territory.Persistence;
using Territory.RegionScene.Evolution;

namespace Territory.RegionScene.Persistence
{
    /// <summary>Writes + loads &lt;saveName&gt;.region.json sidecar alongside GameSaveData. Registered into ServiceRegistry from RegionManager.Start.</summary>
    public sealed class RegionSaveService : MonoBehaviour
    {
        private RegionData _regionData;
        private string _basePath;
        private readonly List<CityData> _lazyCities = new();

        private void Start()
        {
            var registry = FindObjectOfType<ServiceRegistry>();
            if (registry == null)
            {
                Debug.LogWarning("[RegionSaveService] ServiceRegistry not found — save/load disabled.");
                return;
            }
            _regionData = registry.Resolve<RegionData>();
            _basePath   = Application.persistentDataPath;
            registry.Register<RegionSaveService>(this);
        }

        /// <summary>Serialize current RegionData to &lt;basePath&gt;/&lt;saveName&gt;.region.json. Returns absolute file path.</summary>
        public string WriteSave(string saveName)
        {
            var file = BuildSaveFile();
            string path = BuildSavePath(saveName);
            File.WriteAllText(path, JsonUtility.ToJson(file, prettyPrint: false));
            return path;
        }

        /// <summary>Deserialize &lt;saveName&gt;.region.json and populate RegionData. Returns loaded DTO.</summary>
        public RegionSaveFile LoadSave(string saveName)
        {
            string path = BuildSavePath(saveName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[RegionSaveService] Save file not found: {path}");
                return null;
            }
            string json = File.ReadAllText(path);
            var file    = JsonUtility.FromJson<RegionSaveFile>(json);
            file = MigrateLoadedSaveData(file);
            PopulateRegionData(file);
            return file;
        }

        /// <summary>
        /// Atomic city-placement mutation. Links cell to city and stores the lazy CityData record.
        /// Both operations happen before any write; caller must call WriteSave to persist.
        /// </summary>
        public void LinkCity(Vector2Int cell, CityData cityData)
        {
            if (_regionData == null || cityData == null) return;
            var cellData = _regionData.GetCell(cell.x, cell.y);
            if (cellData == null) return;

            // Link cell → city
            cellData.owningCityId = cityData.cityId;

            // Record lazy CityData entry (avoid duplicates on re-placement)
            if (!_lazyCities.Exists(c => c.cityId == cityData.cityId))
                _lazyCities.Add(cityData);
        }

        /// <summary>Read-only snapshot of lazy-created cities in this session.</summary>
        public IReadOnlyList<CityData> LazyCities => _lazyCities;

        /// <summary>Migration hook: bumps schema version; missing fields default-init.</summary>
        public static RegionSaveFile MigrateLoadedSaveData(RegionSaveFile file)
        {
            if (file == null) return null;
            // v1 → v2: lazyCities list added in Stage 5.0
            if (file.cells == null) file.cells = System.Array.Empty<RegionCellData>();
            if (file.cityOwnership == null) file.cityOwnership = new List<CityOwnershipEntry>();
            if (file.lazyCities == null) file.lazyCities = new List<CityData>();
            file.schemaVersion = RegionSaveFile.CurrentSchemaVersion;
            return file;
        }

        private RegionSaveFile BuildSaveFile()
        {
            var cells  = _regionData?.AllCells ?? System.Array.Empty<RegionCellData>();
            int gSize  = _regionData?.GridSize ?? 0;
            var file   = new RegionSaveFile
            {
                schemaVersion  = RegionSaveFile.CurrentSchemaVersion,
                gridSize       = gSize,
                cells          = cells,
                cityOwnership  = new List<CityOwnershipEntry>(),
                lazyCities     = new List<CityData>(_lazyCities),
            };

            // Populate city ownership from cell owningCityId fields.
            for (int i = 0; i < cells.Length; i++)
            {
                if (!string.IsNullOrEmpty(cells[i]?.owningCityId))
                    file.cityOwnership.Add(new CityOwnershipEntry { cityId = cells[i].owningCityId, cellIndex = i });
            }
            return file;
        }

        private void PopulateRegionData(RegionSaveFile file)
        {
            if (_regionData == null || file?.cells == null) return;
            int count = Mathf.Min(file.cells.Length, _regionData.AllCells.Length);
            for (int i = 0; i < count; i++)
            {
                if (file.cells[i] != null)
                    _regionData.AllCells[i] = file.cells[i];
            }

            // Restore lazy cities from save
            _lazyCities.Clear();
            if (file.lazyCities != null)
            {
                foreach (var c in file.lazyCities)
                    if (c != null) _lazyCities.Add(CityData.MigrateLoaded(c));
            }
        }

        private string BuildSavePath(string saveName)
            => Path.Combine(_basePath ?? Application.persistentDataPath, $"{saveName}.region.json");
    }
}
