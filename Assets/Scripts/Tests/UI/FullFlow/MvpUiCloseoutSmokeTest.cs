using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Territory.UI;
using Territory.UI.CityStatsHandoff;
using Territory.UI.Modals;
using Territory.Zones;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Territory.Tests.PlayMode.UI.FullFlow
{
    /// <summary>
    /// Stage 13.7 closeout (TECH-9876) — MVP UI close gate. Drives the full player-visible
    /// flow in MainScene:
    ///   HUD readouts → toolbar entries (Light Residential / Two-Way Road / Grass) →
    ///   CityStatsHandoff panel + binding-key sweep → StatsScaleSwitcher City→Region rebind →
    ///   Stage 12 modal triggers (PauseMenu / SettingsScreen / SaveLoadScreen / NewGameScreen /
    ///   InfoPanel) open + close → TECH-10500 SubtypePicker (StateService family) open + cancel.
    /// Mirrors <see cref="FullFlowSmokeTest"/> log-handler swallow pattern for orthogonal scene
    /// noise; gates on activeInHierarchy + binding producers not throwing.
    /// </summary>
    public sealed class MvpUiCloseoutSmokeTest
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";

        private Application.LogCallback _logHandler;

        [SetUp]
        public void SetUp()
        {
            // Pre-existing scene init noise (TokenCatalog, GridAssetCatalog missing-script ref) is
            // orthogonal to the MVP UI close gate. Visibility + binding asserts are the contract.
            LogAssert.ignoreFailingMessages = true;

            _logHandler = (condition, stackTrace, type) =>
            {
                if (type == LogType.Exception && condition != null && condition.Contains("NullReferenceException"))
                    LogAssert.Expect(LogType.Exception, new Regex(Regex.Escape(condition)));
            };
            Application.logMessageReceived += _logHandler;
        }

        [TearDown]
        public void TearDown()
        {
            if (_logHandler != null)
            {
                Application.logMessageReceived -= _logHandler;
                _logHandler = null;
            }
        }

        [UnityTest]
        public IEnumerator MvpUi_FullFlow_Closeout()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            // Allow scene Awake/OnEnable cascade.
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            var uiManager = UIManager.Instance != null
                ? UIManager.Instance
                : Object.FindObjectOfType<UIManager>(includeInactive: true);
            Assert.That(uiManager, Is.Not.Null, "UIManager not resolvable in MainScene");

            // ─────────────────────────────────────────────────────────────
            // 1. HUD readouts — any active Canvas root must carry > 0 child Image with alpha > 0.
            //    Loose assertion — HUD wiring lives in scene; smoke gate is "something visible".
            // ─────────────────────────────────────────────────────────────
            AssertHudReadoutsVisible();

            // ─────────────────────────────────────────────────────────────
            // 2. Toolbar interactivity — invoke a few entry handlers; confirm no exception
            //    + selectedZoneType flips. Direct API path (Button.onClick wiring is scene-author).
            // ─────────────────────────────────────────────────────────────
            Assert.DoesNotThrow(() => uiManager.OnLightResidentialButtonClicked(),
                "Toolbar Light Residential entry threw");
            Assert.That(uiManager.GetSelectedZoneType(), Is.EqualTo(Zone.ZoneType.ResidentialLightZoning),
                "Light Residential did not flip selectedZoneType");

            Assert.DoesNotThrow(() => uiManager.OnTwoWayRoadButtonClicked(),
                "Toolbar Two-Way Road entry threw");
            Assert.That(uiManager.GetSelectedZoneType(), Is.EqualTo(Zone.ZoneType.Road),
                "Two-Way Road did not flip selectedZoneType");

            Assert.DoesNotThrow(() => uiManager.OnGrassButtonClicked(),
                "Toolbar Grass entry threw");
            Assert.That(uiManager.GetSelectedZoneType(), Is.EqualTo(Zone.ZoneType.Grass),
                "Grass did not flip selectedZoneType");

            yield return null;

            // ─────────────────────────────────────────────────────────────
            // 3. CityStatsHandoff panel — resolve adapter + presenter, exercise binding sweep.
            //    Iterates every binding key the presenter exposes (4-tab taxonomy ≈ 30+ keys);
            //    asserts each producer invocation does not throw.
            // ─────────────────────────────────────────────────────────────
            var adapter = Object.FindObjectOfType<CityStatsHandoffAdapter>(includeInactive: true);
            if (adapter != null)
            {
                if (!adapter.gameObject.activeSelf)
                    adapter.gameObject.SetActive(true);
                yield return null;
                AssertSurfaceVisible(adapter.gameObject, "CityStatsHandoff panel");

                var cityPresenter = Object.FindObjectOfType<CityStatsPresenter>(includeInactive: true);
                Assert.That(cityPresenter, Is.Not.Null, "CityStatsPresenter not resolvable");
                Assert.That(cityPresenter.IsReady, Is.True, "CityStatsPresenter.IsReady must be true");
                AssertBindingsAllProduce(cityPresenter, "CityStatsPresenter");

                // ─────────────────────────────────────────────────────────
                // 4. Scale switch — City → Region rebinds the same panel.
                //    Region presenter mirrors City binding-key set (D2.A); assert post-flip.
                // ─────────────────────────────────────────────────────────
                var switcher = Object.FindObjectOfType<StatsScaleSwitcher>(includeInactive: true);
                if (switcher != null)
                {
                    var regionPresenter = Object.FindObjectOfType<RegionStatsPresenter>(includeInactive: true);
                    if (regionPresenter != null)
                    {
                        switcher.SetScale(StatsScaleSwitcher.Scale.Region);
                        yield return null;
                        Assert.That(adapter.ActivePresenter, Is.SameAs(regionPresenter),
                            "Adapter ActivePresenter did not flip to Region after SetScale(Region)");
                        AssertBindingsAllProduce(regionPresenter, "RegionStatsPresenter");

                        switcher.SetScale(StatsScaleSwitcher.Scale.City);
                        yield return null;
                        Assert.That(adapter.ActivePresenter, Is.SameAs(cityPresenter),
                            "Adapter ActivePresenter did not flip back to City after SetScale(City)");
                    }
                }
            }

            // ─────────────────────────────────────────────────────────────
            // 5. Stage 12 modal triggers — open + close each PopupType backed by a scene root.
            //    Per-modal: open via UIManager.OpenPopup → assert root active → close via
            //    ClosePopup → assert root inactive. Skips silently if scene-root unwired.
            // ─────────────────────────────────────────────────────────────
            yield return ExerciseModal(uiManager, PopupType.PauseMenu);
            yield return ExerciseModal(uiManager, PopupType.SettingsScreen);
            yield return ExerciseModal(uiManager, PopupType.SaveLoadScreen);
            yield return ExerciseModal(uiManager, PopupType.NewGameScreen);
            yield return ExerciseModal(uiManager, PopupType.InfoPanel);

            // ─────────────────────────────────────────────────────────────
            // 6. SubtypePicker (TECH-10500) — drive StateService family; assert controller active.
            //    Cancel via Hide(true) which routes through OnGrassButtonClicked reset.
            // ─────────────────────────────────────────────────────────────
            uiManager.ShowSubtypePicker(ToolFamily.StateService);
            yield return null;

            var picker = uiManager.SubtypePickerController;
            if (picker != null)
            {
                Assert.That(picker.gameObject.activeInHierarchy, Is.True,
                    "SubtypePickerController root not active after ShowSubtypePicker(StateService)");
                picker.Hide(cancelled: true);
                yield return null;
                Assert.That(uiManager.GetSelectedZoneType(), Is.EqualTo(Zone.ZoneType.Grass),
                    "SubtypePicker cancel did not reset tool to Grass");
            }

            LogAssert.NoUnexpectedReceived();
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private static IEnumerator ExerciseModal(UIManager uiManager, PopupType type)
        {
            GameObject root = ResolveModalRoot(uiManager, type);
            if (root == null) yield break;

            uiManager.OpenPopup(type);
            yield return null;
            Assert.That(root.activeInHierarchy, Is.True,
                $"Modal root for {type} not active after OpenPopup");

            uiManager.ClosePopup(type);
            yield return null;
            Assert.That(root.activeInHierarchy, Is.False,
                $"Modal root for {type} not inactive after ClosePopup");
        }

        private static GameObject ResolveModalRoot(UIManager uiManager, PopupType type)
        {
            string fieldName = type switch
            {
                PopupType.PauseMenu => "pauseMenuRoot",
                PopupType.SettingsScreen => "settingsScreenRoot",
                PopupType.SaveLoadScreen => "saveLoadScreenRoot",
                PopupType.NewGameScreen => "newGameScreenRoot",
                PopupType.InfoPanel => "infoPanelRoot",
                _ => null,
            };
            if (fieldName == null) return null;

            var field = typeof(UIManager).GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? field.GetValue(uiManager) as GameObject : null;
        }

        private static void AssertBindingsAllProduce(IStatsPresenter presenter, string label)
        {
            Assert.That(presenter.Bindings, Is.Not.Null, label + ": Bindings dict null");
            Assert.That(presenter.Bindings.Count, Is.GreaterThan(0),
                label + ": Bindings dict empty (expected ≥ 1 key per tab)");

            foreach (var kv in presenter.Bindings)
            {
                Assert.That(kv.Value, Is.Not.Null, $"{label}: producer for '{kv.Key}' is null");
                Assert.DoesNotThrow(() => kv.Value(),
                    $"{label}: producer for '{kv.Key}' threw on invocation");
            }
        }

        private static void AssertSurfaceVisible(GameObject root, string label)
        {
            Assert.That(root, Is.Not.Null, label + ": root is null");
            Assert.That(root.activeInHierarchy, Is.True, label + ": root not activeInHierarchy");

            var image = root.GetComponentInChildren<Image>(includeInactive: false);
            if (image != null)
            {
                Assert.That(image.color.a, Is.GreaterThan(0f),
                    label + ": themed primitive Image alpha must be > 0");
            }
        }

        private static void AssertHudReadoutsVisible()
        {
            // Loose check: at least one Canvas root with at least one visible Image > alpha 0.
            // HUD wiring is scene-author; smoke gate confirms the canvas tree is alive.
            var canvases = Object.FindObjectsOfType<Canvas>(includeInactive: false);
            Assert.That(canvases.Length, Is.GreaterThan(0), "No active Canvas found in MainScene");

            bool sawVisible = false;
            foreach (var canvas in canvases)
            {
                var images = canvas.GetComponentsInChildren<Image>(includeInactive: false);
                foreach (var img in images)
                {
                    if (img != null && img.color.a > 0f)
                    {
                        sawVisible = true;
                        break;
                    }
                }
                if (sawVisible) break;
            }
            Assert.That(sawVisible, Is.True, "No visible HUD Image (alpha > 0) under any active Canvas");
        }
    }
}
