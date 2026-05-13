using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Territory.UI.HUD
{
    /// <summary>Invisible Graphic on the hud-bar money readout that catches pointer clicks and opens the BudgetPanel popup via UIManager.</summary>
    /// <remarks>Inherits from Graphic so raycastTarget is satisfied without a visible Image. OnPopulateMesh emits nothing — zero vertex cost.</remarks>
    [System.Obsolete("Strangler — replaced by VM-direct Host on UIToolkit. See DEC-A28.")]
    public class MoneyReadoutBudgetToggle : Graphic, IPointerClickHandler
    {
        [SerializeField] private UIManager _uiManager;

        protected override void Awake()
        {
            base.Awake();
            if (_uiManager == null) _uiManager = FindObjectOfType<UIManager>();
            raycastTarget = true;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left) return;
            if (_uiManager == null) _uiManager = FindObjectOfType<UIManager>();
            if (_uiManager != null) _uiManager.OpenBudgetPanel();
        }
    }
}
