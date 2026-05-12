using UnityEngine;
using Territory.UI.StudioControls;
using Territory.UI.Registry;
using Domains.UI.Services;

namespace Territory.UI.Toolbar
{
    /// <summary>
    /// Hub adapter — bridges UIManager selection into IlluminatedButton toolbar slots.
    /// Logic delegated to <see cref="ToolbarAdapterService"/> (Stage 5.7 Tier-C THIN).
    /// FILE PATH UNCHANGED. CLASS NAME UNCHANGED. NAMESPACE UNCHANGED.
    /// [SerializeField] field set UNCHANGED.
    /// </summary>
    public class ToolbarDataAdapter : MonoBehaviour
    {
        // ── Producer ref ──────────────────────────────────────────────────────────

        [Header("Producer")]
        [SerializeField] private UIManager _uiManager;

        // ── Wave B1 (TECH-27080) — action + bind registries + picker root ─────────

        [Header("Wave B1 — Action + Bind registries")]
        [SerializeField] private UiActionRegistry _actionRegistry;
        [SerializeField] private UiBindRegistry _bindRegistry;

        [Header("Wave B1 — Subtype Picker root (mount under toolbar root)")]
        [SerializeField] private GameObject _subtypePickerRoot;

        // ── Theme cache ────────────────────────────────────────────────────────────

        [Header("Theme")]
        [SerializeField] private UiTheme _uiTheme;

        // DS-* token audit — TECH-15227: migrate ad-hoc Color literals to UiTheme palette in Stage N.

        // ── Consumer refs (kept for Inspector visibility; service owns live arrays) ─

        /// <summary>Index 0..2 = Residential L/M/H; 3..5 = Commercial L/M/H; 6..8 = Industrial L/M/H; 9 = State.</summary>
        [Header("Consumers — zoning (length 10)")]
        [SerializeField] private IlluminatedButton[] _zoningButtons;

        [Header("Consumers — road (length 1)")]
        [SerializeField] private IlluminatedButton[] _roadButtons;

        [Header("Consumers — terrain (length 1: grass)")]
        [SerializeField] private IlluminatedButton[] _terrainButtons;

        /// <summary>Index 0 = nuclear-power-plant; 1 = water-pump-medium.</summary>
        [Header("Consumers — buildings (length 2)")]
        [SerializeField] private IlluminatedButton[] _buildingButtons;

        /// <summary>Index 0 = sparse; 1 = medium; 2 = dense.</summary>
        [Header("Consumers — forest (length 3)")]
        [SerializeField] private IlluminatedButton[] _forestButtons;

        [Header("Consumers — bulldoze")]
        [SerializeField] private IlluminatedButton _bulldozeButton;

        // ── Service ───────────────────────────────────────────────────────────────

        private readonly ToolbarAdapterService _svc = new ToolbarAdapterService();

        private void Awake()
        {
            // MonoBehaviour producer — Inspector first, FindObjectOfType fallback (invariant #4).
            if (_uiManager      == null) _uiManager      = FindObjectOfType<UIManager>();
            if (_actionRegistry == null) _actionRegistry = FindObjectOfType<UiActionRegistry>();
            if (_bindRegistry   == null) _bindRegistry   = FindObjectOfType<UiBindRegistry>();

            _svc.Wire(_uiManager, _actionRegistry, _bindRegistry, _subtypePickerRoot);
            _svc.RebindButtonsByIconSlug(transform);
            _svc.SubscribeClicks();
            _svc.RegisterActions();
            _svc.InitPickerBinds();
        }

        private void OnDestroy()
        {
            _svc.UnsubscribeClicks();
        }

        private void Update()
        {
            _svc.UpdateIllumination();
        }
    }
}
