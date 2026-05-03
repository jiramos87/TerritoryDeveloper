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
    /// No runtime <c>AddComponent</c>.
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

            // Bake-time digits=1 truncates values to a single character; widen per channel so
            // money/population/happiness render at their natural magnitude.
            ApplyDigitWidth(_moneyReadout, 8);
            ApplyDigitWidth(_populationReadout, 6);
            ApplyDigitWidth(_happinessReadout, 3);

            // Inject caption above each readout so the panel reads as a stats list, not a row of
            // bare digit boxes. Labels are per-instance children — runtime AddComponent on a fresh
            // GameObject is permitted by Stage 6 precedent (no AddComponent on existing nodes).
            EnsureCaption(_moneyReadout, "MONEY");
            EnsureCaption(_populationReadout, "POPULATION");
            EnsureCaption(_happinessReadout, "HAPPINESS");
        }

        private static void ApplyDigitWidth(SegmentedReadout readout, int digits)
        {
            if (readout == null || readout.Detail == null) return;
            if (readout.Detail.digits >= digits) return;
            readout.Detail.digits = digits;
        }

        private static void EnsureCaption(SegmentedReadout readout, string captionText)
        {
            if (readout == null) return;
            var parent = readout.transform;
            if (parent.Find("Caption") != null) return;

            var go = new GameObject("Caption", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 4f);
            rect.sizeDelta = new Vector2(0f, 20f);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = captionText;
            label.fontSize = 14f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            label.raycastTarget = false;
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
