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
// Toolbar tool selection and ghost preview (partial of UIManager).
public partial class UIManager
{
    #region Toolbar and Selection
    // Stage 11: RequestToolbarChromeRefresh() calls removed — ThemedToolbarStrip self-tints active row.

    public void OnLightResidentialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.ResidentialLightZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RegisterToolSelected();
    }

    public void OnMediumResidentialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.ResidentialMediumZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RegisterToolSelected();
    }

    public void OnHeavyResidentialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.ResidentialHeavyZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RegisterToolSelected();
    }

    public void OnLightCommercialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.CommercialLightZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RegisterToolSelected();
    }

    public void OnMediumCommercialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.CommercialMediumZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RegisterToolSelected();
    }

    public void OnHeavyCommercialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.CommercialHeavyZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RegisterToolSelected();
    }

    public void OnLightIndustrialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.IndustrialLightZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RegisterToolSelected();
    }

    public void OnMediumIndustrialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.IndustrialMediumZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RegisterToolSelected();
    }

    public void OnHeavyIndustrialButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.IndustrialHeavyZoning;
        SetGhostPreview(zoneManager.GetRandomZonePrefab(selectedZoneType, 1), 1);
        CheckAndShowDemandFeedback(selectedZoneType);
        RegisterToolSelected();
    }

    private void CheckAndShowDemandFeedback(Zone.ZoneType zoneType)
    {
        if (gridManager != null && gridManager.demandManager != null)
        {
            float demandLevel = gridManager.demandManager.GetDemandLevel(zoneType);
            bool canGrow = gridManager.demandManager.CanZoneTypeGrow(zoneType);

            // Check residential requirements for commercial/industrial
            Zone.ZoneType buildingType = GetBuildingTypeFromZoning(zoneType);
            bool needsResidential = IsCommercialOrIndustrial(buildingType);
            bool hasResidentialSupport = !needsResidential ||
                gridManager.demandManager.CanPlaceCommercialOrIndustrialBuilding(buildingType);

            // Check job requirements for residential
            bool needsJobs = IsResidential(buildingType);
            bool hasJobsAvailable = !needsJobs ||
                gridManager.demandManager.CanPlaceResidentialBuilding();

            // Show warning for various conditions
            if (!hasJobsAvailable)
            {
                ShowDemandWarning(zoneType, demandLevel);
            }
            else if (!hasResidentialSupport)
            {
                ShowDemandWarning(zoneType, demandLevel);
            }
            else if (!canGrow && demandLevel < -10f)
            {
                ShowDemandWarning(zoneType, demandLevel);
            }
        }
    }

    /// <summary>
    /// Zone S toolbar button — enters S placement mode and opens unified subtype picker (TECH-10500).
    /// </summary>
    public void OnStateServiceZoningButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.StateServiceLightZoning;
        currentSubTypeId = -1;
        RegisterToolSelected();
        ShowSubtypePicker(ToolFamily.StateService);
    }

    /// <summary>TECH-10500: Residential family picker entry — auto-picks Light tier (ghost+tool) and opens picker w/ Light highlighted.</summary>
    public void OnResidentialFamilyButtonClicked()
    {
        OnLightResidentialButtonClicked();
        ShowSubtypePicker(ToolFamily.Residential, (int)Zone.ZoneType.ResidentialLightZoning);
    }

    /// <summary>TECH-10500: Commercial family picker entry — auto-picks Light tier and opens picker.</summary>
    public void OnCommercialFamilyButtonClicked()
    {
        OnLightCommercialButtonClicked();
        ShowSubtypePicker(ToolFamily.Commercial, (int)Zone.ZoneType.CommercialLightZoning);
    }

    /// <summary>TECH-10500: Industrial family picker entry — auto-picks Light tier and opens picker.</summary>
    public void OnIndustrialFamilyButtonClicked()
    {
        OnLightIndustrialButtonClicked();
        ShowSubtypePicker(ToolFamily.Industrial, (int)Zone.ZoneType.IndustrialLightZoning);
    }

    /// <summary>Stage 9.8 (TECH-15894): Power family picker entry — opens coal/solar/wind picker.</summary>
    public void OnPowerFamilyButtonClicked()
    {
        ClearCurrentTool();
        ShowSubtypePicker(ToolFamily.Power);
    }

    /// <summary>Stage 9.8 (TECH-15895): Roads family picker entry — opens street/interstate picker.</summary>
    public void OnRoadsFamilyButtonClicked()
    {
        ClearCurrentTool();
        ShowSubtypePicker(ToolFamily.Roads);
    }

    /// <summary>Stage 9.8 (TECH-15896): Water family picker entry — opens water-treatment picker.</summary>
    public void OnWaterFamilyButtonClicked()
    {
        ClearCurrentTool();
        ShowSubtypePicker(ToolFamily.Water);
    }

    /// <summary>Stage 9.8 (TECH-15896): Forests family picker entry — opens forest picker.</summary>
    public void OnForestsFamilyButtonClicked()
    {
        ClearCurrentTool();
        ShowSubtypePicker(ToolFamily.Forests);
    }

    /// <summary>Stage 9.8 (TECH-15897) — confirm Power subtype from picker (prefab-path route).</summary>
    public void OnPowerFamilySubtypeConfirmed(string prefabPath, int baseCost)
    {
        ClearCurrentTool();
        var prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[UIManager] Power subtype prefab not found: {prefabPath}");
            return;
        }
        var go = Instantiate(prefab);
        var building = go.GetComponent<IBuilding>();
        if (building == null)
            building = go.AddComponent<Territory.Buildings.PowerPlantBuilding>();
        selectedBuilding = building;
        cursorManager.ShowBuildingPreview(prefab, building.BuildingSize > 0 ? building.BuildingSize : 3);
        RegisterToolSelected();
    }

    /// <summary>Stage 9.8 (TECH-15897) — confirm Water subtype from picker.</summary>
    public void OnWaterSubtypeConfirmed(string prefabPath, int baseCost)
    {
        ClearCurrentTool();
        var prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[UIManager] Water subtype prefab not found: {prefabPath}");
            return;
        }
        var go = Instantiate(prefab);
        var building = go.GetComponent<IBuilding>();
        if (building != null)
        {
            selectedBuilding = building;
            cursorManager.ShowBuildingPreview(prefab, building.BuildingSize > 0 ? building.BuildingSize : 2);
        }
        RegisterToolSelected();
    }

    /// <summary>Stage 9.8 (TECH-15897) — confirm Forests subtype from picker.</summary>
    public void OnForestsSubtypeConfirmed(string prefabPath, int baseCost)
    {
        // Forests route through selectedForest / ForestSelectionData — reuse sparse handler.
        OnSparseForestButtonClicked();
    }

    public void OnTwoWayRoadButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.Road;
        // cursorManager.SetRoadCursor();
        if (gridManager != null && gridManager.roadManager != null)
        {
            SetGhostPreview(gridManager.roadManager.roadTilePrefab1, 1);
        }
        RegisterToolSelected();
    }

    public void OnGrassButtonClicked()
    {
        ClearCurrentTool();
        selectedZoneType = Zone.ZoneType.Grass;
        RegisterToolSelected();
    }

    public void OnNuclearPowerPlantButtonClicked()
    {
        ClearCurrentTool();

        GameObject powerPlantObject = Instantiate(powerPlantAPrefab);
        PowerPlant powerPlant = powerPlantObject.AddComponent<PowerPlant>();

        powerPlant.Initialize("Power Plant A", 5000, 100, 50, 25, 3, 20000, powerPlantAPrefab);

        selectedBuilding = powerPlant;

        cursorManager.ShowBuildingPreview(powerPlantAPrefab, 3);
        RegisterToolSelected();
    }

    public Zone.ZoneType GetSelectedZoneType()
    {
        return selectedZoneType;
    }

    public IBuilding GetSelectedBuilding()
    {
        return selectedBuilding;
    }

    void ClearSelectedBuilding()
    {
        selectedBuilding = null;
    }

    void ClearSelectedForest()
    {
        selectedForest = null;
        selectedForestData = new ForestSelectionData
        {
            forestType = Forest.ForestType.None,
            prefab = null
        };
    }


    void ClearSelectedZoneType()
    {
        selectedZoneType = Zone.ZoneType.Grass;
    }

    private void ClearCurrentTool()
    {
        bulldozeMode = false;
        detailsMode = false;
        selectedBuilding = null;
        selectedForest = null;
        selectedForestData = new ForestSelectionData
        {
            forestType = Forest.ForestType.None,
            prefab = null
        };
        selectedZoneType = Zone.ZoneType.Grass;
        currentSubTypeId = -1;
        ghostPreviewPrefab = null;
        ghostPreviewSize = 1;
        if (cursorManager != null)
        {
            cursorManager.SetDefaultCursor();
            cursorManager.RemovePreview();
        }
        // TECH-14102 / Stage 8 D9: drop ToolSelected escape frame whenever tool clears (Esc dispatch already popped; idempotent for external paths).
        RemoveFrameFromStack(PopupType.ToolSelected);
        // Reset toolbar button visuals — moved here from removed GridManager Esc handler so any
        // tool-clear path (Esc, programmatic, switching tool) keeps button strip in sync.
        if (buildingSelectorMenuController != null)
        {
            buildingSelectorMenuController.DeselectAndUnpressAllButtons();
        }
    }

    private void SetGhostPreview(GameObject prefab, int size)
    {
        if (prefab == null)
        {
            ghostPreviewPrefab = null;
            ghostPreviewSize = 1;
            cursorManager.RemovePreview();
            return;
        }

        ghostPreviewPrefab = prefab;
        ghostPreviewSize = size;
        cursorManager.ShowBuildingPreview(prefab, size);
    }

    public void HideGhostPreview()
    {
        cursorManager.RemovePreview();
    }

    public void RestoreGhostPreview()
    {
        if (ghostPreviewPrefab == null)
        {
            return;
        }

        cursorManager.ShowBuildingPreview(ghostPreviewPrefab, ghostPreviewSize);
    }

    public void OnBulldozeButtonClicked()
    {
        ClearCurrentTool();
        cursorManager.SetBullDozerCursor();
        bulldozeMode = true;
        // TECH-14102 / Stage 8 D9: register ToolSelected escape frame on toolbar tool activation.
        RegisterToolSelected();
    }

    public bool isBulldozeMode()
    {
        return bulldozeMode;
    }

    public void OnDetailsButtonClicked()
    {
        bool wasDetailsMode = detailsMode;
        ClearCurrentTool();
        if (wasDetailsMode)
        {
            return;
        }
        detailsMode = true;
        cursorManager.SetDetailsCursor();
        // TECH-14102 / Stage 8 D9: register ToolSelected escape frame on toolbar tool activation.
        RegisterToolSelected();
    }

    public void OnRaiseResidentialTaxButtonClicked()
    {
        economyManager.RaiseResidentialTax();
        UpdateUI();
    }

    public void OnLowerResidentialTaxButtonClicked()
    {
        economyManager.LowerResidentialTax();
        UpdateUI();
    }

    public void OnRaiseCommercialTaxButtonClicked()
    {
        economyManager.RaiseCommercialTax();
        UpdateUI();
    }

    public void OnLowerCommercialTaxButtonClicked()
    {
        economyManager.LowerCommercialTax();
        UpdateUI();
    }

    public void OnRaiseIndustrialTaxButtonClicked()
    {
        economyManager.RaiseIndustrialTax();
        UpdateUI();
    }

    public void OnLowerIndustrialTaxButtonClicked()
    {
        economyManager.LowerIndustrialTax();
        UpdateUI();
    }
    #endregion
}
}
