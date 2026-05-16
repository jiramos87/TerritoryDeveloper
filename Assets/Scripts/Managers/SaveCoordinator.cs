using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Territory.Persistence;
using Territory.SceneManagement;
using Territory.Services;
using Domains.Registry;

namespace Territory.Managers
{
    /// <summary>Interface for SaveCoordinator — allows test injection.</summary>
    public interface ISaveCoordinator
    {
        Task SavePair(string saveId, IsoSceneContext active, CancellationToken ct);
        Task<bool> LoadPairAvailable(string saveId, CancellationToken ct);
        Task<PairedSnapshot> LoadPair(string saveId, CancellationToken ct);
        Task CreatePair(string saveId, CancellationToken ct);
    }

    /// <summary>CoreScene hub — atomic paired .city + .region write. Backup protocol: .bak before write; delete on success; restore on fail. Invariant #3: resolve deps in Start.</summary>
    public class SaveCoordinator : MonoBehaviour, ISaveCoordinator
    {
        private const int InitialSnapshotGridSize = 64;

        private string _saveDir;
        private ServiceRegistry _registry;
        private TickClock _tickClock;
        private IRegionTickStamper _regionSaveService;

        void Awake()
        {
            _saveDir = Application.persistentDataPath;
            _registry = FindObjectOfType<ServiceRegistry>();
            if (_registry != null)
                _registry.Register<ISaveCoordinator>(this);
            else
                Debug.LogWarning("[SaveCoordinator] ServiceRegistry not found — ISaveCoordinator not registered.");
        }

        void Start()
        {
            // Stage 7.0 — resolve TickClock + IRegionTickStamper for lastTouchedTicks stamping.
            // Interface lookup avoids a direct dep on RegionScene asmdef (cyclic).
            _tickClock = FindObjectOfType<TickClock>();
            foreach (var mb in FindObjectsOfType<MonoBehaviour>())
            {
                if (mb is IRegionTickStamper stamper) { _regionSaveService = stamper; break; }
            }
        }

        // ── ISaveCoordinator ─────────────────────────────────────────────────

        /// <summary>Atomic paired write: writes active-scene full snapshot + other-scene stamp update. Backup .bak → write → delete .bak. Partial fail → restore + throw SaveFailedException.</summary>
        public async Task SavePair(string saveId, IsoSceneContext active, CancellationToken ct)
        {
            await Task.Yield(); // ensure async context for caller

            ct.ThrowIfCancellationRequested();

            string cityPath   = CityPath(saveId);
            string regionPath = RegionPath(saveId);
            string cityBak    = cityPath + ".bak";
            string regionBak  = regionPath + ".bak";

            // Phase 1 — backup existing files before any write.
            try
            {
                if (File.Exists(cityPath))   File.Copy(cityPath,   cityBak,   overwrite: true);
                if (File.Exists(regionPath)) File.Copy(regionPath, regionBak, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new SaveFailedException($"SaveCoordinator: backup phase failed for '{saveId}'.", ex);
            }

            // Stage 7.0 — stamp lastTouchedTicks into RegionSaveService before write.
            if (_regionSaveService != null && _tickClock != null)
            {
                // If growthSeed is 0 (new/migrated save), assign a new seed once.
                uint seed = _regionSaveService.LoadedGrowthSeed != 0
                    ? _regionSaveService.LoadedGrowthSeed
                    : (uint)UnityEngine.Random.Range(1, int.MaxValue);
                _regionSaveService.StampTicks(_tickClock.CurrentTick, seed);
            }

            try
            {
                ct.ThrowIfCancellationRequested();

                // Phase 2 — write city file.
                byte[] cityBytes = BuildCitySnapshot(saveId);
                await WriteAllBytesAsync(cityPath, cityBytes, ct);

                ct.ThrowIfCancellationRequested();

                // Phase 3 — write region file.
                byte[] regionBytes = BuildRegionStamp(saveId);
                await WriteAllBytesAsync(regionPath, regionBytes, ct);

                // Phase 4 — success: delete .bak files.
                if (File.Exists(cityBak))   File.Delete(cityBak);
                if (File.Exists(regionBak)) File.Delete(regionBak);
            }
            catch (SaveFailedException) { throw; }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                // Rollback: restore from .bak.
                TryRestore(cityPath,   cityBak);
                TryRestore(regionPath, regionBak);
                throw new SaveFailedException($"SaveCoordinator: write failed for '{saveId}'. Rolled back.", ex);
            }
        }

        /// <summary>Returns true if both .city and .region files exist for the given saveId.</summary>
        public Task<bool> LoadPairAvailable(string saveId, CancellationToken ct)
        {
            bool available = File.Exists(CityPath(saveId)) && File.Exists(RegionPath(saveId));
            return Task.FromResult(available);
        }

        /// <summary>Read paired files and return PairedSnapshot POCO.</summary>
        public async Task<PairedSnapshot> LoadPair(string saveId, CancellationToken ct)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();

            if (!File.Exists(CityPath(saveId)) || !File.Exists(RegionPath(saveId)))
                throw new FileNotFoundException($"SaveCoordinator: paired files not found for '{saveId}'.");

            byte[] cityBytes   = File.ReadAllBytes(CityPath(saveId));
            byte[] regionBytes = File.ReadAllBytes(RegionPath(saveId));

            return new PairedSnapshot
            {
                SaveId      = saveId,
                CityBytes   = cityBytes,
                RegionBytes = regionBytes,
                ActiveScene = IsoSceneContext.City
            };
        }

        /// <summary>New-game first-time variant. Writes initial empty city + initial region with player anchor.</summary>
        public async Task CreatePair(string saveId, CancellationToken ct)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();

            try
            {
                byte[] cityBytes   = BuildInitialCitySnapshot(saveId);
                byte[] regionBytes = BuildInitialRegionSnapshot(saveId);

                await WriteAllBytesAsync(CityPath(saveId),   cityBytes,   ct);
                await WriteAllBytesAsync(RegionPath(saveId), regionBytes, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                throw new SaveFailedException($"SaveCoordinator: CreatePair failed for '{saveId}'.", ex);
            }
        }

        // ── Path helpers ─────────────────────────────────────────────────────

        string CityPath(string saveId)   => Path.Combine(_saveDir, saveId + ".city");
        string RegionPath(string saveId) => Path.Combine(_saveDir, saveId + ".region");

        // ── Snapshot builders (stub data for Stage 2.0) ──────────────────────

        byte[] BuildCitySnapshot(string saveId)
        {
            var obj = new { saveId, schemaVersion = 1, kind = "city", timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
            return Encoding.UTF8.GetBytes(JsonShim.Serialize(obj));
        }

        byte[] BuildRegionStamp(string saveId)
        {
            var obj = new { saveId, schemaVersion = 1, kind = "region_stamp", timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
            return Encoding.UTF8.GetBytes(JsonShim.Serialize(obj));
        }

        byte[] BuildInitialCitySnapshot(string saveId)
        {
            var obj = new { saveId, schemaVersion = 1, kind = "city", initial = true, gridWidth = InitialSnapshotGridSize, gridHeight = InitialSnapshotGridSize };
            return Encoding.UTF8.GetBytes(JsonShim.Serialize(obj));
        }

        byte[] BuildInitialRegionSnapshot(string saveId)
        {
            var obj = new { saveId, schemaVersion = 1, kind = "region", initial = true, gridWidth = InitialSnapshotGridSize, gridHeight = InitialSnapshotGridSize, playerAnchorX = 0, playerAnchorY = 0 };
            return Encoding.UTF8.GetBytes(JsonShim.Serialize(obj));
        }

        // ── IO helpers ───────────────────────────────────────────────────────

        static async Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken ct)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true);
            await fs.WriteAsync(bytes, 0, bytes.Length, ct);
        }

        static void TryRestore(string target, string bak)
        {
            try
            {
                if (File.Exists(bak))
                {
                    if (File.Exists(target)) File.Delete(target);
                    File.Move(bak, target);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveCoordinator] Restore failed for '{target}': {ex.Message}");
            }
        }
    }

    /// <summary>Minimal JSON serializer shim — avoids Newtonsoft dep in new hub. Unity JsonUtility only serializes MonoBehaviour subclasses; use this for POCOs.</summary>
    internal static class JsonShim
    {
        /// <summary>Naive key-value serializer for simple anonymous types via reflection.</summary>
        public static string Serialize(object obj)
        {
            if (obj == null) return "{}";
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var prop in obj.GetType().GetProperties())
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(prop.Name).Append("\":");
                var val = prop.GetValue(obj);
                if (val is string s)   sb.Append('"').Append(s).Append('"');
                else if (val is bool b) sb.Append(b ? "true" : "false");
                else                    sb.Append(val);
            }
            sb.Append('}');
            return sb.ToString();
        }
    }
}
