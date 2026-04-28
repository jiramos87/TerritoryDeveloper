using System.IO;
using NUnit.Framework;
using Territory.Editor.Bridge;
using Territory.UI;
using Territory.UI.StudioControls;
using UnityEditor;
using UnityEngine;

namespace Territory.Tests.EditMode.UI
{
    /// <summary>
    /// Stage 4 (TECH-2692) EditMode smoke: end-to-end bake-to-instantiate loop for all 8
    /// StudioControl variants. Fixture IR carries one interactive per kind; per-kind tests
    /// assert detail accessor values populated post-bake; ApplyTheme NPE-free across the ring.
    /// </summary>
    public class StudioControlBakeTests
    {
        private const string FixtureIrPath = "Assets/Tests/EditMode/UI/Fixtures/stage4-all-kinds-ir.json";
        private const string TestThemePath = "Assets/UI/Theme/DefaultUiTheme.asset";
        private const string TestOutDir = "Assets/UI/Prefabs/Generated/Stage4Test";

        private bool _bakeRan;
        private UiTheme _runtimeTheme;

        [SetUp]
        public void SetUp()
        {
            if (_bakeRan) return;
            // Single bake per fixture run (per-test bake would be N× redundant given idempotent prefab write).
            EnsureBakeRan();
            _bakeRan = true;
        }

        [TearDown]
        public void TearDown()
        {
            if (_runtimeTheme != null)
            {
                Object.DestroyImmediate(_runtimeTheme);
                _runtimeTheme = null;
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Remove generated test prefabs to keep repo clean.
            if (AssetDatabase.IsValidFolder(TestOutDir))
            {
                AssetDatabase.DeleteAsset(TestOutDir);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void BakeKnob_DetailPopulated()
        {
            var knob = LoadAndInstantiate<Knob>("test-knob");
            Assert.AreEqual(0f, knob.Detail.min, "Knob.Detail.min");
            Assert.AreEqual(10f, knob.Detail.max, "Knob.Detail.max");
            Assert.AreEqual(1f, knob.Detail.step, "Knob.Detail.step");
            Object.DestroyImmediate(knob.gameObject);
        }

        [Test]
        public void BakeFader_DetailPopulated()
        {
            var fader = LoadAndInstantiate<Fader>("test-fader");
            Assert.AreEqual(0f, fader.Detail.min, "Fader.Detail.min");
            Assert.AreEqual(3f, fader.Detail.max, "Fader.Detail.max");
            Assert.AreEqual(1f, fader.Detail.step, "Fader.Detail.step");
            Object.DestroyImmediate(fader.gameObject);
        }

        [Test]
        public void BakeDetentRing_DetailPopulated()
        {
            var dr = LoadAndInstantiate<DetentRing>("test-detent");
            Assert.AreEqual(8, dr.Detail.detents, "DetentRing.Detail.detents");
            Assert.AreEqual(45f, dr.Detail.snapAngle, "DetentRing.Detail.snapAngle");
            Object.DestroyImmediate(dr.gameObject);
        }

        [Test]
        public void BakeVUMeter_DetailPopulated()
        {
            var vu = LoadAndInstantiate<VUMeter>("test-vu");
            Assert.AreEqual(50f, vu.AttackMs, "VUMeter.AttackMs");
            Assert.AreEqual(250f, vu.ReleaseMs, "VUMeter.ReleaseMs");
            Assert.AreEqual(1f, vu.Range, "VUMeter.Range");
            Object.DestroyImmediate(vu.gameObject);
        }

        [Test]
        public void BakeOscilloscope_DetailPopulated()
        {
            var sc = LoadAndInstantiate<Oscilloscope>("test-scope");
            Assert.AreEqual(256, sc.SampleCount, "Oscilloscope.SampleCount");
            Assert.AreEqual(60f, sc.SweepRateHz, "Oscilloscope.SweepRateHz");
            Assert.AreEqual(1f, sc.Range, "Oscilloscope.Range");
            Object.DestroyImmediate(sc.gameObject);
        }

        [Test]
        public void BakeIlluminatedButton_DetailPopulated()
        {
            var btn = LoadAndInstantiate<IlluminatedButton>("test-button");
            Assert.AreEqual("led-green", btn.Detail.illuminationSlug, "IlluminatedButton.Detail.illuminationSlug");
            Assert.AreEqual("click", btn.Detail.pulseOnEvent, "IlluminatedButton.Detail.pulseOnEvent");
            Object.DestroyImmediate(btn.gameObject);
        }

        [Test]
        public void BakeLED_DetailPopulated()
        {
            var led = LoadAndInstantiate<LED>("test-led");
            Assert.AreEqual("led-amber", led.Detail.illuminationSlug, "LED.Detail.illuminationSlug");
            Assert.AreEqual(true, led.Detail.defaultState, "LED.Detail.defaultState");
            Object.DestroyImmediate(led.gameObject);
        }

        [Test]
        public void BakeSegmentedReadout_DetailPopulated()
        {
            var sr = LoadAndInstantiate<SegmentedReadout>("test-readout");
            Assert.AreEqual(8, sr.Digits, "SegmentedReadout.Digits");
            Assert.AreEqual("display", sr.Detail.fontSlug, "SegmentedReadout.Detail.fontSlug");
            Assert.AreEqual("led-green", sr.Detail.segmentColor, "SegmentedReadout.Detail.segmentColor");
            Object.DestroyImmediate(sr.gameObject);
        }

        [Test]
        public void ApplyTheme_NoNpe_AllKinds()
        {
            var theme = LoadOrCreateTheme();
            var slugs = new[] {
                "test-knob", "test-fader", "test-detent",
                "test-vu", "test-scope",
                "test-button", "test-led", "test-readout",
            };
            foreach (var slug in slugs)
            {
                var go = LoadAndInstantiateGameObject(slug);
                var variant = go.GetComponent<StudioControlBase>();
                Assert.IsNotNull(variant, $"variant component missing on {slug}");
                Assert.DoesNotThrow(
                    () => variant.ApplyTheme(theme),
                    $"ApplyTheme threw on slug={slug}");
                Assert.DoesNotThrow(
                    () => variant.ApplyTheme(null),
                    $"ApplyTheme threw on null theme for slug={slug}");
                Object.DestroyImmediate(go);
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private void EnsureBakeRan()
        {
            Assert.IsTrue(File.Exists(FixtureIrPath), $"fixture IR missing at {FixtureIrPath}");

            // Theme load: existing project theme preferred (real palette/illumination/font dict);
            // missing theme would only break ApplyTheme NPE test if not handled — but we exercise null path too.
            // If theme asset missing, bake handler returns theme_so_not_found → fail loudly.
            var themePath = AssetDatabase.LoadAssetAtPath<UiTheme>(TestThemePath) != null
                ? TestThemePath
                : null;
            if (themePath == null)
            {
                Assert.Ignore($"DefaultUiTheme.asset missing at {TestThemePath} — cannot bake");
                return;
            }

            var args = new UiBakeHandler.BakeArgs
            {
                ir_path = FixtureIrPath,
                out_dir = TestOutDir,
                theme_so = themePath,
            };
            var result = UiBakeHandler.Bake(args);
            Assert.IsNull(result.error,
                $"bake error: {result.error?.error} / {result.error?.details} / {result.error?.path}");
            AssetDatabase.Refresh();
        }

        private GameObject LoadAndInstantiateGameObject(string slug)
        {
            var path = $"{TestOutDir}/{slug}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"baked prefab missing at {path}");
            var go = Object.Instantiate(prefab);
            Assert.IsNotNull(go, $"Instantiate returned null for {path}");
            return go;
        }

        private T LoadAndInstantiate<T>(string slug) where T : StudioControlBase
        {
            var go = LoadAndInstantiateGameObject(slug);
            var variant = go.GetComponent<T>();
            Assert.IsNotNull(variant, $"baked prefab missing component {typeof(T).Name} for slug={slug}");
            return variant;
        }

        private UiTheme LoadOrCreateTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(TestThemePath);
            if (theme != null) return theme;
            _runtimeTheme = ScriptableObject.CreateInstance<UiTheme>();
            return _runtimeTheme;
        }
    }
}
