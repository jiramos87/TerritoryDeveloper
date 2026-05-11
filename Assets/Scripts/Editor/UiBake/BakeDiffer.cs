using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Territory.Editor.UiBake
{
    /// <summary>
    /// Layer 2 bake diff baseline (TECH-28363).
    /// Compares a baked prefab's children against a golden <see cref="BakeBaseline"/>
    /// manifest. Returns <see cref="BakeDiffResult"/> with added/removed/changed sets.
    /// Golden manifests live under Assets/Resources/UI/Generated/Baselines/{panel}.json.
    /// </summary>
    public static class BakeDiffer
    {
        /// <summary>
        /// Diff a baked <paramref name="prefab"/> root against <paramref name="baseline"/>.
        /// Compares immediate child names only (first-level; deep diff deferred to T3+).
        /// </summary>
        public static BakeDiffResult Diff(GameObject prefab, BakeBaseline baseline)
        {
            if (prefab == null)   throw new ArgumentNullException(nameof(prefab));
            if (baseline == null) throw new ArgumentNullException(nameof(baseline));

            // Collect live child names from the baked prefab root.
            var liveNames = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < prefab.transform.childCount; i++)
            {
                var child = prefab.transform.GetChild(i);
                if (child != null) liveNames.Add(child.name);
            }

            // Baseline set.
            var baselineNames = new HashSet<string>(
                baseline.childNames ?? Array.Empty<string>(), StringComparer.Ordinal);

            var added   = new List<string>();
            var removed = new List<string>();

            foreach (var name in liveNames)
                if (!baselineNames.Contains(name)) added.Add(name);

            foreach (var name in baselineNames)
                if (!liveNames.Contains(name)) removed.Add(name);

            return new BakeDiffResult
            {
                added   = added.ToArray(),
                removed = removed.ToArray(),
                changed = Array.Empty<BakeChangedEntry>(),
            };
        }

        // ── Baseline persistence helpers ─────────────────────────────────────────

        private const string BaselineDir = "Assets/Resources/UI/Generated/Baselines";

        /// <summary>Load persisted baseline from disk. Returns null when not found.</summary>
        public static BakeBaseline LoadBaseline(string panelSlug)
        {
            var path = BaselinePath(panelSlug);
            if (!File.Exists(path)) return null;
            try
            {
                var json = File.ReadAllText(path);
                return JsonUtility.FromJson<BakeBaseline>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Persist <paramref name="baseline"/> to disk as commit artefact.</summary>
        public static void SaveBaseline(BakeBaseline baseline)
        {
            if (baseline == null) throw new ArgumentNullException(nameof(baseline));
            Directory.CreateDirectory(BaselineDir);
            var path = BaselinePath(baseline.panelSlug);
            File.WriteAllText(path, JsonUtility.ToJson(baseline, prettyPrint: true));
        }

        private static string BaselinePath(string panelSlug) =>
            $"{BaselineDir}/{panelSlug}.json";
    }

    /// <summary>Golden manifest for one panel's baked child hierarchy.</summary>
    [Serializable]
    public sealed class BakeBaseline
    {
        public string   panelSlug;
        public string[] childNames;
    }

    /// <summary>Diff result returned by <see cref="BakeDiffer.Diff"/>.</summary>
    public sealed class BakeDiffResult
    {
        /// <summary>Child names present in the live bake but absent from baseline.</summary>
        public string[] added;
        /// <summary>Child names present in baseline but absent from the live bake.</summary>
        public string[] removed;
        /// <summary>Children present in both but with changed component composition.</summary>
        public BakeChangedEntry[] changed;
    }

    /// <summary>One changed-child entry in a <see cref="BakeDiffResult"/>.</summary>
    public sealed class BakeChangedEntry
    {
        public string name;
        public string description;
    }
}
