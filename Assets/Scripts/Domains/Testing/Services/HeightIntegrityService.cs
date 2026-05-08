#if UNITY_EDITOR

namespace Domains.Testing.Services
{
    /// <summary>
    /// HeightMap vs CityCell.height integrity sweep service.
    /// Stage 13 tracer slice — establishes namespace + method surface.
    /// Full extraction (sweep loop from AgentTestModeBatchRunner) deferred: requires
    /// TerritoryDeveloper.Game assembly reference (GridManager, HeightMap, CityCell).
    /// Guardrail: sweep behavior preserved in AgentTestModeBatchRunner.HeightIntegritySweep
    /// until asmdef dependency chain allows inline move.
    /// </summary>
    public static class HeightIntegrityService
    {
        /// <summary>Max violation rows captured per sweep pass.</summary>
        public const int MaxOffenders = 10;

        /// <summary>
        /// Returns the max number of violation rows captured per sweep pass.
        /// Tracer anchor: HeightIntegrityService.MaxOffenderCount == 10.
        /// </summary>
        public static int MaxOffenderCount => MaxOffenders;
    }
}
#endif
