using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections.Generic;
using Territory.Core;
using Territory.Roads;
using Territory.Zones;
using Territory.Economy;
using Territory.Terrain;
using Territory.Timing;
using Territory.Persistence;
using Territory.Forests;
using Territory.Buildings;
using Territory.Utilities;

namespace Territory.UI
{
// HUD stats, demand display, and grid debug text (partial of UIManager).
public partial class UIManager
{
    #region Demand Display
    public void UpdateUI()
    {
        if (cityNameText != null && cityStats != null)
            cityNameText.text = cityStats.cityName;
        populationText.text = cityStats.population.ToString();
        int delta = economyManager != null ? economyManager.GetMonthlyIncomeDelta() : 0;
        string deltaStr = delta >= 0 ? $"(+${delta:N0})" : $"(-${Mathf.Abs(delta):N0})";
        if (hudUiTheme != null)
        {
            string primaryHex = ColorUtility.ToHtmlStringRGBA(hudUiTheme.TextPrimary);
            string deltaHex = ColorUtility.ToHtmlStringRGBA(delta >= 0 ? hudUiTheme.AccentPositive : hudUiTheme.AccentNegative);
            if (moneyText != null)
                moneyText.text = $"<color=#{primaryHex}>{cityStats.money:N0}</color> <color=#{deltaHex}>{deltaStr}</color>";
            if (buttonMoneyText != null)
                buttonMoneyText.text = $"<color=#{primaryHex}>${cityStats.money:N0}</color> <color=#{deltaHex}>{deltaStr}</color>";
        }
        else
        {
            if (moneyText != null)
                moneyText.text = $"{cityStats.money:N0} {deltaStr}";
            if (buttonMoneyText != null)
                buttonMoneyText.text = $"${cityStats.money:N0} {deltaStr}";
        }
        happinessText.text = $"{cityStats.happiness:F0}/100";

        cityPowerOutputText.text = cityStats.cityPowerOutput.ToString() + " MW";
        cityPowerConsumptionText.text = cityStats.cityPowerConsumption.ToString() + " MW";
        cityWaterOutputText.text = cityStats.cityWaterOutput.ToString() + " kL";
        cityWaterConsumptionText.text = cityStats.cityWaterConsumption.ToString() + " kL";

        dateText.text = timeManager.GetCurrentDate().Date.ToString();
        residentialTaxText.text = "Residential Tax: " + economyManager.GetResidentialTax() + "%";
        commercialTaxText.text = "Commercial Tax: " + economyManager.GetCommercialTax() + "%";
        industrialTaxText.text = "Industrial Tax: " + economyManager.GetIndustrialTax() + "%";

        EmploymentManager employment = FindObjectOfType<EmploymentManager>();
        DemandManager demand = FindObjectOfType<DemandManager>();
        StatisticsManager stats = FindObjectOfType<StatisticsManager>();

        if (employment != null)
        {
            unemploymentRateText.text = employment.unemploymentRate.ToString("F1") + "%";
            totalJobsText.text = employment.GetAvailableJobs().ToString();

            if (totalJobsCreatedText != null)
                totalJobsCreatedText.text = employment.GetTotalJobs().ToString();
            if (availableJobsText != null)
                availableJobsText.text = employment.GetAvailableJobs().ToString();
            if (jobsTakenText != null)
                jobsTakenText.text = employment.GetJobsTakenByResidents().ToString();
        }

        if (demand != null)
        {
            demandResidentialText.text = demand.GetResidentialDemand().demandStatus +
                " (" + demand.GetResidentialDemand().demandLevel.ToString("F0") + ")";
            demandCommercialText.text = demand.GetCommercialDemand().demandStatus +
                " (" + demand.GetCommercialDemand().demandLevel.ToString("F0") + ")";
            demandIndustrialText.text = demand.GetIndustrialDemand().demandStatus +
                " (" + demand.GetIndustrialDemand().demandLevel.ToString("F0") + ")";
        }

        UpdateDemandBarFills(demand);

        // Update demand feedback for selected zone type
        UpdateDemandFeedback();

        // Update construction cost display near cursor
        UpdateConstructionCostDisplay();
    }

    /// <summary>
    /// Writes <see cref="gridCoordinatesText"/> from <see cref="GridManager.mouseGridPosition"/>; called from <see cref="LateUpdate"/> so it stays in sync with grid picking after <see cref="GridManager.Update"/>.
    /// </summary>
    void UpdateGridCoordinatesDebugText()
    {
        if (gridCoordinatesText == null)
            return;
        if (gameDebugInfoBuilder == null)
            gameDebugInfoBuilder = FindObjectOfType<GameDebugInfoBuilder>();
        if (gameDebugInfoBuilder != null && useFullDebugText && gridManager != null)
        {
            gridCoordinatesText.text = gameDebugInfoBuilder.GetFullDebugText(gridManager.mouseGridPosition, gridManager.selectedPoint);
            RefreshGridCoordinatesChromeLayout();
            return;
        }
        if (gridManager == null)
            return;
        int x = (int)gridManager.mouseGridPosition.x;
        int y = (int)gridManager.mouseGridPosition.y;
        int cx = gridManager.chunkSize > 0 ? x / gridManager.chunkSize : 0;
        int cy = gridManager.chunkSize > 0 ? y / gridManager.chunkSize : 0;
        string line = "x: " + x + ", y: " + y + ", chunk: (" + cx + "," + cy + ")";
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager != null && x >= 0 && x < gridManager.width && y >= 0 && y < gridManager.height)
        {
            int s = waterManager.GetWaterSurfaceHeight(x, y);
            line += s >= 0 ? ", S: " + s : ", S: n/a";
            WaterMap wm = waterManager.GetWaterMap();
            if (wm != null)
            {
                if (s >= 0)
                    line += ", body: " + wm.GetBodyClassificationAt(x, y) + " id=" + wm.GetWaterBodyId(x, y);
                else
                    line += ", body: n/a";
            }
        }
        gridCoordinatesText.text = line;
        RefreshGridCoordinatesChromeLayout();
    }

    private void HideConstructionCostDisplay()
    {
        if (constructionCostText == null)
            return;
        constructionCostText.gameObject.SetActive(false);
    }

    private void UpdateConstructionCostDisplay()
    {
        if (constructionCostText == null)
            return;

        // Hide when pointer is over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            HideConstructionCostDisplay();
            return;
        }

        // Hide when not in placement mode
        if (bulldozeMode || detailsMode)
        {
            HideConstructionCostDisplay();
            return;
        }

        Zone.ZoneType selectedZone = GetSelectedZoneType();
        string displayText = "";
        int cost = 0;

        // Check building and forest first (they set selectedZone to Grass)
        if (selectedBuilding != null)
        {
            cost = selectedBuilding.ConstructionCost;
            displayText = $"${cost}";
        }
        else if (selectedForestData.forestType != Forest.ForestType.None)
        {
            IForest forest = GetSelectedForest();
            cost = forest != null ? forest.ConstructionCost : 0;
            displayText = cost > 0 ? $"${cost}" : "$0";
        }
        else if (selectedZone == Zone.ZoneType.Grass)
        {
            HideConstructionCostDisplay();
            return;
        }
        else if (selectedZone == Zone.ZoneType.Road && gridManager != null && gridManager.roadManager != null)
        {
            RoadManager roadManager = gridManager.roadManager;
            if (roadManager.IsDrawingRoad())
            {
                int tiles = roadManager.GetPreviewRoadTileCount();
                cost = roadManager.GetRoadCostForTileCount(tiles);
                displayText = $"${cost}";
            }
            else
            {
                cost = roadManager.GetRoadCostPerTile();
                displayText = $"${cost}";
            }
        }
        else if (selectedZone == Zone.ZoneType.Water)
        {
            cost = ZoneAttributes.Water.ConstructionCost;
            displayText = cost > 0 ? $"${cost}" : "$0";
        }
        else if (IsInZoningMode())
        {
            ZoneAttributes attrs = zoneManager.GetZoneAttributes(selectedZone);
            if (attrs == null)
            {
                HideConstructionCostDisplay();
                return;
            }

            if (zoneManager.IsZoning())
            {
                int cells = zoneManager.GetPreviewZoneCellCount();
                cost = cells * attrs.ConstructionCost;
                displayText = $"${cost}";
            }
            else
            {
                cost = attrs.ConstructionCost;
                displayText = $"${attrs.ConstructionCost}";
            }
        }
        else
        {
            HideConstructionCostDisplay();
            return;
        }

        constructionCostText.text = displayText;
        bool canAfford = cityStats != null && cityStats.CanAfford(cost);
        if (hudUiTheme != null)
            constructionCostText.color = canAfford ? hudUiTheme.AccentPositive : hudUiTheme.AccentNegative;
        else
            constructionCostText.color = canAfford ? Color.green : Color.red;

        RectTransform rectTransform = constructionCostText.GetComponent<RectTransform>();
        if (rectTransform != null)
            rectTransform.position = (Vector3)((Vector2)Input.mousePosition + constructionCostOffset);

        constructionCostText.gameObject.SetActive(true);
    }

    private bool IsInZoningMode()
    {
        Zone.ZoneType selectedZone = GetSelectedZoneType();
        return selectedZone != Zone.ZoneType.Grass &&
            selectedZone != Zone.ZoneType.Road &&
            selectedZone != Zone.ZoneType.Water &&
            selectedZone != Zone.ZoneType.None;
    }

    private void UpdateDemandBarFills(DemandManager demand)
    {
        if (demand == null)
            return;
        ApplyDemandLevelToFill(demandResidentialBarFill, demand.GetResidentialDemand().demandLevel, GetHeavyZoningDemandBarColor(0));
        ApplyDemandLevelToFill(demandCommercialBarFill, demand.GetCommercialDemand().demandLevel, GetHeavyZoningDemandBarColor(1));
        ApplyDemandLevelToFill(demandIndustrialBarFill, demand.GetIndustrialDemand().demandLevel, GetHeavyZoningDemandBarColor(2));
    }

    /// <summary>
    /// R/C/I demand bar tint: <paramref name="rci"/> 0 = residential heavy zoning, 1 = commercial heavy, 2 = industrial heavy (prefab sample, else strong green / blue / yellow).
    /// </summary>
    private Color GetHeavyZoningDemandBarColor(int rci)
    {
        GameObject prefab = null;
        if (zoneManager != null)
        {
            switch (rci)
            {
                case 0:
                    prefab = GetFirstNonNullPrefab(zoneManager.residentialHeavyZoningPrefabs);
                    break;
                case 1:
                    prefab = GetFirstNonNullPrefab(zoneManager.commercialHeavyZoningPrefabs);
                    break;
                case 2:
                    prefab = GetFirstNonNullPrefab(zoneManager.industrialHeavyZoningPrefabs);
                    break;
            }
        }

        Color sampled = SampleZoningPrefabTint(prefab);
        // Prefabs usually keep SpriteRenderer/Image color at white (1,1,1) — tint lives in the sprite; treat grey/white as "no sample".
        if (sampled.a > 0.5f && IsChromaticBarTint(sampled))
            return sampled;

        Color[] fallback =
        {
            new Color(0.15f, 0.82f, 0.35f, 1f),
            new Color(0.25f, 0.52f, 1f, 1f),
            new Color(1f, 0.82f, 0.12f, 1f),
        };
        return fallback[Mathf.Clamp(rci, 0, 2)];
    }

    /// <summary>
    /// True when <paramref name="c"/> is not near grey/white (prefab <see cref="SpriteRenderer.color"/> defaults are unusable for HUD bars).
    /// </summary>
    private static bool IsChromaticBarTint(Color c)
    {
        float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
        return (max - min) >= 0.14f;
    }

    private static GameObject GetFirstNonNullPrefab(List<GameObject> list)
    {
        if (list == null)
            return null;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null)
                return list[i];
        }

        return null;
    }

    /// <summary>
    /// Reads a representative tint from a zoning tile prefab (sprite or uGUI <see cref="Image"/>).
    /// </summary>
    private static Color SampleZoningPrefabTint(GameObject prefab)
    {
        if (prefab == null)
            return new Color(0f, 0f, 0f, 0f);
        var sr = prefab.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = prefab.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null)
            return sr.color;
        var img = prefab.GetComponent<UnityEngine.UI.Image>();
        if (img == null)
            img = prefab.GetComponentInChildren<UnityEngine.UI.Image>(true);
        if (img != null)
            return img.color;
        return new Color(0f, 0f, 0f, 0f);
    }

    private void ApplyDemandLevelToFill(Image fill, float demandLevel, Color barColor)
    {
        if (fill == null)
            return;
        float n = Mathf.Clamp01((demandLevel + 100f) / 200f);
        fill.fillAmount = n;
        barColor.a = 1f;
        fill.color = barColor;
    }

    private void UpdateDemandFeedback()
    {
        if (demandFeedbackText == null || gridManager == null) return;

        Zone.ZoneType selectedZone = GetSelectedZoneType();
        if (selectedZone == Zone.ZoneType.Grass || selectedZone == Zone.ZoneType.Road)
        {
            demandFeedbackText.text = "";
            return;
        }

        string feedback = gridManager.GetDemandFeedback(selectedZone);
        demandFeedbackText.text = feedback;

        // Enhanced color coding for demand levels (theme tokens when assigned)
        if (feedback.Contains("✓"))
        {
            demandFeedbackText.color = hudUiTheme != null ? hudUiTheme.AccentPositive : Color.green;
        }
        else if (feedback.Contains("No Jobs Available"))
        {
            demandFeedbackText.color = hudUiTheme != null ? hudUiTheme.AccentNegative : Color.red;
        }
        else if (feedback.Contains("Need Residents"))
        {
            demandFeedbackText.color = hudUiTheme != null ? hudUiTheme.AccentPrimary : Color.yellow;
        }
        else if (feedback.Contains("✗"))
        {
            demandFeedbackText.color = hudUiTheme != null ? hudUiTheme.AccentNegative : Color.red;
        }
        else
        {
            demandFeedbackText.color = hudUiTheme != null ? hudUiTheme.TextPrimary : Color.white;
        }
    }

    public void ShowDemandWarning(Zone.ZoneType zoneType, float demandLevel)
    {
        if (demandWarningPanel != null)
        {
            demandWarningPanel.SetActive(true);

            Text warningText = demandWarningPanel.GetComponentInChildren<Text>();
            if (warningText != null)
            {
                string message = "";

                // Check if it's residential that needs jobs
                Zone.ZoneType buildingType = GetBuildingTypeFromZoning(zoneType);
                if (IsResidential(buildingType) &&
                    gridManager.demandManager != null &&
                    !gridManager.demandManager.CanPlaceResidentialBuilding())
                {
                    message = $"Cannot place {zoneType}\nNo jobs available for residents!\nBuild commercial/industrial buildings first.";
                }
                // Check if it's a commercial/industrial that needs residents
                else if (IsCommercialOrIndustrial(buildingType) &&
                    gridManager.demandManager != null &&
                    !gridManager.demandManager.CanPlaceCommercialOrIndustrialBuilding(buildingType))
                {
                    message = $"Cannot place {zoneType}\nNeed residential buildings first!\nCommercial/Industrial requires residents to operate.";
                }
                else if (demandLevel < 0)
                {
                    message = $"Warning: Low demand for {zoneType}\nDemand Level: {demandLevel:F0}%\nBuildings may not develop quickly.";
                }
                else
                {
                    message = $"Placing {zoneType}\nDemand Level: {demandLevel:F0}%";
                }

                warningText.text = message;
            }

            // Auto-hide warning after 4 seconds (longer for important messages)
            Invoke("HideDemandWarning", 4f);
        }
    }

    public void HideDemandWarning()
    {
        if (demandWarningPanel != null)
        {
            demandWarningPanel.SetActive(false);
        }
    }

    private Zone.ZoneType GetBuildingTypeFromZoning(Zone.ZoneType zoneType)
    {
        switch (zoneType)
        {
            case Zone.ZoneType.ResidentialLightZoning: return Zone.ZoneType.ResidentialLightBuilding;
            case Zone.ZoneType.ResidentialMediumZoning: return Zone.ZoneType.ResidentialMediumBuilding;
            case Zone.ZoneType.ResidentialHeavyZoning: return Zone.ZoneType.ResidentialHeavyBuilding;
            case Zone.ZoneType.CommercialLightZoning: return Zone.ZoneType.CommercialLightBuilding;
            case Zone.ZoneType.CommercialMediumZoning: return Zone.ZoneType.CommercialMediumBuilding;
            case Zone.ZoneType.CommercialHeavyZoning: return Zone.ZoneType.CommercialHeavyBuilding;
            case Zone.ZoneType.IndustrialLightZoning: return Zone.ZoneType.IndustrialLightBuilding;
            case Zone.ZoneType.IndustrialMediumZoning: return Zone.ZoneType.IndustrialMediumBuilding;
            case Zone.ZoneType.IndustrialHeavyZoning: return Zone.ZoneType.IndustrialHeavyBuilding;
            default: return zoneType;
        }
    }

    private bool IsResidential(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.ResidentialLightBuilding ||
               zoneType == Zone.ZoneType.ResidentialMediumBuilding ||
               zoneType == Zone.ZoneType.ResidentialHeavyBuilding;
    }

    private bool IsCommercialOrIndustrial(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.CommercialLightBuilding ||
               zoneType == Zone.ZoneType.CommercialMediumBuilding ||
               zoneType == Zone.ZoneType.CommercialHeavyBuilding ||
               zoneType == Zone.ZoneType.IndustrialLightBuilding ||
               zoneType == Zone.ZoneType.IndustrialMediumBuilding ||
               zoneType == Zone.ZoneType.IndustrialHeavyBuilding;
    }
    #endregion
}
}
