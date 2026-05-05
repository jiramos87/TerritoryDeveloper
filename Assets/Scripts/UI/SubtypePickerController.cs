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

            // Bottom-strip horizontal picker (TECH-10500 + post-9.1 polish). Anchor bottom-stretch
            // with right inset reserved for vertical toolbar strip on right edge of canvas.
            GameObject root = new GameObject("SubtypePickerRoot");
            root.transform.SetParent(transform, false);
            root.transform.SetAsLastSibling();
            RectTransform rt = root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(-200f, 140f);
            rt.anchoredPosition = new Vector2(-100f, 24f);
            var bg = root.AddComponent<Image>();
            bg.color = uiTheme != null ? uiTheme.SurfaceCardHud : new Color(0.08f, 0.08f, 0.1f, 0.96f);

            // Viewport — masked rect for horizontal scroll.
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(root.transform, false);
            RectTransform vrt = viewport.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = new Vector2(8f, 8f);
            vrt.offsetMax = new Vector2(-8f, -8f);
            var vbg = viewport.GetComponent<Image>();
            vbg.color = new Color(1f, 1f, 1f, 0.04f);
            var mask = viewport.GetComponent<Mask>();
            mask.showMaskGraphic = true;

            // Content — horizontal strip; ContentSizeFitter expands width to row count.
            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(0f, 1f);
            crt.pivot = new Vector2(0f, 0.5f);
            crt.sizeDelta = new Vector2(0f, 0f);
            crt.anchoredPosition = Vector2.zero;
            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(8, 8, 6, 6);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            // ScrollRect drives horizontal scroll.
            var scroll = root.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            scroll.viewport = vrt;
            scroll.content = crt;

            panelRoot = root;
            rowContainer = content.transform;
        }

        private void EnsureUiBuilt()
        {
            if (uiBuilt) return;
            uiBuilt = true;
            // rowContainer wired by EnsureRuntimePanelRootIfNeeded; legacy fallback for serialized panelRoot.
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
            // Tile in horizontal strip — fixed width, theme-tinted, single-line caption.
            GameObject row = new GameObject($"PickerTile_{spawnedRows.Count}", typeof(RectTransform), typeof(Button), typeof(Image));
            row.transform.SetParent(rowContainer, false);
            var img = row.GetComponent<Image>();
            img.color = uiTheme != null ? uiTheme.SurfaceElevated : new Color(0.16f, 0.16f, 0.2f, 1f);
            var le = row.AddComponent<LayoutElement>();
            le.preferredWidth = 140;
            le.minWidth = 120;
            le.flexibleWidth = 0;

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObj.transform.SetParent(row.transform, false);
            var lrt = labelObj.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(6f, 6f);
            lrt.offsetMax = new Vector2(-6f, -6f);
            var t = labelObj.GetComponent<Text>();
            t.text = label;
            t.fontSize = 13;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = uiTheme != null ? uiTheme.TextPrimary : Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
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
