// TECH-23571 / game-ui-catalog-bake Stage 9.15
//
// §Red-Stage Proof:
// Assets/Tests/EditMode/UI/PanelSnapshotChildInstanceSlugTest.cs::PanelSnapshotChild_Deserializes_InstanceSlug
//
// Red: snapshot DTO drops instance_slug field (field absent on PanelSnapshotChild).
// Green: round-trip preserves instance_slug for every child row.

using NUnit.Framework;
using Territory.Editor.Bridge;
using UnityEngine;

namespace Territory.Tests.EditMode.UI
{
    public class PanelSnapshotChildInstanceSlugTest
    {
        private const string SampleJson = @"{
  ""snapshot_id"": ""2026-01-01T00:00:00.000Z"",
  ""kind"": ""panels"",
  ""schema_version"": 4,
  ""items"": [
    {
      ""slug"": ""hud-bar"",
      ""fields"": {
        ""layout_template"": ""hstack"",
        ""layout"": ""hstack"",
        ""gap_px"": 8,
        ""padding_json"": ""{}"",
        ""params_json"": """"
      },
      ""children"": [
        {
          ""ord"": 1,
          ""kind"": ""illuminated-button"",
          ""params_json"": """",
          ""sprite_ref"": """",
          ""layout_json"": ""{\""\""zone\"":\""left\""}\"""",
          ""instance_slug"": ""hud-bar-zoom-in-button""
        },
        {
          ""ord"": 2,
          ""kind"": ""illuminated-button"",
          ""params_json"": """",
          ""sprite_ref"": """",
          ""layout_json"": ""{\""\""zone\"":\""right\""}\"""",
          ""instance_slug"": ""hud-bar-pause-button""
        },
        {
          ""ord"": 3,
          ""kind"": ""illuminated-button"",
          ""params_json"": """",
          ""sprite_ref"": """",
          ""layout_json"": null,
          ""instance_slug"": null
        }
      ]
    }
  ]
}";

        /// <summary>
        /// Round-trip: parse panels.json JSON with instance_slug per child.
        /// Asserts instance_slug preserved (not dropped) after JsonUtility deserialization.
        /// §Red-Stage Proof surface keywords: PanelSnapshotChild, instance_slug, Green (round-trip preserves).
        /// </summary>
        [Test]
        public void PanelSnapshotChild_Deserializes_InstanceSlug()
        {
            var (snapshot, error) = UiBakeHandler.ParsePanelSnapshot(SampleJson);

            Assert.IsNull(error, $"ParsePanelSnapshot returned error: {error?.error} — {error?.details}");
            Assert.IsNotNull(snapshot, "snapshot must not be null");
            Assert.IsNotNull(snapshot.items, "items must not be null");
            Assert.AreEqual(1, snapshot.items.Length, "expected 1 item");

            var item = snapshot.items[0];
            Assert.AreEqual("hud-bar", item.slug, "item slug mismatch");
            Assert.IsNotNull(item.children, "children must not be null");
            Assert.AreEqual(3, item.children.Length, "expected 3 children");

            // Child 0 — instance_slug present.
            var c0 = item.children[0];
            Assert.AreEqual(1, c0.ord, "child[0].ord mismatch");
            Assert.AreEqual("hud-bar-zoom-in-button", c0.instance_slug,
                "child[0].instance_slug round-trip failed — field dropped by DTO");

            // Child 1 — instance_slug present.
            var c1 = item.children[1];
            Assert.AreEqual("hud-bar-pause-button", c1.instance_slug,
                "child[1].instance_slug round-trip failed");

            // Child 2 — instance_slug null (JsonUtility deserializes null/absent string as empty string — acceptable).
            var c2 = item.children[2];
            Assert.IsTrue(c2.instance_slug == null || c2.instance_slug == string.Empty,
                $"child[2].instance_slug expected null/empty, got '{c2.instance_slug}'");
        }

        [Test]
        public void PanelSnapshotChild_InstanceSlug_Field_Exists_OnDto()
        {
            // Compile-time check: field accessible.
            var child = new UiBakeHandler.PanelSnapshotChild
            {
                ord = 1,
                kind = "illuminated-button",
                instance_slug = "hud-bar-zoom-in-button",
            };
            Assert.AreEqual("hud-bar-zoom-in-button", child.instance_slug,
                "instance_slug field must exist and be assignable on PanelSnapshotChild DTO");
        }
    }
}
