using UnityEngine;

namespace Territory.Simulation.Signals
{
    /// <summary>
    /// Stage 6 tuning surface for <see cref="HappinessComposer"/> + <see cref="DesirabilityComposer"/>.
    /// Externalizes the formula constants previously baked as <c>private const float</c> into a
    /// single <see cref="ScriptableObject"/> so designers can hot-edit without recompile, and so
    /// save-load can persist the exact tuning that produced a save (see <c>GameSaveManager</c>
    /// schema 6, <c>SignalTuningWeightsData</c>). Defaults are bit-identical to the previous
    /// const literals to preserve <c>HappinessComposerParityTest</c> + <c>DesirabilityComposerParityTest</c>
    /// parity bands.
    /// </summary>
    [CreateAssetMenu(fileName = "SignalTuningWeights", menuName = "Territory/Signal Tuning Weights")]
    public class SignalTuningWeightsAsset : ScriptableObject
    {
        // --- Happiness side (ported verbatim from CityStats.cs lines 850-867 / 874-878 / 886) ---
        [SerializeField] private float happinessBaseline = 50f;
        [SerializeField] private float weightEmployment = 30f;
        [SerializeField] private float weightServices = 20f;
        [SerializeField] private float weightForest = 10f;
        [SerializeField] private float weightPollution = 10f;
        [SerializeField] private float weightTax = 27f;
        [SerializeField] private float weightDev = 12f;
        [SerializeField] private float baseConvergenceRate = 0.15f;
        [SerializeField] private float populationScaleFactor = 500f;
        [SerializeField] private float comfortableTaxRate = 10f;
        [SerializeField] private float maxTaxRateForScale = 50f;
        [SerializeField] private float pollutionCap = 200f;
        [SerializeField] private float maxForestBonus = 60f;
        [SerializeField] private float serviceCoverageStub = 0.4f;

        // --- Desirability side (ported from DesirabilityComposer.cs lines 23-25) ---
        [SerializeField] private float parksBonus = 0.3f;
        [SerializeField] private float pollutionPenalty = 0.5f;
        [SerializeField] private float normalizationCap = 100f;

        // --- Stage 7 — PollutionLand producer tier weights (TECH-1889). ---
        [SerializeField] private float pollutionLandHeavy = 2.5f;
        [SerializeField] private float pollutionLandMedium = 1.5f;
        [SerializeField] private float pollutionLandLight = 0.5f;

        // --- Stage 7 — PollutionWater producer tier weights (TECH-1890). Heavier than land — water spillover physics. ---
        [SerializeField] private float pollutionWaterHeavy = 3.0f;
        [SerializeField] private float pollutionWaterMedium = 1.5f;
        [SerializeField] private float pollutionWaterLight = 0.5f;

        // --- Stage 7 — LandValue composite producer (TECH-1891). ---
        [SerializeField] private float landValueBase = 10f;
        [SerializeField] private float landValueParkBonus = 2.0f;
        [SerializeField] private float landValueIndustrialPenalty = 1.5f;
        [SerializeField] private float landValueDensityBonus = 1.0f;

        // --- Stage 7 — EconomyManager tax-base land-value bonus (TECH-1892). At LandValue mean 100 → +50% income. ---
        [SerializeField] private float landValueIncomeMultiplier = 0.005f;

        // --- Stage 8 — CrimeSystem tuning fields (TECH-1953). Five new fields appended contiguously per stage-authoring sizing-gate H6. ---
        [SerializeField] private float crimeBase = 1f;
        [SerializeField] private float crimeDensityWeight = 2f;
        [SerializeField] private float servicePoliceCoverage = 5f;
        [SerializeField] private float servicePoliceConsumerScale = 0.4f;
        [SerializeField] private float crimeHotspotThreshold = 15f;

        // --- Stage 9.A — Service{Fire,Education,Health} tuning fields (TECH-2079). Six new fields appended contiguously per sizing-gate H6 (single SO edit per Stage). ---
        [SerializeField] private float serviceFireCoverage = 5f;
        [SerializeField] private float serviceFireConsumerScale = 0.4f;
        [SerializeField] private float serviceEducationCoverage = 5f;
        [SerializeField] private float serviceEducationConsumerScale = 0.4f;
        [SerializeField] private float serviceHealthCoverage = 5f;
        [SerializeField] private float serviceHealthConsumerScale = 0.4f;

        // --- Stage 9.B — TrafficProducer + TrafficLevelConsumer tuning fields (TECH-2136). Three new fields appended contiguously per sizing-gate H6 (single SO edit per Stage). ---
        [SerializeField] private float trafficBase = 0.5f;
        [SerializeField] private float trafficRoadwayDensityWeight = 1.0f;
        [SerializeField] private float trafficLevelConsumerScale = 0.3f;

        /// <summary>Default happiness baseline (50f) — initial value of <see cref="HappinessComposer.Current"/>.</summary>
        public float HappinessBaseline => happinessBaseline;
        /// <summary>Employment weight (30f).</summary>
        public float WeightEmployment => weightEmployment;
        /// <summary>Services weight (20f).</summary>
        public float WeightServices => weightServices;
        /// <summary>Forest weight (10f).</summary>
        public float WeightForest => weightForest;
        /// <summary>Pollution weight (10f).</summary>
        public float WeightPollution => weightPollution;
        /// <summary>Tax weight (27f).</summary>
        public float WeightTax => weightTax;
        /// <summary>Development weight (12f).</summary>
        public float WeightDev => weightDev;
        /// <summary>Base convergence rate (0.15f).</summary>
        public float BaseConvergenceRate => baseConvergenceRate;
        /// <summary>Population scale factor (500f) — convergence dampens as population grows.</summary>
        public float PopulationScaleFactor => populationScaleFactor;
        /// <summary>Comfortable tax rate threshold (10f) — taxes above this incur happiness penalty.</summary>
        public float ComfortableTaxRate => comfortableTaxRate;
        /// <summary>Max tax rate clamp (50f) — penalty saturates here.</summary>
        public float MaxTaxRateForScale => maxTaxRateForScale;
        /// <summary>Pollution cap (200f) — pollution factor normalized by this.</summary>
        public float PollutionCap => pollutionCap;
        /// <summary>Max forest happiness bonus (60f) — forest factor normalized by this.</summary>
        public float MaxForestBonus => maxForestBonus;
        /// <summary>Service coverage stub (0.4f) — placeholder until service coverage signal lands.</summary>
        public float ServiceCoverageStub => serviceCoverageStub;

        /// <summary>Parks bonus multiplier (0.3f) for <see cref="DesirabilityComposer"/>.</summary>
        public float ParksBonus => parksBonus;
        /// <summary>Pollution penalty multiplier (0.5f) for <see cref="DesirabilityComposer"/>.</summary>
        public float PollutionPenalty => pollutionPenalty;
        /// <summary>Desirability normalization cap (100f).</summary>
        public float NormalizationCap => normalizationCap;

        /// <summary>Stage 7 — IndustrialHeavyBuilding land-pollution emit weight (2.5f).</summary>
        public float PollutionLandHeavy => pollutionLandHeavy;
        /// <summary>Stage 7 — IndustrialMediumBuilding land-pollution emit weight (1.5f).</summary>
        public float PollutionLandMedium => pollutionLandMedium;
        /// <summary>Stage 7 — IndustrialLightBuilding land-pollution emit weight (0.5f).</summary>
        public float PollutionLandLight => pollutionLandLight;

        /// <summary>Stage 7 — IndustrialHeavyBuilding water-pollution emit weight (3.0f) when Moore-adjacent to open water.</summary>
        public float PollutionWaterHeavy => pollutionWaterHeavy;
        /// <summary>Stage 7 — IndustrialMediumBuilding water-pollution emit weight (1.5f) when Moore-adjacent to open water.</summary>
        public float PollutionWaterMedium => pollutionWaterMedium;
        /// <summary>Stage 7 — IndustrialLightBuilding water-pollution emit weight (0.5f) when Moore-adjacent to open water.</summary>
        public float PollutionWaterLight => pollutionWaterLight;

        /// <summary>Stage 7 — LandValue composite baseline (10f) — emitted at every cell.</summary>
        public float LandValueBase => landValueBase;
        /// <summary>Stage 7 — LandValue per-Moore-neighbor park bonus multiplier (2.0f).</summary>
        public float LandValueParkBonus => landValueParkBonus;
        /// <summary>Stage 7 — LandValue per-Moore-neighbor industrial penalty multiplier (1.5f).</summary>
        public float LandValueIndustrialPenalty => landValueIndustrialPenalty;
        /// <summary>Stage 7 — LandValue per-residential-density-tier bonus multiplier (1.0f).</summary>
        public float LandValueDensityBonus => landValueDensityBonus;

        /// <summary>Stage 7 — EconomyManager projected-income land-value bonus multiplier (0.005f). At cityLandValueMean=100 → +50% bonus.</summary>
        public float LandValueIncomeMultiplier => landValueIncomeMultiplier;

        /// <summary>Stage 8 — <see cref="Producers.CrimeProducer"/> baseline crime emitted at every cell (1.0f).</summary>
        public float CrimeBase => crimeBase;
        /// <summary>Stage 8 — <see cref="Producers.CrimeProducer"/> per-residential-density-tier weight (2.0f); ResidentialHeavy contributes 3× this.</summary>
        public float CrimeDensityWeight => crimeDensityWeight;
        /// <summary>Stage 8 — <c>ServicePoliceProducer</c> coverage value emitted at police-equipped state-service cells (5.0f).</summary>
        public float ServicePoliceCoverage => servicePoliceCoverage;
        /// <summary>Stage 8 — <c>ServicePoliceConsumer</c> per-cell crime-reduction multiplier (0.4f); applied as <c>-scale * policeField.Get(x,y)</c>.</summary>
        public float ServicePoliceConsumerScale => servicePoliceConsumerScale;
        /// <summary>Stage 8 — <c>CrimeHotspotEventEmitter</c> P90 threshold per district (15.0f); strict <c>&gt;</c> emits.</summary>
        public float CrimeHotspotThreshold => crimeHotspotThreshold;

        /// <summary>Stage 9.A — <c>ServiceFireProducer</c> coverage value emitted at fire-equipped state-service cells (5.0f).</summary>
        public float ServiceFireCoverage => serviceFireCoverage;
        /// <summary>Stage 9.A — <c>ServiceFireConsumer</c> per-cell scale (0.4f); reserved for downstream demand-model wiring (Bucket 3).</summary>
        public float ServiceFireConsumerScale => serviceFireConsumerScale;
        /// <summary>Stage 9.A — <c>ServiceEducationProducer</c> coverage value emitted at education-equipped state-service cells (5.0f).</summary>
        public float ServiceEducationCoverage => serviceEducationCoverage;
        /// <summary>Stage 9.A — <c>ServiceEducationConsumer</c> per-cell scale (0.4f); reserved for downstream demand-model wiring (Bucket 3).</summary>
        public float ServiceEducationConsumerScale => serviceEducationConsumerScale;
        /// <summary>Stage 9.A — <c>ServiceHealthProducer</c> coverage value emitted at health-equipped state-service cells (5.0f).</summary>
        public float ServiceHealthCoverage => serviceHealthCoverage;
        /// <summary>Stage 9.A — <c>ServiceHealthConsumer</c> per-cell scale (0.4f); reserved for downstream demand-model wiring (Bucket 3).</summary>
        public float ServiceHealthConsumerScale => serviceHealthConsumerScale;

        /// <summary>Stage 9.B — <c>TrafficProducer</c> baseline traffic emitted at every road cell (0.5f).</summary>
        public float TrafficBase => trafficBase;
        /// <summary>Stage 9.B — <c>TrafficProducer</c> per-Moore-neighbor RCI density weight (1.0f); per-road-cell value = <c>TrafficBase + TrafficRoadwayDensityWeight × MooreNeighborRciCount</c>.</summary>
        public float TrafficRoadwayDensityWeight => trafficRoadwayDensityWeight;
        /// <summary>Stage 9.B — <c>TrafficLevelConsumer</c> per-cell scale (0.3f); reserved for downstream demand-model wiring (Bucket 3 RCI demand parity).</summary>
        public float TrafficLevelConsumerScale => trafficLevelConsumerScale;

        /// <summary>Capture current field state into a serializable snapshot for <c>GameSaveData.tuningWeights</c> (schema 6). Stage 7 fields additive — older saves without them round-trip via JsonUtility default-zero on missing JSON keys; restore path then reapplies asset-default during <see cref="RestoreFromData"/> when zero (see Stage 7 round-trip semantics).</summary>
        public SignalTuningWeightsData CaptureSnapshot()
        {
            return new SignalTuningWeightsData
            {
                happinessBaseline = happinessBaseline,
                weightEmployment = weightEmployment,
                weightServices = weightServices,
                weightForest = weightForest,
                weightPollution = weightPollution,
                weightTax = weightTax,
                weightDev = weightDev,
                baseConvergenceRate = baseConvergenceRate,
                populationScaleFactor = populationScaleFactor,
                comfortableTaxRate = comfortableTaxRate,
                maxTaxRateForScale = maxTaxRateForScale,
                pollutionCap = pollutionCap,
                maxForestBonus = maxForestBonus,
                serviceCoverageStub = serviceCoverageStub,
                parksBonus = parksBonus,
                pollutionPenalty = pollutionPenalty,
                normalizationCap = normalizationCap,
                pollutionLandHeavy = pollutionLandHeavy,
                pollutionLandMedium = pollutionLandMedium,
                pollutionLandLight = pollutionLandLight,
                pollutionWaterHeavy = pollutionWaterHeavy,
                pollutionWaterMedium = pollutionWaterMedium,
                pollutionWaterLight = pollutionWaterLight,
                landValueBase = landValueBase,
                landValueParkBonus = landValueParkBonus,
                landValueIndustrialPenalty = landValueIndustrialPenalty,
                landValueDensityBonus = landValueDensityBonus,
                landValueIncomeMultiplier = landValueIncomeMultiplier,
                crimeBase = crimeBase,
                crimeDensityWeight = crimeDensityWeight,
                servicePoliceCoverage = servicePoliceCoverage,
                servicePoliceConsumerScale = servicePoliceConsumerScale,
                crimeHotspotThreshold = crimeHotspotThreshold,
                serviceFireCoverage = serviceFireCoverage,
                serviceFireConsumerScale = serviceFireConsumerScale,
                serviceEducationCoverage = serviceEducationCoverage,
                serviceEducationConsumerScale = serviceEducationConsumerScale,
                serviceHealthCoverage = serviceHealthCoverage,
                serviceHealthConsumerScale = serviceHealthConsumerScale,
                trafficBase = trafficBase,
                trafficRoadwayDensityWeight = trafficRoadwayDensityWeight,
                trafficLevelConsumerScale = trafficLevelConsumerScale,
            };
        }

        /// <summary>Restore field values from a saved snapshot. Null payload → no-op (asset defaults preserved).</summary>
        public void RestoreFromData(SignalTuningWeightsData data)
        {
            if (data == null)
            {
                return;
            }
            happinessBaseline = data.happinessBaseline;
            weightEmployment = data.weightEmployment;
            weightServices = data.weightServices;
            weightForest = data.weightForest;
            weightPollution = data.weightPollution;
            weightTax = data.weightTax;
            weightDev = data.weightDev;
            baseConvergenceRate = data.baseConvergenceRate;
            populationScaleFactor = data.populationScaleFactor;
            comfortableTaxRate = data.comfortableTaxRate;
            maxTaxRateForScale = data.maxTaxRateForScale;
            pollutionCap = data.pollutionCap;
            maxForestBonus = data.maxForestBonus;
            serviceCoverageStub = data.serviceCoverageStub;
            parksBonus = data.parksBonus;
            pollutionPenalty = data.pollutionPenalty;
            normalizationCap = data.normalizationCap;
            pollutionLandHeavy = data.pollutionLandHeavy;
            pollutionLandMedium = data.pollutionLandMedium;
            pollutionLandLight = data.pollutionLandLight;
            pollutionWaterHeavy = data.pollutionWaterHeavy;
            pollutionWaterMedium = data.pollutionWaterMedium;
            pollutionWaterLight = data.pollutionWaterLight;
            landValueBase = data.landValueBase;
            landValueParkBonus = data.landValueParkBonus;
            landValueIndustrialPenalty = data.landValueIndustrialPenalty;
            landValueDensityBonus = data.landValueDensityBonus;
            landValueIncomeMultiplier = data.landValueIncomeMultiplier;
            crimeBase = data.crimeBase;
            crimeDensityWeight = data.crimeDensityWeight;
            servicePoliceCoverage = data.servicePoliceCoverage;
            servicePoliceConsumerScale = data.servicePoliceConsumerScale;
            crimeHotspotThreshold = data.crimeHotspotThreshold;
            serviceFireCoverage = data.serviceFireCoverage;
            serviceFireConsumerScale = data.serviceFireConsumerScale;
            serviceEducationCoverage = data.serviceEducationCoverage;
            serviceEducationConsumerScale = data.serviceEducationConsumerScale;
            serviceHealthCoverage = data.serviceHealthCoverage;
            serviceHealthConsumerScale = data.serviceHealthConsumerScale;
            trafficBase = data.trafficBase;
            trafficRoadwayDensityWeight = data.trafficRoadwayDensityWeight;
            trafficLevelConsumerScale = data.trafficLevelConsumerScale;
        }
    }

    /// <summary>
    /// Serializable snapshot of <see cref="SignalTuningWeightsAsset"/> field state for save persistence
    /// (Stage 6 — schema 6 of <c>GameSaveData</c>). Field names mirror the SO's private backing fields
    /// in camelCase. <see cref="JsonUtility"/>-friendly: 17 public floats only.
    /// </summary>
    [System.Serializable]
    public class SignalTuningWeightsData
    {
        public float happinessBaseline;
        public float weightEmployment;
        public float weightServices;
        public float weightForest;
        public float weightPollution;
        public float weightTax;
        public float weightDev;
        public float baseConvergenceRate;
        public float populationScaleFactor;
        public float comfortableTaxRate;
        public float maxTaxRateForScale;
        public float pollutionCap;
        public float maxForestBonus;
        public float serviceCoverageStub;
        public float parksBonus;
        public float pollutionPenalty;
        public float normalizationCap;
        // Stage 7 — TECH-1889 PollutionLand producer tier weights.
        public float pollutionLandHeavy;
        public float pollutionLandMedium;
        public float pollutionLandLight;
        // Stage 7 — TECH-1890 PollutionWater producer tier weights.
        public float pollutionWaterHeavy;
        public float pollutionWaterMedium;
        public float pollutionWaterLight;
        // Stage 7 — TECH-1891 LandValue composite producer constants.
        public float landValueBase;
        public float landValueParkBonus;
        public float landValueIndustrialPenalty;
        public float landValueDensityBonus;
        // Stage 7 — TECH-1892 EconomyManager tax-base land-value bonus multiplier.
        public float landValueIncomeMultiplier;
        // Stage 8 — TECH-1953 CrimeSystem tuning fields (CrimeProducer + ServicePoliceProducer/Consumer + CrimeHotspotEventEmitter).
        public float crimeBase;
        public float crimeDensityWeight;
        public float servicePoliceCoverage;
        public float servicePoliceConsumerScale;
        public float crimeHotspotThreshold;
        // Stage 9.A — TECH-2079 Service{Fire,Education,Health} producer/consumer tuning fields.
        public float serviceFireCoverage;
        public float serviceFireConsumerScale;
        public float serviceEducationCoverage;
        public float serviceEducationConsumerScale;
        public float serviceHealthCoverage;
        public float serviceHealthConsumerScale;
        // Stage 9.B — TECH-2136 TrafficProducer + TrafficLevelConsumer tuning fields.
        public float trafficBase;
        public float trafficRoadwayDensityWeight;
        public float trafficLevelConsumerScale;
    }
}
