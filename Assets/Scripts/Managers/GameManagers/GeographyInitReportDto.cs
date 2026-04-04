using System;

namespace Territory.Persistence
{
    /// <summary>
    /// Editor / batchmode export snapshot after geography init (TECH-39 §7.11.4). Not Save data — diagnostics and MCP harness input.
    /// Written to <c>tools/reports/last-geography-init.json</c> (gitignored).
    /// </summary>
    [Serializable]
    public class GeographyInitReportRootDto
    {
        public string artifact;
        public int schema_version;
        /// <summary>Approximate export time (UTC seconds) for tooling.</summary>
        public long exported_utc_unix_seconds;
        public int master_seed;
        public bool interchange_file_was_applied;
        /// <summary>
        /// When <see cref="interchange_file_was_applied"/> is true, JSON text produced with Unity <c>JsonUtility.ToJson</c> on the loaded
        /// <see cref="GeographyInitParamsDto"/> (same shape as <c>geography_init_params</c> interchange). Omitted from the export when not applied —
        /// <c>GeographyManager.BuildGeographyInitReportJson</c> strips <c>JsonUtility</c>'s empty-string encoding for null.
        /// </summary>
        public string interchange_snapshot_json;
        public int map_width;
        public int map_height;
        public bool generate_standard_water_bodies;
        public bool procedural_rivers_enabled_effective;
        public bool generate_test_river_on_init;
        public float forest_coverage_percentage;
        public string notes;
    }
}
