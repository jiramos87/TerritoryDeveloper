#if UNITY_EDITOR

namespace Domains.Testing.Services
{
    /// <summary>
    /// City-stats and neighbor-stub snapshot projection service.
    /// Stage 13 tracer slice — establishes namespace + method surface.
    /// Full extraction (BuildCitySnapshot, BuildNeighborStubSnapshot from AgentTestModeBatchRunner)
    /// deferred: requires TerritoryDeveloper.Game assembly reference (CityStats, GameSaveManager, GridManager).
    /// Guardrail: snapshot logic preserved in AgentTestModeBatchRunner until asmdef dep chain allows move.
    /// </summary>
    public static class BatchSnapshotService
    {
        /// <summary>Schema version written into city-stats snapshots.</summary>
        public const int CitySnapshotSchemaVersion = 2;

        /// <summary>
        /// Returns the city snapshot schema version supported by this service.
        /// Tracer anchor: BatchSnapshotService.CitySnapshotSchemaVersion == 2.
        /// </summary>
        public static int GetCitySnapshotSchemaVersion() => CitySnapshotSchemaVersion;
    }
}
#endif
