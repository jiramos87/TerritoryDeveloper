using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Territory.Simulation;

namespace Territory.UI.HUD
{
    /// <summary>
    /// FEAT-58 (Stage 9.9) — catalog-driven growth-budget panel.
    /// Reads <c>growth_budget_panel</c> + <c>slider_row_2</c> rows from <see cref="UiAssetCatalog"/>;
    /// self-spawns its <c>panelRoot</c> on first <see cref="Show"/> (mirror Stage 9.7
    /// <c>SubtypePickerController.EnsureRuntimePanelRootIfNeeded</c>). Three slider rows:
    /// TOTAL (growth budget %), ZONING %, ROADS % — auto-redistributes Zoning↔Roads to sum=100.
    /// Energy + Water frozen at 0 in v1. Click-outside dismiss via invisible Image catcher.
    /// </summary>
    public class GrowthBudgetPanelController : MonoBehaviour
    {
        [SerializeField] private GrowthBudgetManager _manager;
        [SerializeField] private UiAssetCatalog _uiAssetCatalog;
        [SerializeField] private UiTheme _uiTheme;
        [SerializeField] private GameObject _panelRoot;

        private bool _isVisible;
        private bool _suppressCallbacks;

        // Sliders + value labels — built once in EnsureRuntimePanelRootIfNeeded.
        private Slider _totalSlider;
        private TextMeshProUGUI _totalValueLabel;
        private Slider _zoningSlider;
        private TextMeshProUGUI _zoningValueLabel;
        private Slider _roadsSlider;
        private TextMeshProUGUI _roadsValueLabel;

        // Click-outside dismiss catcher (full-canvas invisible Image; sibling order = behind panelRoot).
        private GameObject _dismissCatcher;

        /// <summary>True when panel is currently shown.</summary>
        public bool IsVisible => _isVisible;

        private void Awake()
        {
            if (_manager == null) _manager = FindObjectOfType<GrowthBudgetManager>();
            if (_uiAssetCatalog == null) _uiAssetCatalog = FindObjectOfType<UiAssetCatalog>();
            if (_uiAssetCatalog == null)
            {
                // Lazy-spawn host so panel/archetype defaults become available without scene wiring
                // (mirror SubtypePickerController.Awake fallback).
                var go = new GameObject("UiAssetCatalog");
                _uiAssetCatalog = go.AddComponent<UiAssetCatalog>();
            }
        }

        /// <summary>Open the panel. Idempotent — second call is a no-op.</summary>
        public void Show()
        {
            if (_isVisible) return;
            EnsureRuntimePanelRootIfNeeded();
            if (_panelRoot == null) return;
            BindSlidersFromManager();
            if (_dismissCatcher != null) _dismissCatcher.SetActive(true);
            _panelRoot.SetActive(true);
            _isVisible = true;
        }

        /// <summary>Close the panel. Idempotent.</summary>
        public void Hide()
        {
            if (!_isVisible) return;
            _isVisible = false;
            if (_dismissCatcher != null) _dismissCatcher.SetActive(false);
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        /// <summary>Toggle visibility. Adapter Update tick reads <see cref="IsVisible"/> for illumination.</summary>
        public void Toggle()
        {
            if (_isVisible) Hide(); else Show();
        }

        // ── Build path ──────────────────────────────────────────────────────────

        private void EnsureRuntimePanelRootIfNeeded()
        {
            if (_panelRoot != null) return;
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            UiPanelDef panelDef = null;
            bool hasPanel = _uiAssetCatalog != null && _uiAssetCatalog.TryGetPanel("growth_budget_panel", out panelDef);
            if (!hasPanel || panelDef == null)
            {
                Debug.LogError("[GrowthBudgetPanelController] growth_budget_panel row not found in UiAssetCatalog. Build aborted.");
                return;
            }

            UiArchetypeDef rowArch = null;
            _uiAssetCatalog.TryGetArchetype("slider_row_2", out rowArch);
            float rowHeight  = rowArch != null ? rowArch.rowHeight  : 40f;
            float labelWidth = rowArch != null ? rowArch.labelWidth : 110f;
            float valueWidth = rowArch != null ? rowArch.valueWidth : 48f;

            // Click-outside dismiss catcher — invisible full-canvas Image behind panelRoot.
            // Mirror MoneyReadoutBudgetToggle pattern: sibling under same canvas, raycast-only.
            _dismissCatcher = new GameObject("GrowthBudgetDismissCatcher", typeof(RectTransform), typeof(Image));
            _dismissCatcher.transform.SetParent(canvas.transform, false);
            _dismissCatcher.transform.SetAsLastSibling();
            var dcrt = _dismissCatcher.GetComponent<RectTransform>();
            dcrt.anchorMin = Vector2.zero;
            dcrt.anchorMax = Vector2.one;
            dcrt.offsetMin = Vector2.zero;
            dcrt.offsetMax = Vector2.zero;
            var dcImg = _dismissCatcher.GetComponent<Image>();
            dcImg.color = new Color(0f, 0f, 0f, 0f); // invisible — raycast only
            dcImg.raycastTarget = true;
            var dcBtn = _dismissCatcher.AddComponent<Button>();
            dcBtn.transition = Selectable.Transition.None;
            dcBtn.onClick.AddListener(Hide);
            _dismissCatcher.SetActive(false);

            GameObject root = new GameObject("GrowthBudgetPanelRoot");
            root.transform.SetParent(canvas.transform, false);
            root.transform.SetAsLastSibling();
            RectTransform rt = root.AddComponent<RectTransform>();
            rt.anchorMin = panelDef.anchorMin;
            rt.anchorMax = panelDef.anchorMax;
            rt.pivot = panelDef.pivot;
            rt.sizeDelta = panelDef.sizeDelta;
            rt.anchoredPosition = new Vector2(-12f, -76f); // tucked below hud-bar in top-right.
            var bg = root.AddComponent<Image>();
            bg.color = _uiTheme != null ? _uiTheme.SurfaceCardHud : new Color(0.08f, 0.08f, 0.1f, 0.96f);
            bg.raycastTarget = true; // swallow clicks so dismiss catcher doesn't fire on panel content.

            var vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = panelDef.spacing;
            vlg.padding = new RectOffset(
                Mathf.RoundToInt(panelDef.padding.x),
                Mathf.RoundToInt(panelDef.padding.y),
                Mathf.RoundToInt(panelDef.padding.z),
                Mathf.RoundToInt(panelDef.padding.w));
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Title row.
            BuildLabelRow(root.transform, "GROWTH BUDGET", rowHeight * 0.7f, true);

            // Three slider rows.
            BuildSliderRow(root.transform, "Total %", rowHeight, labelWidth, valueWidth, 0, 100,
                onChanged: OnTotalChanged, sliderOut: out _totalSlider, valueLabelOut: out _totalValueLabel);
            BuildSliderRow(root.transform, "Zoning %", rowHeight, labelWidth, valueWidth, 0, 100,
                onChanged: OnZoningChanged, sliderOut: out _zoningSlider, valueLabelOut: out _zoningValueLabel);
            BuildSliderRow(root.transform, "Roads %", rowHeight, labelWidth, valueWidth, 0, 100,
                onChanged: OnRoadsChanged, sliderOut: out _roadsSlider, valueLabelOut: out _roadsValueLabel);

            _panelRoot = root;
            _panelRoot.SetActive(false);
        }

        private void BuildLabelRow(Transform parent, string text, float height, bool emphasis)
        {
            GameObject row = new GameObject("TitleRow", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleHeight = 0;

            GameObject lbl = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lbl.transform.SetParent(row.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var t = lbl.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = emphasis ? 14 : 12;
            t.fontStyle = emphasis ? FontStyles.Bold : FontStyles.Normal;
            t.alignment = TextAlignmentOptions.Left;
            t.color = _uiTheme != null ? _uiTheme.TextPrimary : Color.white;
            t.raycastTarget = false;
        }

        private void BuildSliderRow(
            Transform parent,
            string labelText,
            float height,
            float labelWidth,
            float valueWidth,
            int min,
            int max,
            Action<int> onChanged,
            out Slider sliderOut,
            out TextMeshProUGUI valueLabelOut)
        {
            GameObject row = new GameObject($"Row_{labelText}", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleHeight = 0;

            // Left label.
            GameObject lbl = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lbl.transform.SetParent(row.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(0f, 1f);
            lrt.pivot = new Vector2(0f, 0.5f);
            lrt.sizeDelta = new Vector2(labelWidth, 0f);
            lrt.anchoredPosition = Vector2.zero;
            var lt = lbl.GetComponent<TextMeshProUGUI>();
            lt.text = labelText;
            lt.fontSize = 12;
            lt.alignment = TextAlignmentOptions.MidlineLeft;
            lt.color = _uiTheme != null ? _uiTheme.TextPrimary : Color.white;
            lt.raycastTarget = false;

            // Right value label.
            GameObject vl = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            vl.transform.SetParent(row.transform, false);
            var vrt = vl.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(1f, 0f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.pivot = new Vector2(1f, 0.5f);
            vrt.sizeDelta = new Vector2(valueWidth, 0f);
            vrt.anchoredPosition = Vector2.zero;
            var vt = vl.GetComponent<TextMeshProUGUI>();
            vt.fontSize = 12;
            vt.alignment = TextAlignmentOptions.MidlineRight;
            vt.color = _uiTheme != null ? _uiTheme.TextPrimary : Color.white;
            vt.raycastTarget = false;
            valueLabelOut = vt;

            // Slider in the middle gap.
            GameObject sliderObj = new GameObject("Slider",
                typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(row.transform, false);
            var srt = sliderObj.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(labelWidth + 8f, 8f);
            srt.offsetMax = new Vector2(-(valueWidth + 8f), -8f);

            // Slider chrome — Background + Fill Area + Fill + Handle Slide Area + Handle.
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderObj.transform, false);
            var bgrt = bg.GetComponent<RectTransform>();
            bgrt.anchorMin = new Vector2(0f, 0.4f);
            bgrt.anchorMax = new Vector2(1f, 0.6f);
            bgrt.offsetMin = Vector2.zero;
            bgrt.offsetMax = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = _uiTheme != null ? _uiTheme.SurfaceElevated : new Color(0.16f, 0.16f, 0.2f, 1f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fart = fillArea.GetComponent<RectTransform>();
            fart.anchorMin = new Vector2(0f, 0.4f);
            fart.anchorMax = new Vector2(1f, 0.6f);
            fart.offsetMin = new Vector2(0f, 0f);
            fart.offsetMax = new Vector2(-10f, 0f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = _uiTheme != null ? _uiTheme.AccentPrimary : new Color(0.29f, 0.62f, 1f, 1f);

            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObj.transform, false);
            var hart = handleArea.GetComponent<RectTransform>();
            hart.anchorMin = Vector2.zero;
            hart.anchorMax = Vector2.one;
            hart.offsetMin = new Vector2(10f, 0f);
            hart.offsetMax = new Vector2(-10f, 0f);

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var hrt = handle.GetComponent<RectTransform>();
            hrt.sizeDelta = new Vector2(16f, height - 12f);
            var hImg = handle.GetComponent<Image>();
            hImg.color = _uiTheme != null ? _uiTheme.AccentPrimary : new Color(0.29f, 0.62f, 1f, 1f);

            var slider = sliderObj.GetComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = hrt;
            slider.targetGraphic = hImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = true;
            slider.value = min;
            slider.onValueChanged.AddListener(v =>
            {
                if (_suppressCallbacks) return;
                onChanged?.Invoke(Mathf.RoundToInt(v));
            });
            sliderOut = slider;
        }

        // ── Manager binding ─────────────────────────────────────────────────────

        private void BindSlidersFromManager()
        {
            if (_manager == null) return;
            _suppressCallbacks = true;
            try
            {
                int total   = _manager.GetGrowthBudgetPercent();
                int zoning  = _manager.GetCategoryPercent(GrowthCategory.Zoning);
                int roads   = _manager.GetCategoryPercent(GrowthCategory.Roads);
                if (zoning + roads == 0)
                {
                    // First-open default split — split evenly.
                    zoning = 50;
                    roads = 50;
                    _manager.SetCategoryPercent(GrowthCategory.Zoning, zoning);
                    _manager.SetCategoryPercent(GrowthCategory.Roads, roads);
                }
                // V1 — Energy + Water frozen at 0.
                _manager.SetCategoryPercent(GrowthCategory.Energy, 0);
                _manager.SetCategoryPercent(GrowthCategory.Water, 0);

                if (_totalSlider != null)
                {
                    _totalSlider.value = total;
                    if (_totalValueLabel != null) _totalValueLabel.text = $"{total}%";
                }
                if (_zoningSlider != null)
                {
                    _zoningSlider.value = zoning;
                    if (_zoningValueLabel != null) _zoningValueLabel.text = $"{zoning}%";
                }
                if (_roadsSlider != null)
                {
                    _roadsSlider.value = roads;
                    if (_roadsValueLabel != null) _roadsValueLabel.text = $"{roads}%";
                }
            }
            finally
            {
                _suppressCallbacks = false;
            }
        }

        private void OnTotalChanged(int pct)
        {
            if (_manager == null) return;
            _manager.SetGrowthBudgetPercent(pct);
            if (_totalValueLabel != null) _totalValueLabel.text = $"{pct}%";
        }

        private void OnZoningChanged(int pct)
        {
            if (_manager == null) return;
            int complement = Mathf.Clamp(100 - pct, 0, 100);
            _manager.SetCategoryPercent(GrowthCategory.Zoning, pct);
            _manager.SetCategoryPercent(GrowthCategory.Roads, complement);
            if (_zoningValueLabel != null) _zoningValueLabel.text = $"{pct}%";
            _suppressCallbacks = true;
            try
            {
                if (_roadsSlider != null) _roadsSlider.value = complement;
            }
            finally
            {
                _suppressCallbacks = false;
            }
            if (_roadsValueLabel != null) _roadsValueLabel.text = $"{complement}%";
        }

        private void OnRoadsChanged(int pct)
        {
            if (_manager == null) return;
            int complement = Mathf.Clamp(100 - pct, 0, 100);
            _manager.SetCategoryPercent(GrowthCategory.Roads, pct);
            _manager.SetCategoryPercent(GrowthCategory.Zoning, complement);
            if (_roadsValueLabel != null) _roadsValueLabel.text = $"{pct}%";
            _suppressCallbacks = true;
            try
            {
                if (_zoningSlider != null) _zoningSlider.value = complement;
            }
            finally
            {
                _suppressCallbacks = false;
            }
            if (_zoningValueLabel != null) _zoningValueLabel.text = $"{complement}%";
        }
    }
}
