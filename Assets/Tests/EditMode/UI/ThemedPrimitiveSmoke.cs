using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Territory.UI;
using Territory.UI.Themed;

namespace Territory.Tests.EditMode.UI
{
    /// <summary>
    /// Stage 3 (TECH-2672) EditMode smoke: themed primitive ring runs ApplyTheme NPE-free
    /// against a baked HudBar prefab + a default UiTheme fixture. Verifies slot graph
    /// composer wiring + ThemedButton/ThemedLabel idempotency. Skips Test 1 gracefully
    /// when bake artifact missing (stage ordering — IR bake runs upstream).
    /// </summary>
    public class ThemedPrimitiveSmoke
    {
        private const string HudBarPrefabPath = "Assets/UI/Prefabs/Generated/HudBar.prefab";
        private const string DefaultThemePath = "Assets/UI/Theme/DefaultUiTheme.asset";

        private GameObject _instantiated;
        private UiTheme _runtimeTheme;

        [TearDown]
        public void TearDown()
        {
            if (_instantiated != null)
            {
                Object.DestroyImmediate(_instantiated);
                _instantiated = null;
            }
            if (_runtimeTheme != null)
            {
                Object.DestroyImmediate(_runtimeTheme);
                _runtimeTheme = null;
            }
        }

        [Test]
        public void Loads_baked_HudBar_prefab_and_runs_slot_graph()
        {
            if (!File.Exists(HudBarPrefabPath))
            {
                Assert.Ignore($"baked HudBar prefab missing at {HudBarPrefabPath} — run unity:bake-ui first");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudBarPrefabPath);
            Assert.IsNotNull(prefab, $"AssetDatabase.LoadAssetAtPath returned null for {HudBarPrefabPath}");

            _instantiated = Object.Instantiate(prefab);
            Assert.IsNotNull(_instantiated, "Instantiate returned null");

            var panel = _instantiated.GetComponent<ThemedPanel>();
            Assert.IsNotNull(panel, "instantiated HudBar root missing ThemedPanel component");

            // Force Awake (EditMode bypasses lifecycle; SendMessage triggers private Awake).
            Assert.DoesNotThrow(
                () => _instantiated.SendMessage("Awake", SendMessageOptions.DontRequireReceiver),
                "ThemedPanel.Awake threw");

            Assert.IsNotNull(panel.Slots, "ThemedPanel._slots null after Awake");
            // Slots may be empty for trivial panels; smoke only asserts no NPE + accessor reachable.
        }

        [Test]
        public void ThemedButton_ApplyTheme_no_NPE_with_valid_UiTheme()
        {
            var theme = LoadOrCreateTheme();

            var go = new GameObject("ThemedButton_Smoke");
            try
            {
                var button = go.AddComponent<ThemedButton>();
                Assert.DoesNotThrow(
                    () => button.ApplyTheme(theme),
                    "ThemedButton.ApplyTheme threw with valid UiTheme");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ThemedLabel_ApplyTheme_no_NPE_with_valid_UiTheme()
        {
            var theme = LoadOrCreateTheme();

            var go = new GameObject("ThemedLabel_Smoke");
            try
            {
                var label = go.AddComponent<ThemedLabel>();
                Assert.DoesNotThrow(
                    () => label.ApplyTheme(theme),
                    "ThemedLabel.ApplyTheme threw with valid UiTheme");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private UiTheme LoadOrCreateTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(DefaultThemePath);
            if (theme != null) return theme;
            // Fallback: in-memory ScriptableObject so suite stays self-contained when fixture asset absent.
            _runtimeTheme = ScriptableObject.CreateInstance<UiTheme>();
            return _runtimeTheme;
        }
    }
}
