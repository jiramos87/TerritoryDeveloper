using UnityEngine;
using UnityEngine.EventSystems;

namespace Territory.UI.Tooltips
{
    /// <summary>
    /// Hover-trigger marker — bubbles <see cref="IPointerEnterHandler"/> /
    /// <see cref="IPointerExitHandler"/> events to a scene-resident
    /// <see cref="TooltipController"/>. Per §Pending Decisions the marker is a MonoBehaviour
    /// (not a C# attribute) because pure attributes cannot subscribe to Unity event system.
    /// </summary>
    [DisallowMultipleComponent]
    public class TooltipText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string _text;

        /// <summary>Body text rendered inside the tooltip body label.</summary>
        public string Text => _text;

        /// <summary>Inspector / test setter; runtime callers normally bind via Inspector.</summary>
        public void SetText(string text)
        {
            _text = text;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var controller = TooltipController.Instance;
            if (controller != null) controller.HandleEnter(this, eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            var controller = TooltipController.Instance;
            if (controller != null) controller.HandleExit(this, eventData);
        }
    }
}
