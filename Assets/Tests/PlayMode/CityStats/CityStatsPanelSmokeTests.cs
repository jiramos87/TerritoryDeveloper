using System.Collections;
using NUnit.Framework;
using Territory.Economy;
using Territory.UI.CityStatsHandoff;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Territory.Tests.PlayMode.CityStats
{
    /// <summary>
    /// Stage 13.5 (TECH-9870) — PlayMode smoke for the presenter-driven
    /// <c>city-stats-handoff</c> pipeline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Scope split:
    /// <list type="bullet">
    /// <item>Mechanism — verifies <see cref="CityStatsPresenter"/> exposes the
    /// 4-tab binding taxonomy and that <see cref="CityStatsFacade.OnTickEnd"/>
    /// drives <see cref="IStatsPresenter.OnRefreshed"/>. Self-contained: spawns
    /// a synthetic <c>CityStats</c> + <c>CityStatsFacade</c> + presenter graph
    /// so it runs independently of any baked scene wiring.</item>
    /// <item>4-tab visual smoke + screenshots (per §Acceptance bullets 3 + 5)
    /// — deferred to a follow-up bridge harness once the upstream
    /// <c>city-stats-handoff</c> JSX surfaces <c>&lt;Tab&gt;</c> markers
    /// (currently the IR carries an empty <c>tabs[]</c> array, so
    /// <see cref="Territory.UI.Themed.ThemedTabBar"/> has no pages to swap).
    /// Tracked as a known gap; not blocking presenter-mechanism ship.</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class CityStatsPanelSmokeTests
    {
        private GameObject _root;
        private Territory.Economy.CityStats _cityStats;
        private CityStatsFacade _facade;
        private CityStatsPresenter _presenter;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _root = new GameObject("CityStatsSmokeRoot");

            // Order: producers first, then presenter — Awake walks FindObjectOfType fallback
            // when Inspector slots empty (guardrail #0).
            _cityStats = _root.AddComponent<Territory.Economy.CityStats>();
            _facade = _root.AddComponent<CityStatsFacade>();
            _presenter = _root.AddComponent<CityStatsPresenter>();

            // One frame so Awake + OnEnable run on every component.
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null) Object.Destroy(_root);
            yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Mechanism gate — presenter binding registry covers all 4 D1 tabs.
        // ─────────────────────────────────────────────────────────────────────
        [Test]
        public void Presenter_Bindings_CoverAllFourD1Tabs()
        {
            Assert.IsTrue(_presenter.IsReady, "Presenter.IsReady must be true after Awake when both producers spawn alongside.");
            Assert.Greater(_presenter.Bindings.Count, 30, "Presenter should expose ~40 binding keys; got fewer than 30.");

            string[] requiredKeys =
            {
                "money.balance",
                "money.envelope.total",
                "money.bond.debt",
                "people.population",
                "people.happiness",
                "people.pollution",
                "land.value.mean",
                "land.forest.cells",
                "infra.power.consumption",
                "infra.water.output",
                "infra.road.cells",
            };
            foreach (var key in requiredKeys)
            {
                Assert.IsTrue(_presenter.Bindings.ContainsKey(key), $"Presenter missing required binding key '{key}'.");
                var producer = _presenter.Bindings[key];
                Assert.IsNotNull(producer, $"Binding producer for '{key}' was null.");
                Assert.DoesNotThrow(() => producer(), $"Binding producer for '{key}' threw on invocation.");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Tick subscription — facade.EndTick fires presenter.OnRefreshed exactly once.
        // Validates invariant #3 (event-driven, not Update polling).
        // ─────────────────────────────────────────────────────────────────────
        [UnityTest]
        public IEnumerator Presenter_OnRefreshed_FiresOnFacadeTickEnd()
        {
            int refreshCount = 0;
            _presenter.OnRefreshed += () => refreshCount++;

            _facade.BeginTick();
            _facade.EndTick();

            // One frame to let any deferred work settle.
            yield return null;

            Assert.AreEqual(1, refreshCount, "OnRefreshed should fire exactly once per facade.EndTick().");
        }

        // ─────────────────────────────────────────────────────────────────────
        // RequestRefresh idempotence — two rapid calls fire two events, no exceptions.
        // ─────────────────────────────────────────────────────────────────────
        [Test]
        public void Presenter_RequestRefresh_IsIdempotentNonThrowing()
        {
            int refreshCount = 0;
            _presenter.OnRefreshed += () => refreshCount++;

            Assert.DoesNotThrow(() => _presenter.RequestRefresh());
            Assert.DoesNotThrow(() => _presenter.RequestRefresh());

            Assert.AreEqual(2, refreshCount, "Two RequestRefresh calls should fire two OnRefreshed events.");
        }
    }
}
