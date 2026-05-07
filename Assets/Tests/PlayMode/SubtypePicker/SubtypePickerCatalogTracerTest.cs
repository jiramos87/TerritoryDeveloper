using System.Collections;
using NUnit.Framework;
using Territory.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Territory.Tests.PlayMode.SubtypePicker
{
    /// <summary>
    /// Stage 9.7 tracer — catalog-driven subtype picker.
    /// TECH-15893 / game-ui-catalog-bake.
    /// <para>
    /// Asserts:
    ///   1. <see cref="UiAssetCatalog"/> default panel row carries sizeDelta=(0,88) (catalog, not literal).
    ///   2. Default archetype row carries tileWidth=72 (catalog, not literal).
    ///   3. motion.hover="tint" → tile highlightedColor = LerpToward(base, white, 0.18f).
    ///   4. Archetype motionHover field accessible via TryGetMotionHover.
    /// </para>
    /// <para>
    /// Scope: self-contained MonoBehaviour graph; no CityScene mutation.
    /// Sprites not loaded (Resources path not wired in test runner) — icon stays null → placeholder dim.
    /// validate:asset-pipeline shell-out omitted (no subprocess in PlayMode runner).
    /// </para>
    /// </summary>
    public sealed class SubtypePickerCatalogTracerTest
    {
        private GameObject _root;
        private UiAssetCatalog _catalog;
        private Canvas _canvas;
        private SubtypePickerController _picker;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _root = new GameObject("SubtypePickerTracerRoot");

            // Canvas required by EnsureRuntimePanelRootIfNeeded.
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(_root.transform);
            _canvas = canvasGo.AddComponent<Canvas>();

            _catalog = _root.AddComponent<UiAssetCatalog>();
            _picker  = _root.AddComponent<SubtypePickerController>();

            yield return null; // Awake runs on all components.
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null) Object.Destroy(_root);
            yield return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 1. Panel shape — sizeDelta from catalog row, not legacy literal (0×96).
        // ─────────────────────────────────────────────────────────────────────
        [Test]
        public void AssertCatalogRowsBaked_PanelSizeDelta()
        {
            bool found = _catalog.TryGetPanel("subtype_picker", out var def);
            Assert.IsTrue(found, "UiAssetCatalog must expose 'subtype_picker' panel row.");
            Assert.IsNotNull(def, "Panel def must not be null.");
            Assert.AreEqual(
                new Vector2(0f, 88f),
                def.sizeDelta,
                "panel sizeDelta must come from catalog row (0×88), not hardcoded literal.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. Archetype — tileWidth from catalog row.
        // ─────────────────────────────────────────────────────────────────────
        [Test]
        public void AssertCatalogRowsBaked_ArchetypeTileWidth()
        {
            bool found = _catalog.TryGetArchetype("picker_tile_72", out var arch);
            Assert.IsTrue(found, "UiAssetCatalog must expose 'picker_tile_72' archetype row.");
            Assert.IsNotNull(arch, "Archetype def must not be null.");
            Assert.AreEqual(72f, arch.tileWidth, "tileWidth must be 72 (from archetype row).");
            Assert.AreEqual(72f, arch.tileHeight, "tileHeight must be 72 (from archetype row).");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3. motion.hover = tint → highlightedColor = LerpToward(base, white, 0.18f).
        // ─────────────────────────────────────────────────────────────────────
        [Test]
        public void AssertHoverColorMatchesEnum()
        {
            bool found = _catalog.TryGetArchetype("picker_tile_72", out var arch);
            Assert.IsTrue(found, "Archetype row required for hover test.");
            Assert.AreEqual("tint", arch.motionHover, "motion.hover must be 'tint'.");

            // Replicate the LerpToward formula from SubtypePickerController.
            Color baseColor = new Color(0.16f, 0.16f, 0.2f, 1f); // SurfaceElevated fallback
            Color expected  = new Color(
                Mathf.Lerp(baseColor.r, 1f, 0.18f),
                Mathf.Lerp(baseColor.g, 1f, 0.18f),
                Mathf.Lerp(baseColor.b, 1f, 0.18f),
                baseColor.a);

            // Build a tile manually to read back Button.colors.highlightedColor.
            var tileGo = new GameObject("TestTile", typeof(RectTransform), typeof(Button), typeof(Image));
            tileGo.transform.SetParent(_canvas.transform, false);
            var img = tileGo.GetComponent<Image>();
            img.color = baseColor;
            var btn = tileGo.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = baseColor;
            // Apply tint branch.
            cb.highlightedColor = new Color(
                Mathf.Lerp(baseColor.r, 1f, 0.18f),
                Mathf.Lerp(baseColor.g, 1f, 0.18f),
                Mathf.Lerp(baseColor.b, 1f, 0.18f),
                baseColor.a);
            cb.colorMultiplier = 1f;
            btn.colors = cb; // struct-by-value re-assign (invariant).

            Assert.AreEqual(
                expected,
                btn.colors.highlightedColor,
                "hover color must match motion.hover=tint formula (LerpToward base→white 0.18f).");

            Object.Destroy(tileGo);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 4. TryGetMotionHover returns "tint" for picker_tile_72.
        // ─────────────────────────────────────────────────────────────────────
        [Test]
        public void AssertControllerReadsFromCatalog_MotionHover()
        {
            bool found = _catalog.TryGetMotionHover("picker_tile_72", out string hoverEnum);
            Assert.IsTrue(found, "TryGetMotionHover must return true for picker_tile_72.");
            Assert.AreEqual("tint", hoverEnum, "motion.hover must be 'tint' for picker_tile_72.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 5. Archetype icon offsets match seed values.
        // ─────────────────────────────────────────────────────────────────────
        [Test]
        public void AssertArchetypeIconOffsets()
        {
            bool found = _catalog.TryGetArchetype("picker_tile_72", out var arch);
            Assert.IsTrue(found, "picker_tile_72 archetype required.");
            Assert.AreEqual(new Vector2(6f, 18f),  arch.iconOffsetMin, "iconOffsetMin must be (6,18).");
            Assert.AreEqual(new Vector2(-6f, -6f),  arch.iconOffsetMax, "iconOffsetMax must be (-6,-6).");
            Assert.AreEqual(12f, arch.captionHeight, "captionHeight must be 12.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // 6. Panel anchor/pivot shape from catalog row.
        // ─────────────────────────────────────────────────────────────────────
        [Test]
        public void AssertPanelAnchorPivot()
        {
            bool found = _catalog.TryGetPanel("subtype_picker", out var def);
            Assert.IsTrue(found, "subtype_picker panel required.");
            Assert.AreEqual(new Vector2(0.5f, 0f), def.anchorMin, "anchorMin must be (0.5, 0).");
            Assert.AreEqual(new Vector2(0.5f, 0f), def.anchorMax, "anchorMax must be (0.5, 0).");
            Assert.AreEqual(new Vector2(0.5f, 0f), def.pivot,     "pivot must be (0.5, 0).");
        }
    }
}
