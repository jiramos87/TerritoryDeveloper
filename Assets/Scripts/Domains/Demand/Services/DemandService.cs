using System;
using UnityEngine;
using Territory.Core;
using Territory.Zones;
using Territory.Economy;

namespace Domains.Demand.Services
{
    /// <summary>
    /// POCO service extracted from DemandManager (Stage 5.4 Tier-C NO-PORT).
    /// RCI demand logic: residential/commercial/industrial update, tax pressure, happiness, external modifier, cell desirability.
    /// Hub (DemandManager) owns serialized fields and delegates here via WireDependencies.
    /// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons) observed.
    /// Invariant #11: UrbanizationProposal pre-scan complete — no runtime references present in DemandManager (flag-only per spec).
    /// autoReferenced:true on TerritoryDeveloper.Game (via .asmref) provides CityStats/EconomyManager/EmploymentManager types.
    /// </summary>
    public class DemandService
    {
        // ── Wired dependencies ──────────────────────────────────────────────────────
        private IGridManager     _grid;
        private EmploymentManager _employment;
        private CityStats         _cityStats;
        private EconomyManager    _economy;

        // ── Hub-owned config mirrored at WireDependencies ───────────────────────────
        private float _startingResidentialDemand;
        private float _startingCommercialDemand;
        private float _startingIndustrialDemand;
        private float _demandSmoothingPerDay;
        private float _unemploymentThreshold;
        private float _unemploymentResidentialPenalty;
        private float _unemploymentJobBoost;
        private float _desirabilityDemandMultiplier;
        private float _comfortableTaxRateForDemand;
        private float _maxTaxRateForDemandScale;
        private float _taxDemandPenaltyAtMax;

        // ── Setup ───────────────────────────────────────────────────────────────────

        /// <summary>Wire dependencies + config from hub. Call from hub Start after FindObjectOfType pass.</summary>
        public void WireDependencies(
            IGridManager     grid,
            EmploymentManager employment,
            CityStats         cityStats,
            EconomyManager    economy,
            float startingResidentialDemand,
            float startingCommercialDemand,
            float startingIndustrialDemand,
            float demandSmoothingPerDay,
            float unemploymentThreshold,
            float unemploymentResidentialPenalty,
            float unemploymentJobBoost,
            float desirabilityDemandMultiplier,
            float comfortableTaxRateForDemand,
            float maxTaxRateForDemandScale,
            float taxDemandPenaltyAtMax)
        {
            _grid                        = grid;
            _employment                  = employment;
            _cityStats                   = cityStats;
            _economy                     = economy;
            _startingResidentialDemand   = startingResidentialDemand;
            _startingCommercialDemand    = startingCommercialDemand;
            _startingIndustrialDemand    = startingIndustrialDemand;
            _demandSmoothingPerDay       = demandSmoothingPerDay;
            _unemploymentThreshold       = unemploymentThreshold;
            _unemploymentResidentialPenalty = unemploymentResidentialPenalty;
            _unemploymentJobBoost        = unemploymentJobBoost;
            _desirabilityDemandMultiplier = desirabilityDemandMultiplier;
            _comfortableTaxRateForDemand = comfortableTaxRateForDemand;
            _maxTaxRateForDemandScale    = maxTaxRateForDemandScale;
            _taxDemandPenaltyAtMax       = taxDemandPenaltyAtMax;
        }

        // ── Building tracking ───────────────────────────────────────────────────────

        /// <summary>Compute building tracker from current CityStats + previous counts.</summary>
        public void UpdateBuildingTracking(
            BuildingTracker tracker,
            ref int previousResidential,
            ref int previousCommercial,
            ref int previousIndustrial)
        {
            if (_cityStats == null) return;

            int curR = _cityStats.residentialLightBuildingCount
                + _cityStats.residentialMediumBuildingCount
                + _cityStats.residentialHeavyBuildingCount;
            int curC = _cityStats.commercialLightBuildingCount
                + _cityStats.commercialMediumBuildingCount
                + _cityStats.commercialHeavyBuildingCount;
            int curI = _cityStats.industrialLightBuildingCount
                + _cityStats.industrialMediumBuildingCount
                + _cityStats.industrialHeavyBuildingCount;

            tracker.residentialZonesWithoutBuildings = Mathf.Max(0,
                (_cityStats.residentialLightZoningCount + _cityStats.residentialMediumZoningCount + _cityStats.residentialHeavyZoningCount)
                - curR);
            tracker.commercialZonesWithoutBuildings = Mathf.Max(0,
                (_cityStats.commercialLightZoningCount + _cityStats.commercialMediumZoningCount + _cityStats.commercialHeavyZoningCount)
                - curC);
            tracker.industrialZonesWithoutBuildings = Mathf.Max(0,
                (_cityStats.industrialLightZoningCount + _cityStats.industrialMediumZoningCount + _cityStats.industrialHeavyZoningCount)
                - curI);

            tracker.newResidentialBuildings = Mathf.Max(0, curR - previousResidential);
            tracker.newCommercialBuildings  = Mathf.Max(0, curC - previousCommercial);
            tracker.newIndustrialBuildings  = Mathf.Max(0, curI - previousIndustrial);

            previousResidential = curR;
            previousCommercial  = curC;
            previousIndustrial  = curI;
        }

        // ── RCI demand update ───────────────────────────────────────────────────────

        /// <summary>Run full RCI demand update pipeline on provided demand data structs.</summary>
        public void UpdateRCIDemand(
            DemandData residential,
            DemandData commercial,
            DemandData industrial,
            BuildingTracker tracker)
        {
            UpdateResidentialDemand(residential, tracker);
            UpdateCommercialDemand(commercial, tracker);
            UpdateIndustrialDemand(industrial, tracker);
            ApplySectorTaxPressure(residential, commercial, industrial);
            ApplyHappinessModifier(residential, commercial, industrial);
            ApplyExternalDemandModifier(residential, commercial, industrial);
        }

        private void UpdateResidentialDemand(DemandData residential, BuildingTracker tracker)
        {
            int availableJobs = _employment != null ? _employment.GetAvailableJobs() : 0;
            float unemploymentRate = _employment != null ? _employment.unemploymentRate : 0f;

            if (availableJobs <= 0)
            {
                float targetDemand = -30f;
                residential.demandLevel = Mathf.Lerp(residential.demandLevel, targetDemand, _demandSmoothingPerDay);
            }
            else
            {
                float targetDemand = _startingResidentialDemand;

                if (tracker.industrialZonesWithoutBuildings > 0 && tracker.residentialZonesWithoutBuildings > 0)
                    targetDemand += 15f;

                if (availableJobs > 10)
                    targetDemand += 10f;

                if (unemploymentRate > _unemploymentThreshold)
                {
                    float excess = unemploymentRate - _unemploymentThreshold;
                    targetDemand -= excess * _unemploymentResidentialPenalty;
                }

                residential.demandLevel = Mathf.Lerp(residential.demandLevel, targetDemand, _demandSmoothingPerDay);
            }

            residential.demandLevel = Mathf.Clamp(residential.demandLevel, -100f, 100f);
        }

        private void UpdateCommercialDemand(DemandData commercial, BuildingTracker tracker)
        {
            float targetDemand = _startingCommercialDemand;
            float unemploymentRate = _employment != null ? _employment.unemploymentRate : 0f;

            if (tracker.residentialZonesWithoutBuildings > 0) targetDemand += 10f;
            if (tracker.newResidentialBuildings > 0) targetDemand += tracker.newResidentialBuildings * 12f;

            if (unemploymentRate > _unemploymentThreshold)
                targetDemand += (unemploymentRate - _unemploymentThreshold) * _unemploymentJobBoost;

            commercial.demandLevel = Mathf.Lerp(commercial.demandLevel, targetDemand, _demandSmoothingPerDay);
            commercial.demandLevel = Mathf.Clamp(commercial.demandLevel, -100f, 100f);
        }

        private void UpdateIndustrialDemand(DemandData industrial, BuildingTracker tracker)
        {
            float targetDemand = _startingIndustrialDemand;
            float unemploymentRate = _employment != null ? _employment.unemploymentRate : 0f;

            if (tracker.newResidentialBuildings > 0) targetDemand += tracker.newResidentialBuildings * 10f;

            if (_cityStats != null)
            {
                int totalIndustrial = _cityStats.industrialLightBuildingCount
                    + _cityStats.industrialMediumBuildingCount
                    + _cityStats.industrialHeavyBuildingCount;
                if (totalIndustrial < 5) targetDemand = Mathf.Max(targetDemand, 25f);
            }

            if (unemploymentRate > _unemploymentThreshold)
                targetDemand += (unemploymentRate - _unemploymentThreshold) * _unemploymentJobBoost;

            industrial.demandLevel = Mathf.Lerp(industrial.demandLevel, targetDemand, _demandSmoothingPerDay);
            industrial.demandLevel = Mathf.Clamp(industrial.demandLevel, -100f, 100f);
        }

        private void ApplySectorTaxPressure(DemandData residential, DemandData commercial, DemandData industrial)
        {
            if (_economy == null) return;
            float denom = _maxTaxRateForDemandScale - _comfortableTaxRateForDemand;
            if (denom <= 0.01f) denom = 0.01f;

            residential.demandLevel *= GetTaxPressureMultiplier(_economy.residentialIncomeTax, denom);
            commercial.demandLevel  *= GetTaxPressureMultiplier(_economy.commercialIncomeTax,  denom);
            industrial.demandLevel  *= GetTaxPressureMultiplier(_economy.industrialIncomeTax,  denom);

            residential.demandLevel = Mathf.Clamp(residential.demandLevel, -100f, 100f);
            commercial.demandLevel  = Mathf.Clamp(commercial.demandLevel,  -100f, 100f);
            industrial.demandLevel  = Mathf.Clamp(industrial.demandLevel,  -100f, 100f);
        }

        private float GetTaxPressureMultiplier(int taxRatePercent, float denom)
        {
            if (taxRatePercent <= _comfortableTaxRateForDemand) return 1f;
            float excess = Mathf.Clamp01((taxRatePercent - _comfortableTaxRateForDemand) / denom);
            return 1f - _taxDemandPenaltyAtMax * excess;
        }

        private void ApplyHappinessModifier(DemandData residential, DemandData commercial, DemandData industrial)
        {
            if (_cityStats == null) return;
            float multiplier = _cityStats.GetHappinessDemandMultiplier();
            residential.demandLevel = Mathf.Clamp(residential.demandLevel * multiplier, -100f, 100f);
            commercial.demandLevel  = Mathf.Clamp(commercial.demandLevel  * multiplier, -100f, 100f);
            industrial.demandLevel  = Mathf.Clamp(industrial.demandLevel  * multiplier, -100f, 100f);
        }

        private void ApplyExternalDemandModifier(DemandData residential, DemandData commercial, DemandData industrial)
        {
            float multiplier = GetExternalDemandModifier();
            residential.demandLevel = Mathf.Clamp(residential.demandLevel * multiplier, -100f, 100f);
            commercial.demandLevel  = Mathf.Clamp(commercial.demandLevel  * multiplier, -100f, 100f);
            industrial.demandLevel  = Mathf.Clamp(industrial.demandLevel  * multiplier, -100f, 100f);
        }

        // ── Public query methods ────────────────────────────────────────────────────

        /// <summary>Stage 7 placeholder: 1.0 + 0.05 * neighbor stub count. Returns 1.0 when grid missing.</summary>
        public float GetExternalDemandModifier()
        {
            if (_grid == null)
            {
                Debug.LogWarning("DemandService: GridManager missing for external modifier");
                return 1.0f;
            }
            int stubCount = 0;
            foreach (BorderSide side in Enum.GetValues(typeof(BorderSide)))
            {
                if (_grid.GetNeighborStub(side).HasValue)
                    stubCount++;
            }
            return 1.0f + 0.05f * stubCount;
        }

        /// <summary>Demand level for specific zone type.</summary>
        public float GetDemandLevel(Zone.ZoneType zoneType, DemandData residential, DemandData commercial, DemandData industrial)
        {
            switch (zoneType)
            {
                case Zone.ZoneType.ResidentialLightZoning:
                case Zone.ZoneType.ResidentialMediumZoning:
                case Zone.ZoneType.ResidentialHeavyZoning:
                    return residential.demandLevel;
                case Zone.ZoneType.CommercialLightZoning:
                case Zone.ZoneType.CommercialMediumZoning:
                case Zone.ZoneType.CommercialHeavyZoning:
                    return commercial.demandLevel;
                case Zone.ZoneType.IndustrialLightZoning:
                case Zone.ZoneType.IndustrialMediumZoning:
                case Zone.ZoneType.IndustrialHeavyZoning:
                    return industrial.demandLevel;
                default:
                    return 100f;
            }
        }

        /// <summary>Cell desirability bonus for (x,y).</summary>
        public float GetCellDesirabilityBonus(int x, int y)
        {
            if (_grid == null) return 0f;
            CityCell cell = _grid.GetCell(x, y);
            if (cell != null) return cell.desirability * _desirabilityDemandMultiplier;
            return 0f;
        }

        /// <summary>Map demand level [-100,100] → spawn probability [0,1].</summary>
        public float GetDemandSpawnFactor(float demandLevel)
            => Mathf.Clamp01((demandLevel + 100f) / 200f);

        /// <summary>True if employment manager reports available jobs > 0.</summary>
        public bool HasAvailableJobs()
            => _employment != null && _employment.GetAvailableJobs() > 0;

        /// <summary>Available job count from employment manager.</summary>
        public int GetAvailableJobs()
            => _employment != null ? _employment.GetAvailableJobs() : 0;

        /// <summary>Environmental bonus stub — returns 0 (API compat, no longer affects demand).</summary>
        public float GetEnvironmentalDemandBonus() => 0f;
    }
}
