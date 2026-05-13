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
        public void ApplyHeroStatRow(Text valueText, int fontSizeCaption, Color textSecondary, int fontSizeDisplay, Color textPrimary)
            => _style.ApplyHeroStatRow(valueText, fontSizeCaption, textSecondary, fontSizeDisplay, textPrimary);

        public void ApplyToolbarMoneyRow(Text valueText, int menuButtonFontSize, Color textPrimary)
            => _style.ApplyToolbarMoneyRow(valueText, menuButtonFontSize, textPrimary);

        public void ApplyBodyStatRow(Text textField, int fontSizeBody, Color textPrimary, int fontSizeCaption, Color textSecondary, bool styleSiblingLabels = true)
            => _style.ApplyBodyStatRow(textField, fontSizeBody, textPrimary, fontSizeCaption, textSecondary, styleSiblingLabels);

        public void ApplyCaptionText(Text textField, int fontSizeCaption, Color textSecondary)
            => _style.ApplyCaptionText(textField, fontSizeCaption, textSecondary);

        public void TintPanelRootBehindReference(string panelName, Text anchorText, Color color)
            => _style.TintPanelRootBehindReference(panelName, anchorText, color);

        public void ApplyTaxPanelBudgetRowTexts(Text residentialTaxText, int fontSizeBody, Color textPrimary, int fontSizeCaption, Color textSecondary)
            => _style.ApplyTaxPanelBudgetRowTexts(residentialTaxText, fontSizeBody, textPrimary, fontSizeCaption, textSecondary);

        public void EnsureTaxPanelDividerStripes(Text residentialTaxText, Color borderSubtle)
            => _style.EnsureTaxPanelDividerStripes(residentialTaxText, borderSubtle);

        public void ApplyLoadGameAndFundsPanels(GameObject loadGameMenu, GameObject insufficientFundsPanel, Color modalDimmerColor, Color surfaceCardHud)
            => _style.ApplyLoadGameAndFundsPanels(loadGameMenu, insufficientFundsPanel, modalDimmerColor, surfaceCardHud);

        public void ApplyCellDataPanelTextStyle(Text gridCoordinatesText, int fontSizeCaption)
            => _style.ApplyCellDataPanelTextStyle(gridCoordinatesText, fontSizeCaption);

        // ─── Cache / chrome delegates ─────────────────────────────────────────────
        public void EnsureDemandGaugeForPanel(Text anchorText, string panelExactName, ref Image fillImageRef, Color? borderSubtle, Color? surfaceToolbar)
            => _cache.EnsureDemandGaugeForPanel(anchorText, panelExactName, ref fillImageRef, borderSubtle, surfaceToolbar);

        public void EnsureCellDataPanelChrome(ref Text gridCoordinatesText, Color? surfaceToolbar)
            => _cache.EnsureCellDataPanelChrome(ref gridCoordinatesText, surfaceToolbar);

        public void EnsureCellDataPanelTextField(ref Text gridCoordinatesText)
            => _cache.EnsureCellDataPanelTextField(ref gridCoordinatesText);

        public void RefreshCellDataPanelLayout(Text gridCoordinatesText)
            => _cache.RefreshCellDataPanelLayout(gridCoordinatesText);

        // ─── Static forwarding (consumers may call ThemeService.StaticHelper) ─────
        public static bool IsCellDataPanelRootName(string n) => ThemeTokenResolveService.IsCellDataPanelRootName(n);
        public static bool IsCellDataPanelTextHolderName(string n) => ThemeTokenResolveService.IsCellDataPanelTextHolderName(n);
        public static Transform FindCellDataPanelRoot(Transform from) => ThemeTokenResolveService.FindCellDataPanelRoot(from);
        public static RectTransform FindCellDataPanelInset(RectTransform chromeRt) => ThemeTokenResolveService.FindCellDataPanelInset(chromeRt);
        public static Transform FindHudLayoutRoot(Transform from) => ThemeTokenResolveService.FindHudLayoutRoot(from);
        public static Transform FindHudLayoutRootForRebuild() => ThemeTokenResolveService.FindHudLayoutRootForRebuild();
        public static void StyleSiblingLabelTexts(Transform valueTransform, int captionSize, Color captionColor)
            => ThemeStyleApplyService.StyleSiblingLabelTexts(valueTransform, captionSize, captionColor);
        public static Transform FindNamedAncestor(Transform t, string exactName) => ThemeTokenResolveService.FindNamedAncestor(t, exactName);
        public static bool TryGetRectBoundsInParent(RectTransform parentRt, RectTransform childRt, out float minX, out float maxX, out float minY, out float maxY)
            => ThemeTokenResolveService.TryGetRectBoundsInParent(parentRt, childRt, out minX, out maxX, out minY, out maxY);
        public static void CreateDividerStripe(Transform parent, string objectName, Sprite sprite, Color lineColor, Vector2 anchoredPosition, Vector2 sizeDelta)
            => ThemeStyleApplyService.CreateDividerStripe(parent, objectName, sprite, lineColor, anchoredPosition, sizeDelta);
        public static void EnsureCellDataPanelHudMount(RectTransform chromeRt) => ThemeCacheService.EnsureCellDataPanelHudMount(chromeRt);
        public static void ApplyCellDataPanelTextInset(RectTransform insetRt) => ThemeCacheService.ApplyCellDataPanelTextInset(insetRt);
        public static void EnsureCellDataPanelTextLayoutDriver(RectTransform textRt) => ThemeCacheService.EnsureCellDataPanelTextLayoutDriver(textRt);
        public static void EnsureCellDataPanelTextUnderInset(Transform chromeTransform, RectTransform textRt) => ThemeCacheService.EnsureCellDataPanelTextUnderInset(chromeTransform, textRt);
        public static void EnsureCellDataPanelScrollUnderInset(RectTransform insetRt, RectTransform textRt) => ThemeCacheService.EnsureCellDataPanelScrollUnderInset(insetRt, textRt);
        public static void UpdateCellDataPanelScrollLayout(RectTransform chromeRt, Text dbgText) => ThemeCacheService.UpdateCellDataPanelScrollLayout(chromeRt, dbgText);
        public static void AlignCellDataPanel(RectTransform chromeRt) => ThemeCacheService.AlignCellDataPanel(chromeRt);
        public static void CellDataPanelApplySquareLayout(RectTransform chromeRt, RectTransform parentRt, float minX, float referenceTopY, float gapAboveReference, float widthBand)
            => ThemeCacheService.CellDataPanelApplySquareLayout(chromeRt, parentRt, minX, referenceTopY, gapAboveReference, widthBand);
        public static bool TryAlignCellDataPanelToControlPanel(RectTransform chromeRt) => ThemeCacheService.TryAlignCellDataPanelToControlPanel(chromeRt);
        public static void AlignCellDataPanelToMiniMap(RectTransform chromeRt) => ThemeCacheService.AlignCellDataPanelToMiniMap(chromeRt);
        public static float EstimateCellDataPanelTextPreferredHeight(Text text, float innerWidth) => ThemeCacheService.EstimateCellDataPanelTextPreferredHeight(text, innerWidth);
    }
}
