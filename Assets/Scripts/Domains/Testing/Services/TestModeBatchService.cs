#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using Domains.Testing.Dto;
using Territory.Testing;
using UnityEngine;

namespace Domains.Testing.Services
{
    /// <summary>
    /// Stage 6.2: command-line arg parsing + report-writing facade for AgentTestModeBatchRunner.
    /// Holds logic that does not reference Game assembly types (CityStats, GridManager, etc.).
    /// </summary>
    public static class TestModeBatchService
    {
        // ── Command-line arg parsers ────────────────────────────────────────────

        /// <summary>Parse -testSimulationTicks N from args; clamped [0,10000].</summary>
        public static int ParseSimulationTicks(string[] args)
        {
            if (args == null || args.Length == 0) return 0;
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-testSimulationTicks" && i + 1 < args.Length)
                    if (int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                        return UnityEngine.Mathf.Clamp(n, 0, 10_000);
            return 0;
        }

        /// <summary>Parse -testGoldenPath path from args; null when absent.</summary>
        public static string ParseTestGoldenPath(string[] args)
        {
            if (args == null || args.Length == 0) return null;
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-testGoldenPath" && i + 1 < args.Length)
                    return args[i + 1]?.Trim();
            return null;
        }

        /// <summary>True if -testNewGame flag present.</summary>
        public static bool ParseNewGameFlag(string[] args)
        {
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-testNewGame") return true;
            return false;
        }

        /// <summary>Parse -testSeed N from args; 0 when absent.</summary>
        public static int ParseTestSeed(string[] args)
        {
            if (args == null) return 0;
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-testSeed")
                    if (int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed))
                        return seed;
            return 0;
        }

        // ── Report writing ──────────────────────────────────────────────────────

        /// <summary>Write JSON report from full batch state.</summary>
        public static void TryWriteReportFromState(string repoRoot, AgentTestModeBatchStateDto state, bool ok, string error)
        {
            int exitCode = state.exit_code != 0 ? state.exit_code : (ok ? 0 : 6);
            TryWriteReportBody(repoRoot, ok, error, exitCode, state.save_path, state.scenario_id,
                state.ticks_requested, state.ticks_applied, state);
        }

        /// <summary>Write minimal JSON report; reads cmdline args for scenario context.</summary>
        public static void TryWriteReportImmediate(string repoRoot, bool ok, string error, int exitCode)
        {
            string savePath = string.Empty;
            string scenarioId = string.Empty;
            if (TestModeCommandLineBootstrap.TryParse(Environment.GetCommandLineArgs(), out string sid, out string sp))
            {
                savePath = sp ?? string.Empty;
                scenarioId = sid ?? string.Empty;
            }
            int ticksRequested = ParseSimulationTicks(Environment.GetCommandLineArgs());
            TryWriteReportBody(repoRoot, ok, error, exitCode, savePath, scenarioId, ticksRequested, 0, null);
        }

        /// <summary>Write full JSON report under tools/reports — composes DTO from state.</summary>
        public static void TryWriteReportBody(
            string repoRoot,
            bool ok,
            string error,
            int exitCode,
            string savePath,
            string scenarioId,
            int ticksRequested,
            int ticksApplied,
            AgentTestModeBatchStateDto extraState)
        {
            try
            {
                string reportsDir = Path.Combine(repoRoot, "tools", "reports");
                Directory.CreateDirectory(reportsDir);
                string fileName = $"agent-testmode-batch-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
                string fullPath = Path.Combine(reportsDir, fileName);

                AgentTestModeBatchCitySnapshotDto cityStatsDto = null;
                string goldenPath = string.Empty;
                bool goldenChecked = false;
                bool goldenMatched = false;
                string goldenDiff = string.Empty;
                HeightIntegrityDto hiDto = null;
                NeighborStubGoldenEntry[] neighborStubsArr = null;
                NeighborBindingGoldenEntry[] neighborBindingsArr = null;
                NeighborStubSmokeResultDto smokeDto = null;
                bool isNewGameMode = false;

                if (extraState != null)
                {
                    goldenPath    = extraState.golden_path ?? string.Empty;
                    goldenChecked = extraState.golden_checked;
                    goldenMatched = extraState.golden_matched;
                    goldenDiff    = extraState.golden_diff ?? string.Empty;
                    isNewGameMode = extraState.new_game_mode;
                    if (!string.IsNullOrEmpty(extraState.city_stats_snapshot_json))
                        cityStatsDto = JsonUtility.FromJson<AgentTestModeBatchCitySnapshotDto>(extraState.city_stats_snapshot_json);
                    if (!string.IsNullOrEmpty(extraState.height_integrity_json))
                        hiDto = JsonUtility.FromJson<HeightIntegrityDto>(extraState.height_integrity_json);
                    if (!string.IsNullOrEmpty(extraState.neighbor_stubs_snapshot_json))
                    {
                        var nSnap = JsonUtility.FromJson<NeighborStubRoundtripGoldenDto>(extraState.neighbor_stubs_snapshot_json);
                        if (nSnap != null) { neighborStubsArr = nSnap.neighborStubs; neighborBindingsArr = nSnap.neighborCityBindings; }
                    }
                    if (!string.IsNullOrEmpty(extraState.neighbor_stub_smoke_json))
                        smokeDto = JsonUtility.FromJson<NeighborStubSmokeResultDto>(extraState.neighbor_stub_smoke_json);
                }

                var dto = new AgentTestModeBatchReportDto
                {
                    mode                       = isNewGameMode ? "new-game" : "load",
                    ok                         = ok,
                    scenario_id                = scenarioId ?? string.Empty,
                    save_path                  = savePath ?? string.Empty,
                    golden_path                = goldenPath,
                    golden_checked             = goldenChecked,
                    golden_matched             = goldenMatched,
                    golden_diff                = goldenDiff,
                    city_stats                 = cityStatsDto,
                    height_integrity           = hiDto,
                    neighbor_stubs             = neighborStubsArr,
                    neighbor_city_bindings     = neighborBindingsArr,
                    neighbor_stub_smoke        = smokeDto,
                    simulation_ticks_requested = ticksRequested,
                    simulation_ticks_applied   = ticksApplied,
                    exit_code                  = exitCode,
                    error                      = error ?? string.Empty,
                    finished_at_utc            = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                };

                File.WriteAllText(fullPath, JsonUtility.ToJson(dto, prettyPrint: true));
                Debug.Log($"[AgentTestModeBatch] Report written: {fullPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AgentTestModeBatch] Failed to write report: {ex.Message}");
            }
        }
    }
}
#endif
