using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using Territory.UI.HUD;
using Territory.UI.Juice;
using Territory.UI.StudioControls;
using Territory.UI.StudioControls.Renderers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Territory.Tests.PlayMode.UI.HUD
{
    /// <summary>
    /// Stage 10 HUD visual smoke PlayMode test (TECH-2968) — closes the visibility regression hole
    /// that <see cref="HudBarParityTest"/> silently passed through. Asserts:
    ///   1. Each rendered StudioControl child of `hud-bar` (vu-meter / illuminated-button /
    ///      segmented-readout kinds — the 3 kinds T10.3 injects renderers into) has a child
    ///      <see cref="Image"/> with <c>color.a &gt; 0</c>.
    ///   2. <see cref="TMP_Text"/> on city-name + money readouts is non-null + non-empty + not
    ///      placeholder default. Population readout deferred to Stage 11 per Route B amendment
    ///      (history_id 810) — adapter does not write to it; test does not assert on it.
    ///   3. Needle <see cref="RectTransform.localRotation"/> Z component reflects the
    ///      <see cref="VUMeterRenderer"/>-mapped angle from <see cref="NeedleBallistics.CurrentValue"/>
    ///      (mapping = <c>Mathf.Lerp(45f, -45f, Clamp01(value))</c>).
    /// </summary>
    public sealed class HudBarVisualSmokeTest
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";
        private const string HudBarName = "hud-bar";
        private const float NeedleAngleToleranceDegrees = 2.0f;
        private const float DefaultMinAngle = 45f;
        private const float DefaultMaxAngle = -45f;

        private GameObject _hudBar;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Suppress baseline runtime LogError noise so HUD-scoped asserts don't auto-fail
            // on unrelated init logs (matches HudBarParityTest convention).
            LogAssert.ignoreFailingMessages = true;

#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#endif
            // Wait for scene load + Awake cascade (renderers fire OnStateApplied at Awake).
            yield return null;
            yield return null;

            _hudBar = GameObject.Find(HudBarName);
            // Allow another frame for renderer pushes to settle into TMP / Image targets.
            yield return null;
        }

        [UnityTest]
        public IEnumerator HudBar_Reachable_UnderUiCanvas()
        {
            Assert.IsNotNull(_hudBar, $"{HudBarName} GameObject not found in scene — Stage 10 reparent incomplete.");
            var parent = _hudBar.transform.parent;
            Assert.IsNotNull(parent, $"{HudBarName} has no parent — should be child of UI Canvas.");
            Assert.AreEqual("UI Canvas", parent.name,
                $"{HudBarName} parent expected 'UI Canvas', got '{parent.name}'.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RenderedChildren_NonZeroImageAlpha()
        {
            Assert.IsNotNull(_hudBar, $"{HudBarName} not found.");

            // Walk direct hud-bar StudioControl children. Filter to the 3 kinds T10.3 injects
            // renderers into: VUMeter, IlluminatedButton, SegmentedReadout. Other 5 kinds have
            // no Stage 10 renderer; asserting on them would be a false-fail.
            var rendered = new List<StudioControlBase>();
            var allControls = _hudBar.GetComponentsInChildren<StudioControlBase>(true);
            for (int i = 0; i < allControls.Length; i++)
            {
                var ctrl = allControls[i];
                if (ctrl == null) continue;
                if (ctrl.gameObject == _hudBar) continue;
                if (ctrl is VUMeter || ctrl is IlluminatedButton || ctrl is SegmentedReadout)
                {
                    rendered.Add(ctrl);
                }
            }

            Assert.Greater(rendered.Count, 0,
                "No rendered StudioControl children found under hud-bar (vu-meter / illuminated-button / segmented-readout). T10.3 bake-time injection incomplete?");

            foreach (var ctrl in rendered)
            {
                var img = ctrl.GetComponentInChildren<Image>(true);
                Assert.IsNotNull(img,
                    $"{ctrl.name} ({ctrl.GetType().Name}): no child Image found — renderer child injection missing (T10.3 / T10.5 gap).");
                Assert.Greater(img.color.a, 0f,
                    $"{ctrl.name} ({ctrl.GetType().Name}): child Image alpha = {img.color.a:F3}, expected > 0.");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Readouts_TextNonEmpty()
        {
            Assert.IsNotNull(_hudBar, $"{HudBarName} not found.");

            var adapter = Object.FindObjectOfType<HudBarDataAdapter>();
            Assert.IsNotNull(adapter, "HudBarDataAdapter not found in scene.");

            // money readout — adapter writes CurrentValue from CityStats.money each Update.
            var money = GetReadoutByFieldName(adapter, "_moneyReadout");
            Assert.IsNotNull(money, "_moneyReadout consumer ref missing on adapter.");
            AssertReadoutTextNonEmpty(money, "money");

            // city-name readout — find any other SegmentedReadout child of hud-bar that is not
            // the money readout. Renderer pushes CurrentValue.ToString("D{digits}") at Awake;
            // text non-empty even when adapter does not drive it.
            SegmentedReadout cityName = null;
            var readouts = _hudBar.GetComponentsInChildren<SegmentedReadout>(true);
            for (int i = 0; i < readouts.Length; i++)
            {
                if (readouts[i] == money) continue;
                cityName = readouts[i];
                break;
            }
            Assert.IsNotNull(cityName,
                "No second SegmentedReadout under hud-bar — city-name readout missing.");
            AssertReadoutTextNonEmpty(cityName, "city-name");

            // population readout deferred to Stage 11 per Route B amendment (history_id 810);
            // adapter._populationReadout is null, no assertion here.

            yield return null;
        }

        [UnityTest]
        public IEnumerator HappinessNeedle_LocalRotation_MatchesMapping()
        {
            Assert.IsNotNull(_hudBar, $"{HudBarName} not found.");

            var meter = _hudBar.GetComponentInChildren<VUMeter>(true);
            Assert.IsNotNull(meter, "No VUMeter child found under hud-bar.");

            var ballistics = meter.GetComponent<NeedleBallistics>();
            Assert.IsNotNull(ballistics,
                "VUMeter has no sibling NeedleBallistics — T10.3 bake / juice attach incomplete.");

            var renderer = meter.GetComponent<VUMeterRenderer>();
            Assert.IsNotNull(renderer,
                "VUMeter has no sibling VUMeterRenderer — T10.3 renderer injection incomplete.");

            // Resolve needle child rect (renderer uses Find("needle") then name-walk).
            var needleRect = ResolveNeedleRect(meter.transform);
            Assert.IsNotNull(needleRect,
                "No 'needle' child RectTransform under vu-meter — render-child injection incomplete.");

            // Drive the needle to a known target so renderer Update writes a non-default angle.
            // Use 0.75f — a value clearly distinct from 0.0f initial (so a stuck needle fails).
            ballistics.TargetValue = 0.75f;
            // Wait for ballistics critical-damped step + renderer Update tick.
            for (int i = 0; i < 60; i++) yield return null;

            float observed = NormalizeDegrees(needleRect.localRotation.eulerAngles.z);
            float expected = NormalizeDegrees(
                Mathf.Lerp(DefaultMinAngle, DefaultMaxAngle, Mathf.Clamp01(ballistics.CurrentValue)));
            float delta = Mathf.Abs(Mathf.DeltaAngle(observed, expected));

            Assert.LessOrEqual(delta, NeedleAngleToleranceDegrees,
                $"needle rotation Z drift > tolerance: observed={observed:F2} expected={expected:F2} (CurrentValue={ballistics.CurrentValue:F3})");
        }

        // ── helpers ─────────────────────────────────────────────────────────

        private static float NormalizeDegrees(float deg)
        {
            // Map to [-180, 180] for symmetric tolerance check.
            deg %= 360f;
            if (deg > 180f) deg -= 360f;
            if (deg < -180f) deg += 360f;
            return deg;
        }

        private static RectTransform ResolveNeedleRect(Transform meterRoot)
        {
            var direct = meterRoot.Find("needle");
            if (direct != null) return direct as RectTransform;
            var rects = meterRoot.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                var r = rects[i];
                if (r == null) continue;
                if (r.transform == meterRoot) continue;
                if (r.gameObject.name == "needle") return r;
            }
            return null;
        }

        private static SegmentedReadout GetReadoutByFieldName(HudBarDataAdapter adapter, string fieldName)
        {
            var f = typeof(HudBarDataAdapter).GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return f != null ? f.GetValue(adapter) as SegmentedReadout : null;
        }

        private static void AssertReadoutTextNonEmpty(SegmentedReadout readout, string label)
        {
            var tmp = readout.GetComponentInChildren<TMP_Text>(true);
            Assert.IsNotNull(tmp, $"{label} readout: no child TMP_Text — bake-handler text child injection missing.");
            Assert.IsFalse(string.IsNullOrEmpty(tmp.text),
                $"{label} readout: TMP_Text.text empty / null.");
            Assert.AreNotEqual("Label", tmp.text,
                $"{label} readout: TMP_Text.text still placeholder default 'Label'.");
        }
    }
}
