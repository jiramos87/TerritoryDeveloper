using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Territory.UI;
using Territory.UI.StudioControls;
using Territory.UI.Themed.Renderers;
using Territory.UI.Toolbar;
using Territory.Zones;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Territory.Tests.PlayMode.UI.Toolbar
{
    /// <summary>
    /// Stage 7 toolbar parity PlayMode tests — assert StudioControl-baked toolbar surface mirrors
    /// the legacy <see cref="UIManager"/> click + overlay paths.
    /// (a) Each <see cref="IlluminatedButton"/> click invokes the same <c>UIManager.On*ButtonClicked()</c>
    ///     method as the legacy GameObject button (verified by post-click <see cref="UIManager"/>
    ///     selection state — <see cref="UIManager.GetSelectedZoneType"/>, <c>isBulldozeMode()</c>, etc.).
    /// (b) <see cref="ThemedOverlayToggleRowRenderer"/> toggle flips <see cref="UIManager.GetOverlayActive"/>
    ///     identically to the legacy overlay path.
    /// (c) Multi-overlay state survives save-load round-trip (capture → mutate → load).
    /// </summary>
    /// <remarks>
    /// Mirrors <see cref="Territory.Tests.PlayMode.UI.HUD.HudBarParityTest"/> — UnitySetUp loads
    /// MainScene in PlayMode, ignores baseline runtime LogError noise, then drives the adapters'
    /// SerializeField slots via reflection.
    /// </remarks>
    public sealed class ToolbarParityTest
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";

        private ToolbarDataAdapter _toolbarAdapter;
        private OverlayToggleDataAdapter _overlayAdapter;
        private UIManager _uiManager;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Suppress baseline runtime LogError noise (BlipBootstrap, ZoneSubTypeRegistry, etc.)
            // so toolbar-scoped asserts don't auto-fail on unrelated init logs.
            LogAssert.ignoreFailingMessages = true;

#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
            // Wait for scene + Awake cascade to settle.
            yield return null;
            yield return null;

            _toolbarAdapter = Object.FindObjectOfType<ToolbarDataAdapter>();
            _overlayAdapter = Object.FindObjectOfType<OverlayToggleDataAdapter>();
            _uiManager = Object.FindObjectOfType<UIManager>();
        }

        // ─────────────────────────────────────────────────────────────────────
        // (a) — IlluminatedButton click parity. For each tool slug, assert
        // _uiManager state after .OnClick.Invoke() matches legacy method dispatch.
        // ─────────────────────────────────────────────────────────────────────
        [UnityTest]
        public IEnumerator ZoningClick_RoutesToUIManager()
        {
            Assert.IsNotNull(_toolbarAdapter, "ToolbarDataAdapter not found in scene — Stage 7 wiring incomplete.");
            Assert.IsNotNull(_uiManager, "UIManager not found in scene.");

            var zoning = GetButtonArray("_zoningButtons");
            Assert.IsNotNull(zoning, "_zoningButtons consumer array not assigned — adapter wiring incomplete.");
            Assert.AreEqual(10, zoning.Length, $"_zoningButtons length expected 10, got {zoning.Length}.");

            // Index 0 = Residential L → ZoneType.ResidentialLightZoning.
            Assert.IsNotNull(zoning[0], "_zoningButtons[0] (Residential Light) unbound.");
            zoning[0].OnClick.Invoke();
            yield return null;
            Assert.AreEqual(Zone.ZoneType.ResidentialLightZoning, _uiManager.GetSelectedZoneType(),
                "Residential-light click did not flip UIManager.selectedZoneType.");

            // Index 9 = StateService Zoning → ZoneType.StateService*Zoning.
            Assert.IsNotNull(zoning[9], "_zoningButtons[9] (StateService) unbound.");
            zoning[9].OnClick.Invoke();
            yield return null;
            var stateZone = _uiManager.GetSelectedZoneType();
            Assert.IsTrue(
                stateZone == Zone.ZoneType.StateServiceLightZoning ||
                stateZone == Zone.ZoneType.StateServiceMediumZoning ||
                stateZone == Zone.ZoneType.StateServiceHeavyZoning,
                $"StateService click did not select a StateService zone (got {stateZone}).");
        }

        [UnityTest]
        public IEnumerator BulldozeClick_RoutesToUIManager()
        {
            Assert.IsNotNull(_toolbarAdapter, "ToolbarDataAdapter not found in scene.");
            Assert.IsNotNull(_uiManager, "UIManager not found in scene.");

            var bulldoze = GetButtonSingle("_bulldozeButton");
            Assert.IsNotNull(bulldoze, "_bulldozeButton consumer not assigned — adapter wiring incomplete.");

            bulldoze.OnClick.Invoke();
            yield return null;
            Assert.IsTrue(_uiManager.isBulldozeMode(), "Bulldoze click did not enter bulldoze mode.");
        }

        [UnityTest]
        public IEnumerator RoadAndTerrainClicks_RouteToUIManager()
        {
            Assert.IsNotNull(_toolbarAdapter, "ToolbarDataAdapter not found in scene.");
            Assert.IsNotNull(_uiManager, "UIManager not found in scene.");

            var road = GetButtonArray("_roadButtons");
            Assert.IsNotNull(road, "_roadButtons unbound.");
            Assert.IsTrue(road.Length >= 1 && road[0] != null, "_roadButtons[0] unbound.");
            road[0].OnClick.Invoke();
            yield return null;
            Assert.AreEqual(Zone.ZoneType.Road, _uiManager.GetSelectedZoneType(),
                "Road click did not flip UIManager.selectedZoneType to Road.");

            var terrain = GetButtonArray("_terrainButtons");
            Assert.IsNotNull(terrain, "_terrainButtons unbound.");
            Assert.IsTrue(terrain.Length >= 1 && terrain[0] != null, "_terrainButtons[0] unbound.");
            terrain[0].OnClick.Invoke();
            yield return null;
            Assert.AreEqual(Zone.ZoneType.Grass, _uiManager.GetSelectedZoneType(),
                "Grass click did not flip UIManager.selectedZoneType to Grass.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // (b) — Overlay toggle parity. Renderer.SetIsOn(true, notify=true) must drive
        // UIManager.GetOverlayActive(slug) to true; SetIsOn(false) flips it back.
        // ─────────────────────────────────────────────────────────────────────
        [UnityTest]
        public IEnumerator OverlayToggle_FlipsUIManagerState()
        {
            Assert.IsNotNull(_overlayAdapter, "OverlayToggleDataAdapter not found in scene — Stage 7 wiring incomplete.");
            Assert.IsNotNull(_uiManager, "UIManager not found in scene.");

            var rows = GetOverlayRows();
            Assert.IsNotNull(rows, "_overlayToggles consumer array not assigned — adapter wiring incomplete.");
            Assert.AreEqual(5, rows.Length, $"_overlayToggles length expected 5, got {rows.Length}.");

            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                Assert.IsNotNull(row, $"_overlayToggles[{i}] unbound — OverlaySlug index {i} adapter wiring incomplete.");

                var slug = (OverlaySlug)i;
                bool initial = _uiManager.GetOverlayActive(slug);

                // Flip ON via renderer (notify=true → UnityEvent fires → adapter → UIManager.SetOverlayActive).
                row.SetIsOn(true, notify: true);
                yield return null;
                Assert.IsTrue(_uiManager.GetOverlayActive(slug),
                    $"Overlay {slug}: SetIsOn(true) did not flip UIManager.GetOverlayActive.");

                // Flip OFF.
                row.SetIsOn(false, notify: true);
                yield return null;
                Assert.IsFalse(_uiManager.GetOverlayActive(slug),
                    $"Overlay {slug}: SetIsOn(false) did not flip UIManager.GetOverlayActive.");

                // Restore initial state.
                row.SetIsOn(initial, notify: true);
                yield return null;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // (c) — Multi-overlay save-load round-trip parity. Capture → mutate → load
        // restores the captured state. Asserts on UIManager API directly (overlay
        // save-load surface is the load-bearing parity invariant).
        // ─────────────────────────────────────────────────────────────────────
        [UnityTest]
        public IEnumerator MultiOverlay_SurvivesSaveLoadRoundTrip()
        {
            Assert.IsNotNull(_uiManager, "UIManager not found in scene.");

            // Set: Terrain ON + Pollution ON + others OFF.
            _uiManager.SetOverlayActive(OverlaySlug.Terrain, true);
            _uiManager.SetOverlayActive(OverlaySlug.Pollution, true);
            _uiManager.SetOverlayActive(OverlaySlug.LandValue, false);
            _uiManager.SetOverlayActive(OverlaySlug.RoadNetwork, false);
            _uiManager.SetOverlayActive(OverlaySlug.TrafficFlow, false);
            yield return null;

            // Capture snapshot (save side).
            List<bool> snapshot = _uiManager.CaptureOverlayActiveForSave();
            Assert.AreEqual(5, snapshot.Count, $"CaptureOverlayActiveForSave length expected 5, got {snapshot.Count}.");
            Assert.IsTrue(snapshot[(int)OverlaySlug.Terrain], "Snapshot Terrain bit not set.");
            Assert.IsTrue(snapshot[(int)OverlaySlug.Pollution], "Snapshot Pollution bit not set.");

            // Clear all (simulate a different game state).
            _uiManager.SetOverlayActive(OverlaySlug.Terrain, false);
            _uiManager.SetOverlayActive(OverlaySlug.Pollution, false);
            yield return null;
            Assert.IsFalse(_uiManager.GetOverlayActive(OverlaySlug.Terrain), "Pre-load Terrain still active.");
            Assert.IsFalse(_uiManager.GetOverlayActive(OverlaySlug.Pollution), "Pre-load Pollution still active.");

            // Reload from snapshot.
            _uiManager.LoadOverlayStateFromSaveData(snapshot);
            yield return null;

            Assert.IsTrue(_uiManager.GetOverlayActive(OverlaySlug.Terrain),
                "Post-load Terrain not restored from snapshot.");
            Assert.IsTrue(_uiManager.GetOverlayActive(OverlaySlug.Pollution),
                "Post-load Pollution not restored from snapshot.");
            Assert.IsFalse(_uiManager.GetOverlayActive(OverlaySlug.LandValue),
                "Post-load LandValue should remain inactive.");
        }

        // ── Reflection helpers — adapter exposes consumer refs as private [SerializeField];
        // tests read them via reflection to keep adapter API surface minimal.

        private IlluminatedButton[] GetButtonArray(string fieldName)
        {
            var f = typeof(ToolbarDataAdapter).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return f != null ? f.GetValue(_toolbarAdapter) as IlluminatedButton[] : null;
        }

        private IlluminatedButton GetButtonSingle(string fieldName)
        {
            var f = typeof(ToolbarDataAdapter).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return f != null ? f.GetValue(_toolbarAdapter) as IlluminatedButton : null;
        }

        private ThemedOverlayToggleRowRenderer[] GetOverlayRows()
        {
            var f = typeof(OverlayToggleDataAdapter).GetField("_overlayToggles",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return f != null ? f.GetValue(_overlayAdapter) as ThemedOverlayToggleRowRenderer[] : null;
        }
    }
}
