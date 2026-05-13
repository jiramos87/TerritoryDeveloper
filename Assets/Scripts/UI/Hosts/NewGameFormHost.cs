using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host — resolves NewGameFormVM and sets UIDocument.rootVisualElement.dataSource.
    /// Lives on the UIDocument GameObject in MainMenu scene (sidecar coexistence per Q2).
    /// Handles form lifecycle: seeding city name on show, reading values on submit.
    /// Legacy NewGameScreenDataAdapter remains alive until Stage 6.0 quarantine plan.
    /// </summary>
    public sealed class NewGameFormHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        NewGameFormVM _vm;

        void OnEnable()
        {
            _vm = new NewGameFormVM();
            SeedDefaults();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[NewGameFormHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void SeedDefaults()
        {
            _vm.MapSize = "medium";
            _vm.Budget = "medium";
            _vm.Seed = UnityEngine.Random.Range(1, 99999);
            _vm.CityName = Modals.CityNamePoolService.TryRollRandom() ?? $"Ciudad-{UnityEngine.Random.Range(100, 999)}";
        }

        void WireCommands()
        {
            _vm.SubmitCommand = OnSubmit;
            _vm.CancelCommand = OnCancel;
        }

        void OnSubmit()
        {
            int mapSizeInt = MapSizeToInt(_vm.MapSize);
            int budgetInt = BudgetToInt(_vm.Budget);
            var mainMenu = FindObjectOfType<MainMenuController>();
            if (mainMenu != null)
                mainMenu.StartNewGame(mapSizeInt, budgetInt, _vm.CityName, _vm.Seed);
            else
                Debug.LogWarning("[NewGameFormHost] MainMenuController not found — stub submit.");
        }

        void OnCancel()
        {
            gameObject.SetActive(false);
        }

        static int MapSizeToInt(string value)
        {
            switch (value)
            {
                case "small": return 1;
                case "large": return 3;
                default: return 2;
            }
        }

        static int BudgetToInt(string value)
        {
            switch (value)
            {
                case "low": return 10000;
                case "high": return 200000;
                default: return 50000;
            }
        }
    }
}
