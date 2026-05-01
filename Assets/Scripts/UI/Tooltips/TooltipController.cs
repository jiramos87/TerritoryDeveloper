using TMPro;
using Territory.UI.Themed;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Territory.UI.Tooltips
{
    /// <summary>
    /// Scene-resident tooltip spawner — instantiates the baked tooltip prefab as a child of
    /// the parent <c>UI Canvas</c> on hover enter; destroys on hover exit. Caches
    /// <see cref="UiTheme"/> ref + Inspector <see cref="_tooltipPrefab"/> in <c>Awake</c>
    /// per invariants #3 and #4.
    /// </summary>
    [DisallowMultipleComponent]
    public class TooltipController : MonoBehaviour
    {
        /// <summary>Process-singleton resolved at <c>Awake</c>. Marker components dispatch through this.</summary>
        public static TooltipController Instance { get; private set; }

        [SerializeField] private GameObject _tooltipPrefab;
        [SerializeField] private UiTheme _themeRef;

        private RectTransform _canvasRect;
        private GameObject _activeTooltip;
        private TooltipText _activeTrigger;

        private void Awake()
        {
            Instance = this;
            if (_themeRef == null)
            {
                _themeRef = FindObjectOfType<UiTheme>();
            }
            // Cache parent canvas RectTransform (invariant #3 — no per-frame parent lookup).
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _canvasRect = canvas.GetComponent<RectTransform>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Spawn tooltip at pointer screen position; called by <see cref="TooltipText"/> on enter.</summary>
        public void HandleEnter(TooltipText trigger, PointerEventData eventData)
        {
            if (trigger == null || _tooltipPrefab == null || _canvasRect == null) return;
            // Replace any prior tooltip — single-instance lifecycle (§Pending Decisions).
            DestroyActive();
            _activeTooltip = Instantiate(_tooltipPrefab, _canvasRect);
            _activeTrigger = trigger;
            var tipRect = _activeTooltip.GetComponent<RectTransform>();
            if (tipRect != null && eventData != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRect, eventData.position, eventData.pressEventCamera, out var localPoint);
                tipRect.anchoredPosition = localPoint;
            }
            // Body text — find child TMP_Text inside the spawned tooltip prefab tree.
            var tmp = _activeTooltip.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) tmp.text = trigger.Text ?? string.Empty;
        }

        /// <summary>Destroy tooltip on hover exit when current trigger matches.</summary>
        public void HandleExit(TooltipText trigger, PointerEventData eventData)
        {
            _ = eventData;
            if (_activeTrigger == trigger) DestroyActive();
        }

        private void DestroyActive()
        {
            if (_activeTooltip != null)
            {
                Destroy(_activeTooltip);
                _activeTooltip = null;
            }
            _activeTrigger = null;
        }
    }
}
