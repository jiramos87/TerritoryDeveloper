using UnityEngine;
using Territory.SceneManagement;
using Territory.UI.Panels;

namespace Territory.RegionScene.UI
{
    /// <summary>RegionScene MonoBehaviour — listens for player 2×2 anchor cell click; opens ConfirmTransitionPanel with "Enter city?" copy variant. Invariant #3: cache refs in Awake; subscribe in Start.</summary>
    public sealed class PlayerTileClickHandler : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [SerializeField] private Camera mainCamera;

        /// <summary>Player anchor cell — top-left corner of the 2×2 block.</summary>
        [SerializeField] private Vector2Int playerAnchorCell = new Vector2Int(31, 31);

        // ── Private ──────────────────────────────────────────────────────────
        private ZoomTransitionController _transitionController;
        private ConfirmTransitionPanelController _confirmPanel;

        void Start()
        {
            _transitionController = FindObjectOfType<ZoomTransitionController>();
            _confirmPanel         = FindObjectOfType<ConfirmTransitionPanelController>();

            if (mainCamera == null) mainCamera = Camera.main;
        }

        void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            var cell = ScreenToCell(Input.mousePosition);
            if (!IsPlayerAnchor(cell)) return;

            // Open confirm panel with "Enter city?" copy.
            if (_confirmPanel != null)
            {
                _confirmPanel.ShowForTarget(IsoSceneContext.City);
            }
            else if (_transitionController != null)
            {
                // Fallback: no confirm panel wired — auto-transition directly.
                _transitionController.AutoConfirm = true;
                _ = _transitionController.RequestTransition(IsoSceneContext.City,
                    destroyCancellationToken);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private Vector2Int ScreenToCell(Vector3 screenPos)
        {
            if (mainCamera == null) return new Vector2Int(-1, -1);
            Vector3 world = mainCamera.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, Mathf.Abs(mainCamera.transform.position.z)));
            float wx = world.x;
            float wy = world.y;
            int col = Mathf.RoundToInt(wx + 2f * wy);
            int row = Mathf.RoundToInt(2f * wy - wx);
            return new Vector2Int(col, row);
        }

        /// <summary>True when the clicked cell falls within the player 2×2 anchor block.</summary>
        private bool IsPlayerAnchor(Vector2Int cell)
        {
            return cell.x >= playerAnchorCell.x && cell.x < playerAnchorCell.x + 2
                && cell.y >= playerAnchorCell.y && cell.y < playerAnchorCell.y + 2;
        }
    }
}
