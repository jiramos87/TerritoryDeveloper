using System;
using System.Collections.Generic;
using UnityEngine;
using Territory.Economy;

namespace Territory.UI.CityStatsHandoff
{
    /// <summary>
    /// Stage 13.6 (TECH-9871) — region-scope <see cref="IStatsPresenter"/> over an
    /// arbitrary set of <see cref="CityStats"/> producers. Aggregates per D2 rules:
    /// <c>population</c> + <c>money</c> as totals; <c>happiness</c>, <c>pollution</c>,
    /// <c>cityLandValueMean</c> as population-weighted means; everything else as
    /// totals (sum default).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Empty-region edge case: <c>SumInt</c>/<c>SumFloat</c> return 0; weighted-mean
    /// returns <c>null</c> when <c>Σpop == 0</c> (adapter renders the null branch as
    /// the upstream Stage 13.5 placeholder once available — out of 13.6 scope).
    /// No NaN propagates; producers with <c>population &lt;= 0</c> are skipped from
    /// weighted accumulation.
    /// </para>
    /// <para>
    /// Refresh trigger: <see cref="CityStatsFacade.OnTickEnd"/> per active facade
    /// found in <see cref="Awake"/> (invariant #4 / guardrail #0); subscriptions
    /// owned by <see cref="OnEnable"/> / <see cref="OnDisable"/>. Any facade tick
    /// fires <see cref="OnRefreshed"/> once per call (D8 parity with
    /// <see cref="CityStatsPresenter"/>). External callers may also drive an
    /// out-of-band <see cref="RequestRefresh"/> after <see cref="SetCities"/> swaps
    /// the producer set.
    /// </para>
    /// <para>
    /// Membership injection: <see cref="SetCities"/> rebuilds bindings + fires
    /// <see cref="OnRefreshed"/> so adapters re-paint immediately. Without an
    /// explicit call, <see cref="Awake"/> falls back to
    /// <see cref="MonoBehaviour.FindObjectsOfType{T}()"/> for both producers
    /// (mirrors Stage 13.5 pattern). <see cref="IsReady"/> goes true once
    /// <see cref="BuildBindings"/> populates the registry.
    /// </para>
    /// </remarks>
    public class RegionStatsPresenter : MonoBehaviour, IStatsPresenter
    {
        private readonly Dictionary<string, Func<object>> _bindings = new Dictionary<string, Func<object>>(64);
        private readonly List<CityStats> _cities = new List<CityStats>();
        private CityStatsFacade[] _facades = Array.Empty<CityStatsFacade>();
        private bool _bindingsBuilt;
        private int _refreshVersion;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, Func<object>> Bindings => _bindings;

        /// <inheritdoc />
        public event Action OnRefreshed;

        /// <inheritdoc />
        public bool IsReady => _bindingsBuilt;

        /// <summary>Monotonic refresh counter (matches <see cref="CityStatsPresenter.RefreshVersion"/> semantics).</summary>
        public int RefreshVersion => _refreshVersion;

        /// <summary>Snapshot of the region's current member count (for diagnostics + tests).</summary>
        public int CityCount => _cities.Count;

        private void Awake()
        {
            // Inspector wiring would normally inject _facades; in the absence of
            // a region-scope manager, fall back to scene-wide enumeration so the
            // presenter wires up automatically (invariant #4 fallback path).
            _facades = FindObjectsOfType<CityStatsFacade>();
            if (_cities.Count == 0)
            {
                var found = FindObjectsOfType<CityStats>();
                if (found != null && found.Length > 0)
                    _cities.AddRange(found);
            }
            BuildBindings();
        }

        private void OnEnable()
        {
            SubscribeFacades();
        }

        private void OnDisable()
        {
            UnsubscribeFacades();
        }

        /// <summary>
        /// Inject the region member set. Invoked by the scale-switcher (T13.6.2)
        /// when toggling to Region scope, or by tests with synthetic producers.
        /// Resets binding registry + fires <see cref="OnRefreshed"/>.
        /// </summary>
        public void SetCities(IEnumerable<CityStats> cities)
        {
            _cities.Clear();
            if (cities != null)
            {
                foreach (var c in cities)
                {
                    if (c != null) _cities.Add(c);
                }
            }
            BuildBindings();
            RequestRefresh();
        }

        /// <inheritdoc />
        public void RequestRefresh()
        {
            _refreshVersion++;
            OnRefreshed?.Invoke();
        }

        private void HandleTickEnd()
        {
            _refreshVersion++;
            OnRefreshed?.Invoke();
        }

        private void SubscribeFacades()
        {
            if (_facades == null) return;
            foreach (var f in _facades)
            {
                if (f != null) f.OnTickEnd += HandleTickEnd;
            }
        }

        private void UnsubscribeFacades()
        {
            if (_facades == null) return;
            foreach (var f in _facades)
            {
                if (f != null) f.OnTickEnd -= HandleTickEnd;
            }
        }

        private void BuildBindings()
        {
            _bindings.Clear();

            // ── Money tab — sum-default ───────────────────────────────────────
            _bindings["money.balance"] = () => SumInt(c => c.money);
            _bindings["money.envelope.total"] = () => SumInt(c => c.totalEnvelopeCap);
            _bindings["money.envelope.remaining.0"] = () => SumInt(c => SafeEnvelope(c, 0));
            _bindings["money.envelope.remaining.1"] = () => SumInt(c => SafeEnvelope(c, 1));
            _bindings["money.envelope.remaining.2"] = () => SumInt(c => SafeEnvelope(c, 2));
            _bindings["money.envelope.remaining.3"] = () => SumInt(c => SafeEnvelope(c, 3));
            _bindings["money.envelope.remaining.4"] = () => SumInt(c => SafeEnvelope(c, 4));
            _bindings["money.envelope.remaining.5"] = () => SumInt(c => SafeEnvelope(c, 5));
            _bindings["money.envelope.remaining.6"] = () => SumInt(c => SafeEnvelope(c, 6));

            // ── People tab — sum-default + 2 weighted means ───────────────────
            _bindings["people.population"] = () => SumInt(c => c.population);
            _bindings["people.happiness"] = () => WeightedMean(c => c.happiness);
            _bindings["people.pollution"] = () => WeightedMean(c => c.pollution);
            _bindings["people.residential.zones"] = () => SumInt(c => c.residentialZoneCount);
            _bindings["people.residential.buildings"] = () => SumInt(c => c.residentialBuildingCount);
            _bindings["people.residential.light.buildings"] = () => SumInt(c => c.residentialLightBuildingCount);
            _bindings["people.residential.light.zoning"] = () => SumInt(c => c.residentialLightZoningCount);
            _bindings["people.residential.medium.buildings"] = () => SumInt(c => c.residentialMediumBuildingCount);
            _bindings["people.residential.medium.zoning"] = () => SumInt(c => c.residentialMediumZoningCount);
            _bindings["people.residential.heavy.buildings"] = () => SumInt(c => c.residentialHeavyBuildingCount);
            _bindings["people.residential.heavy.zoning"] = () => SumInt(c => c.residentialHeavyZoningCount);
            _bindings["people.city_name"] = () => "Region";

            // ── Land tab — sum-default + 1 weighted mean ──────────────────────
            _bindings["land.value.mean"] = () => WeightedMean(c => c.cityLandValueMean);
            _bindings["land.forest.cells"] = () => SumInt(c => c.forestCellCount);
            _bindings["land.forest.coverage_pct"] = () => SumFloat(c => c.forestCoveragePercentage);
            _bindings["land.grass.cells"] = () => SumInt(c => c.grassCount);
            _bindings["land.commercial.zones"] = () => SumInt(c => c.commercialZoneCount);
            _bindings["land.commercial.buildings"] = () => SumInt(c => c.commercialBuildingCount);
            _bindings["land.industrial.zones"] = () => SumInt(c => c.industrialZoneCount);
            _bindings["land.industrial.buildings"] = () => SumInt(c => c.industrialBuildingCount);
            _bindings["land.commercial.light.zoning"] = () => SumInt(c => c.commercialLightZoningCount);
            _bindings["land.industrial.light.zoning"] = () => SumInt(c => c.industrialLightZoningCount);

            // ── Infrastructure tab — sum-default ──────────────────────────────
            _bindings["infra.power.consumption"] = () => SumInt(c => c.cityPowerConsumption);
            _bindings["infra.power.output"] = () => SumInt(c => c.cityPowerOutput);
            _bindings["infra.power.balance"] = () => SumInt(c => c.cityPowerOutput - c.cityPowerConsumption);
            _bindings["infra.water.consumption"] = () => SumInt(c => c.cityWaterConsumption);
            _bindings["infra.water.output"] = () => SumInt(c => c.cityWaterOutput);
            _bindings["infra.water.balance"] = () => SumInt(c => c.cityWaterOutput - c.cityWaterConsumption);
            _bindings["infra.road.cells"] = () => SumInt(c => c.roadCount);
            _bindings["infra.commercial.medium.buildings"] = () => SumInt(c => c.commercialMediumBuildingCount);
            _bindings["infra.commercial.heavy.buildings"] = () => SumInt(c => c.commercialHeavyBuildingCount);
            _bindings["infra.industrial.light.buildings"] = () => SumInt(c => c.industrialLightBuildingCount);
            _bindings["infra.industrial.medium.buildings"] = () => SumInt(c => c.industrialMediumBuildingCount);
            _bindings["infra.industrial.heavy.buildings"] = () => SumInt(c => c.industrialHeavyBuildingCount);

            _bindingsBuilt = true;
        }

        private int SumInt(Func<CityStats, int> selector)
        {
            int total = 0;
            for (int i = 0; i < _cities.Count; i++)
            {
                var c = _cities[i];
                if (c != null) total += selector(c);
            }
            return total;
        }

        private float SumFloat(Func<CityStats, float> selector)
        {
            float total = 0f;
            for (int i = 0; i < _cities.Count; i++)
            {
                var c = _cities[i];
                if (c != null) total += selector(c);
            }
            return total;
        }

        /// <summary>
        /// Population-weighted mean per D2. Returns <c>null</c> (boxed) when
        /// Σpop == 0 so the adapter can branch on null-vs-numeric and render the
        /// "—" placeholder once Stage 13.5 string-handling lands.
        /// </summary>
        private object WeightedMean(Func<CityStats, float> selector)
        {
            float weightSum = 0f;
            float weighted = 0f;
            for (int i = 0; i < _cities.Count; i++)
            {
                var c = _cities[i];
                if (c == null) continue;
                int pop = c.population;
                if (pop <= 0) continue;
                weightSum += pop;
                weighted += pop * selector(c);
            }
            return weightSum > 0f ? (object)(weighted / weightSum) : null;
        }

        private static int SafeEnvelope(CityStats s, int idx)
        {
            var arr = s.envelopeRemainingPerSubType;
            return arr != null && idx >= 0 && idx < arr.Length ? arr[idx] : 0;
        }
    }
}
