using UnityEngine;
using Territory.UI.Themed;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Marshals slider/toggle values from the New Game screen and invokes
    /// <see cref="MainMenuController.StartNewGame"/> on confirm. Inspector producer slot with
    /// FindObjectOfType fallback (invariant #4); UiTheme cached in Awake (invariant #3).
    /// </summary>
    public class NewGameScreenDataAdapter : MonoBehaviour
    {
        [Header("Producer")]
        [SerializeField] private MainMenuController _mainMenu;

        [Header("Consumers — Sliders")]
        [SerializeField] private ThemedSlider _mapSizeSlider;
        [SerializeField] private ThemedSlider _seedSlider;

        [Header("Consumers — Scenario Toggles")]
        [SerializeField] private ThemedToggle[] _scenarioToggles;

        [Header("Consumers — Buttons")]
        [SerializeField] private ThemedButton _confirmButton;
        [SerializeField] private ThemedButton _backButton;

        private void Awake()
        {
            if (_mainMenu == null)
                _mainMenu = FindObjectOfType<MainMenuController>();
        }

        private void OnEnable()
        {
            if (_confirmButton != null) _confirmButton.OnClicked += OnConfirm;
            if (_backButton != null) _backButton.OnClicked += OnBack;

            for (int i = 0; i < (_scenarioToggles?.Length ?? 0); i++)
            {
                int captured = i;
                if (_scenarioToggles[captured] != null)
                    _scenarioToggles[captured].OnToggled += _ => EnforceExclusiveToggle(captured);
            }
        }

        private void OnDisable()
        {
            if (_confirmButton != null) _confirmButton.OnClicked -= OnConfirm;
            if (_backButton != null) _backButton.OnClicked -= OnBack;
        }

        private void EnforceExclusiveToggle(int activeIndex)
        {
            for (int i = 0; i < (_scenarioToggles?.Length ?? 0); i++)
            {
                if (_scenarioToggles[i] == null || i == activeIndex) continue;
                var toggle = _scenarioToggles[i].GetComponent<UnityEngine.UI.Toggle>();
                if (toggle != null) toggle.SetIsOnWithoutNotify(false);
            }
        }

        private int ActiveScenarioIndex()
        {
            for (int i = 0; i < (_scenarioToggles?.Length ?? 0); i++)
            {
                if (_scenarioToggles[i] == null) continue;
                var toggle = _scenarioToggles[i].GetComponent<UnityEngine.UI.Toggle>();
                if (toggle != null && toggle.isOn) return i;
            }
            return 0;
        }

        private void OnConfirm()
        {
            if (_mainMenu == null) return;
            int mapSize = _mapSizeSlider != null ? Mathf.RoundToInt(_mapSizeSlider.GetComponent<UnityEngine.UI.Slider>()?.value ?? 1f) : 1;
            int seed = _seedSlider != null ? Mathf.RoundToInt(_seedSlider.GetComponent<UnityEngine.UI.Slider>()?.value ?? 0f) : 0;
            _mainMenu.StartNewGame(mapSize, seed, ActiveScenarioIndex());
        }

        private void OnBack()
        {
            gameObject.SetActive(false);
        }
    }
}
