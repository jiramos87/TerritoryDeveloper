using System.Collections.Generic;
using UnityEngine;
using Territory.UI.Themed;

namespace Territory.UI.Glossary
{
    /// <summary>
    /// Bridges glossary term registry into baked <c>glossary-panel.prefab</c> Themed SO refs:
    /// rows of terms → <see cref="ThemedList"/>; categories → <see cref="ThemedTabBar"/>.
    /// Mirrors Stage 6 <c>HudBarDataAdapter</c> wiring contract.
    /// </summary>
    /// <remarks>
    /// Inspector-driven term registry (no centralized SO yet — Stage 11 contract). When the
    /// registry is empty the consumers stay null-tolerant. <see cref="UiTheme"/> Inspector-only
    /// (SO; no <c>FindObjectOfType</c> for SOs per invariant #3). No runtime <c>AddComponent</c>.
    /// </remarks>
    public class GlossaryPanelAdapter : MonoBehaviour
    {
        [Header("Theme")]
        [SerializeField] private UiTheme _uiTheme;

        [Header("Term Registry")]
        [SerializeField] private string[] _termLabels;
        [SerializeField] private string[] _categoryLabels;

        [Header("Consumers")]
        [SerializeField] private ThemedList _termList;
        [SerializeField] private ThemedTabBar _categoryTabBar;
        [SerializeField] private ThemedPanel _panelChrome;

        private void Awake()
        {
            // _uiTheme is a ScriptableObject — Inspector-only assignment (invariant #3 cache contract).
        }

        private void OnEnable()
        {
            ApplyThemeToConsumers();
            PopulateConsumers();
        }

        private void OnDisable()
        {
            // No event subscription to tear down.
        }

        private void ApplyThemeToConsumers()
        {
            if (_uiTheme == null) return;
            if (_panelChrome != null) _panelChrome.ApplyTheme(_uiTheme);
            if (_termList != null) _termList.ApplyTheme(_uiTheme);
            if (_categoryTabBar != null) _categoryTabBar.ApplyTheme(_uiTheme);
        }

        private void PopulateConsumers()
        {
            if (_termList != null && _termLabels != null)
            {
                IList<string> rows = _termLabels;
                _termList.Populate(rows, OnTermSelected);
            }
            if (_categoryTabBar != null && _categoryLabels != null && _categoryLabels.Length > 0)
            {
                _categoryTabBar.SetActiveTab(0);
            }
        }

        private void OnTermSelected(int index)
        {
            // Term-detail view wiring deferred — placeholder for downstream consumer hookup.
        }
    }
}
