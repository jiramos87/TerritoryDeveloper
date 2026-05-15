using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Territory.Core
{
/// <summary>
/// Static feature-flag registry. Hydrated at boot from
/// <c>tools/interchange/feature-flags-snapshot.json</c> by the bootstrap
/// MonoBehaviour Awake hook. The bridge <c>flag_flip</c> kind calls
/// <see cref="InvalidateCache"/> then <see cref="HydrateFromJson"/> to
/// pick up a fresh snapshot without restarting Play Mode.
/// </summary>
public static class FeatureFlags
{
    static Dictionary<string, bool> _cache;

    /// <summary>Returns the enabled state for <paramref name="slug"/>.
    /// Falls back to <c>false</c> when slug is absent or cache not hydrated.</summary>
    public static bool IsEnabled(string slug)
    {
        if (_cache == null || string.IsNullOrEmpty(slug)) return false;
        return _cache.TryGetValue(slug, out bool v) && v;
    }

    /// <summary>Reads the interchange snapshot at <paramref name="absolutePath"/> and
    /// populates the in-memory cache. Safe to call multiple times; replaces prior cache.</summary>
    public static void HydrateFromJson(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            Debug.LogWarning("[FeatureFlags] HydrateFromJson called with null/empty path — skipped.");
            return;
        }
        if (!File.Exists(absolutePath))
        {
            Debug.LogWarning($"[FeatureFlags] Snapshot not found at '{absolutePath}' — cache cleared.");
            _cache = new Dictionary<string, bool>(StringComparer.Ordinal);
            return;
        }
        try
        {
            string json = File.ReadAllText(absolutePath);
            var dto = JsonUtility.FromJson<FeatureFlagsSnapshotDto>(json);
            var next = new Dictionary<string, bool>(StringComparer.Ordinal);
            if (dto?.flags != null)
                foreach (var f in dto.flags)
                    if (!string.IsNullOrEmpty(f.slug))
                        next[f.slug] = f.enabled;
            _cache = next;
            Debug.Log($"[FeatureFlags] Hydrated {_cache.Count} flag(s) from '{absolutePath}'.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FeatureFlags] Failed to parse snapshot — cache cleared. {ex.Message}");
            _cache = new Dictionary<string, bool>(StringComparer.Ordinal);
        }
    }

    /// <summary>Clears the cache; the next <see cref="IsEnabled"/> call will return false
    /// until <see cref="HydrateFromJson"/> is called again.</summary>
    public static void InvalidateCache()
    {
        _cache = null;
        Debug.Log("[FeatureFlags] Cache invalidated.");
    }

    // ── DTOs (JsonUtility-compatible) ─────────────────────────────────────

    [Serializable]
    sealed class FeatureFlagsSnapshotDto
    {
        public string artifact;
        public int schema_version;
        public FlagEntryDto[] flags;
    }

    [Serializable]
    sealed class FlagEntryDto
    {
        public string slug;
        public bool enabled;
        public bool default_value;
    }
}
}
