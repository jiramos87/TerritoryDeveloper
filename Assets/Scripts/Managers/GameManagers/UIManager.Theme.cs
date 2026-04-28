using System;
using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI
{
    /// <summary>
    /// Apply <see cref="UiTheme"/> tokens to city HUD texts + common panel chrome (partial of <see cref="UIManager"/>).
    /// </summary>
    public partial class UIManager
    {
        [Header("Theme (optional)")]
        [Tooltip("Assign DefaultUiTheme or a variant; when null, HUD keeps scene-authored colors.")]
        [SerializeField] private UiTheme hudUiTheme;

        [Header("Toolbar / theme wiring (optional)")]
        [Tooltip("Background Image on the ControlPanel strip. When null, resolved once from GameObject name ControlPanel in Awake.")]
        [SerializeField] private Image controlPanelBackgroundImage;

        private void Awake()
        {
            if (controlPanelBackgroundImage == null)
            {
                GameObject go = GameObject.Find("ControlPanel");
                if (go != null)
                    controlPanelBackgroundImage = go.GetComponent<Image>();
            }
        }

        /// <summary>
        /// Apply typography + surface colors once at startup when <see cref="hudUiTheme"/> assigned.
        /// </summary>
        private void ApplyHudUiThemeIfConfigured()
        {
            EnsureGridCoordinatesDebugChrome();
            EnsureDemandGaugeBars();

            if (hudUiTheme == null)
                return;

            // Stage 6 (game-ui-design-system): hero/toolbar money rows removed — population /
            // money / happiness / cityName / buttonMoney now baked StudioControl variants on
            // hud-bar prefab; theme is applied by StudioControlBase.ApplyTheme via UiBakeHandler.

            ApplyBodyStatRow(dateText);
            ApplyBodyStatRow(cityPowerOutputText);
            ApplyBodyStatRow(cityPowerConsumptionText);
            ApplyBodyStatRow(cityWaterOutputText);
            ApplyBodyStatRow(cityWaterConsumptionText);
            ApplyBodyStatRow(residentialTaxText, styleSiblingLabels: false);
            ApplyBodyStatRow(commercialTaxText, styleSiblingLabels: false);
            ApplyBodyStatRow(industrialTaxText, styleSiblingLabels: false);
            ApplyBodyStatRow(unemploymentRateText);
            ApplyBodyStatRow(totalJobsText);
            ApplyBodyStatRow(totalJobsCreatedText);
            ApplyBodyStatRow(availableJobsText);
            ApplyBodyStatRow(jobsTakenText);
            ApplyBodyStatRow(demandResidentialText);
            ApplyBodyStatRow(demandCommercialText);
            ApplyBodyStatRow(demandIndustrialText);
            ApplyBodyStatRow(demandFeedbackText);

            ApplyCaptionText(detailsDebugText);

            ApplyBodyStatRow(detailsNameText);
            ApplyBodyStatRow(detailsOccupancyText);
            ApplyBodyStatRow(detailsHappinessText);
            ApplyBodyStatRow(detailsPowerOutputText);
            ApplyBodyStatRow(detailsPowerConsumptionText);
            ApplyBodyStatRow(detailsDateBuiltText);
            ApplyBodyStatRow(detailsBuildingTypeText);
            ApplyBodyStatRow(detailsSortingOrderText);
            ApplyBodyStatRow(detailsDesirabilityText);
            ApplyBodyStatRow(insufficientFundsText);
            ApplyBodyStatRow(GameSavedText);

            if (constructionCostText != null)
            {
                constructionCostText.fontSize = hudUiTheme.FontSizeBody;
                constructionCostText.color = hudUiTheme.TextPrimary;
            }

            // Stage 6: StatsPanel tint removed — legacy panel surface decommissioned with
            // hero stats relocation to baked hud-bar prefab.
            TintPanelRootBehindReference("DatePanel", dateText, hudUiTheme.SurfaceCardHud);
            TintPanelRootBehindReference("TaxPanel", residentialTaxText, hudUiTheme.SurfaceCardHud);
            ApplyTaxPanelBudgetRowTexts();
            EnsureTaxPanelDividerStripes();
            TintPanelRootBehindReference("DetailsPanel", detailsNameText, hudUiTheme.SurfaceCardHud);

            if (demandWarningPanel != null)
            {
                var dw = demandWarningPanel.GetComponent<Image>();
                if (dw != null)
                    dw.color = hudUiTheme.SurfaceCardHud;
            }

            if (controlPanelBackgroundImage != null)
                controlPanelBackgroundImage.color = hudUiTheme.SurfaceToolbar;

            ApplyLoadGameAndFundsPanels();
        }

        private const string GridCoordinatesChromeName = "Fe50GridCoordinatesChrome";
        private const string GridCoordinatesChromeNameAlt = "Fe50GridCoordinatesPanel";
        private const string GridCoordinatesTextInsetName = "Fe50GridCoordinatesTextInset";
        private const string GridCoordinatesTextHolderAlt = "Fe50GridCoordinatesText";

        private const string GridCoordinatesScrollRootName = "Fe50GridCoordinatesScrollRoot";
        private const string GridCoordinatesViewportName = "Fe50GridCoordinatesViewport";
        private const string GridCoordinatesContentName = "Fe50GridCoordinatesContent";

        private const string ControlPanelObjectName = "ControlPanel";

        /// <summary>Top HUD strip; must stay unobstructed above grid debug chrome (sibling under same HUD root as <see cref="ControlPanelObjectName"/>).</summary>
        private const string DataPanelButtonsObjectName = "DataPanelButtons";

        /// <summary>Gap: grid debug chrome bottom → top of <see cref="ControlPanelObjectName"/>.</summary>
        private const float GridCoordinatesChromeGapAboveControlPanel = 10f;

        /// <summary>Gap: bottom of <see cref="DataPanelButtonsObjectName"/> → top of grid debug chrome.</summary>
        private const float GridCoordinatesGapBelowDataPanelButtons = 8f;

        /// <summary>Gap: grid debug chrome bottom → top of <c>MiniMapPanel</c> (fallback layout).</summary>
        private const float GridCoordinatesChromeGapAboveMinimap = 30f;

        /// <summary>Max outer size (w=h) of square grid debug chrome.</summary>
        private const float GridCoordinatesChromeMaxSquareSide = 220f;

        private static bool IsGridCoordinatesChromeRootName(string n)
        {
            return n == GridCoordinatesChromeName || n == GridCoordinatesChromeNameAlt;
        }

        private static bool IsGridCoordinatesTextHolderName(string n)
        {
            return n == GridCoordinatesTextInsetName || n == GridCoordinatesTextHolderAlt;
        }

        /// <summary>
        /// Walk parents → find grid debug chrome root (inset or scroll lives under it).
        /// </summary>
        private static Transform FindGridCoordinatesChromeRoot(Transform from)
        {
            for (Transform p = from; p != null; p = p.parent)
            {
                if (IsGridCoordinatesChromeRootName(p.name))
                    return p;
            }

            return null;
        }

        /// <summary>
        /// Return text inset <see cref="RectTransform"/> under chrome if present.
        /// </summary>
        private static RectTransform FindGridCoordinatesInset(RectTransform chromeRt)
        {
            if (chromeRt == null)
                return null;
            Transform t = chromeRt.Find(GridCoordinatesTextInsetName);
            if (t == null)
                t = chromeRt.Find(GridCoordinatesTextHolderAlt);
            return t != null ? t.GetComponent<RectTransform>() : null;
        }

        /// <summary>
        /// HUD layout root parenting <c>ControlPanel</c> and/or <c>MiniMapPanel</c> (same <see cref="Transform"/> in MainScene).
        /// </summary>
        private static Transform FindHudLayoutRoot(Transform from)
        {
            for (Transform p = from; p != null; p = p.parent)
            {
                if (p.Find(ControlPanelObjectName) != null || p.Find("MiniMapPanel") != null)
                    return p;
            }
            return null;
        }

        /// <summary>
        /// Parent grid debug chrome under HUD layout root; draw just after <c>ControlPanel</c> if present, else after <c>MiniMapPanel</c>.
        /// </summary>
        private static void EnsureGridCoordinatesChromeHudMount(RectTransform chromeRt)
        {
            if (chromeRt == null)
                return;
            Transform mount = FindHudLayoutRoot(chromeRt);
            if (mount == null)
                return;
            chromeRt.SetParent(mount, false);
            Transform cp = mount.Find(ControlPanelObjectName);
            if (cp != null)
                chromeRt.SetSiblingIndex(cp.GetSiblingIndex() + 1);
            else
            {
                Transform mm = mount.Find("MiniMapPanel");
                if (mm != null)
                    chromeRt.SetSiblingIndex(mm.GetSiblingIndex() + 1);
            }
        }

        /// <summary>
        /// Wrap <see cref="gridCoordinatesText"/> in semi-transparent HUD panel, white copy → contrast over map.
        /// </summary>
        private void EnsureGridCoordinatesDebugChrome()
        {
            if (gridCoordinatesText == null)
                return;

            Transform t = gridCoordinatesText.transform;
            RectTransform textRt = gridCoordinatesText.GetComponent<RectTransform>();

            Transform chromeRoot = FindGridCoordinatesChromeRoot(t);
            if (chromeRoot != null)
            {
                RectTransform chromeRt = chromeRoot.GetComponent<RectTransform>();
                EnsureGridCoordinatesTextUnderInset(chromeRt.transform, textRt);
                ApplyGridCoordinatesChromeTextStyle();
                EnsureGridCoordinatesChromeHudMount(chromeRt);
                AlignGridCoordinatesChrome(chromeRt);
                UpdateGridCoordinatesScrollLayout(chromeRt, gridCoordinatesText);
                return;
            }

            Transform originalParent = t.parent;
            int siblingIndex = t.GetSiblingIndex();
            Transform chromeMount = FindHudLayoutRoot(t) ?? originalParent;

            GameObject chrome = new GameObject(GridCoordinatesChromeName, typeof(RectTransform));
            chrome.transform.SetParent(chromeMount, false);
            Transform cpForOrder = chromeMount.Find(ControlPanelObjectName);
            Transform mmForOrder = chromeMount.Find("MiniMapPanel");
            if (cpForOrder != null)
                chrome.transform.SetSiblingIndex(cpForOrder.GetSiblingIndex() + 1);
            else if (mmForOrder != null)
                chrome.transform.SetSiblingIndex(mmForOrder.GetSiblingIndex() + 1);
            else
                chrome.transform.SetSiblingIndex(siblingIndex);
            RectTransform chromeRtNew = chrome.GetComponent<RectTransform>();
            chromeRtNew.anchorMin = textRt.anchorMin;
            chromeRtNew.anchorMax = textRt.anchorMax;
            chromeRtNew.pivot = textRt.pivot;
            chromeRtNew.anchoredPosition = textRt.anchoredPosition;
            chromeRtNew.sizeDelta = textRt.sizeDelta + new Vector2(36f, 18f);
            chromeRtNew.localScale = textRt.localScale;

            GameObject bgGo = new GameObject("Fe50GridDebugBg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(chrome.transform, false);
            bgGo.transform.SetAsFirstSibling();
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bgGo.GetComponent<Image>();
            bgImg.raycastTarget = false;
            Color panel = hudUiTheme != null ? hudUiTheme.SurfaceToolbar : new Color(0.07f, 0.08f, 0.11f, 1f);
            panel.a = hudUiTheme != null ? Mathf.Clamp(hudUiTheme.SurfaceToolbar.a * 0.92f, 0.78f, 0.9f) : 0.86f;
            bgImg.color = panel;

            EnsureGridCoordinatesTextUnderInset(chrome.transform, textRt);

            ApplyGridCoordinatesChromeTextStyle();
            EnsureGridCoordinatesChromeHudMount(chromeRtNew);
            AlignGridCoordinatesChrome(chromeRtNew);
            UpdateGridCoordinatesScrollLayout(chromeRtNew, gridCoordinatesText);
        }

        /// <summary>
        /// Inset debug copy from chrome edges (plain <see cref="RectTransform"/>, no layout groups).
        /// </summary>
        private static void ApplyGridCoordinatesChromeTextInset(RectTransform insetRt)
        {
            if (insetRt == null)
                return;
            VerticalLayoutGroup vlg = insetRt.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
                UnityEngine.Object.Destroy(vlg);

            insetRt.anchorMin = Vector2.zero;
            insetRt.anchorMax = Vector2.one;
            insetRt.anchoredPosition = Vector2.zero;
            insetRt.sizeDelta = Vector2.zero;
            insetRt.offsetMin = new Vector2(18f, 10f);
            insetRt.offsetMax = new Vector2(-18f, -10f);
        }

        /// <summary>
        /// Strip nested canvas/scaler + layout drivers → legacy <see cref="Text"/> draws on root HUD canvas.
        /// </summary>
        private static void EnsureGridCoordinatesTextLayoutDriver(RectTransform textRt)
        {
            if (textRt == null)
                return;
            ContentSizeFitter fitter = textRt.GetComponent<ContentSizeFitter>();
            if (fitter != null)
                UnityEngine.Object.Destroy(fitter);
            LayoutElement le = textRt.GetComponent<LayoutElement>();
            if (le != null)
                UnityEngine.Object.Destroy(le);

            // CanvasScaler requires Canvas — remove scaler first. Disable before Destroy so a same-frame second pass cannot trip the dependency.
            CanvasScaler scaler = textRt.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.enabled = false;
                UnityEngine.Object.Destroy(scaler);
            }
            Canvas nested = textRt.GetComponent<Canvas>();
            if (nested != null)
            {
                nested.enabled = false;
                UnityEngine.Object.Destroy(nested);
            }

            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = Vector2.zero;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Ensure inset holder exists under chrome; <see cref="gridCoordinatesText"/> is layout-driven child with real padding.
        /// </summary>
        private static void EnsureGridCoordinatesTextUnderInset(Transform chromeTransform, RectTransform textRt)
        {
            if (chromeTransform == null || textRt == null)
                return;

            Transform insetTransform = chromeTransform.Find(GridCoordinatesTextInsetName);
            if (insetTransform == null)
                insetTransform = chromeTransform.Find(GridCoordinatesTextHolderAlt);
            if (insetTransform == null)
            {
                GameObject insetGo = new GameObject(GridCoordinatesTextInsetName, typeof(RectTransform));
                insetTransform = insetGo.transform;
                insetTransform.SetParent(chromeTransform, false);
                Transform bg = chromeTransform.Find("Fe50GridDebugBg");
                if (bg != null)
                    insetTransform.SetSiblingIndex(bg.GetSiblingIndex() + 1);
            }

            RectTransform insetRt = insetTransform.GetComponent<RectTransform>();
            ApplyGridCoordinatesChromeTextInset(insetRt);

            EnsureGridCoordinatesScrollUnderInset(insetRt, textRt);
            EnsureGridCoordinatesTextLayoutDriver(textRt);
        }

        /// <summary>
        /// Add <see cref="ScrollRect"/> under inset → long grid debug copy scrolls inside panel vs overflowing.
        /// </summary>
        private static void EnsureGridCoordinatesScrollUnderInset(RectTransform insetRt, RectTransform textRt)
        {
            if (insetRt == null || textRt == null)
                return;

            Transform scrollRootT = insetRt.Find(GridCoordinatesScrollRootName);
            if (scrollRootT == null)
            {
                GameObject scrollGo = new GameObject(GridCoordinatesScrollRootName, typeof(RectTransform), typeof(ScrollRect));
                scrollRootT = scrollGo.transform;
                scrollRootT.SetParent(insetRt, false);
                RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
                scrollRt.anchorMin = Vector2.zero;
                scrollRt.anchorMax = Vector2.one;
                scrollRt.offsetMin = Vector2.zero;
                scrollRt.offsetMax = Vector2.zero;

                GameObject vpGo = new GameObject(GridCoordinatesViewportName, typeof(RectTransform), typeof(RectMask2D), typeof(Image));
                vpGo.transform.SetParent(scrollRootT, false);
                RectTransform vpRt = vpGo.GetComponent<RectTransform>();
                vpRt.anchorMin = Vector2.zero;
                vpRt.anchorMax = Vector2.one;
                vpRt.offsetMin = Vector2.zero;
                vpRt.offsetMax = Vector2.zero;
                Image vpImg = vpGo.GetComponent<Image>();
                vpImg.color = new Color(0f, 0f, 0f, 0.01f);
                vpImg.raycastTarget = true;

                GameObject contentGo = new GameObject(GridCoordinatesContentName, typeof(RectTransform));
                contentGo.transform.SetParent(vpGo.transform, false);
                RectTransform contentRt = contentGo.GetComponent<RectTransform>();
                contentRt.anchorMin = new Vector2(0f, 1f);
                contentRt.anchorMax = new Vector2(1f, 1f);
                contentRt.pivot = new Vector2(0.5f, 1f);
                contentRt.anchoredPosition = Vector2.zero;
                contentRt.sizeDelta = Vector2.zero;

                ScrollRect sr = scrollGo.GetComponent<ScrollRect>();
                sr.viewport = vpRt;
                sr.content = contentRt;
                sr.horizontal = false;
                sr.vertical = true;
                sr.movementType = ScrollRect.MovementType.Clamped;
                sr.scrollSensitivity = 24f;
                sr.inertia = true;
                sr.decelerationRate = 0.135f;
            }

            Transform viewportT = scrollRootT.Find(GridCoordinatesViewportName);
            Transform contentTransform = viewportT != null ? viewportT.Find(GridCoordinatesContentName) : null;
            RectTransform contentParent = contentTransform != null ? contentTransform.GetComponent<RectTransform>() : null;
            if (contentParent == null)
                return;

            textRt.SetParent(contentParent, false);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.anchoredPosition = Vector2.zero;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            textRt.localScale = Vector3.one;
        }

        /// <summary>
        /// Size scroll content from text preferred height; enable vertical scroll only when exceeds viewport.
        /// </summary>
        private static void UpdateGridCoordinatesScrollLayout(RectTransform chromeRt, Text dbgText)
        {
            if (chromeRt == null)
                return;

            RectTransform insetRt = FindGridCoordinatesInset(chromeRt);
            if (insetRt == null)
                return;

            Transform scrollRootT = insetRt.Find(GridCoordinatesScrollRootName);
            if (scrollRootT == null)
                return;

            ScrollRect sr = scrollRootT.GetComponent<ScrollRect>();
            if (sr == null)
                return;

            RectTransform viewport = sr.viewport;
            RectTransform content = sr.content as RectTransform;
            if (viewport == null || content == null)
                return;

            Canvas.ForceUpdateCanvases();

            float viewW = Mathf.Max(viewport.rect.width, 40f);
            float viewH = Mathf.Max(viewport.rect.height, 1f);

            float prefH = EstimateGridCoordinatesTextPreferredHeight(dbgText, viewW);
            float contentH = Mathf.Max(prefH, viewH);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentH);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            bool needScroll = prefH > viewH + 0.5f;
            sr.vertical = needScroll;
            sr.enabled = true;
            if (!needScroll)
                sr.verticalNormalizedPosition = 1f;

            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// Position grid debug chrome above <c>ControlPanel</c> if available, else above minimap.
        /// </summary>
        private static void AlignGridCoordinatesChrome(RectTransform chromeRt)
        {
            if (chromeRt == null)
                return;
            Canvas.ForceUpdateCanvases();
            EnsureGridCoordinatesChromeHudMount(chromeRt);
            if (TryAlignGridCoordinatesChromeToControlPanel(chromeRt))
                return;
            AlignGridCoordinatesChromeToMiniMap(chromeRt);
        }

        /// <summary>
        /// Square grid debug chrome above reference panel top; cap top below <see cref="DataPanelButtonsObjectName"/> when present.
        /// </summary>
        private static void GridCoordinatesApplySquareChromeLayout(
            RectTransform chromeRt,
            RectTransform parentRt,
            float minX,
            float referenceTopY,
            float gapAboveReference,
            float widthBand)
        {
            float minChromeBottom = referenceTopY + gapAboveReference;

            float limitTop = float.PositiveInfinity;
            Transform dpTransform = parentRt.Find(DataPanelButtonsObjectName);
            RectTransform dpRt = dpTransform != null ? dpTransform.GetComponent<RectTransform>() : null;
            if (dpRt != null && GridCoordinatesTryGetRectBoundsInParent(parentRt, dpRt, out _, out _, out float dpMinY, out _))
                limitTop = dpMinY - GridCoordinatesGapBelowDataPanelButtons;

            float verticalRoom = limitTop - minChromeBottom;
            if (!float.IsFinite(verticalRoom) || verticalRoom > 5000f)
                verticalRoom = GridCoordinatesChromeMaxSquareSide;
            verticalRoom = Mathf.Max(verticalRoom, 1f);

            float panelW = Mathf.Max(widthBand, 44f);
            float side = Mathf.Min(Mathf.Min(panelW, GridCoordinatesChromeMaxSquareSide), verticalRoom);
            side = Mathf.Max(side, 1f);

            float chromeBottomY = minChromeBottom;
            float chromeTopY = chromeBottomY + side;
            if (float.IsFinite(limitTop) && chromeTopY > limitTop)
            {
                chromeBottomY = limitTop - side;
                chromeBottomY = Mathf.Max(chromeBottomY, minChromeBottom);
            }

            Rect parentRect = parentRt.rect;
            Vector2 anchorRefBottomLeft = new Vector2(
                Mathf.Lerp(parentRect.xMin, parentRect.xMax, 0f),
                Mathf.Lerp(parentRect.yMin, parentRect.yMax, 0f));
            chromeRt.anchorMin = Vector2.zero;
            chromeRt.anchorMax = Vector2.zero;
            chromeRt.pivot = Vector2.zero;
            chromeRt.anchoredPosition = new Vector2(minX, chromeBottomY) - anchorRefBottomLeft;
            chromeRt.sizeDelta = new Vector2(side, side);
        }

        /// <summary>
        /// Place grid debug chrome just above left <c>ControlPanel</c> (square, same width band as toolbar).
        /// </summary>
        private static bool TryAlignGridCoordinatesChromeToControlPanel(RectTransform chromeRt)
        {
            RectTransform parentRt = chromeRt.parent as RectTransform;
            if (parentRt == null)
                return false;
            Transform cpTransform = parentRt.Find(ControlPanelObjectName);
            RectTransform cpRt = cpTransform != null ? cpTransform.GetComponent<RectTransform>() : null;
            if (cpRt == null)
                return false;
            if (!GridCoordinatesTryGetRectBoundsInParent(parentRt, cpRt, out float minX, out float maxX, out _, out float cpMaxY))
                return false;
            float panelW = maxX - minX;
            if (panelW < 32f)
                panelW = Mathf.Max(cpRt.rect.width, 80f);

            GridCoordinatesApplySquareChromeLayout(
                chromeRt,
                parentRt,
                minX,
                cpMaxY,
                GridCoordinatesChromeGapAboveControlPanel,
                panelW);
            return true;
        }

        /// <summary>
        /// Place grid debug chrome above minimap, same width band, square (capped below <see cref="DataPanelButtonsObjectName"/> when present).
        /// Do NOT copy <c>MiniMapPanel</c> stretch anchors: anchors (0,0)-(1,1) → height = parent.height + sizeDelta.y, so small positive sizeDelta.y fills almost entire screen.
        /// </summary>
        private static void AlignGridCoordinatesChromeToMiniMap(RectTransform chromeRt)
        {
            if (chromeRt == null)
                return;
            Canvas.ForceUpdateCanvases();
            EnsureGridCoordinatesChromeHudMount(chromeRt);
            RectTransform parentRt = chromeRt.parent as RectTransform;
            if (parentRt == null)
                return;
            Transform mmTransform = parentRt.Find("MiniMapPanel");
            RectTransform mmRt = mmTransform != null ? mmTransform.GetComponent<RectTransform>() : null;
            if (mmRt == null)
                return;

            if (!GridCoordinatesTryGetRectBoundsInParent(parentRt, mmRt, out float minX, out float maxX, out _, out float mmMaxY))
                return;
            float mmWidth = maxX - minX;
            if (mmWidth < 32f)
                mmWidth = Mathf.Max(mmRt.rect.width, 80f);

            GridCoordinatesApplySquareChromeLayout(
                chromeRt,
                parentRt,
                minX,
                mmMaxY,
                GridCoordinatesChromeGapAboveMinimap,
                mmWidth);
        }

        /// <summary>
        /// Axis-aligned bounds of <paramref name="childRt"/> in <paramref name="parentRt"/> local space.
        /// </summary>
        private static bool GridCoordinatesTryGetRectBoundsInParent(RectTransform parentRt, RectTransform childRt, out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = maxX = minY = maxY = 0f;
            if (parentRt == null || childRt == null)
                return false;
            Rect r = childRt.rect;
            Vector3[] corners =
            {
                new Vector3(r.xMin, r.yMin, 0f),
                new Vector3(r.xMin, r.yMax, 0f),
                new Vector3(r.xMax, r.yMax, 0f),
                new Vector3(r.xMax, r.yMin, 0f),
            };
            minX = minY = float.PositiveInfinity;
            maxX = maxY = float.NegativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                Vector3 pl = parentRt.InverseTransformPoint(childRt.TransformPoint(corners[i]));
                minX = Mathf.Min(minX, pl.x);
                maxX = Mathf.Max(maxX, pl.x);
                minY = Mathf.Min(minY, pl.y);
                maxY = Mathf.Max(maxY, pl.y);
            }
            return maxX > minX && maxY > minY;
        }

        /// <summary>
        /// Re-size/place chrome after <see cref="gridCoordinatesText"/> changes (Awake uses empty string; full debug grows height).
        /// </summary>
        private void RefreshGridCoordinatesChromeLayout()
        {
            if (gridCoordinatesText == null)
                return;
            Transform chromeRoot = FindGridCoordinatesChromeRoot(gridCoordinatesText.transform);
            if (chromeRoot == null)
                return;
            RectTransform chromeRt = chromeRoot.GetComponent<RectTransform>();
            if (chromeRt == null)
                return;
            AlignGridCoordinatesChrome(chromeRt);
            UpdateGridCoordinatesScrollLayout(chromeRt, gridCoordinatesText);
        }

        private static float EstimateGridCoordinatesTextPreferredHeight(Text text, float innerWidth)
        {
            if (text == null)
                return 64f;
            string sample = string.IsNullOrEmpty(text.text) ? " " : text.text;
            float w = Mathf.Max(innerWidth, 40f);
            TextGenerationSettings settings = text.GetGenerationSettings(new Vector2(w, 0.01f));
            float px = text.cachedTextGeneratorForLayout.GetPreferredHeight(sample, settings);
            return Mathf.Clamp(px, 28f, 10000f);
        }

        private void ApplyGridCoordinatesChromeTextStyle()
        {
            if (gridCoordinatesText == null)
                return;
            gridCoordinatesText.color = Color.white;
            gridCoordinatesText.fontSize = hudUiTheme != null ? Mathf.Max(hudUiTheme.FontSizeCaption, 11) : 12;
            gridCoordinatesText.horizontalOverflow = HorizontalWrapMode.Wrap;
            gridCoordinatesText.verticalOverflow = VerticalWrapMode.Overflow;
            // Scene default was MiddleLeft — large vertical chrome looked like "padding" while copy stayed flush left.
            gridCoordinatesText.alignment = TextAnchor.UpperLeft;
        }

        /// <summary>
        /// Add horizontal demand gauges under R/C/I stat rows if missing (taller fills, heavy-zoning tint in <see cref="UIManager.Hud"/>).
        /// </summary>
        private void EnsureDemandGaugeBars()
        {
            EnsureDemandGaugeForPanel(demandResidentialText, "DemandResidentialPanel", ref demandResidentialBarFill);
            EnsureDemandGaugeForPanel(demandCommercialText, "DemandCommercialPanel", ref demandCommercialBarFill);
            EnsureDemandGaugeForPanel(demandIndustrialText, "DemandIndustrialPanel", ref demandIndustrialBarFill);
        }

        private void EnsureDemandGaugeForPanel(Text anchorText, string panelExactName, ref Image fillImageRef)
        {
            if (anchorText == null)
                return;
            Transform panel = FindNamedAncestor(anchorText.transform, panelExactName);
            if (panel == null)
                return;
            Transform existing = panel.Find("Fe50DemandGauge");
            if (existing != null)
            {
                RectTransform gaugeRtExisting = existing.GetComponent<RectTransform>();
                if (gaugeRtExisting != null)
                {
                    gaugeRtExisting.offsetMin = new Vector2(8f, 5f);
                    gaugeRtExisting.offsetMax = new Vector2(-8f, 18f);
                }

                if (fillImageRef == null)
                {
                    Transform fillT = existing.Find("Fe50DemandGaugeFill");
                    if (fillT != null)
                        fillImageRef = fillT.GetComponent<Image>();
                }

                return;
            }

            var panelBg = panel.GetComponent<Image>();
            Sprite sprite = panelBg != null ? panelBg.sprite : null;

            GameObject gauge = new GameObject("Fe50DemandGauge", typeof(RectTransform));
            gauge.transform.SetParent(panel, false);
            RectTransform gaugeRt = gauge.GetComponent<RectTransform>();
            gaugeRt.anchorMin = new Vector2(0f, 0f);
            gaugeRt.anchorMax = new Vector2(1f, 0f);
            gaugeRt.pivot = new Vector2(0.5f, 0f);
            gaugeRt.offsetMin = new Vector2(8f, 5f);
            gaugeRt.offsetMax = new Vector2(-8f, 18f);

            GameObject trackGo = new GameObject("Fe50DemandGaugeTrack", typeof(RectTransform), typeof(Image));
            trackGo.transform.SetParent(gauge.transform, false);
            RectTransform trackRt = trackGo.GetComponent<RectTransform>();
            trackRt.anchorMin = Vector2.zero;
            trackRt.anchorMax = Vector2.one;
            trackRt.offsetMin = Vector2.zero;
            trackRt.offsetMax = Vector2.zero;
            Image trackImg = trackGo.GetComponent<Image>();
            trackImg.sprite = sprite;
            trackImg.type = Image.Type.Simple;
            trackImg.raycastTarget = false;
            Color trackCol = hudUiTheme != null ? hudUiTheme.BorderSubtle : new Color(0.12f, 0.13f, 0.16f, 1f);
            trackCol.a = Mathf.Max(0.55f, trackCol.a * 0.85f);
            trackImg.color = trackCol;

            GameObject fillGo = new GameObject("Fe50DemandGaugeFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(gauge.transform, false);
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            Image fillImg = fillGo.GetComponent<Image>();
            fillImg.sprite = sprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 0.5f;
            fillImg.raycastTarget = false;
            fillImageRef = fillImg;
        }

        private static Transform FindNamedAncestor(Transform t, string exactName)
        {
            while (t != null)
            {
                if (t.name == exactName)
                    return t;
                t = t.parent;
            }

            return null;
        }

        private void ApplyHeroStatRow(Text valueText)
        {
            if (valueText == null)
                return;
            StyleSiblingLabelTexts(valueText.transform, hudUiTheme.FontSizeCaption, hudUiTheme.TextSecondary);
            valueText.fontSize = hudUiTheme.FontSizeDisplay;
            valueText.color = hudUiTheme.TextPrimary;
            valueText.supportRichText = true;
        }

        /// <summary>
        /// Toolbar chip next to ShowTaxesButton: compact menu size, rich money/delta, no sibling restyling (avoid touching unrelated HUD texts).
        /// </summary>
        private void ApplyToolbarMoneyRow(Text valueText)
        {
            if (valueText == null || hudUiTheme == null)
                return;
            valueText.fontSize = hudUiTheme.MenuButtonFontSize;
            valueText.color = hudUiTheme.TextPrimary;
            valueText.supportRichText = true;
            valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            valueText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private void ApplyBodyStatRow(Text textField, bool styleSiblingLabels = true)
        {
            if (textField == null)
                return;
            if (styleSiblingLabels)
                StyleSiblingLabelTexts(textField.transform, hudUiTheme.FontSizeCaption, hudUiTheme.TextSecondary);
            textField.fontSize = hudUiTheme.FontSizeBody;
            textField.color = hudUiTheme.TextPrimary;
            textField.supportRichText = true;
        }

        /// <summary>
        /// TaxPanel holds many <see cref="Text"/> rows as direct children → <see cref="ApplyBodyStatRow"/> sibling styling would cross-bleed.
        /// Pass: caption tokens for static "%" titles; body tokens for growth budget value labels only.
        /// </summary>
        private void ApplyTaxPanelBudgetRowTexts()
        {
            if (hudUiTheme == null || residentialTaxText == null)
                return;
            Transform taxPanel = residentialTaxText.transform.parent;
            if (taxPanel == null)
                return;

            foreach (Transform child in taxPanel)
            {
                Text t = child.GetComponent<Text>();
                if (t == null)
                    continue;
                string n = child.name;
                if (n == "ResidentialTaxText" || n.StartsWith("CommercialTaxText", StringComparison.Ordinal) || n == "IndustrialTaxText")
                    continue;

                if (n == "TaxGrowthBudgetPercentLabel" ||
                    (n.Contains("GrowthLabel", StringComparison.Ordinal) && n.Contains("(1)", StringComparison.Ordinal)))
                {
                    ApplyCaptionText(t);
                    continue;
                }

                if (n == "TotalGrowthLabel" || n == "RoadGrowthLabel" || n == "EnergyGrowthLabel" || n == "WaterGrowthLabel" || n == "ZoningGrowthLabel")
                    ApplyBodyStatRow(t, styleSiblingLabels: false);
            }
        }

        /// <summary>
        /// Add thin horizontal rules between growth-budget sliders, category sliders, tax rows if missing (runtime Tax panel dividers).
        /// </summary>
        private void EnsureTaxPanelDividerStripes()
        {
            if (hudUiTheme == null || residentialTaxText == null)
                return;
            Transform taxPanel = residentialTaxText.transform.parent;
            if (taxPanel == null || taxPanel.Find("Fe50TaxDividerUpper") != null)
                return;

            var panelBg = taxPanel.GetComponent<Image>();
            Sprite stripeSprite = panelBg != null ? panelBg.sprite : null;

            Color line = hudUiTheme.BorderSubtle;
            CreateTaxPanelDivider(taxPanel, "Fe50TaxDividerUpper", stripeSprite, line, new Vector2(0f, 48f), new Vector2(200f, 1f));
            CreateTaxPanelDivider(taxPanel, "Fe50TaxDividerLower", stripeSprite, line, new Vector2(0f, -40f), new Vector2(200f, 1f));
        }

        private static void CreateTaxPanelDivider(Transform parent, string objectName, Sprite sprite, Color lineColor, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            img.color = lineColor;
        }

        private void ApplyCaptionText(Text textField)
        {
            if (textField == null)
                return;
            textField.fontSize = hudUiTheme.FontSizeCaption;
            textField.color = hudUiTheme.TextSecondary;
        }

        private static void StyleSiblingLabelTexts(Transform valueTransform, int captionSize, Color captionColor)
        {
            Transform parent = valueTransform.parent;
            if (parent == null)
                return;
            foreach (Transform child in parent)
            {
                if (child == valueTransform)
                    continue;
                var t = child.GetComponent<Text>();
                if (t == null)
                    continue;
                t.fontSize = captionSize;
                t.color = captionColor;
            }
        }

        /// <summary>
        /// Walk parents from serialized HUD <see cref="Text"/> until <paramref name="panelName"/>; tint its <see cref="Image"/>.
        /// Supports inactive stat panels (avoids <c>GameObject.Find</c> missing disabled objects).
        /// </summary>
        private void TintPanelRootBehindReference(string panelName, Text anchorText, Color color)
        {
            if (anchorText == null)
                return;
            Transform t = anchorText.transform;
            for (int depth = 0; depth < 24 && t != null; depth++)
            {
                if (t.name == panelName)
                {
                    var image = t.GetComponent<Image>();
                    if (image != null)
                        image.color = color;
                    return;
                }
                t = t.parent;
            }
        }

        private void ApplyLoadGameAndFundsPanels()
        {
            if (loadGameMenu != null)
            {
                var rootImage = loadGameMenu.GetComponent<Image>();
                if (rootImage != null)
                    rootImage.color = hudUiTheme.ModalDimmerColor;
                foreach (Transform child in loadGameMenu.transform)
                {
                    var img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = hudUiTheme.SurfaceCardHud;
                        break;
                    }
                }
            }

            if (insufficientFundsPanel != null)
            {
                var rootImage = insufficientFundsPanel.GetComponent<Image>();
                if (rootImage != null)
                    rootImage.color = hudUiTheme.ModalDimmerColor;
                foreach (Transform child in insufficientFundsPanel.transform)
                {
                    var img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = hudUiTheme.SurfaceCardHud;
                        break;
                    }
                }
            }
        }
    }
}
