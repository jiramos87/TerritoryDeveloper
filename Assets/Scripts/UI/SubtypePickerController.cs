using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Territory.Economy;
using Territory.Zones;

namespace Territory.UI
{
    /// <summary>Family that drives <see cref="SubtypePickerController"/> row enumeration. R/C/I = density tiers; StateService = catalog rows.</summary>
    public enum ToolFamily
    {
        Residential,
        Commercial,
        Industrial,
        StateService
    }

    /// <summary>
    /// TECH-10500: replaces SubTypePickerModal. Single picker for all four tool families
    /// (R/C/I density tiers + Zone S catalog rows). Code-built UI parented under main Canvas;
    /// UiTheme tokens drive chrome. Selection commits to <see cref="UIManager"/> and closes.
    /// </summary>
    public class SubtypePickerController : MonoBehaviour
    {
        [SerializeField] private ZoneSubTypeRegistry registry;
        [SerializeField] private UiTheme uiTheme;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform rowContainer;

        private UIManager uiManager;
        private ToolFamily currentFamily;
        private bool isVisible;
        private bool uiBuilt;
        private readonly List<GameObject> spawnedRows = new List<GameObject>();

        private void Awake()
        {
            if (registry == null)
                registry = FindObjectOfType<ZoneSubTypeRegistry>();
            EnsureRuntimePanelRootIfNeeded();
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        /// <summary>
        /// Open picker for given family. Vertical list of rows; selection commits via UIManager
        /// and closes. Cancel (ESC) routes through PopupStack → <see cref="Hide"/>(cancelled:true).
        /// </summary>
        public void Show(UIManager caller, ToolFamily family)
        {
            uiManager = caller;
            currentFamily = family;
            EnsureUiBuilt();
            if (panelRoot == null) return;
            ClearRows();
            BuildRows(family);
            panelRoot.SetActive(true);
            isVisible = true;
        }

        /// <summary>Close picker. Cancelled = ESC / outside-click → reset to Grass tool.</summary>
        public void Hide(bool cancelled)
        {
            if (!isVisible) return;
            isVisible = false;
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (cancelled && uiManager != null)
            {
                uiManager.SetCurrentSubTypeId(-1);
                uiManager.OnGrassButtonClicked();
            }
        }

        private void EnsureRuntimePanelRootIfNeeded()
        {
            if (panelRoot != null) return;
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
            GameObject root = new GameObject("SubtypePickerRoot");
            root.transform.SetParent(transform, false);
            root.transform.SetAsLastSibling();
            RectTransform rt = root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(360, 420);
            rt.anchoredPosition = Vector2.zero;
            var img = root.AddComponent<Image>();
            img.color = uiTheme != null ? uiTheme.SurfaceCardHud : new Color(0.08f, 0.08f, 0.1f, 0.96f);
            var v = root.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4;
            v.padding = new RectOffset(10, 10, 10, 10);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true;
            v.childControlHeight = false;
            v.childForceExpandWidth = true;
            panelRoot = root;
        }

        private void EnsureUiBuilt()
        {
            if (uiBuilt) return;
            uiBuilt = true;
            if (rowContainer == null && panelRoot != null)
                rowContainer = panelRoot.transform;
        }

        private void BuildRows(ToolFamily family)
        {
            if (rowContainer == null) return;
            switch (family)
            {
                case ToolFamily.Residential:
                    AddDensityRow("Light Residential", () => uiManager.OnLightResidentialButtonClicked());
                    AddDensityRow("Medium Residential", () => uiManager.OnMediumResidentialButtonClicked());
                    AddDensityRow("Heavy Residential", () => uiManager.OnHeavyResidentialButtonClicked());
                    break;
                case ToolFamily.Commercial:
                    AddDensityRow("Light Commercial", () => uiManager.OnLightCommercialButtonClicked());
                    AddDensityRow("Medium Commercial", () => uiManager.OnMediumCommercialButtonClicked());
                    AddDensityRow("Heavy Commercial", () => uiManager.OnHeavyCommercialButtonClicked());
                    break;
                case ToolFamily.Industrial:
                    AddDensityRow("Light Industrial", () => uiManager.OnLightIndustrialButtonClicked());
                    AddDensityRow("Medium Industrial", () => uiManager.OnMediumIndustrialButtonClicked());
                    AddDensityRow("Heavy Industrial", () => uiManager.OnHeavyIndustrialButtonClicked());
                    break;
                case ToolFamily.StateService:
                    BuildStateServiceRows();
                    break;
            }
        }

        private void BuildStateServiceRows()
        {
            if (registry == null) return;
            var entries = registry.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                int subTypeId = entries[i].id;
                string label;
                if (registry.TryGetPickerLabelForSubType(subTypeId, out string line, out _))
                    label = line;
                else
                    label = entries[i].displayName;
                AddDensityRow(label, () => OnStateServiceRowSelected(subTypeId));
            }
        }

        private void OnStateServiceRowSelected(int subTypeId)
        {
            if (uiManager != null)
                uiManager.SetCurrentSubTypeId(subTypeId);
            Hide(cancelled: false);
        }

        private void AddDensityRow(string label, System.Action onClick)
        {
            GameObject row = new GameObject($"PickerRow_{spawnedRows.Count}", typeof(RectTransform), typeof(Button), typeof(Image));
            row.transform.SetParent(rowContainer, false);
            var img = row.GetComponent<Image>();
            img.color = uiTheme != null ? uiTheme.SurfaceElevated : new Color(0.16f, 0.16f, 0.2f, 1f);
            var le = row.AddComponent<LayoutElement>();
            le.minHeight = 36;
            le.flexibleWidth = 1;

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(row.transform, false);
            var lrt = labelObj.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.sizeDelta = Vector2.zero;
            var t = labelObj.GetComponent<Text>();
            t.text = label;
            t.fontSize = 14;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = uiTheme != null ? uiTheme.TextPrimary : Color.white;
            t.raycastTarget = false;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) t.font = font;

            var btn = row.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                onClick?.Invoke();
                if (currentFamily != ToolFamily.StateService)
                    Hide(cancelled: false);
            });

            spawnedRows.Add(row);
        }

        private void ClearRows()
        {
            for (int i = 0; i < spawnedRows.Count; i++)
            {
                if (spawnedRows[i] != null)
                    Destroy(spawnedRows[i]);
            }
            spawnedRows.Clear();
        }
    }
}
