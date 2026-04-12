using System;

namespace Territory.Persistence
{
    /// <summary>
    /// Editor/batchmode export snapshot after geography init. Not Save data — diagnostics + MCP harness input.
    /// Written to <c>tools/reports/last-geography-init.json</c> (gitignored).
    /// </summary>
    [Serializable]
    public class GeographyInitReportRootDto
    {
        public string artifact;
        public int schema_version;
        /// <summary>Approx export time (UTC seconds) for tooling.</summary>
        public long exported_utc_unix_seconds;
        public int master_seed;
        public bool interchange_file_was_applied;
        /// <summary>
        /// When <see cref="interchange_file_was_applied"/> true → JSON from <c>JsonUtility.ToJson</c> on loaded
        /// <see cref="GeographyInitParamsDto"/> (same shape as <c>geography_init_params</c> interchange).
        /// Omitted when not applied — <c>GeographyManager.BuildGeographyInitReportJson</c> strips
        /// <c>JsonUtility</c> empty-string encoding for null.
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
