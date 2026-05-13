using UnityEngine;
using Territory.UI.Themed;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Bridges <see cref="DetailsPopupController"/> cell-select event to ThemedLabel consumer
    /// slots and ThemedTabBar tab activation. Inspector producer slot with FindObjectOfType
    /// fallback (invariant #4); UiTheme cached in Awake (invariant #3).
    /// </summary>
    [System.Obsolete("Strangler — replaced by VM-direct Host on UIToolkit. See DEC-A28.")]
    public class InfoPanelDataAdapter : MonoBehaviour
    {
        [Header("Producer")]
        [SerializeField] private DetailsPopupController _detailsPopup;

        [Header("Consumers")]
        [SerializeField] private ThemedLabel _cellTypeLabel;
        [SerializeField] private ThemedLabel _zoneTypeLabel;
        [SerializeField] private ThemedLabel _populationLabel;
        [SerializeField] private ThemedLabel _landValueLabel;
        [SerializeField] private ThemedLabel _pollutionLabel;
        [SerializeField] private ThemedTabBar _tabBar;

        private void Awake()
        {
            if (_detailsPopup == null)
                _detailsPopup = FindObjectOfType<DetailsPopupController>();
        }

        private void OnEnable()
        {
            if (_detailsPopup != null)
                _detailsPopup.OnCellInfoShown += OnCellInfoShown;
        }

        private void OnDisable()
        {
            if (_detailsPopup != null)
                _detailsPopup.OnCellInfoShown -= OnCellInfoShown;
        }

        private void OnCellInfoShown(string cellType, string zoneType, string population, string landValue, string pollution)
        {
            if (_cellTypeLabel != null) _cellTypeLabel.Detail = cellType;
            if (_zoneTypeLabel != null) _zoneTypeLabel.Detail = zoneType;
            if (_populationLabel != null) _populationLabel.Detail = population;
            if (_landValueLabel != null) _landValueLabel.Detail = landValue;
            if (_pollutionLabel != null) _pollutionLabel.Detail = pollution;
            if (_tabBar != null) _tabBar.SetActiveTab(0);
        }
    }
}
