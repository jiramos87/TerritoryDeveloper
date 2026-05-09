using System;
using UnityEngine;
using Territory.UI.Registry;

namespace Territory.UI.Modals
{
    /// <summary>
    /// Marshals card/chip/text-input bind subscriptions from the New Game screen and invokes
    /// <see cref="MainMenuController.StartNewGame"/> on confirm. Inspector producer slot with
    /// FindObjectOfType fallback; UiActionRegistry/UiBindRegistry resolved in Awake.
    /// Wave A2 (TECH-27070): drops legacy sliders + scenario toggles; adds card/chip/input binds.
    /// </summary>
    public class NewGameScreenDataAdapter : MonoBehaviour
    {
        [Header("Producer")]
        [SerializeField] private MainMenuController _mainMenu;

        [Header("Registries (optional — resolved via FindObjectOfType if null)")]
        [SerializeField] private UiActionRegistry _actionRegistry;
        [SerializeField] private UiBindRegistry _bindRegistry;

        // Resolved bind values.
        private string _mapSize  = "medium";
        private string _budget   = "medium";
        private string _cityName = string.Empty;
        private int    _seed     = 0;

        // Subscription handles — disposed in OnDisable.
        private IDisposable _mapSizeSub;
        private IDisposable _budgetSub;
        private IDisposable _cityNameSub;

        private void Awake()
        {
            if (_mainMenu == null)
                _mainMenu = FindObjectOfType<MainMenuController>();
            if (_actionRegistry == null)
                _actionRegistry = FindObjectOfType<UiActionRegistry>();
            if (_bindRegistry == null)
                _bindRegistry = FindObjectOfType<UiBindRegistry>();
        }

        private void OnEnable()
        {
            if (_actionRegistry != null)
            {
                _actionRegistry.Register("newgame.mapSize.set",    p => OnMapSizeSet(p as string));
                _actionRegistry.Register("newgame.budget.set",     p => OnBudgetSet(p as string));
                _actionRegistry.Register("newgame.cityName.reroll",_ => OnCityNameReroll());
                _actionRegistry.Register("mainmenu.startNewGame",  _ => OnConfirm());
            }

            if (_bindRegistry != null)
            {
                _mapSizeSub  = _bindRegistry.Subscribe<string>("newgame.mapSize",  OnMapSizeChanged);
                _budgetSub   = _bindRegistry.Subscribe<string>("newgame.budget",   OnBudgetChanged);
                _cityNameSub = _bindRegistry.Subscribe<string>("newgame.cityName", OnCityNameChanged);
            }
        }

        private void OnDisable()
        {
            _mapSizeSub?.Dispose();  _mapSizeSub  = null;
            _budgetSub?.Dispose();   _budgetSub   = null;
            _cityNameSub?.Dispose(); _cityNameSub = null;
        }

        // ── Bind callbacks ────────────────────────────────────────────────────

        private void OnMapSizeChanged(string value)   { if (value != null) _mapSize  = value; }
        private void OnBudgetChanged(string value)    { if (value != null) _budget   = value; }
        private void OnCityNameChanged(string value)  { _cityName = value ?? string.Empty; }

        // ── Action handlers ───────────────────────────────────────────────────

        private void OnMapSizeSet(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            _mapSize = value;
            _bindRegistry?.Set("newgame.mapSize", _mapSize);
        }

        private void OnBudgetSet(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            _budget = value;
            _bindRegistry?.Set("newgame.budget", _budget);
        }

        private void OnCityNameReroll()
        {
            // Roll a name from the city-name-pool-es string pool when CityNamePoolService is
            // available; fallback to a seeded placeholder so the field never stays blank.
            var name = CityNamePoolService.TryRollRandom() ?? $"Ciudad-{UnityEngine.Random.Range(100, 999)}";
            _cityName = name;
            _bindRegistry?.Set("newgame.cityName", _cityName);
        }

        private void OnConfirm()
        {
            if (_mainMenu == null) return;
            int mapSizeInt = MapSizeToInt(_mapSize);
            int budgetInt  = BudgetToInt(_budget);
            _mainMenu.StartNewGame(mapSizeInt, budgetInt, _cityName, _seed);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static int MapSizeToInt(string value)
        {
            switch (value)
            {
                case "small":  return 1;
                case "large":  return 3;
                default:       return 2; // medium
            }
        }

        private static int BudgetToInt(string value)
        {
            switch (value)
            {
                case "low":  return 10000;
                case "high": return 200000;
                default:     return 50000; // medium
            }
        }
    }
}
