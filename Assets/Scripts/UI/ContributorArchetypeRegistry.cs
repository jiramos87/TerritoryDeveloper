using System;
using System.Collections.Generic;
using UnityEngine;

namespace Territory.UI
{
    /// <summary>
    /// Stage 9.8 (TECH-15894–15897) — loads picker-contributors.json and exposes per-family entry
    /// enumeration for <see cref="SubtypePickerController.BuildRows"/>.
    /// Resources path: <c>UI/picker-contributors</c>.
    /// </summary>
    public class ContributorArchetypeRegistry : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public string family;
            public string subtype;
            public string prefabPath;     // Resources-relative; null for manager-hook entries
            public string managerHook;    // e.g. "RoadManager.TwoWay"; null for prefab entries
            public string iconSlug;
            public int baseCost;
            public string pollution;      // "high" | "zero" | null
        }

        [Serializable]
        private class RegistryData
        {
            public Entry[] entries;
        }

        [SerializeField] private string configResourcePath = "UI/picker-contributors";

        private Entry[] _entries = Array.Empty<Entry>();

        private void Awake()
        {
            LoadFromJson();
        }

        private void LoadFromJson()
        {
            var asset = Resources.Load<TextAsset>(configResourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[ContributorArchetypeRegistry] Config not found at Resources/{configResourcePath}.json");
                return;
            }
            try
            {
                var data = JsonUtility.FromJson<RegistryData>(asset.text);
                _entries = data?.entries ?? Array.Empty<Entry>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ContributorArchetypeRegistry] JSON parse error: {ex.Message}");
            }
        }

        /// <summary>Returns all entries whose <c>family</c> matches the given <see cref="ToolFamily"/>.</summary>
        public IReadOnlyList<Entry> GetEntries(ToolFamily family)
        {
            string familyName = family.ToString();
            var result = new List<Entry>();
            for (int i = 0; i < _entries.Length; i++)
            {
                if (string.Equals(_entries[i].family, familyName, StringComparison.OrdinalIgnoreCase))
                    result.Add(_entries[i]);
            }
            return result;
        }
    }
}
