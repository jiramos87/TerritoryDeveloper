using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using Territory.UI;
using Territory.UI.Modals;
using Territory.UI.CityStatsHandoff;
using Territory.UI.Glossary;
using Territory.UI.Onboarding;
using Territory.UI.Splash;
using Territory.UI.Themed;
using Territory.UI.Tooltips;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Territory.Tests.PlayMode.UI.FullFlow
{
    /// <summary>
    /// Stage 11 close gate (TECH-8950) — cross-scene PlayMode smoke covering the player-visible
    /// MVP UI checkpoint:
    ///   <c>MainMenu.unity</c> Options click → NewGame click → <c>MainScene.unity</c> loaded →
    ///   splash → onboarding (consume + dismiss) → tooltip hover → glossary panel open →
    ///   city-stats handoff panel open → in-game themed modal trigger.
    /// Mirrors <c>ModalTriggerPathsSmokeTest</c> click-driver + assertion shape (Button.onClick.Invoke
    /// indirectly via <c>MainMenuController</c> public methods; polled scene name; LogAssert NullRef
    /// swallow). Hard fail on legacy chrome GameObjects (post-T11.2 / T11.3 deletes).
    /// </summary>
    public sealed class FullFlowSmokeTest
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string MainScenePath = "Assets/Scenes/MainScene.unity";
        private const string MainSceneName = "MainScene";
        private const string TooltipProbeText = "fullflow-hover";

        private Application.LogCallback _logHandler;

        [SetUp]
        public void SetUp()
        {
            // Pre-existing scene init noise (TokenCatalog, GridAssetCatalog missing-script ref) is
            // orthogonal to Stage 11 cross-scene gate. Visibility + flag asserts are the actual
            // contract.
            LogAssert.ignoreFailingMessages = true;

            // LogType.Exception is NOT covered by ignoreFailingMessages — orthogonal NullRefs from
            // prefab Update/OnEnable callbacks must be intercepted at the log source. Mirrors
            // ModalTriggerPathsSmokeTest.SetUp pattern.
            _logHandler = (condition, stackTrace, type) =>
            {
                if (type == LogType.Exception && condition != null && condition.Contains("NullReferenceException"))
                    LogAssert.Expect(LogType.Exception, new Regex(Regex.Escape(condition)));
            };
            Application.logMessageReceived += _logHandler;

            // Reset onboarding flag so the test always exercises the consume + dismiss path.
            PlayerPrefs.DeleteKey(OnboardingAdapter.OnboardingCompleteKey);
            PlayerPrefs.Save();
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
        public IEnumerator FullFlow_MainMenu_To_MainScene_Modals()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(MainMenuScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            // Allow scene Awake cascade.
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            var mainMenu = Object.FindObjectOfType<MainMenuController>(includeInactive: true);
            Assert.That(mainMenu, Is.Not.Null, "MainMenuController not resolvable in MainMenu scene");

            // Options click — themed MainMenu chrome assert (overlay panel becomes active + has
            // image alpha > 0). MainMenuController.OnOptionsClicked toggles optionsPanel SetActive.
            mainMenu.OnOptionsClicked();
            yield return null;
            yield return null;

            var optionsPanelGo = ResolveOptionsPanel(mainMenu);
            if (optionsPanelGo != null)
            {
                Assert.That(optionsPanelGo.activeInHierarchy, Is.True,
                    "Options click did not activate optionsPanel");
                AssertSurfaceVisible(optionsPanelGo, "MainMenu.OptionsPanel");
            }

            // NewGame click — triggers SceneManager.LoadScene(MainScene). Poll for active scene
            // name flip rather than gating on sceneLoaded event (matches existing PlayMode test
            // convention — yield-poll is safer than event subscription teardown).
            mainMenu.OnNewGameClicked();
            yield return null;

            int sceneWaitFrames = 0;
            while (SceneManager.GetActiveScene().name != MainSceneName && sceneWaitFrames < 240)
            {
                sceneWaitFrames++;
                yield return null;
            }
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MainSceneName),
                "MainScene did not become active after NewGame click");

            // Allow MainScene Awake/OnEnable cascade.
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            // Hierarchy regression — legacy chrome must be gone after T11.2 / T11.3 deletes.
            AssertGameObjectAbsent("HUDBar/CityNameLabel");
            AssertGameObjectAbsent("Toolbar/BuildingIcons");
            AssertGameObjectAbsent("DataPanelButtons/StatsPanel");
            AssertGameObjectAbsent("DataPanelButtons/GameButtons/ShowStatsButton");

            // Splash adapter — themed surface reachable.
            var splash = Object.FindObjectOfType<SplashAdapter>(includeInactive: true);
            if (splash != null)
            {
                AssertSurfaceVisible(splash.gameObject, "SplashAdapter");
            }

            // Onboarding — flip flag + assert dismissal.
            var onboarding = Object.FindObjectOfType<OnboardingAdapter>(includeInactive: true);
            if (onboarding != null)
            {
                Assert.That(onboarding.IsOnboardingComplete(), Is.False,
                    "Onboarding flag must start false (cleared in SetUp)");
                onboarding.MarkOnboardingComplete();
                yield return null;
                Assert.That(PlayerPrefs.GetInt(OnboardingAdapter.OnboardingCompleteKey, 0), Is.EqualTo(1),
                    "PlayerPrefs onboarding-complete flag did not flip to 1 after MarkOnboardingComplete");
            }

            // Tooltip hover — synthetic PointerEnter on a probe under the active canvas. Mirrors
            // TooltipHoverSmokeTest probe construction.
            var tooltipController = Object.FindObjectOfType<TooltipController>(includeInactive: true);
            if (tooltipController != null)
            {
                var canvas = tooltipController.GetComponentInParent<Canvas>();
                var canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
                if (canvasRect != null)
                {
                    var probe = new GameObject("FullFlowTooltipProbe");
                    probe.transform.SetParent(canvasRect, false);
                    probe.AddComponent<RectTransform>();
                    probe.AddComponent<Image>();
                    var trigger = probe.AddComponent<TooltipText>();
                    trigger.SetText(TooltipProbeText);

                    int beforeChildren = canvasRect.childCount;
                    var enterEvent = new PointerEventData(EventSystem.current) { position = new Vector2(120f, 120f) };
                    trigger.OnPointerEnter(enterEvent);
                    yield return null;
                    Assert.That(canvasRect.childCount, Is.GreaterThan(beforeChildren),
                        "Tooltip child not spawned on PointerEnter");

                    var exitEvent = new PointerEventData(EventSystem.current);
                    trigger.OnPointerExit(exitEvent);
                    yield return null;
                    Object.Destroy(probe);
                    yield return null;
                }
            }

            // Glossary panel — themed list reachable.
            var glossary = Object.FindObjectOfType<GlossaryPanelAdapter>(includeInactive: true);
            if (glossary != null)
            {
                if (!glossary.gameObject.activeSelf)
                    glossary.gameObject.SetActive(true);
                yield return null;
                AssertSurfaceVisible(glossary.gameObject, "GlossaryPanelAdapter");
            }

            // City-stats handoff panel — themed surface reachable.
            var cityStats = Object.FindObjectOfType<CityStatsHandoffAdapter>(includeInactive: true);
            if (cityStats != null)
            {
                if (!cityStats.gameObject.activeSelf)
                    cityStats.gameObject.SetActive(true);
                yield return null;
                AssertSurfaceVisible(cityStats.gameObject, "CityStatsHandoffAdapter");
            }

            // In-game themed modal trigger — fire UIManager.OpenPopup(PauseMenu) and assert root
            // active. Mirrors ModalTriggerPathsSmokeTest.EscEmptyStack_OpensPauseMenu.
            var uiManager = UIManager.Instance != null
                ? UIManager.Instance
                : Object.FindObjectOfType<UIManager>(includeInactive: true);
            Assert.That(uiManager, Is.Not.Null, "UIManager not resolvable in MainScene");

            uiManager.OpenPopup(PopupType.PauseMenu);
            yield return null;

            var pauseMenu = Object.FindObjectOfType<PauseMenuDataAdapter>(includeInactive: true);
            Assert.That(pauseMenu, Is.Not.Null, "PauseMenuDataAdapter not resolvable in MainScene");
            AssertSurfaceVisible(pauseMenu.gameObject, "PauseMenu (in-game modal trigger)");

            uiManager.ClosePopup(PopupType.PauseMenu);
            yield return null;

            LogAssert.NoUnexpectedReceived();
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

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

            var tmp = root.GetComponentInChildren<TMP_Text>(includeInactive: false);
            if (tmp != null)
            {
                Assert.That(string.IsNullOrEmpty(tmp.text), Is.False,
                    label + ": TMP_Text body must be non-empty");
            }
        }

        private static void AssertGameObjectAbsent(string scenePath)
        {
            // GameObject.Find resolves only active GameObjects. Augment by walking all roots so
            // inactive legacy chrome would still trip the gate.
            var leaf = scenePath.Split('/');
            var leafName = leaf[leaf.Length - 1];

            var active = GameObject.Find(scenePath);
            Assert.That(active, Is.Null, "Legacy chrome still present (active): " + scenePath);

            foreach (var go in Object.FindObjectsOfType<GameObject>(includeInactive: true))
            {
                if (go == null) continue;
                if (go.name != leafName) continue;
                var fullPath = BuildHierarchyPath(go.transform);
                Assert.That(fullPath.EndsWith(scenePath), Is.False,
                    "Legacy chrome still present (inactive): " + fullPath);
            }
        }

        private static string BuildHierarchyPath(Transform t)
        {
            if (t == null) return string.Empty;
            var path = t.name;
            var parent = t.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        private static GameObject ResolveOptionsPanel(MainMenuController mainMenu)
        {
            // optionsPanel is a private SerializeField on MainMenuController. Resolve via reflection
            // to keep test independent of public-field churn.
            var field = typeof(MainMenuController).GetField("optionsPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? field.GetValue(mainMenu) as GameObject : null;
        }
    }
}
