using Territory.Core;
using Territory.Economy;
using Territory.Forests;
using Territory.Simulation.Signals;
using UnityEngine;

namespace Territory.Simulation
{
    /// <summary>
    /// Stage 4 happiness consumer over the signal layer. Mirrors legacy
    /// <see cref="CityStats"/> <c>ComputeTargetHappiness</c> formula but pulls the
    /// pollution factor from the Inner-ring district mean of <see cref="SimulationSignal.PollutionAir"/>
    /// via <see cref="DistrictSignalCache"/>. Lerps target into <see cref="Current"/> per tick.
    /// </summary>
    public class HappinessComposer : MonoBehaviour, ISignalConsumer
    {
        [SerializeField] private CityStats cityStats;
        [SerializeField] private EmploymentManager employmentManager;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private ForestManager forestManager;
        [SerializeField] private DistrictManager districtManager;

        // Happiness weights — ported verbatim from CityStats.cs lines 850-854.
        private const float HAPPINESS_BASELINE = 50f;
        private const float WEIGHT_EMPLOYMENT = 30f;
        private const float WEIGHT_SERVICES = 20f;
        private const float WEIGHT_FOREST = 10f;
        private const float WEIGHT_POLLUTION = 10f;

        // Tax + development weights — ported from CityStats.cs lines 858, 860.
        private const float WEIGHT_TAX = 27f;
        private const float WEIGHT_DEV = 12f;

        // Convergence — ported from CityStats.cs lines 866-867.
        private const float BASE_CONVERGENCE_RATE = 0.15f;
        private const float POPULATION_SCALE_FACTOR = 500f;

        // Pollution + tax bands — ported from CityStats.cs lines 874-875, 886.
        private const float COMFORTABLE_TAX_RATE = 10f;
        private const float MAX_TAX_RATE_FOR_SCALE = 50f;
        private const float POLLUTION_CAP = 200f;

        // Forest normalization — ported from CityStats.cs line 878.
        private const float MAX_FOREST_BONUS = 60f;

        // Service stub — matches CityStats.happinessServiceCoverageStub default.
        private const float SERVICE_COVERAGE_STUB = 0.4f;

        /// <summary>Current converged happiness 0–100. Initialized to <see cref="HAPPINESS_BASELINE"/>.</summary>
        public float Current { get; private set; } = HAPPINESS_BASELINE;

        private void Awake()
        {
            if (cityStats == null)
            {
                cityStats = FindObjectOfType<CityStats>();
            }
            if (employmentManager == null)
            {
                employmentManager = FindObjectOfType<EmploymentManager>();
            }
            if (economyManager == null)
            {
                economyManager = FindObjectOfType<EconomyManager>();
            }
            if (forestManager == null)
            {
                forestManager = FindObjectOfType<ForestManager>();
            }
            if (districtManager == null)
            {
                districtManager = FindObjectOfType<DistrictManager>();
            }
        }

        /// <summary>Read pollution rollup from district cache + manager state to lerp <see cref="Current"/> toward target.</summary>
        public void ConsumeSignals(SignalFieldRegistry registry, DistrictSignalCache cache)
        {
            float pollutionFactor = ReadPollutionFactor(cache);

            float employmentFactor = 0.5f;
            if (employmentManager != null)
            {
                employmentFactor = employmentManager.GetEmploymentRate() / 100f;
            }

            float taxFactor = 0f;
            if (economyManager != null)
            {
                float maxTax = Mathf.Max(
                    economyManager.residentialIncomeTax,
                    Mathf.Max(economyManager.commercialIncomeTax, economyManager.industrialIncomeTax));
                if (maxTax > COMFORTABLE_TAX_RATE)
                {
                    taxFactor = -Mathf.Clamp01((maxTax - COMFORTABLE_TAX_RATE) /
                                (MAX_TAX_RATE_FOR_SCALE - COMFORTABLE_TAX_RATE));
                }
            }

            float serviceFactor = Mathf.Clamp01(SERVICE_COVERAGE_STUB);

            float forestFactor = 0f;
            if (cityStats != null)
            {
                forestFactor = Mathf.Clamp01(cityStats.GetForestHappinessBonus() / MAX_FOREST_BONUS);
            }

            float developmentFactor = 0f;
            if (cityStats != null)
            {
                int totalBuildings = cityStats.residentialBuildingCount
                                   + cityStats.commercialBuildingCount
                                   + cityStats.industrialBuildingCount;
                int totalZoned = cityStats.residentialZoneCount
                               + cityStats.commercialZoneCount
                               + cityStats.industrialZoneCount;
                developmentFactor = totalZoned > 0
                    ? Mathf.Clamp01((float)totalBuildings / totalZoned)
                    : 0f;
            }

            float targetHappiness = HAPPINESS_BASELINE
                + employmentFactor * WEIGHT_EMPLOYMENT
                + taxFactor * WEIGHT_TAX
                + serviceFactor * WEIGHT_SERVICES
                + forestFactor * WEIGHT_FOREST
                + developmentFactor * WEIGHT_DEV
                - pollutionFactor * WEIGHT_POLLUTION;

            targetHappiness = Mathf.Clamp(targetHappiness, 0f, 100f);

            int population = cityStats != null ? cityStats.population : 0;
            float convergenceRate = BASE_CONVERGENCE_RATE / (1f + population / POPULATION_SCALE_FACTOR);
            Current = Mathf.Lerp(Current, targetHappiness, convergenceRate);
        }

        /// <summary>Inner-ring district mean of <see cref="SimulationSignal.PollutionAir"/> divided by <see cref="POLLUTION_CAP"/>; NaN/Infinity → 0.</summary>
        private float ReadPollutionFactor(DistrictSignalCache cache)
        {
            if (cache == null)
            {
                return 0f;
            }
            float raw = cache.Get(0, SimulationSignal.PollutionAir);
            if (float.IsNaN(raw) || float.IsInfinity(raw))
            {
                return 0f;
            }
            float factor = raw / POLLUTION_CAP;
            if (float.IsNaN(factor) || float.IsInfinity(factor))
            {
                return 0f;
            }
            return Mathf.Clamp01(factor);
        }
    }
}
