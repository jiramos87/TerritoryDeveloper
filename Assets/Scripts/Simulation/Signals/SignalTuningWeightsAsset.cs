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

        /// <summary>Capture current field state into a serializable snapshot for <c>GameSaveData.tuningWeights</c> (schema 6).</summary>
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
    }
}
