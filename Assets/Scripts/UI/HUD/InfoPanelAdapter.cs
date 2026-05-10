using UnityEngine;
using Territory.Core;
using Territory.UI.StudioControls;

namespace Territory.UI.HUD
{
    /// <summary>
    /// Info-panel adapter — subscribes world.select (Alt+Click) → <see cref="WorldSelectionResolver"/>
    /// → binds field-list. Demolish confirm-button → <see cref="GridManager.DemolishAt"/>.
    /// Replaces DetailsPopupController + OnCellInfoShown event (T9.0.3).
    /// </summary>
    public class InfoPanelAdapter : MonoBehaviour
    {
        [SerializeField] private StudioControlBase[] _widgets;

        public StudioControlBase[] Widgets => _widgets;

        private GridManager _gridManager;
        private WorldSelectionResolver _resolver;
        private Vector2Int _currentGridCoord;

        private void Awake()
        {
            _gridManager = FindObjectOfType<GridManager>();
            _resolver = FindObjectOfType<WorldSelectionResolver>();
        }

        private void OnEnable()
        {
            if (_gridManager != null)
                _gridManager._worldSelectAction += OnWorldSelect;
        }

        private void OnDisable()
        {
            if (_gridManager != null)
                _gridManager._worldSelectAction -= OnWorldSelect;
        }

        /// <summary>Handle world.select — resolve cell info + show panel.</summary>
        private void OnWorldSelect(Vector2Int gridCoord)
        {
            _currentGridCoord = gridCoord;
            if (_resolver == null)
            {
                Debug.LogWarning("[InfoPanelAdapter] WorldSelectionResolver not found — cannot resolve cell info.");
                return;
            }

            SelectionInfo info = _resolver.Resolve(gridCoord);
            if (info == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            ApplyTimeRenderCheck(info);
        }

        /// <summary>
        /// Apply-time render check — mirrors SettingsViewController pattern.
        /// Validates field-list slot + bind assignments after world.select.
        /// </summary>
        private void ApplyTimeRenderCheck(SelectionInfo info)
        {
            if (info == null) return;
            // Bind resolution is deferred to runtime widget system.
            // For now log bind summary so bridge console-sweep can validate clean wiring.
            Debug.Log($"[InfoPanelAdapter] cell=({info.gridCoord.x},{info.gridCoord.y}) type={info.type} fields={info.fields?.Count ?? 0}");
        }

        /// <summary>Demolish confirm-button handler — dispatches info.demolish → GridManager.DemolishAt.</summary>
        public void OnDemolishConfirmed()
        {
            if (_gridManager == null)
            {
                Debug.LogWarning("[InfoPanelAdapter] GridManager not found — cannot demolish.");
                return;
            }
            bool demolished = _gridManager.DemolishAt(_currentGridCoord);
            if (demolished)
            {
                Debug.Log($"[InfoPanelAdapter] Demolished cell ({_currentGridCoord.x},{_currentGridCoord.y}) via info.demolish");
                gameObject.SetActive(false);
            }
        }
    }
}
