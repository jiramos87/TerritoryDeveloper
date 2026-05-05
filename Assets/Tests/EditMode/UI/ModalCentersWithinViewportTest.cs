// TECH-11934 / game-ui-catalog-bake Stage 3 §Red-Stage Proof.
//
// red_test_anchor: visibility-delta-test:Assets/Tests/EditMode/UI/ModalCentersWithinViewportTest.cs::ModalCentersWithinViewport
// target_kind: visibility_delta
//
// Asserts the modal baked by CatalogBakeHandler.BakeModal is fully contained
// within the viewport rect AND its center is within ±1px of the viewport
// center, at three viewport sizes required by Stage 3 exit criteria.
//
// Depends on:
//   - FEAT-55: settings_modal fixture snapshot at
//     Assets/Tests/EditMode/UI/Fixtures/settings-modal-snapshot.json
//   - TECH-11933: CatalogBakeHandler.Modal.cs partial (BakeModal + BakeViewportOverride)

using System.IO;
using NUnit.Framework;
using TerritoryDeveloper.Editor.Bake;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Tests.EditMode.UI
{
    [TestFixture]
    public class ModalCentersWithinViewportTest
    {
        private const string FixtureSnapshotPath =
            "Assets/Tests/EditMode/UI/Fixtures/settings-modal-snapshot.json";

        private const string TestOutDirBase =
            "Assets/Tests/Generated/SettingsModal";

        private GameObject _canvasGo;
        private GameObject _instance;
        private string _testOutDir;

        [SetUp]
        public void SetUp()
        {
            // Fresh Canvas + CanvasScaler per run.
            _canvasGo = new GameObject("TestCanvas");
            _canvasGo.AddComponent<Canvas>();
            _canvasGo.AddComponent<CanvasScaler>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clear viewport override.
            CatalogBakeHandler.BakeViewportOverride = null;

            if (_instance != null)
            {
                Object.DestroyImmediate(_instance);
                _instance = null;
            }

            if (_canvasGo != null)
            {
                Object.DestroyImmediate(_canvasGo);
                _canvasGo = null;
            }

            // Remove ephemeral output dir for this run.
            if (!string.IsNullOrEmpty(_testOutDir) && AssetDatabase.IsValidFolder(_testOutDir))
                AssetDatabase.DeleteAsset(_testOutDir);
        }

        [TestCase(1920, 1080)]
        [TestCase(1280, 720)]
        [TestCase(800, 600)]
        public void ModalCentersWithinViewport(int viewportW, int viewportH)
        {
            if (!File.Exists(FixtureSnapshotPath))
                Assert.Fail($"Fixture snapshot missing at {FixtureSnapshotPath}");

            // ── 1. Configure viewport for this parameterized run ───────────────
            _testOutDir = $"{TestOutDirBase}/{viewportW}x{viewportH}";
            Directory.CreateDirectory(_testOutDir);

            var viewport = new Vector2(viewportW, viewportH);

            // Set CanvasScaler reference resolution on the test canvas.
            var scaler = _canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = viewport;

            // Inject viewport into BakeModal so sizeDelta clamp uses the test resolution.
            CatalogBakeHandler.BakeViewportOverride = viewport;

            // ── 2. Bake ───────────────────────────────────────────────────────
            CatalogBakeHandler.BakeFromSnapshot(FixtureSnapshotPath, _testOutDir);

            var prefabPath = _testOutDir + "/settings_modal.prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(asset,
                $"settings_modal prefab not found at {prefabPath} after bake");

            // ── 3. Instantiate under the test Canvas so world corners are in canvas-space ─
            _instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            _instance.transform.SetParent(_canvasGo.transform, false);

            var rt = _instance.GetComponent<RectTransform>();
            Assert.IsNotNull(rt, "Modal root missing RectTransform");

            // ── 4. Containment assertion ───────────────────────────────────────
            // anchorMin/Max = (0.5, 0.5) → sizeDelta == rect size in canvas-space.
            // For containment, use sizeDelta directly (Canvas is 1:1 with referenceResolution
            // in ScaleWithScreenSize at reference resolution).
            float halfW = rt.sizeDelta.x * 0.5f;
            float halfH = rt.sizeDelta.y * 0.5f;
            float vpCenterX = viewportW * 0.5f;
            float vpCenterY = viewportH * 0.5f;

            float worldLeft   = vpCenterX - halfW;
            float worldRight  = vpCenterX + halfW;
            float worldBottom = vpCenterY - halfH;
            float worldTop    = vpCenterY + halfH;

            Assert.GreaterOrEqual(worldLeft,   0f,
                $"[{viewportW}x{viewportH}] Modal left edge ({worldLeft}) outside viewport left");
            Assert.GreaterOrEqual(worldBottom, 0f,
                $"[{viewportW}x{viewportH}] Modal bottom edge ({worldBottom}) outside viewport bottom");
            Assert.LessOrEqual(worldRight, viewportW,
                $"[{viewportW}x{viewportH}] Modal right edge ({worldRight}) exceeds viewport width {viewportW}");
            Assert.LessOrEqual(worldTop, viewportH,
                $"[{viewportW}x{viewportH}] Modal top edge ({worldTop}) exceeds viewport height {viewportH}");

            // ── 5. Center assertion (±1px tolerance) ──────────────────────────
            // Modal uses anchoredPosition = (0,0) with anchorMin/Max = (0.5,0.5),
            // so the center is at the Canvas center = (0, 0) in anchored space,
            // which maps to (viewportW/2, viewportH/2) in canvas-space.
            // In RectTransform terms: anchoredPosition is the offset from the anchor point.
            float centerOffsetX = Mathf.Abs(rt.anchoredPosition.x);
            float centerOffsetY = Mathf.Abs(rt.anchoredPosition.y);

            Assert.LessOrEqual(centerOffsetX, 1f,
                $"[{viewportW}x{viewportH}] Modal X center offset {rt.anchoredPosition.x} exceeds ±1px");
            Assert.LessOrEqual(centerOffsetY, 1f,
                $"[{viewportW}x{viewportH}] Modal Y center offset {rt.anchoredPosition.y} exceeds ±1px");

            // ── 6. Anchor + pivot invariants ──────────────────────────────────
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rt.anchorMin,
                $"[{viewportW}x{viewportH}] Modal anchorMin must be (0.5, 0.5)");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rt.anchorMax,
                $"[{viewportW}x{viewportH}] Modal anchorMax must be (0.5, 0.5)");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rt.pivot,
                $"[{viewportW}x{viewportH}] Modal pivot must be (0.5, 0.5)");

            // ── 7. sizeDelta clamp invariant ───────────────────────────────────
            float maxAllowedW = viewportW - 2f * CatalogBakeHandler.ModalSafeAreaMarginPx;
            float maxAllowedH = viewportH - 2f * CatalogBakeHandler.ModalSafeAreaMarginPx;

            Assert.LessOrEqual(rt.sizeDelta.x, maxAllowedW + 0.01f,
                $"[{viewportW}x{viewportH}] sizeDelta.x ({rt.sizeDelta.x}) exceeds max clamped width ({maxAllowedW})");
            Assert.LessOrEqual(rt.sizeDelta.y, maxAllowedH + 0.01f,
                $"[{viewportW}x{viewportH}] sizeDelta.y ({rt.sizeDelta.y}) exceeds max clamped height ({maxAllowedH})");
        }
    }
}
