using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Events;

namespace Territory.Catalog
{
    /// <summary>
    /// Hot-reload consumer for the per-kind snapshot JSON pipeline
    /// (TECH-2675). Boots from <c>Application.streamingAssetsPath/{relative}</c>,
    /// watches the directory for changes, atomically swaps an immutable
    /// <see cref="IReadOnlyDictionary{TKey,TValue}"/> per Unity invariant 9.
    ///
    /// Threading: <see cref="FileSystemWatcher"/> events fire on a background
    /// thread; the handler queues a debounced reload via a Coroutine on the
    /// Unity main thread (<see cref="StartCoroutine"/>). The dictionary swap
    /// is a single <c>volatile</c> reference write so readers always observe
    /// either the previous or the newly published map — never a partial
    /// state. See <c>Reload</c> for the hash-parity gate that protects against
    /// torn reads while <c>web/lib/snapshot/export.ts</c> is mid-flight.
    /// </summary>
    public class CatalogLoader : MonoBehaviour
    {
        private static readonly string[] KindOrder =
        {
            "sprite",
            "asset",
            "button",
            "panel",
            "audio",
            "pool",
            "token",
            "archetype",
        };

        private const float DebounceSeconds = 0.25f;

        [Header("Paths")]
        [Tooltip("Directory under Application.streamingAssetsPath that holds manifest.json + per-kind JSON files.")]
        [SerializeField] private string _catalogRelativePath = "catalog";

        [Header("Events")]
        [SerializeField] private UnityEvent _onCatalogReloaded = new UnityEvent();

        // `volatile` ensures the publishing write is observed atomically by
        // readers on any thread (Unity main + Editor inspector) per
        // Unity invariant 9 immutable-replace contract.
        private volatile IReadOnlyDictionary<string, CatalogEntity> _entities =
            new Dictionary<string, CatalogEntity>();

        // TECH-1585 — legacy `subTypeId` (int) → `entity_id` (string) side index for
        // `asset` kind rows only. Rebuilt + atomic-swapped alongside `_entities` on
        // every reload. `volatile` for the same publishing-write contract as `_entities`
        // (Unity invariant 9). Non-`asset` rows + rows lacking `legacy_asset_id`
        // skipped at build time.
        private volatile IReadOnlyDictionary<int, string> _legacyAssetIdIndex =
            new Dictionary<int, string>();

        private FileSystemWatcher _watcher;
        private Coroutine _debounceCoroutine;
        private volatile bool _reloadPending;

        /// <summary>Current immutable snapshot; replaced on every successful reload.</summary>
        public IReadOnlyDictionary<string, CatalogEntity> Entities => _entities;

        /// <summary>Raised after a successful reload; safe to re-query <see cref="Entities"/>.</summary>
        public UnityEvent OnCatalogReloaded => _onCatalogReloaded;

        /// <summary>Absolute path to the catalog directory under StreamingAssets.</summary>
        public string CatalogDirectoryAbsolute =>
            Path.Combine(Application.streamingAssetsPath, _catalogRelativePath);

        private void Awake()
        {
            LoadInitial();
            StartWatching();
        }

        private void OnDestroy()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
            if (_debounceCoroutine != null)
            {
                StopCoroutine(_debounceCoroutine);
                _debounceCoroutine = null;
            }
        }

        /// <summary>Boot-time read; logs + leaves `_entities` empty on failure.</summary>
        public void LoadInitial()
        {
            string err;
            if (!TryBuildEntities(CatalogDirectoryAbsolute, out var fresh, out var freshLegacyIndex, out err))
            {
                Debug.LogError("[CatalogLoader] Initial load failed: " + err);
                return;
            }
            _entities = fresh;
            _legacyAssetIdIndex = freshLegacyIndex;
            if (_onCatalogReloaded != null) _onCatalogReloaded.Invoke();
        }

        /// <summary>
        /// Test entry point: rebuild the dictionary from disk + atomic-swap.
        /// Bypasses the FileSystemWatcher debounce coroutine for deterministic
        /// EditMode coverage.
        /// </summary>
        public bool Reload()
        {
            string err;
            if (!TryBuildEntities(CatalogDirectoryAbsolute, out var fresh, out var freshLegacyIndex, out err))
            {
                Debug.LogWarning("[CatalogLoader] Reload skipped: " + err);
                return false;
            }
            _entities = fresh;
            _legacyAssetIdIndex = freshLegacyIndex;
            if (_onCatalogReloaded != null) _onCatalogReloaded.Invoke();
            return true;
        }

        /// <summary>
        /// TECH-1585 — resolve a legacy `subTypeId` (0..6 from `ZoneSubTypeRegistry`)
        /// to the matching catalog `entity_id` via the live `asset`-kind side index.
        /// Returns <c>true</c> + the entity_id on hit; <c>false</c> + empty string
        /// on miss (caller dispatches placeholder per TECH-1587).
        /// </summary>
        public bool TryResolveByLegacyAssetId(int legacyAssetId, out string entityId)
        {
            var index = _legacyAssetIdIndex;
            if (index != null && index.TryGetValue(legacyAssetId, out var resolved))
            {
                entityId = resolved;
                return true;
            }
            entityId = string.Empty;
            return false;
        }

        private void StartWatching()
        {
            string dir = CatalogDirectoryAbsolute;
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning(
                    "[CatalogLoader] Catalog directory missing — watcher disabled: " + dir);
                return;
            }
            try
            {
                _watcher = new FileSystemWatcher(dir, "*.json")
                {
                    NotifyFilter =
                        NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                    EnableRaisingEvents = true,
                };
                _watcher.Changed += OnWatcherChanged;
                _watcher.Created += OnWatcherChanged;
                _watcher.Renamed += OnWatcherChanged;
            }
            catch (Exception ex)
            {
                Debug.LogError("[CatalogLoader] FileSystemWatcher init failed: " + ex.Message);
            }
        }

        // Background thread → flag the reload + start debounce on Unity main.
        private void OnWatcherChanged(object sender, FileSystemEventArgs args)
        {
            _reloadPending = true;
            // Coroutine start MUST happen on the main thread; Unity throws on
            // background calls. We rely on `_reloadPending` being polled in
            // `Update` instead.
        }

        private void Update()
        {
            if (!_reloadPending) return;
            _reloadPending = false;
            if (_debounceCoroutine != null) StopCoroutine(_debounceCoroutine);
            _debounceCoroutine = StartCoroutine(DebounceReload());
        }

        private IEnumerator DebounceReload()
        {
            yield return new WaitForSecondsRealtime(DebounceSeconds);
            // Coalesce any further events that arrived during the wait.
            _reloadPending = false;
            Reload();
            _debounceCoroutine = null;
        }

        /// <summary>
        /// Pure helper: read manifest + per-kind files from <paramref name="dir"/>,
        /// recompute the kind-ordered sha256 hash, parity-check vs the
        /// manifest, build the immutable lookup. Returns <c>false</c> on any
        /// failure with a diagnostic in <paramref name="err"/>.
        /// TECH-1585 — also builds the side `legacy_asset_id (int) → entity_id`
        /// dictionary for `asset` kind rows in <paramref name="freshLegacyAssetIdIndex"/>.
        /// </summary>
        public static bool TryBuildEntities(
            string dir,
            out IReadOnlyDictionary<string, CatalogEntity> fresh,
            out IReadOnlyDictionary<int, string> freshLegacyAssetIdIndex,
            out string err)
        {
            fresh = null;
            freshLegacyAssetIdIndex = null;
            err = null;
            if (string.IsNullOrEmpty(dir))
            {
                err = "Catalog directory path is null or empty.";
                return false;
            }
            if (!Directory.Exists(dir))
            {
                err = "Catalog directory missing: " + dir;
                return false;
            }
            string manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                err = "manifest.json missing at " + manifestPath;
                return false;
            }

            CatalogManifest manifest;
            try
            {
                string manifestText = File.ReadAllText(manifestPath);
                manifest = JsonUtility.FromJson<CatalogManifest>(manifestText);
            }
            catch (Exception ex)
            {
                err = "manifest.json parse failed: " + ex.Message;
                return false;
            }
            if (manifest == null || string.IsNullOrEmpty(manifest.snapshotHash))
            {
                err = "manifest.json missing snapshotHash.";
                return false;
            }

            // Read all 8 per-kind files into byte buffers up front so the hash
            // recompute observes the same bytes the parser will see (no
            // double-read race window during partial writes).
            var perKindBytes = new Dictionary<string, byte[]>(KindOrder.Length);
            foreach (var kind in KindOrder)
            {
                string kindPath = Path.Combine(dir, kind + ".json");
                if (!File.Exists(kindPath))
                {
                    err = "Per-kind file missing: " + kindPath;
                    return false;
                }
                perKindBytes[kind] = File.ReadAllBytes(kindPath);
            }

            string computed = ComputeKindOrderedHashHex(perKindBytes);
            if (!string.Equals(computed, manifest.snapshotHash, StringComparison.Ordinal))
            {
                err = "Hash parity failed: manifest=" + manifest.snapshotHash + " computed=" + computed;
                return false;
            }

            // Parse per-kind files + populate immutable dictionary keyed by
            // entity_id. Duplicate keys fail the build (would mask data drift).
            var dict = new Dictionary<string, CatalogEntity>(manifest.entityCounts != null
                ? manifest.entityCounts.Total
                : 0);
            // TECH-1585 — side index for `asset` kind only. Legacy `subTypeId` ids
            // are 0..6 today; map covers any future range so long as the snapshot
            // exporter keeps `legacy_asset_id` populated. Duplicate legacy ids
            // logged as warnings (not fatal — DB row uniqueness is exporter
            // territory; first-seen wins here so reload still publishes).
            var legacyDict = new Dictionary<int, string>();
            foreach (var kind in KindOrder)
            {
                string text = System.Text.Encoding.UTF8.GetString(perKindBytes[kind]);
                CatalogPerKindFile parsed;
                try
                {
                    parsed = JsonUtility.FromJson<CatalogPerKindFile>(text);
                }
                catch (Exception ex)
                {
                    err = "Parse failed for " + kind + ".json: " + ex.Message;
                    return false;
                }
                if (parsed == null)
                {
                    err = "Parser returned null for " + kind + ".json";
                    return false;
                }
                if (parsed.rows == null) continue;
                foreach (var row in parsed.rows)
                {
                    if (row == null || string.IsNullOrEmpty(row.entity_id)) continue;
                    row.Kind = kind;
                    if (dict.ContainsKey(row.entity_id))
                    {
                        err = "Duplicate entity_id across per-kind files: " + row.entity_id;
                        return false;
                    }
                    dict[row.entity_id] = row;

                    // TECH-1585 — only `asset` rows carry the legacy carrier.
                    if (kind == "asset" && !string.IsNullOrEmpty(row.legacy_asset_id))
                    {
                        if (int.TryParse(row.legacy_asset_id, out var legacyInt))
                        {
                            if (legacyDict.ContainsKey(legacyInt))
                            {
                                Debug.LogWarning(
                                    "[CatalogLoader] Duplicate legacy_asset_id " + legacyInt
                                    + " — keeping first-seen entity_id, ignoring " + row.entity_id);
                            }
                            else
                            {
                                legacyDict[legacyInt] = row.entity_id;
                            }
                        }
                        else
                        {
                            Debug.LogWarning(
                                "[CatalogLoader] legacy_asset_id parse failed for entity_id="
                                + row.entity_id + " value=" + row.legacy_asset_id);
                        }
                    }
                }
            }

            fresh = dict;
            freshLegacyAssetIdIndex = legacyDict;
            return true;
        }

        /// <summary>
        /// sha256 hex over kind-ordered concatenation of the per-kind file
        /// bytes. Mirrors <c>computeManifestHash</c> in
        /// <c>web/lib/snapshot/manifest.ts</c>.
        /// </summary>
        public static string ComputeKindOrderedHashHex(IDictionary<string, byte[]> perKindBytes)
        {
            using (var sha = SHA256.Create())
            {
                foreach (var kind in KindOrder)
                {
                    if (!perKindBytes.TryGetValue(kind, out var buf) || buf == null)
                    {
                        throw new InvalidOperationException(
                            "ComputeKindOrderedHashHex: missing buffer for kind \"" + kind + "\"");
                    }
                    sha.TransformBlock(buf, 0, buf.Length, null, 0);
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                var hashBytes = sha.Hash;
                var sb = new System.Text.StringBuilder(hashBytes.Length * 2);
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
