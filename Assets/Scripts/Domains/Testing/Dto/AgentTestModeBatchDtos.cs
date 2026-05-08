#if UNITY_EDITOR
using System;

namespace Domains.Testing.Dto
{
    /// <summary>Integer CityStats slice for golden checks. schema_version 2 adds regionId/countryId.</summary>
    [Serializable]
    public class AgentTestModeBatchCitySnapshotDto
    {
        public int schema_version = 2;
        public int simulation_ticks;
        public int population;
        public int money;
        public int roadCount;
        public int grassCount;
        public int residentialZoneCount;
        public int commercialZoneCount;
        public int industrialZoneCount;
        public int residentialBuildingCount;
        public int commercialBuildingCount;
        public int industrialBuildingCount;
        public int forestCellCount;
        public string regionId = "";
        public string countryId = "";
    }

    /// <summary>Neighbor-stub round-trip golden DTO. schema_version 1 only.</summary>
    [Serializable]
    public class NeighborStubRoundtripGoldenDto
    {
        public int schema_version = 1;
        public NeighborStubGoldenEntry[] neighborStubs = Array.Empty<NeighborStubGoldenEntry>();
        public NeighborBindingGoldenEntry[] neighborCityBindings = Array.Empty<NeighborBindingGoldenEntry>();
    }

    /// <summary>One neighbor stub entry inside <see cref="NeighborStubRoundtripGoldenDto"/>.</summary>
    [Serializable]
    public class NeighborStubGoldenEntry
    {
        public string id = "";
        public string displayName = "";
        public string borderSide = "";
    }

    /// <summary>One binding entry inside <see cref="NeighborStubRoundtripGoldenDto"/>.</summary>
    [Serializable]
    public class NeighborBindingGoldenEntry
    {
        public string stubId = "";
        public int exitCellX;
        public int exitCellY;
        public string borderSide = "";
    }

    /// <summary>Single HeightMap[x,y] != CityCell.height mismatch row (first 10 only).</summary>
    [Serializable]
    public class HeightIntegrityViolationDto
    {
        public int x;
        public int y;
        public int heightMap;
        public int cell;
    }

    /// <summary>Result of one HeightIntegritySweep pass (post-load or post-tick).</summary>
    [Serializable]
    public class HeightIntegritySweepResultDto
    {
        public int checked_cells;
        public int violations;
        public HeightIntegrityViolationDto[] first_offenders = Array.Empty<HeightIntegrityViolationDto>();
    }

    /// <summary>Combined height_integrity object emitted in the batch report.</summary>
    [Serializable]
    public class HeightIntegrityDto
    {
        public HeightIntegritySweepResultDto post_load;
        public HeightIntegritySweepResultDto post_tick;
    }

    /// <summary>Neighbor-stub smoke assertions for new-game runs.</summary>
    [Serializable]
    public class NeighborStubSmokeResultDto
    {
        public int stub_count;
        public int binding_count;
        public int resolver_matches;
        /// <summary>True when stub_count >= 1, binding_count >= 1, resolver_matches == binding_count.</summary>
        public bool assertions_passed;
        public string failure_detail = "";
    }

    /// <summary>Transient state persisted across Play Mode domain reloads.</summary>
    [Serializable]
    public class AgentTestModeBatchStateDto
    {
        public int phase;
        public string save_path = "";
        public string scenario_id = "";
        public string golden_path = "";
        public string city_stats_snapshot_json = "";
        public bool golden_checked;
        public bool golden_matched;
        public string golden_diff = "";
        public int ticks_requested;
        public int ticks_applied;
        public int exit_code;
        public string error = "";
        public string started_utc = "";
        /// <summary>JSON of HeightIntegrityDto; empty until sweep runs.</summary>
        public string height_integrity_json = "";
        /// <summary>JSON of NeighborStubRoundtripGoldenDto projected from actual state.</summary>
        public string neighbor_stubs_snapshot_json = "";
        /// <summary>True when run was started with -testNewGame.</summary>
        public bool new_game_mode;
        /// <summary>Optional master seed for deterministic new-game smoke. 0 = not pinned.</summary>
        public int test_seed;
        /// <summary>JSON of NeighborStubSmokeResultDto; empty on load-path runs.</summary>
        public string neighbor_stub_smoke_json = "";
    }

    /// <summary>Batch run report written to tools/reports/agent-testmode-batch-*.json.</summary>
    [Serializable]
    public class AgentTestModeBatchReportDto
    {
        public int schema_version = 2;
        public string kind = "agent-testmode-batch";
        /// <summary>"load" (default) or "new-game".</summary>
        public string mode = "load";
        public bool ok;
        public string scenario_id = "";
        public string save_path = "";
        public string golden_path = "";
        public bool golden_checked;
        public bool golden_matched;
        public string golden_diff = "";
        public AgentTestModeBatchCitySnapshotDto city_stats;
        public HeightIntegrityDto height_integrity;
        public NeighborStubGoldenEntry[] neighbor_stubs;
        public NeighborBindingGoldenEntry[] neighbor_city_bindings;
        public NeighborStubSmokeResultDto neighbor_stub_smoke;
        public int simulation_ticks_requested;
        public int simulation_ticks_applied;
        public int exit_code;
        public string error = "";
        public string finished_at_utc = "";
    }
}
#endif
