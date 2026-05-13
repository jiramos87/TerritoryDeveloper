using System;
using System.Collections.Generic;
using Territory.Simulation;
using Territory.UI.Registry;
using UnityEngine;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Wave B2 (TECH-27085) — stats-panel adapter.
    /// Registers action.stats-panel-toggle / action.stats-panel-close action handlers
    /// (DB-canonical action ids per button_detail — TECH-29752).
    /// Subscribes ~25 binds (3 chart series + 11 service rows + tab + range + 3 stacked-bars).
    /// Wires range-tabs to StatsHistoryRecorder.GetRange.
    /// Apply-time render-check asserts ≥25 widgets + non-zero subscriber counts.
    /// Inv #3: slot resolution at mount only, never per-frame.
    /// </summary>
    [System.Obsolete("Strangler — replaced by VM-direct Host on UIToolkit. See DEC-A28.")]
    public class StatsPanelAdapter : MonoBehaviour
    {
        private const int ExpectedMinWidgets = 25;

        [SerializeField] private UiActionRegistry  _actionRegistry;
        [SerializeField] private UiBindRegistry    _bindRegistry;
        [SerializeField] private ModalCoordinator  _modalCoordinator;
        [SerializeField] private StatsHistoryRecorder _recorder;

        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        // Bind ids
        private static readonly string[] ChartBindIds =
        {
            "stats.chart.population",
            "stats.chart.services",
            "stats.chart.economy",
        };

        private static readonly string[] ServiceBindIds =
        {
            "stats.svc.power", "stats.svc.water", "stats.svc.waste",
            "stats.svc.police", "stats.svc.fire", "stats.svc.health",
            "stats.svc.education", "stats.svc.parks", "stats.svc.transit",
            "stats.svc.roads", "stats.svc.happiness",
        };

        private static readonly string[] BarBindIds =
        {
            "stats.bar.population",
            "stats.bar.services",
            "stats.bar.economy",
        };

        private void Awake()
        {
            if (_actionRegistry    == null) _actionRegistry    = FindObjectOfType<UiActionRegistry>();
            if (_bindRegistry      == null) _bindRegistry      = FindObjectOfType<UiBindRegistry>();
            if (_modalCoordinator  == null) _modalCoordinator  = FindObjectOfType<ModalCoordinator>();
            if (_recorder          == null) _recorder          = FindObjectOfType<StatsHistoryRecorder>();
            if (_recorder == null)
            {
                // Scene didn't wire one — instantiate at runtime so panel always has data flow.
                // Recorder.Awake() lazy-resolves CityStats + EconomyManager via FindObjectOfType.
                var recorderGo = new GameObject("StatsHistoryRecorder (auto)");
                _recorder = recorderGo.AddComponent<StatsHistoryRecorder>();
                Debug.Log("[StatsPanelAdapter][LOG] Auto-created StatsHistoryRecorder (scene missing one).");
            }
            Debug.Log($"[StatsPanelAdapter][LOG] Awake — actionRegistry={(_actionRegistry != null ? "OK" : "NULL")} bindRegistry={(_bindRegistry != null ? "OK" : "NULL")} modalCoordinator={(_modalCoordinator != null ? "OK" : "NULL")} recorder={(_recorder != null ? "OK" : "NULL")}");
            // Stage 13 hotfix — register in Awake instead of Start. Panel root is registered
            // with ModalCoordinator (SetActive false) immediately after AddComponent, so Start
            // never fires on this adapter. Awake runs once on AddComponent regardless of active.
            RegisterActions();
            Subscribe();
        }

        private void OnDestroy()
        {
            foreach (var sub in _subscriptions) sub?.Dispose();
            _subscriptions.Clear();
        }

        private void RegisterActions()
        {
            if (_actionRegistry == null) return;
            // DB-canonical action ids (button_detail.action_id — TECH-29752).
            _actionRegistry.Register("action.stats-panel-toggle", _ => OnStatsOpen());
            _actionRegistry.Register("action.stats-panel-close",  _ => OnStatsClose());
            // Legacy aliases — kept for editor scripts / test harnesses referencing old ids.
            _actionRegistry.Register("stats.open",  _ => OnStatsOpen());
            _actionRegistry.Register("stats.close", _ => OnStatsClose());
        }

        private void OnStatsOpen()
        {
            Debug.Log($"[StatsPanelAdapter][LOG] OnStatsOpen — recorder={(_recorder != null ? "OK" : "NULL")} bindRegistry={(_bindRegistry != null ? "OK" : "NULL")}");
            if (_modalCoordinator != null)
                _modalCoordinator.TryOpen("stats-panel");
            // Force a fresh snapshot so panel shows real values immediately — don't wait for
            // the next monthly tick (day==1 in TimeManager). Without this, opening within the
            // first month of a fresh game shows all-zero charts + rows.
            if (_recorder != null)
                _recorder.OnMonthlyTick();
            RefreshAllBinds("12mo");
            // Default to population tab on every open — also activates the right chart+bar pair.
            if (_bindRegistry != null)
                _bindRegistry.Set("stats.activeTab", "population");
            else
                OnTabChanged("population");
        }

        private void OnStatsClose()
        {
            if (_modalCoordinator != null)
                _modalCoordinator.Close("stats-panel");
        }

        private void Subscribe()
        {
            if (_bindRegistry == null) return;

            // Tab bind
            var tabSub = _bindRegistry.Subscribe<string>("stats.activeTab", tab => OnTabChanged(tab));
            _subscriptions.Add(tabSub);

            // Range bind
            var rangeSub = _bindRegistry.Subscribe<string>("stats.range", range => OnRangeChanged(range));
            _subscriptions.Add(rangeSub);

            // Chart / service / bar binds are owned by bake-time renderers
            // (ChartRenderer, ServiceRowController, FieldListRenderer) — adapter
            // only publishes; renderers subscribe.
        }

        private static readonly string[] TabSeriesIds = { "population", "services", "economy" };

        private void OnTabChanged(string tab)
        {
            if (string.IsNullOrEmpty(tab)) return;

            // Always search from scene root — panel mount hierarchy varies + slot resolver
            // may return a sub-section that excludes chart hosts.
            var searchRoot = transform.root;
            Debug.Log($"[StatsPanelAdapter][LOG] OnTabChanged → active='{tab}' searchRoot='{searchRoot.name}'");

            foreach (var series in TabSeriesIds)
            {
                bool active = (series == tab);
                ToggleByName(searchRoot, $"stats-chart-{series}", active);
                ToggleByName(searchRoot, $"stats-bar-{series}",   active);
            }

            // Re-push series so newly activated chart receives fresh data
            // (subscription replays only future emissions).
            RefreshAllBinds("12mo");
        }

        private static void ToggleByName(Transform root, string goName, bool active)
        {
            var t = FindDeep(root, goName);
            if (t != null)
            {
                t.gameObject.SetActive(active);
                Debug.Log($"[StatsPanelAdapter][LOG] ToggleByName '{goName}' → active={active} found=YES");
            }
            else
            {
                Debug.LogWarning($"[StatsPanelAdapter][LOG] ToggleByName '{goName}' → NOT FOUND");
            }
        }

        private static Transform FindDeep(Transform root, string goName)
        {
            if (root.name == goName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), goName);
                if (r != null) return r;
            }
            return null;
        }

        private void OnRangeChanged(string range)
        {
            RefreshAllBinds(range);
        }

        private void RefreshAllBinds(string rangeKind)
        {
            Debug.Log($"[StatsPanelAdapter][LOG] RefreshAllBinds({rangeKind}) — entering. bindRegistry={(_bindRegistry != null ? "OK" : "NULL")} recorder={(_recorder != null ? "OK" : "NULL")}");
            if (_bindRegistry == null) return;

            // Push chart series.
            foreach (var bindId in ChartBindIds)
            {
                string seriesId = bindId.Replace("stats.chart.", "");
                var data = _recorder != null ? _recorder.GetRange(rangeKind, seriesId) : Array.Empty<float>();
                Debug.Log($"[StatsPanelAdapter][LOG] Set chart bind '{bindId}' ← float[{data.Length}] (subscribers={_bindRegistry.HasSubscribers(bindId)})");
                _bindRegistry.Set(bindId, data);
            }

            // Push stacked-bar series.
            foreach (var bindId in BarBindIds)
            {
                string seriesId = bindId.Replace("stats.bar.", "");
                var data = _recorder != null ? _recorder.GetRange(rangeKind, seriesId) : Array.Empty<float>();
                Debug.Log($"[StatsPanelAdapter][LOG] Set bar bind '{bindId}' ← float[{data.Length}] (subscribers={_bindRegistry.HasSubscribers(bindId)})");
                _bindRegistry.Set(bindId, data);
            }

            // Push service-row snapshots.
            foreach (var bindId in ServiceBindIds)
            {
                string seriesId = bindId.Replace("stats.", "");
                float val = _recorder != null ? _recorder.GetCurrentSnapshot(seriesId) : 0f;
                Debug.Log($"[StatsPanelAdapter][LOG] Set service bind '{bindId}' ← {val:F2} (subscribers={_bindRegistry.HasSubscribers(bindId)})");
                _bindRegistry.Set(bindId, val);
            }
        }

        /// <summary>
        /// Apply-time render-check: walks panel root for widget components + asserts ≥25 + non-zero subscriber counts.
        /// Mirrors SettingsViewController pattern per T6.0.4 spec.
        /// </summary>
        public void ApplyTimeRenderCheck()
        {
            var root = SlotAnchorResolver.ResolveByPanel("stats", transform);
            if (root == null)
                root = SlotAnchorResolver.ResolveByPanel("stats", transform.root);

            if (root == null)
            {
                Debug.LogWarning("[StatsPanelAdapter] stats slot not found — render-check skipped.");
                return;
            }

            var toggles   = root.GetComponentsInChildren<UnityEngine.UI.Toggle>(true);
            var texts     = root.GetComponentsInChildren<TMPro.TMP_Text>(true);
            var images    = root.GetComponentsInChildren<UnityEngine.UI.RawImage>(true);

            int totalWidgets = toggles.Length + texts.Length + images.Length;

            if (totalWidgets < ExpectedMinWidgets)
            {
                Debug.LogWarning(
                    $"[StatsPanelAdapter] render-check: expected ≥{ExpectedMinWidgets} widgets, " +
                    $"found {totalWidgets} (toggles={toggles.Length} texts={texts.Length} images={images.Length}).");
            }

            // Subscriber count check.
            if (_bindRegistry != null)
            {
                var allBindIds = new List<string>(ChartBindIds);
                allBindIds.AddRange(ServiceBindIds);
                allBindIds.AddRange(BarBindIds);
                allBindIds.Add("stats.activeTab");
                allBindIds.Add("stats.range");

                int boundCount = 0;
                foreach (var id in allBindIds)
                    if (_bindRegistry.HasSubscribers(id)) boundCount++;

                if (boundCount == 0)
                    Debug.LogError("[StatsPanelAdapter] render-check: no bind subscribers found — adapter may not be wired.");
            }
        }
    }
}
