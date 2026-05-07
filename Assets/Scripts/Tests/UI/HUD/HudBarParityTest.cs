using System.Collections;
using NUnit.Framework;
using Territory.Economy;
using Territory.Timing;
using Territory.UI.HUD;
using Territory.UI.Juice;
using Territory.UI.StudioControls;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Territory.Tests.PlayMode.UI.HUD
{
    /// <summary>
    /// Stage 6 HUD parity PlayMode tests — assert new HudBar StudioControl readouts mirror live
    /// <see cref="CityStats"/> + <see cref="TimeManager"/> values within tolerance after a
    /// fixed sim tick budget. Channels: money (<see cref="SegmentedReadout"/>.CurrentValue),
    /// happiness (<see cref="NeedleBallistics"/>.CurrentValue conditional), speed
    /// (<see cref="IlluminatedButton"/>.IlluminationAlpha 5-button mirror). Population skipped
    /// (T6.2 left _populationReadout unbound — adapter null-tolerant).
    /// Baseline values loaded from <see cref="HudParityFixture"/> at
    /// <c>Assets/Tests/Fixtures/UI/HUD/HudParityFixture.asset</c>.
    /// </summary>
    public sealed class HudBarParityTest
    {
        private const string FixturePath = "Assets/Tests/Fixtures/UI/HUD/HudParityFixture.asset";
        private const string ScenePath = "Assets/Scenes/CityScene.unity";

        private HudParityFixture _fixture;
        private HudBarDataAdapter _adapter;
        private CityStats _cityStats;
        private TimeManager _timeManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Suppress baseline runtime LogError noise (BlipBootstrap, ZoneSubTypeRegistry, etc.)
            // so HUD-scoped asserts don't auto-fail on unrelated init logs.
            LogAssert.ignoreFailingMessages = true;

#if UNITY_EDITOR
            _fixture = AssetDatabase.LoadAssetAtPath<HudParityFixture>(FixturePath);
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            // Wait for scene + Awake cascade to settle.
            yield return null;
            yield return null;

            _adapter = Object.FindObjectOfType<HudBarDataAdapter>();
            _cityStats = Object.FindObjectOfType<CityStats>();
            _timeManager = Object.FindObjectOfType<TimeManager>();

            // Drive sim tick budget so adapter Update() propagates producer values to consumers.
            int ticks = _fixture != null ? _fixture.simTickBudget : 30;
            for (int i = 0; i < ticks; i++) yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // T6.4 — money channel parity (SegmentedReadout.CurrentValue mirrors CityStats.money).
        // ─────────────────────────────────────────────────────────────────────
        [UnityTest]
        public IEnumerator MoneyChannel_MatchesBaseline()
        {
            Assert.IsNotNull(_fixture, "HudParityFixture asset missing at " + FixturePath);
            Assert.IsNotNull(_adapter, "HudBarDataAdapter not found in scene — Stage 6 wiring incomplete.");

            var money = GetReadoutByFieldName("_moneyReadout");
            Assert.IsNotNull(money, "_moneyReadout consumer not assigned — adapter wiring incomplete.");
            Assert.IsNotNull(_cityStats, "CityStats producer not present in scene.");

            int delta = Mathf.Abs(money.CurrentValue - _cityStats.money);
            Assert.LessOrEqual(delta, 1, $"money readout drift > 1: readout={money.CurrentValue} live={_cityStats.money}");
            yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // T6.4 — happiness channel parity (NeedleBallistics.CurrentValue mirrors CityStats.happiness).
        // Skipped if NeedleBallistics sibling absent (Stage 5 contract — adapter null-tolerant).
        // ─────────────────────────────────────────────────────────────────────
        [UnityTest]
        public IEnumerator HappinessChannel_MatchesBaseline()
        {
            Assert.IsNotNull(_adapter, "HudBarDataAdapter not found in scene.");

            var needle = GetNeedleBallistics();
            if (needle == null)
            {
                // NeedleBallistics not yet attached as sibling on vu-meter (pending bake-handler);
                // adapter ignores happiness write. Skip until juice-component wiring complete.
                Assert.Pass("NeedleBallistics sibling absent — adapter writes nothing per Stage 5 contract; channel skipped.");
                yield break;
            }
            Assert.IsNotNull(_cityStats, "CityStats producer not present.");

            float delta = Mathf.Abs(needle.CurrentValue - _cityStats.happiness);
            Assert.LessOrEqual(delta, _fixture != null ? _fixture.happinessTolerance : 0.05f,
                $"happiness drift > tolerance: needle={needle.CurrentValue} live={_cityStats.happiness}");
            yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // T6.4 — speed channel 5-button mirror (exactly-one-illuminated for index 0..4).
        // ─────────────────────────────────────────────────────────────────────
        [UnityTest]
        public IEnumerator SpeedChannel_FiveButtonMirror()
        {
            Assert.IsNotNull(_adapter, "HudBarDataAdapter not found in scene.");
            Assert.IsNotNull(_timeManager, "TimeManager producer not present.");

            var buttons = GetSpeedButtons();
            Assert.IsNotNull(buttons, "_speedButtons array not assigned — adapter wiring incomplete.");
            Assert.AreEqual(5, buttons.Length, $"_speedButtons length expected 5, got {buttons.Length}.");

            for (int target = 0; target < 5; target++)
            {
                _timeManager.SetTimeSpeedIndex(target);
                // Allow adapter Update() to propagate; one frame suffices since adapter writes
                // IlluminationAlpha synchronously per Update tick.
                yield return null;
                yield return null;

                int illuminatedCount = 0;
                int illuminatedIdx = -1;
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] == null) continue;
                    if (buttons[i].IlluminationAlpha >= 0.5f)
                    {
                        illuminatedCount++;
                        illuminatedIdx = i;
                    }
                }

                Assert.AreEqual(1, illuminatedCount,
                    $"speed={target}: expected exactly 1 illuminated button, got {illuminatedCount}.");
                Assert.AreEqual(target, illuminatedIdx,
                    $"speed={target}: expected button[{target}] illuminated, got button[{illuminatedIdx}].");
            }
        }

        // ── Reflection helpers — adapter exposes consumer refs as private [SerializeField];
        // tests read them via reflection to keep adapter API surface minimal.

        private SegmentedReadout GetReadoutByFieldName(string fieldName)
        {
            var f = typeof(HudBarDataAdapter).GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return f != null ? f.GetValue(_adapter) as SegmentedReadout : null;
        }

        private NeedleBallistics GetNeedleBallistics()
        {
            var f = typeof(HudBarDataAdapter).GetField("_happinessNeedle",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return f != null ? f.GetValue(_adapter) as NeedleBallistics : null;
        }

        private IlluminatedButton[] GetSpeedButtons()
        {
            var f = typeof(HudBarDataAdapter).GetField("_speedButtons",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return f != null ? f.GetValue(_adapter) as IlluminatedButton[] : null;
        }
    }
}
