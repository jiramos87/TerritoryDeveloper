using UnityEngine;
using TMPro;
using Territory.Economy;
using Territory.UI.StudioControls;
using Territory.UI.Themed;

namespace Territory.UI.CityStatsHandoff
{
    /// <summary>
    /// Bridges live <see cref="Territory.Economy.CityStats"/> + <see cref="CityStatsFacade"/>
    /// producers into baked <c>city-stats-handoff.prefab</c> Themed/StudioControl SO refs.
    /// Mirrors Stage 6 <c>HudBarDataAdapter</c> wiring contract verbatim.
    /// </summary>
    /// <remarks>
    /// Read-only consumer. All refs cached in <see cref="Awake"/> (invariant #3 — never
    /// <see cref="MonoBehaviour.FindObjectOfType{T}()"/> in <see cref="Update"/>). Per-channel
    /// null-check on producer + consumer refs so partial init still surfaces ready channels.
    /// MonoBehaviour producers fall back to <see cref="MonoBehaviour.FindObjectOfType{T}()"/> in
    /// <see cref="Awake"/> when Inspector slot empty (invariant #4); <see cref="UiTheme"/>
    /// Inspector-only (SO; no <c>FindObjectOfType</c> for SOs per Stage 6 precedent).
    /// Bake-time row hierarchy + digit widths + captions provided by Stage 13.2 UiBakeHandler v2 —
    /// no runtime <c>AddComponent</c> here.
    /// </remarks>
    public class CityStatsHandoffAdapter : MonoBehaviour
    {
        [Header("Producers")]
        [SerializeField] private Territory.Economy.CityStats _cityStats;
        [SerializeField] private CityStatsFacade _cityStatsFacade;

        [Header("Theme")]
        [SerializeField] private UiTheme _uiTheme;

        [Header("Consumers")]
        [SerializeField] private ThemedPanel _panelChrome;
        [SerializeField] private ThemedTabBar _categoryTabBar;
        [SerializeField] private SegmentedReadout _moneyReadout;
        [SerializeField] private SegmentedReadout _populationReadout;
        [SerializeField] private SegmentedReadout _happinessReadout;

        private void Awake()
        {
            // MonoBehaviour producers — Inspector first, FindObjectOfType fallback (invariant #4).
            if (_cityStats == null) _cityStats = FindObjectOfType<Territory.Economy.CityStats>();
            if (_cityStatsFacade == null) _cityStatsFacade = FindObjectOfType<CityStatsFacade>();
            // _uiTheme is a ScriptableObject — Inspector-only assignment (Stage 6 precedent).
        }

        private void OnEnable()
        {
            ApplyThemeToConsumers();
        }

        private void OnDisable()
        {
            // No event subscription to tear down — facade exposes OnTickEnd but adapter polls in Update for parity with Stage 6.
        }

        private void Update()
        {
            if (_cityStats == null) return;

            if (_moneyReadout != null)
            {
                _moneyReadout.CurrentValue = _cityStats.money;
            }
            if (_populationReadout != null)
            {
                _populationReadout.CurrentValue = _cityStats.population;
            }
            if (_happinessReadout != null)
            {
                _happinessReadout.CurrentValue = (int)_cityStats.happiness;
            }
        }

        private void ApplyThemeToConsumers()
        {
            if (_uiTheme == null) return;
            if (_panelChrome != null) _panelChrome.ApplyTheme(_uiTheme);
            if (_categoryTabBar != null) _categoryTabBar.ApplyTheme(_uiTheme);
        }
    }
}
