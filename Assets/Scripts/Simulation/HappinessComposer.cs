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
    /// Stage 6 externalizes all formula constants to <see cref="SignalTuningWeightsAsset"/>.
    /// </summary>
    public class HappinessComposer : MonoBehaviour, ISignalConsumer
    {
        [SerializeField] private CityStats cityStats;
        [SerializeField] private EmploymentManager employmentManager;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private ForestManager forestManager;
        [SerializeField] private DistrictManager districtManager;
        [SerializeField] private SignalTuningWeightsAsset weights;

        // Fallback baseline used pre-Awake or when weights asset cannot be resolved.
        private const float FALLBACK_BASELINE = 50f;

        /// <summary>Current converged happiness 0–100. Initialized to <see cref="SignalTuningWeightsAsset.HappinessBaseline"/> in <c>Awake</c>.</summary>
        public float Current { get; private set; } = FALLBACK_BASELINE;

        /// <summary>Read-only accessor to the wired tuning weights asset — read at save time by <c>GameSaveManager</c> to capture the snapshot.</summary>
        public SignalTuningWeightsAsset Weights => weights;

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
            if (weights == null)
            {
                weights = Resources.Load<SignalTuningWeightsAsset>("SignalTuningWeights");
                if (weights == null)
                {
                    Debug.LogError("HappinessComposer.weights unresolved — SignalTuningWeightsAsset missing under Resources/ and no Inspector wiring. ConsumeSignals will no-op.");
                }
            }
            if (weights != null)
            {
                Current = weights.HappinessBaseline;
            }
        }

        /// <summary>Read pollution rollup from district cache + manager state to lerp <see cref="Current"/> toward target.</summary>
        public void ConsumeSignals(SignalFieldRegistry registry, DistrictSignalCache cache)
        {
            if (weights == null)
            {
                return;
            }

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
                if (maxTax > weights.ComfortableTaxRate)
                {
                    taxFactor = -Mathf.Clamp01((maxTax - weights.ComfortableTaxRate) /
                                (weights.MaxTaxRateForScale - weights.ComfortableTaxRate));
                }
            }

            float serviceFactor = Mathf.Clamp01(weights.ServiceCoverageStub);

            float forestFactor = 0f;
            if (cityStats != null)
            {
                forestFactor = Mathf.Clamp01(cityStats.GetForestHappinessBonus() / weights.MaxForestBonus);
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

            float targetHappiness = weights.HappinessBaseline
                + employmentFactor * weights.WeightEmployment
                + taxFactor * weights.WeightTax
                + serviceFactor * weights.WeightServices
                + forestFactor * weights.WeightForest
                + developmentFactor * weights.WeightDev
                - pollutionFactor * weights.WeightPollution;

            targetHappiness = Mathf.Clamp(targetHappiness, 0f, 100f);

            int population = cityStats != null ? cityStats.population : 0;
            float convergenceRate = weights.BaseConvergenceRate / (1f + population / weights.PopulationScaleFactor);
            Current = Mathf.Lerp(Current, targetHappiness, convergenceRate);
        }

        /// <summary>Inner-ring district mean of <see cref="SimulationSignal.PollutionAir"/> divided by <see cref="SignalTuningWeightsAsset.PollutionCap"/>; NaN/Infinity → 0.</summary>
        private float ReadPollutionFactor(DistrictSignalCache cache)
        {
            if (cache == null || weights == null)
            {
                return 0f;
            }
            float raw = cache.Get(0, SimulationSignal.PollutionAir);
            if (float.IsNaN(raw) || float.IsInfinity(raw))
            {
                return 0f;
            }
            float factor = raw / weights.PollutionCap;
            if (float.IsNaN(factor) || float.IsInfinity(factor))
            {
                return 0f;
            }
            return Mathf.Clamp01(factor);
        }
    }
}
