using System;
using NUnit.Framework;
using Territory.Editor.Bridge;

namespace Territory.Tests.EditMode.UI
{
    /// <summary>
    /// TECH-17992 / game-ui-catalog-bake Stage 9.10 T3.
    /// Unit tests for PanelSnapshot DTO round-trip (JsonUtility parse) and
    /// MapLayoutTemplate hard-fail on missing layout_template.
    /// No Editor dependencies — uses JsonUtility which is available in EditMode.
    /// </summary>
    public class UiBakeHandlerDtoRoundtripTests
    {
        // ── Minimal valid panels.json (schema_version 3, one hud_bar item) ──────

        private const string ValidPanelsJson = @"{
            ""snapshot_id"": ""test-001"",
            ""kind"": ""game-ui-panels"",
            ""schema_version"": 3,
            ""items"": [
                {
                    ""slug"": ""hud_bar"",
                    ""fields"": {
                        ""layout_template"": ""hstack"",
                        ""layout"": ""hstack"",
                        ""gap_px"": 4,
                        ""padding_json"": ""{}"",
                        ""params_json"": """"
                    },
                    ""children"": [
                        { ""ord"": 1, ""kind"": ""button"", ""layout_json"": ""{\""\zone\"":\""left\""}"" }
                    ]
                }
            ]
        }";

        private const string NullLayoutTemplateJson = @"{
            ""snapshot_id"": ""test-002"",
            ""kind"": ""game-ui-panels"",
            ""schema_version"": 3,
            ""items"": [
                {
                    ""slug"": ""bad_panel"",
                    ""fields"": {
                        ""layout_template"": """",
                        ""layout"": ""vstack""
                    },
                    ""children"": []
                }
            ]
        }";

        // ── ParsePanelSnapshot tests ─────────────────────────────────────────────

        [Test]
        public void test_panel_snapshot_roundtrip_valid_json_returns_snapshot()
        {
            var (snapshot, error) = UiBakeHandler.ParsePanelSnapshot(ValidPanelsJson);

            Assert.IsNull(error, $"expected no error, got: {error?.error} — {error?.details}");
            Assert.IsNotNull(snapshot, "snapshot must not be null");
            Assert.AreEqual(3, snapshot.schema_version, "schema_version must be 3");
            Assert.IsNotNull(snapshot.items, "items must not be null");
            Assert.AreEqual(1, snapshot.items.Length, "expected 1 item");
        }

        [Test]
        public void test_panel_snapshot_roundtrip_slug_and_fields()
        {
            var (snapshot, _) = UiBakeHandler.ParsePanelSnapshot(ValidPanelsJson);

            var item = snapshot.items[0];
            Assert.AreEqual("hud_bar", item.slug, "item.slug must be hud_bar");
            Assert.IsNotNull(item.fields, "item.fields must not be null");
            Assert.AreEqual("hstack", item.fields.layout_template, "fields.layout_template must be hstack");
            Assert.AreEqual(4, item.fields.gap_px, "fields.gap_px must be 4");
        }

        [Test]
        public void test_panel_snapshot_roundtrip_child_layout_json()
        {
            var (snapshot, _) = UiBakeHandler.ParsePanelSnapshot(ValidPanelsJson);

            var child = snapshot.items[0].children[0];
            Assert.AreEqual(1, child.ord, "child.ord must be 1");
            Assert.IsNotNull(child.layout_json, "child.layout_json must not be null");
        }

        [Test]
        public void test_panel_snapshot_empty_json_returns_error()
        {
            var (snapshot, error) = UiBakeHandler.ParsePanelSnapshot(string.Empty);

            Assert.IsNull(snapshot, "snapshot must be null on empty input");
            Assert.IsNotNull(error, "error must not be null on empty input");
            Assert.AreEqual("schema_violation", error.error);
        }

        [Test]
        public void test_panel_snapshot_whitespace_json_returns_error()
        {
            var (snapshot, error) = UiBakeHandler.ParsePanelSnapshot("   \n  ");

            Assert.IsNull(snapshot);
            Assert.IsNotNull(error);
            Assert.AreEqual("schema_violation", error.error);
        }

        // ── ExtractZone tests ────────────────────────────────────────────────────

        [Test]
        public void test_extract_zone_left()
        {
            string zone = UiBakeHandler.ExtractZone("{\"zone\":\"left\"}");
            Assert.AreEqual("left", zone);
        }

        [Test]
        public void test_extract_zone_center()
        {
            string zone = UiBakeHandler.ExtractZone("{\"zone\":\"center\"}");
            Assert.AreEqual("center", zone);
        }

        [Test]
        public void test_extract_zone_right()
        {
            string zone = UiBakeHandler.ExtractZone("{\"zone\":\"right\"}");
            Assert.AreEqual("right", zone);
        }

        [Test]
        public void test_extract_zone_null_input_returns_null()
        {
            Assert.IsNull(UiBakeHandler.ExtractZone(null));
        }

        [Test]
        public void test_extract_zone_empty_input_returns_null()
        {
            Assert.IsNull(UiBakeHandler.ExtractZone(string.Empty));
        }

        [Test]
        public void test_extract_zone_missing_key_returns_null()
        {
            Assert.IsNull(UiBakeHandler.ExtractZone("{\"other\":\"value\"}"));
        }

        // ── MapLayoutTemplate tests ──────────────────────────────────────────────

        [Test]
        public void test_map_layout_template_hstack_returns_hlg()
        {
            var t = UiBakeHandler.MapLayoutTemplate("hstack", "test_panel");
            Assert.AreEqual(typeof(UnityEngine.UI.HorizontalLayoutGroup), t);
        }

        [Test]
        public void test_map_layout_template_vstack_returns_vlg()
        {
            var t = UiBakeHandler.MapLayoutTemplate("vstack", "test_panel");
            Assert.AreEqual(typeof(UnityEngine.UI.VerticalLayoutGroup), t);
        }

        [Test]
        public void test_map_layout_template_grid_returns_glg()
        {
            var t = UiBakeHandler.MapLayoutTemplate("grid", "test_panel");
            Assert.AreEqual(typeof(UnityEngine.UI.GridLayoutGroup), t);
        }

        [Test]
        public void test_map_layout_template_null_throws_layout_template_missing()
        {
            var ex = Assert.Throws<Exception>(() =>
                UiBakeHandler.MapLayoutTemplate(null, "bad_panel"));
            StringAssert.Contains("bake.layout_template_missing", ex.Message);
        }

        [Test]
        public void test_map_layout_template_empty_throws_layout_template_missing()
        {
            var ex = Assert.Throws<Exception>(() =>
                UiBakeHandler.MapLayoutTemplate(string.Empty, "bad_panel"));
            StringAssert.Contains("bake.layout_template_missing", ex.Message);
        }
    }
}
