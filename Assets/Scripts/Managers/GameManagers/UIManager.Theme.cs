using UnityEngine;
using UnityEngine.UI;

namespace Territory.UI
{
    /// <summary>
    /// Apply UiTheme tokens to city HUD texts + common panel chrome (partial of UIManager).
    /// Pass-through over ThemeService POCO port.  Class name + namespace + path + every [SerializeField] UNCHANGED.
    /// Cutover Stage 2 (TECH-26631). Implements Domains.UI.ITheme facade via _themeService field.
    /// </summary>
    public partial class UIManager
    {
        [Header("Theme (optional)")]
        [Tooltip("Assign DefaultUiTheme or a variant; when null, HUD keeps scene-authored colors.")]
        [SerializeField] private UiTheme hudUiTheme;

        // Stage 11 (game-ui-design-system): legacy controlPanelBackgroundImage decommissioned.

        private Domains.UI.Services.ThemeService _themeService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("[UIManager] duplicate instance detected; replacing prior reference.");
            Instance = this;
            _themeService = new Domains.UI.Services.ThemeService();

            // Stage 12 trigger contract: modal roots must start deactivated.
            if (infoPanelRoot != null && infoPanelRoot.activeSelf) infoPanelRoot.SetActive(false);
            if (pauseMenuRoot != null && pauseMenuRoot.activeSelf) pauseMenuRoot.SetActive(false);
            if (settingsScreenRoot != null && settingsScreenRoot.activeSelf) settingsScreenRoot.SetActive(false);
            if (saveLoadScreenRoot != null && saveLoadScreenRoot.activeSelf) saveLoadScreenRoot.SetActive(false);
            if (newGameScreenRoot != null && newGameScreenRoot.activeSelf) newGameScreenRoot.SetActive(false);
        }

        /// <summary>Apply typography + surface colors once at startup when hudUiTheme assigned.</summary>
        private void ApplyHudUiThemeIfConfigured()
        {
            EnsureCellDataPanelChrome();
            EnsureDemandGaugeBars();
            if (hudUiTheme == null) return;

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

            _themeService.TintPanelRootBehindReference("DatePanel", dateText, hudUiTheme.SurfaceCardHud);
            _themeService.TintPanelRootBehindReference("TaxPanel", residentialTaxText, hudUiTheme.SurfaceCardHud);
            ApplyTaxPanelBudgetRowTexts();
            EnsureTaxPanelDividerStripes();
            _themeService.TintPanelRootBehindReference("DetailsPanel", detailsNameText, hudUiTheme.SurfaceCardHud);

            if (demandWarningPanel != null)
            {
                var dw = demandWarningPanel.GetComponent<Image>();
                if (dw != null) dw.color = hudUiTheme.SurfaceCardHud;
            }

            ApplyLoadGameAndFundsPanels();
        }

        private void EnsureCellDataPanelChrome() =>
            _themeService.EnsureCellDataPanelChrome(ref gridCoordinatesText, hudUiTheme != null ? (UnityEngine.Color?)hudUiTheme.SurfaceToolbar : null);

        private void EnsureDemandGaugeBars()
        {
            _themeService.EnsureDemandGaugeForPanel(demandResidentialText, "DemandResidentialPanel", ref demandResidentialBarFill,
                hudUiTheme != null ? (UnityEngine.Color?)hudUiTheme.BorderSubtle : null, hudUiTheme != null ? (UnityEngine.Color?)hudUiTheme.SurfaceToolbar : null);
            _themeService.EnsureDemandGaugeForPanel(demandCommercialText, "DemandCommercialPanel", ref demandCommercialBarFill,
                hudUiTheme != null ? (UnityEngine.Color?)hudUiTheme.BorderSubtle : null, hudUiTheme != null ? (UnityEngine.Color?)hudUiTheme.SurfaceToolbar : null);
            _themeService.EnsureDemandGaugeForPanel(demandIndustrialText, "DemandIndustrialPanel", ref demandIndustrialBarFill,
                hudUiTheme != null ? (UnityEngine.Color?)hudUiTheme.BorderSubtle : null, hudUiTheme != null ? (UnityEngine.Color?)hudUiTheme.SurfaceToolbar : null);
        }

        private void ApplyBodyStatRow(Text textField, bool styleSiblingLabels = true) =>
            _themeService.ApplyBodyStatRow(textField, hudUiTheme.FontSizeBody, hudUiTheme.TextPrimary, hudUiTheme.FontSizeCaption, hudUiTheme.TextSecondary, styleSiblingLabels);

        private void ApplyCaptionText(Text textField) =>
            _themeService.ApplyCaptionText(textField, hudUiTheme.FontSizeCaption, hudUiTheme.TextSecondary);

        private void ApplyTaxPanelBudgetRowTexts() =>
            _themeService.ApplyTaxPanelBudgetRowTexts(residentialTaxText, hudUiTheme.FontSizeBody, hudUiTheme.TextPrimary, hudUiTheme.FontSizeCaption, hudUiTheme.TextSecondary);

        private void EnsureTaxPanelDividerStripes() =>
            _themeService.EnsureTaxPanelDividerStripes(residentialTaxText, hudUiTheme.BorderSubtle);

        private void ApplyLoadGameAndFundsPanels() =>
            _themeService.ApplyLoadGameAndFundsPanels(loadGameMenu, insufficientFundsPanel, hudUiTheme.ModalDimmerColor, hudUiTheme.SurfaceCardHud);

        private void ApplyCellDataPanelTextStyle() =>
            _themeService.ApplyCellDataPanelTextStyle(gridCoordinatesText, hudUiTheme != null ? hudUiTheme.FontSizeCaption : 11);

        private void RefreshCellDataPanelLayout() =>
            _themeService.RefreshCellDataPanelLayout(gridCoordinatesText);

        // §Open Questions (Q5=5b dead-public sweep):
        // ApplyHeroStatRow — zero external callers; retained for sibling-partial potential use.
        // ApplyToolbarMoneyRow — zero external callers; retained for sibling-partial potential use.
        private void ApplyHeroStatRow(Text valueText)
        {
            if (hudUiTheme == null) return;
            _themeService.ApplyHeroStatRow(valueText, hudUiTheme.FontSizeCaption, hudUiTheme.TextSecondary, hudUiTheme.FontSizeDisplay, hudUiTheme.TextPrimary);
        }

        private void ApplyToolbarMoneyRow(Text valueText)
        {
            if (hudUiTheme == null) return;
            _themeService.ApplyToolbarMoneyRow(valueText, hudUiTheme.MenuButtonFontSize, hudUiTheme.TextPrimary);
        }
    }
}
