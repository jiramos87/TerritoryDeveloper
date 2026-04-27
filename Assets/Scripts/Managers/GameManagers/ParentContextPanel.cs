using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Territory.Core;

namespace Territory.UI
{
    /// <summary>
    /// City HUD panel: reads cached <see cref="GridManager.ParentRegionId"/> +
    /// <see cref="GridManager.ParentCountryId"/> Step 1 stubs and renders two
    /// placeholder labels (region + country). Establishes the parent-id consumer
    /// pattern that Step 3 region UI will flesh out with resolved display names.
    ///
    /// Inert: read-only, no input handling, no live refresh (one-shot at Start).
    /// Null-id handling renders <c>(none)</c> literal so dev surfaces missing-seed
    /// regressions instead of NRE.
    /// </summary>
    public class ParentContextPanel : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private GridManager _gridManager;

        [Header("Labels")]
        [SerializeField] private TMP_Text _regionLabel;
        [SerializeField] private TMP_Text _countryLabel;

        private bool _hydrated;

        private void Awake()
        {
            if (_gridManager == null)
                _gridManager = FindObjectOfType<GridManager>();
        }

        private void Start()
        {
            HydrateLabels();
        }

        private void OnEnable()
        {
            // Re-hydrate when re-enabled after Start (e.g. HUD toggle in dev).
            if (_hydrated)
                HydrateLabels();
        }

        private void HydrateLabels()
        {
            if (_gridManager == null)
            {
                Debug.LogWarning("ParentContextPanel: GridManager missing");
                return;
            }

            EnsureRuntimeLabelsIfNeeded();

            string regionId = string.IsNullOrEmpty(_gridManager.ParentRegionId)
                ? "(none)"
                : _gridManager.ParentRegionId;
            string countryId = string.IsNullOrEmpty(_gridManager.ParentCountryId)
                ? "(none)"
                : _gridManager.ParentCountryId;

            if (_regionLabel != null)
                _regionLabel.text = $"Region: {regionId}";
            if (_countryLabel != null)
                _countryLabel.text = $"Country: {countryId}";

            _hydrated = true;
        }

        /// <summary>
        /// When no scene wiring assigns the TMP labels, build a minimal vertical
        /// stack under this transform so HUD / code-only / batch-mode setups still
        /// surface the parent ids. Mirrors the BudgetPanel runtime-root pattern.
        /// </summary>
        private void EnsureRuntimeLabelsIfNeeded()
        {
            if (_regionLabel != null && _countryLabel != null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
                return;

            // Re-parent under a canvas if we are a runtime-only GameObject.
            if (GetComponentInParent<Canvas>() == null)
                transform.SetParent(canvas.transform, false);

            if (GetComponent<RectTransform>() == null)
                gameObject.AddComponent<RectTransform>();

            if (GetComponent<VerticalLayoutGroup>() == null)
            {
                var v = gameObject.AddComponent<VerticalLayoutGroup>();
                v.spacing = 4;
                v.padding = new RectOffset(8, 8, 8, 8);
                v.childForceExpandWidth = true;
                v.childControlWidth = true;
                v.childControlHeight = true;
            }

            if (_regionLabel == null)
                _regionLabel = CreateLabelChild("RegionLabel");
            if (_countryLabel == null)
                _countryLabel = CreateLabelChild("CountryLabel");
        }

        private TMP_Text CreateLabelChild(string childName)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220, 24);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.fontSize = 14;
            label.text = string.Empty;
            return label;
        }
    }
}
