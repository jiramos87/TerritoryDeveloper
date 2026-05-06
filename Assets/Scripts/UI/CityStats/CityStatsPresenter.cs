using System;
using System.Collections.Generic;
using UnityEngine;
using Territory.Economy;

namespace Territory.UI.CityStatsHandoff
{
    /// <summary>
    /// Concrete <see cref="IStatsPresenter"/> over <see cref="CityStats"/> +
    /// <see cref="CityStatsFacade"/>. Subscribes to <see cref="CityStatsFacade.OnTickEnd"/>
    /// (NOT per-frame <c>Update</c> — invariant #3) and republishes a flat key →
    /// <c>Func&lt;object&gt;</c> registry covering the D1 city-stats taxonomy
    /// (Money / People / Land / Infrastructure tabs).
    /// </summary>
    /// <remarks>
    /// Producers wired via Inspector first; <c>Awake</c> falls back to
    /// <see cref="MonoBehaviour.FindObjectOfType{T}()"/> per invariant #4 / guardrail #0.
    /// <see cref="IsReady"/> guards the manager-init race (guardrail #14): true only
    /// after both refs are non-null AND <see cref="Bindings"/> has been populated.
    /// Bindings are <em>live closures</em> — each invocation reads the current field
    /// value, so renderers see fresh data on every <see cref="OnRefreshed"/> fire.
    /// </remarks>
    public class CityStatsPresenter : MonoBehaviour, IStatsPresenter
    {
        [Header("Producers")]
        [SerializeField] private CityStats _cityStats;
        [SerializeField] private CityStatsFacade _facade;

        // DS-* token audit — TECH-15227: city-stats surface.
        // Stat label colors + bar-fill gradients use ad-hoc Inspector Color literals in downstream renderers.
        // Migrate to UiTheme palette entries (ds-text-primary, ds-accent-positive, ds-accent-negative) in Stage N token-bake.

        private readonly Dictionary<string, Func<object>> _bindings = new Dictionary<string, Func<object>>(64);
        private bool _bindingsBuilt;
        private int _refreshVersion;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, Func<object>> Bindings => _bindings;

        /// <inheritdoc />
        public event Action OnRefreshed;

        /// <inheritdoc />
        public bool IsReady => _cityStats != null && _facade != null && _bindingsBuilt;

        /// <summary>Monotonic refresh counter — useful for renderers that want to skip re-paint when version unchanged.</summary>
        public int RefreshVersion => _refreshVersion;

        private void Awake()
        {
            if (_cityStats == null) _cityStats = FindObjectOfType<CityStats>();
            if (_facade == null) _facade = FindObjectOfType<CityStatsFacade>();
            BuildBindings();
        }

        private void OnEnable()
        {
            if (_facade != null) _facade.OnTickEnd += HandleTickEnd;
        }

        private void OnDisable()
        {
            if (_facade != null) _facade.OnTickEnd -= HandleTickEnd;
        }

        /// <inheritdoc />
        public void RequestRefresh()
        {
            // Idempotent: bump version + re-fire without re-subscribing.
            _refreshVersion++;
            OnRefreshed?.Invoke();
        }

        private void HandleTickEnd()
        {
            _refreshVersion++;
            OnRefreshed?.Invoke();
        }

        private void BuildBindings()
        {
            if (_cityStats == null) return;
            var s = _cityStats;

            // ── Money tab (~10 keys) ──────────────────────────────────────────
            _bindings["money.balance"] = () => s.money;
            _bindings["money.envelope.total"] = () => s.totalEnvelopeCap;
            _bindings["money.envelope.remaining.0"] = () => SafeEnvelope(s, 0);
            _bindings["money.envelope.remaining.1"] = () => SafeEnvelope(s, 1);
            _bindings["money.envelope.remaining.2"] = () => SafeEnvelope(s, 2);
            _bindings["money.envelope.remaining.3"] = () => SafeEnvelope(s, 3);
            _bindings["money.envelope.remaining.4"] = () => SafeEnvelope(s, 4);
            _bindings["money.envelope.remaining.5"] = () => SafeEnvelope(s, 5);
            _bindings["money.envelope.remaining.6"] = () => SafeEnvelope(s, 6);
#if BONDS_ENABLED
            // BUG-61 W4 — bond bindings hidden behind feature flag (default OFF) for MVP.
            _bindings["money.bond.debt"] = () => s.activeBondDebt;
            _bindings["money.bond.monthly_repayment"] = () => s.monthlyBondRepayment;
#endif

            // ── People tab (~12 keys) ─────────────────────────────────────────
            _bindings["people.population"] = () => s.population;
            _bindings["people.happiness"] = () => s.happiness;
            _bindings["people.pollution"] = () => s.pollution;
            _bindings["people.residential.zones"] = () => s.residentialZoneCount;
            _bindings["people.residential.buildings"] = () => s.residentialBuildingCount;
            _bindings["people.residential.light.buildings"] = () => s.residentialLightBuildingCount;
            _bindings["people.residential.light.zoning"] = () => s.residentialLightZoningCount;
            _bindings["people.residential.medium.buildings"] = () => s.residentialMediumBuildingCount;
            _bindings["people.residential.medium.zoning"] = () => s.residentialMediumZoningCount;
            _bindings["people.residential.heavy.buildings"] = () => s.residentialHeavyBuildingCount;
            _bindings["people.residential.heavy.zoning"] = () => s.residentialHeavyZoningCount;
            _bindings["people.city_name"] = () => s.cityName;

            // ── Land tab (~10 keys) ───────────────────────────────────────────
            _bindings["land.value.mean"] = () => s.cityLandValueMean;
            _bindings["land.forest.cells"] = () => s.forestCellCount;
            _bindings["land.forest.coverage_pct"] = () => s.forestCoveragePercentage;
            _bindings["land.grass.cells"] = () => s.grassCount;
            _bindings["land.commercial.zones"] = () => s.commercialZoneCount;
            _bindings["land.commercial.buildings"] = () => s.commercialBuildingCount;
            _bindings["land.industrial.zones"] = () => s.industrialZoneCount;
            _bindings["land.industrial.buildings"] = () => s.industrialBuildingCount;
            _bindings["land.commercial.light.zoning"] = () => s.commercialLightZoningCount;
            _bindings["land.industrial.light.zoning"] = () => s.industrialLightZoningCount;

            // ── Infrastructure tab (~12 keys) ─────────────────────────────────
            _bindings["infra.power.consumption"] = () => s.cityPowerConsumption;
            _bindings["infra.power.output"] = () => s.cityPowerOutput;
            _bindings["infra.power.balance"] = () => s.cityPowerOutput - s.cityPowerConsumption;
            _bindings["infra.water.consumption"] = () => s.cityWaterConsumption;
            _bindings["infra.water.output"] = () => s.cityWaterOutput;
            _bindings["infra.water.balance"] = () => s.cityWaterOutput - s.cityWaterConsumption;
            _bindings["infra.road.cells"] = () => s.roadCount;
            _bindings["infra.commercial.medium.buildings"] = () => s.commercialMediumBuildingCount;
            _bindings["infra.commercial.heavy.buildings"] = () => s.commercialHeavyBuildingCount;
            _bindings["infra.industrial.light.buildings"] = () => s.industrialLightBuildingCount;
            _bindings["infra.industrial.medium.buildings"] = () => s.industrialMediumBuildingCount;
            _bindings["infra.industrial.heavy.buildings"] = () => s.industrialHeavyBuildingCount;

            _bindingsBuilt = true;
        }

        private static int SafeEnvelope(CityStats s, int idx)
        {
            var arr = s.envelopeRemainingPerSubType;
            return arr != null && idx >= 0 && idx < arr.Length ? arr[idx] : 0;
        }
    }
}
