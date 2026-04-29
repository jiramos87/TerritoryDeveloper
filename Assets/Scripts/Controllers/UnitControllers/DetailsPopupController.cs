using UnityEngine;

namespace Territory.UI
{
    /// <summary>
    /// Stage 12 (game-ui-design-system): legacy popup chrome retired.
    /// Surface is now a thin event-fire shim: <see cref="ShowCellDetails"/> publishes
    /// <see cref="OnCellInfoShown"/> for <c>InfoPanelDataAdapter</c> to consume, then
    /// opens the themed info-panel via <c>UIManager.Instance.OpenPopup</c>.
    /// </summary>
    public class DetailsPopupController : MonoBehaviour
    {
        public event System.Action<string, string, string, string, string> OnCellInfoShown;

        public void ShowCellDetails(string cellType, string zoneType, string population, string landValue, string pollution)
        {
            OnCellInfoShown?.Invoke(cellType, zoneType, population, landValue, pollution);
            if (UIManager.Instance != null)
                UIManager.Instance.OpenPopup(PopupType.InfoPanel);
        }
    }
}
