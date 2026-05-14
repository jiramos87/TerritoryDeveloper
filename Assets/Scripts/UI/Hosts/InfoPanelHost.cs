using Territory.UI.Modals;
using Territory.UI.ViewModels;
using Territory.Core;
using Territory.Zones;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 4.0 (TECH-32918) — MonoBehaviour Host for info-panel UI Toolkit migration.
    /// Effort 5 (post iter-28): adds cell-click inspect routing + Demolish wiring.
    ///   - Polls GridManager.selectedPoint every frame; opens panel when the player
    ///     clicks an occupied cell with no tool armed (zone == Grass, !bulldoze, !details).
    ///   - Populates VM via WorldSelectionResolver field bundle.
    ///   - DemolishCommand routes to GridManager.DemolishAt; closes on success.
    ///   - Esc / close-X / click-outside close via ModalCoordinator.HideMigrated.
    /// </summary>
    public sealed class InfoPanelHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        InfoPanelVM _vm;
        ModalCoordinator _coordinator;
        GridManager _grid;
        UIManager _ui;
        WorldSelectionResolver _resolver;

        Vector2 _lastSeenSelectedPoint = new Vector2(-1, -1);
        Vector2Int _currentCell = new Vector2Int(-1, -1);
        bool _isOpen;

        void OnEnable()
        {
            _vm = new InfoPanelVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[InfoPanelHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("info-panel", _doc.rootVisualElement);

            _grid = FindObjectOfType<GridManager>();
            _ui = FindObjectOfType<UIManager>();
            _resolver = FindObjectOfType<WorldSelectionResolver>();
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void WireCommands()
        {
            _vm.CloseCommand = OnClose;
            _vm.DemolishCommand = OnDemolish;
        }

        void Update()
        {
            // Effort 5 — passive click listener over GridManager.selectedPoint changes.
            if (_grid == null) { _grid = FindObjectOfType<GridManager>(); if (_grid == null) return; }
            if (!_grid.isInitialized) return;

            var sp = _grid.selectedPoint;
            if (sp.x < 0 || sp.y < 0) return;
            if (sp == _lastSeenSelectedPoint) return;
            _lastSeenSelectedPoint = sp;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (_ui != null)
            {
                if (_ui.isBulldozeMode()) return;
                if (_ui.IsDetailsMode()) return;
                var armed = _ui.GetSelectedZoneType();
                if (armed != Zone.ZoneType.Grass && armed != Zone.ZoneType.None) return;
            }

            int x = (int)sp.x;
            int y = (int)sp.y;
            var cell = _grid.GetCell(x, y);
            if (cell == null) return;
            if (!IsInspectable(cell)) return;

            ShowForCell(new Vector2Int(x, y), cell);
        }

        static bool IsInspectable(CityCell c)
        {
            if (c == null) return false;
            var z = c.zoneType;
            if (z == Zone.ZoneType.Grass || z == Zone.ZoneType.None) return c.occupiedBuilding != null;
            if (z == Zone.ZoneType.Water) return false;
            return true; // Road, Forest, all zoning + building variants
        }

        void ShowForCell(Vector2Int coord, CityCell cell)
        {
            _currentCell = coord;
            var entityType = cell.zoneType.ToString();
            string fieldsSummary;
            if (_resolver != null)
            {
                var info = _resolver.Resolve(coord);
                if (info != null && info.fields != null)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var f in info.fields)
                    {
                        if (sb.Length > 0) sb.Append('\n');
                        sb.Append(f.key).Append(": ").Append(f.value);
                    }
                    fieldsSummary = sb.ToString();
                }
                else fieldsSummary = "(no fields)";
            }
            else
            {
                fieldsSummary = $"Height: {cell.height}\nDesirability: {cell.desirability:F1}";
            }
            _vm?.SetSelection(coord.x, coord.y, entityType, fieldsSummary);
            if (_coordinator != null) _coordinator.Show("info-panel");
            _isOpen = true;
        }

        /// <summary>Public surface kept for legacy Alt+click adapter parity.</summary>
        public void ShowForSelection(int x, int y, string entityType, string fieldsSummary = "")
        {
            _currentCell = new Vector2Int(x, y);
            _vm?.SetSelection(x, y, entityType, fieldsSummary);
            if (_coordinator != null) _coordinator.Show("info-panel");
            _isOpen = true;
        }

        void OnClose()
        {
            if (_coordinator != null) _coordinator.HideMigrated("info-panel");
            else gameObject.SetActive(false);
            _isOpen = false;
        }

        void OnDemolish()
        {
            if (_grid == null) _grid = FindObjectOfType<GridManager>();
            if (_grid == null || _currentCell.x < 0 || _currentCell.y < 0)
            {
                Debug.LogWarning("[InfoPanelHost] Demolish requested but GridManager or cell unresolved.");
                OnClose();
                return;
            }
            bool ok = _grid.DemolishAt(_currentCell);
            if (ok)
                GameNotificationManager.Instance?.PostSuccess("Demolished");
            else
                GameNotificationManager.Instance?.PostWarning("Demolish failed");
            OnClose();
        }
    }
}
