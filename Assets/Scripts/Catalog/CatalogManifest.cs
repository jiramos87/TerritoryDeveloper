using System;

namespace Territory.Catalog
{
    /// <summary>
    /// Mirror of the manifest written by <c>web/lib/snapshot/export.ts</c>
    /// (TECH-2673). Field names match <c>computeManifestHash</c> + the JSON
    /// keys produced by <c>canonicalStringify</c> — camelCase top-level
    /// fields, nested kind→count map.
    ///
    /// `EntityCounts` is a fixed-shape POCO instead of <c>Dictionary</c>
    /// because <see cref="UnityEngine.JsonUtility"/> cannot deserialize
    /// arbitrary maps. The closed 8-kind set per DEC-A9 stays stable across
    /// releases — adding a kind requires bumping <c>schemaVersion</c>.
    /// </summary>
    [Serializable]
    public class CatalogManifest
    {
        public int schemaVersion;
        public string generatedAt;
        public string snapshotHash;
        public CatalogEntityCounts entityCounts;
    }

    /// <summary>Per-kind row counts — closed 8-kind set per DEC-A9.</summary>
    [Serializable]
    public class CatalogEntityCounts
    {
        public int sprite;
        public int asset;
        public int button;
        public int panel;
        public int audio;
        public int pool;
        public int token;
        public int archetype;

        /// <summary>Sum across all 8 kinds — used for dictionary capacity hints + smoke assertions.</summary>
        public int Total =>
            sprite + asset + button + panel + audio + pool + token + archetype;
    }
}
