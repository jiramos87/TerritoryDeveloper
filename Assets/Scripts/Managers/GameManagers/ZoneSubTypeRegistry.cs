using System;
using UnityEngine;

/// <summary>
/// Loads Zone S sub-type entries from Resources JSON and maps legacy subTypeId 0..6 to
/// <see cref="GridAssetCatalog"/> rows for catalog-backed costs and labels (Stage 2.3).
/// </summary>
public class ZoneSubTypeRegistry : MonoBehaviour
{
    [Serializable]
    public class ZoneSubTypeEntry
    {
        public int id;
        public string displayName;
        public string prefabPath;    // Resources-relative path; empty = no prefab
        public string iconPath;      // Resources-relative path; empty = no icon
        public int baseCost;
        public int monthlyUpkeep;

        [NonSerialized] public GameObject prefab;
        [NonSerialized] public Sprite icon;
    }

    [Serializable]
    private class RegistryData
    {
        public ZoneSubTypeEntry[] entries;
    }

    [SerializeField] private string configResourcePath = "Economy/zone-sub-types";

    [SerializeField] private GridAssetCatalog _gridAssetCatalog;

    private ZoneSubTypeEntry[] _entries = Array.Empty<ZoneSubTypeEntry>();

    public System.Collections.Generic.IReadOnlyList<ZoneSubTypeEntry> Entries => _entries;

    /// <summary>Scene catalog instance resolved in <see cref="Awake"/>; used by Stage 2.3 map + UI.</summary>
    internal GridAssetCatalog Catalog => _gridAssetCatalog;

    private void Awake()
    {
        if (_gridAssetCatalog == null)
            _gridAssetCatalog = FindObjectOfType<GridAssetCatalog>();
        if (_gridAssetCatalog == null)
        {
            Debug.LogError("[ZoneSubTypeRegistry] GridAssetCatalog not found in scene.");
            return;
        }
        LoadFromJson();
    }

    public void LoadFromJson()
    {
        var asset = Resources.Load<TextAsset>(configResourcePath);
        if (asset == null)
        {
            Debug.LogError($"[ZoneSubTypeRegistry] Config not found at Resources/{configResourcePath}.json");
            return;
        }
        var data = JsonUtility.FromJson<RegistryData>(asset.text);
        _entries = data?.entries ?? Array.Empty<ZoneSubTypeEntry>();
        foreach (var entry in _entries)
        {
            if (!string.IsNullOrEmpty(entry.prefabPath))
                entry.prefab = Resources.Load<GameObject>(entry.prefabPath);
            if (!string.IsNullOrEmpty(entry.iconPath))
                entry.icon = Resources.Load<Sprite>(entry.iconPath);
        }
    }

    /// <summary>
    /// Returns entry matching id, or null. id -1 returns null (Zone.subTypeId sentinel).
    /// Duplicate ids: first-match wins.
    /// </summary>
    public ZoneSubTypeEntry GetById(int id)
    {
        if (_entries == null) return null;
        foreach (var entry in _entries)
            if (entry.id == id) return entry;
        return null;
    }
}
