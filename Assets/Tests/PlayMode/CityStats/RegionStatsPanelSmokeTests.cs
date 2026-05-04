using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Territory.Economy;
using Territory.UI.CityStatsHandoff;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Territory.Tests.PlayMode.CityStats
{
    /// <summary>
    /// Stage 13.6 (TECH-9873) — PlayMode smoke for the region-scope aggregation +
    /// scale-switcher mechanism layered on top of the Stage 13.5 presenter pipeline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Scope split (mirrors the Stage 13.5 precedent):
    /// <list type="bullet">
    /// <item><b>Mechanism</b> — aggregation correctness over a synthetic 3-city
    /// fixture (totals + population-weighted means per D2), empty-region null
    /// branch, scale-switcher rebind via <see cref="CityStatsHandoffAdapter.SetPresenter"/>,
    /// binding-key parity across scales, and tick-driven refresh.</item>
    /// <item><b>4-tab visual smoke + per-(scale, tab) screenshots</b> — deferred
    /// to a follow-up bridge harness once the upstream <c>city-stats-handoff</c>
    /// JSX surfaces <c>&lt;Tab&gt;</c> markers (currently the IR carries an empty
    /// <c>tabs[]</c> array, so <see cref="Territory.UI.Themed.ThemedTabBar"/> has
    /// no pages to swap). Tracked as a known gap; not blocking
    /// presenter-mechanism ship — same deferral as Stage 13.5
    /// <see cref="CityStatsPanelSmokeTests"/>.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class RegionStatsPanelSmokeTests
    {
        private GameObject _root;
        private List<Territory.Economy.CityStats> _cities;
        private CityStatsFacade _facade;
        private RegionStatsPresenter _regionPresenter;
        private CityStatsPresenter _cityPresenter;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _root = new GameObject("RegionStatsSmokeRoot");
            _cities = new List<Territory.Economy.CityStats>();

            // 3-city fixture for aggregation. Distinct populations + happiness +
            // pollution + landValue + money so weighted means + sums are
            // distinguishable in assertions below.
            _cities.Add(MakeCity(_root, "city_a", population: 100, money: 1000, happiness: 50f, pollution: 10f, landValue: 200f));
            _cities.Add(MakeCity(_root, "city_b", population: 200, money: 2000, happiness: 80f, pollution: 20f, landValue: 400f));
            _cities.Add(MakeCity(_root, "city_c", population: 300, money: 3000, happiness: 100f, pollution: 30f, landValue: 600f));

            _facade = _root.AddComponent<CityStatsFacade>();
            _cityPresenter = _root.AddComponent<CityStatsPresenter>();
            _regionPresenter = _root.AddComponent<RegionStatsPresenter>();

            // One frame so Awake + OnEnable run on every component.
            yield return null;

            // Inject the synthetic city set explicitly — Awake fallback may have
            // also picked them up; SetCities is idempotent.
            _regionPresenter.SetCities(_cities);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null) Object.Destroy(_root);
            yield return null;
        }

        private static Territory.Economy.CityStats MakeCity(GameObject parent, string name, int population, int money, float happiness, float pollution, float landValue)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, worldPositionStays: false);
            var s = go.AddComponent<Territory.Economy.CityStats>();
            s.population = population;
            s.money = money;
            s.happiness = happiness;
            s.pollution = pollution;
            s.cityLandValueMean = landValue;
            return s;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Aggregation — totals (D2 sum-default).
        // ─────────────────────────────────────────────────────────────────────
        [Test]
        public void Region_Population_IsTotal()
        {
            var producer = _regionPresenter.Bindings["people.population"];
            Assert.AreEqual(600, (int)producer(), "population should sum 100+200+300=600.");
        }

        [Test]
        public void Region_Money_IsTotal()
        {
            var producer = _regionPresenter.Bindings["money.balance"];
            Assert.AreEqual(6000, (int)producer(), "money.balance should sum 1000+2000+3000=6000.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Aggregation — population-weighted means (D2 happiness/pollution/land).
        // ─────────────────────────────────────────────────────────────────────
        [Test]
        public void Region_Happiness_IsPopulationWeightedMean()
        {
            // (100*50 + 200*80 + 300*100) / 600 = (5000+16000+30000)/600 = 51000/600 = 85.0
            var producer = _regionPresenter.Bindings["people.happiness"];
            var raw = producer();
            Assert.IsNotNull(raw, "people.happiness must not be null when Σpop>0.");
            Assert.AreEqual(85.0f, (float)raw, 0.001f, "happiness weighted mean.");
        }

        [Test]
        public void Region_Pollution_IsPopulationWeightedMean()
        {
            // (100*10 + 200*20 + 300*30) / 600 = (1000+4000+9000)/600 = 14000/600 = 23.333…
            var producer = _regionPresenter.Bindings["people.pollution"];
            var raw = producer();
            Assert.IsNotNull(raw, "people.pollution must not be null when Σpop>0.");
            Assert.AreEqual(14000f / 600f, (float)raw, 0.001f, "pollution weighted mean.");
        }

        [Test]
        public void Region_LandValueMean_IsPopulationWeightedMean()
        {
            // (100*200 + 200*400 + 300*600) / 600 = (20000+80000+180000)/600 = 280000/600
            var producer = _regionPresenter.Bindings["land.value.mean"];
            var raw = producer();
            Assert.IsNotNull(raw, "land.value.mean must not be null when Σpop>0.");
            Assert.AreEqual(280000f / 600f, (float)raw, 0.001f, "land value weighted mean.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Empty-region edge case — weighted mean returns null when Σpop==0.
        // Adapter renders the null branch as the upstream Stage 13.5 placeholder
        // (out of 13.6 scope; this test asserts the null contract only).
        // ─────────────────────────────────────────────────────────────────────
        [Test]
        public void Region_EmptyRegion_WeightedMean_ReturnsNull()
        {
            _regionPresenter.SetCities(new List<Territory.Economy.CityStats>());

            Assert.IsNull(_regionPresenter.Bindings["people.happiness"](), "happiness should be null on empty region.");
            Assert.IsNull(_regionPresenter.Bindings["people.pollution"](), "pollution should be null on empty region.");
            Assert.IsNull(_regionPresenter.Bindings["land.value.mean"](), "land.value.mean should be null on empty region.");

            // Sums should be 0, not null (D2 sum-default).
            Assert.AreEqual(0, (int)_regionPresenter.Bindings["people.population"](), "population sum on empty region must be 0.");
            Assert.AreEqual(0, (int)_regionPresenter.Bindings["money.balance"](), "money.balance sum on empty region must be 0.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Binding-key parity — City + Region presenters expose identical key sets
        // (D2.A — same panel + 4 tabs, no scale-specific keys).
        // ─────────────────────────────────────────────────────────────────────
        [Test]
        public void Region_And_City_Bindings_HaveIdenticalKeySets()
        {
            var cityKeys = new HashSet<string>(_cityPresenter.Bindings.Keys);
            var regionKeys = new HashSet<string>(_regionPresenter.Bindings.Keys);
            Assert.IsTrue(cityKeys.SetEquals(regionKeys), $"City + Region binding key sets must match. CityOnly={Diff(cityKeys, regionKeys)}; RegionOnly={Diff(regionKeys, cityKeys)}.");
            Assert.Greater(regionKeys.Count, 30, "Region presenter should expose ~40 binding keys; got fewer than 30.");
        }

        private static string Diff(HashSet<string> a, HashSet<string> b)
        {
            var diff = new HashSet<string>(a);
            diff.ExceptWith(b);
            return string.Join(",", diff);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Tick subscription — facade.EndTick fires presenter.OnRefreshed.
        // ─────────────────────────────────────────────────────────────────────
        [UnityTest]
        public IEnumerator Region_OnRefreshed_FiresOnFacadeTickEnd()
        {
            int refreshCount = 0;
            _regionPresenter.OnRefreshed += () => refreshCount++;

            _facade.BeginTick();
            _facade.EndTick();
            yield return null;

            Assert.GreaterOrEqual(refreshCount, 1, "OnRefreshed should fire at least once per facade.EndTick().");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Scale-switcher mechanism — toggle City ↔ Region rebinds the adapter.
        // ─────────────────────────────────────────────────────────────────────
        [Test]
        public void ScaleSwitcher_TogglesAdapterPresenter()
        {
            var hud = new GameObject("hud");
            hud.transform.SetParent(_root.transform, worldPositionStays: false);
            var adapter = hud.AddComponent<CityStatsHandoffAdapter>();
            var switcher = hud.AddComponent<StatsScaleSwitcher>();
            // Force adapter Awake + OnEnable wiring (FindObjectOfType picks the city presenter as default).
            adapter.enabled = false;
            adapter.enabled = true;
            switcher.enabled = false;
            switcher.enabled = true;

            // Default scale = City.
            Assert.AreEqual(StatsScaleSwitcher.Scale.City, switcher.ActiveScale, "Default scale should be City.");
            Assert.IsTrue(ReferenceEquals(adapter.ActivePresenter, _cityPresenter), "Adapter should bind to CityStatsPresenter at default City scale.");

            switcher.SetScale(StatsScaleSwitcher.Scale.Region);
            Assert.AreEqual(StatsScaleSwitcher.Scale.Region, switcher.ActiveScale, "Scale should be Region after toggle.");
            Assert.IsTrue(ReferenceEquals(adapter.ActivePresenter, _regionPresenter), "Adapter should bind to RegionStatsPresenter after Region toggle.");

            switcher.SetScale(StatsScaleSwitcher.Scale.City);
            Assert.IsTrue(ReferenceEquals(adapter.ActivePresenter, _cityPresenter), "Adapter should re-bind to CityStatsPresenter after toggling back to City.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // D9.A guard — Scale enum must enumerate City + Region only.
        // Country / World leak into the enum at any future point → fail loud.
        // ─────────────────────────────────────────────────────────────────────
        [Test]
        public void ScaleSwitcher_Enum_HasCityAndRegionOnly()
        {
            var values = System.Enum.GetNames(typeof(StatsScaleSwitcher.Scale));
            Assert.AreEqual(2, values.Length, $"Scale enum must have exactly 2 values (City + Region per D9.A); got [{string.Join(",", values)}].");
            CollectionAssert.Contains(values, "City");
            CollectionAssert.Contains(values, "Region");
            CollectionAssert.DoesNotContain(values, "Country", "Country must NOT be in the Scale enum (D9.A — hidden entirely).");
            CollectionAssert.DoesNotContain(values, "World", "World must NOT be in the Scale enum (D9.A — hidden entirely).");
        }
    }
}
