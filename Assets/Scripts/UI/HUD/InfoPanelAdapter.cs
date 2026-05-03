using UnityEngine;
using Territory.UI.StudioControls;

namespace Territory.UI.HUD
{
    /// <summary>
    /// Step 16 D2.1 — bake-time adapter attached to the <c>info-panel</c> prefab. Holds an
    /// ordered <see cref="StudioControlBase"/> array (slot order, then child order within slot)
    /// captured at bake time so Stage 13 cell-data wiring can resolve widgets by their canonical
    /// IR-rooted <see cref="StudioControlBase.Slug"/> instead of runtime <c>GetComponentsInChildren</c>
    /// scans (invariant #3).
    /// </summary>
    /// <remarks>
    /// MVP slice: skeleton only — Awake/OnEnable/OnDisable are intentionally empty. Producer
    /// subscriptions land in Stage 13 (cell-selection cell-data feed).
    /// </remarks>
    public class InfoPanelAdapter : MonoBehaviour
    {
        [SerializeField] private StudioControlBase[] _widgets;

        public StudioControlBase[] Widgets => _widgets;

        private void Awake() { }

        private void OnEnable() { }

        private void OnDisable() { }
    }
}
