using UnityEngine;
using Territory.UI.Themed;

namespace Territory.UI.CityStatsHandoff
{
    /// <summary>
    /// Stage 13.6 (TECH-9872) — HUD scale-switcher widget. Enumerates
    /// <see cref="Scale.City"/> + <see cref="Scale.Region"/> only — Country / World
    /// hidden entirely per D9.A (NOT greyed). Toggling rebinds the existing
    /// <see cref="CityStatsHandoffAdapter"/> to the active-scope
    /// <see cref="IStatsPresenter"/> via <see cref="CityStatsHandoffAdapter.SetPresenter"/>.
    /// Same panel, same 4 tabs, same binding-key set (D2.A).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Active-scope storage: instance enum field <see cref="ActiveScale"/> (HUD-scope —
    /// per-widget, not static; survives panel close/reopen as long as the HUD
    /// container persists, satisfying §Acceptance "active scale persists across
    /// panel open/close within session"). Save-game persistence out of MVP scope.
    /// </para>
    /// <para>
    /// Wiring: Inspector-first per invariant #4. Buttons + presenters + adapter
    /// pre-baked on the HUD prefab; no runtime <see cref="GameObject.AddComponent"/>
    /// (invariant #6). Country / World omission is UI-level — the enum simply
    /// excludes those values, so they cannot leak into the widget at runtime.
    /// </para>
    /// </remarks>
    public class StatsScaleSwitcher : MonoBehaviour
    {
        /// <summary>Active stats scale. City + Region only per D9.A — Country / World absent intentionally.</summary>
        public enum Scale
        {
            City = 0,
            Region = 1,
        }

        [Header("Adapter")]
        [SerializeField] private CityStatsHandoffAdapter _adapter;

        [Header("Presenters (Inspector-wired; FindObjectOfType fallback in Awake)")]
        [SerializeField] private CityStatsPresenter _cityPresenter;
        [SerializeField] private RegionStatsPresenter _regionPresenter;

        [Header("Buttons (City + Region — Country / World omitted per D9.A)")]
        [SerializeField] private ThemedButton _cityButton;
        [SerializeField] private ThemedButton _regionButton;

        [Header("Default scale on first activation")]
        [SerializeField] private Scale _defaultScale = Scale.City;

        private Scale _activeScale;
        private bool _wired;

        /// <summary>Currently active stats scale. Defaults to <see cref="Scale.City"/>.</summary>
        public Scale ActiveScale => _activeScale;

        /// <summary>Fires after a successful scale change (post-adapter rebind).</summary>
        public event System.Action<Scale> OnScaleChanged;

        private void Awake()
        {
            // Inspector-first; FindObjectOfType fallback per invariant #4 / guardrail #0.
            if (_adapter == null) _adapter = FindObjectOfType<CityStatsHandoffAdapter>();
            if (_cityPresenter == null) _cityPresenter = FindObjectOfType<CityStatsPresenter>();
            if (_regionPresenter == null) _regionPresenter = FindObjectOfType<RegionStatsPresenter>();
            _activeScale = _defaultScale;
        }

        private void OnEnable()
        {
            WireButtons();
            ApplyActiveScale();
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        private void WireButtons()
        {
            if (_wired) return;
            if (_cityButton != null) _cityButton.OnClicked += HandleCityClicked;
            if (_regionButton != null) _regionButton.OnClicked += HandleRegionClicked;
            _wired = true;
        }

        private void UnwireButtons()
        {
            if (!_wired) return;
            if (_cityButton != null) _cityButton.OnClicked -= HandleCityClicked;
            if (_regionButton != null) _regionButton.OnClicked -= HandleRegionClicked;
            _wired = false;
        }

        private void HandleCityClicked() => SetScale(Scale.City);
        private void HandleRegionClicked() => SetScale(Scale.Region);

        /// <summary>
        /// Toggle the active scale. Idempotent on same-scale calls. On change,
        /// rebinds the adapter to the active-scope presenter via
        /// <see cref="CityStatsHandoffAdapter.SetPresenter"/> and fires
        /// <see cref="OnScaleChanged"/>.
        /// </summary>
        public void SetScale(Scale scale)
        {
            if (scale == _activeScale && _adapter != null && _adapter.ActivePresenter != null) return;
            _activeScale = scale;
            ApplyActiveScale();
            OnScaleChanged?.Invoke(_activeScale);
        }

        private void ApplyActiveScale()
        {
            if (_adapter == null) return;
            IStatsPresenter target = _activeScale == Scale.Region ? (IStatsPresenter)_regionPresenter : _cityPresenter;
            if (target == null) return;
            _adapter.SetPresenter(target);
        }
    }
}
