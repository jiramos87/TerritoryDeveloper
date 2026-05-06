// TECH-17993 / game-ui-catalog-bake Stage 9.10 T4 §Red-Stage Proof.
//
// Re-bakes hud-bar via UiBakeHandler.BakeFromPanelSnapshot (panels.json path).
// Asserts: prefab tree has Left/Center/Right slot-wrapper children,
// each with HorizontalLayoutGroup; BUDGET button parented under Left.

using System.IO;
using NUnit.Framework;
using Territory.Editor.Bridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.Tests.EditMode.UI
{
    public class UiBakeHudBarLayoutTests
    {
        private const string PanelsPath = "Assets/UI/Snapshots/panels.json";
        private const string TestThemePath = "Assets/UI/Theme/DefaultUiTheme.asset";
        private const string TestOutDir = "Assets/UI/Prefabs/Generated/_test_HudBarSnapshot";
        private const string ExpectedPrefab = TestOutDir + "/hud_bar.prefab";

        private GameObject _instance;

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(TestOutDir);
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            if (_instance != null)
            {
                Object.DestroyImmediate(_instance);
                _instance = null;
            }
            if (AssetDatabase.IsValidFolder(TestOutDir))
            {
                AssetDatabase.DeleteAsset(TestOutDir);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void test_hud_bar_slots_emitted()
        {
            if (!File.Exists(PanelsPath))
            {
                Assert.Ignore("panels.json missing — run npm run snapshot:export-game-ui first");
                return;
            }

            var args = new UiBakeHandler.BakeArgs
            {
                panels_path = PanelsPath,
                out_dir = TestOutDir,
                theme_so = TestThemePath,
            };

            var result = UiBakeHandler.BakeFromPanelSnapshot(args);

            if (result.error != null && result.error.error == "theme_so_not_found")
            {
                Assert.Ignore($"UiTheme SO absent at {TestThemePath} — bake-infra not available");
                return;
            }

            Assert.IsNull(result.error, $"BakeFromPanelSnapshot returned error: {result.error?.error} — {result.error?.details}");

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ExpectedPrefab);
            Assert.IsNotNull(asset, $"hud_bar prefab not found at {ExpectedPrefab}");

            _instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            Assert.IsNotNull(_instance, "PrefabUtility.InstantiatePrefab returned null");

            // Assert root has LayoutGroup (HLG from layout_template=hstack).
            var rootHlg = _instance.GetComponent<HorizontalLayoutGroup>();
            Assert.IsNotNull(rootHlg, "hud_bar root must have HorizontalLayoutGroup (layout_template=hstack)");

            // Assert slot-wrapper children exist with their own HLG.
            var leftT = _instance.transform.Find("Left");
            Assert.IsNotNull(leftT, "hud_bar prefab must have 'Left' slot-wrapper child");
            Assert.IsNotNull(leftT.GetComponent<HorizontalLayoutGroup>(), "'Left' must have HorizontalLayoutGroup");

            var centerT = _instance.transform.Find("Center");
            Assert.IsNotNull(centerT, "hud_bar prefab must have 'Center' slot-wrapper child");
            Assert.IsNotNull(centerT.GetComponent<HorizontalLayoutGroup>(), "'Center' must have HorizontalLayoutGroup");

            var rightT = _instance.transform.Find("Right");
            Assert.IsNotNull(rightT, "hud_bar prefab must have 'Right' slot-wrapper child");
            Assert.IsNotNull(rightT.GetComponent<HorizontalLayoutGroup>(), "'Right' must have HorizontalLayoutGroup");

            // Assert at least one child exists under Left wrapper (BUDGET button is ord=1, zone=left).
            Assert.Greater(leftT.childCount, 0, "'Left' wrapper must contain at least one child (BUDGET button ord=1)");
        }
    }
}
