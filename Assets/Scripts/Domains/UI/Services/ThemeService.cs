// registry-resolve-exempt: internal factory — constructs own sub-services (ThemeStyleApplyService, ThemeCacheService) within UI domain
using UnityEngine;
using UnityEngine.UI;

namespace Domains.UI.Services
{
    /// <summary>
    /// Thin orchestrator facade — delegates to ThemeTokenResolveService, ThemeStyleApplyService,
    /// ThemeCacheService.  Implements ITheme; path + namespace UNCHANGED (Stage 7.4 Tier-E split).
    /// </summary>
    public class ThemeService : ITheme
    {
        // ─── Public const forwarding (consumers may reference via ThemeService.X) ─
        public const string CellDataPanelName = ThemeTokenResolveService.CellDataPanelName;
        public const string CellDataPanelNameAlt = ThemeTokenResolveService.CellDataPanelNameAlt;
        public const string CellDataPanelTextInsetName = ThemeTokenResolveService.CellDataPanelTextInsetName;
        public const string CellDataPanelTextHolderAlt = ThemeTokenResolveService.CellDataPanelTextHolderAlt;
        public const string CellDataPanelScrollRootName = ThemeTokenResolveService.CellDataPanelScrollRootName;
        public const string CellDataPanelViewportName = ThemeTokenResolveService.CellDataPanelViewportName;
        public const string CellDataPanelContentName = ThemeTokenResolveService.CellDataPanelContentName;
        public const string ControlPanelObjectName = ThemeTokenResolveService.ControlPanelObjectName;
        public const string DataPanelButtonsObjectName = ThemeTokenResolveService.DataPanelButtonsObjectName;
        public const float CellDataPanelGapAboveControlPanel = ThemeTokenResolveService.CellDataPanelGapAboveControlPanel;
        public const float CellDataPanelGapBelowDataPanelButtons = ThemeTokenResolveService.CellDataPanelGapBelowDataPanelButtons;
        public const float CellDataPanelGapAboveMinimap = ThemeTokenResolveService.CellDataPanelGapAboveMinimap;
        public const float CellDataPanelMaxSquareSide = ThemeTokenResolveService.CellDataPanelMaxSquareSide;

        // ─── Sub-services ─────────────────────────────────────────────────────────
        private readonly ThemeStyleApplyService _style = new ThemeStyleApplyService();
        private readonly ThemeCacheService _cache = new ThemeCacheService();

        // ─── ITheme (facade interface) ────────────────────────────────────────────
        void ITheme.StyleSiblingLabelTexts(Transform valueTransform, int captionSize, Color captionColor)
            => ThemeStyleApplyService.StyleSiblingLabelTexts(valueTransform, captionSize, captionColor);

        Transform ITheme.FindNamedAncestor(Transform t, string exactName)
            => ThemeTokenResolveService.FindNamedAncestor(t, exactName);

        // ─── Style apply delegates ────────────────────────────────────────────────
        /// <summary>Style hero stat row — caption + display sizes/colors.</summary>
        public void ApplyHeroStatRow(Text valueText, int fontSizeCaption, Color textSecondary, int fontSizeDisplay, Color textPrimary)
            => _style.ApplyHeroStatRow(valueText, fontSizeCaption, textSecondary, fontSizeDisplay, textPrimary);

        /// <summary>Style toolbar money row text.</summary>
        public void ApplyToolbarMoneyRow(Text valueText, int menuButtonFontSize, Color textPrimary)
            => _style.ApplyToolbarMoneyRow(valueText, menuButtonFontSize, textPrimary);

        /// <summary>Style body stat row — body text + optional sibling caption labels.</summary>
        public void ApplyBodyStatRow(Text textField, int fontSizeBody, Color textPrimary, int fontSizeCaption, Color textSecondary, bool styleSiblingLabels = true)
            => _style.ApplyBodyStatRow(textField, fontSizeBody, textPrimary, fontSizeCaption, textSecondary, styleSiblingLabels);

        /// <summary>Style caption text — size + secondary color.</summary>
        public void ApplyCaptionText(Text textField, int fontSizeCaption, Color textSecondary)
            => _style.ApplyCaptionText(textField, fontSizeCaption, textSecondary);

        /// <summary>Tint root panel found behind named anchor text.</summary>
        public void TintPanelRootBehindReference(string panelName, Text anchorText, Color color)
            => _style.TintPanelRootBehindReference(panelName, anchorText, color);

        /// <summary>Style tax panel budget row texts.</summary>
        public void ApplyTaxPanelBudgetRowTexts(Text residentialTaxText, int fontSizeBody, Color textPrimary, int fontSizeCaption, Color textSecondary)
            => _style.ApplyTaxPanelBudgetRowTexts(residentialTaxText, fontSizeBody, textPrimary, fontSizeCaption, textSecondary);

        /// <summary>Ensure tax panel has divider stripes.</summary>
        public void EnsureTaxPanelDividerStripes(Text residentialTaxText, Color borderSubtle)
            => _style.EnsureTaxPanelDividerStripes(residentialTaxText, borderSubtle);

        /// <summary>Tint load-game + insufficient-funds modal panels.</summary>
        public void ApplyLoadGameAndFundsPanels(GameObject loadGameMenu, GameObject insufficientFundsPanel, Color modalDimmerColor, Color surfaceCardHud)
            => _style.ApplyLoadGameAndFundsPanels(loadGameMenu, insufficientFundsPanel, modalDimmerColor, surfaceCardHud);

        /// <summary>Style cell-data panel text — caption size.</summary>
        public void ApplyCellDataPanelTextStyle(Text gridCoordinatesText, int fontSizeCaption)
            => _style.ApplyCellDataPanelTextStyle(gridCoordinatesText, fontSizeCaption);

        // ─── Cache / chrome delegates ─────────────────────────────────────────────
        /// <summary>Ensure demand-gauge fill image on panel; mint if missing.</summary>
        public void EnsureDemandGaugeForPanel(Text anchorText, string panelExactName, ref Image fillImageRef, Color? borderSubtle, Color? surfaceToolbar)
            => _cache.EnsureDemandGaugeForPanel(anchorText, panelExactName, ref fillImageRef, borderSubtle, surfaceToolbar);

        /// <summary>Ensure cell-data panel chrome — bg + inset + tint.</summary>
        public void EnsureCellDataPanelChrome(ref Text gridCoordinatesText, Color? surfaceToolbar)
            => _cache.EnsureCellDataPanelChrome(ref gridCoordinatesText, surfaceToolbar);

        /// <summary>Ensure cell-data panel text field exists; mint if missing.</summary>
        public void EnsureCellDataPanelTextField(ref Text gridCoordinatesText)
            => _cache.EnsureCellDataPanelTextField(ref gridCoordinatesText);

        /// <summary>Rebuild cell-data panel layout for current text.</summary>
        public void RefreshCellDataPanelLayout(Text gridCoordinatesText)
            => _cache.RefreshCellDataPanelLayout(gridCoordinatesText);

        // ─── Static forwarding (consumers may call ThemeService.StaticHelper) ─────
        /// <summary>True if name matches cell-data panel root.</summary>
        public static bool IsCellDataPanelRootName(string n) => ThemeTokenResolveService.IsCellDataPanelRootName(n);
        /// <summary>True if name matches cell-data panel text holder.</summary>
        public static bool IsCellDataPanelTextHolderName(string n) => ThemeTokenResolveService.IsCellDataPanelTextHolderName(n);
        /// <summary>Walk up to find cell-data panel root transform.</summary>
        public static Transform FindCellDataPanelRoot(Transform from) => ThemeTokenResolveService.FindCellDataPanelRoot(from);
        /// <summary>Find cell-data panel inset rect under chrome.</summary>
        public static RectTransform FindCellDataPanelInset(RectTransform chromeRt) => ThemeTokenResolveService.FindCellDataPanelInset(chromeRt);
        /// <summary>Walk up to find HUD layout root.</summary>
        public static Transform FindHudLayoutRoot(Transform from) => ThemeTokenResolveService.FindHudLayoutRoot(from);
        /// <summary>Find HUD layout root via scene scan — post-rebuild.</summary>
        public static Transform FindHudLayoutRootForRebuild() => ThemeTokenResolveService.FindHudLayoutRootForRebuild();
        /// <summary>Style sibling label texts under given value transform.</summary>
        public static void StyleSiblingLabelTexts(Transform valueTransform, int captionSize, Color captionColor)
            => ThemeStyleApplyService.StyleSiblingLabelTexts(valueTransform, captionSize, captionColor);
        /// <summary>Walk ancestors for transform matching exact name.</summary>
        public static Transform FindNamedAncestor(Transform t, string exactName) => ThemeTokenResolveService.FindNamedAncestor(t, exactName);
        /// <summary>Get child rect bounds inside parent rect coord space.</summary>
        public static bool TryGetRectBoundsInParent(RectTransform parentRt, RectTransform childRt, out float minX, out float maxX, out float minY, out float maxY)
            => ThemeTokenResolveService.TryGetRectBoundsInParent(parentRt, childRt, out minX, out maxX, out minY, out maxY);
        /// <summary>Mint divider stripe Image under parent.</summary>
        public static void CreateDividerStripe(Transform parent, string objectName, Sprite sprite, Color lineColor, Vector2 anchoredPosition, Vector2 sizeDelta)
            => ThemeStyleApplyService.CreateDividerStripe(parent, objectName, sprite, lineColor, anchoredPosition, sizeDelta);
        /// <summary>Mount cell-data panel chrome under HUD layout root.</summary>
        public static void EnsureCellDataPanelHudMount(RectTransform chromeRt) => ThemeCacheService.EnsureCellDataPanelHudMount(chromeRt);
        /// <summary>Apply inset rect anchors/padding to cell-data panel.</summary>
        public static void ApplyCellDataPanelTextInset(RectTransform insetRt) => ThemeCacheService.ApplyCellDataPanelTextInset(insetRt);
        /// <summary>Ensure ContentSizeFitter on cell-data panel text rect.</summary>
        public static void EnsureCellDataPanelTextLayoutDriver(RectTransform textRt) => ThemeCacheService.EnsureCellDataPanelTextLayoutDriver(textRt);
        /// <summary>Reparent text rect under chrome inset.</summary>
        public static void EnsureCellDataPanelTextUnderInset(Transform chromeTransform, RectTransform textRt) => ThemeCacheService.EnsureCellDataPanelTextUnderInset(chromeTransform, textRt);
        /// <summary>Reparent scroll-content under inset rect.</summary>
        public static void EnsureCellDataPanelScrollUnderInset(RectTransform insetRt, RectTransform textRt) => ThemeCacheService.EnsureCellDataPanelScrollUnderInset(insetRt, textRt);
        /// <summary>Update scroll viewport sizes for cell-data panel.</summary>
        public static void UpdateCellDataPanelScrollLayout(RectTransform chromeRt, Text dbgText) => ThemeCacheService.UpdateCellDataPanelScrollLayout(chromeRt, dbgText);
        /// <summary>Align cell-data panel to control panel or minimap.</summary>
        public static void AlignCellDataPanel(RectTransform chromeRt) => ThemeCacheService.AlignCellDataPanel(chromeRt);
        /// <summary>Apply square layout to cell-data panel above reference rect.</summary>
        public static void CellDataPanelApplySquareLayout(RectTransform chromeRt, RectTransform parentRt, float minX, float referenceTopY, float gapAboveReference, float widthBand)
            => ThemeCacheService.CellDataPanelApplySquareLayout(chromeRt, parentRt, minX, referenceTopY, gapAboveReference, widthBand);
        /// <summary>Try aligning cell-data panel above control panel; false if missing.</summary>
        public static bool TryAlignCellDataPanelToControlPanel(RectTransform chromeRt) => ThemeCacheService.TryAlignCellDataPanelToControlPanel(chromeRt);
        /// <summary>Align cell-data panel above minimap.</summary>
        public static void AlignCellDataPanelToMiniMap(RectTransform chromeRt) => ThemeCacheService.AlignCellDataPanelToMiniMap(chromeRt);
        /// <summary>Compute preferred text height at given inner width.</summary>
        public static float EstimateCellDataPanelTextPreferredHeight(Text text, float innerWidth) => ThemeCacheService.EstimateCellDataPanelTextPreferredHeight(text, innerWidth);
    }
}
