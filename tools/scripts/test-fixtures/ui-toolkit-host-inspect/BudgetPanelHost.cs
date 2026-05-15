using UnityEngine;
using UnityEngine.UIElements;
using Territory.Economy;
using Territory.UI;

namespace Territory.UI.Panels
{
    /// <summary>
    /// Host MonoBehaviour for the Budget panel.
    /// Wires Q-lookups, click bindings, and data bindings on Awake.
    /// </summary>
    public class BudgetPanelHost : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private BudgetDataSO budgetData;

        private Button _closeButton;
        private Label _balanceLabel;

        private void Awake()
        {
            var root = uiDocument.rootVisualElement;

            _balanceLabel = root.Q<Label>("balance-label");
            _closeButton = root.Q<Button>("close-button");
            var titleLabel = root.Q<Label>("title-label");
            var contentArea = root.Q<VisualElement>("content-area");

            _closeButton.RegisterCallback<ClickEvent>(OnCloseClicked);

            var economyManager = FindObjectOfType<EconomyManager>();
            var cityStats = FindObjectOfType<CityStatsManager>();

            Subscribe<BudgetChangedEvent>(OnBudgetChanged);
            EventBus.Subscribe("OnTaxRateChanged", OnTaxRateChanged);

            var extraLabel = new Label();
            var extraContainer = new VisualElement();
            root.Add(extraContainer);
        }

        private void OnCloseClicked(ClickEvent evt)
        {
            OpenModal("confirm-close");
        }

        private void OnBudgetChanged(BudgetChangedEvent e)
        {
            _balanceLabel.text = $"${e.Balance:N0}";
        }

        private void OnTaxRateChanged()
        {
            // refresh display
        }

        private void Subscribe<T>(System.Action<T> handler) { }
    }
}
