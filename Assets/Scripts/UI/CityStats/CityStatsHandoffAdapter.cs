using System;
using System.Collections.Generic;
using UnityEngine;
using Territory.UI.StudioControls;
using Territory.UI.Themed;

namespace Territory.UI.CityStatsHandoff
{
    /// <summary>
    /// Stage 13.5 (TECH-9869) + Stage 13.6 (TECH-9872) — presenter-driven
    /// city-stats panel adapter. Subscribes to <see cref="IStatsPresenter.OnRefreshed"/>
    /// instead of polling producers per frame; row population reads live binding
    /// closures from <see cref="IStatsPresenter.Bindings"/> by slug.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read-only consumer. Default presenter ref (<see cref="_presenter"/>) cached
    /// in <see cref="Awake"/> via Inspector-first + <see cref="MonoBehaviour.FindObjectOfType{T}()"/>
    /// fallback (invariant #4 / guardrail #0). Row repaint gated on
    /// <see cref="IStatsPresenter.IsReady"/> (guardrail #14) so manager-init race
    /// cannot fire writes before bindings populate.
    /// </para>
    /// <para>
    /// Stage 13.6 — <see cref="SetPresenter"/> swaps the active <see cref="IStatsPresenter"/>
    /// in place: unsubscribes the previous source's <see cref="IStatsPresenter.OnRefreshed"/>,
    /// re-subscribes to the new source, then repaints. Driven by
    /// <see cref="StatsScaleSwitcher"/> on City ↔ Region toggle. Same panel + same 4 tabs
    /// (D2.A) — binding-key set unchanged across scales (Region presenter mirrors City keys).
    /// </para>
    /// </remarks>
    public class CityStatsHandoffAdapter : MonoBehaviour
    {
        [Header("Presenter (default — Inspector-wired)")]
        [SerializeField] private CityStatsPresenter _presenter;

        [Header("Theme")]
        [SerializeField] private UiTheme _uiTheme;

        [Header("Chrome consumers")]
        [SerializeField] private ThemedPanel _panelChrome;
        [SerializeField] private ThemedTabBar _categoryTabBar;

        [Header("Row consumers (bake-baked slugs)")]
        [SerializeField] private SegmentedReadout _moneyReadout;
        [SerializeField] private SegmentedReadout _populationReadout;
        [SerializeField] private SegmentedReadout _happinessReadout;

        private readonly Dictionary<string, SegmentedReadout> _rowConsumers = new Dictionary<string, SegmentedReadout>(8);
        private readonly HashSet<string> _missingBindingWarned = new HashSet<string>();
        private IStatsPresenter _activePresenter;

        /// <summary>The currently bound presenter (city or region). Null until <see cref="OnEnable"/> runs.</summary>
        public IStatsPresenter ActivePresenter => _activePresenter;

        private void Awake()
        {
            // Inspector-first; FindObjectOfType fallback per guardrail #0.
            if (_presenter == null) _presenter = FindObjectOfType<CityStatsPresenter>();

            // Bake-baked slug → consumer dict (Stage 13.5 row scaffolding seed).
            _rowConsumers["money.balance"] = _moneyReadout;
            _rowConsumers["people.population"] = _populationReadout;
            _rowConsumers["people.happiness"] = _happinessReadout;
        }

        private void OnEnable()
        {
            ApplyThemeToConsumers();
            if (_activePresenter == null) _activePresenter = _presenter;
            SubscribeActive();
            RepaintActiveTabRows();
        }

        private void OnDisable()
        {
            UnsubscribeActive();
        }

        /// <summary>
        /// Stage 13.6 (TECH-9872) — swap the active <see cref="IStatsPresenter"/>.
        /// Unsubscribes the previous source, re-subscribes to <paramref name="presenter"/>,
        /// then repaints. Idempotent: passing the same ref is a no-op.
        /// </summary>
        public void SetPresenter(IStatsPresenter presenter)
        {
            if (presenter == null) return;
            if (ReferenceEquals(presenter, _activePresenter)) return;

            UnsubscribeActive();
            _activePresenter = presenter;
            // Reset per-presenter warn state so missing-binding noise is per-source.
            _missingBindingWarned.Clear();
            SubscribeActive();
            RepaintActiveTabRows();
        }

        private void SubscribeActive()
        {
            if (_activePresenter != null) _activePresenter.OnRefreshed += HandlePresenterRefreshed;
        }

        private void UnsubscribeActive()
        {
            if (_activePresenter != null) _activePresenter.OnRefreshed -= HandlePresenterRefreshed;
        }

        private void HandlePresenterRefreshed()
        {
            RepaintActiveTabRows();
        }

        private void RepaintActiveTabRows()
        {
            if (_activePresenter == null || !_activePresenter.IsReady) return;

            foreach (var kv in _rowConsumers)
            {
                var slug = kv.Key;
                var consumer = kv.Value;
                if (consumer == null) continue;

                if (!_activePresenter.Bindings.TryGetValue(slug, out var producer) || producer == null)
                {
                    if (_missingBindingWarned.Add(slug))
                    {
                        Debug.LogWarning($"CityStatsHandoffAdapter: presenter missing binding '{slug}' — row skipped.");
                    }
                    continue;
                }

                var raw = producer();
                consumer.CurrentValue = ToInt(raw);
            }
        }

        private static int ToInt(object raw)
        {
            switch (raw)
            {
                case int i: return i;
                case long l: return (int)l;
                case float f: return Mathf.RoundToInt(f);
                case double d: return (int)Math.Round(d);
                default: return 0;
            }
        }

        private void ApplyThemeToConsumers()
        {
            if (_uiTheme == null) return;
            if (_panelChrome != null) _panelChrome.ApplyTheme(_uiTheme);
            if (_categoryTabBar != null) _categoryTabBar.ApplyTheme(_uiTheme);
        }
    }
}
