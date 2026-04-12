using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Territory.Economy;
using Territory.Integration;
using Territory.Testing;
using UnityEngine;

namespace Territory.Simulation
{
    /// <summary>
    /// Optional per-sim-tick snapshots of aggregate city metrics → Postgres via
    /// <c>tools/postgres-ia/insert-city-metrics.mjs</c>. Fire-and-forget; never blocks gameplay.
    /// </summary>
    public sealed class MetricsRecorder : MonoBehaviour
    {
        const int MaxPendingBridgeWrites = 64;
        const int BridgeWriteTimeoutMs = 45000;

        [SerializeField] private CityStats _cityStats;
        [SerializeField] private DemandManager _demandManager;
        [SerializeField] private EmploymentManager _employmentManager;

        int _simulationTickSequence;
        static int _pendingWrites;

        void Awake()
        {
            if (_cityStats == null)
                _cityStats = FindObjectOfType<CityStats>();
            if (_demandManager == null)
                _demandManager = FindObjectOfType<DemandManager>();
            if (_employmentManager == null)
                _employmentManager = FindObjectOfType<EmploymentManager>();
        }

        /// <summary>
        /// Call from <see cref="SimulationManager.ProcessSimulationTick"/> after tick logic (e.g. in <c>finally</c>).
        /// No-op if <see cref="CityStats"/> missing or no DB URL configured.
        /// </summary>
        public void RecordAfterSimulationTick()
        {
            if (_cityStats == null)
                return;

            string repoRoot = ScenarioPathResolver.GetRepositoryRoot();
            string dbUrl = RuntimePostgresEnv.TryGetDatabaseUrl(repoRoot);
            if (string.IsNullOrEmpty(dbUrl))
                return;

            int tick = Interlocked.Increment(ref _simulationTickSequence);
            var payload = BuildPayload(tick);

            if (Interlocked.Increment(ref _pendingWrites) > MaxPendingBridgeWrites)
            {
                Interlocked.Decrement(ref _pendingWrites);
                return;
            }

            string json = JsonUtility.ToJson(payload);
            ThreadPool.QueueUserWorkItem(_ => RunInsertBridge(repoRoot, dbUrl, json));
        }

        CityMetricsInsertPayload BuildPayload(int tickIndex)
        {
            float dR = _demandManager != null && _demandManager.residentialDemand != null
                ? _demandManager.residentialDemand.demandLevel
                : 0f;
            float dC = _demandManager != null && _demandManager.commercialDemand != null
                ? _demandManager.commercialDemand.demandLevel
                : 0f;
            float dI = _demandManager != null && _demandManager.industrialDemand != null
                ? _demandManager.industrialDemand.demandLevel
                : 0f;

            float employmentPct = _employmentManager != null ? _employmentManager.GetEmploymentRate() : 0f;
            float employment01 = Mathf.Clamp01(employmentPct / 100f);

            float forestPct = _cityStats.GetForestCoveragePercentage();
            float forest01 = Mathf.Clamp01(forestPct / 100f);

            string scenarioId = TestModeSessionState.ActiveThisSession ? TestModeSessionState.ScenarioId : null;

            return new CityMetricsInsertPayload
            {
                simulation_tick_index = tickIndex,
                game_date = _cityStats.currentDate.ToString("yyyy-MM-dd"),
                population = _cityStats.population,
                money = _cityStats.money,
                happiness = _cityStats.happiness,
                demand_r = dR,
                demand_c = dC,
                demand_i = dI,
                employment_rate = employment01,
                forest_coverage = forest01,
                scenario_id = string.IsNullOrEmpty(scenarioId) ? string.Empty : scenarioId,
            };
        }

        static void RunInsertBridge(string repoRoot, string dbUrl, string jsonUtf8)
        {
            string stagingDir = Path.Combine(Path.GetTempPath(), "TerritoryCityMetricsStaging");
            string stagingAbs = Path.Combine(stagingDir, $"metrics-{Guid.NewGuid():N}.json");
            try
            {
                Directory.CreateDirectory(stagingDir);
                File.WriteAllText(stagingAbs, jsonUtf8, new UTF8Encoding(false));

                string scriptPath = Path.Combine(repoRoot, "tools", "postgres-ia", "insert-city-metrics.mjs");
                if (!File.Exists(scriptPath))
                    return;

                string nodeExe = RuntimePostgresEnv.ResolveNodeExecutablePath();
                string arguments =
                    $"\"{scriptPath}\" --payload-file \"{stagingAbs.Replace('\\', '/')}\"";

                var psi = new ProcessStartInfo
                {
                    FileName = nodeExe,
                    Arguments = arguments,
                    WorkingDirectory = repoRoot,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                psi.EnvironmentVariables["DATABASE_URL"] = dbUrl;

                using var proc = Process.Start(psi);
                if (proc == null)
                    return;

                if (!proc.WaitForExit(BridgeWriteTimeoutMs))
                {
                    try
                    {
                        proc.Kill();
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            catch
            {
                // Fire-and-forget: never surface to gameplay.
            }
            finally
            {
                try
                {
                    if (File.Exists(stagingAbs))
                        File.Delete(stagingAbs);
                }
                catch
                {
                    // ignored
                }

                Interlocked.Decrement(ref _pendingWrites);
            }
        }

        [Serializable]
        sealed class CityMetricsInsertPayload
        {
            public int simulation_tick_index;
            public string game_date;
            public int population;
            public int money;
            public float happiness;
            public float demand_r;
            public float demand_c;
            public float demand_i;
            public float employment_rate;
            public float forest_coverage;
            public string scenario_id;
        }
    }
}
